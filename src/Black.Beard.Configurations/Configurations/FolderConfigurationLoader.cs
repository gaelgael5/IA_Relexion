using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.FileProviders.Physical;
using System.Collections;

namespace Bb.Configurations
{


    /// <summary>
    /// Provides functionality to load and manage configuration files grouped by their names.
    /// </summary>
    /// <remarks>
    /// This class allows loading configuration files from specified directories, filtering them based on patterns and conditions, 
    /// and grouping them by their names for easy access.
    /// </remarks>
    public class FolderConfigurationLoader : IEnumerable<IGrouping<string, ConfigurationFile>>
    {


        static FolderConfigurationLoader()
        {
            _environmentName = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")
                ?? null;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="FolderConfigurationLoader"/> class.
        /// </summary>
        /// <param name="pattern">The file pattern to search for. Defaults to "*.json" if null or empty.</param>
        /// <param name="filter">A filter function to apply to files. Can be null.</param>
        /// <remarks>
        /// This constructor sets up the loader with a specified file pattern and optional filter for selecting files.
        /// </remarks>
        /// <example>
        /// <code lang="C#">
        /// var loader = new ConfigurationLoader("*.config", file => file.Length > 0);
        /// </code>
        /// </example>
        public FolderConfigurationLoader(string? pattern, Func<FileInfo, bool>? filter = null)
        {
            if (string.IsNullOrEmpty(pattern))
                pattern = $"*.json";

            _filter = filter;
            _pattern = pattern;

            _files = new Dictionary<string, ConfigurationFile>();
        }

        /// <summary>
        /// Retrieves a list of configuration files from the specified directory that match the given pattern and filter.
        /// </summary>
        /// <param name="filter">A filter function to apply to files. Can be null.</param>
        /// <param name="item">The directory to search for files. Must not be null.</param>
        /// <param name="pattern">The file pattern to search for. Must not be null or empty.</param>
        /// <returns>A list of <see cref="ConfigurationFile"/> objects that match the criteria.</returns>
        /// <remarks>
        /// This method scans the specified directory for files matching the given pattern and filter, and includes only files relevant to the current environment.
        /// </remarks>
        /// <exception cref="ArgumentNullException">
        /// Thrown if the directory or pattern is null.
        /// </exception>
        private static List<ConfigurationFile> GetFiles(Func<FileInfo, bool>? filter, DirectoryInfo item, string pattern)
        {
            List<ConfigurationFile> items = new List<ConfigurationFile>();
            item.Refresh();

            var files = item.GetFiles(pattern);
            foreach (var file in files)
                if (filter == null || filter(file))
                {
                    var a = new ConfigurationFile() { FileInfo = file, Name = ComputeName(file.Name), Environment = ComputeEnvironmentName(file.Name) };
                    if (a.Environment == _environmentName || a.Environment == null)
                        items.Add(a);
                }
            return items;
        }

        /// <summary>
        /// Returns an enumerator that iterates through the grouped configuration files.
        /// </summary>
        /// <returns>An enumerator of <see cref="IGrouping{TKey, TElement}"/> where TKey is a string and TElement is <see cref="ConfigurationFile"/>.</returns>
        /// <remarks>
        /// This method groups the configuration files by their names and provides an enumerator for iteration.
        /// </remarks>
        /// <example>
        /// <code lang="C#">
        /// var loader = new ConfigurationLoader("*.json");
        /// foreach (var group in loader)
        /// {
        ///     Console.WriteLine($"Group: {group.Key}");
        ///     foreach (var file in group)
        ///     {
        ///         Console.WriteLine($"File: {file.FileInfo.FullName}");
        ///     }
        /// }
        /// </code>
        /// </example>
        public IEnumerator<IGrouping<string, ConfigurationFile>> GetEnumerator()
        {
            var _i = _files.Values.GroupBy(c => c.Name).ToList();
            return _i.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            var _i = _files.Values.GroupBy(c => c.Name).ToList();
            return _i.GetEnumerator();
        }

        /// <summary>
        /// Adds folders to the configuration loader and loads matching files.
        /// </summary>
        /// <param name="paths">An array of directoryInfo paths to add. Can be null.</param>
        /// <returns>The updated <see cref="FolderConfigurationLoader"/> instance.</returns>
        /// <remarks>
        /// This method scans the specified folders for configuration files matching the loader's pattern and filter, and adds them to the internal collection.
        /// </remarks>
        /// <example>
        /// <code lang="C#">
        /// var loader = new ConfigurationLoader("*.json");
        /// loader.AddFolders("C:\\Configs", "D:\\MoreConfigs");
        /// </code>
        /// </example>
        public FolderConfigurationLoader AddFolders(params DirectoryInfo[] paths)
        {

            if (paths == null)
                return this;

            foreach (var contentRootPath in paths)
            {
                
                var f = GetFiles(_filter, contentRootPath, _pattern);

                foreach (var item in f)
                    if (!_files.ContainsKey(item.Name))
                        _files.Add(item.FileInfo.FullName, item);
            }

            return this;

        }

        public FolderConfigurationLoader AddFolders(IConfigurationBuilder config)
        {

            if (config == null)
                return this;

            var paths = config.GetFileProvider();

            foreach (var contentRootPath in paths.GetDirectoryContents(string.Empty))
            {

                if (contentRootPath is PhysicalFileInfo file)
                {

                    var item = contentRootPath.PhysicalPath.AsFile();
                    if (!_files.ContainsKey(item.Name))
                        _files.Add(item.FullName, new ConfigurationFile() { FileInfo = item, Name = ComputeName(file.Name), Environment = ComputeEnvironmentName(file.Name) } );
                
                }
                else if (contentRootPath is PhysicalDirectoryInfo dir)
                    AddFolders(dir.PhysicalPath);

            }

            return this;

        }

        /// <summary>
        /// Adds folders to the configuration loader and loads matching files.
        /// </summary>
        /// <param name="paths">An array of folder paths to add. Can be null.</param>
        /// <returns>The updated <see cref="FolderConfigurationLoader"/> instance.</returns>
        /// <remarks>
        /// This method scans the specified folders for configuration files matching the loader's pattern and filter, and adds them to the internal collection.
        /// </remarks>
        /// <example>
        /// <code lang="C#">
        /// var loader = new ConfigurationLoader("*.json");
        /// loader.AddFolders("C:\\Configs", "D:\\MoreConfigs");
        /// </code>
        /// </example>
        public FolderConfigurationLoader AddFolders(params string[] paths)
        {
            if (paths == null)
                return this;

            foreach (var item1 in paths)
            {
                var contentRootPath = new DirectoryInfo(item1);

                var f = GetFiles(_filter, contentRootPath, _pattern);

                foreach (var item in f)
                    if (!_files.ContainsKey(item.Name))
                        _files.Add(item.FileInfo.FullName, item);
            }

            return this;
        }



        /// <summary>
        /// Computes the name of a configuration file based on its filename.
        /// </summary>
        /// <param name="name">The filename to process. Must not be null or empty.</param>
        /// <returns>The computed name of the file.</returns>
        /// <remarks>
        /// This method extracts the base name of the file by splitting it on the '.' character.
        /// </remarks>
        private static string ComputeName(string name)
        {
            var n = name.Split('.');
            return n[0];
        }

        /// <summary>
        /// Computes the environment name from a configuration file's filename.
        /// </summary>
        /// <param name="name">The filename to process. Must not be null or empty.</param>
        /// <returns>The environment name if present; otherwise, <see langword="null"/>.</returns>
        /// <remarks>
        /// This method extracts the environment name from the second segment of the filename, if available.
        /// </remarks>
        private static string? ComputeEnvironmentName(string name)
        {
            var n = name.Split('.');
            if (n.Length > 2)
                return n[1];
            return null;
        }


        private static string? _environmentName;
        private readonly Dictionary<string, ConfigurationFile> _files;
        private readonly Func<FileInfo, bool>? _filter;
        private readonly string _pattern;
    }
}
