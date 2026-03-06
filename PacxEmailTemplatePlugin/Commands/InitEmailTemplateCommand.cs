using System.ComponentModel.DataAnnotations;

namespace PacxEmailTemplatePlugin.Commands
{
    /// <summary>
    /// Command to scaffold a local email template folder structure.
    /// Without <c>--remote</c> it creates a stub folder with an empty HTML file and a
    /// <c>definitions.json</c> template — <c>--name</c> is required in this mode.
    /// With <c>--remote</c> it performs a <strong>bulk pull</strong>: all templates found in
    /// Dynamics 365 are scaffolded locally. If <c>--name</c> is also supplied the pull is
    /// restricted to that single template title.
    /// </summary>
    public class InitEmailTemplateCommand
    {
        /// <summary>
        /// Gets or sets the template name, used as the folder name and as the
        /// <c>title</c> value written into <c>definitions.json</c>.
        /// <para>
        /// Required when <c>--remote</c> is <em>not</em> specified.
        /// When <c>--remote</c> is specified this value acts as an optional filter:
        /// if omitted, all D365 templates are pulled; if provided, only the template
        /// whose title matches is pulled.
        /// </para>
        /// </summary>
        [Option("name", "n", "Template name — folder name and D365 title filter (required without --remote)")]
        public string? Name { get; set; }

        /// <summary>
        /// Gets or sets the base directory in which the template folder(s) are created.
        /// Defaults to the current working directory.
        /// </summary>
        [Option("path", "p", "Base directory for the new template folder(s) (default: current directory)")]
        public string? Path { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to pull from Dynamics 365.
        /// When <see langword="true"/> a bulk pull is performed: every template found in D365
        /// (optionally filtered by <see cref="Name"/>) is scaffolded as a local folder.
        /// </summary>
        [Option("remote", "r", "Pull from D365 — scaffolds all unmanaged templates (optionally filtered by --name)")]
        public bool Remote { get; set; } = false;
    }
}
