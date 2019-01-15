using NAudio.CoreAudioApi;
using System.Collections.ObjectModel;
using System.Linq;
using BSc_Thesis.ViewModels;
using NAudio.Wave;
using System;
using System.Windows;
using System.IO;
using System.Threading;
using System.Diagnostics;

namespace BSc_Thesis
{
    class MainWindowViewModel : ViewModelBase
    {
        #region Fields
        private MMDevice selectedDevice;
        private WasapiCapture capture;
        private WaveFileWriter writer;
        private int shareModeIndex;
        private int sampleTypeIndex;
        private int sampleRate;
        private int channelCount;
        private int bitDepth;
        private string currentFileName;
        private float recordLevel;
        private float peak;
        private string message;
        private string selectedRecording;
        private readonly SynchronizationContext synchronizationContext;
        private FileSystemWatcher watcher = new FileSystemWatcher();
        #endregion

        #region Properties
        public DelegateCommand RecordCommand { get; }
        public DelegateCommand StopCommand { get; }
        public ObservableCollection<MMDevice> CaptureDevices { get; }
        public DelegateCommand PlayCommand { get; }
        public DelegateCommand DeleteCommand { get; }
        public DelegateCommand OpenFolderCommand { get; }

        public ObservableCollection<string> Recordings { get; }
        public string OutputFolder { get; }
        public MMDevice SelectedDevice
        {
            get => selectedDevice;
            set
            {
                if (selectedDevice != value)
                {
                    selectedDevice = value;
                    OnPropertyChanged();
                    GetDefaultRecordingFormat(value);
                }
            }
        }
        public int BitDepth
        {
            get => bitDepth;
            set
            {
                if (bitDepth != value)
                {
                    bitDepth = value;
                    OnPropertyChanged("");
                }
            }
        }
        public int ChannelCount
        {
            get => channelCount;
            set
            {
                if (channelCount != value)
                {
                    channelCount = value;
                    OnPropertyChanged("");
                }
            }
        }
        public int ShareModeIndex
        {
            get => shareModeIndex;
            set
            {
                if (shareModeIndex != value)
                {
                    shareModeIndex = value;
                    OnPropertyChanged();
                }
            }
        }
        public int SampleRate
        {
            get => sampleRate;
            set
            {
                if (sampleRate != value)
                {
                    sampleRate = value;
                    OnPropertyChanged("");
                }
            }
        }
        public int SampleTypeIndex
        {
            get => sampleTypeIndex;
            set
            {
                if (sampleTypeIndex != value)
                {
                    sampleTypeIndex = value;
                    OnPropertyChanged();
                    BitDepth = sampleTypeIndex == 1 ? 16 : 32;
                    OnPropertyChanged("IsBitDepthConfigurable");
                }
            }
        }
        public float RecordLevel
        {
            get => recordLevel;
            set
            {
                if (recordLevel != value)
                {
                    recordLevel = value;
                    if (capture != null)
                    {
                        SelectedDevice.AudioEndpointVolume.MasterVolumeLevelScalar = value;
                    }
                    OnPropertyChanged();
                }
            }
        }
        public string Message
        {
            get => message;
            set
            {
                if (message != value)
                {
                    message = value;
                    OnPropertyChanged("Message");
                }
            }
        }
        public string SelectedRecording
        {
            get => selectedRecording;
            set
            {
                if (selectedRecording != value)
                {
                    selectedRecording = value;
                    OnPropertyChanged("");
                    EnableCommands();
                }
            }
        }
        public float Peak
        {
            get => peak;
            set
            {
                if (peak != value)
                {
                    peak = value;
                    OnPropertyChanged("");
                }
            }
        }

        #endregion

