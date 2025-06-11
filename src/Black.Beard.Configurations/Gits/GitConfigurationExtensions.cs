// Ignore Spelling: Gits

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;

namespace Bb.Gits
{

    public static class GitConfigurationExtensions
    {

        public static T DownloadConfiguration<T>(this T self, GitConfiguration gitConfiguration)
            where T : IConfigurationBuilder
        {
            var provider = (PhysicalFileProvider)self.GetFileProvider();
            return self.DownloadConfiguration(gitConfiguration, provider.Root.AsDirectory());
        }

        public static T DownloadConfiguration<T>(this T self, GitConfiguration gitConfiguration, string targetFolder)
            where T : IConfigurationBuilder
        {
            return self.DownloadConfiguration(gitConfiguration, targetFolder.AsDirectory());
        }

        public static T DownloadConfiguration<T>(this T self, GitConfiguration gitConfiguration, DirectoryInfo targetFolder)
            where T : IConfigurationBuilder
        {

            var downloader = new GitConfigurationLoader();
            var result = downloader.Execute(gitConfiguration, targetFolder);
            if (!result)
                throw new InvalidOperationException("Git configuration download failed.");
            return self;
        }

    }

}

