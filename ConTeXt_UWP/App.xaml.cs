using ConTeXt_UWP.Models;
using ConTeXt_UWP.ViewModels;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.ApplicationModel.AppService;
using Windows.ApplicationModel.Background;
using Windows.ApplicationModel.Core;
using Windows.Foundation.Collections;
using Windows.Foundation.Metadata;
using Windows.Storage;
using Windows.UI;
using Windows.UI.Core;
using Windows.UI.Popups;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

namespace ConTeXt_UWP
{
    sealed partial class App : Application
    {

        public App()
        {
            this.InitializeComponent();


            this.Suspending += OnSuspending;
        }

        public static MainPage MainPage { get; set; }
        public static ViewModel VM { get; set; }
        protected override void OnCachedFileUpdaterActivated(CachedFileUpdaterActivatedEventArgs args)
        {
            base.OnCachedFileUpdaterActivated(args);
        }

        protected override async void OnFileActivated(FileActivatedEventArgs args)
        {
            Frame rootFrame = Window.Current.Content as Frame;
            if (rootFrame == null)
            {
                

                await StartUp();

                VM.FileActivatedEvents.Add(args);

                

                rootFrame = new Frame();
                rootFrame.NavigationFailed += OnNavigationFailed;
                Window.Current.Content = rootFrame;

                if (rootFrame.Content == null)
                {
                    // rootFrame.Navigate(typeof(MainPage), e.Arguments);
                    MainPage = new MainPage();
                    rootFrame.Content = MainPage;
                }
                Window.Current.Activate();

            }
            else
            {
                foreach (StorageFile file in args.Files)
                {
                    var fileitem = new FileItem(file) { };
                    VM.OpenFile(fileitem);
                }
            }
        }

        protected override async void OnLaunched(LaunchActivatedEventArgs e)
        {
            try
            {
                Frame rootFrame = Window.Current.Content as Frame;

                if (rootFrame == null)
                {
                    rootFrame = new Frame();
                    rootFrame.NavigationFailed += OnNavigationFailed;
                    if (e.PreviousExecutionState == ApplicationExecutionState.Terminated)
                    {
                    }
                    Window.Current.Content = rootFrame;
                    await StartUp();
                }

                if (e.PrelaunchActivated == false)
                {
                    if (rootFrame.Content == null)
                    {
                        //rootFrame.Navigate(typeof(MainPage), e.Arguments);
                        //await Windows.UI.Core.CoreWindow.GetForCurrentThread().Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                        //{
                        //    MainPage = new MainPage();
                        //    rootFrame.Content = MainPage;
                        //});
                        MainPage = new MainPage();
                        rootFrame.Content = MainPage;
                      
                        
                    }
                    Window.Current.Activate();
                }

                MainPage.FirstStart();
            }
            catch (Exception ex)
            {
                
                VM.Message("OnLaunched: " + ex.Message);
            }
        }



        private void App_BackRequested(object sender, BackRequestedEventArgs e)
        {
            //throw new NotImplementedException();
        }

        private void AppServiceConnection_RequestReceived(AppServiceConnection sender, AppServiceRequestReceivedEventArgs args)
        {
            var appServiceDeferral = args.GetDeferral();
            VM.LOG("Background Service Message: " + args.Request.Message.Values.FirstOrDefault() as string);
            appServiceDeferral.Complete();
        }

        private Frame CreateRootFrame()
        {
            // Do not repeat app initialization when the Window already has content,
            // just ensure that the window is active
            if (!(Window.Current.Content is Frame rootFrame))
            {
                // Create a Frame to act as the navigation context and navigate to the first page
                rootFrame = new Frame
                {
                    Language = Windows.Globalization.ApplicationLanguages.Languages[0]
                };
                rootFrame.NavigationFailed += OnNavigationFailed;
                // Place the frame in the current Window
                Window.Current.Content = rootFrame;
            }

            return rootFrame;
        }

