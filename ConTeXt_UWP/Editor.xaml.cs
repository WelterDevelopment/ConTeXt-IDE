using Microsoft.Toolkit.Uwp.UI.Controls;
using Monaco;
using Monaco.Editor;
using Monaco.Languages;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading;
using System.Threading.Tasks;
using Windows.ApplicationModel.AppService;
using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.Storage.AccessCache;
using Windows.System;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Documents;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Windows.UI.Xaml.Shapes;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace ConTeXt_UWP
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class Editor : Page
    {
        public bool loaded = false;
        public ViewModel currentViewModel = App.AppViewModel;
        public Editor()
        {
            this.InitializeComponent();
            this.NavigationCacheMode = NavigationCacheMode.Enabled;
            //App.AppViewModel.FileItems.CollectionChanged += FileItems_CollectionChanged;
        }

        private void FileItems_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            try
            {
                
                if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Add)
                {
                    //var fi = (FileItem)e.NewItems[0];
                    // var item = Tabs.Items.Where(x => ((FileItem)x) == fi).FirstOrDefault();
                    Tabs.SelectedItem = e.NewItems[0];
                    App.AppViewModel.LOG("TabSwitch: " + ((FileItem)e.NewItems[0]).FileName);
                }
            }
            catch (Exception ex)
            {
                App.AppViewModel.LOG(ex.Message);
            }
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
           
            foreach (var FileEvent in App.AppViewModel.FileActivatedEvents)
            {
                if (FileEvent != null)
                {
                    foreach (StorageFile file in FileEvent.Files)
                    {
                        var fileitem = new FileItem(file) { };
                        App.AppViewModel.OpenFile(fileitem);
                    }
                }
            }
            App.AppViewModel.FileActivatedEvents.Clear();
            loaded = true;
        }
        private void AppServiceConnection_RequestReceived(AppServiceConnection sender, AppServiceRequestReceivedEventArgs args)
        {
            //double d1 = (double)args.Request.Message["D1"];
            //double d2 = (double)args.Request.Message["D2"];
            //double result = d1 + d2;

            //ValueSet response = new ValueSet();
            //response.Add("RESULT", result);
            //await args.Request.SendResponseAsync(response);

            //// log the request in the UI for demo purposes
            //await this.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            //{
            //    App.AppViewModel.LOG(string.Format("Request: {0} + {1} --> Response = {2}\r\n", d1, d2, result));
            //});
            try
            {

                if (args.Request.Message.Keys.FirstOrDefault() == "log")
                    App.AppViewModel.LOG(args.Request.Message.Values.FirstOrDefault() as string);
            }
            catch (Exception ex)
            {
                App.AppViewModel.LOG(ex.Message);
            }
        }

        public async Task Compile()
        {
            App.AppViewModel.LOG("Compiling");
            try
            {
                ValueSet request = new ValueSet
            {
                { "compile", true }
            };
            AppServiceResponse response = await App.AppViewModel.appServiceConnection.SendMessageAsync(request);
            // display the response key/value pairs
            foreach (string key in response.Message.Keys)
            {
                App.AppViewModel.LOG(key + " = " + response.Message[key]);
                if ((string)response.Message[key] == "compiled")
                {
                    
                        string local = ApplicationData.Current.LocalFolder.Path;
                        string curFile = System.IO.Path.GetFileName(App.AppViewModel.Default.TexFilePath);
                        string curPDF = System.IO.Path.GetFileNameWithoutExtension(curFile) + ".pdf";
                        string curPDFPath = System.IO.Path.Combine(App.AppViewModel.Default.TexFilePath, curPDF);
                        string newPathToFile = System.IO.Path.Combine(local, curPDF);
                        App.AppViewModel.LOG("Opening " + System.IO.Path.GetFileNameWithoutExtension(App.AppViewModel.Default.TexFileName) + ".pdf");
                        //StorageFile file = await ApplicationData.Current.LocalFolder.GetFileAsync(curPDF);
                        if (App.AppViewModel.Default.InternalViewer)
                        {
                            App.AppViewModel.LOG(App.AppViewModel.Default.PackageID);
                            var fil = await App.AppViewModel.CurrentProject.Folder.GetFileAsync(System.IO.Path.GetFileNameWithoutExtension(App.AppViewModel.Default.TexFileName) + ".pdf");
                            App.AppViewModel.LOG(App.AppViewModel.CurrentProject.Folder.Name);
                            Stream stream = await fil.OpenStreamForReadAsync();
                            byte[] buffer = new byte[stream.Length];
                            stream.Read(buffer, 0, (int)stream.Length);
                            var asBase64 = Convert.ToBase64String(buffer);
                            await PDFReader.InvokeScriptAsync("openPdfAsBase64", new[] { asBase64 });
                            App.AppViewModel.LOG(App.AppViewModel.CurrentProject.Folder.Name);
                        }
                        else
                        {
                            var file = await App.AppViewModel.CurrentProject.Folder.GetFileAsync(System.IO.Path.GetFileNameWithoutExtension(App.AppViewModel.Default.TexFileName) + ".pdf");
                            if (file != null)
                                await Launcher.LaunchFileAsync(file);
                        }
                    
                }
                else
                {
                    App.AppViewModel.LOG(key + "lol");
                }
            }
            
            }
            catch (Exception f)
            {
                App.AppViewModel.LOG(f.Message);
            }
            App.AppViewModel.IsNotSaving = true;
        }
        private async void Btncompile_Click(object sender, RoutedEventArgs e)
        {
            await UWPSave(((FileItem)Tabs.SelectedItem).File as StorageFile);
            await Compile();
        }

       

        public int logline = 0;


        DispatcherTimer dispatcherTimer;
        DateTimeOffset startTime;
        DateTimeOffset lastTime = DateTimeOffset.Now;
        DateTimeOffset stopTime;
        int timesTicked = 1;
        int timesToTick = 10;

        public void DispatcherTimerSetup()
        {
            dispatcherTimer = new DispatcherTimer();
            dispatcherTimer.Tick += DispatcherTimer_Tick;
            dispatcherTimer.Interval = new TimeSpan(0, 0, 1);
            //IsEnabled defaults to false
            startTime = DateTimeOffset.Now;
            lastTime = startTime;
            //IsEnabled should now be true after calling start
        }

        void DispatcherTimer_Tick(object sender, object e)
        {
            DateTimeOffset time = DateTimeOffset.Now;
            TimeSpan span = time - lastTime;
            lastTime = time;
            //Time since last tick should be very very close to Interval
            timesTicked++;
            if (timesTicked > timesToTick)
            {
                stopTime = time;
                dispatcherTimer.Stop();
                //IsEnabled should now be false after calling stop
                span = stopTime - startTime;
            }
        }
        private async void Btnsave_Click(object sender, RoutedEventArgs e)
        {
            await UWPSave(((FileItem)Tabs.SelectedItem).File as StorageFile);
        }

        private async void Btnopen_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                App.AppViewModel.LOG(System.IO.Path.GetFileNameWithoutExtension(App.AppViewModel.Default.TexFileName) + ".pdf");
                var fil = await App.AppViewModel.CurrentProject.Folder.GetFileAsync(System.IO.Path.GetFileNameWithoutExtension(App.AppViewModel.Default.TexFileName) + ".pdf");
                App.AppViewModel.LOG(App.AppViewModel.CurrentProject.Folder.Name);
                Stream stream = await fil.OpenStreamForReadAsync();
                App.AppViewModel.LOG("1");
                byte[] buffer = new byte[stream.Length];
                stream.Read(buffer, 0, (int)stream.Length);
                App.AppViewModel.LOG("2");
                var asBase64 = Convert.ToBase64String(buffer);
                App.AppViewModel.LOG("3");
                await PDFReader.InvokeScriptAsync("openPdfAsBase64", new[] { asBase64 });
                App.AppViewModel.LOG(App.AppViewModel.CurrentProject.Folder.Name);
                //var picker = new Windows.Storage.Pickers.FileOpenPicker();
                //picker.ViewMode = Windows.Storage.Pickers.PickerViewMode.Thumbnail;
                //picker.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.PicturesLibrary;
                //picker.FileTypeFilter.Add(".jpg");
                //picker.FileTypeFilter.Add(".jpeg");
                //picker.FileTypeFilter.Add(".png");

                //Windows.Storage.StorageFile file = await picker.PickSingleFileAsync();
                // App.AppViewModel.GetData();
                var folderPicker = new Windows.Storage.Pickers.FolderPicker();
                //folderPicker.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.Desktop;
                folderPicker.FileTypeFilter.Add("*");
                folderPicker.CommitButtonText = "Open";
                folderPicker.SettingsIdentifier = "ChooseWorkspace";
                folderPicker.ViewMode = Windows.Storage.Pickers.PickerViewMode.List;
                StorageFolder folder = await folderPicker.PickSingleFolderAsync();
                if (folder != null)
                {
                    Windows.Storage.AccessCache.StorageApplicationPermissions.FutureAccessList.AddOrReplace(folder.Name, folder, "folder");
                    Windows.Storage.AccessCache.StorageApplicationPermissions.MostRecentlyUsedList.AddOrReplace(folder.Name, folder, "folder");
                    App.AppViewModel.RecentAccessList = StorageApplicationPermissions.MostRecentlyUsedList;
                    App.AppViewModel.CurrentProject = new Project(folder.Name, folder, App.AppViewModel.GenerateTreeView(folder));
                }
            }
            catch (Exception ex)
            {
                App.AppViewModel.Message("Error Type: " + ex.GetType().ToString(), ex.Message);
            }
            
        }
        public CodeEditor CE = new CodeEditor();
        private  void Box_KeyDown(Monaco.CodeEditor sender, Monaco.Helpers.WebKeyEventArgs args)
        {
            //   edit.CodeLanguage = CodeLanguage
        }
        public async Task Save()
        {
            if (App.AppViewModel.IsNotSaving)
            {
                App.AppViewModel.IsNotSaving = false;
                //App.AppViewModel.LOG(Tabs.SelectedItem.GetType().ToString());
                var fi = Tabs.SelectedItem as FileItem;

                //ViewModel.Default.CodeContent = fi.FileContent;
                var buffer = Windows.Security.Cryptography.CryptographicBuffer.ConvertStringToBinary(fi.FileContent, Windows.Security.Cryptography.BinaryStringEncoding.Utf8);
                var file = await ApplicationData.Current.LocalFolder.CreateFileAsync(fi.FileName, CreationCollisionOption.ReplaceExisting);
                await FileIO.WriteBufferAsync(file, buffer);
                App.AppViewModel.Default.TexFileName = fi.FileName;
                App.AppViewModel.Default.TexFilePath = fi.FilePath;

                App.AppViewModel.LOG("Saving");
                ValueSet request = new ValueSet
                {
                    { "save", true }
                };
                AppServiceResponse response = await App.AppViewModel.appServiceConnection.SendMessageAsync(request);
                // display the response key/value pairs
                if (response != null)
                    foreach (string key in response.Message.Keys)
                    {
                        if ((string)response.Message[key] == "response")
                        {
                            App.AppViewModel.LOG(key + " = " + response.Message[key]); App.AppViewModel.LOG("Saved on " + DateTime.Now);
                        }
                    }

                //await Task.Delay(2000);
                App.AppViewModel.IsNotSaving = true;
            }
            else App.AppViewModel.LOG("already saving...");
        }

        public async Task UWPSave(StorageFile file)
        {
            if (App.AppViewModel.IsNotSaving)
            {
                App.AppViewModel.IsNotSaving = false;
                //App.AppViewModel.LOG(Tabs.SelectedItem.GetType().ToString());
                var fi = Tabs.SelectedItem as FileItem;

                //ViewModel.Default.CodeContent = fi.FileContent;
                var buffer = Windows.Security.Cryptography.CryptographicBuffer.ConvertStringToBinary(fi.FileContent, Windows.Security.Cryptography.BinaryStringEncoding.Utf8);
               
                await FileIO.WriteBufferAsync(file, buffer);
                App.AppViewModel.Default.TexFileName = fi.FileName;
                App.AppViewModel.Default.TexFilePath = fi.FilePath;
                App.AppViewModel.Default.TexFileFolder = fi.FileFolder;

                App.AppViewModel.LOG("Saved");
                
                App.AppViewModel.IsNotSaving = true;
            }
            else App.AppViewModel.LOG("already saving...");
        }

        private async void Edit_Loading(object sender, RoutedEventArgs e)
        {
            var fi = ((sender as FrameworkElement).DataContext as FileItem);
            (sender as CodeEditor).CodeLanguage = fi.FileLanguage;
            //(sender as CodeEditor).RequestedTheme = ElementTheme.Default;
            var languages = new Monaco.LanguagesHelper((sender as CodeEditor));

            await languages.RegisterHoverProviderAsync("context", new EditorHoverProvider());
            //(model, position) =>
            //{
            //    return AsyncInfo.Run(async delegate (System.Threading.CancellationToken cancelationToken)
            //    {
            //        var word = await model.GetWordAtPositionAsync(position);
            //        return RegisterHover(word, position);

            //    });
            //});
            await languages.RegisterCompletionItemProviderAsync("context", new LanguageProvider());
            //var _myCondition = await (sender as CodeEditor).CreateContextKeyAsync("Save & Compile", true);
            //await (sender as CodeEditor).AddCommandAsync(Monaco.KeyMod.CtrlCmd | Monaco.KeyCode.Enter, async () =>
            //{
            //    Save();
            //    Compile();
            //},_myCondition.Key);
            await (sender as CodeEditor).AddActionAsync(new RunAction());

        }

        private readonly List<string[]> ContextEnvironmentStructureKeywords = new List<string[]>() {
            new string[]{ "starttext","stoptext", "Document content that marks the end of the preamble"},
            new string[]{ "startstandartmakeup","stopstandardmakeup", "used to format the front page"},
            new string[]{ "startfrontmatter", "stopfrontmatter", "used to print the table of contents, list of figures...", "frontpart"},
            new string[]{ "startbodymatter", "stopbodymatter", "used to format the document body style","bodypart"},
            new string[]{ "startappendices","stopappendices" , "optional structure for the Appendix","appendixpart"},
            new string[]{ "startbackmatter","stopbackmatter", "used to print the bibliography, declaration of authorship, ...","backpart"},
        };
        private static readonly List<CompletionItem> contextEnvironmentStructureKeywords2 = new List<CompletionItem>() {
            new CompletionItem(@"\starttext",@"\starttext", CompletionItemKind.Field) { Documentation = new IMarkdownString("Document content"), Detail = "ConTEXt.Environments.DocumentStructure"},
            new CompletionItem(@"\stoptext",@"\stoptext", CompletionItemKind.Field) { Documentation = new IMarkdownString("Document content"), Detail = "ConTEXt.Environments.DocumentStructure"},
            new CompletionItem(@"\starttext",@"\starttext", CompletionItemKind.Field) { Documentation = new IMarkdownString("Document content"), Detail = "ConTEXt.Environments.DocumentStructure"},
            new CompletionItem(@"\starttext",@"\starttext", CompletionItemKind.Field) { Documentation = new IMarkdownString("Document content"), Detail = "ConTEXt.Environments.DocumentStructure"},
            new CompletionItem(@"\starttext",@"\starttext", CompletionItemKind.Field) { Documentation = new IMarkdownString("Document content"), Detail = "ConTEXt.Environments.DocumentStructure"},
            new CompletionItem(@"\starttext",@"\starttext", CompletionItemKind.Field) { Documentation = new IMarkdownString("Document content"), Detail = "ConTEXt.Environments.DocumentStructure"},
            new CompletionItem(@"\starttext",@"\starttext", CompletionItemKind.Field) { Documentation = new IMarkdownString("Document content"), Detail = "ConTEXt.Environments.DocumentStructure"}
        };

        private readonly List<string[]> ContextEnvironmentSectioningKeywords = new List<string[]>() {
            new string[]{ "starttext","stoptext", "Document content that needs to get printed"},
            new string[]{ "startstandartmakeup","stopstandardmakeup", "To format the front page"},
            new string[]{ "startfrontmatter", "stopfrontmatter", "To print the table of contents, list of figures...", "frontpart"},
            new string[]{ "startbodymatter", "stopbodymatter", "To print the actual content","bodypart"},
            new string[]{ "startappendices","stopappendices" , "Optional structure for the Appendix","appendixpart"},
            new string[]{ "startbackmatter","stopbackmatter", "To print the bibliography, declaration of authorship, ...","backpart"},
        };

        private readonly List<EnvironmentItem> EnvItems = new List<EnvironmentItem>()
        {
            new EnvironmentItem(){  },
        };
        private Hover RegisterHover(WordAtPosition word, IPosition position)
        {
            if (word != null && word.Word != null)
            {
                Range R = new Range(position.LineNumber, position.Column, position.LineNumber, position.Column + 5);


                foreach (var item in ContextEnvironmentStructureKeywords)
                    if (word.Word == item[0])
                    {
                        List<string> OtherItems = new List<string>();
                        foreach (var it in ContextEnvironmentStructureKeywords)
                            if (word.Word == it[0]) { } else { OtherItems.Add($"- *{it[0]}*"); }
                        List<string> HelpText = new List<string>() {
                            $"***\\{item[0]}*** *...* *\\{item[1]}*",
                            $"- {item[2]}",
                            item.Count() > 3 ? $"- Referenced as *{item[3]}*" : "",
                            $"Containing list: *ConTEXt.Environments.DocumentStructure*",
                            $"Other commands in this list:"
                        };
                        HelpText.AddRange(OtherItems);
                        return new Hover(HelpText.ToArray<string>(), R);
                    }
                    else if (word.Word == item[1])
                    {
                        List<string> OtherItems = new List<string>();
                        foreach (var it in ContextEnvironmentStructureKeywords)
                            if (word.Word == it[1]) { } else { OtherItems.Add($"- *{it[0]}*"); }
                        List<string> HelpText = new List<string>() {
                            $"*\\{item[0]}* *...* ***\\{item[1]}***",
                            $"- {item[2]}",
                            item.Count() > 3 ? $"- Referenced as *{item[3]}*" : "",
                            $"Containing list: *ConTEXt.Environments.DocumentStructure*",
                            $"Other commands in this list:"
                        };
                        HelpText.AddRange(OtherItems);
                        return new Hover(HelpText.ToArray<string>(), R);
                    }
            }
            return default;
        }
        public bool IsIndex(WordAtPosition word, string str)
        {
            return word.Word.IndexOf(str, 0, StringComparison.CurrentCultureIgnoreCase) != -1;
        }



        private void Edit_Loaded(object sender, RoutedEventArgs e)
        {
            var edit = (sender as CodeEditor);
            edit.Options.UseTabStops = true;
            edit.Options.WordWrap = WordWrap.On;
            edit.Options.WordBasedSuggestions = true;
            edit.Options.SuggestOnTriggerCharacters = true;
            edit.Options.AcceptSuggestionOnCommitCharacter = true;
            edit.Options.AcceptSuggestionOnEnter = AcceptSuggestionOnEnter.On;
            edit.Options.TabCompletion = TabCompletion.On;
            edit.Options.SuggestSelection = SuggestSelection.RecentlyUsed;
            edit.Options.WrappingIndent = WrappingIndent.Indent;
            edit.Options.AutoIndent = AutoIndent.Full;
            edit.Options.CodeLens = true;
            edit.Options.Contextmenu = true ;
            edit.Options.ParameterHints = new IEditorParameterHintOptions() { Cycle = false, Enabled = true };
            edit.Options.Minimap = new EditorMinimapOptions() { Enabled = App.AppViewModel.Default.MiniMap, ShowSlider = Show.Always, RenderCharacters = true, };
            edit.Options.CursorBlinking = CursorBlinking.Solid;
            edit.Options.DragAndDrop = true;
            edit.Options.ScrollBeyondLastLine = false;
            edit.Options.Folding = App.AppViewModel.Default.CodeFolding;
            edit.Options.FormatOnPaste = true;
            edit.Options.Hover = new EditorHoverOptions() { Enabled = true, Delay=100, Sticky = true };
            edit.Options.LineNumbers = (LineNumbersType)App.AppViewModel.Default.ShowLineNumbers;
            edit.Options.RenderControlCharacters = true;
            edit.Options.QuickSuggestions = true;
            edit.Options.SnippetSuggestions = SnippetSuggestions.Inline;
            edit.Options.Links = true;
            edit.Options.MouseWheelZoom = true;
            edit.Options.OccurrencesHighlight = false;
            edit.Options.RoundedSelection = true;

            var myDataObject = edit.DataContext as FileItem;
            Binding myBinding = new Binding() { Path = new PropertyPath("FileContent"), Mode = BindingMode.TwoWay, UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged };
            myBinding.Source = myDataObject;
            BindingOperations.SetBinding(edit, CodeEditor.TextProperty, myBinding);



            edit.Focus(FocusState.Programmatic);
        }

        public string CodeContent
        {
            get { return (string)GetValue(CodeContentProperty); }
            set { SetValue(CodeContentProperty, value); }
        }

        public static List<CompletionItem> ContextEnvironmentStructureKeywords2 => ContextEnvironmentStructureKeywords21;

        public static List<CompletionItem> ContextEnvironmentStructureKeywords21 => ContextEnvironmentStructureKeywords22;

        public static List<CompletionItem> ContextEnvironmentStructureKeywords22 => contextEnvironmentStructureKeywords2;

        // Using a DependencyProperty as the backing store for Content.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty CodeContentProperty =
            DependencyProperty.Register("CodeContent", typeof(string), typeof(Editor), new PropertyMetadata(""));

        private void ClearLog_Click(object sender, RoutedEventArgs e)
        {
            Log.Blocks.Clear();
            logline = 0;
        }

        private void OnFileDragOver(object sender, DragEventArgs e)
        {
            e.AcceptedOperation = DataPackageOperation.Link;

            //var a =  e.DataView.;
            //  App.AppViewModel.LOG(a);
            if (e.DragUIOverride != null)
            {
                e.DragUIOverride.Caption = "Open File";
                e.DragUIOverride.IsContentVisible = true;
            }
        }

        private void OnFileDragLeave(object sender, DragEventArgs e)
        {

        }

        private async void OnFileDrop(object sender, DragEventArgs e)
        {
            if (e.DataView.Contains(StandardDataFormats.StorageItems))
            {
                IReadOnlyList<IStorageItem> items = await e.DataView.GetStorageItemsAsync();
                foreach (StorageFile file in items)
                {
                    var fileitem = new FileItem(file) { };
                    App.AppViewModel.OpenFile(fileitem);
                }
            }
            e.Handled = true;
        }

        private  void PDFReader_Loading(FrameworkElement sender, object args)
        {
            //PDFReader.ContainsFullScreenElementChanged += async (a,b) =>  {
            //    var applicationView = ApplicationView.GetForCurrentView();

            //    if (a.ContainsFullScreenElement)
            //    {
            //        var view = CoreApplication.CreateNewView();
            //        int id = 0;

            //        await view.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            //        {
            //            var frame = new Frame();
            //            frame.Navigate(typeof(PDFViewerFullscreen), null);
            //            Window.Current.Content = frame;
            //            Window.Current.Activate();
            //            id = ApplicationView.GetForCurrentView().Id;
            //        });

            //       await ApplicationViewSwitcher.TryShowAsStandaloneAsync(id);
            //        applicationView.TryEnterFullScreenMode();
            //    }
            //    else if (applicationView.IsFullScreenMode)
            //    {
            //        applicationView.ExitFullScreenMode();
            //    }
            //};
        }

        private CodeEditor NewCodeEditor(string Content = "")
        {
            CodeEditor ce = new CodeEditor() { };
            ce.CodeLanguage = "context";
            ce.Loaded += Edit_Loaded;
            ce.Loading += Edit_Loading;
            ce.Text = Content;
            return ce;
        }
        private Rectangle NewRectangle()
        {
            return new Rectangle()
            {
                Height = 10,
                Width = 10,
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Top,
                Fill = (SolidColorBrush)Resources["ApplicationPageBackgroundThemeBrush"]
            };

        }
        //private TabViewItem InitializeTabViewItem(string Header = "", string Content = "")
        //{
        //    Grid g = new Grid() { };
        //    g.Children.Add(NewCodeEditor(Content));
        //    g.Children.Add(NewRectangle());
        //    return new TabViewItem() { Content = g, Header = Header };
        //}

        

        private async void AddTabButtonUpper_Click(object sender, RoutedEventArgs e)
        {
            App.AppViewModel.Default.TexFileName = "newfile.tex";
            App.AppViewModel.LOG("Creating new file");
            ValueSet request = new ValueSet
            {
                { "newtexfile", true }
            };
            AppServiceResponse response = await App.AppViewModel.appServiceConnection.SendMessageAsync(request);


            App.AppViewModel.LOG((string)response.Message.Values.FirstOrDefault());

            //App.AppViewModel.FileItems.Add(App.AppViewModel.InitializeFileItem("bla", "blub"));
            // App.AppViewModel.LOG((DataContext).GetType().ToString());
            //((ViewModel)DataContext).TabViewItems.Add(tvi);
        }

        private async void ControlEnter_Invoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
        {
            await Save();
            await Compile();
        }

        private void PDFReader_NewWindowRequested(WebView sender, WebViewNewWindowRequestedEventArgs args)
        {
            args.Handled = true;
        }

        private void Log_Loaded(object sender, RoutedEventArgs e)
        {
        }

        private void Log_Loading(FrameworkElement sender, object args)
        {
        }

        private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }

        private void Tabs_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                var item = (FileItem)e.AddedItems.FirstOrDefault();
                App.AppViewModel.Default.LastActiveFileName = item.FileName;
                // App.AppViewModel.Default.TexFileFolder = 
            }
            catch (Exception ex)
            {
                App.AppViewModel.LOG(ex.Message);
            }
        }

        private void Tabs_TabCloseRequested(Microsoft.UI.Xaml.Controls.TabView sender, Microsoft.UI.Xaml.Controls.TabViewTabCloseRequestedEventArgs args)
        {
            var fi = args.Tab.DataContext as FileItem;
            App.AppViewModel.FileItems.Remove(fi);
        }
    }

    public class EnvironmentItem
    {
        public string Start { get; set; }
        public string Stop { get; set; }
        public string Info1 { get; set; }
        public string Info2 { get; set; }
        public string Info3 { get; set; }
    }

    public class LanguageProvider : CompletionItemProvider
    {
        public string[] TriggerCharacters => new string[] { @"\"
        };

        public IAsyncOperation<CompletionList> ProvideCompletionItemsAsync(IModel document, Position position, CompletionContext context)
        {
            return AsyncInfo.Run(async delegate (System.Threading.CancellationToken cancelationToken)
            {
                var textUntilPosition = await document.GetValueInRangeAsync(new Range(1, 1, position.LineNumber, position.Column));
                string[] starts = new string[] { "startxtablenext", "startxtablehead", "startxtablefoot", "startxtablebody", "startxtable", "startxrowgroup", "startxrow", "startxmlsetups", "startxmlraw", "startxmlinlineverbatim", "startxmldisplayverbatim", "startxgroup", "startxcolumn", "startxcellgroup", "startxcell", "startvtopregister", "startvtop", "startviewerlayer", "startvboxtohboxseparator", "startvboxtohbox", "startvboxregister", "startvbox", "startusingbtxspecification", "startuserdata", "startusemathstyleparameter", "startuseMPgraphic", "startusableMPgraphic", "startunpacked", "startunittext", "startuniqueMPpagegraphic", "startuniqueMPgraphic", "starttyping", "starttypescriptcollection", "starttypescript", "starttransparent", "starttokens", "starttokenlist", "starttitle", "starttextrule", "starttextmakeup", "starttextflow", "starttextcolorintent", "starttextcolor", "starttextbackgroundmanual", "starttextbackground", "starttext", "starttexdefinition", "starttexcode", "starttaglabeltext", "starttagged", "starttabulatetail", "starttabulatehead", "starttabulate", "starttabletext", "starttabletail", "starttables", "starttablehead", "starttable", "startsymbolset", "startsuffixtext", "startsubsubsubsubsubject", "startsubsubsubsubsection", "startsubsubsubsubject", "startsubsubsubsection", "startsubsubsubject", "startsubsubsection", "startsubsubject", "startsubstack", "startsubsentence", "startsubsection", "startsubjectlevel", "startsubject", "startsubformulas", "startstyle", "startstrut", "startstructurepageregister", "startstrictinspectnextcharacter", "startstaticMPgraphic", "startstaticMPfigure", "startstandardmakeup", "startspread", "startsplittext", "startsplitformula", "startspformula", "startspeech", "startspecialitem", "startsimplecolumns", "startsidebar", "startshift", "startshapebox", "startsetups", "startsectionlevel", "startsectionblockenvironment", "startsectionblock", "startsection", "startsdformula", "startscript", "startruby", "startrightaligned", "startreusableMPgraphic", "startregister", "startregime", "startreferenceprefix", "startreadingfile", "startrawsetups", "startrandomseed", "startrandomized", "startquote", "startquotation", "startpunctuation", "startpublication", "startprotectedcolors", "startprotect", "startproject", "startproduct", "startprocesscommalist", "startprocesscommacommand", "startprocessassignmentlist", "startprocessassignmentcommand", "startprefixtext", "startpostponingnotes", "startpostponing", "startpositive", "startpositionoverlay", "startpositioning", "startplacetable", "startplacepairedbox", "startplacelegend", "startplaceintermezzo", "startplacegraphic", "startplaceformula", "startplacefloat", "startplacefigure", "startplacechemical", "startpath", "startpart", "startparbuilder", "startparagraphscell", "startparagraphs", "startparagraph", "startpar", "startpagemakeup", "startpagelayout", "startpagefigure", "startpagecomment", "startpagecolumns", "startpacked", "startoverprint", "startoverlay", "startoutputstream", "startopposite", "startoperatortext", "startnotmode", "startnotext", "startnotallmodes", "startnointerference", "startnicelyfilledbox", "startnegative", "startnarrower", "startnarrow", "startnamedsubformulas", "startnamedsection", "startmpformula", "startmoduletestsection", "startmodule", "startmodeset", "startmode", "startmixedcolumns", "startmiddlemakeup", "startmiddlealigned", "startmidaligned", "startmdformula", "startmaxaligned", "startmatrix", "startmatrices", "startmathstyle", "startmathmode", "startmathmatrix", "startmathlabeltext", "startmathcases", "startmathalignment", "startmarkpages", "startmarkedcontent", "startmarginrule", "startmarginblock", "startmakeup", "startluasetups", "startluaparameterset", "startluacode", "startlua", "startlocalsetups", "startlocalnotes", "startlocallinecorrection", "startlocalheadsetup", "startlocalfootnotes", "startlinetablehead", "startlinetablecell", "startlinetablebody", "startlinetable", "startlines", "startlinenumbering", "startlinenote", "startlinefiller", "startlinecorrection", "startlinealignment", "startline", "startlegend", "startleftaligned", "startlayout", "startlanguage", "startlabeltext", "startknockout", "startitemize", "startitemgroupcolumns", "startitemgroup", "startitem", "startintertext", "startintermezzotext", "startinterface", "startinteractionmenu", "startinteraction", "startindentedtext", "startindentation", "startimath", "starthyphenation", "starthighlight", "starthiding", "starthelptext", "startheadtext", "starthead", "starthboxregister", "starthboxestohbox", "starthbox", "starthanging", "startgridsnapping", "startgraphictext", "startgoto", "startfrontmatter", "startframedtext", "startframedtable", "startframedrow", "startframedcontent", "startframedcell", "startframed", "startformulas", "startformula", "startfootnote", "startfontsolution", "startfontclass", "startfont", "startfloatcombination", "startfixed", "startfittingpage", "startfiguretext", "startfigure", "startfact", "startfacingfloat", "startexternalfigurecollection", "startextendedcatcodetable", "startexpandedcollect", "startexpanded", "startexceptions", "startenvironment", "startendofline", "startendnote", "startembeddedxtable", "startelement", "starteffect", "startdocument", "startdmath", "startdisplaymath", "startdelimitedtext", "startdelimited", "startcurrentlistentrywrapper", "startcurrentcolor", "startctxfunctiondefinition", "startctxfunction", "startcontextdefinitioncode", "startcontextcode", "startcomponent", "startcomment", "startcombination", "startcolumnspan", "startcolumnsetspan", "startcolumnset", "startcolumns", "startcolorset", "startcoloronly", "startcolorintent", "startcolor", "startcollecting", "startcollect", "startchemicaltext", "startchemical", "startcheckedfences", "startcharacteralign", "startchapter", "startcenteraligned", "startcatcodetable", "startcases", "startbuffer", "startbtxrenderingdefinitions", "startbtxlabeltext", "startboxedcolumns", "startbordermatrix", "startbodymatter", "startblockquote", "startbitmapimage", "startbbordermatrix", "startbar", "startbackmatter", "startbackground", "startattachment", "startaside", "startarrangedpages", "startappendices", "startallmodes", "startalignment", "startalign", "startXML", "startTY", "startTX", "startTRs", "startTR", "startTN", "startTH", "startTEXpage", "startTEX", "startTDs", "startTD", "startTC", "startTABLEnext", "startTABLEhead", "startTABLEfoot", "startTABLEbody", "startTABLE", "startPARSEDXML", "startMPrun", "startMPpositionmethod", "startMPpositiongraphic", "startMPpage", "startMPinitializations", "startMPinclusions", "startMPextensions", "startMPenvironment", "startMPdrawing", "startMPdefinitions", "startMPcode", "startMPclip", "startMP", "startLUA", "startJSpreamble", "startJScode" };
                List<CompletionItem> ContextEnvironmentStructureKeywords = new List<CompletionItem>();
                var list = starts.Select(array => new CompletionItem(array, array, CompletionItemKind.Field) { Documentation = new IMarkdownString("Document content"), Detail = "Det" }).ToList();

                //Debug.WriteLine(textUntilPosition);
                //if (textUntilPosition.EndsWith(@"\"))
                //{
                //    Debug.WriteLine("EndsWith\\");
                //    return new CompletionList()
                //    {
                //        Items = new List<CompletionItem>()
                //        {
                //            new CompletionItem(@"\starttext", CompletionItemKind.Keyword){ Documentation = new IMarkdownString("LOL"), CommitCharacters = new string[] {@"\","s" }, InsertText = new SnippetString("blub"), Detail = "detaill", Label = "labell" },
                //            new CompletionItem(@"\stoptext", CompletionItemKind.Keyword),
                //        }
                //    };
                //}
                //else 
                //if (context.TriggerKind == SuggestTriggerKind.TriggerCharacter)
                //{
                //    Debug.WriteLine("triggercharacter");
                //    return new CompletionList()
                //    {
                //        Items = 
                //        new List<CompletionItem>()
                //        {
                //            new CompletionItem(@"\starttext", CompletionItemKind.Keyword){ Documentation = new IMarkdownString("LOL"), CommitCharacters = new string[] {@"\","s" }, InsertText = new SnippetString("blub"), Detail = "detaill"},
                //            new CompletionItem(@"\stoptext", CompletionItemKind.Keyword),
                //        }
                //    };
                //}
                return new CompletionList()
                {
                    Suggestions = list.ToArray()

                };
            });
        }

        public IAsyncOperation<CompletionItem> ResolveCompletionItemAsync(IModel document, Position position, CompletionItem item)
        {
            // throw new NotImplementedException();
            return AsyncInfo.Run(async delegate (System.Threading.CancellationToken cancelationToken)
            {
                item.InsertText = item.Label + "\nbla"; 
                return item; // throw new NotImplementedException();

            });
        }
    }
    class RunAction : IActionDescriptor
    {
        public string ContextMenuGroupId => "navigation";
        public float ContextMenuOrder => 1.5f;
        public string Id => "meta-test-action";
        public string KeybindingContext => null;
        //public int[] Keybindings => new int[] { Monaco.KeyMod.Chord(Monaco.KeyMod.CtrlCmd | Monaco.KeyCode.Enter, Monaco.KeyCode.F5) };
        public int[] Keybindings => new int[] { Monaco.KeyMod.CtrlCmd | Monaco.KeyCode.Enter };
        public string Label => "Save & Compile";
        public string Precondition => null;

        public async void Run(CodeEditor editor, object[] obj)
        {
            await (((Window.Current.Content as Frame).Content as MainPage).contentFrame.Content as Editor).Save();
            await (((Window.Current.Content as Frame).Content as MainPage).contentFrame.Content as Editor).Compile();
            editor.Focus(Windows.UI.Xaml.FocusState.Programmatic);
        }
    }
    public class RichTextBlockHelper : DependencyObject
    {
        public static string GetText(DependencyObject obj)
        {
            return (string)obj.GetValue(BlocksProperty);
        }

        public static void SetText(DependencyObject obj, string value)
        {
            obj.SetValue(BlocksProperty, value);

        }

        // Using a DependencyProperty as the backing store for Text.  
        //This enables animation, styling, binding, etc...
        public static readonly DependencyProperty BlocksProperty =
            DependencyProperty.RegisterAttached("Text", typeof(string),
            typeof(RichTextBlockHelper),
            new PropertyMetadata(String.Empty, OnTextChanged));
        private static int logline = 0;
        public static Paragraph LOG(string log)
        {

            logline++;
            Paragraph paragraph = new Paragraph();
            Run run1 = new Run
            {
                Text = string.Format("{0,3:###}", logline) + ": "
            };
            var DefaultTheme = new Windows.UI.ViewManagement.UISettings();

            var lightbrush = DefaultTheme.GetColorValue(Windows.UI.ViewManagement.UIColorType.Background);
            byte max = 255;
            run1.Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(125, (byte)(max - lightbrush.R), (byte)(max - lightbrush.G), (byte)(max - lightbrush.B)));
            Run run2 = new Run
            {
                Text = log
            };
            paragraph.Inlines.Add(run1);
            paragraph.Inlines.Add(run2);
            //Log.Blocks.Add(paragraph);
            //Blocks.Add(paragraph);
            return paragraph;
            //Log.UpdateLayout();
            //logscroll.UpdateLayout();
            //logscroll.ChangeView(0, logscroll.ScrollableHeight, 1);
        }
        private static void OnTextChanged(DependencyObject sender,
            DependencyPropertyChangedEventArgs e)
        {
            try
            {
                if (sender is RichTextBlock control)
                {
                    //control.Blocks.Clear();
                    var value = e.NewValue as string;

                    control.Blocks.Add(LOG(value));
                    control.UpdateLayout();

                    var logscroll = (ScrollViewer)VisualTreeHelper.GetParent(VisualTreeHelper.GetParent(VisualTreeHelper.GetParent(VisualTreeHelper.GetParent(control))));
                    logscroll.UpdateLayout();
                    logscroll.ChangeView(0, logscroll.ScrollableHeight, 1);
                }
            }
            catch (Exception ex)
            {

            }
        }
        private static T FindParent<T>(DependencyObject child)
            where T : DependencyObject
        {
            T parent = VisualTreeHelper.GetParent(child) as T;
            if (parent != null)
                return parent;
            else
                return FindParent<T>(parent);
        }
    }
    class EditorHoverProvider : HoverProvider
    {
        public IAsyncOperation<Hover> ProvideHover(IModel model, Position position)
        {
            return AsyncInfo.Run(async delegate (CancellationToken cancelationToken)
            {
                var word = await model.GetWordAtPositionAsync(position);
                if (word != null && word.Word != null && word.Word.IndexOf("Hit", 0, StringComparison.CurrentCultureIgnoreCase) != -1)
                {
                    return new Hover(new string[]
                    {
                        "*Hit* - press the keys following together.",
                        "Some **more** text is here.",
                        "And a [link](https://www.github.com/)."
                    }, new Range(position.LineNumber, position.Column, position.LineNumber, position.Column + 5));
                }

                return default(Hover);
            });
        }
    }

}
