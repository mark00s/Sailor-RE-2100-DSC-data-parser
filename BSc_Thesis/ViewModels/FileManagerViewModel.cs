using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using BSc_Thesis.Models;

namespace BSc_Thesis.ViewModels
{
    class FileManagerViewModel : ViewModelBase, IFileManager
    {
        private string outputFolder;
        private string selectedFile;
        private string openButtonName;

        protected enum FileExtension { Txt, Wav }
        protected string filextension;
        public FileSystemWatcher watcher = new FileSystemWatcher();
        public DelegateCommand DeleteCommand { get; }
        public DelegateCommand OpenFolderCommand { get; }
        public DelegateCommand OpenCommand { get; }
        public DelegateCommand SelectFolderCommand { get; }
        public ObservableCollection<string> Files { get; }
        public string SelectedFile {
            get => selectedFile;
            set {
                if (selectedFile != value) {
                    selectedFile = value;
                    OnPropertyChanged();
                    EnableCommands();
                }
            }
        }
        public string OutputFolder {
            get => outputFolder;
            set {
                if (outputFolder != value) {
                    outputFolder = value;
                    OnPropertyChanged();
                }
            }
        }


        public string OpenButtonName {
            get => openButtonName; set {
                if (openButtonName != value) {
                    openButtonName = value;
                    OnPropertyChanged();
                }
            }
        }

        private string getFileExtension(FileExtension fe)
        {
            switch (fe) {
                case FileExtension.Txt:
                    OpenButtonName = "Open";
                    return ".txt";
                case FileExtension.Wav:
                    OpenButtonName = "Play";
                    return ".wav";
            }
            return "null";
        }

        protected FileManagerViewModel(FileExtension fe)
        {
            filextension = getFileExtension(fe);
            Files = new ObservableCollection<string>();
            DeleteCommand = new DelegateCommand(Delete);
            SelectFolderCommand = new DelegateCommand(SelectFolder);
            OpenFolderCommand = new DelegateCommand(OpenFolder);
            OpenCommand = new DelegateCommand(Open);
            OutputFolder = Path.Combine(Path.GetTempPath(), "BsC_Recordings");
            Directory.CreateDirectory(OutputFolder);
            foreach (var file in Directory.GetFiles(OutputFolder))
                if (Path.GetExtension(file) == filextension)
                    Files.Add(Path.GetFileName(file));
            EnableCommands();
            rearmWatcher();
        }

        private void OnChanged(object source, FileSystemEventArgs e)
        {
            refreshFiles();
        }

        private void OnRenamed(object source, RenamedEventArgs e)
        {
            refreshFiles();
        }

        private void EnableCommands()
        {
            DeleteCommand.IsEnabled = SelectedFile != null;
        }

        private void refreshFiles()
        {
            Application.Current.Dispatcher.BeginInvoke(
            System.Windows.Threading.DispatcherPriority.Background,
            new Action(() => {
                Files.Clear();
                foreach (var file in Directory.GetFiles(OutputFolder))
                    if (Path.GetExtension(file) == filextension)
                        Files.Add(Path.GetFileName(file));
                OnPropertyChanged("Files");
            }));
        }

        public void rearmWatcher()
        {
            watcher.Path = OutputFolder;
            watcher.Changed += new FileSystemEventHandler(OnChanged);
            watcher.Created += new FileSystemEventHandler(OnChanged);
            watcher.Deleted += new FileSystemEventHandler(OnChanged);
            watcher.Renamed += new RenamedEventHandler(OnRenamed);
            watcher.EnableRaisingEvents = true;
        }

        public void SelectFolder()
        {
            System.Windows.Forms.FolderBrowserDialog Dialog = new System.Windows.Forms.FolderBrowserDialog();
            while (Dialog.ShowDialog() != System.Windows.Forms.DialogResult.OK) {
                Dialog.Reset();
            }
            OutputFolder = Dialog.SelectedPath;
            rearmWatcher();
            refreshFiles();
        }


        public void Delete()
        {
            if (SelectedFile != null) {
                try {
                    File.Delete(Path.Combine(OutputFolder, SelectedFile));
                    Files.Remove(SelectedFile);
                    SelectedFile = Files.FirstOrDefault();
                } catch (Exception) {
                    MessageBox.Show("Could not delete recording");
                }
            }
        }

        public void OpenFolder()
        {
            Process.Start(OutputFolder);
        }

        public void Open()
        {
            if (SelectedFile != null)
                Process.Start(Path.Combine(OutputFolder, SelectedFile));
        }
    }
}