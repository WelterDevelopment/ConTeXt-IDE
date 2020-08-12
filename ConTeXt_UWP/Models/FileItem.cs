using ConTeXt_UWP.Helpers;
using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.IO;
using Windows.Storage;

namespace ConTeXt_UWP.Models
{
    public class FileItem : Bindable
    {
        public FileItem(IStorageItem file, bool isRoot = false)
        {
            FileName = file != null ? file.Name : "";
            FileContent = LastSaveFileContent = "";
            IsRoot = isRoot;
            File = file;
            FileFolder = file != null ? Path.GetDirectoryName(file.Path) : "";
            if (file != null && file is StorageFile)
                FileLanguage = GetFileLanguage(((StorageFile)file).FileType);

            if (Children != null)
            {
                Children.CollectionChanged += Children_CollectionChanged;
            }
            IsLogFile = false;
        }

        public enum ExplorerItemType { Folder, File, ProjectRootFolder };

        public FileItem Parent { get => Get<FileItem>(null); set => Set(value); }

        public ObservableCollection<FileItem> Children
        {
            get { return Get(new ObservableCollection<FileItem>()); }
            set { Set(value); }
        }

        public ObservableCollection<OutlineItem> OutlineItems
        {
            get { return Get(new ObservableCollection<OutlineItem>()); }
            set { Set(value); }
        }

        public IStorageItem File
        {
            get { return Get<IStorageItem>(null); }
            set { Set(value); }
        }

        public string FileContent
        {
            get { return Get(""); }
            set
            {
                Set(value);
                if (!string.IsNullOrEmpty(value))
                {
                    IsChanged = value != LastSaveFileContent;
                }
                //if (App.VM.Default.ShowOutline)
                //{
                //    //App.VM.CurrentEditor.FindMatchesAsync(@"(\\start(sub)*?(section|subject|part|chapter|title)\s*?\[\s*?)(title\s*?=\s*?\{?)(.*?)\}?\s*?([,\]])", false, true, false, null, true, 20);
                //    // var list = await editor.FindMatchesAsync(@"(\\start(sub)*?(section|subject|part|chapter|title)\s*?\[\s*?)(title\s*?=\s*?\{?)(.*?)\}?\s*?([,\]])", false, true, false, null, true, 20);
                //}
            }
        }

        public string FileFolder
        {
            get { return Get(""); }
            set { Set(value); }
        }
        public string FileLanguage
        {
            get { return Get("context"); }
            set { Set(value); }
        }

        public string FileName
        {
            get { return Get(""); }
            set { Set(value); }
        }

        public bool IsChanged
        {
            get { return Get(false); }
            set { Set(value); }
        }

        public bool IsExpanded
        {
            get { return Get(false); }
            set { Set(value); }
        }

        public bool IsLogFile
        {
            get { return Get(false); }
            set { Set(value); }
        }

        public bool IsRoot
        {
            get { return Get(false); }
            set { Set(value); }
        }

        public bool IsSelected
        {
            get { return Get(false); }
            set { Set(value); }
        }

        public string LastSaveFileContent
        {
            get { return Get(""); }
            set { Set(value); }
        }

        public ExplorerItemType Type
        {
            get { return Get(ExplorerItemType.File); }
            set { Set(value); }
        }


        public static string GetFileLanguage(string ext)
        {
            switch (ext)
            {
                case ".tex": return "context";
                case ".mkiv": return "context";
                case ".mkii": return "context";
                case ".mkxl": return "context";
                case ".mkvi": return "context";
                case ".lua": return "lua";
                case ".json": return "javascript";
                case ".js": return "javascript";
                case ".md": return "markdown";
                case ".html": return "html";
                case ".xml": return "xml";
                case ".log": return "log";
                default:
                    return "context";
            }
        }


        private async void Children_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            try
            {
                //if (App.VM.IsProjectLoaded)
                //    if (e.Action == NotifyCollectionChangedAction.Add)
                //    {
                //        bool ischanged = false;
                //        foreach (FileItem fi in e.NewItems)
                //        {
                //            if (fi.File is StorageFile fil && File is StorageFolder fold)
                //            {
                //                var parent = await fil.GetParentAsync();
                //                if (parent.Path != fold.Path)
                //                {
                //                    await fil.MoveAsync(fold, fil.Name, NameCollisionOption.GenerateUniqueName);
                //                    fi.FileFolder = Path.GetDirectoryName(fil.Path);
                //                    //  fi.FilePath = fil.Path;
                //                    App.VM.LOG("Moved " + fil.Name + " from " + parent.Name + " to " + fold.Name);
                //                    ischanged = true;
                //                }
                //            }
                //            else if (fi.File is StorageFolder fol && File is StorageFolder folcurr)
                //            {
                //                var parent = await fol.GetParentAsync();
                //                if (parent.Path != folcurr.Path)
                //                {
                //                    App.VM.LOG("Moving Folders to Subfolders is currently not supported. Please do this operation in the Windows Explorer and reload the project.");
                //                }
                //            }
                //        }
                //        if (ischanged)
                //        {
                //            //Children.Sort((a,b)=> { return string.Compare(a.File.Name, b.File.Name); });
                //        }
                //    }
            }
            catch (Exception ex)
            {
                App.VM.LOG(ex.Message);
            }
        }
    }
}
