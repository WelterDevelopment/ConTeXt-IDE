using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
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
        public ViewModel currentViewModel = App.AppViewModel;
        public Projects()
        {
            this.InitializeComponent();

        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            try
            {

                //App.AppViewModel.Projects.Clear();
                App.AppViewModel.UpdateRecentAccessList();
                
            }
            catch (Exception ex)
            {
                App.AppViewModel.LOG(ex.Message);
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
            var folderPicker = new Windows.Storage.Pickers.FolderPicker();
            //folderPicker.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.Desktop;
            folderPicker.FileTypeFilter.Add("*");
            folderPicker.CommitButtonText = "Open";
            folderPicker.SettingsIdentifier = "ChooseWorkspace";
            folderPicker.ViewMode = Windows.Storage.Pickers.PickerViewMode.List;
            StorageFolder folder = await folderPicker.PickSingleFolderAsync();
            if (folder != null)
            {
                StorageApplicationPermissions.FutureAccessList.AddOrReplace(folder.Name, folder, "folder");
                StorageApplicationPermissions.MostRecentlyUsedList.AddOrReplace(folder.Name, folder, "folder");
                App.AppViewModel.RecentAccessList = StorageApplicationPermissions.MostRecentlyUsedList;
                App.AppViewModel.CurrentProject = new Project(folder.Name, folder, App.AppViewModel.GenerateTreeView(folder));
               // App.AppViewModel.UpdateRecentAccessList();
            }
            
        }

        private void Btndeleteproject_Click(object sender, RoutedEventArgs e)
        {
            var proj = (sender as FrameworkElement).DataContext as Project;
            StorageApplicationPermissions.MostRecentlyUsedList.Remove(proj.Name);
            App.AppViewModel.UpdateRecentAccessList();
        }

        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            try
            {

                var proj = (sender as FrameworkElement).DataContext as Project;
                App.AppViewModel.LOG(proj.Name);
                var f = await StorageApplicationPermissions.MostRecentlyUsedList.GetFolderAsync(proj.Name);
                //App.AppViewModel.CurrentFolder = f;
                App.AppViewModel.CurrentProject = new Project(f.Name, f, App.AppViewModel.GenerateTreeView(f));
            }
            catch (Exception ex)
            {
                App.AppViewModel.LOG(ex.Message);
            }
        }

        private void SetRoot_Click(object sender, RoutedEventArgs e)
        {

        }
    }
    
}
