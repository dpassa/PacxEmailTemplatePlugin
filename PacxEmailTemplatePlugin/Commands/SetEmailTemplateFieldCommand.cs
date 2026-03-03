using System.ComponentModel.DataAnnotations;

namespace PacxEmailTemplatePlugin.Commands
{
    /// <summary>
    /// Dataverse attribute types supported by the set-field command.
    /// </summary>
    public enum FieldType
    {
        /// <summary>Plain text / memo field.</summary>
        String,
        /// <summary>Whole number (Int32).</summary>
        Integer,
        /// <summary>Two-option / boolean field. Accepts true/false, yes/no, 1/0.</summary>
        Boolean,
        /// <summary>Date and time field. Accepts ISO 8601 strings.</summary>
        DateTime,
        /// <summary>Decimal number field.</summary>
        Decimal,
        /// <summary>Floating-point number field.</summary>
        Double,
        /// <summary>Currency field. Value is the decimal amount.</summary>
        Money,
        /// <summary>Choice / option-set field. Value is the integer option code.</summary>
        OptionSet,
        /// <summary>Lookup field. Value is the target record GUID; use --lookup-entity for the entity name.</summary>
        Lookup
    }

    /// <summary>
    /// Command to set a field value on an email template with data-type safety.
    /// Supports all common Dataverse attribute types and performs typed conversion
    /// before sending values to the API, preventing runtime type errors.
    /// </summary>
    public class SetEmailTemplateFieldCommand
    {
        /// <summary>
        /// Gets or sets the GUID of the target email template.
        /// Provide either this or --title.
        /// </summary>
        [Option("template-id", "i", "GUID of the email template (alternative to --title)")]
        public Guid? TemplateId { get; set; }

        /// <summary>
        /// Gets or sets the title of the target email template.
        /// Provide either this or --template-id.
        /// </summary>
        [Option("title", "t", "Title of the email template (alternative to --template-id)")]
        public string? Title { get; set; }

        /// <summary>
        /// Gets or sets the logical name of the attribute to set.
        /// </summary>
        [Required]
        [Option("field", "f", "Logical name of the attribute to set (e.g. new_priority)")]
        public string Field { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the value as a string. It is converted to the correct .NET type
        /// according to --type before being written to Dataverse.
        /// </summary>
        [Required]
        [Option("value", "v", "Value to assign (string form, converted per --type)")]
        public string Value { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the Dataverse attribute type used for value conversion.
        /// Default is String.
        /// </summary>
        [Required]
        [Option("type", "tp", "Attribute type: string | integer | boolean | datetime | decimal | double | money | optionset | lookup")]
        public FieldType Type { get; set; } = FieldType.String;

        /// <summary>
        /// Gets or sets the entity logical name for lookup fields.
        /// Required when --type is Lookup.
        /// </summary>
        [Option("lookup-entity", "le", "Target entity logical name for lookup fields (required with --type lookup)")]
        public string? LookupEntity { get; set; }
    }
}
