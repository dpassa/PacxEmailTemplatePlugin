using System.ComponentModel.DataAnnotations;

namespace PacxEmailTemplatePlugin.Commands
{
    /// <summary>
    /// Command to deploy an email template from local HTML files to Dynamics 365.
    /// Mirrors the web-resource deployment pattern: read files from disk, push to Dataverse.
    /// </summary>
    public class DeployEmailTemplateCommand
    {
        /// <summary>
        /// Gets or sets the title of the email template (used as unique key).
        /// </summary>
        [Required]
        [Option("title", "t", "The title of the email template (used as unique key)")]
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the path to an HTML file whose content will become the template body.
        /// </summary>
        [Option("body-file", "bf", "Path to the HTML file to use as the email body")]
        public string? BodyFile { get; set; }

        /// <summary>
        /// Gets or sets the path to a file whose content will become the template subject.
        /// Takes precedence over --subject when both are provided.
        /// </summary>
        [Option("subject-file", "sf", "Path to the file containing the subject line")]
        public string? SubjectFile { get; set; }

        /// <summary>
        /// Gets or sets an inline subject string used when --subject-file is not provided.
        /// </summary>
        [Option("subject", "s", "Inline subject text (used when --subject-file is not provided)")]
        public string? Subject { get; set; }

        /// <summary>
        /// Gets or sets the description of the email template.
        /// </summary>
        [Option("description", "d", "The description of the email template")]
        public string? Description { get; set; }

        /// <summary>
        /// Gets or sets the template type code (default: email).
        /// </summary>
        [Option("templatetypecode", "tt", "The template type code (default: email)")]
        public string? TemplateTypeCode { get; set; }

        /// <summary>
        /// Gets or sets the language code (e.g., 1033 for English).
        /// </summary>
        [Option("languagecode", "l", "The language code (e.g., 1033 for English)")]
        public int? LanguageCode { get; set; }

        /// <summary>
        /// Gets or sets whether the template is personal (scoped to the owning user).
        /// </summary>
        [Option("ispersonal", "p", "Whether the template is personal (default: false)")]
        public bool? IsPersonal { get; set; }

        /// <summary>
        /// Gets or sets the unique name of a solution to add the template to after deployment.
        /// </summary>
        [Option("solution", "sn", "Unique solution name to add the template to after deployment")]
        public string? SolutionUniqueName { get; set; }
    }
}
