using Downloader;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ShellProgressBar;
using System.Threading;

namespace ModPackSetup
{
    public static class DownloadHelper
    {
        public static ProgressBar downloadProgres { get; set; }
        public static async Task DownloadAsync(string url, string filename)
        {
            var dl = DownloadBuilder.New()
                .WithUrl(url)
                .WithDirectory(Directory.GetCurrentDirectory())
                .WithFileName(filename)
                .WithConfiguration(new DownloadConfiguration())
                .Build();
            dl.DownloadStarted += Dl_DownloadStarted;
            dl.DownloadProgressChanged += OnDownloadProgressChanged;
            dl.DownloadFileCompleted += Dl_DownloadFileCompleted;
            await dl.StartAsync();
        }

        private static void Dl_DownloadFileCompleted(object sender, System.ComponentModel.AsyncCompletedEventArgs e)
        {
            downloadProgres?.Tick(10000);
            Thread.Sleep(500);
            if (e.Cancelled)
            {
                Console.WriteLine("Download canceled!");
            }
            else if (e.Error != null)
            {
                Console.Error.WriteLine(e.Error);
            }
            Console.Write(new string('\n', 3));
        }

        private static void Dl_DownloadStarted(object sender, DownloadStartedEventArgs e)
        {
            downloadProgres = new ProgressBar(10000, $"Downloading {e.FileName}...");
        }

        private static void OnDownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e)
        {
            downloadProgres.Tick((int)(e.ProgressPercentage * 100));            
        }
    }
    
}
