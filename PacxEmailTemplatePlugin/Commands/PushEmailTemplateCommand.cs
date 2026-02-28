using System.ComponentModel.DataAnnotations;

namespace PacxEmailTemplatePlugin.Commands
{
    /// <summary>
    /// Command to push an email template from local files to Dynamics 365 / Dataverse.
    /// Mirrors the web-resource push pattern from Greg.Xrm.Command:
    /// when no explicit file paths are given the command scans the current working
    /// directory for <c>body.html</c> and <c>subject.html</c> / <c>subject.txt</c>.
    /// <para>
    /// Both <c>body</c> and <c>presentationxml</c> are derived from the single body
    /// file; likewise <c>subject</c> and <c>subjectpresentationxml</c> are both
    /// populated from the single subject file — no separate XML parameters needed.
    /// </para>
    /// </summary>
    public class PushEmailTemplateCommand
    {
        /// <summary>
        /// Gets or sets the title of the email template (used as the unique key for upsert).
        /// </summary>
        [Required]
        [Option("title", "t", "Title of the email template — used as the unique key for upsert")]
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the path to the HTML file used as the template body.
        /// When omitted the executor looks for <c>body.html</c> in the current directory.
        /// </summary>
        [Option("body", "b", "Path to the HTML body file (default: body.html in the current directory)")]
        public string? Body { get; set; }

        /// <summary>
        /// Gets or sets the path to the subject file.
        /// When omitted the executor looks for <c>subject.html</c> then <c>subject.txt</c>
        /// in the current directory.
        /// </summary>
        [Option("subject", "s", "Path to the subject file (default: subject.html → subject.txt in current directory)")]
        public string? Subject { get; set; }

        /// <summary>
        /// Gets or sets the description of the email template.
        /// </summary>
        [Option("description", "d", "Description of the email template")]
        public string? Description { get; set; }

        /// <summary>
        /// Gets or sets the template type code.
        /// Defaults to <c>email</c>.
        /// </summary>
        [Option("templatetypecode", "tt", "Template type code (default: email)")]
        public string TemplateTypeCode { get; set; } = "email";

        /// <summary>
        /// Gets or sets the LCID language code.
        /// Defaults to <c>1033</c> (English – United States).
        /// </summary>
        [Option("languagecode", "l", "LCID language code (default: 1033 = English)")]
        public int LanguageCode { get; set; } = 1033;

        /// <summary>
        /// Gets or sets whether the template is personal (scoped to the owning user).
        /// Defaults to <c>false</c> (organisation-wide template).
        /// </summary>
        [Option("ispersonal", "p", "Personal template scoped to owning user (default: false)")]
        public bool IsPersonal { get; set; } = false;

        /// <summary>
        /// Gets or sets the unique name of a Dataverse solution the template should
        /// be added to immediately after the push completes.
        /// </summary>
        [Option("solution", "sn", "Solution unique name — adds template to the solution after push")]
        public string? SolutionUniqueName { get; set; }
    }
}
