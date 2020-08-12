using ConTeXt_UWP.Helpers;

namespace ConTeXt_UWP.Models
{
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
}
