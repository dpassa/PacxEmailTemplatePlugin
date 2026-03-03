using System.ComponentModel.DataAnnotations;

namespace PacxEmailTemplatePlugin.Commands
{
    /// <summary>
    /// Command to export an email template's body and/or subject from Dataverse to local files.
    /// Provides the reverse direction of <see cref="DeployEmailTemplateCommand"/>:
    /// pull from Dataverse → write HTML files to disk for local editing or source control.
    /// </summary>
    public class ExportEmailTemplateCommand
    {
        /// <summary>
        /// Gets or sets the GUID of the email template to export.
        /// Provide either this or --title.
        /// </summary>
        [Option("template-id", "i", "GUID of the email template (alternative to --title)")]
        public Guid? TemplateId { get; set; }

        /// <summary>
        /// Gets or sets the title of the email template to export.
        /// Provide either this or --template-id.
        /// </summary>
        [Option("title", "t", "Title of the email template (alternative to --template-id)")]
        public string? Title { get; set; }

        /// <summary>
        /// Gets or sets the directory where the exported files will be written.
        /// The directory will be created if it does not exist.
        /// </summary>
        [Required]
        [Option("output-dir", "o", "Directory to write the exported files to (created if missing)")]
        public string OutputDir { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets whether to export the template body.
        /// Defaults to <see langword="true"/>.
        /// </summary>
        [Option("include-body", "ib", "Export the body to {title}-body.html (default: true)")]
        public bool IncludeBody { get; set; } = true;

        /// <summary>
        /// Gets or sets whether to export the template subject.
        /// Defaults to <see langword="true"/>.
        /// </summary>
        [Option("include-subject", "is", "Export the subject to {title}-subject.txt (default: true)")]
        public bool IncludeSubject { get; set; } = true;
    }
}
