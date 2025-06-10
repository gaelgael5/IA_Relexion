using Microsoft.Extensions.Configuration;


namespace Bb.Schemas
{
    public static class SchemaGeneratorExtension
    {

        /// <summary>
        /// Initializes the schema generator with the specified path and ID template.
        /// </summary>
        /// <param name="path">The directory path where schemas will be saved. Must not be null or empty.</param>
        /// <param name="result">result configuration object.</param>
        /// <remarks>
        /// This method initializes the schema generator, creating a singleton instance.
        /// </remarks>
        public static IConfigurationRoot ResolveConfiguration<T>(this IConfigurationRoot configuration, out T result)
        {

            if (configuration == null)
                throw new ArgumentNullException(nameof(configuration), "Configuration cannot be null.");

            var section = SchemaGenerator.GetSchemaName(typeof(T));
            result = configuration.GetSection(section).Get<T>();
            SchemaGenerator.GenerateSchema(typeof(T));
            return configuration;
        }

       
    }

}
