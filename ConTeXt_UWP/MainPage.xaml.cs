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
using Windows.ApplicationModel.DataTransfer;
using System.Collections.Generic;
using System.Linq;
using System.Collections.ObjectModel;
using Windows.UI.ViewManagement;
using Windows.UI;
using Windows.Foundation.Collections;
using Windows.ApplicationModel.AppService;
using Windows.Storage.AccessCache;
using Windows.UI.Core.Preview;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace ConTeXt_UWP
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        public ViewModel currentViewModel = App.VM;
        public static List<FileItem> DraggedItems { get; set; }
        public MainPage()
        {
            DraggedItems = new List<FileItem>();
            this.InitializeComponent();
            // Window.Current.SetTitleBar(AppTitleBar);

            //KeyboardAccelerator GoBack = new KeyboardAccelerator();
            //GoBack.Key = VirtualKey.GoBack;
            //GoBack.Invoked += BackInvoked;
            //KeyboardAccelerator AltLeft = new KeyboardAccelerator();
            //AltLeft.Key = VirtualKey.Left;
            //AltLeft.Invoked += BackInvoked;
            Version.Text = string.Format("Version: {0}.{1}.{2}.{3}",
                  Package.Current.Id.Version.Major,
                  Package.Current.Id.Version.Minor,
                  Package.Current.Id.Version.Build,
                  Package.Current.Id.Version.Revision);
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
            SystemNavigationManagerPreview.GetForCurrentView().CloseRequested += MainPage_CloseRequested; ;


        }

        private async void MainPage_CloseRequested(object sender, SystemNavigationCloseRequestedPreviewEventArgs e)
        {
            var deferral = e.GetDeferral();
            bool unsaved = App.VM.FileItems.Any(x => x.IsChanged);
            if (unsaved)
            {
                var save = new ContentDialog() { Title = "Do you want to save the open unsaved files before closing?", PrimaryButtonText = "Yes", SecondaryButtonText = "No", DefaultButton = ContentDialogButton.Primary };
                if (await save.ShowAsync() == ContentDialogResult.Primary)
                {
                    await App.VM.UWPSaveAll();
                }
            }
            deferral.Complete(); 
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
                var coreTitleBar = CoreApplication.GetCurrentView().TitleBar;
                coreTitleBar.ExtendViewIntoTitleBar = true;

                if (App.VM.Default.NavigationViewPaneMode == "Top")
                {
                    Window.Current.SetTitleBar(nvSample.PaneCustomContent as FrameworkElement);
                    nvSample.Header = null;
                }
                else
                    Window.Current.SetTitleBar(Header as FrameworkElement);
                coreTitleBar.ExtendViewIntoTitleBar = true;
                contentFrame.Navigate(typeof(Editor), null, new DrillInNavigationTransitionInfo());
                //foreach (NavigationViewItemBase item in nvSample.MenuItems)
                //{
                //    if (App.VM.CurrentProject.Folder != null | App.VM.FileActivatedEvents.Count > 0)
                //    {
                //        if (item is NavigationViewItem && item.Tag.ToString() == "IDE")
                //        {
                //            nvSample.SelectedItem = item;

                //            contentFrame.Navigate(typeof(Editor), null, new DrillInNavigationTransitionInfo());
                //            break;
                //        }
                //    }
                //    else
                //    {
                //        if (item is NavigationViewItem && item.Tag.ToString() == "Projects")
                //        {
                //            nvSample.SelectedItem = item;

                //            contentFrame.Navigate(typeof(Projects), null, new DrillInNavigationTransitionInfo());
                //            break;
                //        }
                //    }
                //}
                if (ApiInformation.IsApiContractPresent("Windows.ApplicationModel.FullTrustAppContract", 1))
                {
                    await FullTrustProcessLauncher.LaunchFullTrustProcessForCurrentAppAsync("Parameters");
                }
                nvSample.IsBackEnabled = contentFrame.CanGoBack;
            }
            catch (Exception ex)
            {
                App.VM.LOG("Error on Navigation: " + ex.Message);
            }

        }

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

        private async void NvSample_ItemInvoked(NavigationView sender, NavigationViewItemInvokedEventArgs args)
        {
            nvSample.SelectedItem = null;
            if (args.IsSettingsInvoked)
                SettingsPane.IsPaneOpen = !SettingsPane.IsPaneOpen;
            //contentFrame.Navigate(typeof(SettingsPage), null, new DrillInNavigationTransitionInfo());

            else
                try
                {
                    switch (args.InvokedItemContainer.Tag)
                    {
                       // case "IDE": contentFrame.Navigate(typeof(Editor), null, new DrillInNavigationTransitionInfo()); break;
                        case "Settings": SettingsPane.IsPaneOpen = !SettingsPane.IsPaneOpen; break;
                       // case "Projects": contentFrame.Navigate(typeof(Projects), null, new DrillInNavigationTransitionInfo()); break;
                       // case "About": contentFrame.Navigate(typeof(About), null, new DrillInNavigationTransitionInfo()); break;
                        default: break;
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
                        case "IDE": contentFrame.Navigate(typeof(Editor), null, new DrillInNavigationTransitionInfo()); break;
                        case "Settings": SettingsPane.IsPaneOpen = !SettingsPane.IsPaneOpen; break;
                        case "Projects": contentFrame.Navigate(typeof(Projects), null, new DrillInNavigationTransitionInfo()); break;
                        case "About": contentFrame.Navigate(typeof(About), null, new DrillInNavigationTransitionInfo()); break;
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
                    case "Editor": break;
                    case "SettingsPage": contentFrame.Navigate(typeof(SettingsPage), null, new DrillInNavigationTransitionInfo()); break;
                    case "Projects": contentFrame.Navigate(typeof(Projects), null, new DrillInNavigationTransitionInfo()); break;
                    case "About": contentFrame.Navigate(typeof(About), null, new DrillInNavigationTransitionInfo()); break;
                    default: Console.WriteLine("Not a valid Page Type"); break;
                }
            }

            Updateback();
        }

        private void Updateback()
        {
            nvSample.IsBackEnabled = contentFrame.CanGoBack;

        }

        private void ContentFrame_Loaded(object sender, RoutedEventArgs e)
        {

        }

        private void NavigationTree_ItemInvoked(TreeView sender, TreeViewItemInvokedEventArgs args)
        {
            var fileitem = (FileItem)args.InvokedItem;
            if (fileitem.Type == FileItem.ExplorerItemType.File)
            {
                App.VM.OpenFile(fileitem);
            }
            args.Handled = true;
        }

        private void SetRoot_Click(object sender, RoutedEventArgs e)
        {
            var ei = (FileItem)(sender as FrameworkElement).DataContext;
            ei.IsRoot = true;
            App.VM.CurrentProject.RootFile = ei.FileName;
            //App.VM.UpdateMRUEntry(App.VM.CurrentProject);
        }

        private async void Delete_Click(object sender, RoutedEventArgs e)
        {
            var fi = (FileItem)(sender as FrameworkElement).DataContext;
           
                App.VM.CurrentProject.Directory.Remove(fi);
           
                App.VM.FileItems.Remove(fi);
            await fi.File.DeleteAsync();
        }

        private async void Rename_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var fi = (sender as FrameworkElement).DataContext as FileItem;
                string newname = await rename(fi.Type, fi.FileName);

                await fi.File.RenameAsync(newname, NameCollisionOption.GenerateUniqueName);
                fi.FileName = newname;
            }
            catch (Exception ex)
            {
                App.VM.LOG(ex.Message);
            }
        }

        private async Task<string> rename(FileItem.ExplorerItemType type, string startstring)
        {

            var cd = new ContentDialog() { Title = "Rename " + type.ToString().ToLower(), PrimaryButtonText = "rename", DefaultButton = ContentDialogButton.Primary };
            TextBox tb = new TextBox() { Text = startstring };
            cd.Content = tb;
            var res = await cd.ShowAsync();
            if (res == ContentDialogResult.Primary)
            {
                App.VM.LOG($"Renaming {type.ToString().ToLower()} {startstring} to {tb.Text}");
                return tb.Text;
            }
            else
                return startstring;
        }

        private async void AddFile_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string name = "file.tex";
                var cd = new ContentDialog() { Title = "Set file name", PrimaryButtonText = "ok", CloseButtonText = "cancel", DefaultButton = ContentDialogButton.Primary };
                TextBox tb = new TextBox() { Text = name };
                cd.Content = tb;

                if (await cd.ShowAsync() == ContentDialogResult.Primary)
                {
                    var folder = App.VM.CurrentProject.Folder;
                    if (await folder.TryGetItemAsync(tb.Text) == null)
                    {
                        var file = await folder.CreateFileAsync(tb.Text);
                        var fi = new FileItem(file) { Type = FileItem.ExplorerItemType.File, FileLanguage = FileItem.GetFileLanguage(file.FileType) };
                        App.VM.CurrentProject.Directory.Add(fi);
                    }
                    else
                        App.VM.LOG(name + " does already exist.");
                }
            }
            catch (Exception ex)
            {
                App.VM.LOG(ex.Message);
            }
        }

        private async void AddFolder_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string name = "";
                var cd = new ContentDialog() { Title = "Set folder name", PrimaryButtonText = "ok", CloseButtonText = "cancel", DefaultButton = ContentDialogButton.Primary };
                TextBox tb = new TextBox() { Text = name };
                cd.Content = tb;
                cd.PrimaryButtonClick += (a, b) =>
                {
                    name = tb.Text;
                };
                await cd.ShowAsync();
                var folder = App.VM.CurrentProject.Folder;
                if (await folder.TryGetItemAsync(name) == null)
                {
                    var subfolder = await folder.CreateFolderAsync(name);
                    var fi = new FileItem(subfolder) { Type = FileItem.ExplorerItemType.Folder };
                    App.VM.CurrentProject.Directory.Insert(0, fi);
                }
                else
                    App.VM.LOG(name + " does already exist.");
            }
            catch (Exception ex)
            {
                App.VM.LOG(ex.Message);
            }
        }

        private void Tree_Tapped(object sender, TappedRoutedEventArgs e)
        {
            FlyoutBase.ShowAttachedFlyout((FrameworkElement)sender);
        }

        private void TreeView_DropCompleted(UIElement sender, DropCompletedEventArgs args)
        {
        }

        private void TreeView_Drop(object sender, DragEventArgs e)
        {
            e.AcceptedOperation = Windows.ApplicationModel.DataTransfer.DataPackageOperation.Move;
        }

        private async void NewFile_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var fileitem = (sender as FrameworkElement).DataContext as FileItem;
                string name = "newfile.tex";
                var cd = new ContentDialog() { Title = "Set file name", PrimaryButtonText = "Ok", CloseButtonText = "Cancel", DefaultButton = ContentDialogButton.Primary };
                TextBox tb = new TextBox() { Text = name };
                cd.Content = tb;

                var res = await cd.ShowAsync();
                if (res == ContentDialogResult.Primary)
                {
                    name = tb.Text;
                    var folder = fileitem.File as StorageFolder;
                    if (await folder.TryGetItemAsync(name) == null)
                    {
                        var subfile = await folder.CreateFileAsync(name);
                        var fi = new FileItem(subfile) { Type = FileItem.ExplorerItemType.File };
                        fileitem.Children.Add(fi);
                    }
                    else
                        App.VM.LOG(name + " does already exist.");
                }
            }
            catch (Exception ex)
            {
                App.VM.LOG(ex.Message);
            }
        }

        //private void TreeView_DragItemsStarting(TreeView sender, TreeViewDragItemsStartingEventArgs args)
        //{
        //    App.VM.LOG("TreeView_DragItemsStarting" + (args.Items[0] as FileItem).FileName);
        //    args.Data.RequestedOperation = DataPackageOperation.Move;
        //}

        //private void TreeView_DragItemsCompleted(TreeView sender, TreeViewDragItemsCompletedEventArgs args)
        //{
        //    App.VM.LOG("TreeView_DragItemsCompleted" + (args.Items[0] as FileItem).FileName + "\n" + string.Join(", ", (new List<FileItem>(App.VM.CurrentProject.Directory)).Select(x => x.FileName).ToArray()));
        //}

        //private void TreeViewItem_Drop(object sender, DragEventArgs e)
        //{
        //    e.AcceptedOperation = DataPackageOperation.Move;
        //    var target = (sender as Microsoft.UI.Xaml.Controls.TreeViewItem).DataContext as FileItem;
        //    var source = (e.OriginalSource as Microsoft.UI.Xaml.Controls.TreeViewItem).DataContext as FileItem;
        //    if (source.File is StorageFolder)
        //    {
        //        Tree.CanReorderItems = false;
        //        e.AcceptedOperation = DataPackageOperation.None;
        //    }
        //    else
        //    {
        //        if (target.File is StorageFile storageFile)
        //        {
        //            Tree.CanReorderItems = false;
        //            e.AcceptedOperation = DataPackageOperation.None;
        //        }
        //        else
        //        {
        //            Tree.CanReorderItems = true;
        //            e.AcceptedOperation = DataPackageOperation.Move;
        //        }
        //    }
        //}

        private void NavigationTree_ItemInvoked(Microsoft.UI.Xaml.Controls.TreeView sender, Microsoft.UI.Xaml.Controls.TreeViewItemInvokedEventArgs args)
        {

            var fileitem = (FileItem)args.InvokedItem;
            if (fileitem.Type == FileItem.ExplorerItemType.File)
            {
                App.VM.OpenFile(fileitem);
            }
            args.Handled = true;
        }

        private void TreeView_DragItemsStarting(Microsoft.UI.Xaml.Controls.TreeView sender, Microsoft.UI.Xaml.Controls.TreeViewDragItemsStartingEventArgs args)
        {
            try
            {
                foreach (FileItem item in args.Items)
                {
                    DraggedItems.Add(item);
                }
            }
            catch (Exception ex)
            {
                App.VM.LOG("OnDrop" + ex.Message);
            }
        }

        private async void TreeView_DragItemsCompleted(Microsoft.UI.Xaml.Controls.TreeView sender, Microsoft.UI.Xaml.Controls.TreeViewDragItemsCompletedEventArgs args)
        {
            try
            {
                if (args.DropResult != DataPackageOperation.None)
                    foreach (FileItem item in args.Items)
                    {

                        if (args.NewParentItem != null)
                        {
                            var fi = args.NewParentItem as FileItem;
                            if (fi.File.Path == item.FileFolder)
                            {
                                App.VM.CurrentProject.Directory.Add(item);
                                await Task.Delay(100);
                                App.VM.CurrentProject = new Project(App.VM.CurrentProject.Name, App.VM.CurrentProject.Folder, App.VM.GenerateTreeView(App.VM.CurrentProject.Folder, App.VM.CurrentProject.RootFile));
                            }
                        }
                    }
                DraggedItems.Clear();
            }
            catch (Exception ex)
            {
                App.VM.LOG("TreeView_DragItemsCompleted" + ex.Message);
            }
        }
        private void Tree_Drop(object sender, DragEventArgs e)
        {
            e.Handled = true;
        }

        private async void NewFolder_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var fileitem = (sender as FrameworkElement).DataContext as FileItem;
                string name = "";
                var cd = new ContentDialog() { Title = "Set folder name", PrimaryButtonText = "ok", CloseButtonText = "cancel", DefaultButton = ContentDialogButton.Primary };
                TextBox tb = new TextBox() { Text = name };
                cd.Content = tb;

                var res = await cd.ShowAsync();
                if (res == ContentDialogResult.Primary)
                {
                    name = tb.Text;
                    var folder = fileitem.File as StorageFolder;
                    if (await folder.TryGetItemAsync(name) == null)
                    {
                        var subfolder = await folder.CreateFolderAsync(name);
                        var fi = new FileItem(subfolder) { Type = FileItem.ExplorerItemType.Folder };
                        fileitem.Children.Insert(0, fi);
                    }
                    else
                        App.VM.LOG(name + " does already exist.");
                }
            }
            catch (Exception ex)
            {
                App.VM.LOG(ex.Message);
            }
        }

        private async void Compile_Click(object sender, RoutedEventArgs e)
        {
            FileItem fi = (sender as FrameworkElement).DataContext as FileItem;
            await App.VM.UWPSaveAll();
            await Editor.CurrentEditor.Compile(false, fi);
        }

        private void PaneControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if ((string)(sender as ComboBox).SelectedItem == "Top")
            {
                Window.Current.SetTitleBar(nvSample.PaneCustomContent as FrameworkElement);
                //((Window.Current.Content as Frame).Content as MainPage).nvSample.Header = null;
            }

            else
            {
                Window.Current.SetTitleBar(Header);
                //((Window.Current.Content as Frame).Content as MainPage).nvSample.SetBinding(NavigationView.HeaderProperty, new Binding() { Path = new PropertyPath("NVHeader") });
            }
        }

        private async void Update_Click(object sender, RoutedEventArgs e)
        {
            App.VM.IsSaving = true;


            var installing = new ContentDialog() { Title = "Please wait while updating. This can take up to 5 minutes." };
            var prog = new Microsoft.UI.Xaml.Controls.ProgressBar() { IsIndeterminate = true };
            installing.Content = prog;
            installing.ShowAsync();
            ValueSet request = new ValueSet();
            request.Add("command", "update");
            AppServiceResponse response = await App.VM.appServiceConnection.SendMessageAsync(request);
            foreach (string key in response.Message.Keys)
            {
                if (key == "response")
                {
                    if ((bool)response.Message[key])
                    {
                        App.VM.LOG("ConTeXt distribution updated.");
                        installing.Title = "ConTeXt distribution updated!";
                        prog.ShowPaused = true;

                    }
                    else
                    {
                        App.VM.LOG("Update error");
                        installing.Title = "Error. Please try again later";
                        prog.ShowError = true;
                    }
                    installing.PrimaryButtonText = "Ok";
                    installing.IsPrimaryButtonEnabled = true;
                    installing.DefaultButton = ContentDialogButton.Primary;
                }
            }
            App.VM.IsSaving = false;
        }

        private async void Install_Click(object sender, RoutedEventArgs e)
        {
            App.VM.IsSaving = true;
            try
            {
                var cd = new ContentDialog();
                var sp = new StackPanel();
                StorageFolder localFolder = ApplicationData.Current.LocalFolder;
                var installpathtb = new TextBox() { Header = "Install Path (changing this is experimental)", Text = localFolder.Path };
                var downloadlinktb = new TextBox() { Header = "Download link (only change if PRAGMA ADE changed the URL)", Text = App.VM.Default.ContextDownloadLink };
                sp.Children.Add(installpathtb);
                sp.Children.Add(downloadlinktb);
                cd.Title = "First Start: Install the ConTeXt (LuaMetaTeX) Distribution";
                cd.Content = sp;
                cd.PrimaryButtonText = "Install";
                cd.CloseButtonText = "Skip (Setup in the Settings!)";
                cd.PrimaryButtonClick += async (a, b) =>
                {
                    App.VM.Default.ContextDistributionPath = installpathtb.Text;
                    App.VM.Default.ContextDownloadLink = downloadlinktb.Text;
                    ValueSet request = new ValueSet();
                    request.Add("command", "install");
                    AppServiceResponse response = await App.VM.appServiceConnection.SendMessageAsync(request);
                    //AppViewModel.LOG(response.Status.ToString() + " ... " + response.Message.FirstOrDefault().Key.ToString() + " Key Val " + response.Message.FirstOrDefault().Value.ToString());
                    // display the response key/value pairs
                    foreach (string key in response.Message.Keys)
                    {
                        if (key == "response")
                        {
                            if ((bool)response.Message[key])
                                App.VM.LOG("ConTeXt distribution installed.");
                            else
                                App.VM.LOG("Installation error");

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

            }
            catch (Exception ex)
            {
                App.VM.LOG(ex.Message);
            }
            App.VM.IsSaving = false;
        }

        private void ThemeControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ApplicationViewTitleBar titleBar = ApplicationView.GetForCurrentView().TitleBar;
            var DefaultTheme = new Windows.UI.ViewManagement.UISettings();
            var lightbrush = DefaultTheme.GetColorValue(Windows.UI.ViewManagement.UIColorType.Foreground);
            if (App.VM.Default.Theme == "Light")
            {
                lightbrush = Colors.Black;
            }
            else if (App.VM.Default.Theme == "Dark")
            {
                lightbrush = Colors.White;
            }

            titleBar.ButtonBackgroundColor = Colors.Transparent;
            titleBar.ButtonInactiveBackgroundColor = Colors.Transparent;
            titleBar.ButtonForegroundColor = titleBar.ButtonInactiveForegroundColor = lightbrush;
            titleBar.BackgroundColor = Colors.Transparent;
        }

        private void Disclaimer_Click(object sender, RoutedEventArgs e)
        {
            DisclaimerView.Visibility = DisclaimerView.Visibility == Visibility.Collapsed ? Visibility.Visible : Visibility.Collapsed;
        }
        private async void Btnaddproject_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var selectNew = new SelectNew();
                var selectTemplate = new SelectTemplate();
                var selectFolder = new SelectFolder();


                var result = await selectNew.ShowAsync();
                ContentDialogResult res;
                if (result == ContentDialogResult.Primary)
                {
                    switch ((selectNew.TempList.SelectedItem as TemplateSelection).Tag)
                    {
                        case "empty":
                            res = await selectFolder.ShowAsync();
                            if (res == ContentDialogResult.Primary)
                            {
                                var folder = selectFolder.folder;
                                if (folder != null)
                                {
                                    StorageApplicationPermissions.FutureAccessList.AddOrReplace(folder.Name, folder, "");
                                    StorageApplicationPermissions.MostRecentlyUsedList.AddOrReplace(folder.Name, folder, "");
                                    App.VM.RecentAccessList = StorageApplicationPermissions.MostRecentlyUsedList;
                                    var proj = new Project(folder.Name, folder, App.VM.GenerateTreeView(folder));
                                    App.VM.CurrentProject = proj;
                                    App.VM.Default.ProjectList.Add(proj);
                                    App.VM.Default.LastActiveProject = proj.Name;
                                    // App.AppViewModel.UpdateRecentAccessList();
                                }
                            }
                            break;
                        case "template":
                            res = await selectTemplate.ShowAsync();
                            if (res == ContentDialogResult.Primary)
                            {
                                string project = (selectTemplate.TempList.SelectedItem as TemplateSelection).Tag;

                                res = await selectFolder.ShowAsync();
                                if (res == ContentDialogResult.Primary)
                                {
                                    var folder = selectFolder.folder;
                                    if (folder != null)
                                    {
                                        StorageApplicationPermissions.FutureAccessList.AddOrReplace(folder.Name, folder, "");
                                        StorageApplicationPermissions.MostRecentlyUsedList.AddOrReplace(folder.Name, folder, "");
                                        App.VM.RecentAccessList = StorageApplicationPermissions.MostRecentlyUsedList;
                                        string root = Windows.ApplicationModel.Package.Current.InstalledLocation.Path;
                                        string path = root + @"\Templates";
                                        var templateFolder = await StorageFolder.GetFolderFromPathAsync(path + @"\" + project);
                                        //ZipFile.ExtractToDirectory(path + @"\" + project + ".zip", folder.Path,true);
                                        await CopyFolderAsync(templateFolder, folder);
                                        string rootfile = "";
                                        switch (project)
                                        {
                                            case "mwe": rootfile = "main.tex"; break;
                                            case "projpres": rootfile = "prd_presentation.tex"; break;
                                            case "projthes": rootfile = "prd_thesis.tex"; break;
                                            case "single": rootfile = "main.tex"; break;
                                            default: break;
                                        }
                                        var proj = new Project(folder.Name, folder, App.VM.GenerateTreeView(folder, rootfile)) { RootFile = rootfile };
                                        App.VM.CurrentProject = proj;
                                        App.VM.Default.ProjectList.Add(proj);
                                        App.VM.Default.LastActiveProject = proj.Name;

                                        // App.AppViewModel.UpdateRecentAccessList();
                                    }
                                }
                            }
                            break;
                        default: break;

                    }
                }


            }
            catch (Exception ex)
            {
                App.VM.LOG(ex.Message);
            }

        }
        public static async Task CopyFolderAsync(StorageFolder source, StorageFolder destinationContainer, string desiredName = null)
        {
            foreach (var file in await source.GetFilesAsync())
            {
                await file.CopyAsync(destinationContainer, file.Name, NameCollisionOption.ReplaceExisting);
            }
            foreach (var folder in await source.GetFoldersAsync())
            {
                await CopyFolderAsync(folder, destinationContainer);
            }
        }
        private void Btndeleteproject_Click(object sender, RoutedEventArgs e)
        {
            var proj = (sender as FrameworkElement).DataContext as Project;
            StorageApplicationPermissions.MostRecentlyUsedList.Remove(proj.Name);
            App.VM.Default.ProjectList.Remove(proj);
            //App.VM.UpdateRecentAccessList();
        }

        private async void BtnLoad_Click(object sender, RoutedEventArgs e)
        {
            try
            {

                var proj = (sender as FrameworkElement).DataContext as Project;
                var f = await StorageApplicationPermissions.MostRecentlyUsedList.GetFolderAsync(proj.Name);
                var list = App.VM.Default.ProjectList.Where(x => x.Name == f.Name);
                if (list.Count() == 1)
                {
                    var project = list.FirstOrDefault();
                    project.Folder = f;
                    project.Directory = App.VM.GenerateTreeView(f, proj.RootFile);
                    App.VM.CurrentProject = project;
                }
                App.VM.Default.LastActiveProject = proj.Name;
                App.VM.FileItems.Clear();
                // App.VM.CurrentFileItem.FileContent = "";
                // var rf = StorageApplicationPermissions.MostRecentlyUsedList.Entries.Where(x => x.Token == proj.Name).FirstOrDefault();

                if (App.VM.CurrentProject.RootFile != null)
                {
                    await Task.Delay(500);
                    FileItem root = App.VM.CurrentProject.Directory.Where(x => x.IsRoot == true).FirstOrDefault();
                    if (root != null)
                        App.VM.OpenFile(root);
                }

            }
            catch (Exception ex)
            {
                App.VM.LOG("Error on loading project: " + ex.Message);
            }
        }

        private async void OpeninExplorer_Click(object sender, RoutedEventArgs e)
        {
            await Launcher.LaunchFolderAsync(App.VM.CurrentProject.Folder);
        }

        private void Unload_Click(object sender, RoutedEventArgs e)
        {
            App.VM.CurrentProject = new Project();
            App.VM.FileItems.Clear();
        }

        private void NavigationViewItemHeader_RightTapped(object sender, RightTappedRoutedEventArgs e)
        {
            FlyoutBase.ShowAttachedFlyout(sender as FrameworkElement);
        }
    }
    class MyTreeViewItem : Microsoft.UI.Xaml.Controls.TreeViewItem
    {
        protected override void OnDragEnter(DragEventArgs e)
        {
            try
            {
                e.AcceptedOperation = DataPackageOperation.None;
                var draggedItem = MainPage.DraggedItems[0];
                var draggedOverItem = DataContext as FileItem;
                // Block TreeViewNode auto expanding if we are dragging a group onto another group
                if (draggedItem.File is StorageFolder && draggedOverItem.File is StorageFolder)
                {
                    // e.Handled = true;
                }
                if (draggedItem.File is StorageFile sf && draggedOverItem.File is StorageFolder fold)
                {

                    if (draggedItem.FileFolder == fold.Path)
                    {
                        //App.VM.LOG("DRAGENTER "+ draggedItem.FileFolder + " :: " + fold.Path);
                        e.AcceptedOperation = DataPackageOperation.None;

                        e.Handled = true;
                    }
                }
                base.OnDragEnter(e);
            }
            catch (Exception ex)
            {
                App.VM.LOG("OnDragEnter" + ex.Message);
            }
        }

        protected override void OnDragOver(DragEventArgs e)
        {
            try
            {
                var draggedItem = MainPage.DraggedItems[0];
                var draggedOverItem = DataContext as FileItem;

                if (draggedItem.File is StorageFolder && draggedOverItem.File is StorageFolder)
                {
                    e.Handled = true;
                }
                if (draggedItem.File is StorageFile sf && draggedOverItem.File is StorageFolder fold)
                {
                    if (draggedItem.FileFolder == fold.Path)
                    {
                        e.AcceptedOperation = DataPackageOperation.None;
                    }
                    else e.AcceptedOperation = draggedOverItem.File is StorageFolder && !(draggedItem.File is StorageFolder) ? DataPackageOperation.Move : DataPackageOperation.None;
                }
                else e.AcceptedOperation = draggedOverItem.File is StorageFolder && !(draggedItem.File is StorageFolder) ? DataPackageOperation.Move : DataPackageOperation.None;
                base.OnDragOver(e);
            }
            catch (Exception ex)
            {
                App.VM.LOG("OnDragOver" + ex.Message);
            }
        }
        protected override void OnDrop(DragEventArgs e)
        {
            try
            {
                var data = DataContext as FileItem;
                // Block all drops on leaf node
                if (!(data.File is StorageFolder))
                {
                    e.Handled = true;
                }
                base.OnDrop(e);
            }
            catch (Exception ex)
            {
                App.VM.LOG("OnDrop" + ex.Message);
            }
        }
    }
}