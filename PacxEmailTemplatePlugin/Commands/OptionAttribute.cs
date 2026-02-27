namespace PacxEmailTemplatePlugin.Commands
{
    /// <summary>
    /// Attribute to mark command options for PACX commands.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public class OptionAttribute : Attribute
    {
        /// <summary>
        /// Gets the name of the option.
        /// </summary>
        public string Name { get; }
        
        /// <summary>
        /// Gets the short name (alias) of the option.
        /// </summary>
        public string? ShortName { get; }
        
        /// <summary>
        /// Gets the description of the option.
        /// </summary>
        public string? Description { get; }

        /// <summary>
        /// Initializes a new instance of the OptionAttribute class.
        /// </summary>
        /// <param name="name">The name of the option.</param>
        /// <param name="shortName">The short name (alias) of the option.</param>
        /// <param name="description">The description of the option.</param>
        public OptionAttribute(string name, string? shortName = null, string? description = null)
        {
            Name = name;
            ShortName = shortName;
            Description = description;
        }
    }
}
