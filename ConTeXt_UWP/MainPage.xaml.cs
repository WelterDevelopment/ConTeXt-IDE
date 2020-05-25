using System;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Core;
using Windows.Devices.Input;
using Windows.Foundation.Metadata;
using Windows.System;
using Windows.UI.Input;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Navigation;
using Windows.Storage;
using System.IO;
using Windows.UI.Xaml.Controls.Primitives;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace ConTeXt_UWP
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        public ViewModel currentViewModel = App.AppViewModel;
        public MainPage()
        {
            this.InitializeComponent();
            // Window.Current.SetTitleBar(AppTitleBar);
            
            //KeyboardAccelerator GoBack = new KeyboardAccelerator();
            //GoBack.Key = VirtualKey.GoBack;
            //GoBack.Invoked += BackInvoked;
            //KeyboardAccelerator AltLeft = new KeyboardAccelerator();
            //AltLeft.Key = VirtualKey.Left;
            //AltLeft.Invoked += BackInvoked;
            KeyboardAccelerator ButtonBack = new KeyboardAccelerator
            {
                Key = VirtualKey.XButton1
            };
            ButtonBack.Invoked += BackInvoked;
            KeyboardAccelerator ButtonForward = new KeyboardAccelerator
            {
                Key = VirtualKey.XButton2
            };
            ButtonForward.Invoked += ForwardInvoked;
            //this.KeyboardAccelerators.Add(GoBack);
            //this.KeyboardAccelerators.Add(AltLeft);
            //this.PointerPressed += MainPage_PointerPressed;
            // ALT routes here
            this.NavigationCacheMode = NavigationCacheMode.Enabled;
            Application.Current.Suspending += new SuspendingEventHandler(App_Suspending);

        }

        async void App_Suspending(object sender, SuspendingEventArgs e)
        {

        }

        private void MainPage_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            PointerPoint currentPoint = e.GetCurrentPoint(this);
            if (currentPoint.PointerDevice.PointerDeviceType == PointerDeviceType.Mouse)
            {
                PointerPointProperties pointerProperties = currentPoint.Properties;

                if (pointerProperties.IsXButton1Pressed && contentFrame.CanGoBack)
                {
                    On_BackRequested();
                }
                else if (pointerProperties.IsXButton2Pressed && contentFrame.CanGoForward)
                {
                    On_ForwardRequested();
                }
            }
            e.Handled = true;
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            try
            {
                App.AppViewModel.LOG("checking");
                var coreTitleBar = CoreApplication.GetCurrentView().TitleBar;
                coreTitleBar.ExtendViewIntoTitleBar = true;

                if (App.AppViewModel.Default.NavigationViewPaneMode == "Top")
                {
                    Window.Current.SetTitleBar(nvSample.PaneCustomContent as FrameworkElement);
                    nvSample.Header = null;
                }
                else
                    Window.Current.SetTitleBar(Header as FrameworkElement);
                coreTitleBar.ExtendViewIntoTitleBar = true;

                foreach (NavigationViewItemBase item in nvSample.MenuItems)
                {
                    if (item is NavigationViewItem && item.Tag.ToString() == "IDE")
                    {
                        nvSample.SelectedItem = item;

                        contentFrame.Navigate(typeof(Editor), null, new DrillInNavigationTransitionInfo());
                        App.AppViewModel.NVHeader = "Editor";
                        break;
                    }
                }
                App.AppViewModel.LOG("checking");
                if (ApiInformation.IsApiContractPresent("Windows.ApplicationModel.FullTrustAppContract", 1))
                {
                    App.AppViewModel.LOG("launching app");
                    await FullTrustProcessLauncher.LaunchFullTrustProcessForCurrentAppAsync("Parameters");
                    App.AppViewModel.LOG("launched");
                    //Thread.Sleep(5000);
                }
                nvSample.IsBackEnabled = contentFrame.CanGoBack;
            }
            catch (Exception ex)
            {
                App.AppViewModel.LOG(ex.Message);
            }

        }


        // Handles system-level BackRequested events and page-level back button Click events
        private bool On_BackRequested()
        {
            if (contentFrame.CanGoBack)
            {
                contentFrame.GoBack();
                Updateback();
                return true;
            }
            return false;

        }
        private bool On_ForwardRequested()
        {
            if (contentFrame.CanGoForward)
            {
                contentFrame.GoForward();
                Updateback();
                return true;
            }
            return false;

        }

        private void BackInvoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
        {
            On_BackRequested();
            args.Handled = true;
        }
        private void ForwardInvoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
        {
            On_ForwardRequested();
            args.Handled = true;
        }

        private void NvSample_BackRequested(NavigationView sender, NavigationViewBackRequestedEventArgs args)
        {
            On_BackRequested();

        }
        private void NvSample_BackRequested(Microsoft.UI.Xaml.Controls.NavigationView sender, Microsoft.UI.Xaml.Controls.NavigationViewBackRequestedEventArgs args)
        {
            On_BackRequested();

        }

        PageStackEntry Editor = new PageStackEntry(typeof(Editor), null, new DrillInNavigationTransitionInfo());
 private async void NvSample_ItemInvoked(NavigationView sender, NavigationViewItemInvokedEventArgs args)
        {
            
            if (args.IsSettingsInvoked)
                contentFrame.Navigate(typeof(SettingsPage), null, new DrillInNavigationTransitionInfo());
         
            else
                try
                {
                    switch (args.InvokedItemContainer.Tag)
                    {
                        case "IDE": contentFrame.Navigate(typeof(Editor), null, new DrillInNavigationTransitionInfo()); App.AppViewModel.NVHeader = "Editor"; break;
                        case "Settings": contentFrame.Navigate(typeof(SettingsPage), null, new DrillInNavigationTransitionInfo()); App.AppViewModel.NVHeader = "Settings"; break;
                        case "Projects": contentFrame.Navigate(typeof(Projects), null, new DrillInNavigationTransitionInfo()); App.AppViewModel.NVHeader = "Projects"; break;
                        case "About": contentFrame.Navigate(typeof(About), null, new DrillInNavigationTransitionInfo()); App.AppViewModel.NVHeader = "About"; break;
                        default: Console.WriteLine("Not a valid Page Type"); break;
                    }
                }
                catch (Exception e)
                {
                    Debug.WriteLine(e.Message);
                }
            Updateback();
        }
        private async void NvSample_ItemInvoked(Microsoft.UI.Xaml.Controls.NavigationView sender, Microsoft.UI.Xaml.Controls.NavigationViewItemInvokedEventArgs args)
        {
            
            if (args.IsSettingsInvoked)
                contentFrame.Navigate(typeof(SettingsPage), null, new DrillInNavigationTransitionInfo());
         
            else
                try
                {
                    switch (args.InvokedItemContainer.Tag)
                    {
                        case "IDE": contentFrame.Navigate(typeof(Editor), null, new DrillInNavigationTransitionInfo()); App.AppViewModel.NVHeader = "Editor"; break;
                        case "Settings": contentFrame.Navigate(typeof(SettingsPage), null, new DrillInNavigationTransitionInfo()); App.AppViewModel.NVHeader = "Settings"; break;
                        case "Projects": contentFrame.Navigate(typeof(Projects), null, new DrillInNavigationTransitionInfo()); App.AppViewModel.NVHeader = "Projects"; break;
                        case "About": contentFrame.Navigate(typeof(About), null, new DrillInNavigationTransitionInfo()); App.AppViewModel.NVHeader = "About"; break;
                        default: Console.WriteLine("Not a valid Page Type"); break;
                    }
                }
                catch (Exception e)
                {
                    Debug.WriteLine(e.Message);
                }
            Updateback();
        }
        private void Navigate(PageStackEntry pse, string title)
        {
            //if (contentFrame.BackStack.Contains(Editor))
            //{
            //    contentFrame.BackStack[1].
            //}
            //contentFrame.Navigate(page, param, new DrillInNavigationTransitionInfo()); 
            //App.AppViewModel.NVHeader = title;
        }
        private void NvSample_Loaded(object sender, RoutedEventArgs e)
        {
            var goBack = new KeyboardAccelerator { Key = VirtualKey.GoBack };
            goBack.Invoked += (f, g) =>
            {
                if (contentFrame.CanGoBack)
                    contentFrame.GoBack();
            };
            this.KeyboardAccelerators.Add(goBack);
        }



        private void About_Tapped(object sender, TappedRoutedEventArgs e)
        {
            nvSample.SelectedItem = (sender as NavigationViewItem);
            contentFrame.Navigate(typeof(About), null, new DrillInNavigationTransitionInfo()); App.AppViewModel.NVHeader = "Projects";
            App.AppViewModel.NVHeader = "About";
        }

        private void ContentFrame_Navigated(object sender, NavigationEventArgs e)
        {
            //nvSample.IsBackEnabled = contentFrame.CanGoBack;
            //if (contentFrame.SourcePageType == typeof(SettingsPage))
            //{
            //    if (Settings.Default.NavigationViewPaneMode != "Top")
            //        nvSample.SelectedItem = (NavigationViewItem)nvSample.SettingsItem;
            //    else
            //    {
            //        nvSample.SelectedItem = nvSample.MenuItems.OfType<NavigationViewItem>().First(n => n.Tag.Equals("Settings"));
            //    }
            //}
            //else if (contentFrame.SourcePageType != null)
            //{

            //}
            if (e.NavigationMode == NavigationMode.Back || e.NavigationMode == NavigationMode.Forward)
            {
                switch (e.Content.GetType().ToString())
                {
                    case "Editor": ; App.AppViewModel.NVHeader = "Editor"; break;
                    case "SettingsPage": contentFrame.Navigate(typeof(SettingsPage), null, new DrillInNavigationTransitionInfo()); App.AppViewModel.NVHeader = "Settings"; break;
                    case "Projects": contentFrame.Navigate(typeof(Projects), null, new DrillInNavigationTransitionInfo()); App.AppViewModel.NVHeader = "Projects"; break;
                    case "About": contentFrame.Navigate(typeof(About), null, new DrillInNavigationTransitionInfo()); App.AppViewModel.NVHeader = "About"; break;
                    default: Console.WriteLine("Not a valid Page Type"); break;
                }
            }

            Updateback();
        }

        private void Updateback()
        {
            nvSample.IsBackEnabled = contentFrame.CanGoBack;

            //currentViewModel.NVHeader = contentFrame.Content.GetType().Name;
        }

        private void ContentFrame_Loaded(object sender, RoutedEventArgs e)
        {

        }

        private void ClearLog_Click(object sender, RoutedEventArgs e)
        {

        }


        private async Task<string> ExecuteCommandLineString(string CommandString)
        {
            if (ApiInformation.IsApiContractPresent(
      "Windows.ApplicationModel.FullTrustAppContract", 1, 0))
            {
                await
                  FullTrustProcessLauncher.LaunchFullTrustProcessForCurrentAppAsync("User");
            }
            Process process = new Process();
            process.StartInfo.FileName = "context";
            process.StartInfo.Arguments = "c:\\context\\hello.tex";
            process.StartInfo.UseShellExecute = false;
            process.Start();
            process.WaitForExit();
            if (process.ExitCode == 0)
            {
                Console.WriteLine("Command was successfully executed.");
            }
            else
            {
                Console.WriteLine("An error occurred.");
            }

            const string CommandLineProcesserExe = "c:\\windows\\system32\\cmd.exe";
            const uint CommandStringResponseBufferSize = 8192;
            string currentDirectory = "C:\\";

            StringBuilder textOutput = new StringBuilder((int)CommandStringResponseBufferSize);
            uint bytesLoaded = 0;

            if (string.IsNullOrWhiteSpace(CommandString))
                return ("");

            var commandLineText = CommandString.Trim();

            var standardOutput = new Windows.Storage.Streams.InMemoryRandomAccessStream();
            var standardError = new Windows.Storage.Streams.InMemoryRandomAccessStream();
            var options = new Windows.System.ProcessLauncherOptions
            {
                StandardOutput = standardOutput,
                StandardError = standardError
            };

            try
            {
                var args = "/C \"cd \"" + currentDirectory + "\" & " + commandLineText + "\"";
                Debug.WriteLine("1");
                var result = await Windows.System.ProcessLauncher.RunToCompletionAsync(CommandLineProcesserExe, args, options);
                Debug.WriteLine("2");
                //First write std out
                using (var outStreamRedirect = standardOutput.GetInputStreamAt(0))
                {
                    using (var dataReader = new Windows.Storage.Streams.DataReader(outStreamRedirect))
                    {
                        while ((bytesLoaded = await dataReader.LoadAsync(CommandStringResponseBufferSize)) > 0)
                            textOutput.Append(dataReader.ReadString(bytesLoaded));

                        new System.Threading.ManualResetEvent(false).WaitOne(10);
                        if ((bytesLoaded = await dataReader.LoadAsync(CommandStringResponseBufferSize)) > 0)
                            textOutput.Append(dataReader.ReadString(bytesLoaded));
                    }
                }

                //Then write std err
                using (var errStreamRedirect = standardError.GetInputStreamAt(0))
                {
                    using (var dataReader = new Windows.Storage.Streams.DataReader(errStreamRedirect))
                    {
                        while ((bytesLoaded = await dataReader.LoadAsync(CommandStringResponseBufferSize)) > 0)
                            textOutput.Append(dataReader.ReadString(bytesLoaded));

                        new System.Threading.ManualResetEvent(false).WaitOne(10);
                        if ((bytesLoaded = await dataReader.LoadAsync(CommandStringResponseBufferSize)) > 0)
                            textOutput.Append(dataReader.ReadString(bytesLoaded));
                    }
                }

                return (textOutput.ToString());
            }
            catch (UnauthorizedAccessException uex)
            {
                return ("ERROR - " + uex.Message + "\n\nCmdNotEnabled");
            }
            catch (Exception ex)
            {
                return ("ERROR - " + ex.Message + "\n");
            }
        }

        private  void NavigationTree_ItemInvoked(TreeView sender, TreeViewItemInvokedEventArgs args)
        {
            var fileitem = (FileItem)args.InvokedItem;
            if (fileitem.Type == FileItem.ExplorerItemType.File)
            {
                App.AppViewModel.OpenFile(fileitem);
            }
            args.Handled = true;
        }

        private void SetRoot_Click(object sender, RoutedEventArgs e)
        {
            var ei = (FileItem)(sender as FrameworkElement).DataContext;
            ei.IsRoot = true;
            if (App.AppViewModel.CurrentProject.RootFile != null)
            App.AppViewModel.CurrentProject.RootFile.IsRoot = false;
            App.AppViewModel.CurrentProject.RootFile = ei;
        }

        private async void Delete_Click(object sender, RoutedEventArgs e)
        {
            var fi = (FileItem)(sender as FrameworkElement).DataContext;
            if (App.AppViewModel.CurrentProject.Directory.Contains(fi))
            {
                App.AppViewModel.CurrentProject.Directory.Remove(fi);
            }

            await fi.File.DeleteAsync();
        }

        private async void Rename_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var fi = (FileItem)(sender as FrameworkElement).DataContext;
                fi.FileName = await rename(fi.Type, fi.FileName);
                await fi.File.RenameAsync(fi.FileName, NameCollisionOption.GenerateUniqueName);
            }
            catch (Exception ex)
            {
                App.AppViewModel.LOG(ex.Message);
            }
        }

        private async Task<string> rename (FileItem.ExplorerItemType type, string startstring)
        {
            string newstring = startstring;

            var cd = new ContentDialog() { Title = "Rename " + type, PrimaryButtonText = "rename", CloseButtonText = "cancel"};
            TextBox tb = new TextBox() { Text = startstring };
            cd.Content = tb;
            cd.PrimaryButtonClick += (a,b) => {
                newstring = tb.Text;
            };
            await cd.ShowAsync();
            return newstring;
        }

        private async void AddFile_Click(object sender, RoutedEventArgs e)
        {

            try
            {
                string name = "bla.tex";
                var folder = App.AppViewModel.CurrentProject.Folder;
                if (await folder.TryGetItemAsync(name) == null)
                {
                    var file = await folder.CreateFileAsync(name);
                    var fi = new FileItem(file) { Type = FileItem.ExplorerItemType.File, FileLanguage = Path.GetExtension(file.Path) };
                    App.AppViewModel.CurrentProject.Directory.Add(fi);
                }
                else
                    App.AppViewModel.LOG(name + " does already exist.");
            }
            catch (Exception ex)
            {
                App.AppViewModel.LOG(ex.Message);
            }
        }

        private async void AddFolder_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string name = "folder";
                var folder = App.AppViewModel.CurrentProject.Folder;
                if (await folder.TryGetItemAsync(name) == null)
                {
                    var subfolder = await folder.CreateFolderAsync(name);
                    var fi = new FileItem(subfolder) { Type = FileItem.ExplorerItemType.Folder };
                    App.AppViewModel.CurrentProject.Directory.Add(fi);
                }
                else
                    App.AppViewModel.LOG(name + " does already exist.");
            }
            catch (Exception ex)
            {
                App.AppViewModel.LOG(ex.Message);
            }
        }

        private void Tree_Tapped(object sender, TappedRoutedEventArgs e)
        {
            FlyoutBase.ShowAttachedFlyout((FrameworkElement)sender);
        }

       
    }
}
