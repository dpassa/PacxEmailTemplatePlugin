using Greg.Xrm.Command;
using Greg.Xrm.Command.Services.Output;
using Microsoft.PowerPlatform.Dataverse.Client;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;

namespace PacxEmailTemplatePlugin.Commands
{
    /// <summary>
    /// Executor for the UpsertEmailTemplateCommand.
    /// </summary>
    public class UpsertEmailTemplateCommandExecutor : ICommandExecutor<UpsertEmailTemplateCommand>
    {
        private readonly IOutput _output;
        private readonly IOrganizationServiceAsync2 _organizationService;

        public UpsertEmailTemplateCommandExecutor(
            IOutput output,
            IOrganizationServiceAsync2 organizationService)
        {
            _output = output ?? throw new ArgumentNullException(nameof(output));
            _organizationService = organizationService ?? throw new ArgumentNullException(nameof(organizationService));
        }

        public async Task<CommandResult> ExecuteAsync(UpsertEmailTemplateCommand command, CancellationToken cancellationToken)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(command.Title))
                {
                    _output.WriteLine("Error: Title is required.", ConsoleColor.Red);
                    return CommandResult.Fail("Title is required.");
                }

                _output.WriteLine($"Upserting email template: {command.Title}", ConsoleColor.Cyan);

                // Check if template exists based on title
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
                bool isUpdate = false;

                if (results.Entities.Count > 0)
                {
                    // Update existing template
                    template = results.Entities[0];
                    isUpdate = true;
                    _output.WriteLine($"Found existing template with ID: {template.Id}", ConsoleColor.Yellow);
                }
                else
                {
                    // Create new template
                    template = new Entity("template");
                    template["title"] = command.Title;
                }

                // Set template fields
                if (!string.IsNullOrWhiteSpace(command.Subject))
                    template["subject"] = command.Subject;

                if (!string.IsNullOrWhiteSpace(command.Body))
                    template["body"] = command.Body;

                if (!string.IsNullOrWhiteSpace(command.Description))
                    template["description"] = command.Description;

                if (!string.IsNullOrWhiteSpace(command.TemplateTypeCode))
                    template["templatetypecode"] = command.TemplateTypeCode;

                if (!string.IsNullOrWhiteSpace(command.MimeType))
                    template["mimetype"] = command.MimeType;

                if (!string.IsNullOrWhiteSpace(command.SubjectPresentationXml))
                    template["subjectpresentationxml"] = command.SubjectPresentationXml;

                if (!string.IsNullOrWhiteSpace(command.PresentationXml))
                    template["presentationxml"] = command.PresentationXml;

                if (command.LanguageCode.HasValue)
                    template["languagecode"] = command.LanguageCode.Value;

                if (command.IsPersonal.HasValue)
                    template["ispersonal"] = command.IsPersonal.Value;

                Guid templateId;
                if (isUpdate)
                {
                    await _organizationService.UpdateAsync(template, cancellationToken);
                    templateId = template.Id;
                    _output.WriteLine($"Successfully updated email template: {command.Title} (ID: {templateId})", ConsoleColor.Green);
                }
                else
                {
                    templateId = await _organizationService.CreateAsync(template, cancellationToken);
                    _output.WriteLine($"Successfully created email template: {command.Title} (ID: {templateId})", ConsoleColor.Green);
                }

                var result = CommandResult.Success();
                result["TemplateId"] = templateId;
                result["Title"] = command.Title;
                result["Operation"] = isUpdate ? "Update" : "Create";
                return result;
            }
            catch (Exception ex)
            {
                _output.WriteLine($"Error upserting email template: {ex.Message}", ConsoleColor.Red);
                return CommandResult.Fail($"Error upserting email template: {ex.Message}", ex);
            }
        }
    }
}
