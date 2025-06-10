// Ignore Spelling: Gits

using LibGit2Sharp;
using LibGit2Sharp.Handlers;
using System.Diagnostics;
using System.Globalization;

namespace Bb.Gits
{


    public class GitConfigurationDownloader
    {

        #region ctor

        /// <summary>
        /// Initializes a new instance of the <see cref="GitConfigurationDownloader"/> class.
        /// </summary>
        public GitConfigurationDownloader()
        {

        }

        /// <summary>
        /// Initializes a new instance of the <see cref="GitConfigurationDownloader"/> class.
        /// </summary>
        /// <param name="configuration"></param>
        public GitConfigurationDownloader(GitConfiguration configuration)
        {
            GitConfiguration = configuration;
        }

        #endregion ctor


        /// <summary>
        /// Gets the name of the local branch for the specified repository folder.
        /// </summary>
        /// <param name="localFolder">The path to the local repository folder. Must be a valid Git repository path.</param>
        /// <returns>The name of the local branch being tracked, or <see langword="null"/> if no branch is found.</returns>
        /// <remarks>
        /// This method retrieves the name of the local branch that is being tracked by the repository.
        /// </remarks>
        /// <exception cref="LibGit2Sharp.RepositoryNotFoundException">
        /// Thrown if the specified folder is not a valid Git repository.
        /// </exception>
        /// <exception cref="System.ArgumentException">
        /// Thrown if the provided folder path is invalid.
        /// </exception>
        /// <example>
        /// <code lang="C#">
        /// var loader = new ConfigurationLoader();
        /// string branchName = loader.GetLocalBranchName(@"C:\MyRepo");
        /// Console.WriteLine($"Local branch: {branchName}");
        /// </code>
        /// </example>
        public string? GetLocalBranchName(string localFolder)
        {
            
            if (GitConfiguration.Initialized(localFolder) != GitStatus.Initialized)
                return null;

            using var repo = new Repository(localFolder);

            // var l = repo.Commits.Last();
            var b = repo.Branches.FirstOrDefault(c => (!c.IsRemote && c.IsTracking));

            if (b != null)
            {
                var t1 = "origin/";
                var t = b.TrackedBranch.FriendlyName;

                if (t.StartsWith(t1))
                    t = t[t1.Length..];

                return t;

            }

            return null;

        }

        /// <summary>
        /// Displays the status of the latest commit in the specified repository folder.
        /// </summary>
        /// <param name="localFolder">The path to the local repository folder. Must be a valid Git repository path.</param>
        /// <remarks>
        /// This method prints details about the latest commit, including the commit ID, author, date, and message.
        /// </remarks>
        /// <exception cref="LibGit2Sharp.RepositoryNotFoundException">
        /// Thrown if the specified folder is not a valid Git repository.
        /// </exception>
        /// <exception cref="System.ArgumentException">
        /// Thrown if the provided folder path is invalid.
        /// </exception>
        /// <example>
        /// <code lang="C#">
        /// var loader = new ConfigurationLoader();
        /// loader.GetStatus(@"C:\MyRepo");
        /// </code>
        /// </example>
        public static void GetStatus(string localFolder)
        {

            using var repo = new Repository(localFolder);

            foreach (Commit c in repo.Commits.Take(1))
            {

                Console.WriteLine(string.Format("commit {0}", c.Id));

                if (c.Parents.Count() > 1)
                {
                    Console.WriteLine("Merge: {0}",
                        string.Join(" ", [.. c.Parents.Select(p => p.Id.Sha[..7])]));
                }

                Console.WriteLine(string.Format("Author: {0} <{1}>", c.Author.Name, c.Author.Email));
                Console.WriteLine("Date:   {0}", c.Author.When.ToString(RFC2822Format, CultureInfo.InvariantCulture));
                Console.WriteLine();
                Console.WriteLine(c.Message);
                Console.WriteLine();
            }

        }


        public GitConfiguration GitConfiguration { get; set; }

