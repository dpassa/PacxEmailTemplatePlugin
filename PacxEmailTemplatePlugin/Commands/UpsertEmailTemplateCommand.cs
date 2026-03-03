using System.ComponentModel.DataAnnotations;

namespace PacxEmailTemplatePlugin.Commands
{
    /// <summary>
    /// Command to upsert (create or update) an email template in Dynamics 365.
    /// </summary>
    public class UpsertEmailTemplateCommand
    {
        /// <summary>
        /// Gets or sets the title of the email template (used as unique key).
        /// </summary>
        [Required]
        [Option("title", "t", "The title of the email template (used as unique key)")]
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the subject of the email template.
        /// </summary>
        [Option("subject", "s", "The subject of the email template")]
        public string? Subject { get; set; }

        /// <summary>
        /// Gets or sets the body of the email template.
        /// </summary>
        [Option("body", "b", "The body of the email template")]
        public string? Body { get; set; }

        /// <summary>
        /// Gets or sets the description of the email template.
        /// </summary>
        [Option("description", "d", "The description of the email template")]
        public string? Description { get; set; }

        /// <summary>
        /// Gets or sets the template type code.
        /// </summary>
        [Option("templatetypecode", "tt", "The template type code (default: email)")]
        public string? TemplateTypeCode { get; set; }

        /// <summary>
        /// Gets or sets the MIME type.
        /// </summary>
        [Option("mimetype", "m", "The MIME type")]
        public string? MimeType { get; set; }

        /// <summary>
        /// Gets or sets the subject presentation XML.
        /// </summary>
        [Option("subjectpresentationxml", "spx", "The subject presentation XML")]
        public string? SubjectPresentationXml { get; set; }

        /// <summary>
        /// Gets or sets the body presentation XML.
        /// </summary>
        [Option("presentationxml", "px", "The body presentation XML")]
        public string? PresentationXml { get; set; }

        /// <summary>
        /// Gets or sets the language code.
        /// </summary>
        [Option("languagecode", "l", "The language code")]
        public int? LanguageCode { get; set; }

        /// <summary>
        /// Gets or sets whether the template is personal.
        /// </summary>
        [Option("ispersonal", "p", "Whether the template is personal")]
        public bool? IsPersonal { get; set; }
    }
}