        public MainWindowViewModel()
        {
            Recordings = new ObservableCollection<string>();
            OutputFolder = Path.Combine(Path.GetTempPath(), "BsC_Recordings");
            Directory.CreateDirectory(OutputFolder);
            synchronizationContext = SynchronizationContext.Current;
            foreach (var file in Directory.GetFiles(OutputFolder))
            {
                Recordings.Add(Path.GetFileName(file));
            }
            watcher.Path = OutputFolder;
            var enumerator = new MMDeviceEnumerator();
            CaptureDevices = new ObservableCollection<MMDevice>(enumerator.EnumerateAudioEndPoints(DataFlow.Capture, DeviceState.Active).AsEnumerable());
            var defaultDevice = enumerator.GetDefaultAudioEndpoint(DataFlow.Capture, Role.Console);
            SelectedDevice = CaptureDevices.FirstOrDefault(c => c.ID == defaultDevice.ID);
            RecordCommand = new DelegateCommand(Record);
            StopCommand = new DelegateCommand(Stop) { IsEnabled = false };
            PlayCommand = new DelegateCommand(Play);
            DeleteCommand = new DelegateCommand(Delete);
            OpenFolderCommand = new DelegateCommand(OpenFolder);
            watcher.Changed += new FileSystemEventHandler(OnChanged);
            watcher.Created += new FileSystemEventHandler(OnChanged);
            watcher.Deleted += new FileSystemEventHandler(OnChanged);
            watcher.Renamed += new RenamedEventHandler(OnRenamed);
            watcher.EnableRaisingEvents = true;
            EnableCommands();
        }
        private void Record()
        {
            try
            {
                capture = new WasapiCapture(SelectedDevice);
                capture.ShareMode = ShareModeIndex == 0 ? AudioClientShareMode.Shared : AudioClientShareMode.Exclusive;
                capture.WaveFormat =
                    SampleTypeIndex == 0 ? WaveFormat.CreateIeeeFloatWaveFormat(sampleRate, channelCount) :
                    new WaveFormat(sampleRate, bitDepth, channelCount);
                currentFileName = String.Format("{0:dd.MM.yyy - HH-mm-ss}.wav", DateTime.Now);
                RecordLevel = SelectedDevice.AudioEndpointVolume.MasterVolumeLevelScalar;
                capture.StartRecording();
                capture.RecordingStopped += OnRecordingStopped;
                capture.DataAvailable += CaptureOnDataAvailable;
                RecordCommand.IsEnabled = false;
                StopCommand.IsEnabled = true;
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message);
            }
        }
        private void GetDefaultRecordingFormat(MMDevice value)
        {
            using (var c = new WasapiCapture(value))
            {
                SampleTypeIndex = c.WaveFormat.Encoding == WaveFormatEncoding.IeeeFloat ? 0 : 1;
                SampleRate = c.WaveFormat.SampleRate;
                BitDepth = c.WaveFormat.BitsPerSample;
                ChannelCount = c.WaveFormat.Channels;
                Message = "";
            }
        }
        void OnRecordingStopped(object sender, StoppedEventArgs e)
        {
            writer.Dispose();
            writer = null;
            Recordings.Add(currentFileName);
            SelectedRecording = currentFileName;
            if (e.Exception == null)
                Message = "Recording Stopped";
            else
                Message = "Recording Error: " + e.Exception.Message;
            capture.Dispose();
            capture = null;
            RecordCommand.IsEnabled = true;
            StopCommand.IsEnabled = false;
        }

        private void OpenFolder()
        {
            Process.Start(OutputFolder);
        }

        private void CaptureOnDataAvailable(object sender, WaveInEventArgs waveInEventArgs)
        {
            if (writer == null)
            {
                writer = new WaveFileWriter(Path.Combine(OutputFolder,
                    currentFileName),
                    capture.WaveFormat);
            }

            writer.Write(waveInEventArgs.Buffer, 0, waveInEventArgs.BytesRecorded);

            UpdatePeakMeter();
        }
        void UpdatePeakMeter()
        {
            synchronizationContext.Post(s => Peak = SelectedDevice.AudioMeterInformation
                .MasterPeakValue*10.0F, null);
        }
        private void Play()
        {
            if (SelectedRecording != null)
            {
                Process.Start(Path.Combine(OutputFolder, SelectedRecording));
            }
        }

        private void Delete()
        {
            if (SelectedRecording != null)
            {
                try
                {
                    File.Delete(Path.Combine(OutputFolder, SelectedRecording));
                    Recordings.Remove(SelectedRecording);
                    SelectedRecording = Recordings.FirstOrDefault();
                }
                catch (Exception)
                {
                    MessageBox.Show("Could not delete recording");
                }
            }
        }

        private void Stop()
        {
            capture?.StopRecording();
        }

        private void EnableCommands()
        {
            PlayCommand.IsEnabled = SelectedRecording != null;
            DeleteCommand.IsEnabled = SelectedRecording != null;
        }
        private void OnChanged(object source, FileSystemEventArgs e)
        {
            Application.Current.Dispatcher.BeginInvoke(
                System.Windows.Threading.DispatcherPriority.Background,
                new Action(() => {
                    Recordings.Clear();
                    foreach (var file in Directory.GetFiles(OutputFolder))
                    {
                        Recordings.Add(Path.GetFileName(file));
                    }
                    OnPropertyChanged("Recordings");
                }));
        }
        private  void OnRenamed(object source, RenamedEventArgs e)
        {
            Application.Current.Dispatcher.BeginInvoke(
            System.Windows.Threading.DispatcherPriority.Background,
            new Action(() =>
            {
                Recordings.Clear();
                foreach (var file in Directory.GetFiles(OutputFolder))
                {
                    Recordings.Add(Path.GetFileName(file));
                }
                OnPropertyChanged("Recordings");
                }));
        }

    }
}
