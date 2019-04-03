using NAudio.CoreAudioApi;
using NAudio.Wave;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows;
using BSc_Thesis.Models;
using System.Diagnostics;

namespace BSc_Thesis.ViewModels
{
    class SoundReceiverViewModel : FileManagerViewModel, IPlayable
    {
        #region Fields
        private WasapiCapture capture;
        private WaveFileWriter writer;
        private DateTime startDT;
        private bool isRecording = false;
        private string currentFileName;
        private readonly SynchronizationContext synchronizationContext;
        private float timeout = 2.0f;
        private MMDevice selectedDevice;
        private int bitDepth;
        private int channelCount;
        private int shareModeIndex;
        private int sampleRate;
        private int sampleTypeIndex;
        private float peakLevel;
        private float recordLevel;
        private float peak;
        #endregion

        #region Properties
        public DelegateCommand RecordCommand { get; }
        public DelegateCommand StopCommand { get; }
        public ObservableCollection<MMDevice> CaptureDevices { get; }
        public DelegateCommand TestCommand { get; }
        public DelegateCommand PlayCommand { get; }

        public float Timeout {
            get => timeout;
            set {
                if (timeout != value) {
                    timeout = value;
                    OnPropertyChanged();
                }
            }
        }
        public MMDevice SelectedDevice {
            get => selectedDevice;
            set {
                if (selectedDevice != value) {
                    selectedDevice = value;
                    OnPropertyChanged();
                    GetDefaultRecordingFormat(value);
                }
            }
        }

        public int BitDepth {
            get => bitDepth;
            set {
                if (bitDepth != value) {
                    bitDepth = value;
                    OnPropertyChanged();
                }
            }
        }
        public int ChannelCount {
            get => channelCount;
            set {
                if (channelCount != value) {
                    channelCount = value;
                    OnPropertyChanged();
                }
            }
        }
        public int ShareModeIndex {
            get => shareModeIndex;
            set {
                if (shareModeIndex != value) {
                    shareModeIndex = value;
                    OnPropertyChanged();
                }
            }
        }
        public int SampleRate {
            get => sampleRate;
            set {
                if (sampleRate != value) {
                    sampleRate = value;
                    OnPropertyChanged();
                }
            }
        }
        public int SampleTypeIndex {
            get => sampleTypeIndex;
            set {
                if (sampleTypeIndex != value) {
                    sampleTypeIndex = value;
                    OnPropertyChanged();
                    BitDepth = sampleTypeIndex == 1 ? 16 : 32;
                    OnPropertyChanged("IsBitDepthConfigurable");
                }
            }
        }
        public float PeakLevel {
            get => peakLevel;
            set {
                if (peakLevel != value) {
                    peakLevel = value;
                    OnPropertyChanged();
                }
            }
        }
        public float RecordLevel {
            get => recordLevel;
            set {
                if (recordLevel != value) {
                    recordLevel = value;
                    if (capture != null) {
                        SelectedDevice.AudioEndpointVolume.MasterVolumeLevelScalar = value;
                    }
                    OnPropertyChanged();
                }
            }
        }
        public float Peak {
            get => peak;
            set {
                if (Peak != value) {
                    peak = value;
                    OnPropertyChanged();
                }
            }
        }
        #endregion

        public SoundReceiverViewModel() : base(FileExtension.Wav)
        {
            var enumerator = new MMDeviceEnumerator();
            PlayCommand = new DelegateCommand(Play);
            var defaultDevice = enumerator.GetDefaultAudioEndpoint(DataFlow.Capture, Role.Console);
            CaptureDevices = new ObservableCollection<MMDevice>(enumerator.EnumerateAudioEndPoints(DataFlow.All, DeviceState.Active).AsEnumerable());
            SelectedDevice = CaptureDevices.FirstOrDefault(c => c.ID == defaultDevice.ID);
            RecordCommand = new DelegateCommand(Record);
            StopCommand = new DelegateCommand(Stop) { IsEnabled = false };
            TestCommand = new DelegateCommand(Test);
            synchronizationContext = SynchronizationContext.Current;
            startDT = DateTime.Now;
        }

        public void Play()
        {
            if (SelectedFile != null)
                Process.Start(Path.Combine(OutputFolder, SelectedFile));
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
                    writer = new WaveFileWriter(Path.Combine(OutputFolder, currentFileName), capture.WaveFormat);
                }
            }
            if (writer != null)
                writer.Write(args.Buffer, 0, args.BytesRecorded);
            Peak = max;
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
