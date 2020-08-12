using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
using Windows.ApplicationModel;
using Windows.ApplicationModel.AppService;
using Windows.Foundation.Collections;
using Windows.Storage;

namespace ConTeXt_WPF
{
    public partial class App : Application
    {
        public int updateoutput = 0;

        private const int SW_HIDE = 0;

        private const int SW_SHOW = 5;

        private AppServiceConnection connection;

        public App()
        {
            var handle = GetConsoleWindow();
            ShowWindow(handle, SW_SHOW);
            try
            {
                Log(Settings.Default.PackageID);
                InitializeAppServiceConnection();
                Log(Settings.Default.ContextDistributionPath);
            }
            catch (Exception ex)
            {
                Log(ex.Source + "\n" + ex.Message);
            }
        }

        protected override void OnActivated(EventArgs e)
        {
            base.OnActivated(e);
        }

        [DllImport("kernel32.dll")]
        private static extern IntPtr GetConsoleWindow();

        // This is to change visibility of the console window if ConTeXt_WPF is packaged as console app
        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        private void Compile()
        {
            Process p = new Process();
            Log(Settings.Default.TexFilePath);
            ProcessStartInfo info = new ProcessStartInfo(@"C:\Windows\System32\cmd.exe")
            {
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = false,
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden,
                UseShellExecute = false,
                WorkingDirectory = Settings.Default.TexFileFolder
            };
            p.OutputDataReceived += (e, f) => { Log(f.Data); };
            //p.ErrorDataReceived += (e, f) => {Log(f.Data); };
            p.StartInfo = info;
            p.Start();
            p.BeginOutputReadLine();
            //using (StreamReader sr = p.StandardOutput)
            using (StreamWriter sw = p.StandardInput)
            {
                Log("deleting cached files...");
                //sw.WriteLine("del *.log && del *.pdf && del *.tuc");
                Log("setting up context...");
                //await sw.WriteLineAsync("dir");
                //sw.WriteLine(Settings.Default.ContextDistributionPath + @"\" + @"tex\setuptex.bat tex\texmf-win64\bin");
                Log("compiling...");
                // await sw.WriteLineAsync("cd sample");
                string param = "";
                if (Settings.Default.Modes.Length > 0)
                    param += "--mode=" + Settings.Default.Modes + " ";
                if (Settings.Default.AdditionalParameters.Trim().Length > 0)
                    param += "" + Settings.Default.AdditionalParameters + " ";

                sw.WriteLine(Settings.Default.ContextDistributionPath + @"\tex" + getversion() + @"\bin\context.exe" + " " + param + Settings.Default.TexFileName);

                // Thread.Sleep(5000);
            }

            //Console.WriteLine(output);
            //Thread.Sleep(10000);
            p.WaitForExit();
            //string output = p.StandardOutput.ReadToEnd();
            Log("finished!");
            //Log("Output:\n"+output);
            //string filename = "C:\\Users\\Welter\\Desktop\\sample\\bla.pdf";
        }

