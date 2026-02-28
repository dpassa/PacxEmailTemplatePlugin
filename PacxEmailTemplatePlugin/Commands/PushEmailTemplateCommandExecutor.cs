using Greg.Xrm.Command;
using Greg.Xrm.Command.Services.Output;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.PowerPlatform.Dataverse.Client;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;

namespace PacxEmailTemplatePlugin.Commands
{
    /// <summary>
    /// Executor for <see cref="PushEmailTemplateCommand"/>.
    /// <list type="bullet">
    ///   <item>Auto-detects <c>body.html</c> / <c>subject.html|.txt</c> from the current
    ///         working directory when no explicit path is supplied.</item>
    ///   <item>Derives <c>presentationxml</c> and <c>subjectpresentationxml</c> automatically
    ///         from the body and subject content — no separate XML parameters required.</item>
    ///   <item>Applies sensible defaults: <c>languagecode=1033</c>, <c>ispersonal=false</c>.</item>
    /// </list>
    /// </summary>
    public class PushEmailTemplateCommandExecutor : ICommandExecutor<PushEmailTemplateCommand>
    {
        /// <summary>Dynamics 365 solution component type code for Email Template.</summary>
        private const int EmailTemplateComponentType = 36;

        // Default file names scanned in the current working directory.
        private const string DefaultBodyFileName = "body.html";
        private static readonly string[] DefaultSubjectFileNames = ["subject.html", "subject.txt"];

        private readonly IOutput _output;
        private readonly IOrganizationServiceAsync2 _organizationService;

        public PushEmailTemplateCommandExecutor(
            IOutput output,
            IOrganizationServiceAsync2 organizationService)
        {
            _output = output ?? throw new ArgumentNullException(nameof(output));
            _organizationService = organizationService ?? throw new ArgumentNullException(nameof(organizationService));
        }

        // -----------------------------------------------------------------------
        // Entry point
        // -----------------------------------------------------------------------

        public async Task<CommandResult> ExecuteAsync(PushEmailTemplateCommand command, CancellationToken cancellationToken)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(command.Title))
                {
                    _output.WriteLine("Error: --title is required.", ConsoleColor.Red);
                    return CommandResult.Fail("--title is required.");
                }

                var cwd = Directory.GetCurrentDirectory();
                _output.WriteLine($"Working directory: {cwd}", ConsoleColor.DarkGray);

                // --- Resolve body file ------------------------------------------
                var bodyFilePath = ResolveFilePath(
                    command.Body,
                    [DefaultBodyFileName],
                    "body",
                    cwd);

                // --- Resolve subject file ----------------------------------------
                var subjectFilePath = ResolveFilePath(
                    command.Subject,
                    DefaultSubjectFileNames,
                    "subject",
                    cwd);

                // At least one of body / subject must be resolvable
                if (bodyFilePath is null && subjectFilePath is null)
                {
                    _output.WriteLine(
                        $"Error: No body or subject files found. " +
                        $"Supply --body / --subject paths, or place '{DefaultBodyFileName}' / " +
                        $"'{string.Join("' / '", DefaultSubjectFileNames)}' in the current directory.",
                        ConsoleColor.Red);
                    return CommandResult.Fail("No body or subject source files found.");
                }

                // Validate explicit paths exist (auto-detected paths are already verified)
                if (bodyFilePath is not null && !string.IsNullOrWhiteSpace(command.Body) && !File.Exists(bodyFilePath))
                {
                    _output.WriteLine($"Error: Body file not found: {bodyFilePath}", ConsoleColor.Red);
                    return CommandResult.Fail($"Body file not found: {bodyFilePath}");
                }

                if (subjectFilePath is not null && !string.IsNullOrWhiteSpace(command.Subject) && !File.Exists(subjectFilePath))
                {
                    _output.WriteLine($"Error: Subject file not found: {subjectFilePath}", ConsoleColor.Red);
                    return CommandResult.Fail($"Subject file not found: {subjectFilePath}");
                }

                // --- Read file contents -----------------------------------------
                string? bodyContent = null;
                if (bodyFilePath is not null)
                {
                    _output.WriteLine($"Reading body  : {bodyFilePath}", ConsoleColor.Cyan);
                    bodyContent = await File.ReadAllTextAsync(bodyFilePath, cancellationToken);
                    _output.WriteLine($"  {bodyContent.Length:N0} chars", ConsoleColor.DarkGray);
                }

                string? subjectContent = null;
                if (subjectFilePath is not null)
                {
                    _output.WriteLine($"Reading subject: {subjectFilePath}", ConsoleColor.Cyan);
                    subjectContent = await File.ReadAllTextAsync(subjectFilePath, cancellationToken);
                    _output.WriteLine($"  {subjectContent.Length:N0} chars", ConsoleColor.DarkGray);
                }

                _output.WriteLine($"Pushing template: {command.Title}", ConsoleColor.Cyan);

