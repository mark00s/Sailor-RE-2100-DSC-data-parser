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
        private float recordLevel;
        private float peak;
        private float peakLevel;
        private float timeout = 2.0F;
        private DateTime startDT;
        private bool isRecording = false;
        private string currentFileName;
        private string selectedRecording;
        private readonly SynchronizationContext synchronizationContext;
        private FileSystemWatcher watcher = new FileSystemWatcher();
        #endregion

        #region Properties
        public DelegateCommand RecordCommand { get; }
        public DelegateCommand StopCommand { get; }
        public ObservableCollection<MMDevice> CaptureDevices { get; }
        public DelegateCommand PlayCommand { get; }
        public DelegateCommand TestCommand { get; }
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
        public float PeakLevel
        {
            get => peakLevel;
            set
            {
                if (peakLevel!= value)
                {
                    peakLevel = value;
                    OnPropertyChanged();
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
            var enumerator = new MMDeviceEnumerator();
            var defaultDevice = enumerator.GetDefaultAudioEndpoint(DataFlow.Capture, Role.Console);
            CaptureDevices = new ObservableCollection<MMDevice>(enumerator.EnumerateAudioEndPoints(DataFlow.Capture, DeviceState.Active).AsEnumerable());
            SelectedDevice = CaptureDevices.FirstOrDefault(c => c.ID == defaultDevice.ID);
            Recordings = new ObservableCollection<string>();
            RecordCommand = new DelegateCommand(Record);
            StopCommand = new DelegateCommand(Stop) { IsEnabled = false };
            PlayCommand = new DelegateCommand(Play);
            DeleteCommand = new DelegateCommand(Delete);
            TestCommand = new DelegateCommand(Test);
            OpenFolderCommand = new DelegateCommand(OpenFolder);
            synchronizationContext = SynchronizationContext.Current;
            startDT = DateTime.Now;
            OutputFolder = Path.Combine(Path.GetTempPath(), "BsC_Recordings");
            Directory.CreateDirectory(OutputFolder);
            foreach (var file in Directory.GetFiles(OutputFolder))
                Recordings.Add(Path.GetFileName(file));
            watcher.Path = OutputFolder;
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
            }
        }
        void OnRecordingStopped(object sender, StoppedEventArgs e)
        {
            if (writer != null)
            {
                writer.Dispose();
                writer = null;
            }
            capture.Dispose();
            capture = null;
        }

        void TestOnRecordingStopped(object sender, StoppedEventArgs e)
        {
            capture.Dispose();
            capture = null;

        }

        private void Test()
        {
            try
            {
                capture = new WasapiCapture(SelectedDevice);
                capture.ShareMode = ShareModeIndex == 0 ? AudioClientShareMode.Shared : AudioClientShareMode.Exclusive;
                capture.WaveFormat =
                    SampleTypeIndex == 0 ? WaveFormat.CreateIeeeFloatWaveFormat(sampleRate, channelCount) :
                    new WaveFormat(sampleRate, bitDepth, channelCount);
                RecordLevel = SelectedDevice.AudioEndpointVolume.MasterVolumeLevelScalar;
                capture.StartRecording();
                RecordCommand.IsEnabled = false;
                TestCommand.IsEnabled = false;
                StopCommand.IsEnabled = true;
                capture.DataAvailable += TestCaptureOnDataAvailable;
                capture.RecordingStopped += OnRecordingStopped;
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message);
            }
        }

        private void TestCaptureOnDataAvailable(object sender, WaveInEventArgs args)
        {
            UpdatePeakMeter();
        }

        private void OpenFolder()
        {
            Process.Start(OutputFolder);
        }

        private void dumpFile()
        {
            if (writer != null)
            {
                writer.Dispose();
                writer = null;
            }
        }

        private void CaptureOnDataAvailable(object sender, WaveInEventArgs args)
        {
            if ((DateTime.Now - startDT).Seconds > timeout && isRecording == true)
            {
                dumpFile();
                isRecording = false;
            }
            float max = 0;
            var buffer = new WaveBuffer(args.Buffer);
            for (int index = 0; index < args.BytesRecorded / 4; index++)
            {
                var sample = buffer.FloatBuffer[index];
                if (sample < 0) sample = -sample;
                if (sample > max) max = sample;
            }
            if (max >= peakLevel)
            {
                isRecording = true;
                startDT = DateTime.Now;
                if (writer == null)
                {
                    currentFileName = String.Format("{0:dd.MM.yyy - HH-mm-ss}.wav", DateTime.Now);
                    writer = new WaveFileWriter(Path.Combine(OutputFolder, currentFileName), capture.WaveFormat);
                }
                writer.Write(args.Buffer, 0, args.BytesRecorded);
            }
            UpdatePeakMeter();
        }

        void UpdatePeakMeter()
        {
            synchronizationContext.Post(s => Peak = SelectedDevice.AudioMeterInformation.MasterPeakValue, null);
        }
        private void Play()
        {
            if (SelectedRecording != null)
                Process.Start(Path.Combine(OutputFolder, SelectedRecording));
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
            RecordCommand.IsEnabled = true;
            StopCommand.IsEnabled = false;
            TestCommand.IsEnabled = true;
            Peak = 0.0F;
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
                        Recordings.Add(Path.GetFileName(file));
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
                    Recordings.Add(Path.GetFileName(file));
                OnPropertyChanged("Recordings");
                }));
        }

    }
}
