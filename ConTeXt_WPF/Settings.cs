using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using Windows.ApplicationModel;
using Windows.Storage;

namespace ConTeXt_WPF
{
    public class Settings : INotifyPropertyChanged
    {
        [JsonIgnore]
        public static Settings Default { get => GetSettings(); }

        private static Settings GetSettings()
        {
            try
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

                return settings;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Source + "\n" + ex.Message);
                return null;
            }
        }


        bool startWithLastActiveProject = true;
        public bool StartWithLastActiveProject { get => startWithLastActiveProject; set => Set(ref startWithLastActiveProject, value); }

        bool showLog = false;
        public bool ShowLog { get => showLog; set => Set(ref showLog, value); }

        bool showOutline = true;
        public bool ShowOutline { get => showOutline; set => Set(ref showOutline, value); }

        bool useModes = true;
        public bool UseModes { get => useModes; set => Set(ref useModes, value); }

        bool autoOpenPDF = true;
        public bool AutoOpenPDF { get => autoOpenPDF; set => Set(ref autoOpenPDF, value); }

        bool autoOpenLOG = false;
        public bool AutoOpenLOG { get => autoOpenLOG; set => Set(ref autoOpenLOG, value); }

        bool autoOpenLOGOnlyOnError = true;
        public bool AutoOpenLOGOnlyOnError { get => autoOpenLOGOnlyOnError; set => Set(ref autoOpenLOGOnlyOnError, value); }

        bool internalViewer = true;
        public bool InternalViewer { get => internalViewer; set => Set(ref internalViewer, value); }

        string navigationViewPaneMode = "Auto";
        public string NavigationViewPaneMode { get => navigationViewPaneMode; set => Set(ref navigationViewPaneMode, value); }

        string additionalParameters = "--noconsole ";
        public string AdditionalParameters { get => additionalParameters; set => Set(ref additionalParameters, value); }

        bool navigationViewPaneOpen = true;
        public bool NavigationViewPaneOpen { get => navigationViewPaneOpen; set => Set(ref navigationViewPaneOpen, value); }

        int navigationViewPaneOpenLength = 250;
        public int NavigationViewPaneOpenLength { get => navigationViewPaneOpenLength; set => Set(ref navigationViewPaneOpenLength, value); }

        string contextDistributionPath = ApplicationData.Current.LocalFolder.Path;
        public string ContextDistributionPath { get => contextDistributionPath ?? ""; set => Set(ref contextDistributionPath, value); }

        string texFilePath = "";
        public string TexFilePath { get => texFilePath; set => Set(ref texFilePath, value); }

        string texFileFolder = "";
        public string TexFileFolder { get => texFileFolder; set => Set(ref texFileFolder, value); }

        string lastActiveProject = "";
        public string LastActiveProject { get => lastActiveProject; set => Set(ref lastActiveProject, value); }

        string contextDownloadLink = @"http://lmtx.pragma-ade.nl/install-lmtx/context-mswin.zip";
        public string ContextDownloadLink { get => contextDownloadLink; set => Set(ref contextDownloadLink, value); }

        string theme = "Dark";
        public string Theme { get => theme; set => Set(ref theme, value); }

        string texFileName = @"";
        public string TexFileName { get => texFileName; set => Set(ref texFileName, value); }

        string modes = "";
        public string Modes { get => modes; set => Set(ref modes, value); }
     
        bool suggestStartStop = true;
        public bool SuggestStartStop { get => suggestStartStop; set => Set(ref suggestStartStop, value); }

        bool suggestPrimitives = true;
        public bool SuggestPrimitives { get => suggestPrimitives; set => Set(ref suggestPrimitives, value); }

        bool suggestCommands = true;
        public bool SuggestCommands { get => suggestCommands; set => Set(ref suggestCommands, value); }

        string packageID = Package.Current.Id.FamilyName;
        public string PackageID { get => packageID ?? ""; set => Set(ref packageID, value); }

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