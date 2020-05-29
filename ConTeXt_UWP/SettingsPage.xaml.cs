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
        public ViewModel currentViewModel = App.VM;
        public SettingsPage()
        {
            this.InitializeComponent();
            this.NavigationCacheMode = NavigationCacheMode.Enabled;
        }
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            // var _enumval = Enum.GetValues(typeof(NavigationViewPaneDisplayMode)).Cast<NavigationViewPaneDisplayMode>();
            //PaneControl.ItemsSource = _enumval.ToList();
            PaneControl.SelectionChanged += PaneControl_SelectionChanged;
        }


        private void PaneControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if ((string)(sender as ComboBox).SelectedItem == "Top")
            {
                Window.Current.SetTitleBar(((Window.Current.Content as Frame).Content as MainPage).nvSample.PaneCustomContent as FrameworkElement);
                //((Window.Current.Content as Frame).Content as MainPage).nvSample.Header = null;
            }

            else
            {
                Window.Current.SetTitleBar(((Window.Current.Content as Frame).Content as MainPage).Header as FrameworkElement);
                //((Window.Current.Content as Frame).Content as MainPage).nvSample.SetBinding(NavigationView.HeaderProperty, new Binding() { Path = new PropertyPath("NVHeader") });
            }
        }

        private async void Update_Click(object sender, RoutedEventArgs e)
        {
            App.VM.IsSaving = true;
            ValueSet request = new ValueSet();
            request.Add("command", "update");
            AppServiceResponse response = await App.VM.appServiceConnection.SendMessageAsync(request);
            // display the response key/value pairs
            foreach (string key in response.Message.Keys)
            {
                if ((string)response.Message[key] == "response")
                {
                    App.VM.LOG("ConTeXt distribution updated.");
                }
            }
                //await Task.Delay(2000);
          currentViewModel.IsSaving = false;
        }

        private async void Install_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                App.VM.IsSaving = true;
                var cd = new ContentDialog();
                var sp = new StackPanel();
                StorageFolder localFolder = ApplicationData.Current.LocalFolder;
                var installpathtb = new TextBox() { Header = "Install Path (changing this is experimental)", Text = localFolder.Path };
                var downloadlinktb = new TextBox() { Header = "Download link (only change if PRAGMA ADE changed the URL)", Text = App.VM.Default.ContextDownloadLink };
                sp.Children.Add(installpathtb);
                sp.Children.Add(downloadlinktb);
                cd.Title = "First Start: Install the ConTeXt (LuaMetaTeX) Distribution";
                cd.Content = sp;
                cd.PrimaryButtonText = "Install";
                cd.CloseButtonText = "Skip (Setup in the Settings!)";
                cd.PrimaryButtonClick += async (a, b) =>
                {
                    App.VM.Default.ContextDistributionPath = installpathtb.Text;
                    App.VM.Default.ContextDownloadLink = downloadlinktb.Text;
                    ValueSet request = new ValueSet();
                    request.Add("command", "install");
                    AppServiceResponse response = await App.VM.appServiceConnection.SendMessageAsync(request);
                    //AppViewModel.LOG(response.Status.ToString() + " ... " + response.Message.FirstOrDefault().Key.ToString() + " Key Val " + response.Message.FirstOrDefault().Value.ToString());
                    // display the response key/value pairs
                    foreach (string key in response.Message.Keys)
                    {
                        if (key == "response")
                        {
                            if ((bool)response.Message[key])
                                App.VM.LOG("ConTeXt distribution installed.");
                            else
                                App.VM.LOG("Installation error");

                        }
                    }
                    //AppRestartFailureReason result = await CoreApplication.RequestRestartAsync("");
                    //if (result == AppRestartFailureReason.NotInForeground ||
                    //    result == AppRestartFailureReason.RestartPending ||
                    //    result == AppRestartFailureReason.Other)
                    //{
                    //    AppViewModel.LOG("Restart failed");
                    //}
                };
                await cd.ShowAsync();
                App.VM.IsSaving = false;
            }
            catch (Exception ex)
            {
                App.VM.LOG(ex.Message);
            }
        }
    }
}