        void OnNavigationFailed(object sender, NavigationFailedEventArgs e)
        {
            throw new Exception("Failed to load Page " + e.SourcePageType.FullName);
        }

        private void OnSuspending(object sender, SuspendingEventArgs e)
        {
            var deferral = e.SuspendingOperation.GetDeferral();
            deferral.Complete();
        }

        protected override void OnBackgroundActivated(BackgroundActivatedEventArgs args)
        {
            base.OnBackgroundActivated(args);
            if (args.TaskInstance.TriggerDetails is AppServiceTriggerDetails details)
            {
                // only accept connections from callers in the same package
                if (details.CallerPackageFamilyName == Package.Current.Id.FamilyName)
                {
                    // connection established from the fulltrust process
                    VM.AppServiceDeferral = args.TaskInstance.GetDeferral();
                    args.TaskInstance.Canceled += OnTaskCanceled;

                    //coreDispatcher = Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher;
                    VM.AppServiceConnection = details.AppServiceConnection;

                    VM.AppServiceConnection.RequestReceived += AppServiceConnection_RequestReceived;

                    //Connection.RequestReceived += (a, b) => { AppViewModel.LOG(b.Request.Message.Values.FirstOrDefault() as string); };
                    //currViewModel.appServiceConnection = Connection;
                    // currViewModel.appServiceConnection.RequestReceived += (a, b) => {  };
                    //AppServiceConnected?.Invoke(this, args.TaskInstance.TriggerDetails as AppServiceTriggerDetails);
                }
            }
        }

