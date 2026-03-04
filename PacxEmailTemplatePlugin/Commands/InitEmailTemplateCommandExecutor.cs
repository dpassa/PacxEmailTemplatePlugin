using Greg.Xrm.Command;
using Greg.Xrm.Command.Services.Output;
using Microsoft.PowerPlatform.Dataverse.Client;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System.Text.Json;

namespace PacxEmailTemplatePlugin.Commands
{
    /// <summary>
    /// Executor for <see cref="InitEmailTemplateCommand"/>.
    /// Creates the local folder structure for an email template, optionally
    /// pre-populating files from an existing Dynamics 365 record.
    /// </summary>
    public class InitEmailTemplateCommandExecutor : ICommandExecutor<InitEmailTemplateCommand>
    {
        private readonly IOutput _output;
        private readonly IOrganizationServiceAsync2 _organizationService;

        /// <summary>
        /// Initializes a new instance of <see cref="InitEmailTemplateCommandExecutor"/>.
        /// </summary>
        public InitEmailTemplateCommandExecutor(
            IOutput output,
            IOrganizationServiceAsync2 organizationService)
        {
            _output = output ?? throw new ArgumentNullException(nameof(output));
            _organizationService = organizationService ?? throw new ArgumentNullException(nameof(organizationService));
        }

        /// <inheritdoc/>
        public async Task<CommandResult> ExecuteAsync(InitEmailTemplateCommand command, CancellationToken cancellationToken)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(command.Name))
                {
                    _output.WriteLine("Error: --name is required.", ConsoleColor.Red);
                    return CommandResult.Fail("--name is required.");
                }

                var basePath = string.IsNullOrWhiteSpace(command.Path)
                    ? Directory.GetCurrentDirectory()
                    : System.IO.Path.GetFullPath(command.Path);

                var folderPath = System.IO.Path.Combine(basePath, command.Name);

                if (Directory.Exists(folderPath))
                {
                    _output.WriteLine($"Error: Folder already exists: {folderPath}", ConsoleColor.Red);
                    return CommandResult.Fail($"Folder already exists: {folderPath}");
                }

                // Values written to definitions.json — defaults for local-only init.
                string title           = command.Name;
                string subject         = string.Empty;
                string description     = string.Empty;
                string templateTypeCode = "email";
                int    languageCode    = 1033;
                bool   isPersonal      = false;
                string? body           = null;

                if (command.Remote)
                {
                    _output.WriteLine($"Fetching '{command.Name}' from D365...", ConsoleColor.Cyan);

                    var query = new QueryExpression("template")
                    {
                        ColumnSet = new ColumnSet(
                            "templateid", "title", "subject", "body",
                            "description", "templatetypecode", "languagecode", "ispersonal"),
                        Criteria = new FilterExpression
                        {
                            Conditions =
                            {
                                new ConditionExpression("title", ConditionOperator.Equal, command.Name)
                            }
                        },
                        TopCount = 1
                    };

                    var results = await _organizationService.RetrieveMultipleAsync(query, cancellationToken);

                    if (results.Entities.Count == 0)
                    {
                        _output.WriteLine($"Error: No template with title '{command.Name}' found in D365.", ConsoleColor.Red);
                        return CommandResult.Fail($"No template with title '{command.Name}' found in D365.");
                    }

                    var record = results.Entities[0];
                    _output.WriteLine($"Found template (ID: {record.Id}).", ConsoleColor.Yellow);

                    title            = record.GetAttributeValue<string>("title")           ?? command.Name;
                    subject          = record.GetAttributeValue<string>("subject")         ?? string.Empty;
                    description      = record.GetAttributeValue<string>("description")     ?? string.Empty;
                    templateTypeCode = record.GetAttributeValue<string>("templatetypecode") ?? "email";
                    languageCode     = record.GetAttributeValue<int>("languagecode");
                    if (languageCode == 0) languageCode = 1033;
                    isPersonal       = record.GetAttributeValue<bool>("ispersonal");
                    body             = record.GetAttributeValue<string>("body");
                }

                // Create folder and write files.
                Directory.CreateDirectory(folderPath);
                _output.WriteLine($"Created folder: {folderPath}", ConsoleColor.Green);

                // Root definitions.json — written once per templates/ directory.
                var rootDefinitionsPath = System.IO.Path.Combine(basePath, "definitions.json");
                if (!File.Exists(rootDefinitionsPath))
                {
                    var rootDefinitions = new { templateTypeCode, languageCode, isPersonal };
                    var rootJson = JsonSerializer.Serialize(rootDefinitions, new JsonSerializerOptions { WriteIndented = true });
                    await File.WriteAllTextAsync(rootDefinitionsPath, rootJson, cancellationToken);
                    _output.WriteLine($"Created: {rootDefinitionsPath}", ConsoleColor.Green);
                }
                else
                {
                    _output.WriteLine($"Skipped (already exists): {rootDefinitionsPath}", ConsoleColor.DarkGray);
                }

                // Template-specific definitions.json — title, subject, description only.
                var definitionsPath = System.IO.Path.Combine(folderPath, "definitions.json");
                var definitions = new { title, subject, description };
                var json = JsonSerializer.Serialize(definitions, new JsonSerializerOptions { WriteIndented = true });
                await File.WriteAllTextAsync(definitionsPath, json, cancellationToken);
                _output.WriteLine($"Created: {definitionsPath}", ConsoleColor.Green);

                var htmlFilePath = System.IO.Path.Combine(folderPath, $"{command.Name}.html");
                await File.WriteAllTextAsync(htmlFilePath, body ?? string.Empty, cancellationToken);
                _output.WriteLine($"Created: {htmlFilePath}", ConsoleColor.Green);

                var result = CommandResult.Success();
                result["Folder"]              = folderPath;
                result["RootDefinitionsFile"] = rootDefinitionsPath;
                result["DefinitionsFile"]     = definitionsPath;
                result["HtmlFile"]            = htmlFilePath;
                if (command.Remote)
                    result["Source"] = "D365";
                return result;
            }
            catch (Exception ex)
            {
                _output.WriteLine($"Error initializing email template: {ex.Message}", ConsoleColor.Red);
                return CommandResult.Fail($"Error initializing email template: {ex.Message}", ex);
            }
        }
    }
}
