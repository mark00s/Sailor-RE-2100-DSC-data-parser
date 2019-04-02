using NAudio.CoreAudioApi;
using NAudio.Wave;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows;

namespace BSc_Thesis.ViewModels
{
    class SoundReceiverViewModel : ViewModelBase
    {
        #region Fields
        private WasapiCapture capture;
        private WaveFileWriter writer;
        private DateTime startDT;
        private bool isRecording = false;
        private string currentFileName;
        private string selectedRecording;
        private readonly SynchronizationContext synchronizationContext;
        #endregion

        #region Properties
        public DelegateCommand RecordCommand { get; }
        public DelegateCommand StopCommand { get; }
        public ObservableCollection<MMDevice> CaptureDevices { get; }
        public DelegateCommand TestCommand { get; }
        public float Timeout {
            get => 22;
            set {
                if (Timeout != value) {
                    Timeout = value;
                    OnPropertyChanged();
                }
            }
        }
        public MMDevice SelectedDevice {
            get => SelectedDevice;
            set {
                if (SelectedDevice != value) {
                    SelectedDevice = value;
                    OnPropertyChanged();
                    GetDefaultRecordingFormat(value);
                }
            }
        }
        public int BitDepth {
            get => BitDepth;
            set {
                if (BitDepth != value) {
                    BitDepth = value;
                    OnPropertyChanged();
                }
            }
        }
        public int ChannelCount {
            get => ChannelCount;
            set {
                if (ChannelCount != value) {
                    ChannelCount = value;
                    OnPropertyChanged();
                }
            }
        }
        public int ShareModeIndex {
            get => ShareModeIndex;
            set {
                if (ShareModeIndex != value) {
                    ShareModeIndex = value;
                    OnPropertyChanged();
                }
            }
        }
        public int SampleRate {
            get => SampleRate;
            set {
                if (SampleRate != value) {
                    SampleRate = value;
                    OnPropertyChanged();
                }
            }
        }
        public int SampleTypeIndex {
            get => SampleTypeIndex;
            set {
                if (SampleTypeIndex != value) {
                    SampleTypeIndex = value;
                    OnPropertyChanged();
                    BitDepth = SampleTypeIndex == 1 ? 16 : 32;
                    OnPropertyChanged("IsBitDepthConfigurable");
                }
            }
        }
        public float PeakLevel {
            get => PeakLevel;
            set {
                if (PeakLevel != value) {
                    PeakLevel = value;
                    OnPropertyChanged();
                }
            }
        }
        public float RecordLevel {
            get => RecordLevel;
            set {
                if (RecordLevel != value) {
                    RecordLevel = value;
                    if (capture != null) {
                        SelectedDevice.AudioEndpointVolume.MasterVolumeLevelScalar = value;
                    }
                    OnPropertyChanged();
                }
            }
        }
        public float Peak {
            get => Peak;
            set {
                if (Peak != value) {
                    Peak = value;
                    OnPropertyChanged();
                }
            }
        }
        #endregion

        public SoundReceiverViewModel()
        {
            //Timeout = 2.0f;
            var enumerator = new MMDeviceEnumerator();
            var defaultDevice = enumerator.GetDefaultAudioEndpoint(DataFlow.Capture, Role.Console);
            CaptureDevices = new ObservableCollection<MMDevice>(enumerator.EnumerateAudioEndPoints(DataFlow.All, DeviceState.Active).AsEnumerable());
            SelectedDevice = CaptureDevices.FirstOrDefault(c => c.ID == defaultDevice.ID);
            RecordCommand = new DelegateCommand(Record);
            StopCommand = new DelegateCommand(Stop) { IsEnabled = false };
            TestCommand = new DelegateCommand(Test);
            synchronizationContext = SynchronizationContext.Current;
            startDT = DateTime.Now;
        }
        private void Record()
        {
            try {
                if (SelectedDevice.DataFlow == DataFlow.Capture) {
                    capture = new WasapiCapture(SelectedDevice);
                    capture.WaveFormat =
                        SampleTypeIndex == 0 ? WaveFormat.CreateIeeeFloatWaveFormat(SampleRate, ChannelCount) :
                        new WaveFormat(SampleRate, BitDepth, ChannelCount);
                } else
                    capture = new WasapiLoopbackCapture(SelectedDevice);
                capture.ShareMode = ShareModeIndex == 0 ? AudioClientShareMode.Shared : AudioClientShareMode.Exclusive;
                RecordLevel = SelectedDevice.AudioEndpointVolume.MasterVolumeLevelScalar;
                capture.StartRecording();
                capture.RecordingStopped += OnRecordingStopped;
                capture.DataAvailable += CaptureOnDataAvailable;
                RecordCommand.IsEnabled = false;
                TestCommand.IsEnabled = false;
                StopCommand.IsEnabled = true;
            } catch (Exception e) {
                MessageBox.Show(e.Message);
            }
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
            if (writer != null) {
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
            try {
                if (SelectedDevice.DataFlow == DataFlow.Capture) {
                    capture = new WasapiCapture(SelectedDevice);
                    capture.WaveFormat =
                        SampleTypeIndex == 0 ? WaveFormat.CreateIeeeFloatWaveFormat(SampleRate, ChannelCount) :
                        new WaveFormat(SampleRate, BitDepth, ChannelCount);
                } else
                    capture = new WasapiLoopbackCapture(SelectedDevice);
                capture.ShareMode = ShareModeIndex == 0 ? AudioClientShareMode.Shared : AudioClientShareMode.Exclusive;
                RecordLevel = SelectedDevice.AudioEndpointVolume.MasterVolumeLevelScalar;
                capture.StartRecording();
                RecordCommand.IsEnabled = false;
                TestCommand.IsEnabled = false;
                StopCommand.IsEnabled = true;
                capture.DataAvailable += TestCaptureOnDataAvailable;
                capture.RecordingStopped += OnRecordingStopped;
            } catch (Exception e) {
                MessageBox.Show(e.Message);
            }
        }

        private void TestCaptureOnDataAvailable(object sender, WaveInEventArgs args)
        {
            Peak = getMaximumSample(args);
        }

        private void dumpFile()
        {
            if (writer != null) {
                writer.Dispose();
                writer = null;
            }
        }

        private float getMaximumSample(WaveInEventArgs args)
        {
            WaveBuffer buffer = new WaveBuffer(args.Buffer);
            float max = 0;
            for (int index = 0; index < args.BytesRecorded / 4; index++) {
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
            if ((DateTime.Now - startDT).Seconds > Timeout && isRecording == true) {
                dumpFile();
                isRecording = false;
            }
            float max = getMaximumSample(args);
            if (max >= PeakLevel) {
                isRecording = true;
                startDT = DateTime.Now;
                if (writer == null) {
                    currentFileName = String.Format("{0:dd.MM.yyy - HH-mm-ss}.wav", DateTime.Now);
 //                   writer = new WaveFileWriter(Path.Combine(OutputFolder, currentFileName), capture.WaveFormat);
                }
            }
            if (writer != null)
                writer.Write(args.Buffer, 0, args.BytesRecorded);
            Peak = max;
        }

        private void Play()
        {

        }

        private void Stop()
        {
            capture?.StopRecording();
            RecordCommand.IsEnabled = true;
            StopCommand.IsEnabled = false;
            TestCommand.IsEnabled = true;
            Peak = 0.0F;
        }
    }
}