        private void OnTaskCanceled(IBackgroundTaskInstance sender, BackgroundTaskCancellationReason reason)
        {
        }
        private async Task StartUp()
        {
            try
            {
                Resources.TryGetValue("VM", out object Vm);
                if (Vm != null)
                {
                    VM = Vm as ViewModel;
                }
                else VM = new ViewModel();
                VM.Default = Settings.Default;

                if (ApiInformation.IsApiContractPresent("Windows.ApplicationModel.FullTrustAppContract", 1))
                {
                    await FullTrustProcessLauncher.LaunchFullTrustProcessForCurrentAppAsync("Parameters");
                }

                CoreApplication.GetCurrentView().TitleBar.ExtendViewIntoTitleBar = true;
                ApplicationViewTitleBar titleBar = ApplicationView.GetForCurrentView().TitleBar;

                titleBar.ButtonBackgroundColor = Colors.Transparent;
                titleBar.ButtonInactiveBackgroundColor = Colors.Transparent;
                titleBar.ButtonForegroundColor = titleBar.ButtonInactiveForegroundColor = Colors.White;
                titleBar.BackgroundColor = Colors.Transparent;
                SystemNavigationManager.GetForCurrentView().BackRequested += App_BackRequested;

                StorageFolder localFolder = ApplicationData.Current.LocalFolder;

                //if (VM.Default.)
                string file = @"tex\texmf-mswin\bin\context.exe";
                var storageFolder = ApplicationData.Current.LocalFolder;
                string filePath = Path.Combine(storageFolder.Path, file);
                //App.VM.LOG(filePath);

                if (Windows.System.Profile.WindowsIntegrityPolicy.IsEnabled)
                {
                    var installing = new ContentDialog() { Title = "It seems that you are running Windows 10 in S mode.", Content = "You will not be able to use the ConTeXt compiler.", PrimaryButtonText = "Ok", DefaultButton = ContentDialogButton.Primary };
                }
                else
                if (!VM.Default.DistributionInstalled && !File.Exists(filePath))
                {
                    VM.IsSaving = true;
                    VM.IsPaused = false;
                    //var cd = new ContentDialog();
                    //var sp = new StackPanel();

                    // var installpathtb = new TextBox() { Header = "Install Path (changing this is experimental)", Text = localFolder.Path };
                    //var downloadlinktb = new TextBox() { Header = "Download link (only change if PRAGMA ADE changed the URL)", Text = VM.Default.ContextDownloadLink };
                    ////sp.Children.Add(installpathtb);
                    //sp.Children.Add(downloadlinktb);
                    //cd.Title = "First Start: Install the ConTeXt (LuaMetaTeX) Distribution";
                    //cd.Content = sp;
                    //cd.PrimaryButtonText = @"Install (~ 270 MB)";
                    //cd.CloseButtonText = "Skip this time";
                    //cd.DefaultButton = ContentDialogButton.Primary;
                    //if (await cd.ShowAsync() == ContentDialogResult.Primary)
                    //{
                    //    if (NetworkInterface.GetIsNetworkAvailable())
                    //    {
                    VM.Default.ContextDistributionPath = localFolder.Path;
                    //VM.Default.ContextDownloadLink = downloadlinktb.Text;
                    //var installing = new ContentDialog() { Title = "Please wait while installing. This can take up to 10 minutes. Do not close this window." };
                    //var prog = new Microsoft.UI.Xaml.Controls.ProgressBar() { IsIndeterminate = true };
                    //installing.Content = prog;
                    //installing.ShowAsync();

                    //var file = await localFolder.GetFileAsync("file.json");
                    //await file.DeleteAsync();
                    //string root = Windows.ApplicationModel.Package.Current.InstalledLocation.Path;
                    //string path = root;

                    //var templateFolder = await StorageFolder.GetFolderFromPathAsync(path);
                    //await new MessageDialog(path, "path").ShowAsync();
                    //var archive = await templateFolder.GetFileAsync("context-mswin.zip");
                    //await new MessageDialog(path, "path").ShowAsync();

                    //var copiedfile = await archive.CopyAsync(localFolder);
                    //StorageFile file = await StorageFile.GetFileFromApplicationUriAsync(new Uri("ms-appx:///context.zip"));
                    //ZipFile.ExtractToDirectory(copiedfile.Path, localFolder.Path);
                    //installing.Hide();
                    //update();
                    //VM.LOG("Installing... This can take up to 10 minutes depending on your system and the download speed.");
                    //VM.LOG("You can start editing in the meanwhile. Please go to \"Projects\" to add a project folder");
                    ValueSet request = new ValueSet();
                    request.Add("command", "install");
                    while (VM.AppServiceConnection == null)
                    {
                        await Task.Delay(200);
                    }

                    AppServiceResponse response = await VM.AppServiceConnection.SendMessageAsync(request);
                    //AppViewModel.LOG(response.Status.ToString() + " ... " + response.Message.FirstOrDefault().Key.ToString() + " Key Val " + response.Message.FirstOrDefault().Value.ToString());
                    // display the response key/value pairs
                    if (response != null)
                        foreach (string key in response.Message.Keys)
                        {
                            if (key == "response")
                            {
                                if ((bool)response.Message[key])
                                {
                                    VM.LOG("ConTeXt distribution installed.");
                                    //installing.Title = "ConTeXt distribution installed!";
                                    //prog.ShowPaused = true;
                                }
                                else
                                {
                                    VM.LOG("Installation error");
                                    //installing.Title = "Error. Please try again after a reinstall of this app. Make sure to have at least 200 MB of free space.";
                                    //prog.ShowError = true;
                                }
                                //installing.PrimaryButtonText = "Ok";
                                //installing.IsPrimaryButtonEnabled = true;
                                //installing.DefaultButton = ContentDialogButton.Primary;
                                //installing.PrimaryButtonClick += (a, b) => { MainPage.FirstStart(); };
                                
                            }
                        }


                    VM.IsSaving = false;
                    VM.IsPaused = true;
                    VM.IsVisible = false;
                }

                await VM.Startup();

                VM.Default.DistributionInstalled = true;
            }
            catch (Exception ex)
            {
                VM.Message(ex.Message,"Error on app startup");
                Debug.WriteLine(ex.Message);
            }
        }
    }
}