        private async void Connection_RequestReceived(AppServiceConnection sender, AppServiceRequestReceivedEventArgs args)
        {
            try
            {
                switch ((string)args.Request.Message.FirstOrDefault().Key)
                {
                    case "compile":
                        {
                            try
                            {
                                Compile();
                                Log("\nOpening...");
                                var sf = File.Exists(Settings.Default.TexFilePath + @"-error.log");
                                Log(sf.ToString());
                                if (!sf)
                                {
                                    //Openinapp();
                                    ValueSet response = new ValueSet
                                    {
                                        { "response", "compiled" }
                                    };
                                    await args.Request.SendResponseAsync(response);
                                }
                                else
                                {
                                    var ff = await StorageFile.GetFileFromPathAsync(Settings.Default.TexFilePath + @"\" + Path.GetFileNameWithoutExtension(Settings.Default.TexFileName) + @"-error.log");
                                    ValueSet response = new ValueSet();
                                    Log("file opened");
                                    var text = await FileIO.ReadTextAsync(ff);
                                    Log("file read");
                                    response.Add("response", text);

                                    await args.Request.SendResponseAsync(response);
                                    Log("Response sent");
                                }
                            }
                            catch (Exception e)
                            {
                                ValueSet response = new ValueSet
                                {
                                    { "response", e.Message }
                                };
                                await args.Request.SendResponseAsync(response);
                            }
                            break;
                        }
                    case "save":
                        {
                            try
                            {
                                var file = await StorageFile.GetFileFromPathAsync(Settings.Default.TexFilePath + @"\" + Settings.Default.TexFileName);

                                StorageFolder storageFolder = ApplicationData.Current.LocalFolder;
                                StorageFile sampleFile = await storageFolder.GetFileAsync(Settings.Default.TexFileName);
                                string text = await FileIO.ReadTextAsync(sampleFile);
                                await FileIO.WriteTextAsync(file, text);
                                await Task.Delay(500);
                                ValueSet response = new ValueSet
                                {
                                    { "response", "saved" }
                                };
                                await args.Request.SendResponseAsync(response);
                            }
                            catch (Exception e)
                            {
                                Log(e.Message);
                            }

                            break;
                        }
                    case "newtexfile":
                        {
                            string path = Settings.Default.TexFilePath + @"\" + Settings.Default.TexFileName;
                            if (!File.Exists(path))
                            {
                                using (StreamWriter sw = File.CreateText(path))
                                {
                                    sw.WriteLine(@"\starttext");
                                    sw.WriteLine(@"\stoptext");
                                }
                            }
                            ValueSet response = new ValueSet
                            {
                                { "response", "file created" }
                            };
                            await args.Request.SendResponseAsync(response);
                            break;
                        }
                    case "command":
                        {
                            switch ((string)args.Request.Message.FirstOrDefault().Value)
                            {
                                case "update":
                                    {
                                        bool updated = Update();
                                        ValueSet response = new ValueSet
                                        {
                                            { "response", updated }
                                        };
                                        await args.Request.SendResponseAsync(response);
                                        break;
                                    }
                                case "install":
                                    {
                                        Log("installing");
                                        bool installed = Install();

                                        ValueSet response = new ValueSet
                                        {
                                            { "response", installed }
                                        };
                                        await args.Request.SendResponseAsync(response);
                                        break;
                                    }
                                default: break;
                            }
                            break;
                        }
                    default: break;
                }
            }
            catch (Exception ex)
            {
                Log(ex.Message);
            }
        }

        private void Connection_ServiceClosed(AppServiceConnection sender, AppServiceClosedEventArgs args)
        {
            System.Environment.Exit(0);
            //Dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(() =>
            //{
            //}));
        }

        private string getversion()
        {
            if (Directory.Exists(Settings.Default.ContextDistributionPath + @"\tex\texmf-win64"))
            {
                return @"\texmf-win64";
            }
            else if (Directory.Exists(Settings.Default.ContextDistributionPath + @"\tex\texmf-mswin"))
            {
                return @"\texmf-mswin";
            }
            else
                return @"\texmf-mswin";
        }
        private async void InitializeAppServiceConnection()
        {
            connection = new AppServiceConnection
            {
                AppServiceName = "Compiler",
                PackageFamilyName = Settings.Default.PackageID
            };
            connection.RequestReceived += Connection_RequestReceived;
            connection.ServiceClosed += Connection_ServiceClosed;
            var status = await connection.OpenAsync();
            Log("Started");
        }

        private bool Install()
        {
            try
            {
                //string installurl = jsonSettings.Default.ContextDownloadLink;
                string contextDistributionPath = Settings.Default.ContextDistributionPath;
                //Log("Setting up Web Client");
                //using (WebClient client = new WebClient())
                //{
                //    Log("Downloading");

                //    DirectoryInfo di = Directory.CreateDirectory(contextDistributionPath);
                //    client.DownloadFile(installurl, contextDistributionPath + @"\context.zip");
                //    Log("Download finished");
                //    client.Dispose();
                //}
                string root = Package.Current.InstalledLocation.Path;
                string path = root + @"\ConTeXt_WPF";

                Log(path);

                var templateFolder = StorageFolder.GetFolderFromPathAsync(path).AsTask().Result;
                var archive = templateFolder.GetFileAsync("context-mswin.zip").AsTask().Result;

                //var copiedfile =  archive.CopyAsync(ApplicationData.Current.LocalFolder).AsTask().Result;
                //StorageFile file = await StorageFile.GetFileFromApplicationUriAsync(new Uri("ms-appx:///context.zip"));
                Log("Extracting");
                ZipFile.ExtractToDirectory(archive.Path, ApplicationData.Current.LocalFolder.Path);
                //Log("Extracting Zip Archive");
                //using (ZipArchive archive = ZipFile.Open(contextDistributionPath + @"\context.zip", ZipArchiveMode.Read))
                //{
                //    archive.ExtractToDirectory(contextDistributionPath);
                //}

                //var sf =  StorageFile.GetFileFromPathAsync(contextDistributionPath + @"\context-mswin.zip").AsTask().Result;
                //sf.DeleteAsync().AsTask().Wait();

                // return Update();
                return true;
            }
            catch (Exception ex)
            {
                Log(ex.Message);
                return false;
            }
        }

        private async void Log(string msg)
        {
            Console.WriteLine(msg);
            //var vs = new ValueSet
            //{
            //    { "log", msg }
            //};
            //await connection.SendMessageAsync(vs);
        }

        private bool Update()
        {
            try
            {
                Process p = new Process();
                ProcessStartInfo info = new ProcessStartInfo(@"C:\Windows\System32\cmd.exe")
                {
                    RedirectStandardInput = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = false,
                    CreateNoWindow = true,
                    WindowStyle = ProcessWindowStyle.Hidden,
                    UseShellExecute = false,
                    WorkingDirectory = Settings.Default.ContextDistributionPath
                };
                p.OutputDataReceived += (e, f) => { updateoutput++; Log(f.Data); };
                p.StartInfo = info;
                p.Start();
                p.BeginOutputReadLine();

                using (StreamWriter sw = p.StandardInput)
                {
                    sw.WriteLine(@"install.bat --modules=all");
                    Log("Setting up PATH");
                    sw.WriteLine("setx path \"%PATH%;" + Settings.Default.ContextDistributionPath + @"\tex\texmf-mswin\bin" + "\"");
                }
                p.WaitForExit();
                return true;
            }
            catch { return false; }
        }
    }

    public class Program
    {
        private static void Main(string[] args)
        {
            var app = new App();

            while (true)
            {
                Console.ReadLine();
            }
        }
    }
}