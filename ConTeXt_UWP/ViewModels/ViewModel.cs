using ConTeXt_UWP.Helpers;
using ConTeXt_UWP.Models;
using Monaco.Editor;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Threading.Tasks;
using Windows.ApplicationModel.Activation;
using Windows.ApplicationModel.AppService;
using Windows.ApplicationModel.Background;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.Storage.AccessCache;
using Windows.UI.Xaml.Controls;

namespace ConTeXt_UWP.ViewModels
{
    public class ViewModel : Bindable
    {
        public ObservableCollection<FileActivatedEventArgs> FileActivatedEvents = new ObservableCollection<FileActivatedEventArgs>() { };

        private readonly List<string> cancelWords = new List<string> { ".gitignore", ".tuc", ".log", ".pgf", ".pdf" };

        public ObservableCollection<FileItem> FileItemsTree = new ObservableCollection<FileItem>();

        private string rootFile;

        //public CodeEditor CurrentEditor { get => Get<CodeEditor>(null); set => Set(value); }

        public ViewModel()
        {
            try
            {
                //Default = Settings.Default;
                FileItems = new ObservableCollection<FileItem>();
                CurrentFileItem = FileItems.Count > 0 ? FileItems.FirstOrDefault() : new FileItem(null);
                
                FileItems.CollectionChanged += FileItems_CollectionChanged1;

                //var ce = new CodeEditor();
                //EditorOptions = ce.Options;
                EditorOptions.DetectIndentation = false;
                EditorOptions.UseTabStops = true;
                EditorOptions.TabSize = 2;
                EditorOptions.CopyWithSyntaxHighlighting = true;
                EditorOptions.InsertSpaces = false;
                EditorOptions.RoundedSelection = true;
                EditorOptions.WordWrap = "on";
                EditorOptions.WordBasedSuggestions = false;
                EditorOptions.SuggestOnTriggerCharacters = true;
                EditorOptions.AcceptSuggestionOnCommitCharacter = true;
                EditorOptions.SuggestSelection = "recentlyUsed";
                EditorOptions.WrappingIndent = "indent";
                EditorOptions.AutoIndent = "keep";
                EditorOptions.CodeLens = true;
                EditorOptions.Contextmenu = true;
                EditorOptions.ParameterHints = new IEditorParameterHintOptions() { Cycle = false, Enabled = true };
                //EditorOptions.Minimap = new EditorMinimapOptions() { Enabled = Default.MiniMap, ShowSlider = "always", RenderCharacters = true, };
                EditorOptions.CursorBlinking = "solid";
                EditorOptions.DragAndDrop = true;
                EditorOptions.ScrollBeyondLastLine = false;
                //EditorOptions.Folding = Default.CodeFolding;
                EditorOptions.FoldingStrategy = "auto";
                EditorOptions.FormatOnPaste = true;
                //EditorOptions.Hover = new EditorHoverOptions() { Enabled = Default.Hover, Delay = 100, Sticky = true };
                //EditorOptions.LineNumbers = Default.ShowLineNumbers;
                EditorOptions.RenderControlCharacters = true;
                EditorOptions.QuickSuggestions = true;
                EditorOptions.SnippetSuggestions = "inline";
                EditorOptions.Links = true;
                EditorOptions.MouseWheelZoom = true;
                EditorOptions.OccurrencesHighlight = false;
                EditorOptions.RoundedSelection = true;
            }
            catch (TypeInitializationException ex)
            {
                Message("TypeInitializationException" + ex.InnerException.Message);
            }
            catch (Exception ex)
            {
                Message("Exception" + ex.Message);
            }
        }

        public AppServiceConnection appServiceConnection { get; set; }

        public BackgroundTaskDeferral AppServiceDeferral { get; set; }

        public string Blocks { get => Get<string>(); set => Set(value); }

        public ConTeXtErrorMessage ConTeXtErrorMessage { get => Get(new ConTeXtErrorMessage()); set => Set(value); }

        public FileItem CurrentFileItem { get => Get(new FileItem(null)); set => Set(value); }

