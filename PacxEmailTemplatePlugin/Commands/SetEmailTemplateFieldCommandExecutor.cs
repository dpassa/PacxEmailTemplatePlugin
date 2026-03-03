using Greg.Xrm.Command;
using Greg.Xrm.Command.Services.Output;
using Microsoft.PowerPlatform.Dataverse.Client;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System.Globalization;

namespace PacxEmailTemplatePlugin.Commands
{
    /// <summary>
    /// Executor for the SetEmailTemplateFieldCommand.
    /// Resolves the target template, converts the supplied string value to the
    /// correct Dataverse SDK type, and performs a minimal single-field update.
    /// </summary>
    public class SetEmailTemplateFieldCommandExecutor : ICommandExecutor<SetEmailTemplateFieldCommand>
    {
        private readonly IOutput _output;
        private readonly IOrganizationServiceAsync2 _organizationService;

        public SetEmailTemplateFieldCommandExecutor(
            IOutput output,
            IOrganizationServiceAsync2 organizationService)
        {
            _output = output ?? throw new ArgumentNullException(nameof(output));
            _organizationService = organizationService ?? throw new ArgumentNullException(nameof(organizationService));
        }

        public async Task<CommandResult> ExecuteAsync(SetEmailTemplateFieldCommand command, CancellationToken cancellationToken)
        {
            try
            {
                // --- Validate identity ---
                bool hasId = command.TemplateId.HasValue && command.TemplateId.Value != Guid.Empty;
                bool hasTitle = !string.IsNullOrWhiteSpace(command.Title);

                if (!hasId && !hasTitle)
                {
                    _output.WriteLine("Error: Provide either --template-id or --title to identify the template.", ConsoleColor.Red);
                    return CommandResult.Fail("Either --template-id or --title is required.");
                }

                if (string.IsNullOrWhiteSpace(command.Field))
                {
                    _output.WriteLine("Error: --field is required.", ConsoleColor.Red);
                    return CommandResult.Fail("--field is required.");
                }

                if (command.Type == FieldType.Lookup && string.IsNullOrWhiteSpace(command.LookupEntity))
                {
                    _output.WriteLine("Error: --lookup-entity is required when --type is Lookup.", ConsoleColor.Red);
                    return CommandResult.Fail("--lookup-entity is required when --type is Lookup.");
                }

                // --- Resolve template ---
                Entity template = hasId
                    ? await ResolveByIdAsync(command.TemplateId!.Value, cancellationToken)
                    : await ResolveByTitleAsync(command.Title!, cancellationToken);

                if (template is null)
                    return CommandResult.Fail("Template not found.");

                var templateTitle = template.GetAttributeValue<string>("title") ?? template.Id.ToString();
                _output.WriteLine($"Template resolved: {templateTitle} (ID: {template.Id})", ConsoleColor.Yellow);

                // --- Parse value with type safety ---
                object typedValue;
                try
                {
                    typedValue = ParseValue(command.Value, command.Type, command.LookupEntity);
                }
                catch (Exception ex) when (ex is FormatException or OverflowException or ArgumentException)
                {
                    _output.WriteLine($"Error: Cannot convert '{command.Value}' to {command.Type}: {ex.Message}", ConsoleColor.Red);
                    return CommandResult.Fail($"Type conversion failed for '{command.Value}' → {command.Type}: {ex.Message}");
                }

                _output.WriteLine(
                    $"Setting [{command.Field}] = {DescribeValue(typedValue, command.Type)} ({command.Type})",
                    ConsoleColor.Cyan);

                // --- Minimal update — only the target field ---
                var updateEntity = new Entity("template", template.Id);
                updateEntity[command.Field] = typedValue;

                await _organizationService.UpdateAsync(updateEntity, cancellationToken);

                _output.WriteLine($"Successfully set '{command.Field}' on template '{templateTitle}'.", ConsoleColor.Green);

                var result = CommandResult.Success();
                result["TemplateId"] = template.Id;
                result["TemplateTitle"] = templateTitle;
                result["Field"] = command.Field;
                result["Type"] = command.Type.ToString();
                result["Value"] = command.Value;
                return result;
            }
            catch (Exception ex)
            {
                _output.WriteLine($"Error setting field: {ex.Message}", ConsoleColor.Red);
                return CommandResult.Fail($"Error setting field: {ex.Message}", ex);
            }
        }

        // -----------------------------------------------------------------------
        // Resolution helpers
        // -----------------------------------------------------------------------

        private async Task<Entity> ResolveByIdAsync(Guid templateId, CancellationToken cancellationToken)
        {
            _output.WriteLine($"Resolving template by ID: {templateId}", ConsoleColor.DarkGray);

            var query = new QueryExpression("template")
            {
                ColumnSet = new ColumnSet("templateid", "title"),
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

        private async Task<Entity> ResolveByTitleAsync(string title, CancellationToken cancellationToken)
        {
            _output.WriteLine($"Resolving template by title: {title}", ConsoleColor.DarkGray);

            var query = new QueryExpression("template")
            {
                ColumnSet = new ColumnSet("templateid", "title"),
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
        // Type conversion
        // -----------------------------------------------------------------------

        /// <summary>
        /// Converts <paramref name="raw"/> to the appropriate Dataverse SDK type.
        /// Throws <see cref="FormatException"/> or <see cref="OverflowException"/> on bad input.
        /// </summary>
        private static object ParseValue(string raw, FieldType type, string? lookupEntity) => type switch
        {
            FieldType.String    => raw,
            FieldType.Integer   => int.Parse(raw, CultureInfo.InvariantCulture),
            FieldType.Boolean   => ParseBool(raw),
            FieldType.DateTime  => DateTime.Parse(raw, null, DateTimeStyles.RoundtripKind),
            FieldType.Decimal   => decimal.Parse(raw, CultureInfo.InvariantCulture),
            FieldType.Double    => double.Parse(raw, CultureInfo.InvariantCulture),
            FieldType.Money     => new Money(decimal.Parse(raw, CultureInfo.InvariantCulture)),
            FieldType.OptionSet => new OptionSetValue(int.Parse(raw, CultureInfo.InvariantCulture)),
            FieldType.Lookup    => new EntityReference(lookupEntity!, Guid.Parse(raw)),
            _                   => throw new ArgumentOutOfRangeException(nameof(type), $"Unsupported type: {type}")
        };

        /// <summary>
        /// Parses boolean-like strings: true/false, yes/no, 1/0.
        /// </summary>
        private static bool ParseBool(string raw) => raw.Trim().ToLowerInvariant() switch
        {
            "true"  or "yes" or "1" => true,
            "false" or "no"  or "0" => false,
            _ => throw new FormatException($"Cannot parse '{raw}' as boolean. Use true/false, yes/no, or 1/0.")
        };

        /// <summary>
        /// Returns a human-readable description of the typed value for console output.
        /// </summary>
        private static string DescribeValue(object value, FieldType type) => type switch
        {
            FieldType.Money     => $"${((Money)value).Value:N2}",
            FieldType.OptionSet => $"OptionSet({((OptionSetValue)value).Value})",
            FieldType.Lookup    => $"{((EntityReference)value).LogicalName}({((EntityReference)value).Id})",
            _                   => value.ToString() ?? "(null)"
        };
    }
}
