using Greg.Xrm.Command;
using Greg.Xrm.Command.Services.Output;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.PowerPlatform.Dataverse.Client;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;

namespace PacxEmailTemplatePlugin.Commands
{
    /// <summary>
    /// Executor for the DeployEmailTemplateCommand.
    /// Reads HTML files from disk and upserts them as Dataverse email template records,
    /// mirroring the web-resource deployment pattern from Greg.Xrm.Command.
    /// </summary>
    public class DeployEmailTemplateCommandExecutor : ICommandExecutor<DeployEmailTemplateCommand>
    {
        /// <summary>Component type code for Email Template in Dynamics 365.</summary>
        private const int EmailTemplateComponentType = 36;

        private readonly IOutput _output;
        private readonly IOrganizationServiceAsync2 _organizationService;

        public DeployEmailTemplateCommandExecutor(
            IOutput output,
            IOrganizationServiceAsync2 organizationService)
        {
            _output = output ?? throw new ArgumentNullException(nameof(output));
            _organizationService = organizationService ?? throw new ArgumentNullException(nameof(organizationService));
        }

        public async Task<CommandResult> ExecuteAsync(DeployEmailTemplateCommand command, CancellationToken cancellationToken)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(command.Title))
                {
                    _output.WriteLine("Error: --title is required.", ConsoleColor.Red);
                    return CommandResult.Fail("--title is required.");
                }

                if (string.IsNullOrWhiteSpace(command.BodyFile) && string.IsNullOrWhiteSpace(command.SubjectFile))
                {
                    _output.WriteLine("Error: At least one of --body-file or --subject-file must be specified.", ConsoleColor.Red);
                    return CommandResult.Fail("At least one of --body-file or --subject-file must be specified.");
                }

                // --- Read body from file ---
                string? bodyContent = null;
                if (!string.IsNullOrWhiteSpace(command.BodyFile))
                {
                    if (!File.Exists(command.BodyFile))
                    {
                        _output.WriteLine($"Error: Body file not found: {command.BodyFile}", ConsoleColor.Red);
                        return CommandResult.Fail($"Body file not found: {command.BodyFile}");
                    }

                    _output.WriteLine($"Reading body from: {Path.GetFullPath(command.BodyFile)}", ConsoleColor.Cyan);
                    bodyContent = await File.ReadAllTextAsync(command.BodyFile, cancellationToken);
                    _output.WriteLine($"  {bodyContent.Length:N0} characters read.", ConsoleColor.DarkGray);
                }

                // --- Read subject from file or inline value ---
                string? subjectContent = command.Subject;
                if (!string.IsNullOrWhiteSpace(command.SubjectFile))
                {
                    if (!File.Exists(command.SubjectFile))
                    {
                        _output.WriteLine($"Error: Subject file not found: {command.SubjectFile}", ConsoleColor.Red);
                        return CommandResult.Fail($"Subject file not found: {command.SubjectFile}");
                    }

                    _output.WriteLine($"Reading subject from: {Path.GetFullPath(command.SubjectFile)}", ConsoleColor.Cyan);
                    subjectContent = await File.ReadAllTextAsync(command.SubjectFile, cancellationToken);
                    _output.WriteLine($"  {subjectContent.Length:N0} characters read.", ConsoleColor.DarkGray);
                }

                _output.WriteLine($"Deploying email template: {command.Title}", ConsoleColor.Cyan);

                // --- Check if template already exists ---
                var query = new QueryExpression("template")
                {
                    ColumnSet = new ColumnSet("templateid", "title"),
                    Criteria = new FilterExpression
                    {
                        Conditions =
                        {
                            new ConditionExpression("title", ConditionOperator.Equal, command.Title),
                            new ConditionExpression("templatetypecode", ConditionOperator.Equal, command.TemplateTypeCode ?? "email")
                        }
                    },
                    TopCount = 1
                };

                var results = await _organizationService.RetrieveMultipleAsync(query, cancellationToken);

                Entity template;
                bool isUpdate = results.Entities.Count > 0;

                if (isUpdate)
                {
                    template = results.Entities[0];
                    _output.WriteLine($"Existing template found (ID: {template.Id}) — updating.", ConsoleColor.Yellow);
                }
                else
                {
                    template = new Entity("template");
                    template["title"] = command.Title;
                    template["templatetypecode"] = command.TemplateTypeCode ?? "email";
                    _output.WriteLine("No existing template found — creating.", ConsoleColor.Yellow);
                }

                // --- Apply field values ---
                if (bodyContent is not null)
                    template["body"] = bodyContent;

                if (subjectContent is not null)
                    template["subject"] = subjectContent;

                if (!string.IsNullOrWhiteSpace(command.Description))
                    template["description"] = command.Description;

                if (command.LanguageCode.HasValue)
                    template["languagecode"] = command.LanguageCode.Value;

                if (command.IsPersonal.HasValue)
                    template["ispersonal"] = command.IsPersonal.Value;

                // --- Persist to Dataverse ---
                Guid templateId;
                if (isUpdate)
                {
                    await _organizationService.UpdateAsync(template, cancellationToken);
                    templateId = template.Id;
                    _output.WriteLine($"Updated: {command.Title} (ID: {templateId})", ConsoleColor.Green);
                }
                else
                {
                    templateId = await _organizationService.CreateAsync(template, cancellationToken);
                    _output.WriteLine($"Created: {command.Title} (ID: {templateId})", ConsoleColor.Green);
                }

                // --- Optionally add to solution ---
                if (!string.IsNullOrWhiteSpace(command.SolutionUniqueName))
                {
                    _output.WriteLine($"Adding to solution: {command.SolutionUniqueName}", ConsoleColor.Cyan);

                    var addRequest = new AddSolutionComponentRequest
                    {
                        ComponentId = templateId,
                        ComponentType = EmailTemplateComponentType,
                        SolutionUniqueName = command.SolutionUniqueName,
                        AddRequiredComponents = false,
                        DoNotIncludeSubcomponents = false
                    };

                    await _organizationService.ExecuteAsync(addRequest, cancellationToken);
                    _output.WriteLine($"Template added to solution '{command.SolutionUniqueName}'.", ConsoleColor.Green);
                }

                var result = CommandResult.Success();
                result["TemplateId"] = templateId;
                result["Title"] = command.Title;
                result["Operation"] = isUpdate ? "Update" : "Create";
                if (!string.IsNullOrWhiteSpace(command.SolutionUniqueName))
                    result["Solution"] = command.SolutionUniqueName;
                return result;
            }
            catch (Exception ex)
            {
                _output.WriteLine($"Error deploying email template: {ex.Message}", ConsoleColor.Red);
                return CommandResult.Fail($"Error deploying email template: {ex.Message}", ex);
            }
        }
    }
}
