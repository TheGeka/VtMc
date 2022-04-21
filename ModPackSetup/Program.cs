using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
namespace ModPackSetup
{
    internal class Program
    {
        private static readonly string _javaInstaller = "https://download.oracle.com/java/18/latest/jdk-18_windows-x64_bin.exe";
        private static readonly string _multiMcInstaller = "https://files.multimc.org/downloads/mmc-stable-win32.zip";
        private static readonly string _modPackInstance = "https://cdn.discordapp.com/attachments/962102553963814963/966424917539188746/vtmc_rc2.zip";

        private static readonly string _desktopFolder = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);



        static async Task Main(string[] args)
        {
            Console.ForegroundColor = ConsoleColor.White;


            Console.WriteLine("1/3 Java setup");
            await GetJavaVersion();
            Console.WriteLine("2/3 MultiMc setup");
            await LookForPreviousMmcAsync();
            Console.WriteLine("3/3 Installing ModPack");
            await InstallModPack();
            Console.WriteLine("Setup done!");
            Console.Write("Create MultiMc Desktop shortcut? Y/N:");
            string mmcFolder = Path.Combine(_desktopFolder, "MultiMC");
            if (string.Equals(Console.ReadLine(), "Y", StringComparison.InvariantCultureIgnoreCase))
            {
                CreateUacProcess("cmd", @$" /C mklink {Path.Combine(_desktopFolder, "MultiMC")} {Path.Combine(mmcFolder, "MultiMC.exe")}").Start();
            }
            CreateProcess("explorer.exe", mmcFolder).Start();
            Environment.Exit(0);
        }
        private static async Task GetJavaVersion()
        {
            try
            {
                Console.WriteLine("Detecting Java version");
                var proc = CreateProcess(@"C:/Program Files/Java/jdk-18.0.1/bin/javaw.exe", " --version");
                proc.Start();
                var version = proc.StandardOutput.ReadLine();
                Console.WriteLine($"Found version {version}");
                if (!version.Contains("java 18.0.1", StringComparison.CurrentCultureIgnoreCase))
                {
                    await InstallJava18Async();
                }

            }
            catch (Exception e)
            {
                Console.WriteLine("No java installation found, installing java 18...");
                await InstallJava18Async();                
            }
        }
        private static async Task InstallJava18Async()
        {
            Console.WriteLine("Downloading installer...");
            await DownloadHelper.DownloadAsync(_javaInstaller, "JavaInstaller.exe");
            
            Console.WriteLine("Download finished, installing...");    
            var Installerproc = CreateUacProcess(Path.Combine(Directory.GetCurrentDirectory(), "JavaInstaller.exe"), "/s");
            Installerproc.Start();
            Console.WriteLine("It's not stuck just wait");
            await Installerproc.WaitForExitAsync();
            try
            {
                var proc = CreateProcess("C:/Program Files/Java/jdk-18.0.1/bin/javaw.exe", "-version");
                proc.Start();
                if (proc.StandardError.ReadLine().Contains("java 18.0.1", StringComparison.CurrentCultureIgnoreCase))
                {
                    return;
                }
                else
                {
                }
            }
            catch (Exception e)
            {

                Console.WriteLine("Java installation failed contact Tech Support");
                Console.WriteLine(e);
                Console.ReadLine();
                throw;
            }
            

        }
        private static Process CreateProcess(string command, string args)
        {
            ProcessStartInfo procStartInfo =
                    new ProcessStartInfo(command, args);

            procStartInfo.RedirectStandardOutput = true;
            procStartInfo.RedirectStandardError = true;
            procStartInfo.UseShellExecute = false;
            procStartInfo.CreateNoWindow = true;
            Process proc = new Process();
            proc.StartInfo = procStartInfo;
            return proc;
        }
        private static Process CreateUacProcess(string command, string args)
        {
            ProcessStartInfo procStartInfo =
                    new ProcessStartInfo(command, args);

            procStartInfo.Verb = "runas";
            procStartInfo.UseShellExecute = true;
            procStartInfo.RedirectStandardOutput = false;
            procStartInfo.RedirectStandardError = false;
            procStartInfo.CreateNoWindow = true;
            Process proc = new Process();
            proc.StartInfo = procStartInfo;
            return proc;
        }
        private static async Task LookForPreviousMmcAsync()
        {
            Console.WriteLine("Looking for MultiMc in desktop");
            if (Directory.Exists(Path.Combine(_desktopFolder, "MultiMC")))
            {
                Console.WriteLine("Found MultiMc, Skipping install");
                return;
            }
            else
            {
                Console.WriteLine("MultiMc not found, Installing...");
                await InstallMultiMc(_desktopFolder);
            }

        }
        private static async Task InstallMultiMc(string installPath)
        {
            Console.WriteLine("Downloading MultiMc");
            await DownloadHelper.DownloadAsync(_multiMcInstaller, "multimc.zip");
            Console.WriteLine("Download finished, installing...");
            ZipFile.ExtractToDirectory(Path.Combine(Directory.GetCurrentDirectory(), "multimc.zip"),
                installPath, true);

            var cfg = File.ReadAllLines(Path.Combine(Directory.GetCurrentDirectory(), "multimc.cfg"));
            var index = Array.FindIndex(cfg, s => s.Contains("LastHostname"));
            cfg[index] = $"LastHostname={System.Environment.MachineName}";
            File.WriteAllLines(Path.Combine(installPath, "MultiMC", "multimc.cfg"), cfg);

        }
        private static async Task InstallModPack()
        {
            Console.WriteLine("Downloading ModPack");
            await DownloadHelper.DownloadAsync(_modPackInstance, "vtmc.zip");
            Console.WriteLine("Download finished, installing...");
            var modpackPath = Path.Combine(Directory.GetCurrentDirectory(), "vtmc.zip");
            var MultiMcExe = Path.Combine(_desktopFolder, "MultiMC", "MultiMC.exe");
            var mmc = CreateProcess(MultiMcExe, $"-I \"{modpackPath.Replace("\\", "/")}\"");
            
            
            mmc.Start();
            await ReadLogAsync(mmc, Path.Combine(_desktopFolder, "MultiMC"));
            await mmc.WaitForExitAsync();



        }

        private static async Task ReadLogAsync(Process mmc, string multimcPath)
        {
            var logFile = Path.Combine(multimcPath, "MultiMC-0.log");
            if (File.Exists(logFile))
            {
                File.Copy(logFile, Path.Combine(multimcPath, "MultiMC-OneClick.log"),true);
            }
            Thread.Sleep(2000);
            using (var fs = File.Open(logFile, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using (var reader = new StreamReader(fs))
            {
                while (true)
                {
                    var line = await reader.ReadLineAsync();
                    if (!string.IsNullOrWhiteSpace(line))
                    {
                        if (Regex.Match(line, @"D .+InstanceStaging\(.+\).+ (succeeded)").Success)
                        {
                            mmc.Kill();
                            break;
                        }
                    }
                }
            }
        }
    }


    
}
