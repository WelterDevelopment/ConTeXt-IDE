using ConTeXt_UWP.Helpers;
using ConTeXt_UWP.Models;
using ConTeXt_UWP.ViewModels;
using Microsoft.UI.Xaml.Controls;
using Monaco;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.ApplicationModel.AppService;
using Windows.ApplicationModel.Core;
using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.Storage.AccessCache;
using Windows.System;
using Windows.UI;
using Windows.UI.Core.Preview;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace ConTeXt_UWP
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        public MainPage()
        {
            this.InitializeComponent();

            try
            {
                var coreTitleBar = CoreApplication.GetCurrentView().TitleBar;
                coreTitleBar.ExtendViewIntoTitleBar = true;
                Window.Current.SetTitleBar(TabStripFooter);

                coreTitleBar.LayoutMetricsChanged += CoreTitleBar_LayoutMetricsChanged;

                App.VM.Default.PropertyChanged += Default_PropertyChanged;

                SystemNavigationManagerPreview.GetForCurrentView().CloseRequested += MainPage_CloseRequested;

                Version.Text = string.Format("Version: {0}.{1}.{2}.{3}",
                     Package.Current.Id.Version.Major,
                     Package.Current.Id.Version.Minor,
                     Package.Current.Id.Version.Build,
                     Package.Current.Id.Version.Revision);

                foreach (var FileEvent in App.VM.FileActivatedEvents)
                {
                    if (FileEvent != null)
                    {
                        foreach (StorageFile file in FileEvent.Files)
                        {
                            var fileitem = new FileItem(file) { };
                            App.VM.OpenFile(fileitem);
                        }
                    }
                }
                App.VM.FileActivatedEvents.Clear();
            }
            catch (Exception ex)
            {
                App.VM.Message(ex.Message);
            }
        }

        public static List<FileItem> DraggedItems { get; set; } = new List<FileItem>();

        public static ObservableCollection<FileItem> DraggedItemsSource { get; set; }

        private ViewModel VM { get; } = App.VM;

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

        public async Task Compile(bool compileRoot = false, FileItem fileToCompile = null)
        {
            if (!App.VM.IsSaving)
                try
                {
                    App.VM.IsError = false;
                    App.VM.IsPaused = false;
                    App.VM.IsSaving = true;

                    string[] modes = new string[] { };
                    if (App.VM.CurrentProject != null)
                        modes = App.VM.CurrentProject.Modes.Where(x => x.IsSelected).Select(x => x.Name).ToArray();
                    if (modes.Length > 0 && App.VM.Default.UseModes)
                        App.VM.Default.Modes = string.Join(",", modes);
                    else App.VM.Default.Modes = "";

                    FileItem filetocompile = null;
                    if (compileRoot)
                    {
                        FileItem[] root = new FileItem[] { };
                        if (App.VM.CurrentProject != null)
                            root = App.VM.CurrentProject.Directory.Where(x => x.IsRoot).ToArray();
                        if (root.Length > 0)
                            filetocompile = root.FirstOrDefault();
                        else
                            filetocompile = fileToCompile ?? App.VM.CurrentFileItem;
                    }
                    else
                    {
                        filetocompile = fileToCompile ?? App.VM.CurrentFileItem;
                    }
                    string logtext = "Compiling " + filetocompile.File.Name;
                    if (modes.Length > 0 && App.VM.Default.UseModes)
                        logtext += "; with modes: " + App.VM.Default.Modes;
                    if (App.VM.Default.AdditionalParameters.Trim().Length > 0 && App.VM.Default.UseParameters)
                        logtext += "; with parameters: " + App.VM.Default.AdditionalParameters;
                    App.VM.LOG(logtext);
                    App.VM.Default.TexFileFolder = filetocompile.FileFolder;
                    App.VM.Default.TexFileName = filetocompile.FileName;
                    App.VM.Default.TexFilePath = filetocompile.File.Path;
                    ValueSet request = new ValueSet { { "compile", true } };
                    AppServiceResponse response = await App.VM.appServiceConnection.SendMessageAsync(request);
                    // display the response key/value pairs
                    foreach (string key in response.Message.Keys)
                    {
                        if ((string)response.Message[key] == "compiled")
                        {
                            string local = ApplicationData.Current.LocalFolder.Path;
                            string curFile = System.IO.Path.GetFileName(App.VM.Default.TexFilePath);
                            string filewithoutext = System.IO.Path.GetFileNameWithoutExtension(curFile);
                            string curPDF = filewithoutext + ".pdf";
                            string curPDFPath = System.IO.Path.Combine(App.VM.Default.TexFilePath, curPDF);
                            string newPathToFile = System.IO.Path.Combine(local, curPDF);
                            StorageFolder currFolder = await StorageFolder.GetFolderFromPathAsync(App.VM.Default.TexFileFolder);
                            App.VM.LOG("Opening " + System.IO.Path.GetFileNameWithoutExtension(App.VM.Default.TexFileName) + ".pdf");
                            //StorageFile file = await ApplicationData.Current.LocalFolder.GetFileAsync(curPDF);

                            var error = await currFolder.TryGetItemAsync(System.IO.Path.GetFileNameWithoutExtension(App.VM.Default.TexFileName) + "-error.log");
                            if (error != null)
                            {
                                App.VM.IsError = true;
                                var errorfile = error as StorageFile;
                                //var stream = await errorfile.OpenStreamForReadAsync();
                                //byte[] buffer = new byte[stream.Length];
                                //stream.Read(buffer,0,(int)stream.Length);
                                //string text = Convert.ToString(buffer);
                                string text = await FileIO.ReadTextAsync(errorfile);
                                string newtext = text.Replace("  ", "").Replace("return", "").Replace("[\"", "\"").Replace("\"]", "\"").Replace(@"\n", "").Replace("=", ":");
                                var errormessage = JsonConvert.DeserializeObject<ConTeXtErrorMessage>(newtext);
                                App.VM.LOG("Compiler error: " + errormessage.lasttexerror);

                                App.VM.ConTeXtErrorMessage = errormessage;
                            }
                            else
                            {
                                App.VM.IsPaused = true;
                                App.VM.IsError = false;
                                //await Task.Delay(2000);
                                App.VM.IsVisible = false;
                            }

                            if (App.VM.Default.AutoOpenPDF)
                            {
                                StorageFile pdfout = await currFolder.TryGetItemAsync(System.IO.Path.GetFileNameWithoutExtension(App.VM.Default.TexFileName) + ".pdf") as StorageFile;
                                if (pdfout != null)
                                {
                                    if (App.VM.Default.InternalViewer)
                                    {
                                        await OpenPDF(pdfout);
                                    }
                                    else
                                    {
                                        await Launcher.LaunchFileAsync(pdfout);
                                    }
                                }
                            }

                            if (App.VM.Default.AutoOpenLOG)
                            {
                                if ((App.VM.Default.AutoOpenLOGOnlyOnError && error != null) | !App.VM.Default.AutoOpenLOGOnlyOnError)
                                {
                                    StorageFile logout = await currFolder.TryGetItemAsync(System.IO.Path.GetFileNameWithoutExtension(App.VM.Default.TexFileName) + ".log") as StorageFile;
                                    if (logout != null)
                                    {
                                        FileItem logFile = new FileItem(logout) { };
                                        App.VM.OpenFile(logFile);
                                    }
                                }
                                else if (App.VM.Default.AutoOpenLOGOnlyOnError && error == null)
                                {
                                    if (App.VM.FileItems.Any(x => x.IsLogFile))
                                    {
                                        App.VM.FileItems.Remove(App.VM.FileItems.First(x => x.IsLogFile));
                                    }
                                }
                            }
                        }
                        else
                        {
                            App.VM.LOG("Compiler error");
                        }
                    }
                }
                catch (Exception f)
                {
                    App.VM.IsError = true;
                    App.VM.LOG("Exception at compile: " + f.Message);
                }
            App.VM.IsSaving = false;
        }

        public async void FirstStart()
        {
            if (App.VM.Default.FirstStart)
            {
                ShowTeachingTip("AddProject", Btnaddproject);

                App.VM.Default.FirstStart = false;
            }
        }

        public void ShowTeachingTip(string ID, object Target)
        {
            var helpItem = App.VM.Default.HelpItemList[App.VM.Default.HelpItemList.IndexOf(App.VM.Default.HelpItemList.First(x => x.ID == ID))];
            if (!helpItem.Shown)
            {
                var tip = new TeachingTip() { Title = helpItem.Title, Target = (FrameworkElement)Target, PreferredPlacement = TeachingTipPlacementMode.Right, IsLightDismissEnabled = false, IsOpen = false };
                if (!string.IsNullOrWhiteSpace(helpItem.Subtitle))
                    tip.Subtitle = helpItem.Subtitle;
                tip.Content = new TextBlock() { TextWrapping = TextWrapping.WrapWholeWords, Text = helpItem.Text };
                RootGrid.Children.Add(tip);
                tip.IsOpen = true;
                helpItem.Shown = true;
                App.VM.Default.SaveSettings();
            }
        }

        private static MyTreeViewItem VisualUpwardSearch(DependencyObject source)
        {
            while (source != null && !(source is MyTreeViewItem))
                source = VisualTreeHelper.GetParent(source);

            return source as MyTreeViewItem;
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
                        App.VM.CurrentProject.Directory[0].Children.Add(fi);
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
                var result = await cd.ShowAsync();
                if (result == ContentDialogResult.Primary)
                {
                    name = tb.Text;
                    var folder = App.VM.CurrentProject.Folder;
                    if (await folder.TryGetItemAsync(name) == null)
                    {
                        var subfolder = await folder.CreateFolderAsync(name);
                        var fi = new FileItem(subfolder) { Type = FileItem.ExplorerItemType.Folder };
                        App.VM.CurrentProject.Directory[0].Children.Insert(0, fi);
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

        private async void addMode_Click(object sender, RoutedEventArgs e)
        {
            Mode mode = new Mode();
            string name = "";
            var cd = new ContentDialog() { Title = "Add mode", PrimaryButtonText = "Ok", CloseButtonText = "Cancel", DefaultButton = ContentDialogButton.Primary };
            TextBox tb = new TextBox() { Text = name };
            cd.Content = tb;
            if (await cd.ShowAsync() == ContentDialogResult.Primary)
            {
                mode.IsSelected = true;
                mode.Name = tb.Text;
                App.VM.CurrentProject.Modes.Add(mode);
                App.VM.Default.SaveSettings();
            }
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

                                    App.VM.FileItemsTree.Clear();
                                    var proj = new Project(folder.Name, folder, App.VM.FileItemsTree);
                                    App.VM.Default.ProjectList.Add(proj);
                                    App.VM.CurrentProject = proj;
                                    App.VM.GenerateTreeView(folder);

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
                                        //var proj = new Project(folder.Name, folder, await App.VM.GenerateTreeView(folder, rootfile)) { RootFile = rootfile };
                                        App.VM.FileItemsTree.Clear();
                                        var proj = new Project(folder.Name, folder, App.VM.FileItemsTree) { RootFile = rootfile };
                                        App.VM.Default.ProjectList.Add(proj);
                                        App.VM.CurrentProject = proj;
                                        App.VM.GenerateTreeView(folder, rootfile);
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

        private async void Btncompile_Click(object sender, RoutedEventArgs e)
        {
            await App.VM.UWPSave();
            await Compile();
        }

        private async void Btncompileroot_Click(object sender, RoutedEventArgs e)
        {
            await App.VM.UWPSaveAll();
            await Compile(true);
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
                    App.VM.FileItemsTree = new ObservableCollection<FileItem>();
                    App.VM.GenerateTreeView(f, proj.RootFile);

                    project.Directory = App.VM.FileItemsTree;

                    //await Task.Delay(500);

                    App.VM.CurrentProject = project;

                    // App.VM.CurrentProject.Directory.CollectionChanged += App.VM.CurrentProject.Directory_CollectionChanged;
                }

                App.VM.Default.LastActiveProject = proj.Name;
                // App.VM.FileItems?.Clear();
                // App.VM.CurrentFileItem.FileContent = "";
                // var rf = StorageApplicationPermissions.MostRecentlyUsedList.Entries.Where(x => x.Token == proj.Name).FirstOrDefault();

                if (App.VM.CurrentProject?.RootFile != null)
                {
                    await Task.Delay(500);
                    FileItem root = App.VM.CurrentProject?.Directory?.Where(x => x.IsRoot == true).FirstOrDefault();
                    if (root != null)
                        App.VM.OpenFile(root);
                }
            }
            catch (Exception ex)
            {
                App.VM.LOG("Error on loading project: " + ex.Message);
            }
        }

        private async void Btnsave_Click(object sender, RoutedEventArgs e)
        {
            //App.VM.EditorOptions.WordWrap = App.VM.EditorOptions.WordWrap ==  WordWrap.On ? WordWrap.Off : WordWrap.On; ;
            //App.VM.EditorOptions.LineNumbers = LineNumbersType.Relative;

            await App.VM.UWPSave();
        }

        private async void btnsaveall_Click(object sender, RoutedEventArgs e)
        {
            await App.VM.UWPSaveAll();
        }


        private void CheckBox_Checked(object sender, RoutedEventArgs e)
        {
            App.VM.Default.SaveSettings();
        }

        private void ClearLog_Click(object sender, RoutedEventArgs e)
        {
            Log.Blocks.Clear();
            RichTextBlockHelper.logline = 0;
        }

        private async void Compile_Click(object sender, RoutedEventArgs e)
        {
            FileItem fi = (sender as FrameworkElement).DataContext as FileItem;
            await App.VM.UWPSaveAll();
            Compile(false, fi);
        }

        private void CoreTitleBar_LayoutMetricsChanged(CoreApplicationViewTitleBar sender, object args)
        {
            if (FlowDirection == FlowDirection.LeftToRight)
            {
                TabStripFooter.MinWidth = sender.SystemOverlayRightInset;
                TabStripHeader.MinWidth = sender.SystemOverlayLeftInset;
            }
            else
            {
                TabStripFooter.MinWidth = sender.SystemOverlayLeftInset;
                TabStripHeader.MinWidth = sender.SystemOverlayRightInset;
            }

            TabStripFooter.Height = TabStripHeader.Height = sender.Height;
        }

        private void CurrentProject_RightTapped(object sender, RightTappedRoutedEventArgs e)
        {
            FlyoutBase.ShowAttachedFlyout(sender as FrameworkElement);
        }

        private void Default_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case "ShowLog":
                    if (App.VM.Default.ShowLog)
                    {
                        IDEGridRow.Height = new GridLength(2, GridUnitType.Star);
                        LogGridSplitter.Height = new GridLength(6, GridUnitType.Pixel);
                        LogGridRow.Height = new GridLength(200, GridUnitType.Pixel);
                    }
                    else
                    {
                        IDEGridRow.Height = new GridLength(1, GridUnitType.Star);
                        LogGridSplitter.Height = new GridLength(0, GridUnitType.Pixel);
                        LogGridRow.Height = new GridLength(0, GridUnitType.Pixel);
                    }
                    break;

                case "ShowProjects":
                    if (App.VM.Default.ShowProjects)
                    {
                        ProjectsGridSplitter.Height = new GridLength(6, GridUnitType.Pixel);
                        ProjectsGridLibraryRow.Height = new GridLength(300, GridUnitType.Pixel);
                    }
                    else
                    {
                        ProjectsGridSplitter.Height = new GridLength(0, GridUnitType.Pixel);
                        ProjectsGridLibraryRow.Height = new GridLength(0, GridUnitType.Pixel);
                    }
                    break;

                case "ShowProjectPane":
                    if (App.VM.Default.ShowProjectPane)
                    {
                        ContentGridSplitter.Width = new GridLength(6, GridUnitType.Pixel);
                        ContentGridProjectPaneColumn.Width = new GridLength(300, GridUnitType.Pixel);
                    }
                    else
                    {
                        ContentGridSplitter.Width = new GridLength(0, GridUnitType.Pixel);
                        ContentGridProjectPaneColumn.Width = new GridLength(0, GridUnitType.Pixel);
                    }
                    break;

                case "InternalViewer":
                    if (App.VM.Default.InternalViewer)
                    {
                        PDFGridSplitter.Width = new GridLength(6, GridUnitType.Pixel);
                        PDFGridColumn.Width = new GridLength(400, GridUnitType.Pixel);
                    }
                    else
                    {
                        PDFGridSplitter.Width = new GridLength(0, GridUnitType.Pixel);
                        PDFGridColumn.Width = new GridLength(0, GridUnitType.Pixel);
                    }
                    break;
                // case "ShowLog": IDEGridRow.Height = new GridLength(1, GridUnitType.Star); break;
                default: break;
            }
        }

        private async void Delete_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var fi = (FileItem)(sender as FrameworkElement).DataContext;
                //// App.VM.LOG(sender.GetType());
                //// App.VM.LOG(((MenuFlyoutItem)sender).Parent.GetType());
                //// App.VM.LOG(((MenuFlyout)((MenuFlyoutItem)sender).Parent).Target.GetType());

                // MenuFlyout parent = (((MenuFlyoutItem)sender).FindName("MenuFlyoutContainer") as MenuFlyout);

                // var parentfileitem = ((MyTreeViewItem)parent.Target).DataContext as FileItem ;
                //// App.VM.LOG(sender.GetType());
                //// App.VM.LOG((sender as MyTreeViewItem).Parent.GetType());

                // //var parentfileitem = ((sender as MyTreeViewItem).Parent as MyTreeViewItem).DataContext as FileItem;
                // parentfileitem.Children.Remove(fi);

                RemoveItem(App.VM.CurrentProject.Directory, fi);
                App.VM.FileItems.Remove(fi);
                await fi.File.DeleteAsync();
                App.VM.LOG("File " + fi.FileName + " removed.");
            }
            catch (Exception ex)
            {
                App.VM.LOG(ex.Source + " : " + ex.Message);
            }
        }

        private void Disclaimer_Click(object sender, RoutedEventArgs e)
        {
            DisclaimerView.Visibility = DisclaimerView.Visibility == Visibility.Collapsed ? Visibility.Visible : Visibility.Collapsed;
        }

        private async void Edit_Loading(object sender, RoutedEventArgs e)
        {
            var edit = (sender as CodeEditor);
            var fileitem = edit.DataContext;
            //if (!editloadet)
            var languages = new Monaco.LanguagesHelper(edit);

            await languages.RegisterHoverProviderAsync("context", new EditorHoverProvider());

            await languages.RegisterCompletionItemProviderAsync("context", new LanguageProvider());
            if (fileitem is FileItem file)
            {
                if (file.FileLanguage == "context")
                {
                    await edit.AddActionAsync(new RunAction());
                }
            }
            else
            {
                await edit.AddActionAsync(new RunAction());
            }

            await edit.AddActionAsync(new RunRootAction());

            await edit.AddActionAsync(new SaveAction());
            await edit.AddActionAsync(new SaveAllAction());

            //await edit.AddActionAsync(new FileOutlineAction());

            // App.VM.CurrentEditor = edit;
        }

        private void EditorTabViewDrag(object sender, DragEventArgs e)
        {
            
            e.AcceptedOperation = DataPackageOperation.Link;
            if (e.DragUIOverride != null)
            {
                e.DragUIOverride.Caption = "Open File";
                e.DragUIOverride.IsContentVisible = true;
            }
        }

        private async void EditorTabViewDrop(object sender, DragEventArgs e)
        {
            //if (e.DataView.Contains())
            //FileDrop(e);
            //foreach (var item in await e.DataView.())
            //App.VM.LOG(string.Join(", ", e.DataView.AvailableFormats));

            foreach (var item in DraggedItems)
            {
                if (item.Type == FileItem.ExplorerItemType.File)
                    App.VM.OpenFile(item);
            }
            DraggedItems.Clear();
            e.Handled = true;
        }

        private async void FileCopy(DragEventArgs e)
        {
            if (e.DataView.Contains(StandardDataFormats.StorageItems) && App.VM.IsProjectLoaded)
            {
                IReadOnlyList<IStorageItem> items = await e.DataView.GetStorageItemsAsync();
                foreach (StorageFile file in items)
                {
                    var fi = new FileItem(file);
                    App.VM.CurrentProject.Directory.Add(fi);

                    if (Path.GetDirectoryName(file.Path) != Path.GetDirectoryName(App.VM.CurrentProject.Folder.Path))
                    {
                        await file.CopyAsync(App.VM.CurrentProject.Folder, file.Name, NameCollisionOption.GenerateUniqueName);
                        fi.FileFolder = Path.GetDirectoryName(file.Path);
                    }

                    //else if (fi.File is StorageFolder fold)
                    //{
                    //    // await fold.c
                    //}

                    //var fileitem = new FileItem(file) { };
                    //App.VM.OpenFile(fileitem);
                }
            }
        }

        private async void FileDrop(DragEventArgs e)
        {
            if (e.DataView.Contains(StandardDataFormats.StorageItems))
            {
                IReadOnlyList<IStorageItem> items = await e.DataView.GetStorageItemsAsync();
                foreach (StorageFile file in items)
                {
                    var fileitem = new FileItem(file) { };
                    App.VM.OpenFile(fileitem);
                }
            }
        }

        private async void Github_Click(object sender, RoutedEventArgs e)
        {
            bool result = await Launcher.LaunchUriAsync(new System.Uri("https://github.com/WelterDevelopment/ConTeXt-IDE"));
        }

        private async void Help_ItemClick(object sender, ItemClickEventArgs e)
        {
            try
            {
                var hf = e.ClickedItem as Helpfile;
                var lsf = ApplicationData.Current.LocalFolder;
                App.VM.LOG("Opening " + lsf.Path + hf.Path + hf.FileName);
                var sf = await StorageFile.GetFileFromPathAsync(lsf.Path + hf.Path + hf.FileName);

                if (App.VM.Default.HelpPDFInInternalViewer)
                {
                    await OpenPDF(sf);
                }
                else
                    await Launcher.LaunchFileAsync(sf);
            }
            catch (Exception ex)
            {
                App.VM.LOG(ex.Message);
            }
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

        private async void Modes_Click(object sender, RoutedEventArgs e)
        {
            ShowTeachingTip("Modes", sender);
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

        private async void OnFileDrop(object sender, DragEventArgs e)
        {
            if (e.DataView.Contains(StandardDataFormats.StorageItems))
            {
                IReadOnlyList<IStorageItem> items = await e.DataView.GetStorageItemsAsync();
                foreach (StorageFile file in items)
                {
                    var fileitem = new FileItem(file) { };
                    App.VM.OpenFile(fileitem);
                }
            }
            else
            {
                //object obj = null;
                //if (e.DataView.GetType.TryGetValue("FileItem", out obj))
                //{
                //    var fi = obj as FileItem;
                //    if (fi.Type == FileItem.ExplorerItemType.File)
                //    {
                //        App.VM.OpenFile(fi);
                //    }
                //}
            }
            e.Handled = true;
        }

        private async void OpeninExplorer_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                await Launcher.LaunchFolderAsync(App.VM.CurrentProject.Folder);
            }
            catch (Exception ex)
            {
                App.VM.LOG("Error on opening folder: " + ex.Message);
            }
        }

        private async Task<bool> OpenPDF(StorageFile pdfout)
        {
            try
            {
                Stream stream = await pdfout.OpenStreamForReadAsync();
                byte[] buffer = new byte[stream.Length];
                stream.Read(buffer, 0, (int)stream.Length);
                var asBase64 = Convert.ToBase64String(buffer);
                await PDFReader.InvokeScriptAsync("openPdfAsBase64", new[] { asBase64 });
                return true;
            }
            catch (Exception ex)
            {
                App.VM.LOG("Error on opening the pdf file in the internal viewer: " + ex.Message);
                return false;
            }
        }

        private void PDFReader_NewWindowRequested(WebView sender, WebViewNewWindowRequestedEventArgs args)
        {
            args.Handled = true;
        }

        private void PDFReader_ScriptNotify(object sender, NotifyEventArgs e)
        {
            App.VM.LOG(e.CallingUri.OriginalString);
            App.VM.LOG(e.Value);
        }

        private async void Rate_Click(object sender, RoutedEventArgs e)
        {
            bool result = await Launcher.LaunchUriAsync(new System.Uri("ms-windows-store://review/?ProductId=9nn9q389ttjr"));
        }

        private void RemoveItem(ObservableCollection<FileItem> fileItems, FileItem fileItem)
        {
            foreach (FileItem item in fileItems)
            {
                if (item == fileItem)
                {
                    fileItems.Remove(item);
                    return;
                }
                if (item.File is StorageFolder)
                {
                    RemoveItem(item.Children, fileItem);
                }
            }
        }

        private void RemoveMode_Click(object sender, RoutedEventArgs e)
        {
            Mode m = (sender as FrameworkElement).DataContext as Mode;
            App.VM.CurrentProject.Modes.Remove(m);
            App.VM.Default.SaveSettings();
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

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            App.VM.UWPSave((sender as FrameworkElement).DataContext as FileItem);
        }

        private void SetRoot_Click(object sender, RoutedEventArgs e)
        {
            var ei = (FileItem)(sender as FrameworkElement).DataContext;
            ei.IsRoot = true;
            App.VM.CurrentProject.RootFile = ei.FileName;
            //App.VM.UpdateMRUEntry(App.VM.CurrentProject);
        }

        private async void Tabs_TabCloseRequested(TabView sender, TabViewTabCloseRequestedEventArgs args)
        {
            var fi = args.Tab.DataContext as FileItem;
            if (fi.IsChanged)
            {
                var save = new ContentDialog() { Title = "Do you want to save this file before closing?", PrimaryButtonText = "Yes", SecondaryButtonText = "No", DefaultButton = ContentDialogButton.Primary };

                if (await save.ShowAsync() == ContentDialogResult.Primary)
                {
                    await App.VM.UWPSave(fi);
                }
            }
            if (App.VM.CurrentFileItem == fi)
            {
                //App.VM.CurrentFileItem = new FileItem(null);
            }

            App.VM.FileItems.Remove(fi);
        }

        private void ThemeControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ApplicationViewTitleBar titleBar = ApplicationView.GetForCurrentView().TitleBar;
            //var DefaultTheme = new Windows.UI.ViewManagement.UISettings();
            //var lightbrush = DefaultTheme.GetColorValue(Windows.UI.ViewManagement.UIColorType.Foreground);
            //if (App.VM.Default.Theme == "Light")
            //{
            //    lightbrush = Colors.Black;
            //}
            //else if (App.VM.Default.Theme == "Dark")
            //{
            //    lightbrush = Colors.White;
            //}

            titleBar.ButtonBackgroundColor = Colors.Transparent;
            titleBar.ButtonInactiveBackgroundColor = Colors.Transparent;
            titleBar.ButtonForegroundColor = titleBar.ButtonInactiveForegroundColor = Colors.White;
            titleBar.BackgroundColor = Colors.Transparent;
        }

        private void TitleButton_Click(object sender, RoutedEventArgs e)
        {
            AboutDialog.ShowAsync();
        }

        private void Tree_Drop(object sender, DragEventArgs e)
        {
            //FileCopy(e);
            // if (e.DataView.Contains(StandardDataFormats.Text) && App.VM.IsProjectLoaded)
            App.VM.CurrentProject.Directory.Add(DraggedItems[0]);
            DraggedItems.Clear();
            App.VM.LOG("Tree_Drop: " + e.OriginalSource.ToString());
            e.Handled = true;
        }

        private void Tree_Tapped(object sender, TappedRoutedEventArgs e)
        {
            FlyoutBase.ShowAttachedFlyout((FrameworkElement)sender);
        }

        private async void TreeView_DragItemsCompleted(Microsoft.UI.Xaml.Controls.TreeView sender, Microsoft.UI.Xaml.Controls.TreeViewDragItemsCompletedEventArgs args)
        {
            try
            {
                //if (args.DropResult == DataPackageOperation.Move)
                //    foreach (FileItem item in args.Items)
                //    {
                //        if (args.NewParentItem != null)
                //        {
                //            var fi = args.NewParentItem as FileItem;
                //            if (fi.File.Path == item.FileFolder)
                //            {
                //                App.VM.LOG("fi.File.Path == item.FileFolder");
                //                //App.VM.CurrentProject.Directory.Add(item);
                //                await Task.Delay(100);
                //                //App.VM.CurrentProject = new Project(App.VM.CurrentProject.Name, App.VM.CurrentProject.Folder, await App.VM.GenerateTreeView(App.VM.CurrentProject.Folder, App.VM.CurrentProject.RootFile));
                //               // App.VM.FileItemsTree.Clear();
                //               // App.VM.GenerateTreeView(App.VM.CurrentProject.Folder, App.VM.CurrentProject.RootFile);
                //            }
                //            else
                //            {
                //                App.VM.LOG("else");
                //            }

                //        }
                //        else // Dropped to the root directory
                //        {
                //           // App.VM.CurrentProject.Directory.Add(item);
                //        }

                //    }
                //DraggedItems.Clear();
                //App.VM.LOG("TreeView_DragItemsCompleted");
            }
            catch (Exception ex)
            {
                App.VM.LOG("TreeView_DragItemsCompleted" + ex.Message);
            }
        }

        private void TreeView_DragItemsStarting(Microsoft.UI.Xaml.Controls.TreeView sender, Microsoft.UI.Xaml.Controls.TreeViewDragItemsStartingEventArgs args)
        {
            //App.VM.LOG("TreeView_DragItemsStarting");
            try
            {
                DraggedItems.Clear();
                foreach (FileItem item in args.Items)
                {
                    DraggedItems.Add(item);
                }
            }
            catch (Exception ex)
            {
                App.VM.LOG("Error on TreeView_DragItemsStarting: " + ex.Message);
            }
        }

        private void TreeView_Drop(object sender, DragEventArgs e)
        {
            e.AcceptedOperation = DataPackageOperation.Move;
            if (e.DragUIOverride != null)
            {
                e.DragUIOverride.Caption = "Move to root folder";
                e.DragUIOverride.IsContentVisible = true;
            }

            App.VM.LOG("Dragging from: " + e.OriginalSource.ToString());

            e.Handled = true;
        }

        private async void Unload_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                App.VM.CurrentProject = new Project();
                App.VM.FileItems.Clear();
            }
            catch (Exception ex)
            {
                App.VM.LOG("Error on opening folder: " + ex.Message);
            }
        }

        private async void Update_Click(object sender, RoutedEventArgs e)
        {
            if (NetworkInterface.GetIsNetworkAvailable())
            {
                App.VM.IsSaving = true;
                var installing = new ContentDialog() { Title = "Please wait while updating. This can take up to 10 minutes." };
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
            else
                App.VM.Message("You need to be connected to the internet in order to update your ConTeXt distribution!", "No internet connection");
        }
    }
}