        /// <summary>
        /// Refreshes the Git repository in the specified folder by cloning or pulling changes.
        /// </summary>
        /// <param name="localFolder">The path to the local repository folder. Must be a valid directory path.</param>
        /// <param name="branch">The branch to clone or pull. If <see langword="null"/>, the default branch is used.</param>
        /// <returns><see langword="true"/> if the operation succeeds; otherwise, <see langword="false"/>.</returns>
        /// <remarks>
        /// This method ensures that the repository is up-to-date by either cloning it if it is not initialized or pulling changes if it is already initialized.
        /// </remarks>
        /// <exception cref="System.IO.IOException">
        /// Thrown if there is an issue accessing the folder.
        /// </exception>
        /// <example>
        /// <code lang="C#">
        /// var loader = new ConfigurationLoader();
        /// bool success = loader.Refresh(@"C:\MyRepo", "main");
        /// Console.WriteLine($"Refresh successful: {success}");
        /// </code>
        /// </example>
        public bool Refresh(string localFolder, string? branch = null)
        {
            var folder = localFolder.AsDirectory();
            return Refresh(folder, branch);
        }

        /// <summary>
        /// Refreshes the Git repository in the specified folder by cloning or pulling changes.
        /// </summary>
        /// <param name="folder">The target folder as a <see cref="DirectoryInfo"/> object. Must be a valid directory.</param>
        /// <param name="branch">The branch to clone or pull. If <see langword="null"/>, the default branch is used.</param>
        /// <returns><see langword="true"/> if the operation succeeds; otherwise, <see langword="false"/>.</returns>
        /// <remarks>
        /// This method ensures that the repository is up-to-date by either cloning it if it is not initialized or pulling changes if it is already initialized.
        /// </remarks>
        /// <exception cref="System.IO.IOException">
        /// Thrown if there is an issue accessing the folder.
        /// </exception>
        /// <example>
        /// <code lang="C#">
        /// var loader = new ConfigurationLoader();
        /// var folder = new DirectoryInfo(@"C:\MyRepo");
        /// bool success = loader.Refresh(folder, "main");
        /// Console.WriteLine($"Refresh successful: {success}");
        /// </code>
        /// </example>
        public bool Refresh(DirectoryInfo folder, string? branch = null)
        {

            if (!folder.Exists)
                folder.CreateFolderIfNotExists();

            var f = folder.FullName;

            var status = GitConfiguration.Initialized(folder.FullName);
            switch (status)
            {

                case GitStatus.NotInitialized:
                    return Clone(f, branch ?? GitConfiguration.GitBranch ?? "main");

                case GitStatus.Initialized:
                    return Pull(f);

                case GitStatus.FolderNotEmpty:
                case GitStatus.FolderNotCreated:
                default:
                    break;
            }

            return false;

        }

        #region private
        

        private bool Pull(string localFolder)
        {
            try
            {
                using var repo = new Repository(localFolder);
                var pullOptions = GetPullOptions();
                var identity = new Identity(GitConfiguration.GitUserName, GitConfiguration.GitEmail);
                var signature = new Signature(identity, DateTimeOffset.Now);
                Commands.Pull(repo, signature, pullOptions);

                return true;

            }
            catch (Exception ex)
            {
                Trace.TraceError($"Failed to pull : {ex.Message}");
            }

            return false;

        }

        private bool Clone(string localFolder, string branch)
        {
            try
            {
                var cloneOptions = GetCloneOptions(branch);
                Repository.Clone(GitConfiguration.GitRemoteUrl, localFolder, cloneOptions);
                return true;
            }
            catch (Exception ex)
            {
                Trace.TraceError($"Une erreur est survenue : {ex.Message}");
            }

            return false;

        }

        private FetchOptions GetFetchOptions()
        {
            FetchOptions options = new()
            {
                CredentialsProvider = GetCredential()
            };
            return options;
        }

        private CloneOptions GetCloneOptions(string branch = "main")
        {

            var options = new CloneOptions()
            {
                BranchName = branch,
                //Checkout = true,
                RecurseSubmodules = true,
                IsBare = false,
            };
            options.FetchOptions.CredentialsProvider = GetCredential();
            return options;

        }

        private PullOptions GetPullOptions()
        {
            var options = new PullOptions()
            {
                FetchOptions = GetFetchOptions()
            };
            return options;
        }

        private CredentialsHandler GetCredential()
        {

            if (!GitConfiguration.HasPassword)
                return (_url, _user, _cred) => new LibGit2Sharp.DefaultCredentials();

            return (_url, _user, _cred) =>
            {
                return new UsernamePasswordCredentials()
                {
                    Username = GitConfiguration.GitUserName,
                    Password = GitConfiguration.GitPassword
                };
            };
        }

        #endregion private


        private const string RFC2822Format = "ddd dd MMM HH:mm:ss yyyy K";

    }
}
