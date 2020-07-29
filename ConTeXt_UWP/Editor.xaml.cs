﻿using Monaco;
using Monaco.Editor;
using Monaco.Extensions;
using Monaco.Languages;
using Monaco.Monaco;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading;
using System.Threading.Tasks;
using Windows.ApplicationModel.AppService;
using Windows.ApplicationModel.DataTransfer;
using Windows.Data.Json;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
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
        // Using a DependencyProperty as the backing store for Content.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty CodeContentProperty =
            DependencyProperty.Register("CodeContent", typeof(string), typeof(Editor), new PropertyMetadata(""));

        public ViewModel currentViewModel = App.VM;
        public bool loaded = false;
        public int logline = 0;

        private DispatcherTimer dispatcherTimer;
        private bool editloadet = false;
        private DateTimeOffset lastTime = DateTimeOffset.Now;
        private DateTimeOffset startTime;
        private DateTimeOffset stopTime;
        private int timesTicked = 1;
        private int timesToTick = 10;
        public Editor()
        {
            this.InitializeComponent();
            this.NavigationCacheMode = NavigationCacheMode.Disabled;
            CurrentEditor = this;
        }

        public static Editor CurrentEditor { get; set; }
        public string CodeContent
        {
            get { return (string)GetValue(CodeContentProperty); }
            set { SetValue(CodeContentProperty, value); }
        }

        public async Task Compile(bool compileRoot = false, FileItem fileToCompile = null)
        {
            if (!App.VM.IsSaving)
                try
                {
                    App.VM.IsError = false;
                    App.VM.IsPaused = false;
                    App.VM.IsSaving = true;

                    string[] modes = new string[] { };
                    if (App.VM.CurrentProject != null)
                        modes = App.VM.CurrentProject.Modes.Where(x => x.IsSelected).Select(x => x.Name).ToArray();
                    if (modes.Length > 0 && App.VM.Default.UseModes)
                        App.VM.Default.Modes = string.Join(",", modes);
                    else App.VM.Default.Modes = "";

                    FileItem filetocompile = null;
                    if (compileRoot)
                    {
                        FileItem[] root = new FileItem[] { };
                        if (App.VM.CurrentProject != null)
                            root = App.VM.CurrentProject.Directory.Where(x => x.IsRoot).ToArray();
                        if (root.Length > 0)
                            filetocompile = root.FirstOrDefault();
                        else
                            filetocompile = fileToCompile ?? App.VM.CurrentFileItem;
                    }
                    else
                    {
                        filetocompile = fileToCompile ?? App.VM.CurrentFileItem;
                    }
                    string logtext = "Compiling " + filetocompile.File.Name;
                    if (modes.Length > 0 && App.VM.Default.UseModes)
                        logtext += " with modes: " + App.VM.Default.Modes;
                    if (App.VM.Default.AdditionalParameters.Trim().Length > 0)
                        logtext += " with parameters: " + App.VM.Default.AdditionalParameters;
                    App.VM.LOG(logtext);
                    App.VM.Default.TexFileFolder = filetocompile.FileFolder;
                    App.VM.Default.TexFileName = filetocompile.FileName;
                    App.VM.Default.TexFilePath = filetocompile.File.Path;
                    ValueSet request = new ValueSet { { "compile", true } };
                    AppServiceResponse response = await App.VM.appServiceConnection.SendMessageAsync(request);
                    // display the response key/value pairs
                    foreach (string key in response.Message.Keys)
                    {
                        if ((string)response.Message[key] == "compiled")
                        {
                            string local = ApplicationData.Current.LocalFolder.Path;
                            string curFile = System.IO.Path.GetFileName(App.VM.Default.TexFilePath);
                            string filewithoutext = System.IO.Path.GetFileNameWithoutExtension(curFile);
                            string curPDF = filewithoutext + ".pdf";
                            string curPDFPath = System.IO.Path.Combine(App.VM.Default.TexFilePath, curPDF);
                            string newPathToFile = System.IO.Path.Combine(local, curPDF);
                            StorageFolder currFolder = await StorageFolder.GetFolderFromPathAsync(App.VM.Default.TexFileFolder);
                            App.VM.LOG("Opening " + System.IO.Path.GetFileNameWithoutExtension(App.VM.Default.TexFileName) + ".pdf");
                            //StorageFile file = await ApplicationData.Current.LocalFolder.GetFileAsync(curPDF);

                            var error = await currFolder.TryGetItemAsync(System.IO.Path.GetFileNameWithoutExtension(App.VM.Default.TexFileName) + "-error.log");
                            if (error != null)
                            {
                                App.VM.IsError = true;
                                var errorfile = error as StorageFile;
                                //var stream = await errorfile.OpenStreamForReadAsync();
                                //byte[] buffer = new byte[stream.Length];
                                //stream.Read(buffer,0,(int)stream.Length);
                                //string text = Convert.ToString(buffer);
                                string text = await FileIO.ReadTextAsync(errorfile);
                                string newtext = text.Replace("  ", "").Replace("return", "").Replace("[\"", "\"").Replace("\"]", "\"").Replace(@"\n", "").Replace("=", ":");
                                var errormessage = JsonConvert.DeserializeObject<ConTeXtErrorMessage>(newtext);
                                App.VM.LOG("Compiler error: " + errormessage.lasttexerror);

                                App.VM.ConTeXtErrorMessage = errormessage;
                            }
                            else
                            {
                                App.VM.IsPaused = true;
                                App.VM.IsError = false;
                                //await Task.Delay(2000);
                                App.VM.IsVisible = false;
                            }

                            if (App.VM.Default.AutoOpenPDF)
                            {
                                StorageFile pdfout = await currFolder.TryGetItemAsync(System.IO.Path.GetFileNameWithoutExtension(App.VM.Default.TexFileName) + ".pdf") as StorageFile;
                                if (pdfout != null)
                                {
                                    if (App.VM.Default.InternalViewer)
                                    {
                                        Stream stream = await pdfout.OpenStreamForReadAsync();
                                        byte[] buffer = new byte[stream.Length];
                                        stream.Read(buffer, 0, (int)stream.Length);
                                        var asBase64 = Convert.ToBase64String(buffer);
                                        await PDFReader.InvokeScriptAsync("openPdfAsBase64", new[] { asBase64 });
                                    }
                                    else
                                    {
                                        await Launcher.LaunchFileAsync(pdfout);
                                    }
                                }
                            }

                            if (App.VM.Default.AutoOpenLOG)
                            {
                                if ((App.VM.Default.AutoOpenLOGOnlyOnError && error != null) | !App.VM.Default.AutoOpenLOGOnlyOnError)
                                {
                                    StorageFile logout = await currFolder.TryGetItemAsync(System.IO.Path.GetFileNameWithoutExtension(App.VM.Default.TexFileName) + ".log") as StorageFile;
                                    if (logout != null)
                                    {
                                        FileItem logFile = new FileItem(logout) {  };
                                        App.VM.OpenFile(logFile);
                                        
                                    }
                                }
                                else if (App.VM.Default.AutoOpenLOGOnlyOnError && error == null)
                                {
                                    if (App.VM.FileItems.Any(x => x.IsLogFile))
                                    {
                                        App.VM.FileItems.Remove(App.VM.FileItems.First(x => x.IsLogFile));
                                    }
                                }
                            }
                        }
                        else
                        {
                            App.VM.LOG("Compiler error");
                        }
                    }
                }
                catch (Exception f)
                {
                    App.VM.IsError = true;
                    App.VM.LOG("Exception at compile: " + f.Message);
                }
            App.VM.IsSaving = false;
        }

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

        public bool IsIndex(WordAtPosition word, string str)
        {
            return word.Word.IndexOf(str, 0, StringComparison.CurrentCultureIgnoreCase) != -1;
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            foreach (var FileEvent in App.VM.FileActivatedEvents)
            {
                if (FileEvent != null)
                {
                    foreach (StorageFile file in FileEvent.Files)
                    {
                        var fileitem = new FileItem(file) { };
                        App.VM.OpenFile(fileitem);
                    }
                }
            }
            App.VM.FileActivatedEvents.Clear();
            loaded = true;
        }
        private async void addMode_Click(object sender, RoutedEventArgs e)
        {
            Mode mode = new Mode();
            string name = "";
            var cd = new ContentDialog() { Title = "Add mode", PrimaryButtonText = "ok", CloseButtonText = "cancel", DefaultButton = ContentDialogButton.Primary };
            TextBox tb = new TextBox() { Text = name };
            cd.Content = tb;
            if (await cd.ShowAsync() == ContentDialogResult.Primary)
            {
                mode.IsSelected = true;
                mode.Name = tb.Text;
                App.VM.CurrentProject.Modes.Add(mode);
                App.VM.Default.SaveSettings();
            }
        }

        private async void Btncompile_Click(object sender, RoutedEventArgs e)
        {
            await App.VM.UWPSave();
            await Compile();
        }
        private async void Btncompileroot_Click(object sender, RoutedEventArgs e)
        {
            await App.VM.UWPSaveAll();
            await Compile(true);
        }

        private async void Btnsave_Click(object sender, RoutedEventArgs e)
        {
            //App.VM.EditorOptions.WordWrap = App.VM.EditorOptions.WordWrap ==  WordWrap.On ? WordWrap.Off : WordWrap.On; ;
            //App.VM.EditorOptions.LineNumbers = LineNumbersType.Relative;

            await App.VM.UWPSave();
        }

        private async void btnsaveall_Click(object sender, RoutedEventArgs e)
        {
            await App.VM.UWPSaveAll();
        }

        private void CheckBox_Checked(object sender, RoutedEventArgs e)
        {
            App.VM.Default.SaveSettings();
        }

        private void ClearLog_Click(object sender, RoutedEventArgs e)
        {
            Log.Blocks.Clear();
            logline = 0;
        }

        private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
        }

        private async void ControlEnter_Invoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
        {
            await App.VM.UWPSave();
            await Compile();
        }

        private void DispatcherTimer_Tick(object sender, object e)
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
        

        private async void Edit_Loading(object sender, RoutedEventArgs e)
        {
            var edit = (sender as CodeEditor);
            var fileitem = edit.DataContext;
            //if (!editloadet)
            var languages = new Monaco.LanguagesHelper(edit);

            await languages.RegisterHoverProviderAsync("context", new EditorHoverProvider());

            await languages.RegisterCompletionItemProviderAsync("context", new LanguageProvider());
            if (fileitem is FileItem file)
            {
                if (file.FileLanguage == "context")
                {
                    await edit.AddActionAsync(new RunAction());
                }
            }
            else
            {
                await edit.AddActionAsync(new RunAction());
            }

            await edit.AddActionAsync(new RunRootAction());

            await edit.AddActionAsync(new SaveAction());
            await edit.AddActionAsync(new SaveAllAction());

            await edit.AddActionAsync(new FileOutlineAction());

           // App.VM.CurrentEditor = edit;

        }
        private async void Help_ItemClick(object sender, ItemClickEventArgs e)
        {
            try
            {
                var hf = e.ClickedItem as Helpfile;
                var lsf = ApplicationData.Current.LocalFolder;
                App.VM.LOG("Opening " + lsf.Path + hf.Path + hf.FileName);
                var sf = await StorageFile.GetFileFromPathAsync(lsf.Path + hf.Path + hf.FileName);

                await Launcher.LaunchFileAsync(sf);
            }
            catch (Exception ex)
            {
                App.VM.LOG(ex.Message);
            }
        }

        private void Log_Loaded(object sender, RoutedEventArgs e)
        {
        }

        private void Log_Loading(FrameworkElement sender, object args)
        {
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

        private void OnFileDragLeave(object sender, DragEventArgs e)
        {
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

        private async void OnFileDrop(object sender, DragEventArgs e)
        {
            if (e.DataView.Contains(StandardDataFormats.StorageItems))
            {
                IReadOnlyList<IStorageItem> items = await e.DataView.GetStorageItemsAsync();
                foreach (StorageFile file in items)
                {
                    var fileitem = new FileItem(file) { };
                    App.VM.OpenFile(fileitem);
                }
            }
            else
            {
                //object obj = null;
                //if (e.DataView.GetType.TryGetValue("FileItem", out obj))
                //{
                //    var fi = obj as FileItem;
                //    if (fi.Type == FileItem.ExplorerItemType.File)
                //    {
                //        App.VM.OpenFile(fi);
                //    }
                //}
            }
            e.Handled = true;
        }

        private void PDFReader_Loading(FrameworkElement sender, object args)
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

        private void PDFReader_NewWindowRequested(WebView sender, WebViewNewWindowRequestedEventArgs args)
        {
            args.Handled = true;
        }

        private void PDFReader_ScriptNotify(object sender, NotifyEventArgs e)
        {
            App.VM.LOG(e.CallingUri.OriginalString);
            App.VM.LOG(e.Value);
        }

       
        private void RemoveMode_Click(object sender, RoutedEventArgs e)
        {
            Mode m = (sender as FrameworkElement).DataContext as Mode;
            App.VM.CurrentProject.Modes.Remove(m);
            App.VM.Default.SaveSettings();
        }

        private void Tabs_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            //try
            //{
            //    var item = (FileItem)e.AddedItems.FirstOrDefault();
            //    App.AppViewModel.Default.LastActiveFileName = item.FileName;
            //    // App.AppViewModel.Default.TexFileFolder =
            //}
            //catch (Exception ex)
            //{
            //    App.AppViewModel.LOG(ex.Message);
            //}
        }

        private async void Tabs_TabCloseRequested(Microsoft.UI.Xaml.Controls.TabView sender, Microsoft.UI.Xaml.Controls.TabViewTabCloseRequestedEventArgs args)
        {
            var fi = args.Tab.DataContext as FileItem;
            if (fi.IsChanged)
            {
                var save = new ContentDialog() { Title = "Do you want to save this file before closing?", PrimaryButtonText = "Yes", SecondaryButtonText = "No", DefaultButton = ContentDialogButton.Primary };

                if (await save.ShowAsync() == ContentDialogResult.Primary)
                {
                    await App.VM.UWPSave(fi);
                }
            }
            if (App.VM.CurrentFileItem == fi)
            {
                //App.VM.CurrentFileItem = new FileItem(null);
            }

            App.VM.FileItems.Remove(fi);
        }
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

                string[] starts = new string[] { "startxtablenext", "startxtablehead", "startxtablefoot", "startxtablebody", "startxtable", "startxrowgroup", "startxrow", "startxmlsetups", "startxmlraw", "startxmlinlineverbatim", "startxmldisplayverbatim", "startxgroup", "startxcolumn", "startxcellgroup", "startxcell", "startvtopregister", "startvtop", "startviewerlayer", "startvboxtohboxseparator", "startvboxtohbox", "startvboxregister", "startvbox", "startusingbtxspecification", "startuserdata", "startusemathstyleparameter", "startuseMPgraphic", "startusableMPgraphic", "startunpacked", "startunittext", "startuniqueMPpagegraphic", "startuniqueMPgraphic", "starttyping", "starttypescriptcollection", "starttypescript", "starttransparent", "starttokens", "starttokenlist", "starttextrule", "starttextmakeup", "starttextflow", "starttextcolorintent", "starttextcolor", "starttextbackgroundmanual", "starttextbackground", "starttext", "starttexdefinition", "starttexcode", "starttaglabeltext", "starttagged", "starttabulatetail", "starttabulatehead", "starttabulate", "starttabletext", "starttabletail", "starttables", "starttablehead", "starttable", "startsymbolset", "startsuffixtext", "startsubstack", "startsubsentence", "startsubjectlevel", "startsubformulas", "startstyle", "startstrut", "startstructurepageregister", "startstrictinspectnextcharacter", "startstaticMPgraphic", "startstaticMPfigure", "startstandardmakeup", "startspread", "startsplittext", "startsplitformula", "startspformula", "startspeech", "startspecialitem", "startsimplecolumns", "startsidebar", "startshift", "startshapebox", "startsetups", "startsectionlevel", "startsectionblockenvironment", "startsectionblock", "startsdformula", "startscript", "startruby", "startrightaligned", "startreusableMPgraphic", "startregister", "startregime", "startreferenceprefix", "startreadingfile", "startrawsetups", "startrandomseed", "startrandomized", "startquote", "startquotation", "startpunctuation", "startpublication", "startprotectedcolors", "startprotect", "startproject", "startproduct", "startprocesscommalist", "startprocesscommacommand", "startprocessassignmentlist", "startprocessassignmentcommand", "startprefixtext", "startpostponingnotes", "startpostponing", "startpositive", "startpositionoverlay", "startpositioning", "startplacetable", "startplacepairedbox", "startplacelegend", "startplaceintermezzo", "startplacegraphic", "startplaceformula", "startplacefloat", "startplacefigure", "startplacechemical", "startpath", "startparbuilder", "startparagraphscell", "startparagraphs", "startparagraph", "startpar", "startpagemakeup", "startpagelayout", "startpagefigure", "startpagecomment", "startpagecolumns", "startpacked", "startoverprint", "startoverlay", "startoutputstream", "startopposite", "startoperatortext", "startnotmode", "startnotext", "startnotallmodes", "startnointerference", "startnicelyfilledbox", "startnegative", "startnarrower", "startnarrow", "startnamedsubformulas", "startnamedsection", "startmpformula", "startmoduletestsection", "startmodule", "startmodeset", "startmode", "startmixedcolumns", "startmiddlemakeup", "startmiddlealigned", "startmidaligned", "startmdformula", "startmaxaligned", "startmatrix", "startmatrices", "startmathstyle", "startmathmode", "startmathmatrix", "startmathlabeltext", "startmathcases", "startmathalignment", "startmarkpages", "startmarkedcontent", "startmarginrule", "startmarginblock", "startmakeup", "startluasetups", "startluaparameterset", "startluacode", "startlua", "startlocalsetups", "startlocalnotes", "startlocallinecorrection", "startlocalheadsetup", "startlocalfootnotes", "startlinetablehead", "startlinetablecell", "startlinetablebody", "startlinetable", "startlines", "startlinenumbering", "startlinenote", "startlinefiller", "startlinecorrection", "startlinealignment", "startline", "startlegend", "startleftaligned", "startlayout", "startlanguage", "startlabeltext", "startknockout", "startitemize", "startitemgroupcolumns", "startitemgroup", "startitem", "startintertext", "startintermezzotext", "startinterface", "startinteractionmenu", "startinteraction", "startindentedtext", "startindentation", "startimath", "starthyphenation", "starthighlight", "starthiding", "starthelptext", "startheadtext", "starthead", "starthboxregister", "starthboxestohbox", "starthbox", "starthanging", "startgridsnapping", "startgraphictext", "startgoto", "startfrontmatter", "startframedtext", "startframedtable", "startframedrow", "startframedcontent", "startframedcell", "startframed", "startformulas", "startformula", "startfootnote", "startfontsolution", "startfontclass", "startfont", "startfloatcombination", "startfixed", "startfittingpage", "startfiguretext", "startfigure", "startfact", "startfacingfloat", "startexternalfigurecollection", "startextendedcatcodetable", "startexpandedcollect", "startexpanded", "startexceptions", "startenvironment", "startendofline", "startendnote", "startembeddedxtable", "startelement", "starteffect", "startdocument", "startdmath", "startdisplaymath", "startdelimitedtext", "startdelimited", "startcurrentlistentrywrapper", "startcurrentcolor", "startctxfunctiondefinition", "startctxfunction", "startcontextdefinitioncode", "startcontextcode", "startcomponent", "startcomment", "startcombination", "startcolumnspan", "startcolumnsetspan", "startcolumnset", "startcolumns", "startcolorset", "startcoloronly", "startcolorintent", "startcolor", "startcollecting", "startcollect", "startchemicaltext", "startchemical", "startcheckedfences", "startcharacteralign", "startcenteraligned", "startcatcodetable", "startcases", "startbuffer", "startbtxrenderingdefinitions", "startbtxlabeltext", "startboxedcolumns", "startbordermatrix", "startbodymatter", "startblockquote", "startbitmapimage", "startbbordermatrix", "startbar", "startbackmatter", "startbackground", "startattachment", "startaside", "startarrangedpages", "startappendices", "startallmodes", "startalignment", "startalign", "startXML", "startTY", "startTX", "startTRs", "startTR", "startTN", "startTH", "startTEXpage", "startTEX", "startTDs", "startTD", "startTC", "startTABLEnext", "startTABLEhead", "startTABLEfoot", "startTABLEbody", "startTABLE", "startPARSEDXML", "startMPrun", "startMPpositionmethod", "startMPpositiongraphic", "startMPpage", "startMPinitializations", "startMPinclusions", "startMPextensions", "startMPenvironment", "startMPdrawing", "startMPdefinitions", "startMPcode", "startMPclip", "startMP", "startLUA", "startJSpreamble", "startJScode" };
                var list = starts.Select(array => new CompletionItem(@"\" + array, array + "\n\t$0\n\\" + array.Replace("start", "stop"), CompletionItemKind.Interface) { Documentation = new IMarkdownString("Start-Stop Environment"), Detail = "Environment", InsertTextRules = CompletionItemInsertTextRule.InsertAsSnippet }).ToList();
                string[] startsection = new string[] { "startsection", "startsubsection", "startsubsubsection", "startsubsubsubsection", "startsubject", "startsubsubject", "startsubsubsubject", "startsubsubsubsubject", "startpart", "starttitle", "startchapter", };
                var listsection = startsection.Select(array => new CompletionItem(@"\" + array, array + "[title={${1:tiltle}}]\n\t$0\n\\" + array.Replace("start", "stop"), CompletionItemKind.Field) { Documentation = new IMarkdownString("Sectioning environment"), Detail = "Environment", InsertTextRules = CompletionItemInsertTextRule.InsertAsSnippet }).ToList();
                string[] commands = new string[] { "zwnj", "zwj", "zstroke", "zhook", "zeta", "zerowidthspace", "zerowidthnobreakspace", "zdotaccent", "zcaron", "zacute", "ytilde", "ymacron", "yhook", "ygrave", "yen", "ydotbelow", "ydiaeresis", "ycircumflex", "yacute", "xypos", "xxfrac", "xtwoheadrightarrow", "xtwoheadleftarrow", "xtriplerel", "xsplitstring", "xrightoverleftarrow", "xrightleftharpoons", "xrightharpoonup", "xrightharpoondown", "xrightarrow", "xrel", "xmlverbatim", "xmlvalue", "xmltofile", "xmltobufferverbose", "xmltobuffer", "xmltext", "xmltexentity", "xmltag", "xmlstrippednolines", "xmlstripped", "xmlstripnolines", "xmlstrip", "xmlsnippet", "xmlshow", "xmlsetup", "xmlsetsetup", "xmlsetparam", "xmlsetpar", "xmlsetinjectors", "xmlsetfunction", "xmlsetentity", "xmlsetattribute", "xmlsetatt", "xmlsave", "xmlresetsetups", "xmlresetinjectors", "xmlresetdocumentsetups", "xmlremovesetup", "xmlremovedocumentsetup", "xmlremapnamespace", "xmlremapname", "xmlregisterns", "xmlregisteredsetups", "xmlregistereddocumentsetups", "xmlrefatt", "xmlraw", "xmlpure", "xmlprocessfile", "xmlprocessdata", "xmlprocessbuffer", "xmlprettyprinttext", "xmlprettyprint", "xmlprependsetup", "xmlprependdocumentsetup", "xmlposition", "xmlpos", "xmlpath", "xmlparam", "xmlpar", "xmlnonspace", "xmlnamespace", "xmlname", "xmlmapvalue", "xmlmain", "xmlloadonly", "xmlloadfile", "xmlloaddirectives", "xmlloaddata", "xmlloadbuffer", "xmllastpar", "xmllastmatch", "xmllastatt", "xmllast", "xmlinstalldirective", "xmlinlineverbatim", "xmlinlineprettyprinttext", "xmlinlineprettyprint", "xmlinjector", "xmlinfo", "xmlinclusions", "xmlinclusion", "xmlinclude", "xmlflushtext", "xmlflushspacewise", "xmlflushpure", "xmlflushlinewise", "xmlflushdocumentsetups", "xmlflushcontext", "xmlflush", "xmlfirst", "xmlfilter", "xmlelement", "xmldoiftext", "xmldoifselfempty", "xmldoifnottext", "xmldoifnotselfempty", "xmldoifnotatt", "xmldoifnot", "xmldoifelsevalue", "xmldoifelsetext", "xmldoifelseselfempty", "xmldoifelseempty", "xmldoifelseatt", "xmldoifelse", "xmldoifatt", "xmldoif", "xmldisplayverbatim", "xmldirectivesbefore", "xmldirectivesafter", "xmldirectives", "xmldefaulttotext", "xmlcount", "xmlcontext", "xmlconcatrange", "xmlconcat", "xmlcommand", "xmlchecknamespace", "xmlchainattdef", "xmlchainatt", "xmlbeforesetup", "xmlbeforedocumentsetup", "xmlbadinclusions", "xmlattributedef", "xmlattribute", "xmlattdef", "xmlatt", "xmlapplyselectors", "xmlappendsetup", "xmlappenddocumentsetup", "xmlall", "xmlaftersetup", "xmlafterdocumentsetup", "xmladdindex", "xmapsto", "xleftrightharpoons", "xleftrightarrow", "xleftharpoonup", "xleftharpoondown", "xleftarrow", "xi", "xhookrightarrow", "xhookleftarrow", "xfrac", "xequal", "xdefconvertedargument", "xRightarrow", "xLeftrightarrow", "xLeftarrow", "writetolist", "writestatus", "writedatatolist", "writebetweenlist", "wr", "wp", "wordtonumber", "words", "wordright", "word", "withoutpt", "widthspanningtext", "widthofstring", "widetilde", "widehat", "whitearrowupfrombar", "weekday", "wedgeeq", "wedge", "wdofstring", "wcircumflex", "vspacing", "vspace", "vsmashed", "vsmashbox", "vsmash", "vpos", "vphantom", "vl", "viewerlayer", "vglue", "veryraggedright", "veryraggedleft", "veryraggedcenter", "verticalpositionbar", "verticalgrowingbar", "vert", "version", "verbosenumber", "verbatimstring", "verbatim", "veeeq", "veebar", "vee", "vec", "vdots", "vdash", "vboxreference", "vartheta", "varsigma", "varrho", "varpi", "varphi", "varnothing", "varkappa", "varepsilon", "varTheta", "vDash", "utilityregisterlength", "utilde", "utfupper", "utflower", "utfchar", "usezipfile", "useurl", "usetypescriptfile", "usetypescript", "usetexmodule", "usesymbols", "usesubpath", "usestaticMPfigure", "usesetupsparameter", "userpagenumber", "usereferenceparameter", "useproject", "useprofileparameter", "useproduct", "usemodule", "usemathstyleparameter", "useluamodule", "uselanguageparameter", "useinterlinespaceparameter", "useindentnextparameter", "useindentingparameter", "usegridparameter", "usefile", "usefigurebase", "useexternalsoundtrack", "useexternalrendering", "useexternalfigure", "useexternaldocument", "useenvironment", "usedummystyleparameter", "usedummystyleandcolor", "usedummycolorparameter", "usedirectory", "usecomponent", "usecolors", "usecitation", "usebtxdefinitions", "usebtxdataset", "usebodyfontparameter", "usebodyfont", "useblocks", "useblankparameter", "usealignparameter", "useURL", "useMPvariables", "useMPrun", "useMPlibrary", "useMPgraphic", "useMPenvironmentbuffer", "useJSscripts", "url", "uring", "urcorner", "upwhitearrow", "upuparrows", "upsilon", "upperrightsinglesixquote", "upperrightsingleninequote", "upperrightdoublesixquote", "upperrightdoubleninequote", "upperleftsinglesixquote", "upperleftsingleninequote", "upperleftdoublesixquote", "upperleftdoubleninequote", "uppercasestring", "uppercased", "uplus", "upharpoonright", "upharpoonleft", "updownarrows", "updownarrowbar", "updownarrow", "updasharrow", "uparrow", "upand", "uogonek", "untexcommand", "untexargument", "unspacestring", "unspaced", "unspaceargument", "unspaceafter", "unregisterhyphenationpattern", "unprotected", "unknown", "unittext", "unitslow", "unitshigh", "unitlanguage", "unit", "uniqueMPpagegraphic", "uniqueMPgraphic", "unihex", "unhhbox", "unframed", "unexpandeddocumentvariable", "undoassign", "understrikes", "understrike", "underset", "underrightarrow", "underrandoms", "underrandom", "underparent", "underleftarrow", "underdots", "underdot", "underdashes", "underdash", "underbracket", "underbrace", "underbars", "underbar", "undepthed", "undefinevalue", "umacron", "ulcorner", "uinvertedbreve", "uhungarumlaut", "uhorntilde", "uhornhook", "uhorngrave", "uhorndotbelow", "uhornacute", "uhorn", "uhook", "ugrave", "uedcatcodecommand", "udoublegrave", "udots", "udotbelow", "udiaeresismacron", "udiaeresisgrave", "udiaeresiscaron", "udiaeresisacute", "udiaeresis", "uconvertnumber", "ucircumflex", "ucaron", "ubreve", "uacute", "typesetfile", "typesetbuffer", "typescripttwo", "typescriptthree", "typescriptprefix", "typescriptone", "typeinlinebuffer", "typefile", "typeface", "typedefinedbuffer", "typebuffer", "type", "typ", "txx", "tx", "twothirds", "twosuperior", "twoheaduparrow", "twoheadrightarrowtail", "twoheadrightarrow", "twoheadleftarrow", "twoheaddownarrow", "twofifths", "twodigitrounding", "turnediota", "ttwoheadrightarrow", "ttwoheadleftarrow", "ttriplerel", "ttraggedright", "tstroke", "truefontname", "truefilename", "tripleverticalbar", "tripleprime", "triplebond", "trightoverleftarrow", "trightleftharpoons", "trightharpoonup", "trightharpoondown", "trightarrow", "triangleright", "triangleq", "triangleleft", "triangledown", "triangle", "trel", "transparent", "transparencycomponents", "translate", "trademark", "tracepositions", "traceoutputroutines", "tracedfontname", "tracecatcodetables", "topskippedbox", "toprightbox", "toplinebox", "topleftbox", "topbox", "top", "tooltip", "tolinenote", "tochar", "to", "tmapsto", "tleftrightharpoons", "tleftrightarrow", "tleftharpoonup", "tleftharpoondown", "tleftarrow", "tlap", "title", "tinyfont", "times", "tilde", "tightlayer", "tibetannumerals", "threesuperior", "threequarter", "threeperemspace", "threefifths", "threeeighths", "threedigitrounding", "thorn", "thookrightarrow", "thookleftarrow", "thook", "thirdofthreeunexpanded", "thirdofthreearguments", "thirdofsixarguments", "thirdoffourarguments", "thirdoffivearguments", "thinspace", "thinrules", "thinrule", "thickspace", "theta", "theremainingcharacters", "therefore", "thenormalizedbodyfontsize", "thefirstcharacter", "thainumerals", "thai", "textyen", "textvisiblespace", "textunderscore", "texttilde", "textsterling", "textslash", "textrule", "textring", "textreference", "textpm", "textplus", "textperiod", "textpercent", "textounce", "textormathchars", "textormathchar", "textohm", "textogonek", "textnumero", "textmultiply", "textmu", "textminus", "textmho", "textmath", "textmacron", "textlognot", "textkelvin", "texthyphen", "texthungarumlaut", "texthorizontalbar", "texthash", "textgrave", "textfraction", "textflowcollector", "texteuro", "textellipsis", "textdotaccent", "textdong", "textdollar", "textdiv", "textdiaeresis", "textdegree", "textddag", "textdag", "textcurrency", "textcontrolspace", "textcomma", "textcite", "textcitation", "textcircumflex", "textcircledP", "textcent", "textcelsius", "textcedilla", "textcaron", "textbullet", "textbrokenbar", "textbreve", "textbraceright", "textbraceleft", "textbottomdot", "textbottomcomma", "textbar", "textbackslash", "textat", "textasciitilde", "textasciicircum", "textampersand", "textacute", "textAngstrom", "texsetup", "texdefinition", "tex", "testtokens", "testpagesync", "testpageonly", "testpage", "testfeatureonce", "testfeature", "testcolumn", "testandsplitstring", "test", "tequal", "tcurl", "tcommaaccent", "tcedilla", "tcaron", "tbox", "tbinom", "tau", "taglabeltext", "taglabellanguage", "taggedlabeltexts", "taggedctxcommand", "tabulaterule", "tabulateline", "tabulateautorule", "tabulateautoline", "tRightarrow", "tLeftrightarrow", "tLeftarrow", "systemsetups", "systemloglast", "systemlogfirst", "systemlog", "synctexsetfilename", "synctexresetfilename", "synctexblockfilename", "synchronizewhitespace", "synchronizestrut", "synchronizeoutputstreams", "synchronizemarking", "synchronizeindenting", "synchronizeblank", "symbolreference", "symbol", "switchtointerlinespace", "switchtocolor", "switchtobodyfont", "switchstyleonly", "swarrow", "swaptypeface", "swapmacros", "swapface", "swapdimens", "swapcounts", "surdradical", "surd", "supsetneqq", "supsetneq", "supseteqq", "supseteq", "supset", "sum", "suffixtext", "suffixlanguage", "succsim", "succnsim", "succneqq", "succneq", "succnapprox", "succeqq", "succeq", "succcurlyeq", "succapprox", "succ", "subtractfeature", "subsubsubsubsubject", "subsubsubsubsection", "subsubsubsubject", "subsubsubsection", "subsubsubject", "subsubsection", "subsubject", "substituteincommalist", "subsetneqq", "subsetneq", "subseteqq", "subseteq", "subset", "subsentence", "subsection", "subpagenumber", "subject", "styleinstance", "style", "strutwd", "struttedbox", "struthtdp", "strutht", "strutgap", "strutdp", "strut", "structurevariable", "structureuservariable", "structuretitle", "structurenumber", "structurelistuservariable", "stripspaces", "strippedcsname", "stripcharacter", "strictdoifnextoptionalelse", "strictdoifelsenextoptional", "stretched", "stligature", "stareq", "star", "stackrel", "ssharp", "squaredots", "square", "sqsupsetneq", "sqsupseteq", "sqsupset", "sqsubsetneq", "sqsubseteq", "sqsubset", "sqrt", "sqcup", "sqcap", "spreadhbox", "splitstring", "splitofftype", "splitofftokens", "splitoffroot", "splitoffpath", "splitoffname", "splitoffkind", "splitofffull", "splitoffbase", "splitfrac", "splitfloat", "splitfilename", "splitdfrac", "splitatperiod", "splitatcomma", "splitatcolons", "splitatcolon", "splitatasterisk", "sphericalangle", "speech", "spanishnumerals", "spanishNumerals", "spadesuit", "spaceddigitssymbol", "spaceddigitsseparator", "spaceddigitsmethod", "spaceddigits", "space", "somewhere", "someplace", "somenamedheadnumber", "somelocalfloat", "someline", "somekindoftab", "someheadnumber", "solidus", "softhyphen", "snaptogrid", "smile", "smashedvbox", "smashedhbox", "smashboxed", "smashbox", "smash", "smalltype", "smallslantedbold", "smallslanted", "smallskip", "smallnormal", "smallitalicbold", "smaller", "smallcappedromannumerals", "smallcappedcharacters", "smallboldslanted", "smallbolditalic", "smallbold", "smallbodyfont", "small", "sloveniannumerals", "slovenianNumerals", "slong", "slicepages", "slash", "slantedface", "slantedbold", "slanted", "sixthofsixarguments", "sixperemspace", "singleverticalbar", "singlebond", "singalcharacteralign", "simplereversealignedboxplus", "simplereversealignedbox", "simplegroupedcommand", "simplealignedspreadbox", "simplealignedboxplus", "simplealignedbox", "simeq", "sim", "signalrightpage", "sigma", "showwarning", "showvariable", "showvalue", "showtrackers", "showtokens", "showtimer", "showsymbolset", "showstruts", "showsetupsdefinition", "showsetups", "showprint", "showparentchain", "showpalet", "showotfcomposition", "shownextbox", "showminimalbaseline", "showmessage", "showmargins", "showmakeup", "showlogcategories", "showligatures", "showligature", "showlayoutcomponents", "showlayout", "showkerning", "showjustification", "showinjector", "showhyphens", "showhyphenationtrace", "showhelp", "showgridsnapping", "showgrid", "showglyphs", "showglyphdata", "showframe", "showfontstyle", "showfontstrip", "showfontparameters", "showfontkerns", "showfontitalics", "showfontexpansion", "showfontdata", "showfont", "showexperiments", "showedebuginfo", "showdirsinmargin", "showdirectives", "showcounter", "showcolorstruts", "showcolorset", "showcolorgroup", "showcolorcomponents", "showcolorbar", "showcolor", "showcharratio", "showchardata", "showbtxtables", "showbtxhashedauthors", "showbtxfields", "showbtxdatasetfields", "showbtxdatasetcompleteness", "showbtxdatasetauthors", "showboxes", "showbodyfontenvironment", "showbodyfont", "showattributes", "showallmakeup", "shiftup", "shiftdown", "shiftbox", "sharp", "shapedhbox", "sfrac", "seveneighths", "setxvariables", "setxvariable", "setxvalue", "setxmeasure", "setwidthof", "setvtopregister", "setvisualizerfont", "setvboxregister", "setvariables", "setvariable", "setvalue", "setuxvalue", "setuvalue", "setupxtable", "setupxml", "setupwithargumentswapped", "setupwithargument", "setupwhitespace", "setupvspacing", "setupviewerlayer", "setupversion", "setupuserpagenumber", "setupuserdataalternative", "setupuserdata", "setupurl", "setupunittext", "setupunit", "setuptyping", "setuptype", "setuptoptexts", "setuptop", "setuptooltip", "setuptolerance", "setupthinrules", "setuptexttexts", "setuptextrules", "setuptextflow", "setuptextbackground", "setuptext", "setuptaglabeltext", "setuptagging", "setuptabulation", "setuptabulate", "setuptables", "setupsynonyms", "setupsynctex", "setupsymbolset", "setupsuffixtext", "setupsubpagenumber", "setupsubformulas", "setupsubformula", "setupstyle", "setupstruts", "setupstretched", "setupstartstop", "setupspellchecking", "setupspacing", "setupsorting", "setupsidebar", "setupshift", "setupselector", "setupsectionblock", "setupscripts", "setupscript", "setupscale", "setups", "setupruby", "setuprotate", "setuprenderingwindow", "setupregisters", "setupregister", "setupreferencing", "setupreferencestructureprefix", "setupreferenceprefix", "setupreferenceformat", "setuprealpagenumber", "setupquote", "setupquotation", "setupprograms", "setupprofile", "setupprocessor", "setupprefixtext", "setuppositioning", "setuppositionbar", "setupplacement", "setupperiods", "setupperiodkerning", "setupparallel", "setupparagraphs", "setupparagraphnumbering", "setupparagraphintro", "setupparagraph", "setuppapersize", "setuppaper", "setuppalet", "setuppairedbox", "setuppagetransitions", "setuppagestate", "setuppageshift", "setuppagenumbering", "setuppagenumber", "setuppageinjectionalternative", "setuppageinjection", "setuppagecomment", "setuppagecolumns", "setuppagechecker", "setupoutputroutine", "setupoppositeplacing", "setupoperatortext", "setupoffsetbox", "setupoffset", "setupnotes", "setupnote", "setupnotations", "setupnotation", "setupnarrower", "setupmodule", "setupmixedcolumns", "setupmathstyle", "setupmathstackers", "setupmathradical", "setupmathornament", "setupmathmatrix", "setupmathlabeltext", "setupmathframed", "setupmathfractions", "setupmathfraction", "setupmathfence", "setupmathematics", "setupmathcases", "setupmathalignment", "setupmarking", "setupmarginrules", "setupmarginrule", "setupmarginframed", "setupmargindata", "setupmarginblock", "setupmakeup", "setuplowmidhigh", "setuplowhigh", "setuplow", "setuplocalinterlinespace", "setuplocalfloats", "setuplistextra", "setuplistalternative", "setuplist", "setuplinewidth", "setuplinetable", "setuplines", "setuplinenumbering", "setuplinenote", "setuplinefillers", "setuplinefiller", "setuplegend", "setuplayouttext", "setuplayout", "setuplayeredtext", "setuplayer", "setuplanguage", "setuplabeltext", "setuplabel", "setupitems", "setupitemize", "setupitemizations", "setupitemgroup", "setupitaliccorrection", "setupinterlinespace", "setupinteractionscreen", "setupinteractionmenu", "setupinteractionbar", "setupinteraction", "setupinsertion", "setupinitial", "setupindex", "setupindenting", "setupindentedtext", "setuphyphenmark", "setuphyphenation", "setuphighlight", "setuphigh", "setuphelp", "setupheadtext", "setupheads", "setupheadnumber", "setupheadertexts", "setupheader", "setupheadalternative", "setuphead", "setupglobalreferenceprefix", "setupframedtexts", "setupframedtext", "setupframedtablerow", "setupframedtablecolumn", "setupframedtable", "setupframedcontent", "setupframed", "setupformulaframed", "setupformulae", "setupformula", "setupforms", "setupfootertexts", "setupfooter", "setupfontsolution", "setupfonts", "setupfontprotrusion", "setupfontexpansion", "setupfloatsplitting", "setupfloats", "setupfloatframed", "setupfloat", "setupfittingpage", "setupfirstline", "setupfillinrules", "setupfillinlines", "setupfiller", "setupfieldtotalframed", "setupfields", "setupfieldlabelframed", "setupfieldcontentframed", "setupfieldcategory", "setupfieldbody", "setupfield", "setupfacingfloat", "setupexternalsoundtracks", "setupexternalfigure", "setupexport", "setupenv", "setupenumerations", "setupenumeration", "setupeffect", "setupdocument", "setupdirections", "setupdescription", "setupdelimitedtext", "setupdataset", "setupcounter", "setupcontent", "setupcomment", "setupcombinedlist", "setupcombination", "setupcolumnspan", "setupcolumnsetstart", "setupcolumnsetspan", "setupcolumnsetlines", "setupcolumnsetareatext", "setupcolumnsetarea", "setupcolumnset", "setupcolumns", "setupcolors", "setupcolor", "setupcollector", "setupclipping", "setupchemicalframed", "setupchemical", "setupcharacterspacing", "setupcharacterkerning", "setupcharacteralign", "setupcaptions", "setupcaption", "setupcapitals", "setupbutton", "setupbuffer", "setupbtxrendering", "setupbtxregister", "setupbtxlist", "setupbtxlabeltext", "setupbtxdataset", "setupbtx", "setupbottomtexts", "setupbottom", "setupbookmark", "setupbodyfont", "setupblock", "setupbleeding", "setupblank", "setupblackrules", "setupbars", "setupbar", "setupbackgrounds", "setupbackground", "setupbackend", "setupattachments", "setupattachment", "setuparranging", "setupalternativestyles", "setupalign", "setupTEXpage", "setupTABLE", "setupMPvariables", "setupMPpage", "setupMPinstance", "setupMPgraphics", "setup", "setunreferencedobject", "setugvalue", "setuevalue", "settrialtypesetting", "settokenlist", "settightunreferencedobject", "settightstrut", "settightreferencedobject", "settightobject", "settextcontent", "settaggedmetadata", "settabular", "setsystemmode", "setsuperiors", "setstrut", "setstructurepageregister", "setstackbox", "setsmallcaps", "setsmallbodyfont", "setsimplecolumnhsize", "setsectionblock", "setsecondpasscharacteralign", "setscript", "setrigidcolumnhsize", "setrigidcolumnbalance", "setreplacement", "setregisterentry", "setreferencedobject", "setreference", "setrandomseed", "setprofile", "setpositionstrut", "setpositionplus", "setpositiononly", "setpositiondataplus", "setpositiondata", "setpositionbox", "setposition", "setperiodkerning", "setpercentdimen", "setpenalties", "setpagestaterealpageno", "setpagestate", "setpagereference", "setoldstyle", "setobject", "setnotetext", "setnote", "setnostrut", "setmode", "setminus", "setmessagetext", "setmeasure", "setmathstyle", "setmarking", "setmarker", "setmainparbuilder", "setmainbodyfont", "setlocalscript", "setlocalhsize", "setlinefiller", "setlayertext", "setlayerframed", "setlayer", "setitaliccorrection", "setinternalrendering", "setinterfacevariable", "setinterfacemessage", "setinterfaceelement", "setinterfaceconstant", "setinterfacecommand", "setinteraction", "setinjector", "setinitial", "sethyphenationfeatures", "sethyphenatedurlnormal", "sethyphenatedurlbefore", "sethyphenatedurlafter", "sethboxregister", "setgvariables", "setgvariable", "setgvalue", "setgmeasure", "setglobalscript", "setfontstrut", "setfontsolution", "setfontfeature", "setfontcolorsheme", "setfont", "setflag", "setfirstpasscharacteralign", "setfirstline", "setevariables", "setevariable", "setevalue", "setemeasure", "setelementexporttag", "setdummyparameter", "setdocumentfilename", "setdocumentargumentdefault", "setdocumentargument", "setdirection", "setdigitsmanipulation", "setdefaultpenalties", "setdataset", "setcurrentfontclass", "setcounterown", "setcounter", "setcolormodell", "setcollector", "setcharstrut", "setcharacterstripping", "setcharacterspacing", "setcharacterkerning", "setcharactercleaning", "setcharactercasing", "setcharacteraligndetail", "setcharacteralign", "setcatcodetable", "setcapstrut", "setbreakpoints", "setboxlly", "setboxllx", "setbigbodyfont", "setbar", "setautopagestaterealpageno", "setMPvariables", "setMPvariable", "setMPtext", "setMPpositiongraphicrange", "setMPpositiongraphic", "setMPlayer", "setJSpreamble", "serifnormal", "serifbold", "serif", "serializedcommalist", "serializecommalist", "selectblocks", "select", "seeindex", "sectionmark", "section", "secondoftwounexpanded", "secondoftwoarguments", "secondofthreeunexpanded", "secondofthreearguments", "secondofsixarguments", "secondoffourarguments", "secondoffivearguments", "searrow", "screen", "scommaaccent", "scircumflex", "schwahook", "schwa", "scedilla", "scaron", "scale", "sbox", "savetwopassdata", "savetaggedtwopassdata", "savenormalmeaning", "savecurrentattributes", "savecounter", "savebuffer", "savebtxdataset", "savebox", "sansserif", "sansnormal", "sansbold", "sans", "samplefile", "safechar", "sacute", "rvert", "runninghbox", "runMPbuffer", "ruledvtop", "ruledvpack", "ruledvbox", "ruledtpack", "ruledtopv", "ruledmbox", "ruledhpack", "ruledhbox", "ruby", "rtop", "rtimes", "rrointerval", "rrbracket", "rrangle", "rparent", "rotate", "rootradical", "romanxii", "romanxi", "romanx", "romanviii", "romanvii", "romanvi", "romanv", "romannumerals", "romanm", "romanl", "romanix", "romaniv", "romaniii", "romanii", "romani", "romand", "romanc", "romanXII", "romanXI", "romanX", "romanVIII", "romanVII", "romanVI", "romanV", "romanM", "romanL", "romanIX", "romanIV", "romanIII", "romanII", "romanI", "romanD", "romanC", "roman", "rollbutton", "rointerval", "robustpretocommalist", "robustdoifinsetelse", "robustdoifelseinset", "robustaddtocommalist", "rneq", "rmoustache", "rlointerval", "rlap", "risingdotseq", "rinvertedbreve", "rinterval", "ring", "rightwhitearrow", "rightwavearrow", "righttopbox", "righttoleftvtop", "righttoleftvbox", "righttolefthbox", "righttoleft", "rightthreetimes", "rightthreearrows", "rightsubguillemot", "rightsquigarrow", "rightskipadaption", "rightrightarrows", "rightpageorder", "rightorleftpageaction", "rightmathlabeltext", "rightline", "rightleftharpoons", "rightleftarrows", "rightlabeltext", "rightheadtext", "righthbox", "rightharpoonup", "rightharpoondown", "rightguillemot", "rightdasharrow", "rightbox", "rightbottombox", "rightarrowtriangle", "rightarrowtail", "rightarrowbar", "rightarrow", "rightangle", "rightaligned", "right", "rhookswarrow", "rhooknearrow", "rho", "rhbox", "rgroup", "rfloor", "rfence", "revivefeature", "reversedtripleprime", "reversedprime", "reverseddoubleprime", "reuserandomseed", "reuseMPgraphic", "reusableMPgraphic", "restriction", "restoreglobalbodyfont", "restoreendofline", "restorecurrentattributes", "restorecounter", "restorecatcodes", "restorebox", "restartcounter", "resolvedglyphstyled", "resolvedglyphdirect", "reshapebox", "resetvisualizers", "resetvalue", "resetuserpagenumber", "resetusedsynonyms", "resetusedsortings", "resettrialtypesetting", "resettrackers", "resettokenlist", "resettimer", "resetsystemmode", "resetsymbolset", "resetsubpagenumber", "resetshownsynonyms", "resetsetups", "resetscript", "resetreplacement", "resetreference", "resetrecurselevel", "resetprofile", "resetperiodkerning", "resetpenalties", "resetpath", "resetparallel", "resetpagenumber", "resetmode", "resetmarking", "resetmarker", "resetlocalfloats", "resetlayer", "resetitaliccorrection", "resetinteractionmenu", "resetinjector", "resethyphenationfeatures", "resetfontsolution", "resetfontfallback", "resetfontcolorsheme", "resetflag", "resetfeature", "resetdirection", "resetdigitsmanipulation", "resetcounter", "resetcollector", "resetcharacterstripping", "resetcharacterspacing", "resetcharacterkerning", "resetcharacteralign", "resetbuffer", "resetbreakpoints", "resetboxesincache", "resetbar", "resetandaddfeature", "resetallattributes", "resetMPinstance", "resetMPenvironment", "resetMPdrawing", "rescanwithsetup", "rescan", "replaceword", "replaceincommalist", "replacefeature", "removeunwantedspaces", "removetoks", "removesubstring", "removepunctuation", "removemarkedcontent", "removelastspace", "removelastskip", "removefromcommalist", "removedepth", "removebottomthings", "remark", "remainingcharacters", "relbar", "relaxvalueifundefined", "relateparameterhandlers", "relatemarking", "regular", "registerunit", "registersynonym", "registersort", "registermenubuttons", "registerhyphenationpattern", "registerhyphenationexception", "registerfontclass", "registerexternalfigure", "registered", "registerctxluafile", "registerattachment", "referring", "referenceprefix", "reference", "ref", "redoconvertfont", "recursestring", "recurselevel", "recursedepth", "realsmallcapped", "reals", "realpagenumber", "realSmallcapped", "realSmallCapped", "readxmlfile", "readtexfile", "readsysfile", "readsetfile", "readlocfile", "readjobfile", "readfixfile", "readfile", "rdoublegrave", "rcommaaccent", "rceil", "rcaron", "rbracket", "rbrace", "rbox", "rawsubcountervalue", "rawstructurelistuservariable", "rawprocesscommalist", "rawprocesscommacommand", "rawprocessaction", "rawgetparameters", "rawdoifinsetelse", "rawdoifinset", "rawdoifelseinset", "rawdate", "rawcountervalue", "rawcounter", "rationals", "rangle", "randomnumber", "randomizetext", "raisebox", "raggedwidecenter", "raggedright", "raggedleft", "raggedcenter", "raggedbottom", "racute", "rVert", "quotesinglebase", "quotesingle", "quoteright", "quoteleft", "quotedblright", "quotedblleft", "quotedblbase", "quotedbl", "quote", "quotation", "quittypescriptscanning", "quitprevcommalist", "quitcommalist", "questionedeq", "questiondown", "quarterstrut", "quads", "quadrupleprime", "quad", "qquad", "putnextboxincache", "putboxincache", "pushsystemmode", "pushoutputstream", "pushmode", "pushmacro", "pushbutton", "pushattribute", "purenumber", "punctuationspace", "psi", "pseudosmallcapped", "pseudoSmallcapped", "pseudoSmallCapped", "pseudoMixedCapped", "propto", "project", "program", "profilegivenbox", "profiledbox", "product", "prod", "processyear", "processxtablebuffer", "processuntil", "processtokens", "processtexbuffer", "processseparatedlist", "processranges", "processmonth", "processlist", "processlinetablefile", "processlinetablebuffer", "processisolatedwords", "processisolatedchars", "processfirstactioninset", "processfileonce", "processfilenone", "processfilemany", "processfile", "processcontent", "processcommalistwithparameters", "processcommalist", "processcommacommand", "processcolorcomponents", "processbodyfontenvironmentlist", "processblocks", "processbetween", "processassignmentlist", "processassignmentcommand", "processassignlist", "processallactionsinset", "processaction", "processMPfigurefile", "processMPbuffer", "procent", "primes", "prime", "prevuserpagenumber", "prevuserpage", "prevsubpagenumber", "prevsubpage", "prevsubcountervalue", "prevrealpagenumber", "prevrealpage", "preventmode", "prevcountervalue", "prevcounter", "prettyprintbuffer", "pretocommalist", "presetunittext", "presettaglabeltext", "presetsuffixtext", "presetprefixtext", "presetoperatortext", "presetmathlabeltext", "presetlabeltext", "presetheadtext", "presetfieldsymbols", "presetdocument", "presetbtxlabeltext", "prerollblank", "prependvalue", "prependtoksonce", "prependtoks", "prependtocommalist", "prependgvalue", "prependetoks", "prefixtext", "prefixlanguage", "prefixedpagenumber", "predefinesymbol", "predefinefont", "predefinedfont", "precsim", "precnsim", "precneqq", "precneq", "precnapprox", "preceqq", "preceq", "preccurlyeq", "precapprox", "prec", "postponenotes", "positivesign", "positionregionoverlay", "positionoverlay", "position", "popsystemmode", "popmode", "popmacro", "popattribute", "pm", "placetable", "placesubformula", "placesidebyside", "placerenderingwindow", "placeregister", "placerawlist", "placeparallel", "placepairedbox", "placepagenumber", "placeontopofeachother", "placeongrid", "placenotes", "placenamedformula", "placenamedfloat", "placement", "placelocalnotes", "placelocalfootnotes", "placelistoftables", "placelistofsynonyms", "placelistofsorts", "placelistofpublications", "placelistoflogos", "placelistofintermezzi", "placelistofgraphics", "placelistoffigures", "placelistofchemicals", "placelistofabbreviations", "placelist", "placelegend", "placelayeredtext", "placelayer", "placeintermezzo", "placeinitial", "placeindex", "placehelp", "placeheadtext", "placeheadnumber", "placegraphic", "placeframed", "placeformula", "placefootnotes", "placefloatwithsetups", "placefloat", "placefigure", "placedbox", "placecurrentformulanumber", "placecontent", "placecomments", "placecombinedlist", "placecitation", "placechemical", "placebtxrendering", "placebookmarks", "placeattachments", "pitchfork", "pickupgroupedcommand", "pi", "phook", "phi", "phantombox", "phantom", "perthousand", "persianthousandsseparator", "persiannumerals", "persiandecimalseparator", "persiandecimals", "perp", "permitspacesbetweengroups", "permitcircumflexescape", "permitcaretescape", "periods", "periodcentered", "percentdimen", "percent", "pdfeTeX", "pdfcolor", "pdfbackendsetshade", "pdfbackendsetpattern", "pdfbackendsetpagesattribute", "pdfbackendsetpageresource", "pdfbackendsetpageattribute", "pdfbackendsetname", "pdfbackendsetinfo", "pdfbackendsetextgstate", "pdfbackendsetcolorspace", "pdfbackendsetcatalog", "pdfbackendcurrentresources", "pdfbackendactualtext", "pdfactualtext", "pdfTeX", "partial", "part", "parallel", "paragraphmark", "paletsize", "pagestaterealpageorder", "pagestaterealpage", "pagereference", "pagenumber", "pageinjection", "pagefigure", "pagebreak", "pagearea", "page", "owns", "overstrikes", "overstrike", "overset", "overrightarrow", "overparentunderparent", "overparent", "overloaderror", "overleftarrow", "overlaywidth", "overlayrollbutton", "overlayoffset", "overlaylinewidth", "overlaylinecolor", "overlayimage", "overlayheight", "overlayfigure", "overlaydepth", "overlaycolor", "overlaybutton", "overbracketunderbracket", "overbracket", "overbraceunderbrace", "overbrace", "overbarunderbar", "overbars", "overbar", "over", "outputstreamunvcopy", "outputstreamunvbox", "outputstreamcopy", "outputstreambox", "outputfilename", "otimes", "otildemacron", "otilde", "ostrokeacute", "ostroke", "oslash", "ornamenttext", "ordmasculine", "ordinalstr", "ordinaldaynumber", "ordfeminine", "oplus", "operatortext", "operatorlanguage", "oogonekmacron", "oogonek", "onethird", "onesuperior", "onesixth", "onequarter", "onehalf", "onefifth", "oneeighth", "onedigitrounding", "ominus", "omicron", "omega", "omacron", "oinvertedbreve", "ointctrclockwise", "ointclockwise", "oint", "oiint", "oiiint", "ohungarumlaut", "ohorntilde", "ohornhook", "ohorngrave", "ohorndotbelow", "ohornacute", "ohorn", "ohook", "ohm", "ograve", "offsetbox", "offset", "oeligature", "odoublegrave", "odotbelow", "odotaccentmacron", "odotaccent", "odot", "odiaeresismacron", "odiaeresis", "ocircumflextilde", "ocircumflexhook", "ocircumflexgrave", "ocircumflexdotbelow", "ocircumflexacute", "ocircumflex", "ocaron", "obreve", "obox", "objectwidth", "objectmargin", "objectheight", "objectdepth", "obeydepth", "oacute", "nwsearrow", "nwarrow", "nvrightarrow", "nvleftrightarrow", "nvleftarrow", "nvdash", "nvDash", "numbers", "numberofpoints", "nu", "ntrianglerighteq", "ntriangleright", "ntrianglelefteq", "ntriangleleft", "ntimes", "ntilde", "nsupseteq", "nsupset", "nsucccurlyeq", "nsucc", "nsubseteq", "nsubset", "nsqsupseteq", "nsqsubseteq", "nsimeq", "nsim", "nrightarrow", "npreccurlyeq", "nprec", "nparallel", "nowns", "notragged", "notopandbottomlines", "notin", "notesymbol", "note", "not", "nospace", "normaltypeface", "normalslantedface", "normalizetextwidth", "normalizetextline", "normalizetextheight", "normalizetextdepth", "normalizefontwidth", "normalizefontline", "normalizefontheight", "normalizefontdepth", "normalizedfontsize", "normalizebodyfontsize", "normalitalicface", "normalframedwithsettings", "normalboldface", "normal", "nonmathematics", "nonfrenchspacing", "noitem", "noindentation", "noheightstrut", "noheaderandfooterlines", "noflocalfloats", "noflines", "noffigurepages", "nodetostring", "nocite", "nocitation", "nocharacteralign", "nocap", "nobreakspace", "nobar", "nni", "nmid", "nlesssim", "nlessgtr", "nless", "nleq", "nleftrightarrow", "nleftarrow", "njligature", "nin", "nihongo", "ni", "ngtrsim", "ngtrless", "ngtr", "ngrave", "ngeq", "nextuserpagenumber", "nextuserpage", "nextsubpagenumber", "nextsubpage", "nextsubcountervalue", "nextrealpagenumber", "nextrealpage", "nextparagraphs", "nextdepth", "nextcountervalue", "nextcounter", "nextboxwd", "nextboxhtdp", "nextboxht", "nextboxdp", "nextbox", "nexists", "newsystemmode", "newsignal", "newmode", "newfrenchspacing", "newevery", "newcounter", "newcatcodetable", "newattribute", "neswarrow", "nequiv", "neq", "neng", "negthinspace", "negenspace", "negemspace", "negativesign", "negated", "negatecolorbox", "neg", "nearrow", "ne", "ndivides", "ncurl", "ncong", "ncommaaccent", "ncaron", "naturalwd", "naturalvtop", "naturalvpack", "naturalvcenter", "naturalvbox", "naturalnumbers", "naturalhpack", "naturalhbox", "natural", "nasymp", "narrownobreakspace", "napproxEq", "napprox", "napostrophe", "namedtaggedlabeltexts", "namedstructurevariable", "namedstructureuservariable", "namedstructureheadlocation", "namedheadnumber", "nacute", "nabla", "nVrightarrow", "nVleftrightarrow", "nVleftarrow", "nVdash", "nVDash", "nRightarrow", "nLeftrightarrow", "nLeftarrow", "nHuparrow", "nHdownarrow", "multimap", "mu", "mtwoheadrightarrow", "mtwoheadleftarrow", "mtriplerel", "mtext", "mrightoverleftarrow", "mrightleftharpoons", "mrightharpoonup", "mrightharpoondown", "mrightarrow", "mrel", "mprandomnumber", "mp", "monthshort", "monthlong", "month", "mononormal", "monobold", "mono", "molecule", "moduleparameter", "models", "mmapsto", "mleftrightharpoons", "mleftrightarrow", "mleftharpoonup", "mleftharpoondown", "mleftarrow", "mkvibuffer", "mixedcaps", "mirror", "minuscolon", "minus", "minimalhbox", "midsubsentence", "midhbox", "middlebox", "middlealigned", "middle", "midaligned", "mid", "mhookrightarrow", "mhookleftarrow", "mho", "mhbox", "mfunctionlabeltext", "mfunction", "mframed", "mfence", "message", "mequal", "menubutton", "medspace", "medskip", "measuredeq", "measuredangle", "measured", "measure", "mcframed", "mbox", "maxaligned", "mathwordtf", "mathwordsl", "mathwordit", "mathwordbs", "mathwordbi", "mathwordbf", "mathword", "mathupright", "mathunder", "mathtt", "mathtriplet", "mathtf", "mathtexttf", "mathtextsl", "mathtextit", "mathtextbs", "mathtextbi", "mathtextbf", "mathtext", "mathss", "mathsl", "mathscript", "mathrm", "mathpercent", "mathover", "mathop", "mathlabeltexts", "mathlabeltext", "mathlabellanguage", "mathitalic", "mathit", "mathhyphen", "mathhash", "mathfunction", "mathfraktur", "mathematics", "mathdouble", "mathdollar", "mathdefault", "mathbs", "mathblackboard", "mathbi", "mathbf", "mathampersand", "math", "mat", "markpage", "markinjector", "marking", "markedpages", "markcontent", "margintext", "margindata", "mapsup", "mapsto", "mapsfrom", "mapsdown", "mapfontsize", "maltese", "makestrutofbox", "makerawcommalist", "makecharacteractive", "mainlanguage", "mRightarrow", "mLeftrightarrow", "mLeftarrow", "m", "lvert", "luaversion", "luasetup", "luaparameterset", "luaminorversion", "luametaTeX", "luamajorversion", "luajitTeX", "luafunction", "luaexpr", "luaexpanded", "luaenvironment", "luaconditional", "luacode", "luaTeX", "ltop", "ltimes", "lt", "lstroke", "lrtbbox", "lrointerval", "lrcorner", "lparent", "lozenge", "lowerrightsingleninequote", "lowerrightdoubleninequote", "lowerleftsingleninequote", "lowerleftdoubleninequote", "lowercasestring", "lowercased", "lowerbox", "low", "lor", "looparrowright", "looparrowleft", "longrightsquigarrow", "longrightarrow", "longmapsto", "longmapsfrom", "longleftrightarrow", "longleftarrow", "lomihi", "lointerval", "lohi", "logo", "locfilename", "locatefilepath", "locatedfilepath", "localundefine", "localpushmacro", "localpushbox", "localpopmacro", "localpopbox", "localhsize", "localframedwithsettings", "localframed", "loadtypescriptfile", "loadtexfileonce", "loadtexfile", "loadspellchecklist", "loadluafileonce", "loadluafile", "loadfontgoodies", "loadcldfileonce", "loadcldfile", "loadbtxreplacementfile", "loadbtxdefinitionfile", "loadanyfileonce", "loadanyfile", "lnsim", "lnot", "lneqq", "lneq", "lnapprox", "lmoustache", "llless", "lll", "llcorner", "llbracket", "llap", "llangle", "ll", "ljligature", "listnamespaces", "listlength", "listcite", "listcitation", "linterval", "linethickness", "linespanningtext", "linenote", "linefeed", "linebox", "line", "limitatetext", "limitatelines", "limitatefirstline", "lhooksearrow", "lhooknwarrow", "lhbox", "lgroup", "lfloor", "lfence", "letvaluerelax", "letvalueempty", "letvalue", "letterunderscore", "lettertilde", "letterspacing", "letterslash", "lettersinglequote", "letterrightparenthesis", "letterrightbracket", "letterrightbrace", "letterquestionmark", "letterpercent", "letteropenbrace", "lettermore", "letterless", "letterleftparenthesis", "letterleftbracket", "letterleftbrace", "letterhat", "letterhash", "letterexclamationmark", "letterescape", "letteregroup", "letterdoublequote", "letterdollar", "lettercolon", "letterclosebrace", "letterbgroup", "letterbar", "letterbackslash", "letterat", "letterampersand", "letgvalurelax", "letgvalueempty", "letgvalue", "letempty", "letdummyparameter", "letcsnamecsname", "letcsnamecs", "letcscsname", "letcatcodecommand", "letbeundefined", "lesssim", "lessgtr", "lesseqqgtr", "lesseqgtr", "lessdot", "lessapprox", "leqslant", "leqq", "leq", "leftwhitearrow", "leftwavearrow", "lefttorightvtop", "lefttorightvbox", "lefttorighthbox", "lefttoright", "lefttopbox", "leftthreetimes", "leftsubguillemot", "leftsquigarrow", "leftskipadaption", "leftrightsquigarrow", "leftrightharpoons", "leftrightarrowtriangle", "leftrightarrows", "leftrightarrow", "leftorrightvtop", "leftorrightvbox", "leftorrighthbox", "leftmathlabeltext", "leftline", "leftleftarrows", "leftlabeltext", "leftheadtext", "lefthbox", "leftharpoonup", "leftharpoondown", "leftguillemot", "leftdasharrow", "leftbox", "leftbottombox", "leftarrowtriangle", "leftarrowtail", "leftarrow", "leftaligned", "left", "leadsto", "le", "ldots", "ldotp", "ldotmiddle", "lcurl", "lcommaaccent", "lceil", "lcaron", "lbracket", "lbrace", "lbox", "lbar", "lazysavetwopassdata", "lazysavetaggedtwopassdata", "layerwidth", "layerheight", "layeredtext", "latin", "lateluacode", "lastuserpagenumber", "lastuserpage", "lasttwodigits", "lastsubpagenumber", "lastsubpage", "lastsubcountervalue", "lastrealpagenumber", "lastrealpage", "lastpredefinedsymbol", "lastnaturalboxwd", "lastnaturalboxht", "lastnaturalboxdp", "lastlinewidth", "lastdigit", "lastcountervalue", "lastcounter", "languagecharwidth", "languagecharacters", "languageCharacters", "language", "langle", "land", "lambdabar", "lambda", "lacute", "labeltexts", "labeltext", "labellanguage", "lVert", "koreanparentnumerals", "koreannumeralsp", "koreannumeralsc", "koreannumerals", "koreancirclenumerals", "kkra", "khook", "kerncharacters", "keepunwantedspaces", "keeplinestogether", "keepblocks", "kcommaaccent", "kcaron", "kappa", "kap", "jobfilesuffix", "jobfilename", "jmath", "jcircumflex", "jcaron", "itilde", "itemtag", "items", "item", "italicface", "italiccorrection", "italicbold", "italic", "istrtdir", "istltdir", "iota", "iogonek", "invokepageheandler", "invisibletimes", "intop", "intertext", "intercal", "interactionmenu", "interactionbuttons", "interactionbar", "integers", "integerrounding", "intclockwise", "int", "installversioninfo", "installunitsspace", "installunitsseparator", "installtopframerenderer", "installtextracker", "installtexdirective", "installswitchsetuphandler", "installswitchcommandhandler", "installstyleandcolorhandler", "installsimpleframedcommandhandler", "installsimplecommandhandler", "installshipoutmethod", "installsetuponlycommandhandler", "installsetuphandler", "installrootparameterhandler", "installrightframerenderer", "installparentinjector", "installparametersethandler", "installparameterhashhandler", "installparameterhandler", "installpagearrangement", "installoutputroutine", "installnamespace", "installmacrostack", "installleftframerenderer", "installlanguage", "installglobalmacrostack", "installframedcommandhandler", "installframedautocommandhandler", "installdirectstyleandcolorhandler", "installdirectsetuphandler", "installdirectparametersethandler", "installdirectparameterhandler", "installdirectcommandhandler", "installdefinitionsetmember", "installdefinitionset", "installdefinehandler", "installcorenamespace", "installcommandhandler", "installbottomframerenderer", "installbasicparameterhandler", "installbasicautosetuphandler", "installautosetuphandler", "installautocommandhandler", "installattributestack", "installanddefineactivecharacter", "installactivecharacter", "installactionhandler", "insertpages", "inrightmargin", "inrightedge", "inright", "inputgivenfile", "inputfilesuffix", "inputfilerealsuffix", "inputfilename", "inputfilebarename", "input", "inoutermargin", "inouteredge", "inouter", "inother", "innerflushshapebox", "inmframed", "inmargin", "inlinerange", "inlineprettyprintbuffer", "inlineordisplaymath", "inlinemessage", "inlinemathematics", "inlinemath", "inlinedbox", "inlinebuffer", "inline", "inleftmargin", "inleftedge", "inleft", "initializeboxstack", "ininnermargin", "ininneredge", "ininner", "inhibitblank", "inheritparameter", "infull", "infty", "inframed", "infofontbold", "infofont", "index", "indentation", "incrementvalue", "incrementsubpagenumber", "incrementpagenumber", "incrementedcounter", "incrementcounter", "increment", "includeversioninfo", "includemenu", "in", "imply", "implies", "impliedby", "immediatesavetwopassdata", "imath", "imaginaryj", "imaginaryi", "imacron", "ijligature", "iinvertedbreve", "iintop", "iint", "iiintop", "iiint", "iiiintop", "iiiint", "ihook", "igrave", "ignorevalue", "ignoretagsinexport", "ignoreimplicitspaces", "iftrialtypesetting", "ifparameters", "ifinoutputstream", "ifinobject", "iff", "ifassignment", "idoublegrave", "idotbelow", "idotaccent", "idiaeresis", "ideographicspace", "ideographichalffillspace", "icircumflex", "icaron", "ibreve", "ibox", "iacute", "hyphenatedword", "hyphenatedurl", "hyphenatedpar", "hyphenatedhbox", "hyphenatedfilename", "hyphenatedfile", "hyphenatedcoloredword", "hyphen", "htofstring", "htdpofstring", "hstroke", "hspace", "hsmashed", "hsmashbox", "hsmash", "hslash", "hsizefraction", "hpos", "hphantom", "horizontalpositionbar", "horizontalgrowingbar", "hookrightarrow", "hookleftarrow", "hl", "himilo", "hilo", "highordinalstr", "highlight", "high", "hideblocks", "hiddencite", "hiddencitation", "hiddenbar", "hglue", "helptext", "heightspanningtext", "heightofstring", "heightanddepthofstring", "hebrewZayin", "hebrewYod", "hebrewVav", "hebrewTsadifinal", "hebrewTsadi", "hebrewTet", "hebrewTav", "hebrewShin", "hebrewSamekh", "hebrewResh", "hebrewQof", "hebrewPefinal", "hebrewPe", "hebrewNunfinal", "hebrewNun", "hebrewMemfinal", "hebrewMem", "hebrewLamed", "hebrewKaffinal", "hebrewKaf", "hebrewHet", "hebrewHe", "hebrewGimel", "hebrewDalet", "hebrewBet", "hebrewAyin", "hebrewAlef", "heartsuit", "headwidth", "headvbox", "headtextwidth", "headtexts", "headtextdistance", "headtextcontent", "headtext", "headsetupspacing", "headreferenceattributes", "headnumberwidth", "headnumberdistance", "headnumbercontent", "headnumber", "headlanguage", "headhbox", "hdofstring", "hcircumflex", "hcaron", "hboxreference", "hboxofvbox", "hbar", "hat", "hash", "hanzi", "hangul", "handwritten", "handletokens", "halfwaybox", "halfstrut", "halflinestrut", "hairspace", "hairline", "gurmurkhinumerals", "gujaratinumerals", "guilsingleright", "guilsingleleft", "gtrsim", "gtrless", "gtreqqless", "gtreqless", "gtrdot", "gtrapprox", "gt", "gstroke", "gsetboxlly", "gsetboxllx", "groupedcommand", "grid", "greekzeta", "greekxi", "greekvaria", "greekupsilonvrachy", "greekupsilonvaria", "greekupsilontonos", "greekupsilonpsilivaria", "greekupsilonpsilitonos", "greekupsilonpsiliperispomeni", "greekupsilonpsili", "greekupsilonperispomeni", "greekupsilonoxia", "greekupsilonmacron", "greekupsilondialytikavaria", "greekupsilondialytikatonos", "greekupsilondialytikaperispomeni", "greekupsilondiaeresis", "greekupsilondasiavaria", "greekupsilondasiatonos", "greekupsilondasiaperispomeni", "greekupsilondasia", "greekupsilon", "greektonos", "greekthetaalt", "greektheta", "greektau", "greekstigma", "greeksigmalunate", "greeksigma", "greeksampi", "greekrhopsili", "greekrhodasia", "greekrhoalt", "greekrho", "greekpsilivaria", "greekpsilitonos", "greekpsiliperispomeni", "greekpsili", "greekpsi", "greekprosgegrammeni", "greekpialt", "greekpi", "greekphialt", "greekphi", "greekperispomeni", "greekoxia", "greekomicronvaria", "greekomicrontonos", "greekomicronpsilivaria", "greekomicronpsilitonos", "greekomicronpsili", "greekomicronoxia", "greekomicrondasiavaria", "greekomicrondasiatonos", "greekomicrondasia", "greekomicron", "greekomegavaria", "greekomegatonos", "greekomegapsilivaria", "greekomegapsilitonos", "greekomegapsiliperispomeni", "greekomegapsili", "greekomegaperispomeni", "greekomegaoxia", "greekomegaiotasubvaria", "greekomegaiotasubtonos", "greekomegaiotasubpsilivaria", "greekomegaiotasubpsilitonos", "greekomegaiotasubpsiliperispomeni", "greekomegaiotasubpsili", "greekomegaiotasubperispomeni", "greekomegaiotasubdasiavaria", "greekomegaiotasubdasiatonos", "greekomegaiotasubdasiaperispomeni", "greekomegaiotasubdasia", "greekomegaiotasub", "greekomegadasiavaria", "greekomegadasiatonos", "greekomegadasiaperispomeni", "greekomegadasia", "greekomega", "greeknumkoppa", "greeknumerals", "greeknu", "greekmu", "greeklambda", "greekkoppa", "greekkappa", "greekiotavrachy", "greekiotavaria", "greekiotatonos", "greekiotapsilivaria", "greekiotapsilitonos", "greekiotapsiliperispomeni", "greekiotapsili", "greekiotaperispomeni", "greekiotaoxia", "greekiotamacron", "greekiotadialytikavaria", "greekiotadialytikatonos", "greekiotadialytikaperispomeni", "greekiotadialytika", "greekiotadasiavaria", "greekiotadasiatonos", "greekiotadasiaperispomeni", "greekiotadasia", "greekiota", "greekgamma", "greekfinalsigma", "greeketavaria", "greeketatonos", "greeketapsilivaria", "greeketapsilitonos", "greeketapsiliperispomeni", "greeketapsili", "greeketaperispomeni", "greeketaoxia", "greeketaiotasubvaria", "greeketaiotasubtonos", "greeketaiotasubpsilivaria", "greeketaiotasubpsilitonos", "greeketaiotasubpsiliperispomeni", "greeketaiotasubpsili", "greeketaiotasubperispomeni", "greeketaiotasubdasiavaria", "greeketaiotasubdasiatonos", "greeketaiotasubdasiaperispomeni", "greeketaiotasubdasia", "greeketaiotasub", "greeketadasiavaria", "greeketadasiatonos", "greeketadasiaperispomeni", "greeketadasia", "greeketa", "greekepsilonvaria", "greekepsilontonos", "greekepsilonpsilivaria", "greekepsilonpsilitonos", "greekepsilonpsili", "greekepsilonoxia", "greekepsilondasiavaria", "greekepsilondasiatonos", "greekepsilondasia", "greekepsilonalt", "greekepsilon", "greekdigamma", "greekdialytikavaria", "greekdialytikatonos", "greekdialytikaperispomeni", "greekdelta", "greekdasiavaria", "greekdasiatonos", "greekdasiaperispomeni", "greekdasia", "greekchi", "greekbetaalt", "greekbeta", "greekalphavrachy", "greekalphavaria", "greekalphatonos", "greekalphapsilivaria", "greekalphapsilitonos", "greekalphapsiliperispomeni", "greekalphapsili", "greekalphaperispomeni", "greekalphaoxia", "greekalphamacron", "greekalphaiotasubvaria", "greekalphaiotasubtonos", "greekalphaiotasubpsilivaria", "greekalphaiotasubpsilitonos", "greekalphaiotasubpsiliperispomeni", "greekalphaiotasubpsili", "greekalphaiotasubperispomeni", "greekalphaiotasubdasiavaria", "greekalphaiotasubdasiatonos", "greekalphaiotasubdasiaperispomeni", "greekalphaiotasubdasia", "greekalphaiotasub", "greekalphadasiavaria", "greekalphadasiatonos", "greekalphadasiaperispomeni", "greekalphadasia", "greekalpha", "greekZeta", "greekXi", "greekUpsilonvrachy", "greekUpsilonvaria", "greekUpsilontonos", "greekUpsilonmacron", "greekUpsilondialytika", "greekUpsilondasiavaria", "greekUpsilondasiatonos", "greekUpsilondasiaperispomeni", "greekUpsilondasia", "greekUpsilon", "greekTheta", "greekTau", "greekSigmalunate", "greekSigma", "greekRhodasia", "greekRho", "greekPsi", "greekPi", "greekPhi", "greekOmicronvaria", "greekOmicrontonos", "greekOmicronpsilivaria", "greekOmicronpsilitonos", "greekOmicronpsili", "greekOmicrondasiavaria", "greekOmicrondasiatonos", "greekOmicrondasia", "greekOmicron", "greekOmegavaria", "greekOmegatonos", "greekOmegapsilivaria", "greekOmegapsilitonos", "greekOmegapsiliperispomeni", "greekOmegapsili", "greekOmegaiotasubpsilivaria", "greekOmegaiotasubpsilitonos", "greekOmegaiotasubpsiliperispomeni", "greekOmegaiotasubpsili", "greekOmegaiotasubdasiavaria", "greekOmegaiotasubdasiatonos", "greekOmegaiotasubdasiaperispomeni", "greekOmegaiotasubdasia", "greekOmegaiotasub", "greekOmegadasiavaria", "greekOmegadasiatonos", "greekOmegadasiaperispomeni", "greekOmegadasia", "greekOmega", "greekNu", "greekMu", "greekLambda", "greekKappa", "greekIotavrachy", "greekIotavaria", "greekIotatonos", "greekIotapsilivaria", "greekIotapsilitonos", "greekIotapsiliperispomeni", "greekIotapsili", "greekIotamacron", "greekIotadialytika", "greekIotadasiavaria", "greekIotadasiatonos", "greekIotadasiaperispomeni", "greekIotadasia", "greekIota", "greekGamma", "greekEtavaria", "greekEtatonos", "greekEtapsilivaria", "greekEtapsilitonos", "greekEtapsiliperispomeni", "greekEtapsili", "greekEtaiotasubpsilivaria", "greekEtaiotasubpsilitonos", "greekEtaiotasubpsiliperispomeni", "greekEtaiotasubpsili", "greekEtaiotasubdasiavaria", "greekEtaiotasubdasiatonos", "greekEtaiotasubdasiaperispomeni", "greekEtaiotasubdasia", "greekEtaiotasub", "greekEtadasiavaria", "greekEtadasiatonos", "greekEtadasiaperispomeni", "greekEtadasia", "greekEta", "greekEpsilonvaria", "greekEpsilontonos", "greekEpsilonpsilivaria", "greekEpsilonpsilitonos", "greekEpsilonpsili", "greekEpsilondasiavaria", "greekEpsilondasiatonos", "greekEpsilondasia", "greekEpsilon", "greekDelta", "greekCoronis", "greekChi", "greekBeta", "greekAlphavrachy", "greekAlphavaria", "greekAlphatonos", "greekAlphapsilivaria", "greekAlphapsilitonos", "greekAlphapsiliperispomeni", "greekAlphapsili", "greekAlphamacron", "greekAlphaiotasubpsilivaria", "greekAlphaiotasubpsilitonos", "greekAlphaiotasubpsiliperispomeni", "greekAlphaiotasubpsili", "greekAlphaiotasubdasiavaria", "greekAlphaiotasubdasiatonos", "greekAlphaiotasubdasiaperispomeni", "greekAlphaiotasubdasia", "greekAlphaiotasub", "greekAlphadasiavaria", "greekAlphadasiatonos", "greekAlphadasiaperispomeni", "greekAlphadasia", "greekAlpha", "greedysplitstring", "grayvalue", "graycolor", "grave", "grabuntil", "grabbufferdatadirect", "grabbufferdata", "gotopage", "gotobox", "goto", "godown", "gobbleuntilrelax", "gobbleuntil", "gobbletwooptionals", "gobbletwoarguments", "gobblethreeoptionals", "gobblethreearguments", "gobbletenarguments", "gobblespacetokens", "gobblesixarguments", "gobblesingleempty", "gobblesevenarguments", "gobbleoneoptional", "gobbleoneargument", "gobbleninearguments", "gobblefouroptionals", "gobblefourarguments", "gobblefiveoptionals", "gobblefivearguments", "gobbleeightarguments", "gobbledoubleempty", "gnsim", "gneqq", "gnapprox", "glyphfontfile", "globalundefine", "globalswapmacros", "globalswapdimens", "globalswapcounts", "globalpushmacro", "globalpushbox", "globalprocesscommalist", "globalpreventmode", "globalpopmacro", "globalpopbox", "globalletempty", "globalenablemode", "globaldisablemode", "gimel", "gggtr", "ggg", "gg", "getxparameters", "getvariabledefault", "getvariable", "getvalue", "getuvalue", "getuserdata", "gettwopassdatalist", "gettwopassdata", "gettokenlist", "getsubstring", "gets", "getroundednoflines", "getreferenceentry", "getreference", "getrawxparameters", "getrawparameters", "getrawnoflines", "getrawgparameters", "getraweparameters", "getrandomseed", "getrandomnumber", "getrandomfloat", "getrandomdimen", "getrandomcount", "getprivateslot", "getprivatechar", "getparameters", "getpaletsize", "getobjectdimensions", "getobject", "getnoflines", "getnaturaldimensions", "getnamedtwopassdatalist", "getnamedglyphstyled", "getnamedglyphdirect", "getmessage", "getmarking", "getlocalfloats", "getlocalfloat", "getlasttwopassdata", "getinlineuserdata", "getgparameters", "getglyphstyled", "getglyphdirect", "getfromtwopassdata", "getfromcommalist", "getfromcommacommand", "getfirsttwopassdata", "getfirstcharacter", "getfiguredimensions", "getexpandedparameters", "geteparameters", "getemptyparameters", "getdummyparameters", "getdocumentfilename", "getdocumentargumentdefault", "getdocumentargument", "getdefinedbuffer", "getdayspermonth", "getdayoftheweek", "getcommalistsize", "getcommacommandsize", "getbufferdata", "getbuffer", "getboxlly", "getboxllx", "getboxfromcache", "getMPlayer", "getMPdrawing", "geqslant", "geqq", "geq", "ge", "gdotaccent", "gdefconvertedcommand", "gdefconvertedargument", "gcommaaccent", "gcircumflex", "gcaron", "gbreve", "gamma", "gacute", "frule", "frozenhbox", "frown", "fromlinenote", "from", "frenchspacing", "freezemeasure", "freezedimenmacro", "framedtext", "framedparameter", "frameddimension", "framed", "frac", "fourthofsixarguments", "fourthoffourarguments", "fourthoffivearguments", "fourperemspace", "fourfifths", "foundbox", "formulanumber", "formula", "forgetragged", "forgetparskip", "forgetparameters", "forgeteverypar", "forcelocalfloats", "forcecharacterstripping", "forall", "footnotetext", "footnote", "fontstyle", "fontsize", "fontfeaturelist", "fontface", "fontclassname", "fontclass", "fontcharbyindex", "fontchar", "fontbody", "fontalternative", "flushtoks", "flushtokens", "flushtextflow", "flushshapebox", "flushoutputstream", "flushnotes", "flushnextbox", "flushlocalfloats", "flushlayer", "flushedrightlastline", "flushcollector", "flushboxregister", "flushbox", "floatuserdataparameter", "flligature", "flat", "flag", "fixedspaces", "fixedspace", "fivesixths", "fiveeighths", "fittopbaselinegrid", "fitfieldframed", "fitfield", "firstuserpagenumber", "firstuserpage", "firstsubpagenumber", "firstsubpage", "firstsubcountervalue", "firstrealpagenumber", "firstrealpage", "firstoftwounexpanded", "firstoftwoarguments", "firstofthreeunexpanded", "firstofthreearguments", "firstofsixarguments", "firstofoneunexpanded", "firstofoneargument", "firstoffourarguments", "firstoffivearguments", "firstinlist", "firstcountervalue", "firstcounter", "firstcharacter", "finishregisterentry", "findtwopassdata", "filterreference", "filterpages", "filterfromvalue", "filterfromnext", "fillupto", "fillintext", "fillinrules", "fillinline", "filler", "filledhboxy", "filledhboxr", "filledhboxm", "filledhboxk", "filledhboxg", "filledhboxc", "filledhboxb", "filigature", "filename", "figurewidth", "figuresymbol", "figurespace", "figurenaturalwidth", "figurenaturalheight", "figureheight", "figurefullname", "figurefiletype", "figurefilepath", "figurefilename", "fifthofsixarguments", "fifthoffivearguments", "fieldstack", "fieldbody", "field", "fhook", "fflligature", "ffligature", "ffiligature", "fetchtwomarks", "fetchtwomarkings", "fetchruntinecommand", "fetchonemarking", "fetchonemark", "fetchmarking", "fetchmark", "fetchallmarks", "fetchallmarkings", "fenced", "fence", "feature", "fastsxsy", "fastswitchtobodyfont", "fastsetupwithargumentswapped", "fastsetupwithargument", "fastsetup", "fastscale", "fastloopindex", "fastloopfinal", "fastlocalframed", "fastincrement", "fastdecrement", "fallingdotseq", "fakebox", "externalfigurecollectionparameter", "externalfigurecollectionminwidth", "externalfigurecollectionminheight", "externalfigurecollectionmaxwidth", "externalfigurecollectionmaxheight", "externalfigure", "exponentiale", "expdoifnot", "expdoifinsetelse", "expdoifelseinset", "expdoifelsecommon", "expdoifelse", "expdoifcommonelse", "expdoif", "expandfontsynonym", "expandeddoifnot", "expandeddoifelse", "expandeddoif", "expanded", "expandcheckedcsname", "exitloopnow", "exitloop", "exists", "executeifdefined", "exclamdown", "eunderparentfill", "eunderbracketfill", "eunderbracefill", "eunderbarfill", "etwoheadrightarrowfill", "etilde", "ethiopic", "eth", "eta", "erightharpoonupfill", "erightharpoondownfill", "erightarrowfill", "equiv", "equalscolon", "equaldigits", "eqslantless", "eqslantgtr", "eqsim", "eqless", "eqgtr", "eqeqeq", "eqeq", "eqcirc", "eq", "epsilon", "epos", "eoverparentfill", "eoverbracketfill", "eoverbracefill", "eoverbarfill", "eogonek", "envvar", "environment", "env", "enspace", "enskip", "enquad", "endnote", "endash", "enabletrackers", "enableregime", "enableparpositions", "enableoutputstream", "enablemode", "enableexperiments", "enabledirectives", "emspace", "emquad", "emptyset", "emptylines", "emphasistypeface", "emphasisboldface", "emdash", "emacron", "em", "ell", "eleftrightarrowfill", "eleftharpoonupfill", "eleftharpoondownfill", "eleftarrowfill", "elapsedtime", "elapsedseconds", "einvertedbreve", "ehook", "egrave", "effect", "efcparameter", "efcminwidth", "efcminheight", "efcmaxwidth", "efcmaxheight", "edoublegrave", "edotbelow", "edotaccent", "ediaeresis", "edefconvertedargument", "ecircumflextilde", "ecircumflexhook", "ecircumflexgrave", "ecircumflexdotbelow", "ecircumflexacute", "ecircumflex", "ecedilla", "ecaron", "ebreve", "eacute", "eTeX", "dzligature", "dzcaronligature", "dummyparameter", "dummydigit", "dtail", "dstroke", "dpofstring", "downzigzagarrow", "downwhitearrow", "downuparrows", "downharpoonright", "downharpoonleft", "downdownarrows", "downdasharrow", "downarrow", "dowithwargument", "dowithrange", "dowithpargument", "dowithnextboxcs", "dowithnextboxcontentcs", "dowithnextboxcontent", "dowithnextbox", "dowith", "doubleverticalbar", "doubleprime", "doubleparent", "doublecup", "doublecap", "doublebracket", "doublebrace", "doublebond", "doublebar", "dottedrightarrow", "dottedcircle", "dots", "dotriplegroupempty", "dotripleemptywithset", "dotripleempty", "dotripleargumentwithset", "dotripleargument", "dotplus", "dotoks", "dotminus", "dotlessjstroke", "dotlessj", "dotlessi", "dotlessJ", "dotlessI", "dotfskip", "doteqdot", "doteq", "dot", "dosubtractfeature", "dostepwiserecurse", "dosixtupleempty", "dosixtupleargument", "dosinglegroupempty", "dosingleempty", "dosingleargument", "doseventupleempty", "doseventupleargument", "dosetupcheckedinterlinespace", "dosetrightskipadaption", "dosetleftskipadaption", "dosetattribute", "dorotatebox", "doresetattribute", "doresetandafffeature", "doreplacefeature", "dorepeatwithcommand", "dorecurse", "dorechecknextindentation", "doquintuplegroupempty", "doquintupleempty", "doquintupleargument", "doquadruplegroupempty", "doquadrupleempty", "doquadrupleargument", "doprocesslocalsetups", "dopositionaction", "dontpermitspacesbetweengroups", "dontleavehmode", "dontconvertfont", "donothing", "doloopoverlist", "doloop", "dollar", "doindentation", "doifvariableelse", "doifvariable", "doifvaluesomething", "doifvaluenothingelse", "doifvaluenothing", "doifvalueelse", "doifvalue", "doifurldefinedelse", "doifunknownfontfeature", "doifundefinedelse", "doifundefinedcounter", "doifundefined", "doiftypingfileelse", "doiftopofpageelse", "doiftextflowelse", "doiftextflowcollectorelse", "doiftextelse", "doiftext", "doifsymbolsetelse", "doifsymboldefinedelse", "doifstructurelisthaspageelse", "doifstructurelisthasnumberelse", "doifstringinstringelse", "doifsometokselse", "doifsometoks", "doifsomethingelse", "doifsomething", "doifsomespaceelse", "doifsomebackgroundelse", "doifsomebackground", "doifsetupselse", "doifsetups", "doifsamestringelse", "doifsamestring", "doifsamelinereferenceelse", "doifrighttoleftinboxelse", "doifrightpagefloatelse", "doifreferencefoundelse", "doifpositionsusedelse", "doifpositionsonthispageelse", "doifpositionsonsamepageelse", "doifpositiononpageelse", "doifpositionelse", "doifpositionactionelse", "doifpositionaction", "doifposition", "doifpatternselse", "doifpathexistselse", "doifpathelse", "doifparentfileelse", "doifparallelelse", "doifoverlayelse", "doifoverlappingelse", "doifolderversionelse", "doifoldercontextelse", "doifoddpagefloatelse", "doifoddpageelse", "doifobjectreferencefoundelse", "doifobjectfoundelse", "doifnumberelse", "doifnumber", "doifnotvariable", "doifnotvalue", "doifnotsetups", "doifnotsamestring", "doifnotnumber", "doifnotmode", "doifnotinstring", "doifnotinsidesplitfloat", "doifnotinset", "doifnothingelse", "doifnothing", "doifnotflagged", "doifnotfile", "doifnotescollected", "doifnoteonsamepageelse", "doifnotenv", "doifnotemptyvariable", "doifnotemptyvalue", "doifnotempty", "doifnotdocumentvariable", "doifnotdocumentfilename", "doifnotdocumentargument", "doifnotcounter", "doifnotcommon", "doifnotcommandhandler", "doifnotallmodes", "doifnotallcommon", "doifnot", "doifnonzeropositiveelse", "doifnextparenthesiselse", "doifnextoptionalelse", "doifnextoptionalcselse", "doifnextcharelse", "doifnextbgroupelse", "doifnextbgroupcselse", "doifmodeelse", "doifmode", "doifmessageelse", "doifmeaningelse", "doifmarkingelse", "doifmainfloatbodyelse", "doiflocfileelse", "doiflocationelse", "doiflistelse", "doifleapyearelse", "doiflayouttextlineelse", "doiflayoutsomelineelse", "doiflayoutdefinedelse", "doiflayerdataelse", "doiflanguageelse", "doifitalicelse", "doifintwopassdataelse", "doifintokselse", "doifinsymbolsetelse", "doifinsymbolset", "doifinstringelse", "doifinstring", "doifinsetelse", "doifinset", "doifinsertionelse", "doifinputfileelse", "doifinelementelse", "doifincsnameelse", "doifhelpelse", "doifhasspaceelse", "doiffontsynonymelse", "doiffontpresentelse", "doiffontfeatureelse", "doiffontcharelse", "doifflaggedelse", "doiffirstcharelse", "doiffileexistselse", "doiffileelse", "doiffiledefinedelse", "doiffile", "doiffigureelse", "doiffieldcategoryelse", "doiffieldbodyelse", "doiffastoptionalcheckelse", "doiffastoptionalcheckcselse", "doifenvelse", "doifenv", "doifemptyvariableelse", "doifemptyvariable", "doifemptyvalueelse", "doifemptyvalue", "doifemptytoks", "doifemptyelse", "doifempty", "doifelsevariable", "doifelsevaluenothing", "doifelsevalue", "doifelseurldefined", "doifelseundefined", "doifelsetypingfile", "doifelsetopofpage", "doifelsetextflowcollector", "doifelsetextflow", "doifelsetext", "doifelsesymbolset", "doifelsesymboldefined", "doifelsestructurelisthaspage", "doifelsestructurelisthasnumber", "doifelsestringinstring", "doifelsesometoks", "doifelsesomething", "doifelsesomespace", "doifelsesomebackground", "doifelsesetups", "doifelsesamestring", "doifelsesamelinereference", "doifelserighttoleftinbox", "doifelserightpagefloat", "doifelserightpage", "doifelsereferencefound", "doifelsepositionsused", "doifelsepositionsonthispage", "doifelsepositionsonsamepage", "doifelsepositiononpage", "doifelsepositionaction", "doifelseposition", "doifelsepatterns", "doifelsepathexists", "doifelsepath", "doifelseparentfile", "doifelseparallel", "doifelseoverlay", "doifelseoverlapping", "doifelseolderversion", "doifelseoldercontext", "doifelseoddpagefloat", "doifelseoddpage", "doifelseobjectreferencefound", "doifelseobjectfound", "doifelsenumber", "doifelsenothing", "doifelsenoteonsamepage", "doifelsenonzeropositive", "doifelsenextparenthesis", "doifelsenextoptionalcs", "doifelsenextoptional", "doifelsenextchar", "doifelsenextbgroupcs", "doifelsenextbgroup", "doifelsemode", "doifelsemessage", "doifelsemeaning", "doifelsemarking", "doifelsemarkedpage", "doifelsemainfloatbody", "doifelselocfile", "doifelselocation", "doifelselist", "doifelseleapyear", "doifelselayouttextline", "doifelselayoutsomeline", "doifelselayoutdefined", "doifelselayerdata", "doifelselanguage", "doifelseitalic", "doifelseintwopassdata", "doifelseintoks", "doifelseinsymbolset", "doifelseinstring", "doifelseinset", "doifelseinsertion", "doifelseinputfile", "doifelseinelement", "doifelseincsname", "doifelsehelp", "doifelsehasspace", "doifelseframed", "doifelsefontsynonym", "doifelsefontpresent", "doifelsefontfeature", "doifelsefontchar", "doifelseflagged", "doifelsefirstchar", "doifelsefileexists", "doifelsefiledefined", "doifelsefile", "doifelsefigure", "doifelsefieldcategory", "doifelsefieldbody", "doifelsefastoptionalcheckcs", "doifelsefastoptionalcheck", "doifelseenv", "doifelseemptyvariable", "doifelseemptyvalue", "doifelseempty", "doifelsedrawingblack", "doifelsedocumentvariable", "doifelsedocumentfilename", "doifelsedocumentargument", "doifelsedimenstring", "doifelsedimension", "doifelsedefinedcounter", "doifelsedefined", "doifelsecurrentsynonymused", "doifelsecurrentsynonymshown", "doifelsecurrentsortingused", "doifelsecurrentfonthasfeature", "doifelsecounter", "doifelseconversionnumber", "doifelseconversiondefined", "doifelsecommon", "doifelsecommandhandler", "doifelsecolor", "doifelsebuffer", "doifelseboxincache", "doifelsebox", "doifelseblack", "doifelseassignmentcs", "doifelseassignment", "doifelseallmodes", "doifelsealldefined", "doifelseallcommon", "doifelseMPgraphic", "doifelse", "doifdrawingblackelse", "doifdocumentvariableelse", "doifdocumentvariable", "doifdocumentfilenameelse", "doifdocumentfilename", "doifdocumentargumentelse", "doifdocumentargument", "doifdimenstringelse", "doifdimensionelse", "doifdefinedelse", "doifdefinedcounterelse", "doifdefinedcounter", "doifdefined", "doifcurrentfonthasfeatureelse", "doifcounterelse", "doifcounter", "doifconversionnumberelse", "doifconversiondefinedelse", "doifcontent", "doifcommonelse", "doifcommon", "doifcommandhandlerelse", "doifcommandhandler", "doifcolorelse", "doifcolor", "doifbufferelse", "doifboxelse", "doifbothsidesoverruled", "doifbothsides", "doifblackelse", "doifassignmentelsecs", "doifassignmentelse", "doifallmodeselse", "doifallmodes", "doifalldefinedelse", "doifallcommonelse", "doifallcommon", "doifMPgraphicelse", "doif", "dogobblesingleempty", "dogobbledoubleempty", "dogetcommacommandelement", "dogetattributeid", "dogetattribute", "dofastloopcs", "doexpandedrecurse", "doeassign", "dodoublegroupempty", "dodoubleemptywithset", "dodoubleempty", "dodoubleargumentwithset", "dodoubleargument", "documentvariable", "docheckedpair", "docheckedpagestate", "docheckassignment", "doboundtext", "doassignempty", "doassign", "doaddfeature", "doadaptrightskip", "doadaptleftskip", "divides", "divideontimes", "dividedsize", "div", "distributedhsize", "displaymessage", "displaymathematics", "displaymath", "disabletrackers", "disableregime", "disableparpositions", "disableoutputstream", "disablemode", "disableexperiments", "disabledirectives", "dis", "directvspacing", "directsymbol", "directsetup", "directsetbar", "directselect", "directluacode", "directlocalframed", "directhighlight", "directgetboxlly", "directgetboxllx", "directdummyparameter", "directcopyboxfromcache", "directconvertedcounter", "directcolored", "directcolor", "directboxfromcache", "dimensiontocount", "digits", "digamma", "differentiald", "differentialD", "diamondsuit", "diamond", "diameter", "dhook", "dfrac", "devanagarinumerals", "determineregistercharacteristics", "determinenoflines", "determinelistcharacteristics", "determineheadnumber", "depthstrut", "depthspanningtext", "depthonlybox", "depthofstring", "delta", "delimitedtext", "delimited", "definextable", "definevspacingamount", "definevspacing", "definevspace", "defineviewerlayer", "defineuserdataalternative", "defineuserdata", "defineunit", "definetyping", "definetypesetting", "definetypescriptsynonym", "definetypescriptprefix", "definetypeface", "definetype", "definetwopasslist", "definetransparency", "definetooltip", "definetokenlist", "definetextflow", "definetextbackground", "definetext", "definetabulation", "definetabulate", "definetabletemplate", "definesystemvariable", "definesystemconstant", "definesystemattribute", "definesynonyms", "definesynonym", "definesymbol", "definesubformula", "definesubfield", "definestyleinstance", "definestyle", "definestartstop", "definespotcolor", "definesorting", "definesort", "definesidebar", "defineshift", "defineseparatorset", "defineselector", "definesectionlevels", "definesectionblock", "definesection", "definescript", "definescale", "defineruby", "defineresetset", "definerenderingwindow", "defineregister", "definereferenceformat", "definereference", "definepushsymbol", "definepushbutton", "defineprogram", "defineprofile", "defineprocessor", "defineprocesscolor", "defineprefixset", "definepositioning", "defineplacement", "defineperiodkerning", "defineparbuilder", "defineparallel", "defineparagraphs", "defineparagraph", "definepapersize", "definepalet", "definepairedbox", "definepagestate", "definepageshift", "definepageinjectionalternative", "definepageinjection", "definepagecolumns", "definepagechecker", "definepagebreak", "definepage", "defineoverlay", "defineoutputroutinecommand", "defineoutputroutine", "defineornament", "definenote", "definenarrower", "definenamespace", "definenamedcolor", "definemultitonecolor", "definemode", "definemixedcolumns", "definemessageconstant", "definemeasure", "definemathunstacked", "definemathundertextextensible", "definemathunderextensible", "definemathunder", "definemathtriplet", "definemathstyle", "definemathstackers", "definemathradical", "definemathovertextextensible", "definemathoverextensible", "definemathover", "definemathornament", "definemathmatrix", "definemathframed", "definemathfraction", "definemathfence", "definemathextensible", "definemathematics", "definemathdoubleextensible", "definemathdouble", "definemathcommand", "definemathcases", "definemathalignment", "definemathaccent", "definemarking", "definemarker", "definemargindata", "definemarginblock", "definemakeup", "definelowmidhigh", "definelowhigh", "definelow", "definelistextra", "definelistalternative", "definelist", "definelines", "definelinenumbering", "definelinenote", "definelinefiller", "definelayout", "definelayerpreset", "definelayer", "definelabelclass", "definelabel", "defineitems", "defineitemgroup", "defineintermediatecolor", "defineinterlinespace", "defineinterfacevariable", "defineinterfaceelement", "defineinterfaceconstant", "defineinteractionmenu", "defineinteractionbar", "defineinteraction", "defineinsertion", "defineinitial", "defineindenting", "defineindentedtext", "definehypenationfeatures", "definehspace", "definehighlight", "definehigh", "definehelp", "defineheadalternative", "definehead", "definehbox", "definegridsnapping", "definegraphictypesynonym", "defineglobalcolor", "definefrozenfont", "defineframedtext", "defineframedtable", "defineframedcontent", "defineframed", "defineformulaframed", "defineformulaalternative", "defineformula", "definefontsynonym", "definefontstyle", "definefontsolution", "definefontsize", "definefontfile", "definefontfeature", "definefontfamilypreset", "definefontfamily", "definefontfallback", "definefontalternative", "definefont", "definefloat", "definefittingpage", "definefirstline", "definefiller", "definefilesynonym", "definefilefallback", "definefileconstant", "definefiguresymbol", "definefieldstack", "definefieldcategory", "definefieldbodyset", "definefieldbody", "definefield", "definefallbackfamily", "definefacingfloat", "defineexternalfigure", "defineexpandable", "defineenumeration", "defineeffect", "definedfont", "definedescription", "definedeq", "definedelimitedtext", "definedataset", "definecounter", "defineconversionset", "defineconversion", "definecomplexorsimpleempty", "definecomplexorsimple", "definecomment", "definecommand", "definecombinedlist", "definecombination", "definecolumnsetspan", "definecolumnsetarea", "definecolumnset", "definecolumnbreak", "definecolorgroup", "definecolor", "definecollector", "definechemicalsymbol", "definechemicals", "definechemical", "definecharacterspacing", "definecharacterkerning", "definecharacter", "definecapitals", "definebutton", "definebuffer", "definebtxrendering", "definebtxregister", "definebtxdataset", "definebtx", "definebreakpoints", "definebreakpoint", "definebodyfontswitch", "definebodyfontenvironment", "definebodyfont", "defineblock", "definebar", "definebackground", "defineattribute", "defineattachment", "defineanchor", "definealternativestyle", "defineactivecharacter", "defineaccent", "defineTABLEsetup", "defineMPinstance", "define", "defconvertedvalue", "defconvertedcommand", "defconvertedargument", "defcatcodecommand", "defaultobjectreference", "defaultobjectpage", "defaultinterface", "decrementvalue", "decrementsubpagenumber", "decrementpagenumber", "decrementedcounter", "decrementcounter", "decrement", "ddots", "ddot", "dddot", "ddagger", "ddag", "dcurl", "dcaron", "dbinom", "dayspermonth", "dayoftheweek", "date", "datasetvariable", "dashv", "dashedrightarrow", "dashedleftarrow", "dasharrow", "daleth", "dagger", "dag", "d", "cyrilliczhediaeresis", "cyrilliczhedescender", "cyrilliczhebreve", "cyrilliczh", "cyrilliczediaeresis", "cyrilliczdsc", "cyrillicz", "cyrillicyu", "cyrillicystrstroke", "cyrillicystr", "cyrillicyo", "cyrillicyi", "cyrillicyerudiaeresis", "cyrillicyat", "cyrillicya", "cyrillicv", "cyrillicushrt", "cyrillicumacron", "cyrillicuk", "cyrillicudoubleacute", "cyrillicudiaeresis", "cyrillicu", "cyrillictshe", "cyrillictetse", "cyrillictedc", "cyrillict", "cyrillicshha", "cyrillicshch", "cyrillicsh", "cyrillicsftsn", "cyrillicsemisoft", "cyrillicsdsc", "cyrillicschwadiaeresis", "cyrillicschwa", "cyrillics", "cyrillicr", "cyrillicpsi", "cyrillicpemidhook", "cyrillicp", "cyrillicot", "cyrillicomegatitlo", "cyrillicomegaround", "cyrillicomega", "cyrillicodiaeresis", "cyrillicobarreddiaeresis", "cyrillicobarred", "cyrillico", "cyrillicnje", "cyrillicn", "cyrillicm", "cyrilliclje", "cyrilliclittleyusiotified", "cyrilliclittleyus", "cyrillicl", "cyrillicksi", "cyrillickoppa", "cyrillickje", "cyrillickavertstroke", "cyrillickastroke", "cyrillickahook", "cyrillickadc", "cyrillickabashkir", "cyrillick", "cyrillicje", "cyrillicizhitsadoublegrave", "cyrillicizhitsa", "cyrillicishrttail", "cyrillicishrt", "cyrillicimacron", "cyrillicii", "cyrillicigrave", "cyrillicie", "cyrillicidiaeresis", "cyrillici", "cyrillichrdsn", "cyrillichadc", "cyrillicha", "cyrillich", "cyrillicgje", "cyrillicgheupturn", "cyrillicghestroke", "cyrillicghemidhook", "cyrillicg", "cyrillicfita", "cyrillicf", "cyrillicery", "cyrillicertick", "cyrillicerev", "cyrillicentail", "cyrillicenhook", "cyrillicenghe", "cyrillicendc", "cyrillicemtail", "cyrilliceltail", "cyrilliceiotified", "cyrillicegrave", "cyrillicediaeresis", "cyrillicebreve", "cyrillice", "cyrillicdzhe", "cyrillicdzeabkhasian", "cyrillicdze", "cyrillicdje", "cyrillicd", "cyrillicchevertstroke", "cyrillicchekhakassian", "cyrillicchediaeresis", "cyrillicchedcabkhasian", "cyrillicchedc", "cyrilliccheabkhasian", "cyrillicch", "cyrillicc", "cyrillicbigyusiotified", "cyrillicbigyus", "cyrillicb", "cyrillicae", "cyrillicadiaeresis", "cyrillicabreve", "cyrillica", "cyrillicZHEdiaeresis", "cyrillicZHEdescender", "cyrillicZHEbreve", "cyrillicZH", "cyrillicZEdiaeresis", "cyrillicZDSC", "cyrillicZ", "cyrillicYstrstroke", "cyrillicYstr", "cyrillicYU", "cyrillicYO", "cyrillicYI", "cyrillicYERUdiaeresis", "cyrillicYAT", "cyrillicYA", "cyrillicV", "cyrillicUmacron", "cyrillicUdoubleacute", "cyrillicUdiaeresis", "cyrillicUSHRT", "cyrillicUK", "cyrillicU", "cyrillicTSHE", "cyrillicTITLO", "cyrillicTETSE", "cyrillicTEDC", "cyrillicT", "cyrillicSHHA", "cyrillicSHCH", "cyrillicSH", "cyrillicSFTSN", "cyrillicSEMISOFT", "cyrillicSDSC", "cyrillicSCHWAdiaeresis", "cyrillicSCHWA", "cyrillicS", "cyrillicR", "cyrillicPSILIPNEUMATA", "cyrillicPSI", "cyrillicPEmidhook", "cyrillicPALOCHKA", "cyrillicPALATALIZATION", "cyrillicP", "cyrillicOdiaeresis", "cyrillicObarreddiaeresis", "cyrillicObarred", "cyrillicOT", "cyrillicOMEGAtitlo", "cyrillicOMEGAround", "cyrillicOMEGA", "cyrillicO", "cyrillicNJE", "cyrillicN", "cyrillicM", "cyrillicLJE", "cyrillicLITTLEYUSiotified", "cyrillicLITTLEYUS", "cyrillicL", "cyrillicKSI", "cyrillicKOPPA", "cyrillicKJE", "cyrillicKAvertstroke", "cyrillicKAstroke", "cyrillicKAhook", "cyrillicKAbashkir", "cyrillicKADC", "cyrillicK", "cyrillicJE", "cyrillicImacron", "cyrillicIgrave", "cyrillicIdiaeresis", "cyrillicIZHITSAdoublegrave", "cyrillicIZHITSA", "cyrillicISHRTtail", "cyrillicISHRT", "cyrillicII", "cyrillicIE", "cyrillicI", "cyrillicHRDSN", "cyrillicHADC", "cyrillicHA", "cyrillicH", "cyrillicGJE", "cyrillicGHEupturn", "cyrillicGHEstroke", "cyrillicGHEmidhook", "cyrillicG", "cyrillicFITA", "cyrillicF", "cyrillicEiotified", "cyrillicEgrave", "cyrillicEdiaeresis", "cyrillicEbreve", "cyrillicERtick", "cyrillicERY", "cyrillicEREV", "cyrillicENtail", "cyrillicENhook", "cyrillicENGHE", "cyrillicENDC", "cyrillicEMtail", "cyrillicELtail", "cyrillicE", "cyrillicDZHE", "cyrillicDZEabkhasian", "cyrillicDZE", "cyrillicDJE", "cyrillicDASIAPNEUMATA", "cyrillicD", "cyrillicCHEvertstroke", "cyrillicCHEkhakassian", "cyrillicCHEdiaeresis", "cyrillicCHEabkhasian", "cyrillicCHEDCabkhasian", "cyrillicCHEDC", "cyrillicCH", "cyrillicC", "cyrillicBIGYUSiotified", "cyrillicBIGYUS", "cyrillicB", "cyrillicAdiaeresis", "cyrillicAbreve", "cyrillicAE", "cyrillicA", "cwopencirclearrow", "curvearrowright", "curvearrowleft", "currentxtablerow", "currentxtablecolumn", "currentvalue", "currenttime", "currentresponses", "currentregisterpageuserdata", "currentregime", "currentproject", "currentproduct", "currentoutputstream", "currentmoduleparameter", "currentmessagetext", "currentmainlanguage", "currentlistsymbol", "currentlistentrytitlerendered", "currentlistentrytitle", "currentlistentryreferenceattribute", "currentlistentrypagenumber", "currentlistentrynumber", "currentlistentrylimitedtext", "currentlistentrydestinationattribute", "currentlanguage", "currentinterface", "currentheadnumber", "currentfeaturetest", "currentenvironment", "currentdate", "currentcomponent", "currentcommalistitem", "currentbtxuservariable", "currentassignmentlistvalue", "currentassignmentlistkey", "curlywedge", "curlyvee", "curlyeqsucc", "curlyeqprec", "cup", "ctxsprint", "ctxreport", "ctxluacode", "ctxluabuffer", "ctxlua", "ctxloadluafile", "ctxlatelua", "ctxlatecommand", "ctxfunction", "ctxdirectlua", "ctxdirectcommand", "ctxcommand", "ctop", "cstroke", "crightoverleftarrow", "crightarrow", "crampedrlap", "crampedllap", "crampedclap", "cramped", "counttokens", "counttoken", "countersubs", "correctwhitespace", "copyunittext", "copytaglabeltext", "copysuffixtext", "copysetups", "copyright", "copyprefixtext", "copyposition", "copyparameters", "copypages", "copyoperatortext", "copymathlabeltext", "copylabeltext", "copyheadtext", "copyfield", "copybtxlabeltext", "copyboxfromcache", "coprod", "convertvboxtohbox", "convertvalue", "convertnumber", "convertmonth", "convertedsubcounter", "converteddimen", "convertedcounter", "convertcommand", "convertargument", "continueifinputfile", "continuednumber", "contentreference", "constantnumberargument", "constantnumber", "constantemptyargument", "constantdimenargument", "constantdimen", "cong", "compresult", "composedlayer", "composedcollector", "component", "complexorsimpleempty", "complexorsimple", "complexes", "completeregister", "completepagenumber", "completelistoftables", "completelistofsynonyms", "completelistofsorts", "completelistofpublications", "completelistoflogos", "completelistofintermezzi", "completelistofgraphics", "completelistoffigures", "completelistofchemicals", "completelistofabbreviations", "completelist", "completeindex", "completecontent", "completebtxrendering", "complement", "comparepalet", "comparedimensioneps", "comparedimension", "comparecolorgroup", "comment", "commalistsize", "commalistsentence", "commalistelement", "combinepages", "columnsetspanwidth", "columnbreak", "column", "colorvalue", "coloronly", "colored", "colorcomponents", "colorbar", "color", "colonequals", "coloncolonequals", "colon", "collectexpanded", "collectedtext", "collect", "clubsuit", "clonefield", "clippedoverlayimage", "clip", "cleftarrow", "cldprocessfile", "cldloadfile", "cldcontext", "cldcommand", "classfont", "clap", "cite", "citation", "circleonrightarrow", "circledequals", "circleddash", "circledcirc", "circledast", "circledS", "circledR", "circlearrowright", "circlearrowleft", "circeq", "circ", "chook", "chinesenumerals", "chinesecapnumerals", "chineseallnumerals", "chi", "chemicaltoptext", "chemicaltext", "chemicalsymbol", "chemicalmidtext", "chemicalbottext", "chemical", "chem", "checkvariables", "checktwopassdata", "checksoundtrack", "checkpreviousinjector", "checkparameters", "checkpage", "checknextinjector", "checknextindentation", "checkmark", "checkinjector", "checkedstrippedcsname", "checkedfiller", "checkedchar", "checkedblank", "checkcharacteralign", "check", "charwidthlanguage", "chardescription", "characters", "character", "chapter", "cfrac", "centerline", "centerednextbox", "centeredlastline", "centeredbox", "centerdot", "centerbox", "centeraligned", "cdots", "cdotp", "cdotaccent", "cdot", "ccurl", "ccircumflex", "ccedilla", "ccaron", "cbox", "catcodetablename", "carriagereturn", "cap", "camel", "calligraphic", "cacute", "button", "bullet", "buildtextognek", "buildtextmacron", "buildtextgrave", "buildtextcedilla", "buildtextbottomdot", "buildtextbottomcomma", "buildtextaccent", "buildmathaccent", "btxtextcitation", "btxsingularplural", "btxsingularorplural", "btxsetup", "btxsavejournalist", "btxremapauthor", "btxoneorrange", "btxloadjournalist", "btxlistcitation", "btxlabeltext", "btxlabellanguage", "btxhybridcite", "btxhiddencitation", "btxfoundtype", "btxfoundname", "btxflushsuffix", "btxflushauthornormalshort", "btxflushauthornormal", "btxflushauthorname", "btxflushauthorinvertedshort", "btxflushauthorinverted", "btxflushauthor", "btxflush", "btxfirstofrange", "btxfieldtype", "btxfieldname", "btxfield", "btxexpandedjournal", "btxdoifuservariableelse", "btxdoifsameaspreviouselse", "btxdoifsameaspreviouscheckedelse", "btxdoifnot", "btxdoifelseuservariable", "btxdoifelsesameaspreviouschecked", "btxdoifelsesameasprevious", "btxdoifelsecombiinlist", "btxdoifelse", "btxdoifcombiinlistelse", "btxdoif", "btxdirect", "btxdetail", "btxauthorfield", "btxalwayscitation", "btxaddjournal", "btxabbreviatedjournal", "bstroke", "breve", "breakhere", "breakablethinspace", "bpos", "boxtimes", "boxreference", "boxplus", "boxofsize", "boxminus", "boxmarker", "boxdot", "boxcursor", "bowtie", "bottomrightbox", "bottomleftbox", "bottombox", "bot", "bordermatrix", "booleanmodevalue", "bookmark", "boldslanted", "bolditalic", "boldface", "bold", "bodyfontsize", "bodyfontenvironmentlist", "blockuservariable", "blocksynctexfile", "blockquote", "blockligatures", "bleedwidth", "bleedheight", "bleed", "blap", "blank", "blacktriangleright", "blacktriangleleft", "blacktriangledown", "blacktriangle", "blacksquare", "blackrules", "blackrule", "blacklozenge", "bitmapimage", "binom", "bigwedge", "bigvee", "biguplus", "bigudot", "bigtriangleup", "bigtriangledown", "bigtimes", "bigstar", "bigsquare", "bigsqcup", "bigsqcap", "bigskip", "bigr", "bigotimes", "bigoplus", "bigodot", "bigm", "bigl", "biggr", "biggm", "biggl", "bigger", "bigg", "bigdiamond", "bigcup", "bigcircle", "bigcirc", "bigcap", "bigbodyfont", "big", "bhook", "between", "beth", "beta", "beforetestandsplitstring", "beforesplitstring", "because", "bbox", "bbordermatrix", "baselinerightbox", "baselinemiddlebox", "baselineleftbox", "baselinebottom", "basegrid", "barwedge", "barovernorthwestarrow", "barleftarrowrightarrowbar", "barleftarrow", "bar", "backslash", "backsim", "backprime", "backgroundline", "backgroundimagefill", "backgroundimage", "background", "backepsilon", "averagecharwidth", "availablehsize", "autosetups", "autopagestaterealpageorder", "autopagestaterealpage", "automathematics", "autointegral", "autoinsertnextspace", "autodirvtop", "autodirvbox", "autodirhbox", "autocap", "attachment", "atrightmargin", "atpage", "atleftmargin", "atilde", "at", "asymp", "astype", "ast", "assumelongusagecs", "assignwidth", "assignvalue", "assigntranslation", "assignifempty", "assigndimension", "assigndimen", "assignalfadimension", "aside", "asciistr", "arrowvert", "aringacute", "aring", "arg", "arabicwasallam", "arabicvowelyeh", "arabicvowelwaw", "arabictripledot", "arabicstartofrubc", "arabicslcm", "arabicshighthreedots", "arabicsemicolon", "arabicsanah", "arabicsamvat", "arabicsalla", "arabicsajdah", "arabicsafha", "arabicrialsign", "arabicray", "arabicrasoul", "arabicquestion", "arabicqala", "arabicpoeticverse", "arabicpertenthousand", "arabicpermille", "arabicperiod", "arabicpercent", "arabicparenright", "arabicparenleft", "arabicnumerals", "arabicnumberabove", "arabicnumber", "arabicmuhammad", "arabicmisra", "arabiclowseen", "arabiclownoonkasra", "arabiclowmeemlong", "arabiclettermark", "arabicjallajalalouhou", "arabichighzain", "arabichighyeh", "arabichighwaqf", "arabichighthalatha", "arabichightakhallus", "arabichightah", "arabichighsmallsafha", "arabichighseen", "arabichighsallallahou", "arabichighsakta", "arabichighsajda", "arabichighsad", "arabichighrubc", "arabichighrahmatullahalayhe", "arabichighradiallahouanhu", "arabichighqif", "arabichighqaf", "arabichighnoonkasra", "arabichighnoon", "arabichighnisf", "arabichighmeemshort", "arabichighmeemlong", "arabichighmadda", "arabichighlamalef", "arabichighjeem", "arabichighfootnotemarker", "arabichighesala", "arabichigheqala", "arabichighalayheassallam", "arabichighain", "arabicfourthroot", "arabicfootnotemarker", "arabicexnumerals", "arabicendofayah", "arabicdisputedendofayah", "arabicdecimals", "arabicdateseparator", "arabiccuberoot", "arabiccomma", "arabicbasmalah", "arabicasterisk", "arabicallallahou", "arabicallah", "arabicalayhe", "arabicakbar", "approxnEq", "approxeq", "approxEq", "approx", "applytowords", "applytosplitstringwordspaced", "applytosplitstringword", "applytosplitstringlinespaced", "applytosplitstringline", "applytosplitstringcharspaced", "applytosplitstringchar", "applytofirstcharacter", "applytocharacters", "applyprocessor", "applyalternativestyle", "apply", "appendvalue", "appendtoksonce", "appendtoks", "appendtocommalist", "appendgvalue", "appendetoks", "aogonek", "angle", "anchor", "ampersand", "amalg", "amacron", "alwayscite", "alwayscitation", "alphabeticnumerals", "alpha", "allinputpaths", "alignmentcharacter", "alignhere", "alignedline", "alignedbox", "aligned", "alignbottom", "aleph", "ainvertedbreve", "ahook", "agrave", "aftertestandsplitstring", "aftersplitstring", "afghanicurrency", "aemacron", "aeligature", "aeacute", "adoublegrave", "adotbelow", "adotaccentmacron", "adotaccent", "adiaeresismacron", "adiaeresis", "addvalue", "addtocommalist", "addtoJSpreamble", "addfontpath", "addfeature", "adaptpapersize", "adaptlayout", "adaptfontfeature", "adaptcollector", "acwopencirclearrow", "acute", "actuarial", "activatespacehandler", "acircumflextilde", "acircumflexhook", "acircumflexgrave", "acircumflexdotbelow", "acircumflexacute", "acircumflex", "acaron", "abrevetilde", "abrevehook", "abrevegrave", "abrevedotbelow", "abreveacute", "abreve", "about", "abjadnumerals", "abjadnodotnumerals", "abjadnaivenumerals", "abbreviation", "aacute", "Zstroke", "Zhook", "Zeta", "Zdotaccent", "Zcaron", "Zacute", "Ytilde", "Ymacron", "Yhook", "Ygrave", "Ydotbelow", "Ydiaeresis", "Ycircumflex", "Yacute", "Xi", "XeTeX", "XETEX", "Words", "Word", "WidthSpanningText", "Wcircumflex", "WORDS", "WORD", "WEEKDAY", "Vvdash", "Vert", "VerboseNumber", "Vdash", "VDash", "Uuparrow", "Utilde", "Uring", "Upsilon", "Updownarrow", "Uparrow", "Uogonek", "Umacron", "Uinvertedbreve", "Uhungarumlaut", "Uhorntilde", "Uhornhook", "Uhorngrave", "Uhorndotbelow", "Uhornacute", "Uhorn", "Uhook", "Ugrave", "Udoublegrave", "Udotbelow", "Udiaeresismacron", "Udiaeresisgrave", "Udiaeresiscaron", "Udiaeresisacute", "Udiaeresis", "Ucircumflex", "Ucaron", "Ubreve", "Uacute", "Tstroke", "TransparencyHack", "Thorn", "Thook", "Theta", "TheNormalizedFontSize", "TeX", "Tcommaaccent", "Tcedilla", "Tcaron", "Tau", "TaBlE", "TEX", "TABLE", "Swarrow", "Supset", "Subset", "Smallcapped", "Sigma", "Searrow", "Scommaaccent", "Scircumflex", "Schwa", "Scedilla", "Scaron", "ScaledPointsToWholeBigPoints", "ScaledPointsToBigPoints", "Sacute", "S", "Rsh", "Rrightarrow", "Romannumerals", "Rinvertedbreve", "Rightarrow", "Rho", "Relbar", "ReadFile", "Re", "Rdsh", "Rdoublegrave", "Rcommaaccent", "Rcaron", "Racute", "PtToCm", "Psi", "PropertyLine", "PointsToWholeBigPoints", "PointsToReal", "PointsToBigPoints", "Plankconst", "PiCTeX", "Pi", "Phook", "Phi", "PRAGMA", "PPCHTeX", "PPCHTEX", "PICTEX", "PDFcolor", "PDFTEX", "PDFETEX", "P", "Otildemacron", "Otilde", "Ostrokeacute", "Ostroke", "Oogonekmacron", "Oogonek", "Omicron", "Omega", "Omacron", "Oinvertedbreve", "Ohungarumlaut", "Ohorntilde", "Ohornhook", "Ohorngrave", "Ohorndotbelow", "Ohornacute", "Ohorn", "Ohook", "Ograve", "Odoublegrave", "Odotbelow", "Odotaccentmacron", "Odotaccent", "Odiaeresismacron", "Odiaeresis", "Ocircumflextilde", "Ocircumflexhook", "Ocircumflexgrave", "Ocircumflexdotbelow", "Ocircumflexacute", "Ocircumflex", "Ocaron", "Obreve", "Oacute", "OEligature", "Nwarrow", "Numbers", "Nu", "Ntilde", "NormalizeTextWidth", "NormalizeTextHeight", "NormalizeFontWidth", "NormalizeFontHeight", "Njligature", "Ngrave", "Neng", "Nearrow", "Ncommaaccent", "Ncaron", "Nacute", "NJligature", "Mu", "MetaPost", "MetaFun", "MetaFont", "Mapsto", "Mapsfrom", "MPy", "MPxywhd", "MPxy", "MPx", "MPwhd", "MPw", "MPvv", "MPvariable", "MPvar", "MPv", "MPur", "MPul", "MPtransparency", "MPtext", "MPstring", "MPrs", "MPrightskip", "MPrest", "MPregion", "MPrawvar", "MPr", "MPposset", "MPpositiongraphic", "MPpos", "MPplus", "MPpardata", "MPpage", "MPp", "MPoverlayanchor", "MPoptions", "MPn", "MPmenubuttons", "MPls", "MPlr", "MPll", "MPleftskip", "MPinclusions", "MPh", "MPgetposboxes", "MPgetmultishape", "MPgetmultipars", "MPfontsizehskip", "MPdrawing", "MPd", "MPcolumn", "MPcoloronly", "MPcolor", "MPcode", "MPc", "MPbetex", "MPanchor", "MPVI", "MPIV", "MPII", "MONTHSHORT", "MONTHLONG", "MONTH", "MKXI", "MKVI", "MKIX", "MKIV", "MKII", "METAPOST", "METAFUN", "METAFONT", "LuajitTeX", "LuaTeX", "LuaMetaTeX", "Lua", "Lstroke", "Lsh", "Longrightarrow", "Longmapsto", "Longmapsfrom", "Longleftrightarrow", "Longleftarrow", "Lleftarrow", "Ljligature", "Leftrightarrow", "Leftarrow", "Ldsh", "Ldotmiddle", "Lcommaaccent", "Lcaron", "Lbar", "Lambda", "LamSTeX", "Lacute", "LaTeX", "LUATEX", "LUAMETATEX", "LUAJITTEX", "LJligature", "LATEX", "LAMSTEX", "Khook", "Kcommaaccent", "Kcaron", "Kappa", "Join", "Jcircumflex", "Itilde", "Istroke", "Iota", "Iogonek", "Imacron", "Im", "Iinvertedbreve", "Ihook", "Igrave", "Idoublegrave", "Idotbelow", "Idotaccent", "Idiaeresis", "Icircumflex", "Icaron", "Ibreve", "Iacute", "INRSTEX", "IJligature", "Hstroke", "Hcircumflex", "Hcaron", "Hat", "Gstroke", "Greeknumerals", "GotoPar", "Ghook", "GetPar", "Gdotaccent", "Gcommaaccent", "Gcircumflex", "Gcaron", "Gbreve", "Gamma", "Game", "Gacute", "Finv", "Fhook", "EveryPar", "EveryLine", "Eulerconst", "Etilde", "Eth", "Eta", "Epsilon", "Eogonek", "Emacron", "Einvertedbreve", "Ehook", "Egrave", "Edoublegrave", "Edotbelow", "Edotaccent", "Ediaeresis", "Ecircumflextilde", "Ecircumflexhook", "Ecircumflexgrave", "Ecircumflexdotbelow", "Ecircumflexacute", "Ecircumflex", "Ecedilla", "Ecaron", "Ebreve", "Eacute", "ETEX", "Dzligature", "Dzcaronligature", "Dstroke", "Downarrow", "Doteq", "Dhook", "Delta", "Ddownarrow", "Dcaron", "Dafrican", "DZligature", "DZcaronligature", "Cup", "Cstroke", "ConvertToConstant", "ConvertConstantAfter", "Context", "ConTeXt", "Chook", "Chi", "Characters", "Character", "Cdotaccent", "Ccircumflex", "Ccedilla", "Ccaron", "Caps", "Cap", "Cacute", "CONTEXT", "Bumpeq", "Box", "Bigr", "Bigm", "Bigl", "Biggr", "Biggm", "Biggl", "Bigg", "Big", "Bhook", "Beta", "BeforePar", "Atilde", "Astroke", "Arrowvert", "Aringacute", "Aring", "Aogonek", "Angstrom", "And", "Amacron", "AmSTeX", "Alphabeticnumerals", "Alpha", "Ainvertedbreve", "Ahook", "Agrave", "AfterPar", "Adoublegrave", "Adotbelow", "Adotaccentmacron", "Adotaccent", "Adiaeresismacron", "Adiaeresis", "Acircumflextilde", "Acircumflexhook", "Acircumflexgrave", "Acircumflexdotbelow", "Acircumflexacute", "Acircumflex", "Acaron", "Abrevetilde", "Abrevehook", "Abrevegrave", "Abrevedotbelow", "Abreveacute", "Abreve", "Aacute", "AMSTEX", "AEmacron", "AEligature", "AEacute" };
                var listcommands = commands.Select(array => new CompletionItem(@"\" + array, array, CompletionItemKind.Function) { Documentation = new IMarkdownString("ConTeXt Command"), Detail = "Command", InsertTextRules = CompletionItemInsertTextRule.KeepWhitespace }).ToList();
                string[] primitives = new string[] { "year", "xtokspre", "xtoksapp", "xspaceskip", "xleaders", "xdef", "write", "wordboundary", "widowpenalty", "widowpenalties", "wd", "vtop", "vss", "vsplit", "vskip", "vsize", "vrule", "vpack", "voffset", "vfuzz", "vfilneg", "vfill", "vfil", "vcenter", "vbox", "vbadness", "valign", "vadjust", "useimageresource", "useboxresource", "uppercase", "unvcopy", "unvbox", "unskip", "unpenalty", "unless", "unkern", "uniformdeviate", "unhcopy", "unhbox", "underline", "uchyph", "uccode", "tracingstats", "tracingscantokens", "tracingrestores", "tracingparagraphs", "tracingpages", "tracingoutput", "tracingonline", "tracingnesting", "tracingmacros", "tracinglostchars", "tracingifs", "tracinggroups", "tracingfonts", "tracingcommands", "tracingassigns", "tpack", "topskip", "topmarks", "topmark", "tolerance", "tokspre", "toksdef", "toksapp", "toks", "time", "thinmuskip", "thickmuskip", "the", "textstyle", "textfont", "textdirection", "textdir", "tagcode", "tabskip", "synctex", "suppressprimitiveerror", "suppressoutererror", "suppressmathparerror", "suppresslongerror", "suppressifcsnameerror", "suppressfontnotfounderror", "string", "splittopskip", "splitmaxdepth", "splitfirstmarks", "splitfirstmark", "splitdiscards", "splitbotmarks", "splitbotmark", "special", "span", "spaceskip", "spacefactor", "skipdef", "skip", "skewchar", "showtokens", "showthe", "showlists", "showifs", "showgroups", "showboxdepth", "showboxbreadth", "showbox", "show", "shipout", "shapemode", "sfcode", "setrandomseed", "setlanguage", "setfontid", "setbox", "scrollmode", "scriptstyle", "scriptspace", "scriptscriptstyle", "scriptscriptfont", "scriptfont", "scantokens", "scantextokens", "savingvdiscards", "savinghyphcodes", "savepos", "saveimageresource", "savecatcodetable", "saveboxresource", "rpcode", "romannumeral", "rightskip", "rightmarginkern", "righthyphenmin", "rightghost", "right", "relpenalty", "relax", "readline", "read", "randomseed", "raise", "radical", "quitvmode", "pxdimen", "protrusionboundary", "protrudechars", "primitive", "prevgraf", "prevdepth", "pretolerance", "prerelpenalty", "prehyphenchar", "preexhyphenchar", "predisplaysize", "predisplaypenalty", "predisplaygapfactor", "predisplaydirection", "prebinoppenalty", "posthyphenchar", "postexhyphenchar", "postdisplaypenalty", "penalty", "pdfximage", "pdfxformresources", "pdfxformname", "pdfxformmargin", "pdfxformattr", "pdfxform", "pdfvorigin", "pdfvariable", "pdfuniqueresname", "pdfuniformdeviate", "pdftrailerid", "pdftrailer", "pdftracingfonts", "pdfthreadmargin", "pdfthread", "pdftexversion", "pdftexrevision", "pdftexbanner", "pdfsuppressptexinfo", "pdfsuppressoptionalinfo", "pdfstartthread", "pdfstartlink", "pdfsetrandomseed", "pdfsetmatrix", "pdfsavepos", "pdfsave", "pdfretval", "pdfrestore", "pdfreplacefont", "pdfrefximage", "pdfrefxform", "pdfrefobj", "pdfrecompress", "pdfrandomseed", "pdfpxdimen", "pdfprotrudechars", "pdfprimitive", "pdfpkresolution", "pdfpkmode", "pdfpkfixeddpi", "pdfpagewidth", "pdfpagesattr", "pdfpageresources", "pdfpageref", "pdfpageheight", "pdfpagebox", "pdfpageattr", "pdfoutput", "pdfoutline", "pdfomitcidset", "pdfomitcharset", "pdfobjcompresslevel", "pdfobj", "pdfnormaldeviate", "pdfnoligatures", "pdfnames", "pdfminorversion", "pdfmapline", "pdfmapfile", "pdfmajorversion", "pdfliteral", "pdflinkmargin", "pdflastypos", "pdflastxpos", "pdflastximagepages", "pdflastximage", "pdflastxform", "pdflastobj", "pdflastlink", "pdflastlinedepth", "pdflastannot", "pdfinsertht", "pdfinfoomitdate", "pdfinfo", "pdfinclusionerrorlevel", "pdfinclusioncopyfonts", "pdfincludechars", "pdfimageresolution", "pdfimagehicolor", "pdfimagegamma", "pdfimageapplygamma", "pdfimageaddfilename", "pdfignoreunknownimages", "pdfignoreddimen", "pdfhorigin", "pdfglyphtounicode", "pdfgentounicode", "pdfgamma", "pdffontsize", "pdffontobjnum", "pdffontname", "pdffontexpand", "pdffontattr", "pdffirstlineheight", "pdffeedback", "pdfextension", "pdfendthread", "pdfendlink", "pdfeachlineheight", "pdfeachlinedepth", "pdfdraftmode", "pdfdestmargin", "pdfdest", "pdfdecimaldigits", "pdfcreationdate", "pdfcopyfont", "pdfcompresslevel", "pdfcolorstackinit", "pdfcolorstack", "pdfcatalog", "pdfannot", "pdfadjustspacing", "pausing", "patterns", "parskip", "parshapelength", "parshapeindent", "parshapedimen", "parshape", "parindent", "parfillskip", "pardirection", "pardir", "par", "pagewidth", "pagetotal", "pagetopoffset", "pagestretch", "pageshrink", "pagerightoffset", "pageleftoffset", "pageheight", "pagegoal", "pagefilstretch", "pagefillstretch", "pagefilllstretch", "pagediscards", "pagedirection", "pagedir", "pagedepth", "pagebottomoffset", "overwithdelims", "overline", "overfullrule", "over", "outputpenalty", "outputmode", "outputbox", "output", "outer", "or", "openout", "openin", "omit", "numexpr", "number", "nullfont", "nulldelimiterspace", "novrule", "nospaces", "normalyear", "normalxtokspre", "normalxtoksapp", "normalxspaceskip", "normalxleaders", "normalxdef", "normalwrite", "normalwordboundary", "normalwidowpenalty", "normalwidowpenalties", "normalwd", "normalvtop", "normalvss", "normalvsplit", "normalvskip", "normalvsize", "normalvrule", "normalvpack", "normalvoffset", "normalvfuzz", "normalvfilneg", "normalvfill", "normalvfil", "normalvcenter", "normalvbox", "normalvbadness", "normalvalign", "normalvadjust", "normaluseimageresource", "normaluseboxresource", "normaluppercase", "normalunvcopy", "normalunvbox", "normalunskip", "normalunpenalty", "normalunless", "normalunkern", "normaluniformdeviate", "normalunhcopy", "normalunhbox", "normalunexpanded", "normalunderline", "normaluchyph", "normaluccode", "normaltracingstats", "normaltracingscantokens", "normaltracingrestores", "normaltracingparagraphs", "normaltracingpages", "normaltracingoutput", "normaltracingonline", "normaltracingnesting", "normaltracingmacros", "normaltracinglostchars", "normaltracingifs", "normaltracinggroups", "normaltracingfonts", "normaltracingcommands", "normaltracingassigns", "normaltpack", "normaltopskip", "normaltopmarks", "normaltopmark", "normaltolerance", "normaltokspre", "normaltoksdef", "normaltoksapp", "normaltoks", "normaltime", "normalthinmuskip", "normalthickmuskip", "normalthe", "normaltextstyle", "normaltextfont", "normaltextdirection", "normaltextdir", "normaltagcode", "normaltabskip", "normalsynctex", "normalsuppressprimitiveerror", "normalsuppressoutererror", "normalsuppressmathparerror", "normalsuppresslongerror", "normalsuppressifcsnameerror", "normalsuppressfontnotfounderror", "normalstring", "normalsplittopskip", "normalsplitmaxdepth", "normalsplitfirstmarks", "normalsplitfirstmark", "normalsplitdiscards", "normalsplitbotmarks", "normalsplitbotmark", "normalspecial", "normalspan", "normalspaceskip", "normalspacefactor", "normalskipdef", "normalskip", "normalskewchar", "normalshowtokens", "normalshowthe", "normalshowlists", "normalshowifs", "normalshowgroups", "normalshowboxdepth", "normalshowboxbreadth", "normalshowbox", "normalshow", "normalshipout", "normalshapemode", "normalsfcode", "normalsetrandomseed", "normalsetlanguage", "normalsetfontid", "normalsetbox", "normalscrollmode", "normalscriptstyle", "normalscriptspace", "normalscriptscriptstyle", "normalscriptscriptfont", "normalscriptfont", "normalscantokens", "normalscantextokens", "normalsavingvdiscards", "normalsavinghyphcodes", "normalsavepos", "normalsaveimageresource", "normalsavecatcodetable", "normalsaveboxresource", "normalrpcode", "normalromannumeral", "normalrightskip", "normalrightmarginkern", "normalrighthyphenmin", "normalrightghost", "normalright", "normalrelpenalty", "normalrelax", "normalreadline", "normalread", "normalrandomseed", "normalraise", "normalradical", "normalquitvmode", "normalpxdimen", "normalprotrusionboundary", "normalprotrudechars", "normalprotected", "normalprimitive", "normalprevgraf", "normalprevdepth", "normalpretolerance", "normalprerelpenalty", "normalprehyphenchar", "normalpreexhyphenchar", "normalpredisplaysize", "normalpredisplaypenalty", "normalpredisplaygapfactor", "normalpredisplaydirection", "normalprebinoppenalty", "normalposthyphenchar", "normalpostexhyphenchar", "normalpostdisplaypenalty", "normalpenalty", "normalpdfximage", "normalpdfxformresources", "normalpdfxformname", "normalpdfxformmargin", "normalpdfxformattr", "normalpdfxform", "normalpdfvorigin", "normalpdfvariable", "normalpdfuniqueresname", "normalpdfuniformdeviate", "normalpdftrailerid", "normalpdftrailer", "normalpdftracingfonts", "normalpdfthreadmargin", "normalpdfthread", "normalpdftexversion", "normalpdftexrevision", "normalpdftexbanner", "normalpdfsuppressptexinfo", "normalpdfsuppressoptionalinfo", "normalpdfstartthread", "normalpdfstartlink", "normalpdfsetrandomseed", "normalpdfsetmatrix", "normalpdfsavepos", "normalpdfsave", "normalpdfretval", "normalpdfrestore", "normalpdfreplacefont", "normalpdfrefximage", "normalpdfrefxform", "normalpdfrefobj", "normalpdfrecompress", "normalpdfrandomseed", "normalpdfpxdimen", "normalpdfprotrudechars", "normalpdfprimitive", "normalpdfpkresolution", "normalpdfpkmode", "normalpdfpkfixeddpi", "normalpdfpagewidth", "normalpdfpagesattr", "normalpdfpageresources", "normalpdfpageref", "normalpdfpageheight", "normalpdfpagebox", "normalpdfpageattr", "normalpdfoutput", "normalpdfoutline", "normalpdfomitcidset", "normalpdfomitcharset", "normalpdfobjcompresslevel", "normalpdfobj", "normalpdfnormaldeviate", "normalpdfnoligatures", "normalpdfnames", "normalpdfminorversion", "normalpdfmapline", "normalpdfmapfile", "normalpdfmajorversion", "normalpdfliteral", "normalpdflinkmargin", "normalpdflastypos", "normalpdflastxpos", "normalpdflastximagepages", "normalpdflastximage", "normalpdflastxform", "normalpdflastobj", "normalpdflastlink", "normalpdflastlinedepth", "normalpdflastannot", "normalpdfinsertht", "normalpdfinfoomitdate", "normalpdfinfo", "normalpdfinclusionerrorlevel", "normalpdfinclusioncopyfonts", "normalpdfincludechars", "normalpdfimageresolution", "normalpdfimagehicolor", "normalpdfimagegamma", "normalpdfimageapplygamma", "normalpdfimageaddfilename", "normalpdfignoreunknownimages", "normalpdfignoreddimen", "normalpdfhorigin", "normalpdfglyphtounicode", "normalpdfgentounicode", "normalpdfgamma", "normalpdffontsize", "normalpdffontobjnum", "normalpdffontname", "normalpdffontexpand", "normalpdffontattr", "normalpdffirstlineheight", "normalpdffeedback", "normalpdfextension", "normalpdfendthread", "normalpdfendlink", "normalpdfeachlineheight", "normalpdfeachlinedepth", "normalpdfdraftmode", "normalpdfdestmargin", "normalpdfdest", "normalpdfdecimaldigits", "normalpdfcreationdate", "normalpdfcopyfont", "normalpdfcompresslevel", "normalpdfcolorstackinit", "normalpdfcolorstack", "normalpdfcatalog", "normalpdfannot", "normalpdfadjustspacing", "normalpausing", "normalpatterns", "normalparskip", "normalparshapelength", "normalparshapeindent", "normalparshapedimen", "normalparshape", "normalparindent", "normalparfillskip", "normalpardirection", "normalpardir", "normalpar", "normalpagewidth", "normalpagetotal", "normalpagetopoffset", "normalpagestretch", "normalpageshrink", "normalpagerightoffset", "normalpageleftoffset", "normalpageheight", "normalpagegoal", "normalpagefilstretch", "normalpagefillstretch", "normalpagefilllstretch", "normalpagediscards", "normalpagedirection", "normalpagedir", "normalpagedepth", "normalpagebottomoffset", "normaloverwithdelims", "normaloverline", "normaloverfullrule", "normalover", "normaloutputpenalty", "normaloutputmode", "normaloutputbox", "normaloutput", "normalouter", "normalor", "normalopenout", "normalopenin", "normalomit", "normalnumexpr", "normalnumber", "normalnullfont", "normalnulldelimiterspace", "normalnovrule", "normalnospaces", "normalnormaldeviate", "normalnonstopmode", "normalnonscript", "normalnolimits", "normalnoligs", "normalnokerns", "normalnoindent", "normalnohrule", "normalnoexpand", "normalnoboundary", "normalnoalign", "normalnewlinechar", "normalmutoglue", "normalmuskipdef", "normalmuskip", "normalmultiply", "normalmuexpr", "normalmskip", "normalmoveright", "normalmoveleft", "normalmonth", "normalmkern", "normalmiddle", "normalmessage", "normalmedmuskip", "normalmeaning", "normalmaxdepth", "normalmaxdeadcycles", "normalmathsurroundskip", "normalmathsurroundmode", "normalmathsurround", "normalmathstyle", "normalmathscriptsmode", "normalmathscriptcharmode", "normalmathscriptboxmode", "normalmathrulethicknessmode", "normalmathrulesmode", "normalmathrulesfam", "normalmathrel", "normalmathpunct", "normalmathpenaltiesmode", "normalmathord", "normalmathoption", "normalmathopen", "normalmathop", "normalmathnolimitsmode", "normalmathitalicsmode", "normalmathinner", "normalmathflattenmode", "normalmatheqnogapstep", "normalmathdisplayskipmode", "normalmathdirection", "normalmathdir", "normalmathdelimitersmode", "normalmathcode", "normalmathclose", "normalmathchoice", "normalmathchardef", "normalmathchar", "normalmathbin", "normalmathaccent", "normalmarks", "normalmark", "normalmag", "normalluatexversion", "normalluatexrevision", "normalluatexbanner", "normalluafunctioncall", "normalluafunction", "normalluaescapestring", "normalluadef", "normalluacopyinputnodes", "normalluabytecodecall", "normalluabytecode", "normallpcode", "normallowercase", "normallower", "normallooseness", "normallong", "normallocalrightbox", "normallocalleftbox", "normallocalinterlinepenalty", "normallocalbrokenpenalty", "normallinepenalty", "normallinedirection", "normallinedir", "normallimits", "normalletterspacefont", "normalletcharcode", "normallet", "normalleqno", "normalleftskip", "normalleftmarginkern", "normallefthyphenmin", "normalleftghost", "normalleft", "normalleaders", "normallccode", "normallateluafunction", "normallatelua", "normallastypos", "normallastxpos", "normallastskip", "normallastsavedimageresourcepages", "normallastsavedimageresourceindex", "normallastsavedboxresourceindex", "normallastpenalty", "normallastnodetype", "normallastnamedcs", "normallastlinefit", "normallastkern", "normallastbox", "normallanguage", "normalkern", "normaljobname", "normalinterlinepenalty", "normalinterlinepenalties", "normalinteractionmode", "normalinsertpenalties", "normalinsertht", "normalinsert", "normalinputlineno", "normalinput", "normalinitcatcodetable", "normalindent", "normalimmediateassignment", "normalimmediateassigned", "normalimmediate", "normalignorespaces", "normalignoreligaturesinfont", "normalifx", "normalifvoid", "normalifvmode", "normalifvbox", "normaliftrue", "normalifprimitive", "normalifpdfprimitive", "normalifpdfabsnum", "normalifpdfabsdim", "normalifodd", "normalifnum", "normalifmmode", "normalifinner", "normalifincsname", "normalifhmode", "normalifhbox", "normaliffontchar", "normaliffalse", "normalifeof", "normalifdim", "normalifdefined", "normalifcsname", "normalifcondition", "normalifcat", "normalifcase", "normalifabsnum", "normalifabsdim", "normalif", "normalhyphenpenaltymode", "normalhyphenpenalty", "normalhyphenchar", "normalhyphenationmin", "normalhyphenationbounds", "normalhyphenation", "normalht", "normalhss", "normalhskip", "normalhsize", "normalhrule", "normalhpack", "normalholdinginserts", "normalhoffset", "normalhjcode", "normalhfuzz", "normalhfilneg", "normalhfill", "normalhfil", "normalhbox", "normalhbadness", "normalhangindent", "normalhangafter", "normalhalign", "normalgtokspre", "normalgtoksapp", "normalgluetomu", "normalgluestretchorder", "normalgluestretch", "normalglueshrinkorder", "normalglueshrink", "normalglueexpr", "normalglobaldefs", "normalglobal", "normalglet", "normalgleaders", "normalgdef", "normalfuturelet", "normalfutureexpandis", "normalfutureexpand", "normalformatname", "normalfontname", "normalfontid", "normalfontdimen", "normalfontcharwd", "normalfontcharic", "normalfontcharht", "normalfontchardp", "normalfont", "normalfloatingpenalty", "normalfixupboxesmode", "normalfirstvalidlanguage", "normalfirstmarks", "normalfirstmark", "normalfinalhyphendemerits", "normalfi", "normalfam", "normalexplicithyphenpenalty", "normalexplicitdiscretionary", "normalexpandglyphsinfont", "normalexpanded", "normalexpandafter", "normalexhyphenpenalty", "normalexhyphenchar", "normalexceptionpenalty", "normaleveryvbox", "normaleverypar", "normaleverymath", "normaleveryjob", "normaleveryhbox", "normaleveryeof", "normaleverydisplay", "normaleverycr", "normaletokspre", "normaletoksapp", "normalescapechar", "normalerrorstopmode", "normalerrorcontextlines", "normalerrmessage", "normalerrhelp", "normaleqno", "normalendlocalcontrol", "normalendlinechar", "normalendinput", "normalendgroup", "normalendcsname", "normalend", "normalemergencystretch", "normalelse", "normalefcode", "normaledef", "normaleTeXversion", "normaleTeXrevision", "normaleTeXminorversion", "normaleTeXVersion", "normaldvivariable", "normaldvifeedback", "normaldviextension", "normaldump", "normaldraftmode", "normaldp", "normaldoublehyphendemerits", "normaldivide", "normaldisplaywidth", "normaldisplaywidowpenalty", "normaldisplaywidowpenalties", "normaldisplaystyle", "normaldisplaylimits", "normaldisplayindent", "normaldiscretionary", "normaldirectlua", "normaldimexpr", "normaldimendef", "normaldimen", "normaldeviate", "normaldetokenize", "normaldelimitershortfall", "normaldelimiterfactor", "normaldelimiter", "normaldelcode", "normaldefaultskewchar", "normaldefaulthyphenchar", "normaldef", "normaldeadcycles", "normalday", "normalcurrentiftype", "normalcurrentiflevel", "normalcurrentifbranch", "normalcurrentgrouptype", "normalcurrentgrouplevel", "normalcsstring", "normalcsname", "normalcrcr", "normalcrampedtextstyle", "normalcrampedscriptstyle", "normalcrampedscriptscriptstyle", "normalcrampeddisplaystyle", "normalcr", "normalcountdef", "normalcount", "normalcopyfont", "normalcopy", "normalcompoundhyphenmode", "normalclubpenalty", "normalclubpenalties", "normalcloseout", "normalclosein", "normalclearmarks", "normalcleaders", "normalchardef", "normalchar", "normalcatcodetable", "normalcatcode", "normalbrokenpenalty", "normalbreakafterdirmode", "normalboxmaxdepth", "normalboxdirection", "normalboxdir", "normalbox", "normalboundary", "normalbotmarks", "normalbotmark", "normalbodydirection", "normalbodydir", "normalbinoppenalty", "normalbelowdisplayskip", "normalbelowdisplayshortskip", "normalbegingroup", "normalbegincsname", "normalbatchmode", "normalbadness", "normalautomatichyphenpenalty", "normalautomatichyphenmode", "normalautomaticdiscretionary", "normalattributedef", "normalattribute", "normalatopwithdelims", "normalatop", "normalaligntab", "normalalignmark", "normalaftergroup", "normalafterassignment", "normaladvance", "normaladjustspacing", "normaladjdemerits", "normalaccent", "normalabovewithdelims", "normalabovedisplayskip", "normalabovedisplayshortskip", "normalabove", "normalXeTeXversion", "normalUvextensible", "normalUunderdelimiter", "normalUsuperscript", "normalUsubscript", "normalUstopmath", "normalUstopdisplaymath", "normalUstartmath", "normalUstartdisplaymath", "normalUstack", "normalUskewedwithdelims", "normalUskewed", "normalUroot", "normalUright", "normalUradical", "normalUoverdelimiter", "normalUnosuperscript", "normalUnosubscript", "normalUmiddle", "normalUmathunderdelimitervgap", "normalUmathunderdelimiterbgap", "normalUmathunderbarvgap", "normalUmathunderbarrule", "normalUmathunderbarkern", "normalUmathsupsubbottommax", "normalUmathsupshiftup", "normalUmathsupshiftdrop", "normalUmathsupbottommin", "normalUmathsubtopmax", "normalUmathsubsupvgap", "normalUmathsubsupshiftdown", "normalUmathsubshiftdrop", "normalUmathsubshiftdown", "normalUmathstackvgap", "normalUmathstacknumup", "normalUmathstackdenomdown", "normalUmathspaceafterscript", "normalUmathskewedfractionvgap", "normalUmathskewedfractionhgap", "normalUmathrelrelspacing", "normalUmathrelpunctspacing", "normalUmathrelordspacing", "normalUmathrelopspacing", "normalUmathrelopenspacing", "normalUmathrelinnerspacing", "normalUmathrelclosespacing", "normalUmathrelbinspacing", "normalUmathradicalvgap", "normalUmathradicalrule", "normalUmathradicalkern", "normalUmathradicaldegreeraise", "normalUmathradicaldegreebefore", "normalUmathradicaldegreeafter", "normalUmathquad", "normalUmathpunctrelspacing", "normalUmathpunctpunctspacing", "normalUmathpunctordspacing", "normalUmathpunctopspacing", "normalUmathpunctopenspacing", "normalUmathpunctinnerspacing", "normalUmathpunctclosespacing", "normalUmathpunctbinspacing", "normalUmathoverdelimitervgap", "normalUmathoverdelimiterbgap", "normalUmathoverbarvgap", "normalUmathoverbarrule", "normalUmathoverbarkern", "normalUmathordrelspacing", "normalUmathordpunctspacing", "normalUmathordordspacing", "normalUmathordopspacing", "normalUmathordopenspacing", "normalUmathordinnerspacing", "normalUmathordclosespacing", "normalUmathordbinspacing", "normalUmathoprelspacing", "normalUmathoppunctspacing", "normalUmathopordspacing", "normalUmathopopspacing", "normalUmathopopenspacing", "normalUmathopinnerspacing", "normalUmathoperatorsize", "normalUmathopenrelspacing", "normalUmathopenpunctspacing", "normalUmathopenordspacing", "normalUmathopenopspacing", "normalUmathopenopenspacing", "normalUmathopeninnerspacing", "normalUmathopenclosespacing", "normalUmathopenbinspacing", "normalUmathopclosespacing", "normalUmathopbinspacing", "normalUmathnolimitsupfactor", "normalUmathnolimitsubfactor", "normalUmathlimitbelowvgap", "normalUmathlimitbelowkern", "normalUmathlimitbelowbgap", "normalUmathlimitabovevgap", "normalUmathlimitabovekern", "normalUmathlimitabovebgap", "normalUmathinnerrelspacing", "normalUmathinnerpunctspacing", "normalUmathinnerordspacing", "normalUmathinneropspacing", "normalUmathinneropenspacing", "normalUmathinnerinnerspacing", "normalUmathinnerclosespacing", "normalUmathinnerbinspacing", "normalUmathfractionrule", "normalUmathfractionnumvgap", "normalUmathfractionnumup", "normalUmathfractiondenomvgap", "normalUmathfractiondenomdown", "normalUmathfractiondelsize", "normalUmathconnectoroverlapmin", "normalUmathcodenum", "normalUmathcode", "normalUmathcloserelspacing", "normalUmathclosepunctspacing", "normalUmathcloseordspacing", "normalUmathcloseopspacing", "normalUmathcloseopenspacing", "normalUmathcloseinnerspacing", "normalUmathcloseclosespacing", "normalUmathclosebinspacing", "normalUmathcharslot", "normalUmathcharnumdef", "normalUmathcharnum", "normalUmathcharfam", "normalUmathchardef", "normalUmathcharclass", "normalUmathchar", "normalUmathbinrelspacing", "normalUmathbinpunctspacing", "normalUmathbinordspacing", "normalUmathbinopspacing", "normalUmathbinopenspacing", "normalUmathbininnerspacing", "normalUmathbinclosespacing", "normalUmathbinbinspacing", "normalUmathaxis", "normalUmathaccent", "normalUleft", "normalUhextensible", "normalUdelimiterunder", "normalUdelimiterover", "normalUdelimiter", "normalUdelcodenum", "normalUdelcode", "normalUchar", "normalOmegaversion", "normalOmegarevision", "normalOmegaminorversion", "normalAlephversion", "normalAlephrevision", "normalAlephminorversion", "normal ", "nonstopmode", "nonscript", "nolimits", "noligs", "nokerns", "noindent", "nohrule", "noexpand", "noboundary", "noalign", "newlinechar", "mutoglue", "muskipdef", "muskip", "multiply", "muexpr", "mskip", "moveright", "moveleft", "month", "mkern", "middle", "message", "medmuskip", "meaning", "maxdepth", "maxdeadcycles", "mathsurroundskip", "mathsurroundmode", "mathsurround", "mathstyle", "mathscriptsmode", "mathscriptcharmode", "mathscriptboxmode", "mathrulethicknessmode", "mathrulesmode", "mathrulesfam", "mathrel", "mathpunct", "mathpenaltiesmode", "mathord", "mathoption", "mathopen", "mathop", "mathnolimitsmode", "mathitalicsmode", "mathinner", "mathflattenmode", "matheqnogapstep", "mathdisplayskipmode", "mathdirection", "mathdir", "mathdelimitersmode", "mathcode", "mathclose", "mathchoice", "mathchardef", "mathchar", "mathbin", "mathaccent", "marks", "mark", "mag", "luatexversion", "luatexrevision", "luatexbanner", "luafunctioncall", "luafunction", "luaescapestring", "luadef", "luacopyinputnodes", "luabytecodecall", "luabytecode", "lpcode", "lowercase", "lower", "looseness", "long", "localrightbox", "localleftbox", "localinterlinepenalty", "localbrokenpenalty", "lineskiplimit", "lineskip", "linepenalty", "linedirection", "linedir", "limits", "letterspacefont", "letcharcode", "let", "leqno", "leftskip", "leftmarginkern", "lefthyphenmin", "leftghost", "left", "leaders", "lccode", "lateluafunction", "latelua", "lastypos", "lastxpos", "lastskip", "lastsavedimageresourcepages", "lastsavedimageresourceindex", "lastsavedboxresourceindex", "lastpenalty", "lastnodetype", "lastnamedcs", "lastlinefit", "lastkern", "lastbox", "language", "kern", "jobname", "interlinepenalty", "interlinepenalties", "interactionmode", "insertpenalties", "insertht", "insert", "inputlineno", "input", "initcatcodetable", "indent", "immediateassignment", "immediateassigned", "immediate", "ignorespaces", "ignoreligaturesinfont", "ifx", "ifvoid", "ifvmode", "ifvbox", "iftrue", "ifprimitive", "ifpdfprimitive", "ifpdfabsnum", "ifpdfabsdim", "ifodd", "ifnum", "ifmmode", "ifinner", "ifincsname", "ifhmode", "ifhbox", "iffontchar", "iffalse", "ifeof", "ifdim", "ifdefined", "ifcsname", "ifcondition", "ifcat", "ifcase", "ifabsnum", "ifabsdim", "if", "hyphenpenaltymode", "hyphenpenalty", "hyphenchar", "hyphenationmin", "hyphenationbounds", "hyphenation", "ht", "hss", "hskip", "hsize", "hrule", "hpack", "holdinginserts", "hoffset", "hjcode", "hfuzz", "hfilneg", "hfill", "hfil", "hbox", "hbadness", "hangindent", "hangafter", "halign", "gtokspre", "gtoksapp", "gluetomu", "gluestretchorder", "gluestretch", "glueshrinkorder", "glueshrink", "glueexpr", "globaldefs", "global", "gleaders", "gdef", "futurelet", "futureexpandis", "futureexpand", "formatname", "fontname", "fontid", "fontdimen", "fontcharwd", "fontcharic", "fontcharht", "fontchardp", "font", "floatingpenalty", "fixupboxesmode", "firstvalidlanguage", "firstmarks", "firstmark", "finalhyphendemerits", "fi", "fam", "explicithyphenpenalty", "explicitdiscretionary", "expandglyphsinfont", "expandafter", "exhyphenpenalty", "exhyphenchar", "exceptionpenalty", "everyvbox", "everypar", "everymath", "everyjob", "everyhbox", "everyeof", "everydisplay", "everycr", "etokspre", "etoksapp", "escapechar", "errorstopmode", "errorcontextlines", "errmessage", "errhelp", "eqno", "endlocalcontrol", "endlinechar", "endinput", "endgroup", "endcsname", "end", "emergencystretch", "else", "efcode", "edef", "eTeXversion", "eTeXrevision", "eTeXminorversion", "eTeXVersion", "dvivariable", "dvifeedback", "dviextension", "dump", "draftmode", "dp", "doublehyphendemerits", "divide", "displaywidth", "displaywidowpenalty", "displaywidowpenalties", "displaystyle", "displaylimits", "displayindent", "discretionary", "directlua", "dimexpr", "dimendef", "dimen", "detokenize", "delimitershortfall", "delimiterfactor", "delimiter", "delcode", "defaultskewchar", "defaulthyphenchar", "def", "deadcycles", "day", "currentiftype", "currentiflevel", "currentifbranch", "currentgrouptype", "currentgrouplevel", "csstring", "csname", "crcr", "crampedtextstyle", "crampedscriptstyle", "crampedscriptscriptstyle", "crampeddisplaystyle", "cr", "countdef", "count", "copyfont", "copy", "compoundhyphenmode", "clubpenalty", "clubpenalties", "closeout", "closein", "clearmarks", "cleaders", "chardef", "char", "catcodetable", "catcode", "brokenpenalty", "breakafterdirmode", "boxmaxdepth", "boxdirection", "boxdir", "box", "boundary", "botmarks", "botmark", "bodydirection", "bodydir", "binoppenalty", "belowdisplayskip", "belowdisplayshortskip", "begingroup", "begincsname", "batchmode", "baselineskip", "badness", "automatichyphenpenalty", "automatichyphenmode", "automaticdiscretionary", "attributedef", "attribute", "atopwithdelims", "atop", "aligntab", "alignmark", "aftergroup", "afterassignment", "advance", "adjustspacing", "adjdemerits", "accent", "abovewithdelims", "abovedisplayskip", "abovedisplayshortskip", "above", "XeTeXversion", "Uvextensible", "Uunderdelimiter", "Usuperscript", "Usubscript", "Ustopmath", "Ustopdisplaymath", "Ustartmath", "Ustartdisplaymath", "Ustack", "Uskewedwithdelims", "Uskewed", "Uroot", "Uright", "Uradical", "Uoverdelimiter", "Unosuperscript", "Unosubscript", "Umiddle", "Umathunderdelimitervgap", "Umathunderdelimiterbgap", "Umathunderbarvgap", "Umathunderbarrule", "Umathunderbarkern", "Umathsupsubbottommax", "Umathsupshiftup", "Umathsupshiftdrop", "Umathsupbottommin", "Umathsubtopmax", "Umathsubsupvgap", "Umathsubsupshiftdown", "Umathsubshiftdrop", "Umathsubshiftdown", "Umathstackvgap", "Umathstacknumup", "Umathstackdenomdown", "Umathspaceafterscript", "Umathskewedfractionvgap", "Umathskewedfractionhgap", "Umathrelrelspacing", "Umathrelpunctspacing", "Umathrelordspacing", "Umathrelopspacing", "Umathrelopenspacing", "Umathrelinnerspacing", "Umathrelclosespacing", "Umathrelbinspacing", "Umathradicalvgap", "Umathradicalrule", "Umathradicalkern", "Umathradicaldegreeraise", "Umathradicaldegreebefore", "Umathradicaldegreeafter", "Umathquad", "Umathpunctrelspacing", "Umathpunctpunctspacing", "Umathpunctordspacing", "Umathpunctopspacing", "Umathpunctopenspacing", "Umathpunctinnerspacing", "Umathpunctclosespacing", "Umathpunctbinspacing", "Umathoverdelimitervgap", "Umathoverdelimiterbgap", "Umathoverbarvgap", "Umathoverbarrule", "Umathoverbarkern", "Umathordrelspacing", "Umathordpunctspacing", "Umathordordspacing", "Umathordopspacing", "Umathordopenspacing", "Umathordinnerspacing", "Umathordclosespacing", "Umathordbinspacing", "Umathoprelspacing", "Umathoppunctspacing", "Umathopordspacing", "Umathopopspacing", "Umathopopenspacing", "Umathopinnerspacing", "Umathoperatorsize", "Umathopenrelspacing", "Umathopenpunctspacing", "Umathopenordspacing", "Umathopenopspacing", "Umathopenopenspacing", "Umathopeninnerspacing", "Umathopenclosespacing", "Umathopenbinspacing", "Umathopclosespacing", "Umathopbinspacing", "Umathnolimitsupfactor", "Umathnolimitsubfactor", "Umathlimitbelowvgap", "Umathlimitbelowkern", "Umathlimitbelowbgap", "Umathlimitabovevgap", "Umathlimitabovekern", "Umathlimitabovebgap", "Umathinnerrelspacing", "Umathinnerpunctspacing", "Umathinnerordspacing", "Umathinneropspacing", "Umathinneropenspacing", "Umathinnerinnerspacing", "Umathinnerclosespacing", "Umathinnerbinspacing", "Umathfractionrule", "Umathfractionnumvgap", "Umathfractionnumup", "Umathfractiondenomvgap", "Umathfractiondenomdown", "Umathfractiondelsize", "Umathconnectoroverlapmin", "Umathcodenum", "Umathcode", "Umathcloserelspacing", "Umathclosepunctspacing", "Umathcloseordspacing", "Umathcloseopspacing", "Umathcloseopenspacing", "Umathcloseinnerspacing", "Umathcloseclosespacing", "Umathclosebinspacing", "Umathcharslot", "Umathcharnumdef", "Umathcharnum", "Umathcharfam", "Umathchardef", "Umathcharclass", "Umathchar", "Umathbinrelspacing", "Umathbinpunctspacing", "Umathbinordspacing", "Umathbinopspacing", "Umathbinopenspacing", "Umathbininnerspacing", "Umathbinclosespacing", "Umathbinbinspacing", "Umathaxis", "Umathaccent", "Uleft", "Uhextensible", "Udelimiterunder", "Udelimiterover", "Udelimiter", "Udelcodenum", "Udelcode", "Uchar", "Omegaversion", "Omegarevision", "Omegaminorversion", "Alephversion", "Alephrevision", "Alephminorversion" };
                var listprimitives = primitives.Select(array => new CompletionItem(@"\" + array, array, CompletionItemKind.EnumMember) { Documentation = new IMarkdownString("TeX Primitive"), Detail = "Primitive", InsertTextRules = CompletionItemInsertTextRule.KeepWhitespace }).ToList();

                List<CompletionItem> ls = new List<CompletionItem>();

                if (App.VM.Default.SuggestStartStop)
                {
                    ls.AddRange(list);
                    ls.AddRange(listsection);
                }
                if (App.VM.Default.SuggestCommands)
                    ls.AddRange(listcommands);
                if (App.VM.Default.SuggestPrimitives)
                    ls.AddRange(listprimitives);

                if (context.TriggerKind == CompletionTriggerKind.TriggerCharacter)
                {
                    return new CompletionList()
                    {
                        Suggestions = ls.ToArray()
                    };
                }
                return new CompletionList();
            });
        }

        public IAsyncOperation<CompletionItem> ResolveCompletionItemAsync(IModel document, Position position, CompletionItem item)
        {
            return AsyncInfo.Run(delegate (CancellationToken cancelationToken)
            {
                return Task.FromResult(item); // throw new NotImplementedException();
            });
        }
    }

    public class RichTextBlockHelper : DependencyObject
    {
        // Using a DependencyProperty as the backing store for Text.
        //This enables animation, styling, binding, etc...
        public static readonly DependencyProperty BlocksProperty =
            DependencyProperty.RegisterAttached("Text", typeof(string),
            typeof(RichTextBlockHelper),
            new PropertyMetadata(String.Empty, OnTextChanged));

        private static int logline = 0;

        public static string GetText(DependencyObject obj)
        {
            return (string)obj.GetValue(BlocksProperty);
        }

        public static Paragraph LOG(string log)
        {
            logline++;
            Paragraph paragraph = new Paragraph();
            Run run1 = new Run
            {
                Text = $"{string.Format("{0,3:###}", logline)} [{DateTime.Now.ToString("HH:mm:ss")}] : "
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

        public static void SetText(DependencyObject obj, string value)
        {
            obj.SetValue(BlocksProperty, value);
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
    }

    internal class EditorHoverProvider : HoverProvider
    {
        public IAsyncOperation<Hover> ProvideHover(IModel model, Position position)
        {
            string[] startsection = new string[] { "startsection", "startsubsection", "startsubsubsection", "startsubsubsubsection", "starsubject", "startsubsubject", "startsubsubsubject", "startsubsubsubsubject", "startpart", "starttitle", "startchapter", };
            string[] startfrontmatter = new string[] { "startfrontmatter", "startbodymatter", "startappendices", "startbackmatter", };
            return AsyncInfo.Run(async delegate (CancellationToken cancelationToken)
            {
                var word = await model.GetWordAtPositionAsync(position);
                if (word != null && word.Word != null)
                {
                    foreach (string keyword in startsection)
                    {
                        if (word.Word == keyword)
                        {
                            return new Hover(new string[]
                            {
                                $"**\\{keyword}**" + " [title={TEXT}, list={TEXT}, bookmark={TEXT}, marking={TEXT}, reference=sec:NAME]" + " *\\[...=...\\]*",
                                "**...**",
                                $"**\\{keyword.Replace("start", "stop")}**",
                                "Sectioning structure. In the second option declared variables can be used inside the structure with \\structureuservariable{varNAME}.",
                                "ConTeXtGarden wiki page: [Titles](https://www.contextgarden.net/Titles).",
                                $"ConTeXtGarden command reference: [Command/{keyword}](https://www.contextgarden.net/Command/{keyword})."
                            }, new Range(position.LineNumber, position.Column, position.LineNumber, position.Column + 5));
                        }
                    }
                    foreach (string keyword in startfrontmatter)
                    {
                        if (word.Word == keyword)
                        {
                            return new Hover(new string[]
                            {
                                $"**\\{keyword}**" + " *\\[bookmark={TEXT}\\]*",
                                "**...**",
                                $"**\\{keyword.Replace("start", "stop")}**",
                                "One of the four standard section block environments: frontmatter, bodymatter, appendices, backmatter",
                                "The corresponding internal section block NAMEs are: frontpart, bodypart, appendix, backpart",
                                "Can be manipulated with \\setupsectionblock[NAME][] and \\startsectionblockenvironment[NAME]...\\stopsectionblockenvironment",
                                "ConTeXtGarden wiki page: [Modes#System modes](https://wiki.contextgarden.net/Modes#System_modes).",
                            }, new Range(position.LineNumber, position.Column, position.LineNumber, position.Column + 5));
                        }
                    }
                }

                return default(Hover);
            });
        }
    }

    internal class RunAction : IActionDescriptor
    {
        public string ContextMenuGroupId => "compile";
        public float ContextMenuOrder => 1.5f;
        public string Id => "compile";
        public string KeybindingContext => null;

        //public int[] Keybindings => new int[] { Monaco.KeyMod.Chord(Monaco.KeyMod.CtrlCmd | Monaco.KeyCode.Enter, Monaco.KeyCode.F5) };
        public int[] Keybindings => new int[] { Monaco.KeyMod.CtrlCmd | Monaco.KeyCode.Enter };

        public string Label => "Save & Compile";
        public string Precondition => null;

        public async void Run(CodeEditor editor, object[] obj)
        {
            await App.VM.UWPSave();
            //await (((Window.Current.Content as Frame).Content as MainPage).contentFrame.Content as Editor).Compile();
            await Editor.CurrentEditor.Compile();
            editor.Focus(Windows.UI.Xaml.FocusState.Programmatic);
        }
    }

    internal class RunRootAction : IActionDescriptor
    {
        public string ContextMenuGroupId => "compile";
        public float ContextMenuOrder => 1.5f;
        public string Id => "compileroot";
        public string KeybindingContext => null;

        //public int[] Keybindings => new int[] { Monaco.KeyMod.Chord(Monaco.KeyMod.CtrlCmd | Monaco.KeyCode.Enter, Monaco.KeyCode.F5) };
        public int[] Keybindings => new int[] { KeyMod.CtrlCmd | KeyMod.Shift | KeyCode.Enter };

        public string Label => "Save all & Compile root";
        public string Precondition => null;

        public async void Run(CodeEditor editor, object[] obj)
        {
            await App.VM.UWPSaveAll();
            //await (((Window.Current.Content as Frame).Content as MainPage).contentFrame.Content as Editor).Compile();
            await Editor.CurrentEditor.Compile(true);
            editor.Focus(Windows.UI.Xaml.FocusState.Programmatic);
        }
    }

    internal class FileOutlineAction : IActionDescriptor
    {
        public string ContextMenuGroupId => "outline";
        public float ContextMenuOrder => 1.5f;
        public string Id => "fileoutline";
        public string KeybindingContext => null;

        //public int[] Keybindings => new int[] { Monaco.KeyMod.Chord(Monaco.KeyMod.CtrlCmd | Monaco.KeyCode.Enter, Monaco.KeyCode.F5) };
        public int[] Keybindings => new int[] { Monaco.KeyMod.CtrlCmd | Monaco.KeyCode.F2 };

        public string Label => "File Outline";
        public string Precondition => null;

        public async void Run(CodeEditor editor, object[] obj)
        {
            try
            {
                //var list = await editor.GetModel().FindMatchesAsync("startsection", false, false, false, "", true);
                //var list = await findMatches(editor, "startsection", false, false, false, "", true);
                var list = await editor.FindMatchesAsync(@"(\\start(sub)*?(section|subject|part|chapter|title)\s*?\[\s*?)(title\s*?=\s*?\{?)(.*?)\}?\s*?([,\]])", false, true, false, null, true, 20);
                App.VM.LOG((list != null).ToString());
                foreach (var i in list)
                {
                    App.VM.LOG(string.Join(" | ",i.Matches));
                    App.VM.LOG(i.Range.GetStartPosition().LineNumber.ToString());
                }
               
                    }
            catch (Exception ex)
            {
                App.VM.LOG("FileOutlineAction Error: " + ex.Message);
            }
            editor.Focus(Windows.UI.Xaml.FocusState.Programmatic);
        }

        public async Task<FindMatch[]> findMatches(CodeEditor editor, string searchString, bool searchOnlyEditableRange, bool isRegex, bool matchCase, string wordSeparators, bool captureMatches)
        {
           return await editor._view.RunScriptHelperTaskAsync<FindMatch[]>("model.findMatches(" + searchString + ", " + searchOnlyEditableRange.ToString() + ", " + isRegex.ToString() + ", " + matchCase.ToString() + ", " + wordSeparators + ", " + captureMatches.ToString() + ", " + "20" +  ");");
        }
    }
    internal static class WebViewExtensions
    {
        public static async Task<T> RunScriptHelperTaskAsync<T>(this WebView _view, string script)
        {

            var start = "try {\n";
            if (typeof(T) != typeof(object))
            {
                script = script.Trim(';');
                start += "JSON.stringify(" + script + ");";
            }
            else
            {
                start += script;
            }
            var fullscript = start +
                "\n} catch (err) { JSON.stringify({ wv_internal_error: true, message: err.message, description: err.description, number: err.number, stack: err.stack }); }";
            var returnstring = await _view.InvokeScriptAsync("eval", new string[] { fullscript });
            App.VM.LOG("ReturnString: "+returnstring);
            if (JsonObject.TryParse(returnstring, out JsonObject result))
            {
                if (result.ContainsKey("wv_internal_error") && result["wv_internal_error"].ValueType == JsonValueType.Boolean && result["wv_internal_error"].GetBoolean())
                {
                    //throw new JavaScriptInnerException(result["message"].GetString(), result["stack"].GetString());
                    App.VM.LOG("Message: "+result["message"].GetString() + " Stack: " + result["stack"].GetString());
                }
            }

            if (returnstring != null && returnstring != "null")
            {
                if (typeof(T) == typeof(List<FindMatch>))
                {
                    var list = JsonConvert.DeserializeObject<FindMatch[]>(returnstring).ToList();
                    return (T)(object)list;
                }
                return JsonConvert.DeserializeObject<T>(returnstring);
            }

            return default;
        }

    }

    internal class SaveAction : IActionDescriptor
    {
        public string ContextMenuGroupId => "save";
        public float ContextMenuOrder => 1.5f;
        public string Id => "save";
        public string KeybindingContext => null;

        //public int[] Keybindings => new int[] { Monaco.KeyMod.Chord(Monaco.KeyMod.CtrlCmd | Monaco.KeyCode.Enter, Monaco.KeyCode.F5) };
        public int[] Keybindings => new int[] { Monaco.KeyMod.CtrlCmd | Monaco.KeyCode.KEY_S };

        public string Label => "Save";
        public string Precondition => null;

        public async void Run(CodeEditor editor, object[] obj)
        {
            await App.VM.UWPSave();
            editor.Focus(Windows.UI.Xaml.FocusState.Programmatic);
        }
    }

    internal class SaveAllAction : IActionDescriptor
    {
        public string ContextMenuGroupId => "save";
        public float ContextMenuOrder => 1.5f;
        public string Id => "saveall";
        public string KeybindingContext => null;

        //public int[] Keybindings => new int[] { Monaco.KeyMod.Chord(Monaco.KeyMod.CtrlCmd | Monaco.KeyCode.Enter, Monaco.KeyCode.F5) };
        public int[] Keybindings => new int[] { KeyMod.CtrlCmd | KeyCode.Shift | KeyCode.KEY_S };

        public string Label => "Save all";
        public string Precondition => null;

        public async void Run(CodeEditor editor, object[] obj)
        {
            await App.VM.UWPSaveAll();
            editor.Focus(Windows.UI.Xaml.FocusState.Programmatic);
        }
    }
}