        public Project CurrentProject { get => Get(new Project()); set { Set(value); IsProjectLoaded = value?.Folder != null; if (value?.Folder != null) LOG("Project "+value.Name+" loaded."); } }

        public Settings Default { get; set; }

        public StandaloneEditorConstructionOptions EditorOptions { get => Get(new StandaloneEditorConstructionOptions()); set => Set(value); }

        public ObservableCollection<FileItem> FileItems { get => Get(new ObservableCollection<FileItem>()); set => Set(value); }

        public ObservableCollection<Helpfile> HelpFiles { get; } = new ObservableCollection<Helpfile>() {
            new Helpfile() { FriendlyName = "Manual", FileName = "ma-cb-en.pdf", Path = @"\tex\texmf-context\doc\context\documents\general\manuals\" },
            new Helpfile() { FriendlyName = "Commands", FileName = "setup-en.pdf", Path = @"\tex\texmf-context\doc\context\documents\general\qrcs\" },
        };

        public ObservableCollection<HelpItem> HelpItems { get; set; }

        //private ObservableCollection<HelpItem> PopulateHelpItems()
        //{
        //    var helpItems = Default.HelpItemList;
        //    var modes = helpItems[helpItems.FindIndex(x => x.ID == "Modes")]; modes.Title = "Compiler Modes"; modes.Text = "Select any number of modes. They will activate the corresponding \n'\\startmode[<ModeName>] ... \\stopmode\"\n environments.";
        //    return new ObservableCollection<HelpItem>(helpItems);
        //}

        public bool IsError { get => Get(false); set => Set(value); }

        //public bool Modes { get => IsProjectLoaded && Default.UseModes; set => Set(value); }

        public bool IsFileItemLoaded { get => Get(false); set { Set(value); if (value) { IsVisible = false; } else { IsVisible = false; } } }

        public bool IsPaused { get => Get(false); set { Set(value); } }

        public bool Started { get => Get(false); set { Set(value); } }

        public bool IsInstalled { get => Get(false); set { Set(value); } }

        public bool IsProjectLoaded { get => Get(false); set => Set(value); }

        public bool IsSaving { get => Get(false); set { Set(value); if (value) { IsVisible = true; } if (!value && !IsError) { IsVisible = false; } } }

        public bool IsVisible { get => Get(false); set => Set(value); }

        public string NVHead { get => Get(""); set => Set(value); }

        public StorageItemMostRecentlyUsedList RecentAccessList { get => Get<StorageItemMostRecentlyUsedList>(); set => Set(value); }

        //public ObservableCollection<KeyValuePair<string, string>> Helpfile { get; set; }
        public string SelectedPath { get => Get(""); set => Set(value); }

        //public KeyValuePair<string, string> Help
        //{
        //    get { return this.help; }
        //    set { this.help = value; this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Help")); }
        //}
        public void GenerateTreeView(StorageFolder folder, string rootfile = null)
        {
            rootFile = rootfile;
            
            if (folder != null)
            {
                FileItemsTree.Add(new FileItem(folder) { IsExpanded = true, Type = FileItem.ExplorerItemType.ProjectRootFolder });
                DirWalk(folder);
            }
            else
            {
                LOG("Operation cancelled.");
            }
        }
        private async void DirWalk(StorageFolder sDir, FileItem currFolder = null, int level = 0)
        {
            try
            {
                var folders = await sDir.GetFoldersAsync();
                var files = await sDir.GetFilesAsync();
                foreach (StorageFolder d in folders)
                {
                    if (!d.Name.StartsWith("."))
                    {
                        var SubFolder = new FileItem(d) { Type = FileItem.ExplorerItemType.Folder, FileName = d.Name, IsRoot = false };
                        if (level > 0)
                            currFolder.Children.Add(SubFolder);
                        else
                            FileItemsTree[0].Children.Add(SubFolder);
                        DirWalk(d, SubFolder, level + 1);
                    }
                }
                foreach (StorageFile f in files)
                {
                    if (!f.Name.StartsWith(".") && !cancelWords.Contains(f.FileType))
                    {
                        var fi = new FileItem(f) { File = f, Type = FileItem.ExplorerItemType.File, FileName = f.Name, IsRoot = false };
                        if (level > 0)
                            currFolder.Children.Add(fi);
                        else
                        {
                            fi.IsRoot = fi.FileName == rootFile;
                            FileItemsTree[0].Children.Add(fi);
                        }

                        //   App.VM.LOG("added "+fi.FileName);
                    }
                }
            }
            catch (Exception excpt)
            {
                Message("Error in generating the directory tree: " + excpt.Message);
            }
        }

        public FileItem InitializeFileItem(StorageFile File, string Content = "", bool IsRoot = true)
        {
            return new FileItem(File, IsRoot) { FileContent = Content };
        }

        public async Task<bool> LOG(object log, string title = "Error:")
        {
            try
            {
                Blocks = log.ToString();
                await Task.Delay(200);
                return true;
            }
            catch
            {
                return false;
            }
            // await new MessageDialog(log).ShowAsync();

            // return await new ContentDialog() { Title = title, Content = (log).ToString(), PrimaryButtonText = "ok" }.ShowAsync();
        }

        public async void Message(string content, string title = "Error")
        {
            ContentDialog md = new ContentDialog() { Content = content, Title = title, PrimaryButtonText = "ok" };
            await md.ShowAsync();
        }

        public Dictionary<string, string> Meta(string m)
        {
            return m.Split(';').Select(x => x.Split('=')).ToDictionary(x => x[0], x => x[1]);
        }

        public async void OpenFile(FileItem File)
        {
            try
            {
                if (!FileItems.Contains(File))
                {
                    var openfile = FileItems.Where(x => x.File.Path == File.File.Path);

                    var read = await FileIO.ReadTextAsync((StorageFile)File.File);
                    File.LastSaveFileContent = read;

                    //await Task.Delay(500);

                    File.FileContent = read;

                    
                    if (openfile.Count() > 0)
                    {
                        if (CurrentFileItem == openfile.FirstOrDefault())
                        {
                            FileItems[FileItems.IndexOf(openfile.FirstOrDefault())] = File;
                            //CurrentFileItem = File;
                        }
                        else
                        {
                            FileItems[FileItems.IndexOf(openfile.FirstOrDefault())] = File;
                            //CurrentFileItem = File;
                        }
                    }
                    else
                        FileItems.Add(File);

                    await Task.Delay(500);
                    CurrentFileItem = File;
                    LOG("File " + File.FileName+" opened");
                }
                else
                {
                    CurrentFileItem = File;
                }
            }
            catch (Exception ex)
            {
                LOG("Cannot open selected file: " + ex.Message);
            }
        }

        public async Task Save()
        {
            if (!IsSaving)
            {
                IsSaving = true;
                //App.AppViewModel.LOG(Tabs.SelectedItem.GetType().ToString());

                //ViewModel.Default.CodeContent = fi.FileContent;
                var buffer = Windows.Security.Cryptography.CryptographicBuffer.ConvertStringToBinary(CurrentFileItem.FileContent, Windows.Security.Cryptography.BinaryStringEncoding.Utf8);
                var file = await ApplicationData.Current.LocalFolder.CreateFileAsync(CurrentFileItem.FileName, CreationCollisionOption.ReplaceExisting);
                await FileIO.WriteBufferAsync(file, buffer);
                Default.TexFileName = CurrentFileItem.FileName;
                Default.TexFilePath = CurrentFileItem.File.Path;

                LOG("Saving");
                ValueSet request = new ValueSet
                {
                    { "save", true }
                };
                AppServiceResponse response = await appServiceConnection.SendMessageAsync(request);
                // display the response key/value pairs
                if (response != null)
                    foreach (string key in response.Message.Keys)
                    {
                        if ((string)response.Message[key] == "response")
                        {
                            LOG(key + " = " + response.Message[key]); LOG("Saved on " + DateTime.Now);
                        }
                    }

                //await Task.Delay(2000);
                IsSaving = false;
            }
            else LOG("already saving...");
        }

        public async Task Startup()
        {
            try
            {
                //HelpItems = PopulateHelpItems();
                // UpdateRecentAccessList();
                if (Default.StartWithLastActiveProject && !string.IsNullOrWhiteSpace(Default.LastActiveProject))
                {
                    RecentAccessList = StorageApplicationPermissions.MostRecentlyUsedList;
                    if (RecentAccessList.ContainsItem(Default.LastActiveProject))
                    {
                        IsSaving = true;
                        var folder = await RecentAccessList.GetFolderAsync(Default.LastActiveProject);
                        //var f = RecentAccessList.Entries.Where(x => x.Token == folder.Name).FirstOrDefault();
                        var list = Default.ProjectList.Where(x => x.Name == folder.Name);
                        if (list != null && list.Count() == 1)
                        {
                            var project = list.FirstOrDefault();
                            project.Folder = folder;
                            FileItemsTree = new ObservableCollection<FileItem>();
                            GenerateTreeView(project.Folder, project.RootFile);
                            project.Directory = FileItemsTree;
                            CurrentProject = project;
                            
                            if (CurrentProject.RootFile != null)
                            {
                                await Task.Delay(500);
                                FileItem root = CurrentProject.Directory.Where(x => x.IsRoot == true).FirstOrDefault();
                                if (root != null)
                                    OpenFile(root);
                            }
                        }
                        //CurrentProject = new Project(folder.Name, folder, GenerateTreeView(folder));
                        //CurrentProject.RootFile = Meta(f.Metadata).ContainsKey("rootfile") ? Meta(f.Metadata)["rootfile"] : null;

                        //Message(GenerateTreeView(folder).Count.ToString());
                        // LOG(CurrentProject.Folder.Path);
                        //LOG(CurrentProject.Directory.Count.ToString());
                        //var fileitem = CurrentProject.Directory.Where(x => x.FileName == Default.LastActiveFileName).FirstOrDefault();
                        //OpenFile(fileitem);
                    }
                }
            }
            catch (Exception ex)
            {
                LOG("Error on ViewModel startup: " + ex.Message);
            }
            finally
            {
                IsSaving = false;
                Started = true;
            }
        }

        public async void UpdateMRUEntry(Project prj)
        {
            StorageApplicationPermissions.MostRecentlyUsedList.AddOrReplace(prj.Name, prj.Folder, "rootfile=" + prj.RootFile);
            var entry = StorageApplicationPermissions.MostRecentlyUsedList.Entries.Where(x => x.Token == prj.Name).FirstOrDefault();
            LOG("Updated" + entry.Token + entry.Metadata);
        }

        public async void UpdateRecentAccessList()
        {
            IsSaving = true;
            LOG("upd1");
            if (Default.ProjectList == null) Default.ProjectList = new ObservableCollection<Project>();
            if (Default.ProjectList.Count == 0)
            {
                RecentAccessList = StorageApplicationPermissions.MostRecentlyUsedList;
                var accesslist = RecentAccessList.Entries;
                Default.ProjectList.Clear();
                if (accesslist.Count() > 0)
                {
                    //Default.LastActiveProject = accesslist.FirstOrDefault().Token;
                    foreach (var accessitem in accesslist)
                    {
                        var folder = await RecentAccessList.GetFolderAsync(accessitem.Token);
                        //var tree = GenerateTreeView(folder);
                        Default.ProjectList.Add(new Project(folder.Name, folder));
                    }
                }
            }
            IsSaving = false;
        }

        public async Task UWPSave(FileItem fileItem = null)
        {
            FileItem filetosave = fileItem ?? CurrentFileItem;

            if (filetosave != null)
                if (!IsSaving && filetosave.File != null)
                {
                    try
                    {
                        if (filetosave.IsChanged)
                        {
                            IsSaving = true;
                            IsPaused = false;
                            string cont = filetosave.FileContent ?? " ";
                            var buffer = Windows.Security.Cryptography.CryptographicBuffer.ConvertStringToBinary(cont, Windows.Security.Cryptography.BinaryStringEncoding.Utf8);
                            await FileIO.WriteBufferAsync(CurrentFileItem.File as StorageFile, buffer);
                            Default.TexFileName = filetosave.FileName;
                            Default.TexFilePath = filetosave.File.Path;
                            Default.TexFileFolder = filetosave.FileFolder;
                            filetosave.LastSaveFileContent = filetosave.FileContent;
                            filetosave.IsChanged = false;
                            IsSaving = false;
                            IsPaused = true;
                            IsVisible = false;
                            LOG("File " + filetosave.File.Name + " saved");
                        }

                    }
                    catch (Exception ex)
                    {
                        IsError = true;
                        IsSaving = false;
                        LOG("Error on Saving file: " + ex.Message);
                    }
                }
                else LOG("Cannot save this file");
        }

        public async Task UWPSaveAll()
        {
            try
            {
                if (!IsSaving && CurrentFileItem != null)
                {
                    if (CurrentFileItem.File != null)
                    {
                        IsSaving = true;
                        IsPaused = false;
                        foreach (var item in FileItems)
                        {
                            if (item.IsChanged)
                            {
                                string cont = item.FileContent ?? " ";
                                var buffer = Windows.Security.Cryptography.CryptographicBuffer.ConvertStringToBinary(cont, Windows.Security.Cryptography.BinaryStringEncoding.Utf8);
                                await FileIO.WriteBufferAsync((StorageFile)item.File, buffer);
                                item.LastSaveFileContent = item.FileContent;
                                item.IsChanged = false;
                                LOG("File "+item.File.Name+" saved");
                            }

                        }
                        Default.TexFileName = CurrentFileItem.FileName;
                        Default.TexFilePath = CurrentFileItem.File.Path;
                        Default.TexFileFolder = CurrentFileItem.FileFolder;
                        IsSaving = false;
                        IsPaused = true;
                        IsVisible = false;
                    }
                }
                //else LOG("Error");
            }
            catch (Exception ex)
            {
                IsError = true;
                IsSaving = false;
                LOG("Error on Saving file: " + ex.Message);
            }
        }

        private async void DirSearch(StorageFolder sDir, int level = 0)
        {
            try
            {
                foreach (StorageFolder d in await sDir.GetFoldersAsync())
                {
                    if (!d.Name.StartsWith("."))
                    {
                        FileItem SubFolder = new FileItem(d) { Type = FileItem.ExplorerItemType.Folder, FileName = d.Name, IsRoot = false };
                        foreach (StorageFile f in await d.GetFilesAsync())
                        {
                            if (!cancelWords.Contains(f.FileType))
                            {
                                SubFolder.Children.Add(new FileItem(f) { File = f, Type = FileItem.ExplorerItemType.File, FileName = f.Name, IsRoot = false });
                            }
                        }
                        FileItemsTree.Add(SubFolder);
                        DirSearch(d, level + 1);
                    }
                }
                if (level == 0)
                {
                    foreach (StorageFile f in await sDir.GetFilesAsync())
                    {
                        if (!cancelWords.Contains(f.FileType))
                        {
                            FileItemsTree.Add(new FileItem(f) { File = f, Type = FileItem.ExplorerItemType.File, FileName = f.Name, IsRoot = false });
                        }
                    }
                }
            }
            catch (Exception excpt)
            {
                Message("Error in generating the directory tree: " + excpt.Message);
            }
        }

       

        private async void FileItems_CollectionChanged1(object sender, NotifyCollectionChangedEventArgs e)
        {
            //if (e.Action == NotifyCollectionChangedAction.Add)
            //{
            //    await Task.Delay(500);

            //    CurrentFileItem = e.NewItems[0] as FileItem;
            //}
            if (FileItems?.Count == 0)
            {
                IsFileItemLoaded = false;
            }
            else
            {
                IsFileItemLoaded = true;
            }
        }
    }
}