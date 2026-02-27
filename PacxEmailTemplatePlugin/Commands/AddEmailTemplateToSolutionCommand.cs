using System.ComponentModel.DataAnnotations;

namespace PacxEmailTemplatePlugin.Commands
{
    /// <summary>
    /// Command to add an email template to a Dynamics 365 solution.
    /// </summary>
    public class AddEmailTemplateToSolutionCommand
    {
        /// <summary>
        /// Gets or sets the ID of the email template to add to the solution.
        /// </summary>
        [Required]
        [Option("templateid", "tid", "The ID of the email template to add to the solution")]
        public Guid TemplateId { get; set; }

        /// <summary>
        /// Gets or sets the unique name of the target solution.
        /// </summary>
        [Required]
        [Option("solution", "s", "The unique name of the target solution")]
        public string SolutionUniqueName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets whether to add required components.
        /// </summary>
        [Option("addrequiredcomponents", "arc", "Whether to add required components")]
        public bool AddRequiredComponents { get; set; } = false;

        /// <summary>
        /// Gets or sets whether to include subcomponents.
        /// </summary>
        [Option("donotincludesubcomponents", "dns", "Whether to exclude subcomponents")]
        public bool DoNotIncludeSubcomponents { get; set; } = false;
    }
}
