using ConTeXt_UWP.Models;
using ConTeXt_UWP.ViewModels;
using System.Collections.ObjectModel;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

// The Content Dialog item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace ConTeXt_UWP
{
    public sealed partial class SelectNew : ContentDialog
    {
        public SelectNew()
        {
            this.InitializeComponent();
        }

        private void BGRadioButton_Checked(object sender, RoutedEventArgs e)
        {

        }

        ViewModel vm = App.VM;

        public ObservableCollection<TemplateSelection> templateSelections = new ObservableCollection<TemplateSelection>() {
         new TemplateSelection(){ Content = "Empty and/or existing project folder", Tag = "empty", IsSelected = false},
         new TemplateSelection(){ Content = "New Project folder with template", Tag = "template", IsSelected = false},
        };
    
    }
}
