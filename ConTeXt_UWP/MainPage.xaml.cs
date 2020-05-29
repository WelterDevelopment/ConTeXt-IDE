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

                foreach (NavigationViewItemBase item in nvSample.MenuItems)
                {
                    if (App.VM.CurrentProject.Folder != null)
                    {
                        if (item is NavigationViewItem && item.Tag.ToString() == "IDE")
                        {
                            nvSample.SelectedItem = item;

                            contentFrame.Navigate(typeof(Editor), null, new DrillInNavigationTransitionInfo());
                            break;
                        }
                    }
                    else
                    {
                        if (item is NavigationViewItem && item.Tag.ToString() == "Projects")
                        {
                            nvSample.SelectedItem = item;

                            contentFrame.Navigate(typeof(Projects), null, new DrillInNavigationTransitionInfo());
                            break;
                        }
                    }
                }
                if (ApiInformation.IsApiContractPresent("Windows.ApplicationModel.FullTrustAppContract", 1))
                {
                    await FullTrustProcessLauncher.LaunchFullTrustProcessForCurrentAppAsync("Parameters");
                    //Thread.Sleep(5000);
                }
                nvSample.IsBackEnabled = contentFrame.CanGoBack;
            }
            catch (Exception ex)
            {
                App.VM.LOG("Error on Navigation: " + ex.Message);
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
                        case "IDE": contentFrame.Navigate(typeof(Editor), null, new DrillInNavigationTransitionInfo()); break;
                        case "Settings": contentFrame.Navigate(typeof(SettingsPage), null, new DrillInNavigationTransitionInfo()); break;
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
                        case "Settings": contentFrame.Navigate(typeof(SettingsPage), null, new DrillInNavigationTransitionInfo()); break;
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



        private void About_Tapped(object sender, TappedRoutedEventArgs e)
        {
            nvSample.SelectedItem = (sender as NavigationViewItem);
            contentFrame.Navigate(typeof(About), null, new DrillInNavigationTransitionInfo());

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

            //currentViewModel.NVHeader = contentFrame.Content.GetType().Name;
        }

        private void ContentFrame_Loaded(object sender, RoutedEventArgs e)
        {

        }

        private void ClearLog_Click(object sender, RoutedEventArgs e)
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
            App.VM.UpdateMRUEntry(App.VM.CurrentProject);
        }

        private async void Delete_Click(object sender, RoutedEventArgs e)
        {
            var fi = (FileItem)(sender as FrameworkElement).DataContext;
            if (App.VM.CurrentProject.Directory.Contains(fi))
            {
                App.VM.CurrentProject.Directory.Remove(fi);
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
                App.VM.LOG(ex.Message);
            }
        }

        private async Task<string> rename(FileItem.ExplorerItemType type, string startstring)
        {
            string newstring = startstring;

            var cd = new ContentDialog() { Title = "Rename " + type, PrimaryButtonText = "rename", CloseButtonText = "cancel" };
            TextBox tb = new TextBox() { Text = startstring };
            cd.Content = tb;
            cd.PrimaryButtonClick += (a, b) =>
            {
                newstring = tb.Text;
            };
            await cd.ShowAsync();
            return newstring;
        }

        private async void AddFile_Click(object sender, RoutedEventArgs e)
        {

            try
            {
                string name = "file.tex";
                var cd = new ContentDialog() { Title = "Set file name", PrimaryButtonText = "ok", CloseButtonText = "cancel", DefaultButton = ContentDialogButton.Primary };
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
                    var file = await folder.CreateFileAsync(name);
                    var fi = new FileItem(file) { Type = FileItem.ExplorerItemType.File, FileLanguage = FileItem.GetFileLanguage(file.FileType) };
                    App.VM.CurrentProject.Directory.Add(fi);
                }
                else
                    App.VM.LOG(name + " does already exist.");
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
            App.VM.LOG("TreeView_DropCompleted");
        }

        private void TreeView_Drop(object sender, DragEventArgs e)
        {
            App.VM.LOG(e.GetType().FullName);
            e.AcceptedOperation = Windows.ApplicationModel.DataTransfer.DataPackageOperation.Copy;

           // e.Handled = true;
        }

        private async void NewFile_Click(object sender, RoutedEventArgs e)
        {
            try
            {

                var fileitem = (sender as FrameworkElement).DataContext as FileItem;
                string name = "bla.tex";
                var folder = fileitem.File as StorageFolder;
                if (await folder.TryGetItemAsync(name) == null)
                {
                    var file = await folder.CreateFileAsync(name);
                    var fi = new FileItem(file) { Type = FileItem.ExplorerItemType.File, FileLanguage = Path.GetExtension(file.Path) };
                    fileitem.Children.Add(fi);
                }
                else
                    App.VM.LOG(name + " does already exist.");
            }
            catch (Exception ex)
            {
                App.VM.LOG(ex.Message);
            }
        }

        private void TreeView_DragItemsStarting(TreeView sender, TreeViewDragItemsStartingEventArgs args)
        {
            App.VM.LOG("TreeView_DragItemsStarting" + (args.Items[0] as FileItem).FileName);
            args.Data.RequestedOperation = DataPackageOperation.Move;
        }

        private void TreeView_DragItemsCompleted(TreeView sender, TreeViewDragItemsCompletedEventArgs args)
        {
            App.VM.LOG("TreeView_DragItemsCompleted" + (args.Items[0] as FileItem).FileName + "\n" + string.Join(", ", (new List<FileItem>(App.VM.CurrentProject.Directory)).Select(x => x.FileName).ToArray()));
        }

        private void TreeViewItem_Drop(object sender, DragEventArgs e)
        {
            e.AcceptedOperation = DataPackageOperation.Move;
            var target = (sender as Microsoft.UI.Xaml.Controls.TreeViewItem).DataContext as FileItem;
            var source = (e.OriginalSource as Microsoft.UI.Xaml.Controls.TreeViewItem).DataContext as FileItem;
            if (source.File is StorageFolder)
            {
                Tree.CanReorderItems = false;
                e.AcceptedOperation = DataPackageOperation.None;
                // e.Handled = false;
            }
            else
            {
                if (target.File is StorageFile storageFile)
                {
                    Tree.CanReorderItems = false;
                    e.AcceptedOperation = DataPackageOperation.None;
                    //e.Handled = false;
                }
                else
                {
                    Tree.CanReorderItems = true;
                    e.AcceptedOperation = DataPackageOperation.Move;
                    // e.Handled = true;
                }
            }
            // e.Handled = true;
        }

        private void TreeViewItem_DropCompleted(UIElement sender, DropCompletedEventArgs args)
        {
        }

        private void TreeView_DragStarting(UIElement sender, DragStartingEventArgs args)
        {
            // App.VM.LOG(args.OriginalSource.GetType().FullName);
        }

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
            // if ((args.Items[0] as FileItem).File is StorageFolder)
            //     sender.CanReorderItems = false;
            // else
            //     sender.CanReorderItems = true;
            //// App.VM.LOG("TreeView_DragItemsStarting" + (args.Items[0] as FileItem).FileName);
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

        private void TreeView_DragItemsCompleted(Microsoft.UI.Xaml.Controls.TreeView sender, Microsoft.UI.Xaml.Controls.TreeViewDragItemsCompletedEventArgs args)
        {
            //  App.VM.LOG("TreeView_DragItemsCompleted" + (args.Items[0] as FileItem).FileName + "\n" + string.Join(", ", (new List<FileItem>(App.VM.CurrentProject.Directory)).Select(x => x.FileName).ToArray()));
            try
            {
                //App.VM.LOG(args.Items.Count.ToString());
                //if (args.DropResult != DataPackageOperation.None)
                //    foreach (FileItem item in args.Items)
                //    {
                //        if (args.NewParentItem != null)
                //        {
                //            var parent = args.NewParentItem as FileItem;
                //            parent.Children.Add(item);
                //        }
                //    }

                DraggedItems.Clear();
            }
            catch (Exception ex)
            {
                App.VM.LOG("TreeView_DragItemsCompleted" + ex.Message);
            }
        }
        private void Tree_Drop(object sender, DragEventArgs e)
        {
            App.VM.LOG("Tree_Drop");
            e.Handled = true;
        }
    }
    class MyTreeViewItem : Microsoft.UI.Xaml.Controls.TreeViewItem
    {
        protected override void OnDragEnter(DragEventArgs e)
        {
            try
            {
                var draggedItem = MainPage.DraggedItems[0];
                var draggedOverItem = DataContext as FileItem;
                // Block TreeViewNode auto expanding if we are dragging a group onto another group
                if (draggedItem.File is StorageFolder && draggedOverItem.File is StorageFolder)
                {
                    e.Handled = true;
                }

                base.OnDragEnter(e);
            }
            catch (Exception ex)
            {
                App.VM.LOG("OnDrop" + ex.Message);
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
                    //- Group
                    //-- Leaf1
                    //-- (Group2) <- Blocks dropping another Group here
                    //-- Leaf2
                    e.Handled = true;
                }
                base.OnDragOver(e);
                e.AcceptedOperation = draggedOverItem.File is StorageFolder && !(draggedItem.File is StorageFolder) ? DataPackageOperation.Move : DataPackageOperation.None;
            }
            catch (Exception ex)
            {
                App.VM.LOG("OnDrop" + ex.Message);
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