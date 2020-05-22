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

    public class CompileMode : INotifyPropertyChanged
    {

        private string command;

        private string key;

        private string name;

        private string passphrase;

        private string privatekey;

        public CompileMode(string Name, string Command)
        {
            name = Name;
            command = Command;

        }

        public event PropertyChangedEventHandler PropertyChanged;
        public string Command
        {
            get { return command; }
            set { command = value; }
        }

        public string Name
        {
            get { return name; }
            set
            {
                if (value == name)
                    return;
                name = value;
                this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Name"));
            }
        }
    }

    //public class ExplorerItem : INotifyPropertyChanged
    //{
    //    private StorageFile file;

    //    private bool isRoot;

    //    private ObservableCollection<ExplorerItem> m_children;

    //    private bool m_isExpanded;

    //    private bool m_isSelected;

    //    private string name;

    //    private string path;

    //    public event PropertyChangedEventHandler PropertyChanged;
    //    public enum ExplorerItemType { Folder, File };
    //    public ObservableCollection<ExplorerItem> Children
    //    {
    //        get
    //        {
    //            if (m_children == null)
    //            {
    //                m_children = new ObservableCollection<ExplorerItem>();
    //            }
    //            return m_children;
    //        }
    //        set
    //        {
    //            m_children = value;
    //        }
    //    }

    //    public StorageFile File
    //    {
    //        get { return file; }
    //        set
    //        {
    //            if (file != value)
    //            {
    //                file = value;
    //                NotifyPropertyChanged("File");
    //            }
    //        }
    //    }

    //    public bool IsExpanded
    //    {
    //        get { return m_isExpanded; }
    //        set
    //        {
    //            if (m_isExpanded != value)
    //            {
    //                m_isExpanded = value;
    //                NotifyPropertyChanged("IsExpanded");
    //            }
    //        }
    //    }

    //    public bool IsRoot
    //    {
    //        get { return isRoot; }
    //        set
    //        {
    //            if (isRoot != value)
    //            {
    //                isRoot = value;
    //                NotifyPropertyChanged("IsRoot");
    //            }
    //        }
    //    }

    //    public bool IsSelected
    //    {
    //        get { return m_isSelected; }

    //        set
    //        {
    //            if (m_isSelected != value)
    //            {
    //                m_isSelected = value;
    //                NotifyPropertyChanged("IsSelected");
    //            }
    //        }

    //    }

    //    public string Name
    //    {
    //        get { return name; }
    //        set
    //        {
    //            if (name != value)
    //            {
    //                name = value;
    //                NotifyPropertyChanged("Name");
    //            }
    //        }
    //    }

    //    public string Path
    //    {
    //        get { return path; }
    //        set
    //        {
    //            if (path != value)
    //            {
    //                path = value;
    //                NotifyPropertyChanged("Path");
    //            }
    //        }
    //    }

    //    // public string Name { get; set; }
    //    public ExplorerItemType Type { get; set; }
    //    private void NotifyPropertyChanged(String propertyName)
    //    {
    //        if (PropertyChanged != null)
    //        {
    //            PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
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

        private string fileLanguage;

        private string fileName;

        private string filePath;

        private bool isRoot;

        private ObservableCollection<FileItem> m_children;

        private bool m_isExpanded;

        private bool m_isSelected;

        public FileItem(IStorageItem file, bool isRoot = false)
        {
            this.fileName = file.Name;
            this.filePath = file.Path;
            this.fileContent = "";
            this.isRoot = isRoot;
            this.file = file;
            this.fileFolder = Path.GetDirectoryName(file.Path);
            if (file is StorageFile)
            switch (((StorageFile)file).FileType)
            {
                case ".tex": this.fileLanguage = "context"; break;
                case ".lua": this.fileLanguage = "lua"; break;
                case ".json": this.fileLanguage = "javascript"; break;
                case ".md": this.fileLanguage = "markdown"; break;
                case ".html": this.fileLanguage = "html"; break;
                case ".xml": this.fileLanguage = "xml"; break;
                default: this.fileLanguage = "context"; break;
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
                if (value == fileFolder)
                    return;
                fileFolder = value;
                this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("FileFolder"));
            }
        }

        public string FileLanguage
        {
            get { return fileLanguage; }
            set
            {
                if (value == fileLanguage)
                    return;
                fileLanguage = value;
                this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("FileLanguage"));
            }
        }

        public string FileName
        {
            get { return fileName; }
            set
            {
                if (value == fileName)
                    return;
                fileName = value;
                this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("FileName"));
            }
        }

        public string FilePath
        {
            get { return filePath; }
            set
            {
                if (value == filePath)
                    return;
                filePath = value;
                this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("FilePath"));
            }
        }

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
        }

        public event PropertyChangedEventHandler PropertyChanged;
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
        private FileItem rootFile;
        public FileItem RootFile
        {
            get { return rootFile; }
            set
            {
                if (value == rootFile)
                    return;
                rootFile = value;
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

    public class ViewModel : INotifyPropertyChanged
    {
        public ObservableCollection<FileActivatedEventArgs> FileActivatedEvents = new ObservableCollection<FileActivatedEventArgs>() { };

        public ObservableCollection<CompileMode> ModeList = new ObservableCollection<CompileMode>() { new CompileMode("testname", "testcommand"), new CompileMode("bla", "blub") };

        public Settings settings = new Settings();

        readonly List<string> cancelWords = new List<string> { ".gitignore", ".tuc", ".log", ".pgf", ".pdf" };

        private string blocks;

        private ObservableCollection<FileItem> fileItems = new ObservableCollection<FileItem>();

        private KeyValuePair<string, string> help;

        private ObservableCollection<FileItem> list;

        private string nVHeader;

        private ObservableCollection<Project> projectList = new ObservableCollection<Project>();

        private StorageItemMostRecentlyUsedList recentAccessList;

        public ViewModel()
        {

            FileItems = new ObservableCollection<FileItem>();
            FileItems.CollectionChanged += FileItems_CollectionChanged;
            CurrentFileItem = FileItems.Count > 0 ? FileItems.FirstOrDefault() : null;
            //FileItems.Add(new FileItem("bla","blub","content",true));
            TabViewItems = new ObservableCollection<TabViewItem>();
            IsNotSaving = true;
            //ModeList = new ObservableCollection<string>(new List<string>() { "sample1", "sample2" });
            //AddHelp();

        }

        public event PropertyChangedEventHandler PropertyChanged;
        //public ObservableCollection<Page> PageList { get; set; }
        public AppServiceConnection appServiceConnection { get; set; }
        public BackgroundTaskDeferral AppServiceDeferral { get; set; }
        public string Blocks
        {
            get { return blocks; }
            set
            {
                if (value == blocks)
                    return;
                blocks = value;
                this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Blocks"));

            }
        }

        public Project CurrentProject
        {
            get { return currentProject == null ? new Project() : currentProject; }
            set
            {
                if (value == currentProject)
                    return;
                currentProject = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("CurrentProject"));
            }
        }

        public Settings Default
        {
            get { return settings; }
        }

        public ObservableCollection<FileItem> FileItems
        {
            get { return fileItems; }
            set
            {
                if (value == fileItems)
                    return;
                fileItems = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("FileItems"));
            }
        }

        private FileItem currentFileItem;
        public FileItem CurrentFileItem
        {
            get { return currentFileItem; }
            set
            {
                if (value == currentFileItem)
                    return;
                currentFileItem = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("CurrentFileItem"));
            }
        }

        //public StorageFolder CurrentFolder { get; set; }
        public KeyValuePair<string, string> Help
        {
            get { return this.help; }
            set { this.help = value; this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Help")); }
        }

        public ObservableCollection<KeyValuePair<string, string>> Helpfile { get; set; }
        public bool IsNotSaving { get; set; }



        //public string CodeContent { get; set; }
        public string NVHeader
        {
            get { return nVHeader; }
            set { nVHeader = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("NVHeader")); }
        }

        public ObservableCollection<Project> ProjectList
        {
            get { return projectList; }
            set
            {
                if (value == projectList)
                    return;
                projectList = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("ProjectList"));
            }
        }

        public StorageItemMostRecentlyUsedList RecentAccessList
        {
            get { return recentAccessList; }
            set
            {
                if (value == recentAccessList)
                    return;
                recentAccessList = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("RecentAccessList"));
            }
        }

        //public TabViewList TabViewList { get; set; }
        public ObservableCollection<TabViewItem> TabViewItems { get; set; }

        private Project currentProject { get; set; }

        public ObservableCollection<FileItem> GenerateTreeView(StorageFolder folder)
        {
            list = new ObservableCollection<FileItem>();
            if (folder != null)
            {
                DirSearch(folder);
                LOG("Picked folder: " + folder.Name);
            }
            else
            {
                Message("Operation cancelled.");
            }
            return list;
        }

        public FileItem InitializeFileItem(StorageFile File, string Content = "", bool IsRoot = true)
        {
            return new FileItem(File, IsRoot) { FileContent=Content };
        }

        public void LOG(string log)
        {
            Blocks = log;
            Debug.WriteLine("LOG: "+log);
        }
        public async void Message(string content, string title = "Error")
        {
            ContentDialog md = new ContentDialog() { Content = content, Title = title, PrimaryButtonText = "ok" };
            await md.ShowAsync();
        }

        public async void OpenFile(FileItem File)
        {
            var read = await FileIO.ReadTextAsync((StorageFile)File.File);
            Default.TexFileName = File.FileName;
            Default.TexFileFolder = Path.GetDirectoryName(File.FilePath);
            Default.TexFilePath = File.FilePath;
            File.FileContent = read;

            LOG(File.FileName + " opened.");
            FileItems.Add(File);
            CurrentFileItem = File;
        }

        public async void Startup()
        {
            try
            {
                if (Default.StartWithLastActiveProject)
                {
                    RecentAccessList = StorageApplicationPermissions.MostRecentlyUsedList;
                    if (RecentAccessList.ContainsItem(Default.LastActiveProject))
                    {
                        var folder = await RecentAccessList.GetFolderAsync(Default.LastActiveProject);
                        CurrentProject = new Project(folder.Name, folder, GenerateTreeView(folder));
                        //Message(GenerateTreeView(folder).Count.ToString());
                       // LOG(CurrentProject.Folder.Path);
                        //LOG(CurrentProject.Directory.Count.ToString());
                        //var fileitem = CurrentProject.Directory.Where(x => x.FileName == Default.LastActiveFileName).FirstOrDefault();
                        //OpenFile(fileitem);
                    }
                }
            }
            catch (Exception ex)
            {
                LOG(ex.Message);
            }
        }

        public async void UpdateRecentAccessList()
        {
            RecentAccessList = StorageApplicationPermissions.MostRecentlyUsedList;
            var accesslist = RecentAccessList.Entries.Where(x => x.Metadata == "folder");
            ProjectList.Clear();
            if (accesslist.Count() > 0)
            {
                Default.LastActiveProject = accesslist.FirstOrDefault().Token;
                foreach (var accessitem in accesslist)
                {
                    var folder = await RecentAccessList.GetFolderAsync(accessitem.Token);
                    var tree = GenerateTreeView(folder);
                    ProjectList.Add(new Project(folder.Name, folder, tree));
                }
            }
        }
        private void AddHelp()
        {
            this.Helpfile = new ObservableCollection<KeyValuePair<string, string>>();
            Helpfile.Add(new KeyValuePair<string, string>("Dashboard", "Overview of your holdings and active Bots."));
            Helpfile.Add(new KeyValuePair<string, string>("MainPage", "This is a sample Text. This is a sample Text. \n This is a sample Text. \n This is a sample Text."));
            Helpfile.Add(new KeyValuePair<string, string>("APIKeys", "Here you can add your Coinbase Pro API Keys. The details needed for adding a key are:\n - Company (Only Coinbase.Pro is supported yet)\n - API Key\n - Secret Key\n - Passphrase\n\nOnly the Company, API Key and Name will ever appear in this App. To Change the secret data you need to delete the Key and add a new one."));
            Helpfile.Add(new KeyValuePair<string, string>("DayTrading", ""));


            Helpfile.Add(new KeyValuePair<string, string>("ActiveAccounts", "These are your wallets."));
            Helpfile.Add(new KeyValuePair<string, string>("LogView", ""));
            Helpfile.Add(new KeyValuePair<string, string>("ShowLog", "The Log is needed for troubleshooting purposes. Error messages will only appear in the Log."));
            Helpfile.Add(new KeyValuePair<string, string>("ShowHelp", "The Help Window may be needed when you first get in touch with the Software."));
            Helpfile.Add(new KeyValuePair<string, string>("Market", ""));
            Helpfile.Add(new KeyValuePair<string, string>("Limit", ""));
            Helpfile.Add(new KeyValuePair<string, string>("Stop", ""));
            Helpfile.Add(new KeyValuePair<string, string>("Amount", "Amount of the Currency you want to buy/sell.\nInsert a decimal number with your local decimal separator.\n - Use a decimal point in British / American English and Thai; e.g. 1.23\n - Use a decimal comma in most other languages; e.g. 1,23"));
            Helpfile.Add(new KeyValuePair<string, string>("Size", ""));
            Helpfile.Add(new KeyValuePair<string, string>("", ""));
        }

        async void DirSearch(StorageFolder sDir, int level = 0)
        {
            try
            {
                foreach (StorageFolder d in await sDir.GetFoldersAsync())
                {
                    if (!d.Name.StartsWith("."))
                    {
                        FileItem SubFolder = new FileItem(d) { Type = FileItem.ExplorerItemType.Folder, FileName = d.Name, IsRoot = true };
                        foreach (StorageFile f in await d.GetFilesAsync())
                        {
                            if (!cancelWords.Contains(f.FileType))
                            {
                                SubFolder.Children.Add(new FileItem(f) { File = f, FilePath = f.Path, Type = FileItem.ExplorerItemType.File, FileName = f.Name, IsRoot = false });
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
                            list.Add(new FileItem(f) { File = f, FilePath = f.Path, Type = FileItem.ExplorerItemType.File, FileName = f.Name, IsRoot = false });
                        }
                    }
                }
            }
            catch (Exception excpt)
            {
                Message(excpt.Message);
            }
        }

        private void FileItems_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    LOG("File opened.");
                    break;
                default:break;
            }
        }
    }
}