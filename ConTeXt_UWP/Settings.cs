using Microsoft.UI.Xaml.Controls;
using Monaco.Editor;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using Windows.Storage;

namespace ConTeXt_UWP
{
    public static class SettingsExtensions
    {
        public static void SaveSettings(this Settings settings)
        {
            string file = "settings.json";
            var storageFolder = ApplicationData.Current.LocalFolder;
            string settingsPath = Path.Combine(storageFolder.Path, file);
            string json = settings.ToJson();
            File.WriteAllText(settingsPath, json);
        }
    }
    public class Settings : INotifyPropertyChanged
    {
        [JsonIgnore]
        public static Settings Default { get; } = GetSettings();

        //public static Settings RestoreSettings()
        //{
        //    string file = "settings.json";
        //    var storageFolder = ApplicationData.Current.LocalFolder;
        //    string settingsPath = Path.Combine(storageFolder.Path, file);
        //    if (File.Exists(settingsPath))
        //    {
        //        File.Delete(settingsPath);
        //    }

        //        return GetSettings();
        //}

            private static Settings GetSettings()
        {
            string file = "settings.json";
            var storageFolder = ApplicationData.Current.LocalFolder;
            string settingsPath = Path.Combine(storageFolder.Path, file);
            Settings settings;
            if (!File.Exists(settingsPath))
            {
                settings = new Settings();
                string json = settings.ToJson();
                File.WriteAllText(settingsPath, json);
            }
            else
            {
                string json = File.ReadAllText(settingsPath);
                settings = FromJson(json);
            }

            settings.PropertyChanged += (o, a) =>
            {
                string json = settings.ToJson();
                File.WriteAllText(settingsPath, json);
            };

            settings.ProjectList.CollectionChanged += (o, a) =>
            {
                string json = settings.ToJson();
                File.WriteAllText(settingsPath, json);
            };
           
            return settings;

        }

        
        bool startWithLastActiveProject = true;
        public bool StartWithLastActiveProject { get => startWithLastActiveProject; set => Set(ref startWithLastActiveProject, value); }

        string showLineNumbers = "on";
        public string ShowLineNumbers{ get => showLineNumbers; set { Set(ref showLineNumbers, value); if (App.VM.EditorOptions != null) App.VM.EditorOptions.LineNumbers = value; } }

        bool showLog = false;
        public bool ShowLog { get => showLog; set => Set(ref showLog, value); }

        bool useModes = true;
        public bool UseModes { get => useModes; set => Set(ref useModes, value); }

        bool showOutline = true;
        public bool ShowOutline { get => showOutline; set => Set(ref showOutline, value); }

        bool autoOpenPDF = true;
        public bool AutoOpenPDF { get => autoOpenPDF; set => Set(ref autoOpenPDF, value); }

        bool autoOpenLOG = false;
        public bool AutoOpenLOG { get => autoOpenLOG; set => Set(ref autoOpenLOG, value); }

        bool autoOpenLOGOnlyOnError = true;
        public bool AutoOpenLOGOnlyOnError { get => autoOpenLOGOnlyOnError; set => Set(ref autoOpenLOGOnlyOnError, value); }

        bool internalViewer = true;
        public bool InternalViewer{ get => internalViewer; set => Set(ref internalViewer, value); }

        bool distributionInstalled = false;
        public bool DistributionInstalled { get => distributionInstalled; set => Set(ref distributionInstalled, value); }

        string navigationViewPaneMode = "Auto";
        public string NavigationViewPaneMode{ get => navigationViewPaneMode; set => Set(ref navigationViewPaneMode, value); }

        string additionalParameters = "--autogenerate --nonstopmode --noconsole ";
        public string AdditionalParameters { get => additionalParameters; set => Set(ref additionalParameters, value); }

        bool navigationViewPaneOpen = true;
        public bool NavigationViewPaneOpen { get => navigationViewPaneOpen; set => Set(ref navigationViewPaneOpen, value); }

        int navigationViewPaneOpenLength = 250;
        public int NavigationViewPaneOpenLength { get => navigationViewPaneOpenLength; set => Set(ref navigationViewPaneOpenLength, value); }


