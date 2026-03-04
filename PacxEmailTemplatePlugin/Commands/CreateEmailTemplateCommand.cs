namespace PacxEmailTemplatePlugin.Commands
{
    /// <summary>
    /// Command to create a new email template in Dynamics 365 from a local template folder.
    /// Reads <c>definitions.json</c> and the optional HTML body file, then creates the
    /// record. Fails explicitly if a template with the same title already exists — use
    /// <c>pacx emailtemplate push</c> to update an existing template.
    /// </summary>
    public class CreateEmailTemplateCommand
    {
        /// <summary>
        /// Gets or sets the path to the template folder containing <c>definitions.json</c>
        /// and the optional HTML body file. Defaults to the current working directory.
        /// </summary>
        [Option("path", "p", "Path to the template folder (default: current directory)")]
        public string? Path { get; set; }

        /// <summary>
        /// Gets or sets the unique name of a Dataverse solution the template should be
        /// added to immediately after creation.
        /// </summary>
        [Option("solution", "s", "Solution unique name — adds template to the solution after creation")]
        public string? SolutionUniqueName { get; set; }
    }
}
