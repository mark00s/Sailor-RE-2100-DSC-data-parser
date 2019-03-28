using NAudio.CoreAudioApi;
using NAudio.Wave;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows;

namespace BSc_Thesis.ViewModels
{
    class SoundReceiverViewModel : ViewModelBase
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
        private string outputFolder;
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
        public DelegateCommand SelectFolderCommand { get; }
        public ObservableCollection<string> Recordings { get; }
        public float Timeout
        {
            get => timeout;
            set
            {
                if (timeout != value)
                {
                    timeout = value;
                    OnPropertyChanged();
                }
            }
        }
        public string OutputFolder
        {
            get => outputFolder;
            set
            {
                if (outputFolder != value)
                {
                    outputFolder = value;
                    OnPropertyChanged();
                }
            }
        }
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
                    OnPropertyChanged();
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
                    OnPropertyChanged();
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
                    OnPropertyChanged();
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
                if (peakLevel != value)
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
                    OnPropertyChanged();
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
                    OnPropertyChanged();
                }
            }
        }

        #endregion

        public SoundReceiverViewModel()
        {
            var enumerator = new MMDeviceEnumerator();
            var defaultDevice = enumerator.GetDefaultAudioEndpoint(DataFlow.Capture, Role.Console);
            CaptureDevices = new ObservableCollection<MMDevice>(enumerator.EnumerateAudioEndPoints(DataFlow.All, DeviceState.Active).AsEnumerable());
            SelectedDevice = CaptureDevices.FirstOrDefault(c => c.ID == defaultDevice.ID);
            Recordings = new ObservableCollection<string>();
            RecordCommand = new DelegateCommand(Record);
            StopCommand = new DelegateCommand(Stop) { IsEnabled = false };
            PlayCommand = new DelegateCommand(Play);
            DeleteCommand = new DelegateCommand(Delete);
            TestCommand = new DelegateCommand(Test);
            SelectFolderCommand = new DelegateCommand(SelectFolder);
            OpenFolderCommand = new DelegateCommand(OpenFolder);
            synchronizationContext = SynchronizationContext.Current;
            startDT = DateTime.Now;
            OutputFolder = Path.Combine(Path.GetTempPath(), "BsC_Recordings");
            Directory.CreateDirectory(OutputFolder);
            foreach (var file in Directory.GetFiles(OutputFolder))
                if (Path.GetExtension(file) == ".wav")
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
                if (selectedDevice.DataFlow == DataFlow.Capture)
                {
                    capture = new WasapiCapture(SelectedDevice);
                    capture.WaveFormat =
                        SampleTypeIndex == 0 ? WaveFormat.CreateIeeeFloatWaveFormat(sampleRate, channelCount) :
                        new WaveFormat(sampleRate, bitDepth, channelCount);
                }
                else
                    capture = new WasapiLoopbackCapture(SelectedDevice);
                capture.ShareMode = ShareModeIndex == 0 ? AudioClientShareMode.Shared : AudioClientShareMode.Exclusive;
                RecordLevel = SelectedDevice.AudioEndpointVolume.MasterVolumeLevelScalar;
                capture.StartRecording();
                capture.RecordingStopped += OnRecordingStopped;
                capture.DataAvailable += CaptureOnDataAvailable;
                RecordCommand.IsEnabled = false;
                TestCommand.IsEnabled = false;
                StopCommand.IsEnabled = true;
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message);
            }
        }

        private void SelectFolder()
        {
            System.Windows.Forms.FolderBrowserDialog Dialog = new System.Windows.Forms.FolderBrowserDialog();
            while (Dialog.ShowDialog() != System.Windows.Forms.DialogResult.OK)
            {
                Dialog.Reset();
            }
            OutputFolder = Dialog.SelectedPath;
            watcher.Path = OutputFolder;
            watcher.Changed += new FileSystemEventHandler(OnChanged);
            watcher.Created += new FileSystemEventHandler(OnChanged);
            watcher.Deleted += new FileSystemEventHandler(OnChanged);
            watcher.Renamed += new RenamedEventHandler(OnRenamed);
            watcher.EnableRaisingEvents = true;
            Application.Current.Dispatcher.BeginInvoke(
            System.Windows.Threading.DispatcherPriority.Background,
            new Action(() =>
            {
                Recordings.Clear();
                foreach (var file in Directory.GetFiles(OutputFolder))
                    if (Path.GetExtension(file) == ".wav")
                        Recordings.Add(Path.GetFileName(file));
                OnPropertyChanged("Recordings");
            }));
        }

        private void GetDefaultRecordingFormat(MMDevice value)
        {
            WasapiCapture c = value.DataFlow == DataFlow.Capture ? c = new WasapiCapture(value) : c = new WasapiLoopbackCapture(value);
            SampleTypeIndex = c.WaveFormat.Encoding == WaveFormatEncoding.IeeeFloat ? 0 : 1;
            SampleRate = c.WaveFormat.SampleRate;
            BitDepth = c.WaveFormat.BitsPerSample;
            ChannelCount = c.WaveFormat.Channels;
            c.Dispose();
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
                if (selectedDevice.DataFlow == DataFlow.Capture)
                {
                    capture = new WasapiCapture(SelectedDevice);
                    capture.WaveFormat =
                        SampleTypeIndex == 0 ? WaveFormat.CreateIeeeFloatWaveFormat(sampleRate, channelCount) :
                        new WaveFormat(sampleRate, bitDepth, channelCount);
                }
                else
                    capture = new WasapiLoopbackCapture(SelectedDevice);
                capture.ShareMode = ShareModeIndex == 0 ? AudioClientShareMode.Shared : AudioClientShareMode.Exclusive;
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
            Peak = getMaximumSample(args); ;
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

        private float getMaximumSample(WaveInEventArgs args)
        {
            WaveBuffer buffer = new WaveBuffer(args.Buffer);
            float max = 0;
            for (int index = 0; index < args.BytesRecorded / 4; index++)
            {
                var sample = buffer.FloatBuffer[index];
                if (sample < 0)
                    sample = -sample;
                if (sample > max)
                    max = sample;
            }
            return max;
        }

        private void CaptureOnDataAvailable(object sender, WaveInEventArgs args)
        {
            if ((DateTime.Now - startDT).Seconds > Timeout && isRecording == true)
            {
                dumpFile();
                isRecording = false;
            }
            float max = getMaximumSample(args);
            if (max >= peakLevel)
            {
                isRecording = true;
                startDT = DateTime.Now;
                if (writer == null)
                {
                    currentFileName = String.Format("{0:dd.MM.yyy - HH-mm-ss}.wav", DateTime.Now);
                    writer = new WaveFileWriter(Path.Combine(OutputFolder, currentFileName), capture.WaveFormat);
                }
            }
            if (writer != null)
                writer.Write(args.Buffer, 0, args.BytesRecorded);
            Peak = max;
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
                new Action(() =>
                {
                    Recordings.Clear();
                    foreach (var file in Directory.GetFiles(OutputFolder))
                        if (Path.GetExtension(file) == ".wav")
                            Recordings.Add(Path.GetFileName(file));
                    OnPropertyChanged("Recordings");
                }));
        }
        private void OnRenamed(object source, RenamedEventArgs e)
        {
            Application.Current.Dispatcher.BeginInvoke(
            System.Windows.Threading.DispatcherPriority.Background,
            new Action(() =>
            {
                Recordings.Clear();
                foreach (var file in Directory.GetFiles(OutputFolder))
                    if (Path.GetExtension(file) == ".wav")
                        Recordings.Add(Path.GetFileName(file));
                OnPropertyChanged("Recordings");
            }));
        }
    }
}
