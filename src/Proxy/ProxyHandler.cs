using System.ComponentModel;
using System.Net;
using BoothWatcher.JSON;

namespace BoothWatcher
{
    public static class Proxyhandler
    {
        private static List<string> proxylistparsed = new();

        private static readonly string _proxiefile = "Proxies.txt";

        // called at startup if no list exists
        internal static void DownloadFreeProxies()
        {
            try
            {
                using WebClient wc = new WebClient();
                var uri = JsonConfig._instance._proxyHost;
                wc.DownloadProgressChanged += DownloadProgressCallback;
                wc.DownloadFileCompleted += DownloadProcessCompleteCallBack;
                wc.DownloadFileAsync(new Uri(uri), _proxiefile);
            }
            catch (Exception exception)
            {
                Console.WriteLine($"exception at method DownloadFreeProxies {exception}");

            }
        }

        private static void DownloadProgressCallback(object sender, DownloadProgressChangedEventArgs e)
        {
            try
            {
                Console.WriteLine("{0}    downloaded {1} of {2} bytes. {3} % complete...",
                    (string) e.UserState!,
                    e,
                    e.TotalBytesToReceive,
                    e.ProgressPercentage);
            }
            catch (Exception ext)
            {
                Console.WriteLine($"Exception created in DownloadProgressCallback {ext.StackTrace}");
            }
        }

        private static void DownloadProcessCompleteCallBack(object? sender, AsyncCompletedEventArgs e)
        {
            try
            {
                if (!e.Cancelled && e.Error == null)
                {
                    Console.WriteLine($"Download of Proxies complete");
                    Addproxies();
                }
            }
            catch (Exception exception)
            {
                Console.WriteLine($"exception at method DownloadProcessCompleteCallBack {exception}");
            }

        }

        // adds proxies to list after startup and then deletes file for next list
        private static void Addproxies()
        {
            foreach (string line in File.ReadAllLines(_proxiefile))
            {
                var split = line.Split(':');
                var ip = split[0];
                var port = split[1];
                var username = split[2];
                var password = split[3];
                var result = ip + ":" + port;
                proxylistparsed.Add(result);
            }

            File.Delete(_proxiefile);
        }

        internal static string Randomprox()
        {
            var randomprox = new Random();
            var listdex = randomprox.Next(proxylistparsed.Count);
            return proxylistparsed[listdex];
        }

        // Called after 1 hour to redownload a fresh list of proxies
        internal static void ResetProxies(object? sender = null, System.Timers.ElapsedEventArgs? e = null)
        {
            Console.WriteLine("redownloading proxylist");
            proxylistparsed.Clear();
            DownloadFreeProxies();
        }
    }
}