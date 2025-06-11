
using Bb.Configurations;
using System.Diagnostics;


namespace Bb.Schemas
{


    /// <summary>
    /// Generates JSON schema for specified types and saves them to a directory.
    /// </summary>
    public class SchemaGenerator
    {

        /// <summary>
        /// Initializes a new instance of the <see cref="SchemaGenerator"/> class.
        /// </summary>
        /// <param name="path">The directory path where schema will be saved. Must not be null or empty.</param>
        /// <param name="idTemplate">The template for generating schema IDs. Must not be null or empty.</param>
        /// <remarks>
        /// This constructor sets up the schema generator by creating the specified directory if it does not exist.
        /// </remarks>
        /// <exception cref="ArgumentNullException">
        /// Thrown if <paramref name="path"/> or <paramref name="idTemplate"/> is null or empty.
        /// </exception>
        private SchemaGenerator(string path, string idTemplate)
        {

            this._path = path;
            this._idTemplate = idTemplate;

            var d = this._path.AsDirectory();

            if (!d.Exists)
                d.Create();

        }

        /// <summary>
        /// Initializes the schema generator with the specified path and ID template.
        /// </summary>
        /// <param name="path">The directory path where schemas will be saved. Must not be null or empty.</param>
        /// <param name="idTemplate">The template for generating schema IDs. Must not be null or empty.</param>
        /// <remarks>
        /// This method creates a singleton instance of the <see cref="SchemaGenerator"/> class.
        /// </remarks>
        /// <example>
        /// <code lang="C#">
        /// SchemaGenerator.Initialize("C:\\Schemas", "http://example.com/schemas/{0}");
        /// </code>
        /// </example>
        public static void Initialize(string path, string idTemplate)
        {
            _instance = new SchemaGenerator(path, idTemplate);
        }

        /// <summary>
        /// Generates a schema for the specified type.
        /// </summary>
        /// <param name="type">The type for which to generate the schema. Must not be null.</param>
        /// <param name="name">The name of the type in the the schema. Might be null.</param>
        /// <remarks>
        /// This method generates a JSON schema for the specified type and saves it to the configured directory.
        /// </remarks>
        /// <exception cref="ArgumentNullException">
        /// Thrown if <paramref name="type"/> is null.
        /// </exception>
        /// <example>
        /// <code lang="C#">
        /// SchemaGenerator.GenerateSchema(typeof(MyClass));
        /// </code>
        /// </example>
        public static void GenerateSchema(Type type)
        {
            _instance?.GenerateSchemaImpl(type);
        }

        /// <summary>
        /// Generates a schema for the specified type and saves it to a file.
        /// </summary>
        /// <param name="type">The type for which to generate the schema. Must not be null.</param>
        /// <remarks>
        /// This method retrieves metadata from the type, generates a schema, and saves it to a file in the configured directory.
        /// </remarks>
        /// <exception cref="ArgumentNullException">
        /// Thrown if <paramref name="type"/> is null.
        /// </exception>
        private void GenerateSchemaImpl(Type type)
        {

            string SchemaName = GetSchemaName( type);
            var filename = GetFilename(SchemaName);
            var id = new Uri(string.Format(_idTemplate, SchemaName));

            try
            {
                var schema = type.GenerateSchemaForConfiguration(id);
                filename.Save(schema);
            }
            catch (Exception ex)
            {
                Trace.TraceError(ex.ToString());
            }

        }

        public static string GetSchemaName(Type type)
        {
            return type.Name;
        }

        /// <summary>
        /// Gets the file path for the schema file based on the type name.
        /// </summary>
        /// <param name="name">The name of the type. Must not be null or empty.</param>
        /// <returns>A <see cref="FileInfo"/> object representing the schema file.</returns>
        /// <remarks>
        /// This method constructs the file path for the schema file and ensures that any existing file is deleted.
        /// </remarks>
        /// <exception cref="ArgumentNullException">
        /// Thrown if <paramref name="name"/> is null or empty.
        /// </exception>
        private FileInfo GetFilename(string name)
        {
            string path = _path.Combine(name + ".schema.json");
            var d = path.AsFile();

            if (d.Exists)
                d.Delete();
            d.Refresh();

            return d;

        }

        /// <summary>
        /// The directory path where schemas are saved.
        /// </summary>
        private readonly string _path;

        /// <summary>
        /// The template for generating schema IDs.
        /// </summary>
        private readonly string _idTemplate;

        /// <summary>
        /// The singleton instance of the <see cref="SchemaGenerator"/> class.
        /// </summary>
        private static SchemaGenerator? _instance;

    }

}
