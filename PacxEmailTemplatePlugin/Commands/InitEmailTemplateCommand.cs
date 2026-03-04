using System.ComponentModel.DataAnnotations;

namespace PacxEmailTemplatePlugin.Commands
{
    /// <summary>
    /// Command to scaffold a local email template folder structure.
    /// Without <c>--remote</c> it creates a stub folder with an empty HTML file and a
    /// <c>definitions.json</c> template. With <c>--remote</c> it fetches an existing
    /// Dynamics 365 template and populates the local files from the record.
    /// </summary>
    public class InitEmailTemplateCommand
    {
        /// <summary>
        /// Gets or sets the template name, used as the folder name and as the
        /// <c>title</c> value written into <c>definitions.json</c>.
        /// When <c>--remote</c> is specified this value is also the D365 record title
        /// used to locate the existing template.
        /// </summary>
        [Required]
        [Option("name", "n", "Template name — used as folder name and D365 title in definitions.json")]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the base directory in which the template folder is created.
        /// Defaults to the current working directory.
        /// </summary>
        [Option("path", "p", "Base directory for the new template folder (default: current directory)")]
        public string? Path { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to populate the local files from an
        /// existing Dynamics 365 template record instead of creating stubs.
        /// </summary>
        [Option("remote", "r", "Fetch from D365 and populate local files from the existing record")]
        public bool Remote { get; set; } = false;
    }
}
