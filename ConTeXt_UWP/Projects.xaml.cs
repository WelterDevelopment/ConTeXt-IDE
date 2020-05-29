using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.Storage.AccessCache;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace ConTeXt_UWP
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class Projects : Page
    {
        public ViewModel currentViewModel = App.VM;
        public Projects()
        {
            this.InitializeComponent();

        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            try
            {
               
                App.VM.UpdateRecentAccessList();
                

            }
            catch (Exception ex)
            {
                App.VM.LOG(ex.Message);
            }
           
        }

        private void sampleTreeView_ItemInvoked(TreeView sender, TreeViewItemInvokedEventArgs args)
        {
            return;
        }



        //static List<FileInfo> files = new List<FileInfo>();  // List that will hold the files and subfiles in path
        //static List<DirectoryInfo> folders = new List<DirectoryInfo>(); // List that hold direcotries that cannot be accessed
        //static FileItem FullDirList(DirectoryInfo dir, string searchPattern)
        //{
        //    FileItem folder1 = new FileItem() { Name = dir.Name, Type = ExplorerItem.ExplorerItemType.File };
        //    // Console.WriteLine("Directory {0}", dir.FullName);
        //    // list the files
        //    try
        //    {
        //        foreach (FileInfo f in dir.GetFiles(searchPattern))
        //        {
        //            //Console.WriteLine("File {0}", f.FullName);
        //            var n = new ExplorerItem() { Type = ExplorerItem.ExplorerItemType.File, Name = f.Name };
        //            folder1.Children.Add(n);
        //        }
        //    }
        //    catch
        //    {
        //        Console.WriteLine("Directory {0}  \n could not be accessed!!!!", dir.FullName);
        //        //return;  // We alredy got an error trying to access dir so dont try to access it again
        //    }

        //    // process each directory
        //    // If I have been able to see the files in the directory I should also be able 
        //    // to look at its directories so I dont think I should place this in a try catch block
        //    foreach (DirectoryInfo d in dir.GetDirectories())
        //    {
        //        var n = new ExplorerItem() { Type = ExplorerItem.ExplorerItemType.Folder, Name = d.Name };
        //        folder1.Children.Add(n);
        //        //FullDirList(d, searchPattern);
        //    }
        //    return folder1;

        //}

        

        private void sampleTreeView2_ItemInvoked(TreeView sender, TreeViewItemInvokedEventArgs args)
        {

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
                                    App.VM.ProjectList.Add(proj);
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
                                        var proj = new Project(folder.Name, folder, App.VM.GenerateTreeView(folder));
                                        App.VM.CurrentProject = proj;
                                        App.VM.ProjectList.Add(proj);

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
            App.VM.ProjectList.Remove(proj);
            //App.VM.UpdateRecentAccessList();
        }

        private async void BtnLoad_Click(object sender, RoutedEventArgs e)
        {
            try
            {

                var proj = (sender as FrameworkElement).DataContext as Project;
                var f = await StorageApplicationPermissions.MostRecentlyUsedList.GetFolderAsync(proj.Name);
                //App.AppViewModel.CurrentFolder = f;
                App.VM.CurrentProject = new Project(f.Name, f, App.VM.GenerateTreeView(f));

                App.VM.Default.LastActiveProject = proj.Name;
                App.VM.FileItems.Clear();
                var rf = StorageApplicationPermissions.MostRecentlyUsedList.Entries.Where(x => x.Token == proj.Name).FirstOrDefault();
                //App.VM.LOG("Loaded1" + rf.Token + rf.Metadata);
                //App.VM.LOG("Loaded2" + App.VM.Meta(rf.Metadata)["rootfile"]);
                //App.VM.CurrentProject.RootFile = App.VM.Meta(rf.Metadata)["rootfile"];
                //App.VM.LOG("Loaded2"+rf.Token+rf.Metadata);
            }
            catch (Exception ex)
            {
                App.VM.LOG(ex.Message);
            }
        }

        private void SetRoot_Click(object sender, RoutedEventArgs e)
        {

        }
    }
    
}
