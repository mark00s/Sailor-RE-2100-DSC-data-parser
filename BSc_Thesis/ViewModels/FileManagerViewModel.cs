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
        protected enum FileExtension { Txt, Wav }
        protected string filextension;
        public FileSystemWatcher watcher = new FileSystemWatcher();
        public DelegateCommand DeleteCommand { get; }
        public DelegateCommand OpenFolderCommand { get; }
        public DelegateCommand SelectFolderCommand { get; }
        public ObservableCollection<string> Files { get; }
        public string SelectedRecording {
            get => SelectedRecording;
            set {
                if (SelectedRecording != value) {
                    SelectedRecording = value;
                    OnPropertyChanged();
                    EnableCommands();
                }
            }
        }
        public string OutputFolder {
            get => OutputFolder;
            set {
                if (OutputFolder != value) {
                    OutputFolder = value;
                    OnPropertyChanged();
                }
            }
        }

        private string getFileExtension(FileExtension fe)
        {
            switch (fe) {
                case FileExtension.Txt:
                    return ".txt";
                case FileExtension.Wav:
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
            DeleteCommand.IsEnabled = SelectedRecording != null;
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
            if (SelectedRecording != null) {
                try {
                    File.Delete(Path.Combine(OutputFolder, SelectedRecording));
                    Files.Remove(SelectedRecording);
                    SelectedRecording = Files.FirstOrDefault();
                } catch (Exception) {
                    MessageBox.Show("Could not delete recording");
                }
            }
        }

        public void OpenFolder()
        {
            Process.Start(OutputFolder);
        }

    }
}