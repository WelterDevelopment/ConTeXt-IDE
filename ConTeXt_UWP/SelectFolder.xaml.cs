using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
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

// The Content Dialog item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace ConTeXt_UWP
{
    public sealed partial class SelectFolder : ContentDialog
    {
        ViewModel vm = App.VM;
        public SelectFolder()
        {
            App.VM.SelectedPath = "";
            this.InitializeComponent();
        }
      

        public StorageFolder folder;

        private async  void Button_Click(object sender, RoutedEventArgs e)
        {
            var folderPicker = new Windows.Storage.Pickers.FolderPicker();
            //folderPicker.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.Desktop;
            folderPicker.FileTypeFilter.Add("*");
            folderPicker.CommitButtonText = "Open";
            folderPicker.SettingsIdentifier = "ChooseWorkspace";
            folderPicker.ViewMode = Windows.Storage.Pickers.PickerViewMode.List;
            folder = await folderPicker.PickSingleFolderAsync();
            if (folder != null)
            {
                App.VM.SelectedPath = folder.Path;
                this.PrimaryButtonText = "Select";
            }
        }
    }
}