        string contextDistributionPath = "";
        public string ContextDistributionPath{ get => contextDistributionPath; set => Set(ref contextDistributionPath, value); }

        string texFilePath = "";
        public string TexFilePath{ get => texFilePath; set => Set(ref texFilePath, value); }

        string texFileFolder = "";
        public string TexFileFolder { get => texFileFolder; set => Set(ref texFileFolder, value); }

        string lastActiveProject = "";
        public string LastActiveProject { get => lastActiveProject; set => Set(ref lastActiveProject, value); }

        string contextDownloadLink = @"http://lmtx.pragma-ade.nl/install-lmtx/context-mswin.zip";
        public string ContextDownloadLink{ get => contextDownloadLink; set => Set(ref contextDownloadLink, value); }

        string theme = "Dark";
        public string Theme{ get => theme; set => Set(ref theme, value); }

        string texFileName = @"";
        public string TexFileName{ get => texFileName; set => Set(ref texFileName, value); }

        string modes = "";
        public string Modes { get => modes; set => Set(ref modes, value); }

        bool codeFolding = true;
        public bool CodeFolding{ get => codeFolding; set { Set(ref codeFolding, value); if (App.VM.EditorOptions != null) App.VM.EditorOptions.Folding = value;  } }

        bool miniMap = true;
        public bool MiniMap { get => miniMap; set { Set(ref miniMap, value); if (App.VM.EditorOptions != null) App.VM.EditorOptions.Minimap =  new EditorMinimapOptions() { Enabled = value, ShowSlider = "always", RenderCharacters = true }; ; } }

        bool hover = true;
        public bool Hover { get => hover; set { Set(ref hover, value); if (App.VM.EditorOptions != null) App.VM.EditorOptions.Hover = new EditorHoverOptions() { Enabled = value, Delay = 100, Sticky = true }; } }

        bool suggestStartStop = true;
        public bool SuggestStartStop{ get => suggestStartStop; set => Set(ref suggestStartStop, value); }

        bool suggestPrimitives = true;
        public bool SuggestPrimitives{ get => suggestPrimitives; set => Set(ref suggestPrimitives, value); }

        bool suggestCommands = true;
        public bool SuggestCommands{ get => suggestCommands; set => Set(ref suggestCommands, value); }

        string packageID = @"";
        public string PackageID { get => packageID; set => Set(ref packageID, value); }

        ObservableCollection<Project> projectList = new ObservableCollection<Project>();
        public ObservableCollection<Project> ProjectList { get => projectList; set => Set(ref projectList, value); }



        [JsonIgnore]
        public List<string> ShowLineNumberOptions
        {
            get
            {
                //var n = Enum.GetValues(typeof(LineNumbersType)).Cast<LineNumbersType>().ToList();
                var n = new List<string>() { "on", "off", "interval", "relative" };
                //List<string> myDic = new List<string>();
                //foreach (var foo in Enum.GetValues(typeof(LineNumbersType)))
                //{
                //    myDic.Add(foo.ToString());
                //}
                return n;
            }
        }
        [JsonIgnore]
        public string[] NavigationOption
        {
            get
            {

                string[] nav = Enum.GetNames(typeof(NavigationViewPaneDisplayMode)); // { "Left", "LeftCompact", "Auto", "Top", "LeftMinimal" };
                return nav;
            }
        }
        [JsonIgnore]
        public string[] ThemeOption
        {
            get
            {
                string[] nav = { "Default", "Light", "Dark" };
                return nav;
            }
        }

        public static Settings FromJson(string json) => JsonConvert.DeserializeObject<Settings>(json);

        protected bool Set<T>(ref T backingStore, T value,
            [CallerMemberName]string propertyName = "",
            Action onChanged = null)
        {
            if (EqualityComparer<T>.Default.Equals(backingStore, value))
                return false;
            

            backingStore = value;
            onChanged?.Invoke();
            OnPropertyChanged(propertyName);
            return true;
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = "")
        {
            var changed = PropertyChanged;
            if (changed == null)
                return;

            changed.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public static class Serialize
    {
        
        public static string ToJson(this Settings self) => JsonConvert.SerializeObject(self, Formatting.Indented);

    }

}