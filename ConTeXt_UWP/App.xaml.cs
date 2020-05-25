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

        public static ViewModel AppViewModel
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
            AppViewModel.LOG("activated");
            base.OnBackgroundActivated(args);
            AppViewModel.LOG(args.TaskInstance.TriggerDetails.GetType().ToString());
            if (args.TaskInstance.TriggerDetails is AppServiceTriggerDetails details)
            {
                AppViewModel.LOG(Package.Current.Id.FamilyName);
                // only accept connections from callers in the same package
                if (details.CallerPackageFamilyName == Package.Current.Id.FamilyName)
                {
                    AppViewModel.LOG("activated");
                    // connection established from the fulltrust process
                    AppViewModel.AppServiceDeferral = args.TaskInstance.GetDeferral();
                    args.TaskInstance.Canceled += OnTaskCanceled;

                    //coreDispatcher = Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher;
                    AppViewModel.appServiceConnection = details.AppServiceConnection;

                    AppViewModel.appServiceConnection.RequestReceived += AppServiceConnection_RequestReceived;


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
                AppViewModel.FileActivatedEvents.Add(args);
                if (!CreateRootFrame().Navigate(typeof(MainPage)))
                {
                    await new MessageDialog("miep").ShowAsync();
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
                    AppViewModel.Default.PackageID = Package.Current.Id.FamilyName;
                }
            }
            else
            {
                foreach (StorageFile file in args.Files)
                {
                    var fileitem = new FileItem(file) { };
                    AppViewModel.OpenFile(fileitem);
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
            AppViewModel.Default.PackageID = Package.Current.Id.FamilyName;
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
            
            if (AppViewModel.Default.ContextDistributionPath == "")
            {
                try
                {
                    AppViewModel.IsNotSaving = false;
                    var cd = new ContentDialog();
                    var sp = new StackPanel();
                    StorageFolder localFolder = ApplicationData.Current.LocalFolder;
                    var installpathtb = new TextBox() { Header = "Install Path (changing this is experimental)", Text = localFolder.Path };
                    var downloadlinktb = new TextBox() { Header = "Download link (only change if PRAGMA ADE changed the URL)", Text = AppViewModel.Default.ContextDownloadLink };
                    sp.Children.Add(installpathtb);
                    sp.Children.Add(downloadlinktb);
                    cd.Title = "First Start: Install the ConTeXt (LuaMetaTeX) Distribution";
                    cd.Content = sp;
                    cd.PrimaryButtonText = "Install";
                    cd.CloseButtonText = "Skip (Setup in the Settings!)";
                    cd.PrimaryButtonClick += async (a, b) =>
                    {
                        AppViewModel.Default.ContextDistributionPath = installpathtb.Text;
                        AppViewModel.Default.ContextDownloadLink = downloadlinktb.Text;
                        ValueSet request = new ValueSet();
                        request.Add("command", "install");
                        AppServiceResponse response = await AppViewModel.appServiceConnection.SendMessageAsync(request);
                        //AppViewModel.LOG(response.Status.ToString() + " ... " + response.Message.FirstOrDefault().Key.ToString() + " Key Val " + response.Message.FirstOrDefault().Value.ToString());
                        // display the response key/value pairs
                        foreach (string key in response.Message.Keys)
                        {
                            if (key == "response")
                            {
                                if ((bool)response.Message[key])
                                    AppViewModel.LOG("ConTeXt distribution installed.");
                                else
                                    AppViewModel.LOG("Installation error");

                            }
                        }
                        //AppRestartFailureReason result = await CoreApplication.RequestRestartAsync("");
                        //if (result == AppRestartFailureReason.NotInForeground ||
                        //    result == AppRestartFailureReason.RestartPending ||
                        //    result == AppRestartFailureReason.Other)
                        //{
                        //    AppViewModel.LOG("Restart failed");
                        //}
                    };
                    await cd.ShowAsync();
                    AppViewModel.IsNotSaving = true;
                }
                catch (Exception ex)
                {
                    AppViewModel.LOG(ex.Message);
                }
            }
            else
                AppViewModel.Startup();
        }

        private void App_BackRequested(object sender, BackRequestedEventArgs e)
        {
            //throw new NotImplementedException();
        }

        private void AppServiceConnection_RequestReceived(AppServiceConnection sender, AppServiceRequestReceivedEventArgs args)
        {
            var appServiceDeferral = args.GetDeferral();
            AppViewModel.LOG("Background Service Message: " + args.Request.Message.Values.FirstOrDefault() as string);
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