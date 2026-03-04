using Greg.Xrm.Command;
using Greg.Xrm.Command.Services.Output;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.PowerPlatform.Dataverse.Client;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System.Text.Json;

namespace PacxEmailTemplatePlugin.Commands
{
    /// <summary>
    /// Executor for <see cref="CreateEmailTemplateCommand"/>.
    /// Reads a local template folder and creates a new Dynamics 365 email template record.
    /// Fails if a record with the same title already exists.
    /// </summary>
    public class CreateEmailTemplateCommandExecutor : ICommandExecutor<CreateEmailTemplateCommand>
    {
        /// <summary>Dynamics 365 solution component type code for Email Template.</summary>
        private const int EmailTemplateComponentType = 36;

        private readonly IOutput _output;
        private readonly IOrganizationServiceAsync2 _organizationService;

        /// <summary>
        /// Deserialization model for the root <c>templates/definitions.json</c>.
        /// Provides shared defaults (language, visibility, type) for all templates in the directory.
        /// </summary>
        private sealed class RootDefinition
        {
            /// <summary>Gets or sets the template type code (default: <c>email</c>).</summary>
            public string? TemplateTypeCode { get; set; }

            /// <summary>Gets or sets the LCID language code (default: 1033).</summary>
            public int? LanguageCode { get; set; }

            /// <summary>Gets or sets whether templates are personal by default.</summary>
            public bool? IsPersonal { get; set; }
        }

        /// <summary>
        /// Deserialization model for a template's own <c>definitions.json</c>.
        /// Template-level values override the root when both are present.
        /// </summary>
        private sealed class TemplateDefinition
        {
            /// <summary>Gets or sets the D365 record title (unique key).</summary>
            public string Title { get; set; } = string.Empty;

            /// <summary>Gets or sets the email subject line.</summary>
            public string? Subject { get; set; }

            /// <summary>Gets or sets the optional template description.</summary>
            public string? Description { get; set; }

            /// <summary>Gets or sets an override for the root template type code.</summary>
            public string? TemplateTypeCode { get; set; }

            /// <summary>Gets or sets an override for the root language code.</summary>
            public int? LanguageCode { get; set; }

            /// <summary>Gets or sets an override for the root personal flag.</summary>
            public bool? IsPersonal { get; set; }

            /// <summary>
            /// Gets or sets a dictionary of custom field values to set on creation.
            /// Types are inferred from the JSON value kind.
            /// </summary>
            public Dictionary<string, JsonElement>? AdditionalFields { get; set; }
        }

        /// <summary>
        /// Initializes a new instance of <see cref="CreateEmailTemplateCommandExecutor"/>.
        /// </summary>
        public CreateEmailTemplateCommandExecutor(
            IOutput output,
            IOrganizationServiceAsync2 organizationService)
        {
            _output = output ?? throw new ArgumentNullException(nameof(output));
            _organizationService = organizationService ?? throw new ArgumentNullException(nameof(organizationService));
        }

