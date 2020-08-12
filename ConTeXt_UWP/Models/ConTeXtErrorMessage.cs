using ConTeXt_UWP.Helpers;

namespace ConTeXt_UWP.Models
{
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
}
