namespace Bb.Configurations
{
    /// <summary>
    /// Represents a configuration file with its associated properties.
    /// </summary>
    /// <remarks>
    /// This structure holds information about a configuration file, including its name, file information, and associated environment.
    /// </remarks>
    public struct ConfigurationFile
    {
        /// <summary>
        /// Gets or sets the name of the configuration file.
        /// </summary>
        /// <remarks>
        /// The name is computed based on the file's base name.
        /// </remarks>
        public string Name;

        /// <summary>
        /// Gets or sets the file information of the configuration file.
        /// </summary>
        /// <remarks>
        /// This property provides access to the file's metadata and path.
        /// </remarks>
        public FileInfo FileInfo;

        /// <summary>
        /// Gets or sets the environment associated with the configuration file.
        /// </summary>
        /// <remarks>
        /// The environment is extracted from the file's name, if available.
        /// </remarks>
        public string? Environment;
    }
}
