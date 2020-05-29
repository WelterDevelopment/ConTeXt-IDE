using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml;

namespace ConTeXt_WPF
{

    public class Settings : ObservableSettings
    {
        public static Settings settings = new Settings();
        public static Settings Default
        {
            get { return settings; }
        }

        public Settings() : base(ApplicationData.Current.LocalSettings) { }

        [DefaultSettingValue(Value = true)]
        public bool StartWithLastActiveProject
        {
            get { return Get<bool>(); }
            set { Set(value); }
        }


        [DefaultSettingValue(Value = 2)]
        public int ShowLineNumbers
        {
            get { return Get<int>(); }
            set { Set(value); }
        }


        [DefaultSettingValue(Value = true)]
        public bool ShowLog
        {
            get { return Get<bool>(); }
            set { Set(value); }
        }

        [DefaultSettingValue(Value = true)]
        public bool UseModes
        {
            get { return Get<bool>(); }
            set { Set(value); }
        }

        [DefaultSettingValue(Value = true)]
        public bool InternalViewer
        {
            get { return Get<bool>(); }
            set { Set(value); }
        }

        [DefaultSettingValue(Value = false)]
        public bool Outputready
        {
            get { return Get<bool>(); }
            set { Set(value); }
        }

        [DefaultSettingValue(Value = "Left")]
        public string NavigationViewPaneMode
        {
            get { return Get<string>(); }
            set { Set(value); }
        }

        [DefaultSettingValue(Value = true)]
        public bool NavigationViewPaneOpen
        {
            get { return Get<bool>(); }
            set { Set(value); }
        }

        [DefaultSettingValue(Value = 200)]
        public int NavigationViewPaneOpenLength
        {
            get { return Get<int>(); }
            set { Set(value); }
        }

        [DefaultSettingValue(Value = @"")]
        public string ContextDistributionPath
        {
            get { return Get<string>(); }
            set { Set(value); }
        }

        [DefaultSettingValue(Value = @"")]
        public string TexFilePath
        {
            get { return Get<string>(); }
            set { Set(value); }
        }

        [DefaultSettingValue(Value = @"")]
        public string TexFileFolder
        {
            get { return Get<string>(); }
            set { Set(value); }
        }

        [DefaultSettingValue(Value = "")]
        public string LastActiveProject
        {
            get { return Get<string>(); }
            set { Set(value); }
        }

        [DefaultSettingValue(Value = "")]
        public string LastActiveFileName
        {
            get { return Get<string>(); }
            set { Set(value); }
        }

        [DefaultSettingValue(Value = @"http://lmtx.pragma-ade.nl/install-lmtx/context-win64.zip")]
        public string ContextDownloadLink
        {
            get { return Get<string>(); }
            set { Set(value); }
        }
        [DefaultSettingValue(Value = "Dark")]
        public string Theme
        {
            get { return Get<string>(); }
            set { Set(value); }
        }
        [DefaultSettingValue(Value = @"bla.tex")]
        public string TexFileName
        {
            get { return Get<string>(); }
            set { Set(value); }
        }
        [DefaultSettingValue(Value = "")]
        public string Modes
        {
            get { return Get<string>(); }
            set { Set(value); }
        }
        [DefaultSettingValue(Value = true)]
        public bool CodeFolding
        {
            get { return Get<bool>(); }
            set { Set(value); }
        }
        [DefaultSettingValue(Value = true)]
        public bool MiniMap
        {
            get { return Get<bool>(); }
            set { Set(value); }
        }
        [DefaultSettingValue(Value = @"")]
        public string PackageID
        {
            get { return Get<string>(); }
            set { Set(value); }
        }

       
        public string[] NavigationOption
        {
            get
            {

                string[] nav = Enum.GetNames(typeof(NavigationViewPaneDisplayMode)); // { "Left", "LeftCompact", "Auto", "Top", "LeftMinimal" };
                return nav;
            }
        }
        public string[] ThemeOption
        {
            get
            {
                string[] nav = { "Default", "Light", "Dark" };
                return nav;
            }
        }
    }

    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public sealed class DefaultSettingValueAttribute : Attribute
    {
        public DefaultSettingValueAttribute()
        {
        }

        public DefaultSettingValueAttribute(object value)
        {
            Value = value;
        }

        public object Value { get; set; }



    }

    public class ObservableSettings : INotifyPropertyChanged
    {
        private readonly ApplicationDataContainer settings;

        public ObservableSettings(ApplicationDataContainer settings)
        {
            this.settings = settings;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected bool Set<T>(T value, [CallerMemberName] string propertyName = null)
        {
            if (settings.Values.ContainsKey(propertyName))
            {
                var currentValue = (T)settings.Values[propertyName];
                if (EqualityComparer<T>.Default.Equals(currentValue, value))
                    return false;
            }

            settings.Values[propertyName] = value;
            //PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            OnPropertyChanged(propertyName);

            return true;
        }

        protected void OnPropertyChanged(string name)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(name));
                //if (name == "Outputready") { ((Window.Current.Content as Frame) as MainPage).nv };
            }
        }

        protected T Get<T>([CallerMemberName] string propertyName = null)
        {
            if (settings.Values.ContainsKey(propertyName))
                return (T)settings.Values[propertyName];

            var attributes = GetType().GetTypeInfo().GetDeclaredProperty(propertyName).CustomAttributes.Where(ca => ca.AttributeType == typeof(DefaultSettingValueAttribute)).ToList();
            if (attributes.Count == 1)
                return (T)attributes[0].NamedArguments[0].TypedValue.Value;

            return default(T);
        }
    }
}