                // --- Check whether the template already exists ------------------
                var query = new QueryExpression("template")
                {
                    ColumnSet = new ColumnSet("templateid", "title"),
                    Criteria = new FilterExpression
                    {
                        Conditions =
                        {
                            new ConditionExpression("title", ConditionOperator.Equal, command.Title),
                            new ConditionExpression("templatetypecode", ConditionOperator.Equal, command.TemplateTypeCode)
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
                    template["title"]            = command.Title;
                    template["templatetypecode"] = command.TemplateTypeCode;
                    _output.WriteLine("No existing template found — creating.", ConsoleColor.Yellow);
                }

                // --- Apply default / command values -----------------------------
                template["languagecode"] = command.LanguageCode;
                template["ispersonal"]   = command.IsPersonal;

                if (!string.IsNullOrWhiteSpace(command.Description))
                    template["description"] = command.Description;

                // --- Body: set body + auto-derived presentationxml ---------------
                if (bodyContent is not null)
                {
                    template["body"]            = bodyContent;
                    template["presentationxml"] = WrapInPresentationXml(bodyContent);
                    _output.WriteLine("  body + presentationxml set.", ConsoleColor.DarkGray);
                }

                // --- Subject: set subject + auto-derived subjectpresentationxml --
                if (subjectContent is not null)
                {
                    template["subject"]                  = subjectContent;
                    template["subjectpresentationxml"]   = WrapInPresentationXml(subjectContent);
                    _output.WriteLine("  subject + subjectpresentationxml set.", ConsoleColor.DarkGray);
                }

                // --- Persist to Dataverse ----------------------------------------
                Guid templateId;
                if (isUpdate)
                {
                    await _organizationService.UpdateAsync(template, cancellationToken);
                    templateId = template.Id;
                    _output.WriteLine($"Updated : {command.Title} (ID: {templateId})", ConsoleColor.Green);
                }
                else
                {
                    templateId = await _organizationService.CreateAsync(template, cancellationToken);
                    _output.WriteLine($"Created : {command.Title} (ID: {templateId})", ConsoleColor.Green);
                }

                // --- Optional: add to solution -----------------------------------
                if (!string.IsNullOrWhiteSpace(command.SolutionUniqueName))
                {
                    _output.WriteLine($"Adding to solution: {command.SolutionUniqueName}", ConsoleColor.Cyan);

                    var addRequest = new AddSolutionComponentRequest
                    {
                        ComponentId           = templateId,
                        ComponentType         = EmailTemplateComponentType,
                        SolutionUniqueName    = command.SolutionUniqueName,
                        AddRequiredComponents = false,
                        DoNotIncludeSubcomponents = false
                    };

                    await _organizationService.ExecuteAsync(addRequest, cancellationToken);
                    _output.WriteLine($"Added to solution '{command.SolutionUniqueName}'.", ConsoleColor.Green);
                }

                var result = CommandResult.Success();
                result["TemplateId"] = templateId;
                result["Title"]      = command.Title;
                result["Operation"]  = isUpdate ? "Update" : "Create";
                if (bodyFilePath is not null)    result["BodyFile"]    = bodyFilePath;
                if (subjectFilePath is not null) result["SubjectFile"] = subjectFilePath;
                if (!string.IsNullOrWhiteSpace(command.SolutionUniqueName))
                    result["Solution"] = command.SolutionUniqueName;
                return result;
            }
            catch (Exception ex)
            {
                _output.WriteLine($"Error pushing email template: {ex.Message}", ConsoleColor.Red);
                return CommandResult.Fail($"Error pushing email template: {ex.Message}", ex);
            }
        }

        // -----------------------------------------------------------------------
        // File resolution
        // -----------------------------------------------------------------------

        /// <summary>
        /// Returns the resolved absolute file path for a body or subject source.
        /// <list type="number">
        ///   <item>If <paramref name="explicitPath"/> is provided it is used as-is
        ///         (existence is validated later by the caller).</item>
        ///   <item>Otherwise each name in <paramref name="defaults"/> is checked inside
        ///         <paramref name="cwd"/>; the first match is returned.</item>
        ///   <item>Returns <see langword="null"/> when nothing is found.</item>
        /// </list>
        /// </summary>
        private string? ResolveFilePath(
            string? explicitPath,
            string[] defaults,
            string label,
            string cwd)
        {
            if (!string.IsNullOrWhiteSpace(explicitPath))
                return Path.GetFullPath(explicitPath);

            foreach (var name in defaults)
            {
                var candidate = Path.Combine(cwd, name);
                if (File.Exists(candidate))
                {
                    _output.WriteLine($"Auto-detected {label}: {candidate}", ConsoleColor.DarkGray);
                    return candidate;
                }
            }

            _output.WriteLine(
                $"No {label} file found (checked: {string.Join(", ", defaults)}).",
                ConsoleColor.DarkGray);
            return null;
        }

        // -----------------------------------------------------------------------
        // PresentationXml generation
        // -----------------------------------------------------------------------

        /// <summary>
        /// Wraps <paramref name="content"/> in the Dynamics 365 email template
        /// XML envelope used by both <c>presentationxml</c> and
        /// <c>subjectpresentationxml</c> fields.
        /// <para>
        /// Any <c>]]&gt;</c> sequences inside <paramref name="content"/> are escaped
        /// by splitting the CDATA section so the XML remains well-formed.
        /// </para>
        /// </summary>
        private static string WrapInPresentationXml(string content)
        {
            // Escape the CDATA end marker inside the content to keep the XML valid.
            var escaped = content.Replace("]]>", "]]]]><![CDATA[>");
            return $"<template><![CDATA[{escaped}]]></template>";
        }
    }
}
