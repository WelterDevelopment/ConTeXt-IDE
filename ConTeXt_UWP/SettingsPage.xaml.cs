using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.ApplicationModel.AppService;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// Die Elementvorlage "Leere Seite" wird unter https://go.microsoft.com/fwlink/?LinkId=234238 dokumentiert.

namespace ConTeXt_UWP
{
    /// <summary>
    /// Eine leere Seite, die eigenständig verwendet oder zu der innerhalb eines Rahmens navigiert werden kann.
    /// </summary>
    public sealed partial class SettingsPage : Page
    {
        public ViewModel currentViewModel = App.AppViewModel;
        public SettingsPage()
        {
            this.InitializeComponent();
            this.NavigationCacheMode = NavigationCacheMode.Enabled;
        }
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            // var _enumval = Enum.GetValues(typeof(NavigationViewPaneDisplayMode)).Cast<NavigationViewPaneDisplayMode>();
            //PaneControl.ItemsSource = _enumval.ToList();
            PaneControl.SelectionChanged += PaneControl_SelectionChanged1;
        }

        private void PaneControl_SelectionChanged1(object sender, SelectionChangedEventArgs e)
        {
            if ((string)(sender as ComboBox).SelectedItem == "Top")
            {
                Window.Current.SetTitleBar(((Window.Current.Content as Frame).Content as MainPage).nvSample.PaneCustomContent as FrameworkElement);
                ((Window.Current.Content as Frame).Content as MainPage).nvSample.Header = null;
            }

            else
            {
                Window.Current.SetTitleBar(((Window.Current.Content as Frame).Content as MainPage).nvSample.Header as FrameworkElement);
            }
        }

        private void PaneControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if ((string)(sender as ComboBox).SelectedItem == "Top")
            {
                Window.Current.SetTitleBar(((Window.Current.Content as Frame).Content as MainPage).nvSample.PaneCustomContent as FrameworkElement);
                ((Window.Current.Content as Frame).Content as MainPage).nvSample.Header = null;
            }

            else
            {
                Window.Current.SetTitleBar(((Window.Current.Content as Frame).Content as MainPage).nvSample.Header as FrameworkElement);
                ((Window.Current.Content as Frame).Content as MainPage).nvSample.Header = "Restart needed!";
            }
        }

        private async void Update_Click(object sender, RoutedEventArgs e)
        {
            App.AppViewModel.IsNotSaving = false;
            ValueSet request = new ValueSet();
            request.Add("command", "update");
            AppServiceResponse response = await App.AppViewModel.appServiceConnection.SendMessageAsync(request);
            // display the response key/value pairs
            foreach (string key in response.Message.Keys)
            {
                if ((string)response.Message[key] == "response")
                {
                    App.AppViewModel.LOG("ConTeXt distribution updated.");
                }
            }
                //await Task.Delay(2000);
          currentViewModel.IsNotSaving = true;
        }
    }
}
