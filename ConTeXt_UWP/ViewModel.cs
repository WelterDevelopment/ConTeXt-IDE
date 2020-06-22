using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using Windows.UI.Xaml.Controls;
using System.Threading.Tasks;
using Microsoft.Toolkit.Uwp.UI.Controls;
using Windows.ApplicationModel.Activation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Documents;
using Windows.ApplicationModel.AppService;
using Windows.UI.Xaml.Media;
using Windows.ApplicationModel.Background;
using System.Collections.Specialized;
using Windows.UI.Popups;
using System.IO;
using Windows.Storage;
using Windows.Storage.AccessCache;
using System.Diagnostics;
using Windows.UI.Text;
using Monaco.Editor;
using Windows.Foundation.Collections;
using System.Runtime.CompilerServices;
using Newtonsoft.Json;

namespace ConTeXt_UWP
{
    public class BoolInverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            bool invert = true;
            if (value is bool)
            {
                invert = !(bool)value;
            }
            return invert;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            bool invert = true;
            if (value is bool)
            {
                invert = !(bool)value;
            }
            return invert;
        }
    }

    public class BoolToFontWeightConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            FontWeight isroot = FontWeights.Normal;
            if (value is bool)
            {
                isroot = (bool)value ? FontWeights.Bold : FontWeights.Normal;
            }
            return isroot;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            return false;
        }
    }

    public class BoolToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            Visibility IsVisible = Visibility.Visible;
            if (!string.IsNullOrEmpty(value.ToString()))
            {
                IsVisible = (bool)value ? Visibility.Visible : Visibility.Collapsed;
            }
            //if((string)parameter == "tree" && IsVisible == Visibility.Visible)
            //{
            //    IsVisible =  App.AppViewModel.NVHeader == "Editor" ? Visibility.Visible : Visibility.Collapsed;
            //}
            return IsVisible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            return ((Visibility)value) == Visibility.Visible ? true : false;
        }
    }
    public class PaneVisibiltyToMarginConverter : IValueConverter
    {

        public object Convert(object value, Type targetType, object parameter, string language)
        {
            Visibility IsVisible = Visibility.Visible;
            if (!string.IsNullOrEmpty(value.ToString()))
            {
                IsVisible = (bool)value ? Visibility.Visible : Visibility.Collapsed;
            }
            return IsVisible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            return ((Visibility)value) == Visibility.Visible ? true : false;
        }
    }
    public class BoolToWidthConverter : IValueConverter
    {
        GridLength closed = new GridLength(0, GridUnitType.Pixel);
        GridLength open = new GridLength(1, GridUnitType.Star);
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            GridLength IsVisible = open;
            if (!string.IsNullOrEmpty(value.ToString()))
            {
                IsVisible = (bool)value ? open : closed;
            }
            return IsVisible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            return ((GridLength)value) == open ? true : false;
        }
    }

    //public class EnumToStringConverter : IValueConverter
    //{
    //    public object Convert(object value, Type targetType, object parameter, string language)
    //    {
    //        int val = (int)value;
    //        string enu = (string)parameter;
    //        switch (enu)
    //        {
    //            case "LineNumbersType":
    //                return Enum.GetName(typeof(LineNumbersType), val);
    //            default: return 0;

    //        }
    //    }

    //    public object ConvertBack(object value, Type targetType, object parameter, string language)
    //    {
    //        string val = (string)value;
    //        string enu = (string)parameter;
    //        switch (enu)
    //        {
    //            case "LineNumbersType":
    //                return (int)(LineNumbersType)Enum.Parse(typeof(LineNumbersType), val);
    //            default: return 0;

    //        }
    //    }
    //}

    public class ExplorerItemTemplateSelector : DataTemplateSelector
    {
        public DataTemplate FileTemplate { get; set; }
        public DataTemplate FolderTemplate { get; set; }
        protected override DataTemplate SelectTemplateCore(object item)
        {
            var explorerItem = (FileItem)item;
            return explorerItem.Type == FileItem.ExplorerItemType.Folder ? FolderTemplate : FileTemplate;
        }
    }

    public class FileItem : INotifyPropertyChanged
    {

        private IStorageItem file;

        private string fileContent;

        private string fileFolder;

        private string fileLanguage = "context";

        private string fileName;

        private string filePath;

        private bool isRoot;

        private ObservableCollection<FileItem> m_children = new ObservableCollection<FileItem>();

        private bool m_isExpanded;

        private bool m_isSelected;

        public static string GetFileLanguage(string ext)
        {
            switch (ext)
            {
                case ".tex": return "context";
                case ".mkiv": return "context";
                case ".mkii": return "context";
                case ".lua": return "lua";
                case ".json": return "javascript";
                case ".js": return "javascript";
                case ".md": return "markdown";
                case ".html": return "html";
                case ".xml": return "xml";
                default:
                    return "context";
            }
        }

        public FileItem(IStorageItem file, bool isRoot = false)
        {
            this.fileName = file != null ? file.Name : "";
            this.filePath = file != null ? file.Path : "";
            this.fileContent = "";
            this.isRoot = isRoot;
            this.file = file;
            this.fileFolder = file != null ? Path.GetDirectoryName(file.Path) : "";
            if (file != null && file is StorageFile)
                this.fileLanguage = GetFileLanguage(((StorageFile)file).FileType);

            if (Children != null)
            {
                // App.VM.LOG("AddedChildrenChngedHandler");
                Children.CollectionChanged += Children_CollectionChanged;
            }
        }

        private async void Children_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            try
            {
                if (App.VM.IsProjectLoaded)
                    if (e.Action == NotifyCollectionChangedAction.Add)
                    {
                        bool ischanged = false;
                        foreach (FileItem fi in e.NewItems)
                        {
                            if (fi.File is StorageFile fil && File is StorageFolder fold)
                            {
                                var parent = await fil.GetParentAsync();
                                if (parent.Path != fold.Path)
                                {
                                    await fil.MoveAsync(fold, fil.Name, NameCollisionOption.GenerateUniqueName);
                                    fi.FileFolder = Path.GetDirectoryName(fil.Path);
                                    //  fi.FilePath = fil.Path;
                                    App.VM.LOG("Moved " + fil.Name + " from " + parent.Name + " to " + fold.Name);
                                    ischanged = true;
                                }
                            }
                            else if (fi.File is StorageFolder fol && File is StorageFolder folcurr)
                            {
                                var parent = await fol.GetParentAsync();
                                if (parent.Path != folcurr.Path)
                                {
                                    App.VM.LOG("Moving Folders to Subfolders is currently not supported. Please do this operation in the Windows Explorer and reload the project.");
                                }
                            }

                        }
                        if (ischanged)
                        {
                            //Children.Sort((a,b)=> { return string.Compare(a.File.Name, b.File.Name); });
                        }

                    }
            }
            catch (Exception ex)
            {
                App.VM.LOG(ex.Message);
            }
        }


        public event PropertyChangedEventHandler PropertyChanged;
        public enum ExplorerItemType { Folder, File };
        public ObservableCollection<FileItem> Children
        {
            get
            {
                if (m_children == null)
                {
                    m_children = new ObservableCollection<FileItem>();
                }
                return m_children;
            }
            set
            {
                m_children = value;
            }
        }

        public IStorageItem File
        {
            get { return file; }
            set
            {
                if (file != value)
                {
                    file = value;
                    NotifyPropertyChanged("File");
                }
            }
        }

        public string FileContent
        {
            get { return fileContent; }
            set
            {
                if (value == fileContent)
                    return;
                fileContent = value;
                this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("FileContent"));
            }
        }

        public string FileFolder
        {
            get { return fileFolder; }
            set
            {
                fileFolder = value;
                this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("FileFolder"));
            }
        }

        public string FileLanguage
        {
            get { return fileLanguage; }
            set
            {
                fileLanguage = value;
                this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("FileLanguage"));
            }
        }

        public string FileName
        {
            get { return fileName; }
            set
            {
                fileName = value;
                this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("FileName"));
            }
        }

        //public string FilePath
        //{
        //    get { return filePath; }
        //    set
        //    {
        //        if (value == filePath)
        //            return;
        //        filePath = value;
        //        this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("FilePath"));
        //    }
        //}

        public bool IsExpanded
        {
            get { return m_isExpanded; }
            set
            {
                if (m_isExpanded != value)
                {
                    m_isExpanded = value;
                    NotifyPropertyChanged("IsExpanded");
                }
            }
        }

        public bool IsRoot
        {
            get { return isRoot; }
            set
            {
                if (isRoot != value)
                {
                    isRoot = value;
                    NotifyPropertyChanged("IsRoot");
                }
            }
        }

        public bool IsSelected
        {
            get { return m_isSelected; }

            set
            {
                if (m_isSelected != value)
                {
                    m_isSelected = value;
                    NotifyPropertyChanged("IsSelected");
                }
            }

        }
        // public string Name { get; set; }
        public ExplorerItemType Type { get; set; }
        private void NotifyPropertyChanged(String propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }

    public class NavViewToBoolConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            bool IsVisible = true;
            if (!string.IsNullOrEmpty(value.ToString()))
            {
                switch (value.ToString())
                {
                    case "Left": IsVisible = true; break;
                    case "Auto": IsVisible = true; break;
                    case "LeftCompact": IsVisible = true; break;
                    case "LeftMinimal": IsVisible = true; break;
                    case "Top": IsVisible = false; break;
                    default: IsVisible = true; break;
                }
            }
            return IsVisible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            return ((bool)value).ToString();
        }
    }

    public class NavViewToNotVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            Visibility IsVisible = Visibility.Visible;
            if (!string.IsNullOrEmpty(value.ToString()))
            {
                switch (value.ToString())
                {
                    case "Left": IsVisible = Visibility.Collapsed; break;
                    case "Auto": IsVisible = Visibility.Collapsed; break;
                    case "LeftCompact": IsVisible = Visibility.Collapsed; break;
                    case "LeftMinimal": IsVisible = Visibility.Collapsed; break;
                    case "Top": IsVisible = Visibility.Visible; break;
                    default: IsVisible = Visibility.Collapsed; break;
                }
            }
            return IsVisible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            return ((Visibility)value).ToString();
        }
    }

    public class NavViewToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            Visibility IsVisible = Visibility.Visible;
            if (!string.IsNullOrEmpty(value.ToString()))
            {
                switch (value.ToString())
                {
                    case "Left": IsVisible = Visibility.Visible; break;
                    case "Auto": IsVisible = Visibility.Visible; break;
                    case "LeftCompact": IsVisible = Visibility.Visible; break;
                    case "LeftMinimal": IsVisible = Visibility.Visible; break;
                    case "Top": IsVisible = Visibility.Collapsed; break;
                    default: IsVisible = Visibility.Visible; break;
                }
            }
            return IsVisible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            return ((Visibility)value).ToString();
        }
    }

    public class Project : INotifyPropertyChanged
    {
        private ObservableCollection<FileItem> directory;

        private StorageFolder folder;

        private string name;

        public Project(string name = "", StorageFolder folder = null, ObservableCollection<FileItem> directory = null)
        {
            this.directory = directory;
            this.name = name;
            this.folder = folder;
            if (Directory != null)
                Directory.CollectionChanged += Directory_CollectionChanged;
        }

        private async void Directory_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            try
            {
                if (App.VM.IsProjectLoaded)
                    if (e.Action == NotifyCollectionChangedAction.Add)
                    {
                        foreach (FileItem fi in e.NewItems)
                        {
                            if (fi.File is StorageFile file)
                            {
                                if (fi.FileFolder != Folder.Path)
                                {
                                    await file.MoveAsync(Folder, file.Name, NameCollisionOption.GenerateUniqueName);
                                    fi.FileFolder = Path.GetDirectoryName(file.Path);
                                }
                            }
                            else if (fi.File is StorageFolder fold)
                            {
                                // await fold.c
                            }
                        }
                    }
            }
            catch (Exception ex)
            {
                App.VM.LOG(ex.Message);
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [JsonIgnore]
        public ObservableCollection<FileItem> Directory
        {
            get { return directory; }
            set
            {
                if (value == directory)
                    return;
                directory = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Directory"));

            }
        }
        [JsonIgnore]
        public StorageFolder Folder
        {
            get { return folder; }
            set
            {
                if (value == folder)
                    return;
                folder = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Folder"));
            }
        }

        public string Name
        {
            get { return name; }
            set
            {
                if (value == name)
                    return;
                name = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Name"));
            }
        }

        private ObservableCollection<Mode> modes = new ObservableCollection<Mode>() { new Mode() { Name = "print", IsSelected = false }, new Mode() { Name = "screen", IsSelected = false } };
        public ObservableCollection<Mode> Modes
        {
            get { return modes; }
            set
            {
                if (value == modes)
                    return;
                modes = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Modes"));
            }
        }

        private string rootFile = null;
        public string RootFile
        {
            get { return rootFile; }
            set
            {
                if (value == rootFile)
                    return;
                rootFile = value;
                if (Directory != null)
                {
                    Directory.Where(x => x.FileName != value).ToList().ForEach(x => x.IsRoot = false);
                    var df = Directory.Where(x => x.FileName == value);
                    if (df.Count() == 1)
                    {
                        df.FirstOrDefault().IsRoot = true;
                    }

                    App.VM.LOG("Root file changed to " + RootFile);
                    App.VM.Default.SaveSettings();
                }
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("RootFile"));
            }
        }
    }

    public class StringToNavViewConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            NavigationViewPaneDisplayMode Mode = NavigationViewPaneDisplayMode.Left;
            if (!string.IsNullOrEmpty(value.ToString()))
            {
                switch (value.ToString())
                {
                    case "Left": Mode = NavigationViewPaneDisplayMode.Left; break;
                    case "Auto": Mode = NavigationViewPaneDisplayMode.Auto; break;
                    case "LeftCompact": Mode = NavigationViewPaneDisplayMode.LeftCompact; break;
                    case "LeftMinimal": Mode = NavigationViewPaneDisplayMode.LeftMinimal; break;
                    case "Top": Mode = NavigationViewPaneDisplayMode.Top; break;
                    default: Mode = NavigationViewPaneDisplayMode.Left; break;
                }
            }
            return Mode;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            return ((NavigationViewPaneDisplayMode)value).ToString();
        }
    }

    public class StringToThemeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            ElementTheme Mode = ElementTheme.Default;
            if (!string.IsNullOrEmpty(value.ToString()))
            {
                //var defTheme = ApplicationTheme.Dark;
                //var DefaultTheme = new Windows.UI.ViewManagement.UISettings();
                //var uiTheme = DefaultTheme.GetColorValue(Windows.UI.ViewManagement.UIColorType.Background).ToString();
                //if (uiTheme == "#FF000000")
                //{
                //    defTheme = ApplicationTheme.Dark;
                //}
                //else if (uiTheme == "#FFFFFFFF")
                //{
                //    defTheme = ApplicationTheme.Light;
                //}

                switch (value.ToString())
                {
                    case "Default": Mode = ElementTheme.Default; break;
                    case "Light": Mode = ElementTheme.Light; break;
                    case "Dark": Mode = ElementTheme.Dark; break;
                    default: Mode = ElementTheme.Default; break;
                }
            }

            return Mode;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            return ((ElementTheme)value).ToString();
        }
    }

    public class ViewModel : Bindable
    {
        public ObservableCollection<FileActivatedEventArgs> FileActivatedEvents = new ObservableCollection<FileActivatedEventArgs>() { };

        public ObservableCollection<Helpfile> HelpFiles { get; } = new ObservableCollection<Helpfile>() {
            new Helpfile() { FriendlyName = "Manual", FileName = "ma-cb-en.pdf", Path = @"\tex\texmf-context\doc\context\documents\general\manuals\" },
            new Helpfile() { FriendlyName = "Commands", FileName = "setup-en.pdf", Path = @"\tex\texmf-context\doc\context\documents\general\qrcs\" },

        };

        public Settings Default { get; } = Settings.Default;

        readonly List<string> cancelWords = new List<string> { ".gitignore", ".tuc", ".log", ".pgf", ".pdf" };

        private ObservableCollection<FileItem> list;


        public ViewModel()
        {
            FileItems = new ObservableCollection<FileItem>();
            CurrentFileItem = FileItems.Count > 0 ? FileItems.FirstOrDefault() : new FileItem(null);
            FileItems.CollectionChanged += FileItems_CollectionChanged1;
           // EditorOptions = new StandaloneEditorConstructionOptions();
        }

        private async void FileItems_CollectionChanged1(object sender, NotifyCollectionChangedEventArgs e)
        {

            if (e.Action == NotifyCollectionChangedAction.Add)
            {
                await Task.Delay(100);

                CurrentFileItem = e.NewItems[0] as FileItem;
            }
            if (FileItems.Count == 0)
            {
                IsFileItemLoaded = false;
            }
            else
            {
                IsFileItemLoaded = true;
            }
        }

        public AppServiceConnection appServiceConnection { get; set; }
        public BackgroundTaskDeferral AppServiceDeferral { get; set; }
        public string Blocks { get => Get<string>(); set => Set(value); }

        public Project CurrentProject { get => Get(new Project()); set { Set(value); if (CurrentProject.Folder != null) IsProjectLoaded = true; } }

        public ObservableCollection<FileItem> FileItems { get => Get(new ObservableCollection<FileItem>()); set => Set(value); }

        public FileItem CurrentFileItem { get => Get(new FileItem(null)); set => Set(value); }

        //public KeyValuePair<string, string> Help
        //{
        //    get { return this.help; }
        //    set { this.help = value; this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Help")); }
        //}

        //public ObservableCollection<KeyValuePair<string, string>> Helpfile { get; set; }

        public bool IsSaving { get => Get(false); set { Set(value); if (value) { IsError = false; IsVisible = true; } if (!value && !IsError) { IsVisible = false; } } }
        public bool IsFileItemLoaded { get => Get(false); set { Set(value); if (value) { IsError = false; IsVisible = false; } else { IsVisible = false; } } }

        public string SelectedPath { get => Get(""); set => Set(value); }

        public bool IsVisible { get => Get(true); set => Set(value); }

        public bool IsPaused { get => Get(false); set { Set(value); if (value) IsError = false; } }

        public bool IsError { get => Get(false); set => Set(value); }

        public bool IsProjectLoaded { get => Get(false); set => Set(value); }

        public string NVHead { get => Get(""); set => Set(value); }

        public StandaloneEditorConstructionOptions EditorOptions { get => Get<StandaloneEditorConstructionOptions>(); set => Set(value); }

        public StorageItemMostRecentlyUsedList RecentAccessList { get => Get<StorageItemMostRecentlyUsedList>(); set => Set(value); }

        private string rootFile;
        public ObservableCollection<FileItem> GenerateTreeView(StorageFolder folder, string rootfile)
        {
            rootFile = rootfile;
            list = new ObservableCollection<FileItem>();
            if (folder != null)
            {
                DirWalk(folder);

            }
            else
            {
                App.VM.LOG("Operation cancelled.");
            }
            return list;
        }

        public FileItem InitializeFileItem(StorageFile File, string Content = "", bool IsRoot = true)
        {
            return new FileItem(File, IsRoot) { FileContent = Content };
        }

        public void LOG(string log)
        {
            Blocks = log;
        }
        public async void Message(string content, string title = "Error")
        {
            ContentDialog md = new ContentDialog() { Content = content, Title = title, PrimaryButtonText = "ok" };
            await md.ShowAsync();
        }

        public async void OpenFile(FileItem File)
        {
            try
            {
                if (!FileItems.Contains(File))
                {
                    var read = await FileIO.ReadTextAsync((StorageFile)File.File);
                    File.FileContent = read;
                    LOG("Opening " + File.FileName);
                    FileItems.Add(File);
                }
                else
                {
                    CurrentFileItem = File;
                }
            }
            catch (Exception ex)
            {
                App.VM.LOG("Cannot open selected file: " + ex.Message);
            }
        }
        public async Task Save()
        {
            if (!IsSaving)
            {
                IsSaving = true;
                //App.AppViewModel.LOG(Tabs.SelectedItem.GetType().ToString());

                //ViewModel.Default.CodeContent = fi.FileContent;
                var buffer = Windows.Security.Cryptography.CryptographicBuffer.ConvertStringToBinary(CurrentFileItem.FileContent, Windows.Security.Cryptography.BinaryStringEncoding.Utf8);
                var file = await ApplicationData.Current.LocalFolder.CreateFileAsync(CurrentFileItem.FileName, CreationCollisionOption.ReplaceExisting);
                await FileIO.WriteBufferAsync(file, buffer);
                Default.TexFileName = CurrentFileItem.FileName;
                Default.TexFilePath = CurrentFileItem.File.Path;

                LOG("Saving");
                ValueSet request = new ValueSet
                {
                    { "save", true }
                };
                AppServiceResponse response = await appServiceConnection.SendMessageAsync(request);
                // display the response key/value pairs
                if (response != null)
                    foreach (string key in response.Message.Keys)
                    {
                        if ((string)response.Message[key] == "response")
                        {
                            LOG(key + " = " + response.Message[key]); LOG("Saved on " + DateTime.Now);
                        }
                    }

                //await Task.Delay(2000);
                IsSaving = false;
            }
            else LOG("already saving...");
        }

        public async Task UWPSaveAll()
        {

            try
            {
                if (!IsSaving && CurrentFileItem != null)
                {
                    if (CurrentFileItem.File != null)
                    {
                        IsSaving = true;
                        IsPaused = false;
                        foreach (var item in FileItems)
                        {
                            string cont = item.FileContent ?? " ";
                            var buffer = Windows.Security.Cryptography.CryptographicBuffer.ConvertStringToBinary(cont, Windows.Security.Cryptography.BinaryStringEncoding.Utf8);
                            await FileIO.WriteBufferAsync((StorageFile)item.File, buffer);
                        }
                        Default.TexFileName = CurrentFileItem.FileName;
                        Default.TexFilePath = CurrentFileItem.File.Path;
                        Default.TexFileFolder = CurrentFileItem.FileFolder;
                        IsSaving = false;
                        IsPaused = true;
                        IsVisible = false;
                    }
                }
                //else LOG("Error");
            }
            catch (Exception ex)
            {
                IsError = true;
                IsSaving = false;
                LOG("Error on Saving file: " + ex.Message);
            }



        }
        public async Task UWPSave()
        {
            if (CurrentFileItem != null)
                if (!IsSaving && CurrentFileItem.File != null)
                {
                    try
                    {
                        IsSaving = true;
                        IsPaused = false;
                        string cont = CurrentFileItem.FileContent ?? " ";
                        var buffer = Windows.Security.Cryptography.CryptographicBuffer.ConvertStringToBinary(cont, Windows.Security.Cryptography.BinaryStringEncoding.Utf8);
                        await FileIO.WriteBufferAsync(CurrentFileItem.File as StorageFile, buffer);
                        Default.TexFileName = CurrentFileItem.FileName;
                        Default.TexFilePath = CurrentFileItem.File.Path;
                        Default.TexFileFolder = CurrentFileItem.FileFolder;
                        IsSaving = false;
                        IsPaused = true;
                        IsVisible = false;
                    }
                    catch (Exception ex)
                    {
                        IsError = true;
                        IsSaving = false;
                        LOG("Error on Saving file: " + ex.Message);
                    }

                }
                else LOG("Error");

        }



        public Dictionary<string, string> Meta(string m)
        {
            return m.Split(';').Select(x => x.Split('=')).ToDictionary(x => x[0], x => x[1]);

        }

        public async void Startup()
        {
            try
            {
               // UpdateRecentAccessList();
                if (Default.StartWithLastActiveProject)
                {
                    RecentAccessList = StorageApplicationPermissions.MostRecentlyUsedList;
                    if (RecentAccessList.ContainsItem(Default.LastActiveProject))
                    {
                        IsSaving = true;
                        var folder = await RecentAccessList.GetFolderAsync(Default.LastActiveProject);
                        var f = RecentAccessList.Entries.Where(x => x.Token == folder.Name).FirstOrDefault();
                        var list = App.VM.Default.ProjectList.Where(x => x.Name == folder.Name);
                        if (list.Count() == 1)
                        {
                            var project = list.FirstOrDefault();
                            project.Folder = folder;
                            project.Directory = App.VM.GenerateTreeView(folder, project.RootFile);
                            App.VM.CurrentProject = project;
                            if (App.VM.CurrentProject.RootFile != null)
                            {
                                FileItem root = App.VM.CurrentProject.Directory.Where(x => x.IsRoot).FirstOrDefault();
                                if (root != null)
                                App.VM.OpenFile(root);
                            }
                        }
                        //CurrentProject = new Project(folder.Name, folder, GenerateTreeView(folder));
                        //CurrentProject.RootFile = Meta(f.Metadata).ContainsKey("rootfile") ? Meta(f.Metadata)["rootfile"] : null;

                        //Message(GenerateTreeView(folder).Count.ToString());
                        // LOG(CurrentProject.Folder.Path);
                        //LOG(CurrentProject.Directory.Count.ToString());
                        //var fileitem = CurrentProject.Directory.Where(x => x.FileName == Default.LastActiveFileName).FirstOrDefault();
                        //OpenFile(fileitem);
                        IsSaving = false;
                    }
                }
            }
            catch (Exception ex)
            {
                LOG("Error on ViewModel startup: " + ex.Message);
            }
        }

        public async void UpdateMRUEntry(Project prj)
        {
            StorageApplicationPermissions.MostRecentlyUsedList.AddOrReplace(prj.Name, prj.Folder, "rootfile=" + prj.RootFile);
            var entry = StorageApplicationPermissions.MostRecentlyUsedList.Entries.Where(x => x.Token == prj.Name).FirstOrDefault();
            LOG("Updated" + entry.Token + entry.Metadata);
        }

        public async void UpdateRecentAccessList()
        {
            App.VM.IsSaving = true;
            LOG("upd1");
            if (App.VM.Default.ProjectList == null) App.VM.Default.ProjectList = new ObservableCollection<Project>();
            if (App.VM.Default.ProjectList.Count == 0)
            {
                RecentAccessList = StorageApplicationPermissions.MostRecentlyUsedList;
                var accesslist = RecentAccessList.Entries;
                App.VM.Default.ProjectList.Clear();
                if (accesslist.Count() > 0)
                {
                    //Default.LastActiveProject = accesslist.FirstOrDefault().Token;
                    foreach (var accessitem in accesslist)
                    {
                        var folder = await RecentAccessList.GetFolderAsync(accessitem.Token);
                        //var tree = GenerateTreeView(folder);
                        App.VM.Default.ProjectList.Add(new Project(folder.Name, folder));
                    }
                }
            }
            App.VM.IsSaving = false;
        }
        //private void AddHelp()
        //{
        //    this.Helpfile = new ObservableCollection<KeyValuePair<string, string>>();
        //    Helpfile.Add(new KeyValuePair<string, string>("Dashboard", "Overview of your holdings and active Bots."));
        //    Helpfile.Add(new KeyValuePair<string, string>("MainPage", "This is a sample Text. This is a sample Text. \n This is a sample Text. \n This is a sample Text."));
        //    Helpfile.Add(new KeyValuePair<string, string>("APIKeys", "Here you can add your Coinbase Pro API Keys. The details needed for adding a key are:\n - Company (Only Coinbase.Pro is supported yet)\n - API Key\n - Secret Key\n - Passphrase\n\nOnly the Company, API Key and Name will ever appear in this App. To Change the secret data you need to delete the Key and add a new one."));
        //    Helpfile.Add(new KeyValuePair<string, string>("DayTrading", ""));


        //    Helpfile.Add(new KeyValuePair<string, string>("ActiveAccounts", "These are your wallets."));
        //    Helpfile.Add(new KeyValuePair<string, string>("LogView", ""));
        //    Helpfile.Add(new KeyValuePair<string, string>("ShowLog", "The Log is needed for troubleshooting purposes. Error messages will only appear in the Log."));
        //    Helpfile.Add(new KeyValuePair<string, string>("ShowHelp", "The Help Window may be needed when you first get in touch with the Software."));
        //    Helpfile.Add(new KeyValuePair<string, string>("Market", ""));
        //    Helpfile.Add(new KeyValuePair<string, string>("Limit", ""));
        //    Helpfile.Add(new KeyValuePair<string, string>("Stop", ""));
        //    Helpfile.Add(new KeyValuePair<string, string>("Amount", "Amount of the Currency you want to buy/sell.\nInsert a decimal number with your local decimal separator.\n - Use a decimal point in British / American English and Thai; e.g. 1.23\n - Use a decimal comma in most other languages; e.g. 1,23"));
        //    Helpfile.Add(new KeyValuePair<string, string>("Size", ""));
        //    Helpfile.Add(new KeyValuePair<string, string>("", ""));
        //}

        async void DirSearch(StorageFolder sDir, int level = 0)
        {
            try
            {
                foreach (StorageFolder d in await sDir.GetFoldersAsync())
                {
                    if (!d.Name.StartsWith("."))
                    {
                        FileItem SubFolder = new FileItem(d) { Type = FileItem.ExplorerItemType.Folder, FileName = d.Name, IsRoot = false };
                        foreach (StorageFile f in await d.GetFilesAsync())
                        {
                            if (!cancelWords.Contains(f.FileType))
                            {
                                SubFolder.Children.Add(new FileItem(f) { File = f, Type = FileItem.ExplorerItemType.File, FileName = f.Name, IsRoot = false });
                            }
                        }
                        list.Add(SubFolder);
                        DirSearch(d, level + 1);
                    }
                }
                if (level == 0)
                {
                    foreach (StorageFile f in await sDir.GetFilesAsync())
                    {
                        if (!cancelWords.Contains(f.FileType))
                        {
                            list.Add(new FileItem(f) { File = f, Type = FileItem.ExplorerItemType.File, FileName = f.Name, IsRoot = false });
                        }
                    }
                }
            }
            catch (Exception excpt)
            {
                Message("Error in generating the directory tree: " + excpt.Message);
            }
        }
        async void DirWalk(StorageFolder sDir, FileItem currFolder = null, int level = 0)
        {
            try
            {
                var folders = await sDir.GetFoldersAsync();
                var files = await sDir.GetFilesAsync();
                foreach (StorageFolder d in folders)
                {
                    if (!d.Name.StartsWith("."))
                    {
                        var SubFolder = new FileItem(d) { Type = FileItem.ExplorerItemType.Folder, FileName = d.Name, IsRoot = false };
                        if (level > 0)
                            currFolder.Children.Add(SubFolder);
                        else
                            list.Add(SubFolder);
                        DirWalk(d, SubFolder, level + 1);
                    }
                }
                foreach (StorageFile f in files)
                {
                    if (!f.Name.StartsWith(".") && !cancelWords.Contains(f.FileType))
                    {
                        var fi = new FileItem(f) { File = f, Type = FileItem.ExplorerItemType.File, FileName = f.Name, IsRoot = false };
                        if (level > 0)
                            currFolder.Children.Add(fi);
                        else
                        {
                            fi.IsRoot = fi.FileName == rootFile;
                            list.Add(fi);
                        }
                    }
                }
            }
            catch (Exception excpt)
            {
                Message("Error in generating the directory tree: " + excpt.Message);
            }
        }

    }
    public class Mode : Bindable
    {
        public string Name
        {
            get { return Get<string>(); }
            set { Set(value); }
        }
        public bool IsSelected
        {
            get { return Get<bool>(); }
            set { Set(value); }
        }
    }

    public class Helpfile : Bindable
    {
        public string FileName
        {
            get { return Get<string>(); }
            set { Set(value); }
        }
        public string FriendlyName
        {
            get { return Get<string>(); }
            set { Set(value); }
        }
        public string Path
        {
            get { return Get<string>(); }
            set { Set(value); }
        }

    }

    public class TemplateSelection : Bindable
    {
        public string Content
        {
            get { return Get<string>(); }
            set { Set(value); }
        }

        public string Tag
        {
            get { return Get<string>(); }
            set { Set(value); }
        }
        public bool IsSelected
        {
            get { return Get<bool>(); }
            set { Set(value); }
        }
    }
    public class Bindable : INotifyPropertyChanged
    {
        private Dictionary<string, object> _properties = new Dictionary<string, object>();

        /// <summary>
        /// Gets the value of a property
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="name"></param>
        /// <returns></returns>
        protected T Get<T>(T defaultVal = default, [CallerMemberName] string name = null)
        {
            Debug.Assert(name != null, "name != null");
            object value = null;
            if (!_properties.TryGetValue(name, out value))
            {
                value = _properties[name] = defaultVal;
            }
            return (T)value;
        }

        /// <summary>
        /// Sets the value of a property
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="value"></param>
        /// <param name="name"></param>
        /// <remarks>Use this overload when implicitly naming the property</remarks>
        protected void Set<T>(T value, [CallerMemberName] string name = null)
        {
            Debug.Assert(name != null, "name != null");
            if (Equals(value, Get<T>(value, name)))
                return;
            _properties[name] = value;
            OnPropertyChanged(name);
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }
    public static class Extensions
    {
        public static void Sort<T>(this ObservableCollection<T> collection, Comparison<T> comparison)
        {
            var sortableList = new List<T>(collection);
            sortableList.Sort(comparison);
            App.VM.LOG("sorting");
            for (int i = 0; i < sortableList.Count; i++)
            {
                collection.Move(collection.IndexOf(sortableList[i]), i);
            }
        }
    }
}