        /// <inheritdoc/>
        public async Task<CommandResult> ExecuteAsync(CreateEmailTemplateCommand command, CancellationToken cancellationToken)
        {
            try
            {
                // 1. Resolve folder.
                var folderPath = string.IsNullOrWhiteSpace(command.Path)
                    ? Directory.GetCurrentDirectory()
                    : System.IO.Path.GetFullPath(command.Path);

                if (!Directory.Exists(folderPath))
                {
                    _output.WriteLine($"Error: Folder not found: {folderPath}", ConsoleColor.Red);
                    return CommandResult.Fail($"Folder not found: {folderPath}");
                }

                // 2. Read definitions.json.
                var definitionsPath = System.IO.Path.Combine(folderPath, "definitions.json");
                if (!File.Exists(definitionsPath))
                {
                    _output.WriteLine($"Error: definitions.json not found in {folderPath}", ConsoleColor.Red);
                    return CommandResult.Fail("definitions.json not found.");
                }

                TemplateDefinition definition;
                try
                {
                    var json = await File.ReadAllTextAsync(definitionsPath, cancellationToken);
                    definition = JsonSerializer.Deserialize<TemplateDefinition>(
                        json,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
                        ?? throw new InvalidOperationException("Failed to deserialize definitions.json.");
                }
                catch (Exception ex)
                {
                    _output.WriteLine($"Error reading definitions.json: {ex.Message}", ConsoleColor.Red);
                    return CommandResult.Fail($"Error reading definitions.json: {ex.Message}", ex);
                }

                if (string.IsNullOrWhiteSpace(definition.Title))
                {
                    _output.WriteLine("Error: 'title' is required in definitions.json.", ConsoleColor.Red);
                    return CommandResult.Fail("'title' is required in definitions.json.");
                }

                if (string.IsNullOrWhiteSpace(definition.Subject))
                {
                    _output.WriteLine("Error: 'subject' is required in definitions.json.", ConsoleColor.Red);
                    return CommandResult.Fail("'subject' is required in definitions.json.");
                }

                // 3. Read root definitions.json (one level up) for shared defaults.
                RootDefinition? rootDefinition = null;
                var rootDefinitionsPath = System.IO.Path.Combine(
                    new DirectoryInfo(folderPath).Parent?.FullName ?? folderPath,
                    "definitions.json");
                if (File.Exists(rootDefinitionsPath))
                {
                    try
                    {
                        var rootJson = await File.ReadAllTextAsync(rootDefinitionsPath, cancellationToken);
                        rootDefinition = JsonSerializer.Deserialize<RootDefinition>(
                            rootJson,
                            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                        _output.WriteLine($"Loaded root settings from: {rootDefinitionsPath}", ConsoleColor.DarkGray);
                    }
                    catch (Exception ex)
                    {
                        _output.WriteLine($"Warning: Could not read root definitions.json: {ex.Message}", ConsoleColor.Yellow);
                    }
                }

                // Resolve common fields: template-level overrides root, root overrides hard-coded defaults.
                var templateTypeCode = definition.TemplateTypeCode ?? rootDefinition?.TemplateTypeCode ?? "email";
                var languageCode     = definition.LanguageCode     ?? rootDefinition?.LanguageCode     ?? 1033;
                var isPersonal       = definition.IsPersonal       ?? rootDefinition?.IsPersonal       ?? false;

                // 5. Read HTML body — filename matches folder name.
                var folderName   = new DirectoryInfo(folderPath).Name;
                var htmlFilePath = System.IO.Path.Combine(folderPath, $"{folderName}.html");
                string? body     = null;
                if (File.Exists(htmlFilePath))
                {
                    body = await File.ReadAllTextAsync(htmlFilePath, cancellationToken);
                    _output.WriteLine($"Loaded body from: {folderName}.html", ConsoleColor.Yellow);
                }

                // 4. Fail if the template already exists.
                _output.WriteLine($"Creating email template: {definition.Title}", ConsoleColor.Cyan);

                var query = new QueryExpression("template")
                {
                    ColumnSet = new ColumnSet("templateid"),
                    Criteria  = new FilterExpression
                    {
                        Conditions =
                        {
                            new ConditionExpression("title", ConditionOperator.Equal, definition.Title)
                        }
                    },
                    TopCount = 1
                };

                var results = await _organizationService.RetrieveMultipleAsync(query, cancellationToken);
                if (results.Entities.Count > 0)
                {
                    _output.WriteLine(
                        $"Error: A template with title '{definition.Title}' already exists " +
                        $"(ID: {results.Entities[0].Id}). Use 'push' to update it.",
                        ConsoleColor.Red);
                    return CommandResult.Fail($"Template '{definition.Title}' already exists. Use 'push' to update it.");
                }

                // 6. Build the entity.
                var template = new Entity("template");
                template["title"]                  = definition.Title;
                template["subject"]                = definition.Subject;
                template["subjectpresentationxml"] = WrapInPresentationXml(definition.Subject);
                template["templatetypecode"]       = templateTypeCode;
                template["languagecode"]           = languageCode;
                template["ispersonal"]             = isPersonal;

                if (!string.IsNullOrWhiteSpace(definition.Description))
                    template["description"] = definition.Description;

                if (body != null)
                {
                    template["body"]            = body;
                    template["presentationxml"] = WrapInPresentationXml(body);
                }

                // 7. Apply additionalFields (create only).
                foreach (var (key, value) in definition.AdditionalFields ?? [])
                {
                    template[key] = value.ValueKind switch
                    {
                        JsonValueKind.String                                    => value.GetString(),
                        JsonValueKind.Number when value.TryGetInt32(out var i)  => (object)i,
                        JsonValueKind.Number                                    => value.GetDouble(),
                        JsonValueKind.True                                      => (object)true,
                        JsonValueKind.False                                     => (object)false,
                        _                                                       => null
                    };
                }

                // 8. Create.
                var templateId = await _organizationService.CreateAsync(template, cancellationToken);
                _output.WriteLine($"Created: {definition.Title} (ID: {templateId})", ConsoleColor.Green);

                // 9. Optionally add to solution.
                if (!string.IsNullOrWhiteSpace(command.SolutionUniqueName))
                {
                    _output.WriteLine($"Adding to solution: {command.SolutionUniqueName}", ConsoleColor.Cyan);
                    var addRequest = new AddSolutionComponentRequest
                    {
                        ComponentId               = templateId,
                        ComponentType             = EmailTemplateComponentType,
                        SolutionUniqueName        = command.SolutionUniqueName,
                        AddRequiredComponents     = false,
                        DoNotIncludeSubcomponents = false
                    };
                    await _organizationService.ExecuteAsync(addRequest, cancellationToken);
                    _output.WriteLine($"Added to solution '{command.SolutionUniqueName}'.", ConsoleColor.Green);
                }

                var result = CommandResult.Success();
                result["TemplateId"] = templateId;
                result["Title"]      = definition.Title;
                result["Operation"]  = "Create";
                return result;
            }
            catch (Exception ex)
            {
                _output.WriteLine($"Error creating email template: {ex.Message}", ConsoleColor.Red);
                return CommandResult.Fail($"Error creating email template: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Wraps <paramref name="content"/> in the Dynamics 365 email template XML envelope
        /// used by both <c>presentationxml</c> and <c>subjectpresentationxml</c>.
        /// Any <c>]]&gt;</c> sequences are escaped to keep the XML well-formed.
        /// </summary>
        private static string WrapInPresentationXml(string content)
        {
            var escaped = content.Replace("]]>", "]]]]><![CDATA[>");
            return $"<template><![CDATA[{escaped}]]></template>";
        }
    }
}
