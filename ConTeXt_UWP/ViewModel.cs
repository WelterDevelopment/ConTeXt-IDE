using Monaco;
using Monaco.Editor;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Windows.ApplicationModel.Activation;
using Windows.ApplicationModel.AppService;
using Windows.ApplicationModel.Background;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.Storage.AccessCache;
using Windows.UI.Text;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;

namespace ConTeXt_UWP
{
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

    public class Bindable : INotifyPropertyChanged
    {
        private Dictionary<string, object> _properties = new Dictionary<string, object>();

        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Gets the value of a property
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="name"></param>
        /// <returns></returns>
        protected T Get<T>(T defaultVal = default, [CallerMemberName] string name = null)
        {
            object value = null;
            if (!_properties.TryGetValue(name, out value))
            {
                value = _properties[name] = defaultVal;
            }
            return (T)value;
        }

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(propertyName));
            }
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
    }

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

    public class BoolToWidthConverter : IValueConverter
    {
        private GridLength closed = new GridLength(0, GridUnitType.Pixel);
        private GridLength open = new GridLength(1, GridUnitType.Star);

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

    public class ConTeXtErrorMessage : Bindable
    {
        public string filename
        {
            get { return Get<string>(); }
            set { Set(value); }
        }

        public string lastcontext
        {
            get { return Get<string>(); }
            set { Set(value); }
        }

        public string lastluaerror
        {
            get { return Get<string>(); }
            set { Set(value); }
        }

        public string lasttexerror
        {
            get { return Get<string>(); }
            set { Set(value); }
        }

        public string lasttexhelp
        {
            get { return Get<string>(); }
            set { Set(value); }
        }

        public int linenumber
        {
            get { return Get(0); }
            set { Set(value); }
        }

        public int luaerrorline
        {
            get { return Get(0); }
            set { Set(value); }
        }

        public int offset
        {
            get { return Get(0); }
            set { Set(value); }
        }
    }

    public class ErrorStringToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            Visibility IsVisible = Visibility.Visible;
            if (value != null)
            {
                string str = value.ToString().Trim();
                if (str == "" | str == "?" | str == "0")
                    IsVisible = Visibility.Collapsed;
            }
            else
                IsVisible = Visibility.Collapsed;
            return IsVisible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            return ((Visibility)value).ToString();
        }
    }

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

    public class FileItem : Bindable
    {
        public FileItem(IStorageItem file, bool isRoot = false)
        {
            FileName = file != null ? file.Name : "";
            FileContent = LastSaveFileContent = "";
            IsRoot = isRoot;
            File = file;
            FileFolder = file != null ? Path.GetDirectoryName(file.Path) : "";
            if (file != null && file is StorageFile)
                FileLanguage = GetFileLanguage(((StorageFile)file).FileType);

            if (Children != null)
            {
                Children.CollectionChanged += Children_CollectionChanged;
            }
            IsLogFile = false;
        }

        public enum ExplorerItemType { Folder, File };

        public ObservableCollection<FileItem> Children
        {
            get { return Get(new ObservableCollection<FileItem>()); }
            set { Set(value); }
        }

        public ObservableCollection<OutlineItem> OutlineItems
        {
            get { return Get(new ObservableCollection<OutlineItem>()); }
            set { Set(value); }
        }

        public IStorageItem File
        {
            get { return Get<IStorageItem>(null); }
            set { Set(value); }
        }

        public string FileContent
        {
            get { return Get(""); }
            set
            {
                Set(value);
                if (!string.IsNullOrEmpty(value) && !string.IsNullOrEmpty(LastSaveFileContent))
                { 
                    IsChanged = value != LastSaveFileContent; 
                }
                if (App.VM.Default.ShowOutline)
                {
                    //App.VM.CurrentEditor.FindMatchesAsync(@"(\\start(sub)*?(section|subject|part|chapter|title)\s*?\[\s*?)(title\s*?=\s*?\{?)(.*?)\}?\s*?([,\]])", false, true, false, null, true, 20);
                    // var list = await editor.FindMatchesAsync(@"(\\start(sub)*?(section|subject|part|chapter|title)\s*?\[\s*?)(title\s*?=\s*?\{?)(.*?)\}?\s*?([,\]])", false, true, false, null, true, 20);
                }
            }
        }

        public string FileFolder
        {
            get { return Get(""); }
            set { Set(value); }
        }
        public string FileLanguage
        {
            get { return Get("context"); }
            set { Set(value); }
        }

        public string FileName
        {
            get { return Get(""); }
            set { Set(value); }
        }

        public bool IsChanged
        {
            get { return Get(false); }
            set { Set(value); }
        }

        public bool IsExpanded
        {
            get { return Get(false); }
            set { Set(value); }
        }

        public bool IsLogFile
        {
            get { return Get(false); }
            set { Set(value); }
        }

        public bool IsRoot
        {
            get { return Get(false); }
            set { Set(value); }
        }

        public bool IsSelected
        {
            get { return Get(false); }
            set { Set(value); }
        }

        public string LastSaveFileContent
        {
            get { return Get(""); }
            set { Set(value); }
        }

        public ExplorerItemType Type
        {
            get { return Get(ExplorerItemType.File); }
            set { Set(value); }
        }


        public static string GetFileLanguage(string ext)
        {
            switch (ext)
            {
                case ".tex": return "context";
                case ".mkiv": return "context";
                case ".mkii": return "context";
                case ".mkxl": return "context";
                case ".mkvi": return "context";
                case ".lua": return "lua";
                case ".json": return "javascript";
                case ".js": return "javascript";
                case ".md": return "markdown";
                case ".html": return "html";
                case ".xml": return "xml";
                case ".log": return "log";
                default:
                    return "context";
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

    public class Mode : Bindable
    {
        public bool IsSelected
        {
            get { return Get<bool>(); }
            set { Set(value); }
        }

        public string Name
        {
            get { return Get<string>(); }
            set { Set(value); }
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

    public class Project : INotifyPropertyChanged
    {
        private ObservableCollection<FileItem> directory;

        private StorageFolder folder;

        private ObservableCollection<Mode> modes = new ObservableCollection<Mode>() { new Mode() { Name = "print", IsSelected = false }, new Mode() { Name = "screen", IsSelected = false } };
        private string name;

        private string rootFile = null;

        public Project(string name = "", StorageFolder folder = null, ObservableCollection<FileItem> directory = null)
        {
            this.directory = directory;
            this.name = name;
            this.folder = folder;
            if (Directory != null)
                Directory.CollectionChanged += Directory_CollectionChanged;
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
    }

    //        }
    //    }
    public class StringComparer : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            string currenttext = value as string;

            if (string.IsNullOrEmpty(currenttext) | string.IsNullOrEmpty(App.VM.CurrentFileItem.LastSaveFileContent))
                return Visibility.Collapsed;

            return currenttext == App.VM.CurrentFileItem.LastSaveFileContent ? Visibility.Collapsed : Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            return ((bool)value).ToString();
        }
    }

    public class StringToVisibility : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            string currenttext = value as string;

            if (string.IsNullOrEmpty(currenttext))
                return Visibility.Collapsed;
            else
                return Visibility.Visible;

        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            return ((bool)value).ToString();
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

    public class TemplateSelection : Bindable
    {
        public string Content
        {
            get { return Get<string>(); }
            set { Set(value); }
        }

        public bool IsSelected
        {
            get { return Get<bool>(); }
            set { Set(value); }
        }

        public string Tag
        {
            get { return Get<string>(); }
            set { Set(value); }
        }
    }

    public class OutlineItem : Bindable
    {
        public string Title
        {
            get { return Get<string>(); }
            set { Set(value); }
        }

        public int Row
        {
            get { return Get<int>(); }
            set { Set(value); }
        }

    }

    public class ViewModel : Bindable
    {
        public ObservableCollection<FileActivatedEventArgs> FileActivatedEvents = new ObservableCollection<FileActivatedEventArgs>() { };

        private readonly List<string> cancelWords = new List<string> { ".gitignore", ".tuc", ".log", ".pgf", ".pdf" };

        private ObservableCollection<FileItem> list;

        private string rootFile;

        //public CodeEditor CurrentEditor { get => Get<CodeEditor>(null); set => Set(value); }

        public ViewModel()
        {
            Default = Settings.Default;
            FileItems = new ObservableCollection<FileItem>();
            CurrentFileItem = FileItems.Count > 0 ? FileItems.FirstOrDefault() : new FileItem(null);
            FileItems.CollectionChanged += FileItems_CollectionChanged1;
            try
            {
                //var ce = new CodeEditor();
                //EditorOptions = ce.Options;
                EditorOptions.DetectIndentation = false;
                EditorOptions.UseTabStops = true;
                EditorOptions.TabSize = 2;
                EditorOptions.CopyWithSyntaxHighlighting = true;
                EditorOptions.InsertSpaces = false;
                EditorOptions.WordWrap = "on";
                EditorOptions.WordBasedSuggestions = false;
                EditorOptions.SuggestOnTriggerCharacters = true;
                EditorOptions.AcceptSuggestionOnCommitCharacter = true;
                EditorOptions.SuggestSelection = "recentlyUsed";
                EditorOptions.WrappingIndent = "indent";
                EditorOptions.AutoIndent = "keep";
                EditorOptions.CodeLens = true;
                EditorOptions.Contextmenu = true;
                EditorOptions.ParameterHints = new IEditorParameterHintOptions() { Cycle = false, Enabled = true };
                EditorOptions.Minimap = new EditorMinimapOptions() { Enabled = Default.MiniMap, ShowSlider = "always", RenderCharacters = true, };
                EditorOptions.CursorBlinking = "solid";
                EditorOptions.DragAndDrop = true;
                EditorOptions.ScrollBeyondLastLine = false;
                EditorOptions.Folding = Default.CodeFolding;
                EditorOptions.FoldingStrategy = "auto";
                EditorOptions.FormatOnPaste = true;
                EditorOptions.Hover = new EditorHoverOptions() { Enabled = Default.Hover, Delay = 100, Sticky = true };
                EditorOptions.LineNumbers = Default.ShowLineNumbers;
                EditorOptions.RenderControlCharacters = true;
                EditorOptions.QuickSuggestions = true;
                EditorOptions.SnippetSuggestions = "inline";
                EditorOptions.Links = true;
                EditorOptions.MouseWheelZoom = true;
                EditorOptions.OccurrencesHighlight = false;
                EditorOptions.RoundedSelection = true;
            }
            catch (Exception ex)
            {
                LOG(ex.Message);
            }
        }

        public AppServiceConnection appServiceConnection { get; set; }

        public BackgroundTaskDeferral AppServiceDeferral { get; set; }

        public string Blocks { get => Get<string>(); set => Set(value); }

        public ConTeXtErrorMessage ConTeXtErrorMessage { get => Get(new ConTeXtErrorMessage()); set => Set(value); }

        public FileItem CurrentFileItem { get => Get(new FileItem(null)); set => Set(value); }

        public Project CurrentProject { get => Get(new Project()); set { Set(value); IsProjectLoaded = CurrentProject.Folder != null; } }

        public Settings Default { get; set; }

        public StandaloneEditorConstructionOptions EditorOptions { get => Get(new StandaloneEditorConstructionOptions()); set => Set(value); }

        public ObservableCollection<FileItem> FileItems { get => Get(new ObservableCollection<FileItem>()); set => Set(value); }

        public ObservableCollection<Helpfile> HelpFiles { get; } = new ObservableCollection<Helpfile>() {
            new Helpfile() { FriendlyName = "Manual", FileName = "ma-cb-en.pdf", Path = @"\tex\texmf-context\doc\context\documents\general\manuals\" },
            new Helpfile() { FriendlyName = "Commands", FileName = "setup-en.pdf", Path = @"\tex\texmf-context\doc\context\documents\general\qrcs\" },
        };

        public bool IsError { get => Get(false); set => Set(value); }

        public bool Modes { get => IsProjectLoaded && Default.UseModes; set => Set(value); }

        public bool IsFileItemLoaded { get => Get(false); set { Set(value); if (value) { IsVisible = false; } else { IsVisible = false; } } }

        public bool IsPaused { get => Get(false); set { Set(value); } }

        public bool IsInstalled { get => Get(false); set { Set(value); } }

        public bool IsProjectLoaded { get => Get(false); set => Set(value); }

        public bool IsSaving { get => Get(false); set { Set(value); if (value) { IsVisible = true; } if (!value && !IsError) { IsVisible = false; } } }

        public bool IsVisible { get => Get(true); set => Set(value); }

        public string NVHead { get => Get(""); set => Set(value); }

        public StorageItemMostRecentlyUsedList RecentAccessList { get => Get<StorageItemMostRecentlyUsedList>(); set => Set(value); }

        //public ObservableCollection<KeyValuePair<string, string>> Helpfile { get; set; }
        public string SelectedPath { get => Get(""); set => Set(value); }

        //public KeyValuePair<string, string> Help
        //{
        //    get { return this.help; }
        //    set { this.help = value; this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Help")); }
        //}
        public ObservableCollection<FileItem> GenerateTreeView(StorageFolder folder, string rootfile = null)
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

        public Dictionary<string, string> Meta(string m)
        {
            return m.Split(';').Select(x => x.Split('=')).ToDictionary(x => x[0], x => x[1]);
        }

        public async void OpenFile(FileItem File)
        {
            try
            {
                if (!FileItems.Contains(File))
                {
                    var openfile = FileItems.Where(x => x.File.Path == File.File.Path);


                    var read = await FileIO.ReadTextAsync((StorageFile)File.File);
                    File.LastSaveFileContent = read;

                    //await Task.Delay(500);

                    File.FileContent = read;

                    LOG("Opening " + File.FileName);
                    if (openfile.Count() > 0)
                    {
                        if (CurrentFileItem == openfile.FirstOrDefault())
                        {
                            FileItems[FileItems.IndexOf(openfile.FirstOrDefault())] = File;
                            //CurrentFileItem = File;
                        }
                        else
                        {
                            FileItems[FileItems.IndexOf(openfile.FirstOrDefault())] = File;
                            //CurrentFileItem = File;
                        }
                    }
                    else
                        FileItems.Add(File);

                    await Task.Delay(500);
                    CurrentFileItem = File;

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
                        //var f = RecentAccessList.Entries.Where(x => x.Token == folder.Name).FirstOrDefault();
                        var list = App.VM.Default.ProjectList.Where(x => x.Name == folder.Name);
                        if (list != null && list.Count() == 1)
                        {
                            var project = list.FirstOrDefault();
                            project.Folder = folder;
                            project.Directory = App.VM.GenerateTreeView(folder, project.RootFile);
                            App.VM.CurrentProject = project;
                            if (App.VM.CurrentProject.RootFile != null)
                            {
                                await Task.Delay(500);
                                FileItem root = App.VM.CurrentProject.Directory.Where(x => x.IsRoot == true).FirstOrDefault();
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
                IsSaving = false;
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

        public async Task UWPSave(FileItem fileItem = null)
        {
            FileItem filetosave = fileItem ?? CurrentFileItem;

            if (filetosave != null)
                if (!IsSaving && filetosave.File != null)
                {
                    try
                    {
                        IsSaving = true;
                        IsPaused = false;
                        string cont = filetosave.FileContent ?? " ";
                        var buffer = Windows.Security.Cryptography.CryptographicBuffer.ConvertStringToBinary(cont, Windows.Security.Cryptography.BinaryStringEncoding.Utf8);
                        await FileIO.WriteBufferAsync(CurrentFileItem.File as StorageFile, buffer);
                        Default.TexFileName = filetosave.FileName;
                        Default.TexFilePath = filetosave.File.Path;
                        Default.TexFileFolder = filetosave.FileFolder;
                        filetosave.LastSaveFileContent = filetosave.FileContent;
                        filetosave.IsChanged = false;
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
                            item.LastSaveFileContent = item.FileContent;
                            item.IsChanged = false;
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

        private async void DirSearch(StorageFolder sDir, int level = 0)
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

        private async void DirWalk(StorageFolder sDir, FileItem currFolder = null, int level = 0)
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

        private async void FileItems_CollectionChanged1(object sender, NotifyCollectionChangedEventArgs e)
        {
            //if (e.Action == NotifyCollectionChangedAction.Add)
            //{
            //    await Task.Delay(500);

            //    CurrentFileItem = e.NewItems[0] as FileItem;
            //}
            if (FileItems.Count == 0)
            {
                IsFileItemLoaded = false;
            }
            else
            {
                IsFileItemLoaded = true;
            }
        }
    }
}