using Greg.Xrm.Command;
using Greg.Xrm.Command.Services.Output;
using Microsoft.PowerPlatform.Dataverse.Client;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System.Text.RegularExpressions;

namespace PacxEmailTemplatePlugin.Commands
{
    /// <summary>
    /// Executor for the ExportEmailTemplateCommand.
    /// Retrieves a template from Dataverse and writes its body and/or subject
    /// to local files, enabling source-control workflows for email templates.
    /// </summary>
    public class ExportEmailTemplateCommandExecutor : ICommandExecutor<ExportEmailTemplateCommand>
    {
        private readonly IOutput _output;
        private readonly IOrganizationServiceAsync2 _organizationService;

        public ExportEmailTemplateCommandExecutor(
            IOutput output,
            IOrganizationServiceAsync2 organizationService)
        {
            _output = output ?? throw new ArgumentNullException(nameof(output));
            _organizationService = organizationService ?? throw new ArgumentNullException(nameof(organizationService));
        }

        public async Task<CommandResult> ExecuteAsync(ExportEmailTemplateCommand command, CancellationToken cancellationToken)
        {
            try
            {
                // --- Validate ---
                bool hasId = command.TemplateId.HasValue && command.TemplateId.Value != Guid.Empty;
                bool hasTitle = !string.IsNullOrWhiteSpace(command.Title);

                if (!hasId && !hasTitle)
                {
                    _output.WriteLine("Error: Provide either --template-id or --title.", ConsoleColor.Red);
                    return CommandResult.Fail("Either --template-id or --title is required.");
                }

                if (string.IsNullOrWhiteSpace(command.OutputDir))
                {
                    _output.WriteLine("Error: --output-dir is required.", ConsoleColor.Red);
                    return CommandResult.Fail("--output-dir is required.");
                }

                if (!command.IncludeBody && !command.IncludeSubject)
                {
                    _output.WriteLine("Error: At least one of --include-body or --include-subject must be true.", ConsoleColor.Red);
                    return CommandResult.Fail("At least one of --include-body or --include-subject must be true.");
                }

                // --- Retrieve template ---
                var columns = new List<string> { "templateid", "title" };
                if (command.IncludeBody)
                    columns.Add("body");
                if (command.IncludeSubject)
                    columns.Add("subject");

                Entity template = hasId
                    ? await RetrieveByIdAsync(command.TemplateId!.Value, columns, cancellationToken)
                    : await RetrieveByTitleAsync(command.Title!, columns, cancellationToken);

                if (template is null)
                    return CommandResult.Fail("Template not found.");

                var templateTitle = template.GetAttributeValue<string>("title") ?? template.Id.ToString();
                _output.WriteLine($"Template: {templateTitle} (ID: {template.Id})", ConsoleColor.Yellow);

                // --- Ensure output directory exists ---
                var outputDir = Path.GetFullPath(command.OutputDir);
                Directory.CreateDirectory(outputDir);
                _output.WriteLine($"Output directory: {outputDir}", ConsoleColor.DarkGray);

                var safeTitle = SanitizeFileName(templateTitle);
                var exportedFiles = new List<string>();

                // --- Export body ---
                if (command.IncludeBody)
                {
                    var body = template.GetAttributeValue<string>("body");
                    if (body is null)
                    {
                        _output.WriteLine("Warning: Template body is empty, skipping body export.", ConsoleColor.Yellow);
                    }
                    else
                    {
                        var bodyPath = Path.Combine(outputDir, $"{safeTitle}-body.html");
                        await File.WriteAllTextAsync(bodyPath, body, cancellationToken);
                        _output.WriteLine($"Body exported → {bodyPath} ({body.Length:N0} chars)", ConsoleColor.Green);
                        exportedFiles.Add(bodyPath);
                    }
                }

                // --- Export subject ---
                if (command.IncludeSubject)
                {
                    var subject = template.GetAttributeValue<string>("subject");
                    if (subject is null)
                    {
                        _output.WriteLine("Warning: Template subject is empty, skipping subject export.", ConsoleColor.Yellow);
                    }
                    else
                    {
                        var subjectPath = Path.Combine(outputDir, $"{safeTitle}-subject.txt");
                        await File.WriteAllTextAsync(subjectPath, subject, cancellationToken);
                        _output.WriteLine($"Subject exported → {subjectPath} ({subject.Length:N0} chars)", ConsoleColor.Green);
                        exportedFiles.Add(subjectPath);
                    }
                }

                if (exportedFiles.Count == 0)
                {
                    _output.WriteLine("Warning: No files were exported (template fields were empty).", ConsoleColor.Yellow);
                }

                var result = CommandResult.Success();
                result["TemplateId"] = template.Id;
                result["TemplateTitle"] = templateTitle;
                result["OutputDir"] = outputDir;
                result["ExportedFiles"] = string.Join(", ", exportedFiles);
                return result;
            }
            catch (Exception ex)
            {
                _output.WriteLine($"Error exporting email template: {ex.Message}", ConsoleColor.Red);
                return CommandResult.Fail($"Error exporting email template: {ex.Message}", ex);
            }
        }

        // -----------------------------------------------------------------------
        // Retrieval helpers
        // -----------------------------------------------------------------------

        private async Task<Entity> RetrieveByIdAsync(Guid templateId, List<string> columns, CancellationToken cancellationToken)
        {
            _output.WriteLine($"Retrieving template by ID: {templateId}", ConsoleColor.Cyan);

            var query = new QueryExpression("template")
            {
                ColumnSet = new ColumnSet([.. columns]),
                Criteria = new FilterExpression
                {
                    Conditions = { new ConditionExpression("templateid", ConditionOperator.Equal, templateId) }
                },
                TopCount = 1
            };

            var results = await _organizationService.RetrieveMultipleAsync(query, cancellationToken);

            if (results.Entities.Count == 0)
            {
                _output.WriteLine($"Error: Template with ID {templateId} not found.", ConsoleColor.Red);
                return null!;
            }

            return results.Entities[0];
        }

        private async Task<Entity> RetrieveByTitleAsync(string title, List<string> columns, CancellationToken cancellationToken)
        {
            _output.WriteLine($"Retrieving template by title: {title}", ConsoleColor.Cyan);

            var query = new QueryExpression("template")
            {
                ColumnSet = new ColumnSet([.. columns]),
                Criteria = new FilterExpression
                {
                    Conditions = { new ConditionExpression("title", ConditionOperator.Equal, title) }
                },
                TopCount = 1
            };

            var results = await _organizationService.RetrieveMultipleAsync(query, cancellationToken);

            if (results.Entities.Count == 0)
            {
                _output.WriteLine($"Error: Template with title '{title}' not found.", ConsoleColor.Red);
                return null!;
            }

            return results.Entities[0];
        }

        // -----------------------------------------------------------------------
        // File name helper
        // -----------------------------------------------------------------------

        /// <summary>
        /// Strips characters that are illegal in file names and collapses whitespace.
        /// </summary>
        private static string SanitizeFileName(string name)
        {
            var invalid = new string(Path.GetInvalidFileNameChars());
            var pattern = $"[{Regex.Escape(invalid)}]";
            var safe = Regex.Replace(name, pattern, "_");
            safe = Regex.Replace(safe, @"\s+", "_");
            return safe.Trim('_').Length > 0 ? safe.Trim('_') : "template";
        }
    }
}
