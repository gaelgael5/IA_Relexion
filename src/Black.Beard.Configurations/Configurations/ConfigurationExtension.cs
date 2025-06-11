using System.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;

namespace Bb.Configurations
{

    /// <summary>
    /// Extension methods for configuring types in a web application.
    /// </summary>
    public static class ConfigurationExtension
    {

        #region Load configuration from files

        /// <summary>
        /// Add configuration files to the configuration builder.
        /// </summary>
        /// <param name="self">configuration builder</param>
        /// <param name="pattern">pattern globing to select files</param>
        /// <param name="filter">filter file</param>
        /// <returns></returns>
        public static T LoadConfiguration<T>(this T self, 
            string? pattern,
            Func<FileInfo, bool>? filter = null)
            where T : IConfigurationBuilder
        {         

            if (filter == null)
                filter = _ => _.Length > 0;

            FolderConfigurationLoader worker = new FolderConfigurationLoader(pattern, filter).AddFolders(self);
            LoadConfiguration(self, worker);

            return self;
            
        }        

        /// <summary>
        /// Add configuration files to the configuration builder.
        /// </summary>
        /// <param name="self">configuration builder</param>
        /// <param name="path">Path to add in the search</param>
        /// <param name="pattern">pattern globing to select files</param>
        /// <param name="filter">filter file</param>
        /// <returns></returns>
        public static T LoadConfiguration<T>(this T self, DirectoryInfo path,
            string? pattern,
            Func<FileInfo, bool>? filter = null)
            where T : IConfigurationBuilder
        {

            if (filter == null)
                filter = _ => _.Length > 0;

            FolderConfigurationLoader worker = new FolderConfigurationLoader(pattern, filter).AddFolders(self);
            LoadConfiguration(self, worker);

            return self;

        }       

        public static T LoadConfiguration<T>(this T self, FolderConfigurationLoader files)
            where T : IConfigurationBuilder
        {

            foreach (var file in files)
            {

                var c = file.Count();
                FileInfo? f = null;
                if (c == 1)
                    f = file.FirstOrDefault().FileInfo;

                else if (c == 2) // one more it is because we have a file for environment and a file for all environment
                {

                    var f1 = file.FirstOrDefault(c => string.IsNullOrEmpty(c.Environment));
                    if (f1.FileInfo != null)
                        Load(self, f1.FileInfo);

                    var f2 = file.FirstOrDefault(c => !string.IsNullOrEmpty(c.Environment));
                    if (f2.FileInfo != null)
                        Load(self, f2.FileInfo);

                }

                if (f != null)
                    Load(self, f);

            }

            return self;

        }

        private static void Load(IConfigurationBuilder config, FileInfo f)
        {

            var type = FileContentTypeDetector.DetectFileType(f);
            switch (type)
            {

                case "JSON":
                    config.AddJsonFile(f.FullName, optional: false, reloadOnChange: false);
                    Trace.TraceInformation($"configuration file {f.FullName} is loaded.");
                    break;

                case "XML":
                    config.AddXmlFile(f.FullName, optional: false, reloadOnChange: false);
                    Trace.TraceInformation($"configuration file {f.FullName} is loaded.");
                    break;

                case "INI":
                    config.AddIniFile(f.FullName, optional: false, reloadOnChange: false);
                    Trace.TraceInformation($"configuration file {f.FullName} is loaded.");
                    break;

                case "PerKey":
                    config.AddKeyPerFile(f.FullName, optional: false, reloadOnChange: false);
                    Trace.TraceInformation($"configuration file {f.FullName} is loaded.");
                    break;

                default:
                    Trace.TraceWarning($"configuration file {f.FullName} is is not recognized.");
                    break;

            }
        }

        #endregion Load configuration from files


    }

}
