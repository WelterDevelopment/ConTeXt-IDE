using ConTeXt_UWP.Helpers;

namespace ConTeXt_UWP.Models
{
    // Template projects the user can choose from when he adds a project folder
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
}
