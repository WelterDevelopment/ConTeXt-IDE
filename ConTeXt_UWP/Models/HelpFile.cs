using ConTeXt_UWP.Helpers;

namespace ConTeXt_UWP.Models
{
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
}
