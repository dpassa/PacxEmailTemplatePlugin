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
    /// Creates the local folder structure for email templates, optionally performing
    /// a bulk pull from Dynamics 365 to pre-populate files from existing records.
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
                if (!command.Remote && string.IsNullOrWhiteSpace(command.Name))
                {
                    _output.WriteLine("Error: --name is required when not using --remote.", ConsoleColor.Red);
                    return CommandResult.Fail("--name is required when not using --remote.");
                }

                var basePath = string.IsNullOrWhiteSpace(command.Path)
                    ? Directory.GetCurrentDirectory()
                    : System.IO.Path.GetFullPath(command.Path);

                if (command.Remote)
                {
                    _output.WriteLine("NOTE: Run this command against your DEV environment.", ConsoleColor.Yellow);
                    _output.WriteLine("      Pulling templates from DEV ensures version control captures the baseline", ConsoleColor.Yellow);
                    _output.WriteLine("      state of unmanaged (custom) templates and lets the whole team adopt this", ConsoleColor.Yellow);
                    _output.WriteLine("      tooling even on already-started projects.", ConsoleColor.Yellow);
                    _output.WriteLine(string.Empty);

                    return await PullFromRemoteAsync(command, basePath, cancellationToken);
                }

                return await LocalInitAsync(command, basePath, cancellationToken);
            }
            catch (Exception ex)
            {
                _output.WriteLine($"Error initializing email template: {ex.Message}", ConsoleColor.Red);
                return CommandResult.Fail($"Error initializing email template: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Pulls all templates from D365 (optionally filtered by name) and scaffolds
        /// a local folder for each one, skipping folders that already exist.
        /// </summary>
        private async Task<CommandResult> PullFromRemoteAsync(
            InitEmailTemplateCommand command,
            string basePath,
            CancellationToken cancellationToken)
        {
            var query = new QueryExpression("template")
            {
                ColumnSet = new ColumnSet(
                    "templateid", "title", "subject", "body",
                    "description", "templatetypecode", "languagecode", "ispersonal"),
            };

            if (!string.IsNullOrWhiteSpace(command.Name))
            {
                query.Criteria.AddCondition("title", ConditionOperator.Equal, command.Name);
                _output.WriteLine($"Fetching template '{command.Name}' from D365...", ConsoleColor.Cyan);
            }
            else
            {
                _output.WriteLine("Fetching all templates from D365...", ConsoleColor.Cyan);
            }

            var results = await _organizationService.RetrieveMultipleAsync(query, cancellationToken);

            if (results.Entities.Count == 0)
            {
                var msg = string.IsNullOrWhiteSpace(command.Name)
                    ? "No templates found in D365."
                    : $"No template with title '{command.Name}' found in D365.";
                _output.WriteLine($"Error: {msg}", ConsoleColor.Red);
                return CommandResult.Fail(msg);
            }

            _output.WriteLine($"Found {results.Entities.Count} template(s).", ConsoleColor.Yellow);

            var rootDefinitionsPath = System.IO.Path.Combine(basePath, "definitions.json");
            int created = 0;
            int skipped = 0;

            foreach (var record in results.Entities)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var title            = record.GetAttributeValue<string>("title")            ?? string.Empty;
                var subject          = record.GetAttributeValue<string>("subject")          ?? string.Empty;
                var description      = record.GetAttributeValue<string>("description")      ?? string.Empty;
                var templateTypeCode = record.GetAttributeValue<string>("templatetypecode") ?? "email";
                var languageCode     = record.GetAttributeValue<int>("languagecode");
                if (languageCode == 0) languageCode = 1033;
                var isPersonal       = record.GetAttributeValue<bool>("ispersonal");
                var body             = record.GetAttributeValue<string>("body");

                var folderName = SanitizeFolderName(title);
                var folderPath = System.IO.Path.Combine(basePath, folderName);

                if (Directory.Exists(folderPath))
                {
                    _output.WriteLine($"Skipped (folder already exists): {folderPath}", ConsoleColor.DarkGray);
                    skipped++;
                    continue;
                }

                // Root definitions.json — written once for the first template processed.
                if (!File.Exists(rootDefinitionsPath))
                {
                    var rootDefs = new { templateTypeCode, languageCode, isPersonal };
                    var rootJson = JsonSerializer.Serialize(rootDefs, new JsonSerializerOptions { WriteIndented = true });
                    await File.WriteAllTextAsync(rootDefinitionsPath, rootJson, cancellationToken);
                    _output.WriteLine($"Created: {rootDefinitionsPath}", ConsoleColor.Green);
                }

                Directory.CreateDirectory(folderPath);
                _output.WriteLine($"Created folder: {folderPath}", ConsoleColor.Green);

                var definitionsPath = System.IO.Path.Combine(folderPath, "definitions.json");
                var defs = new { title, subject, description };
                var json = JsonSerializer.Serialize(defs, new JsonSerializerOptions { WriteIndented = true });
                await File.WriteAllTextAsync(definitionsPath, json, cancellationToken);
                _output.WriteLine($"Created: {definitionsPath}", ConsoleColor.Green);

                var htmlFilePath = System.IO.Path.Combine(folderPath, $"{folderName}.html");
                await File.WriteAllTextAsync(htmlFilePath, body ?? string.Empty, cancellationToken);
                _output.WriteLine($"Created: {htmlFilePath}", ConsoleColor.Green);

                created++;
            }

            _output.WriteLine(string.Empty);
            _output.WriteLine($"Done. Created: {created}, Skipped: {skipped}.", ConsoleColor.Green);

            var result = CommandResult.Success();
            result["BasePath"] = basePath;
            result["Created"]  = created;
            result["Skipped"]  = skipped;
            result["Source"]   = "D365";
            return result;
        }

        /// <summary>
        /// Creates a single local stub folder (no remote connection).
        /// </summary>
        private async Task<CommandResult> LocalInitAsync(
            InitEmailTemplateCommand command,
            string basePath,
            CancellationToken cancellationToken)
        {
            var folderPath = System.IO.Path.Combine(basePath, command.Name!);

            if (Directory.Exists(folderPath))
            {
                _output.WriteLine($"Error: Folder already exists: {folderPath}", ConsoleColor.Red);
                return CommandResult.Fail($"Folder already exists: {folderPath}");
            }

            Directory.CreateDirectory(folderPath);
            _output.WriteLine($"Created folder: {folderPath}", ConsoleColor.Green);

            var rootDefinitionsPath = System.IO.Path.Combine(basePath, "definitions.json");
            if (!File.Exists(rootDefinitionsPath))
            {
                var rootDefs = new { templateTypeCode = "email", languageCode = 1033, isPersonal = false };
                var rootJson = JsonSerializer.Serialize(rootDefs, new JsonSerializerOptions { WriteIndented = true });
                await File.WriteAllTextAsync(rootDefinitionsPath, rootJson, cancellationToken);
                _output.WriteLine($"Created: {rootDefinitionsPath}", ConsoleColor.Green);
            }
            else
            {
                _output.WriteLine($"Skipped (already exists): {rootDefinitionsPath}", ConsoleColor.DarkGray);
            }

            var definitionsPath = System.IO.Path.Combine(folderPath, "definitions.json");
            var defs = new { title = command.Name, subject = string.Empty, description = string.Empty };
            var json = JsonSerializer.Serialize(defs, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(definitionsPath, json, cancellationToken);
            _output.WriteLine($"Created: {definitionsPath}", ConsoleColor.Green);

            var htmlFilePath = System.IO.Path.Combine(folderPath, $"{command.Name}.html");
            await File.WriteAllTextAsync(htmlFilePath, string.Empty, cancellationToken);
            _output.WriteLine($"Created: {htmlFilePath}", ConsoleColor.Green);

            var result = CommandResult.Success();
            result["Folder"]              = folderPath;
            result["RootDefinitionsFile"] = rootDefinitionsPath;
            result["DefinitionsFile"]     = definitionsPath;
            result["HtmlFile"]            = htmlFilePath;
            return result;
        }

        /// <summary>
        /// Replaces characters that are invalid in file/folder names with underscores.
        /// </summary>
        private static string SanitizeFolderName(string name)
        {
            var invalid = System.IO.Path.GetInvalidFileNameChars();
            return string.Concat(name.Select(c => invalid.Contains(c) ? '_' : c)).Trim();
        }
    }
}
