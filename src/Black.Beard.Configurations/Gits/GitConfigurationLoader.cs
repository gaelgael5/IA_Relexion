using System.Diagnostics;
using System.Net.NetworkInformation;
using System.Text;

namespace Bb.Gits
{


    /// <summary>
    /// Download configuration form git repository and load the configuration
    /// </summary>
    /// <example>
    /// <code lang="C#">
    /// 
    /// </code> 
    /// </example>
    public class GitConfigurationLoader
    {


        /// <summary>
        /// Executes the configuration loader to download and load configurations from a Git repository.
        /// </summary>
        /// <param name="gitConfiguration">The git configuration.</param>
        /// <param name="dir">The target directory.</param>
        /// <returns><see langword="null"/> after executing the configuration loader.</returns>
        /// <remarks>
        /// This method downloads configuration files from a Git repository based on the provided configuration path and loads them into the application context.
        /// </remarks>
        /// <example>
        /// <code lang="C#">
        /// var builder = WebApplication.CreateBuilder(args);
        /// var loader = new GitConfigurationLoader();
        /// loader.Execute(builder.Configuration, new DirectoryInfo("c:/configuration"));
        /// var app = builder.Build();
        /// app.Run();
        /// </code>
        /// </example>
        public bool Execute(GitConfiguration gitConfiguration, DirectoryInfo dir)
        {

            if (gitConfiguration == null)
            {
                Trace.TraceError("no configuration is downloaded because the git configuration is null");
                return false;
            }

            if (!gitConfiguration.IsValid())
            {
                Trace.TraceError("no configuration is downloaded because the git configuration is null");
                return false;
            }


            if (!InternetConnectivityChecker.IsConnected)
            {
                Trace.TraceError("no configuration is downloaded because the git configuration is null");
                return false;
            }


            return ExecuteGit(gitConfiguration, dir);

        }

        private bool ExecuteGit(GitConfiguration git, DirectoryInfo folder)
        {

            var loader = new GitConfigurationDownloader(git);

            folder.Refresh();
            if (folder.Exists)
            {
                try
                {
                    var branchName = loader.GetLocalBranchName(folder.FullName);
                    if (git.GitBranch != branchName)
                    {
                        folder.DeleteFolderIfExists();
                        folder.Refresh();
                    }
                }
                catch (LibGit2Sharp.RepositoryNotFoundException)
                {

                }
            }

            return loader.Refresh(folder);
        
        }

        public static class InternetConnectivityChecker
        {
            private static Func<Task<bool>>? InternetMethodMock;

            private static readonly HttpClient httpClient = new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(1.0)
            };

            public static bool IsConnected => IsConnectedToInternet().Result;

            //
            // Résumé :
            //     return true if connected to Internet
            public static async Task<bool> IsConnectedToInternet()
            {
                if (InternetMethodMock != null)
                {
                    return await InternetMethodMock();
                }

                if (!IsNetworkInterfaceConnected())
                {
                    return false;
                }

                return await _isConnectedToInternet();
            }

