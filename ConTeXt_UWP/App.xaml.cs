using System;
using System.Linq;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.ApplicationModel.AppService;
using Windows.ApplicationModel.Background;
using Windows.ApplicationModel.Core;
using Windows.Foundation.Collections;
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
    /// <summary>
    /// Provides application-specific behavior to supplement the default Application class.
    /// </summary>
    sealed partial class App : Application
    {
        private static ViewModel viewModel;

        /// <summary>
        /// Initializes the singleton application object.  This is the first line of authored code
        /// executed, and as such is the logical equivalent of main() or WinMain().
        /// </summary>
        public App()
        {
            this.InitializeComponent();
            this.Suspending += OnSuspending;
        }

        public static ViewModel VM
        {
            get
            {
                // Auto-Initialization on first call
                if (viewModel == null)
                    viewModel = new ViewModel();
                return viewModel;
            }
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
                    VM.appServiceConnection = details.AppServiceConnection;

                    VM.appServiceConnection.RequestReceived += AppServiceConnection_RequestReceived;


                    //Connection.RequestReceived += (a, b) => { AppViewModel.LOG(b.Request.Message.Values.FirstOrDefault() as string); };
                    //currViewModel.appServiceConnection = Connection;
                    // currViewModel.appServiceConnection.RequestReceived += (a, b) => {  };
                    //AppServiceConnected?.Invoke(this, args.TaskInstance.TriggerDetails as AppServiceTriggerDetails);
                }
            }
            //(((Window.Current.Content as Frame).Content as MainPage).contentFrame.Content as Editor).LOG("back started");
        }

        protected override void OnCachedFileUpdaterActivated(CachedFileUpdaterActivatedEventArgs args)
        {
            base.OnCachedFileUpdaterActivated(args);
        }

        protected override async void OnFileActivated(FileActivatedEventArgs args)
        {
            // TODO: Handle file activation
            // The number of files received is args.Files.Size
            // The name of the first file is args.Files[0].Name
            if (Window.Current.Content == null)
            {
                VM.FileActivatedEvents.Add(args);
                if (!CreateRootFrame().Navigate(typeof(MainPage)))
                {
                    await new MessageDialog("Error").ShowAsync();
                }
                else
                {
                    //MainPage p = rootFrame.Content as MainPage;
                    //p.ProtocolEvent = null;
                    //p.NavigateToFilePage();
                    // Ensure the current window is active
                    Window.Current.Activate();
                    CoreApplication.GetCurrentView().TitleBar.ExtendViewIntoTitleBar = true;
                    ApplicationViewTitleBar titleBar = ApplicationView.GetForCurrentView().TitleBar;
                    var DefaultTheme = new Windows.UI.ViewManagement.UISettings();

                    var lightbrush = DefaultTheme.GetColorValue(Windows.UI.ViewManagement.UIColorType.Foreground);
                    titleBar.ButtonBackgroundColor = Colors.Transparent;
                    titleBar.ButtonInactiveBackgroundColor = Colors.Transparent;
                    //titleBar.ButtonForegroundColor = titleBar.ButtonInactiveForegroundColor = lightbrush;
                    titleBar.BackgroundColor = Colors.Transparent;
                    titleBar.ButtonForegroundColor = Colors.Transparent;
                    SystemNavigationManager.GetForCurrentView().BackRequested += App_BackRequested;
                    VM.Default.PackageID = Package.Current.Id.FamilyName;
                }
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

        /// <summary>
        /// Invoked when the application is launched normally by the end user. Other entry points
        /// will be used such as when the application is launched to open a specific file.
        /// </summary>
        /// <param name="e">Details about the launch request and process.</param>
        protected override async void OnLaunched(LaunchActivatedEventArgs e)
        {
            // Do not repeat app initialization when the Window already has content,
            // just ensure that the window is active
            VM.Default.PackageID = Package.Current.Id.FamilyName;
            if (!(Window.Current.Content is Frame rootFrame))
            {
                // Create a Frame to act as the navigation context and navigate to the first page
                rootFrame = new Frame();

                rootFrame.NavigationFailed += OnNavigationFailed;

                if (e.PreviousExecutionState == ApplicationExecutionState.Terminated)
                {
                    //TODO: Load state from previously suspended application
                }

                // Place the frame in the current Window
                Window.Current.Content = rootFrame;
            }

            if (e.PrelaunchActivated == false)
            {
                if (rootFrame.Content == null)
                {
                    // When the navigation stack isn't restored navigate to the first page,
                    // configuring the new page by passing required information as a navigation
                    // parameter
                    rootFrame.Navigate(typeof(MainPage), e.Arguments);
                }
                // Ensure the current window is active
                Window.Current.Activate();
            }

            CoreApplication.GetCurrentView().TitleBar.ExtendViewIntoTitleBar = true;
            ApplicationViewTitleBar titleBar = ApplicationView.GetForCurrentView().TitleBar;
            var DefaultTheme = new Windows.UI.ViewManagement.UISettings();

            var lightbrush = DefaultTheme.GetColorValue(Windows.UI.ViewManagement.UIColorType.Foreground);
            titleBar.ButtonBackgroundColor = Colors.Transparent;
            titleBar.ButtonInactiveBackgroundColor = Colors.Transparent;
            titleBar.ButtonForegroundColor = titleBar.ButtonInactiveForegroundColor = lightbrush;
            titleBar.BackgroundColor = Colors.Transparent;
            titleBar.ButtonForegroundColor = Colors.Transparent;
            SystemNavigationManager.GetForCurrentView().BackRequested += App_BackRequested;
            
            if (VM.Default.ContextDistributionPath == "")
            {
                try
                {
                    VM.IsSaving = true;
                    VM.IsPaused = false;
                    var cd = new ContentDialog();
                    var sp = new StackPanel();
                    StorageFolder localFolder = ApplicationData.Current.LocalFolder;
                    var installpathtb = new TextBox() { Header = "Install Path (changing this is experimental)", Text = localFolder.Path };
                    var downloadlinktb = new TextBox() { Header = "Download link (only change if PRAGMA ADE changed the URL)", Text = VM.Default.ContextDownloadLink };
                    sp.Children.Add(installpathtb);
                    sp.Children.Add(downloadlinktb);
                    cd.Title = "First Start: Install the ConTeXt (LuaMetaTeX) Distribution";
                    cd.Content = sp;
                    cd.PrimaryButtonText = "Install";
                    cd.CloseButtonText = "Skip (Setup in the Settings!)";
                    if (await cd.ShowAsync() == ContentDialogResult.Primary)
                    {
                        VM.Default.ContextDistributionPath = installpathtb.Text;
                        VM.Default.ContextDownloadLink = downloadlinktb.Text;
                        var installing = new ContentDialog() { Title = "Please wait while installing. This can take up to 10 minutes." };
                        var prog = new Microsoft.UI.Xaml.Controls.ProgressBar() { IsIndeterminate = true };
                        installing.Content = prog;
                        installing.ShowAsync();

                        //VM.LOG("Installing... This can take up to 10 minutes depending on your system and the download speed.");
                        //VM.LOG("You can start editing in the meanwhile. Please go to \"Projects\" to add a project folder");
                        ValueSet request = new ValueSet();
                        request.Add("command", "install");
                        AppServiceResponse response = await VM.appServiceConnection.SendMessageAsync(request);
                        //AppViewModel.LOG(response.Status.ToString() + " ... " + response.Message.FirstOrDefault().Key.ToString() + " Key Val " + response.Message.FirstOrDefault().Value.ToString());
                        // display the response key/value pairs
                        
                        foreach (string key in response.Message.Keys)
                        {
                            if (key == "response")
                            {
                                if ((bool)response.Message[key])
                                {
                                    VM.LOG("ConTeXt distribution installed.");
                                    installing.Title = "ConTeXt distribution installed!";
                                    prog.ShowPaused = true;

                                }
                                else
                                { 
                                    VM.LOG("Installation error");
                                    installing.Title = "Error. Please try again in the settings";
                                    prog.ShowError = true;
                                }
                                installing.PrimaryButtonText = "Ok";
                                installing.IsPrimaryButtonEnabled = true;
                                installing.DefaultButton = ContentDialogButton.Primary;



                            }
                        }
                        VM.IsSaving = false;
                        VM.IsPaused = true;
                        VM.IsVisible = false;
                        //AppRestartFailureReason result = await CoreApplication.RequestRestartAsync("");
                        //if (result == AppRestartFailureReason.NotInForeground ||
                        //    result == AppRestartFailureReason.RestartPending ||
                        //    result == AppRestartFailureReason.Other)
                        //{
                        //    AppViewModel.LOG("Restart failed");
                        //}
                    }
                    
                    
                }
                catch (Exception ex)
                {
                    VM.LOG("Error on Startup: "+ex.Message);
                }
            }
            else
                VM.Startup();
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

        /// <summary>
        /// Invoked when Navigation to a certain page fails
        /// </summary>
        /// <param name="sender">The Frame which failed navigation</param>
        /// <param name="e">Details about the navigation failure</param>
        private void OnNavigationFailed(object sender, NavigationFailedEventArgs e)
        {
            throw new Exception("Failed to load Page " + e.SourcePageType.FullName);
        }

        /// <summary>
        /// Invoked when application execution is being suspended.  Application state is saved
        /// without knowing whether the application will be terminated or resumed with the contents
        /// of memory still intact.
        /// </summary>
        /// <param name="sender">The source of the suspend request.</param>
        /// <param name="e">Details about the suspend request.</param>
        private void OnSuspending(object sender, SuspendingEventArgs e)
        {
            var deferral = e.SuspendingOperation.GetDeferral();
            //TODO: Save application state and stop any background activity
            deferral.Complete();
        }

        private void OnTaskCanceled(IBackgroundTaskInstance sender, BackgroundTaskCancellationReason reason)
        {
            //throw new NotImplementedException();
        }

        private void RestoreStatus(ApplicationExecutionState previousExecutionState)
        {
            // Do not repeat app initialization when the Window already has content,
            // just ensure that the window is active
            if (previousExecutionState == ApplicationExecutionState.Terminated)
            {
            }
        }
    }
}