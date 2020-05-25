using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Threading.Tasks;
using System.Windows.Threading;
using Windows.ApplicationModel.AppService;
using Windows.Foundation.Collections;
using Windows.Storage;
using System.Threading;

namespace ConTeXt_WPF
{
    public class Program
    {

        static void Main(string[] args)
        {
           
            // kick off asynchronous stuff 
            
            Console.WriteLine("loool");
           var app = new App();

            while (true) {
                Console.ReadLine();
            }
        }
    }
    public partial class App : Application
    {
        public int updateoutput = 0;

        private const int SW_HIDE = 0;

        private const int SW_SHOW = 5;

        private AppServiceConnection connection;

        public App()
        {
            // InitializeComponent();
            var handle = GetConsoleWindow();
            ShowWindow(handle, SW_SHOW);
            Log(Settings.Default.PackageID);
            InitializeAppServiceConnection();
            Log(Settings.Default.ContextDistributionPath);
        }

        protected override void OnActivated(EventArgs e)
        {
            base.OnActivated(e);
        }

        [DllImport("kernel32.dll")]
        private static extern IntPtr GetConsoleWindow();

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
                sw.WriteLine("del *.log && del *.pdf && del *.tuc");
                Log("setting up context...");
                //await sw.WriteLineAsync("dir");
                //sw.WriteLine(Settings.Default.ContextDistributionPath + @"\" + @"tex\setuptex.bat tex\texmf-win64\bin");
                Log("compiling...");
                // await sw.WriteLineAsync("cd sample");
                sw.WriteLine(Settings.Default.ContextDistributionPath + @"\tex\texmf-win64\bin\context.exe"+ " " + Settings.Default.TexFileName);

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
                                        Update();
                                        ValueSet response = new ValueSet
                                        {
                                            { "response", "updated" }
                                        };
                                        await args.Request.SendResponseAsync(response);
                                        break;
                                    }
                                case "install":
                                    {
                                        Log("installing");
                                        bool installed = await Install(Settings.Default.ContextDistributionPath);
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
        private async Task<bool> Install(string contextDistributionPath)
        {
            try
            {
                string installurl = Settings.Default.ContextDownloadLink;
                Log("Setting up Web Client");
                using (WebClient client = new WebClient())
                {
                    Log("Downloading");
                    DirectoryInfo di = Directory.CreateDirectory(contextDistributionPath);
                    client.DownloadFile(installurl, contextDistributionPath + @"\context-win64.zip");
                    Log("Download finished");
                    client.Dispose();
                }
                Log("Extracting Zip Archive");
                using (ZipArchive archive = ZipFile.Open(contextDistributionPath + @"\context-win64.zip", ZipArchiveMode.Read))
                {
                    archive.ExtractToDirectory(contextDistributionPath);
                }
                Log("Installing the Distribution");
                var sf = await  StorageFile.GetFileFromPathAsync(contextDistributionPath + @"\context-win64.zip");
                await sf.DeleteAsync();
                await Update();
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

        private void Open()
        {
            Process p = new Process();
            p.StartInfo.FileName = "C:\\Users\\welte\\Desktop\\context\\bla.pdf";
            p.Start();
            p.WaitForExit();
        }

        private void Openinapp()
        {
            string local = ApplicationData.Current.LocalFolder.Path;
            string curFile = Path.GetFileName(Settings.Default.TexFilePath + @"\" + Settings.Default.TexFileName);
            string curPDF = Path.GetFileNameWithoutExtension(curFile) + ".pdf";
            string curPDFPath = Path.Combine(Settings.Default.TexFilePath, curPDF);
            string newPathToFile = Path.Combine(local, curPDF);
            File.Copy(curPDFPath, newPathToFile, true);
        }
        private async Task<bool> Update()
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
                    sw.WriteLine("setx path \"%PATH%;" + Settings.Default.ContextDistributionPath + @"\tex\texmf-win64\bin" + "\"");
                }
                //string output = p.StandardOutput.ReadToEnd();
                //Log(output);
                //Thread.Sleep(10000);
                p.WaitForExit();
                Log("count = " + updateoutput.ToString());
                return true;
            }
            catch { return false; }
        }
    }
}
