using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using Windows.Storage;

namespace ConTeXt_WPF
{
    public class jsonSettings : INotifyPropertyChanged
    {
        [JsonIgnore]
        public static jsonSettings Default { get { return GetSettings(); } set { } } 

        private static jsonSettings GetSettings()
        {
            string file = "settings.json";
            var storageFolder = ApplicationData.Current.LocalFolder;
            string settingsPath = Path.Combine(storageFolder.Path, file);
            jsonSettings settings;
            if (!File.Exists(settingsPath))
            {
                settings = new jsonSettings();
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

            return settings;

        }



        int quoteFrequency = 15;
        public int QuoteFrequency { get => quoteFrequency; set => Set(ref quoteFrequency, value); }


        bool startWithLastActiveProject = true;
        public bool StartWithLastActiveProject { get => startWithLastActiveProject; set => Set(ref startWithLastActiveProject, value); }

        int showLineNumbers = 1;
        public int ShowLineNumbers { get => showLineNumbers; set => Set(ref showLineNumbers, value); }


        bool showLog = true;
        public bool ShowLog { get => showLog; set => Set(ref showLog, value); }

        bool useModes = true;
        public bool UseModes { get => useModes; set => Set(ref useModes, value); }

        bool internalViewer = true;
        public bool InternalViewer { get => internalViewer; set => Set(ref internalViewer, value); }

        string navigationViewPaneMode = "Left";
        public string NavigationViewPaneMode { get => navigationViewPaneMode; set => Set(ref navigationViewPaneMode, value); }

        bool navigationViewPaneOpen = true;
        public bool NavigationViewPaneOpen { get => navigationViewPaneOpen; set => Set(ref navigationViewPaneOpen, value); }

        int navigationViewPaneOpenLength = 250;
        public int NavigationViewPaneOpenLength { get => navigationViewPaneOpenLength; set => Set(ref navigationViewPaneOpenLength, value); }


        string contextDistributionPath = "";
        public string ContextDistributionPath { get => contextDistributionPath; set => Set(ref contextDistributionPath, value); }

        string texFilePath = "";
        public string TexFilePath { get => texFilePath; set => Set(ref texFilePath, value); }

        string texFileFolder = "";
        public string TexFileFolder { get => texFileFolder; set => Set(ref texFileFolder, value); }

        string lastActiveProject = "";
        public string LastActiveProject { get => lastActiveProject; set => Set(ref lastActiveProject, value); }

        string contextDownloadLink = @"http://lmtx.pragma-ade.nl/install-lmtx/context-win64.zip";
        public string ContextDownloadLink { get => contextDownloadLink; set => Set(ref contextDownloadLink, value); }

        string theme = "Dark";
        public string Theme { get => theme; set => Set(ref theme, value); }

        string texFileName = @"";
        public string TexFileName { get => texFileName; set => Set(ref texFileName, value); }

        string modes = "";
        public string Modes { get => modes; set => Set(ref modes, value); }

        bool codeFolding = true;
        public bool CodeFolding { get => codeFolding; set => Set(ref codeFolding, value); }

        bool miniMap = true;
        public bool MiniMap { get => miniMap; set => Set(ref miniMap, value); }

        bool suggestStartStop = true;
        public bool SuggestStartStop { get => suggestStartStop; set => Set(ref suggestStartStop, value); }

        bool suggestPrimitives = true;
        public bool SuggestPrimitives { get => suggestPrimitives; set => Set(ref suggestPrimitives, value); }

        bool suggestCommands = true;
        public bool SuggestCommands { get => suggestCommands; set => Set(ref suggestCommands, value); }

        string packageID = @"";
        public string PackageID { get => packageID; set => Set(ref packageID, value); }

        public static jsonSettings FromJson(string json) => JsonConvert.DeserializeObject<jsonSettings>(json);

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

        public static string ToJson(this jsonSettings self) => JsonConvert.SerializeObject(self, Formatting.Indented);

    }

}