            //
            // Résumé :
            //     Return true if network interface is connected
            public static bool IsNetworkInterfaceConnected()
            {
                bool result = false;
                NetworkInterface.GetAllNetworkInterfaces();
                NetworkInterface[] allNetworkInterfaces = NetworkInterface.GetAllNetworkInterfaces();
                foreach (NetworkInterface networkInterface in allNetworkInterfaces)
                {
                    if (networkInterface.OperationalStatus == OperationalStatus.Up && networkInterface.NetworkInterfaceType != NetworkInterfaceType.Loopback && networkInterface.NetworkInterfaceType != NetworkInterfaceType.Tunnel)
                    {
                        StringBuilder stringBuilder = new StringBuilder();
                        if ((networkInterface.NetworkInterfaceType & NetworkInterfaceType.Tunnel) == NetworkInterfaceType.Tunnel)
                        {
                            stringBuilder.Append(NetworkInterfaceType.Tunnel.ToString() + " ");
                        }

                        if ((networkInterface.NetworkInterfaceType & NetworkInterfaceType.Ethernet) == NetworkInterfaceType.Ethernet)
                        {
                            stringBuilder.Append(NetworkInterfaceType.Ethernet.ToString() + " ");
                        }

                        if ((networkInterface.NetworkInterfaceType & NetworkInterfaceType.TokenRing) == NetworkInterfaceType.TokenRing)
                        {
                            stringBuilder.Append(NetworkInterfaceType.TokenRing.ToString() + " ");
                        }

                        if ((networkInterface.NetworkInterfaceType & NetworkInterfaceType.Fddi) == NetworkInterfaceType.Fddi)
                        {
                            stringBuilder.Append(NetworkInterfaceType.Fddi.ToString() + " ");
                        }

                        if ((networkInterface.NetworkInterfaceType & NetworkInterfaceType.BasicIsdn) == NetworkInterfaceType.BasicIsdn)
                        {
                            stringBuilder.Append(NetworkInterfaceType.BasicIsdn.ToString() + " ");
                        }

                        if ((networkInterface.NetworkInterfaceType & NetworkInterfaceType.PrimaryIsdn) == NetworkInterfaceType.PrimaryIsdn)
                        {
                            stringBuilder.Append(NetworkInterfaceType.PrimaryIsdn.ToString() + " ");
                        }

                        if ((networkInterface.NetworkInterfaceType & NetworkInterfaceType.Ppp) == NetworkInterfaceType.Ppp)
                        {
                            stringBuilder.Append(NetworkInterfaceType.Ppp.ToString() + " ");
                        }

                        if ((networkInterface.NetworkInterfaceType & NetworkInterfaceType.Loopback) == NetworkInterfaceType.Loopback)
                        {
                            stringBuilder.Append(NetworkInterfaceType.Loopback.ToString() + " ");
                        }

                        if ((networkInterface.NetworkInterfaceType & NetworkInterfaceType.Ethernet3Megabit) == NetworkInterfaceType.Ethernet3Megabit)
                        {
                            stringBuilder.Append(NetworkInterfaceType.Ethernet3Megabit.ToString() + " ");
                        }

                        if ((networkInterface.NetworkInterfaceType & NetworkInterfaceType.Slip) == NetworkInterfaceType.Slip)
                        {
                            stringBuilder.Append(NetworkInterfaceType.Slip.ToString() + " ");
                        }

                        if ((networkInterface.NetworkInterfaceType & NetworkInterfaceType.Atm) == NetworkInterfaceType.Atm)
                        {
                            stringBuilder.Append(NetworkInterfaceType.Atm.ToString() + " ");
                        }

                        if ((networkInterface.NetworkInterfaceType & NetworkInterfaceType.GenericModem) == NetworkInterfaceType.GenericModem)
                        {
                            stringBuilder.Append(NetworkInterfaceType.GenericModem.ToString() + " ");
                        }

                        if ((networkInterface.NetworkInterfaceType & NetworkInterfaceType.FastEthernetT) == NetworkInterfaceType.FastEthernetT)
                        {
                            stringBuilder.Append(NetworkInterfaceType.FastEthernetT.ToString() + " ");
                        }

                        if ((networkInterface.NetworkInterfaceType & NetworkInterfaceType.Isdn) == NetworkInterfaceType.Isdn)
                        {
                            stringBuilder.Append(NetworkInterfaceType.Isdn.ToString() + " ");
                        }

                        if ((networkInterface.NetworkInterfaceType & NetworkInterfaceType.FastEthernetFx) == NetworkInterfaceType.FastEthernetFx)
                        {
                            stringBuilder.Append(NetworkInterfaceType.FastEthernetFx.ToString() + " ");
                        }

                        if ((networkInterface.NetworkInterfaceType & NetworkInterfaceType.Wireless80211) == NetworkInterfaceType.Wireless80211)
                        {
                            stringBuilder.Append(NetworkInterfaceType.Wireless80211.ToString() + " ");
                        }

                        if ((networkInterface.NetworkInterfaceType & NetworkInterfaceType.AsymmetricDsl) == NetworkInterfaceType.AsymmetricDsl)
                        {
                            stringBuilder.Append(NetworkInterfaceType.AsymmetricDsl.ToString() + " ");
                        }

                        if ((networkInterface.NetworkInterfaceType & NetworkInterfaceType.RateAdaptDsl) == NetworkInterfaceType.RateAdaptDsl)
                        {
                            stringBuilder.Append(NetworkInterfaceType.RateAdaptDsl.ToString() + " ");
                        }

                        if ((networkInterface.NetworkInterfaceType & NetworkInterfaceType.SymmetricDsl) == NetworkInterfaceType.SymmetricDsl)
                        {
                            stringBuilder.Append(NetworkInterfaceType.SymmetricDsl.ToString() + " ");
                        }

                        if ((networkInterface.NetworkInterfaceType & NetworkInterfaceType.VeryHighSpeedDsl) == NetworkInterfaceType.VeryHighSpeedDsl)
                        {
                            stringBuilder.Append(NetworkInterfaceType.VeryHighSpeedDsl.ToString() + " ");
                        }

                        if ((networkInterface.NetworkInterfaceType & NetworkInterfaceType.IPOverAtm) == NetworkInterfaceType.IPOverAtm)
                        {
                            stringBuilder.Append(NetworkInterfaceType.IPOverAtm.ToString() + " ");
                        }

                        if ((networkInterface.NetworkInterfaceType & NetworkInterfaceType.GigabitEthernet) == NetworkInterfaceType.GigabitEthernet)
                        {
                            stringBuilder.Append(NetworkInterfaceType.GigabitEthernet.ToString() + " ");
                        }

                        if ((networkInterface.NetworkInterfaceType & NetworkInterfaceType.MultiRateSymmetricDsl) == NetworkInterfaceType.MultiRateSymmetricDsl)
                        {
                            stringBuilder.Append(NetworkInterfaceType.MultiRateSymmetricDsl.ToString() + " ");
                        }

                        if ((networkInterface.NetworkInterfaceType & NetworkInterfaceType.HighPerformanceSerialBus) == NetworkInterfaceType.HighPerformanceSerialBus)
                        {
                            stringBuilder.Append(NetworkInterfaceType.HighPerformanceSerialBus.ToString() + " ");
                        }

                        if ((networkInterface.NetworkInterfaceType & NetworkInterfaceType.Wman) == NetworkInterfaceType.Wman)
                        {
                            stringBuilder.Append(NetworkInterfaceType.Wman.ToString() + " ");
                        }

                        if ((networkInterface.NetworkInterfaceType & NetworkInterfaceType.Wwanpp) == NetworkInterfaceType.Wwanpp)
                        {
                            stringBuilder.Append(NetworkInterfaceType.Wwanpp.ToString() + " ");
                        }

                        if ((networkInterface.NetworkInterfaceType & NetworkInterfaceType.Wwanpp2) == NetworkInterfaceType.Wwanpp2)
                        {
                            stringBuilder.Append(NetworkInterfaceType.Wwanpp2.ToString() + " ");
                        }

                        Trace.WriteLine($"network interface '{networkInterface.Name}' of type '{stringBuilder.ToString()}' is connected");
                        result = true;
                    }
                }

                return result;
            }

            //
            // Résumé :
            //     Set a mock for the method Bb.InternetConnectivityChecker.IsConnectedToInternet
            //
            //
            // Paramètres :
            //   method:
            public static void SetMock(Func<Task<bool>> method)
            {
                InternetMethodMock = method;
            }

            private static async Task<bool> _isConnectedToInternet()
            {
                try
                {
                    return (await httpClient.GetAsync("http://www.google.com")).IsSuccessStatusCode;
                }
                catch
                {
                    return false;
                }
            }
        }

    }




}
