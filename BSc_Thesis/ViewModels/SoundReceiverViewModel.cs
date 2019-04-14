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
    class SoundReceiverViewModel : FileManagerViewModel
    {
        #region Fields
        private WasapiCapture capture;
        private WaveFileWriter writer;
        private DateTime startDT;
        private bool isRecording = false;
        private string currentFileName;
        private float timeout = 2.0f;
        private MMDevice selectedDevice;
        private SoundData sd = new SoundData();

        #endregion

        #region Properties
        public DelegateCommand RecordCommand { get; }
        public DelegateCommand StopCommand { get; }
        public ObservableCollection<MMDevice> CaptureDevices { get; }
        public DelegateCommand TestCommand { get; }
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
        public int SampleTypeIndex { get => 0; set { } }
        public int BitDepth {
            get => sd.BitDepth;
            set {
                if (sd.BitDepth != value) {
                    sd.BitDepth = value;
                    OnPropertyChanged();
                }
            }
        }
        public int ChannelCount {
            get => sd.ChannelCount;
            set {
                if (sd.ChannelCount != value) {
                    sd.ChannelCount = value;
                    OnPropertyChanged();
                }
            }
        }
        public int ShareModeIndex {
            get => sd.ShareModeIndex;
            set {
                if (sd.ShareModeIndex != value) {
                    sd.ShareModeIndex = value;
                    OnPropertyChanged();
                }
            }
        }
        public int SampleRate {
            get => sd.SampleRate;
            set {
                if (sd.SampleRate != value) {
                    sd.SampleRate = value;
                    OnPropertyChanged();
                }
            }
        }
        public float PeakLevel {
            get => sd.PeakLevel;
            set {
                if (sd.PeakLevel != value) {
                    sd.PeakLevel = value;
                    OnPropertyChanged();
                }
            }
        }
        public float RecordLevel {
            get => sd.RecordLevel;
            set {
                if (sd.RecordLevel != value) {
                    sd.RecordLevel = value;
                    if (capture != null) {
                        SelectedDevice.AudioEndpointVolume.MasterVolumeLevelScalar = value;
                    }
                    OnPropertyChanged();
                }
            }
        }
        public float Peak {
            get => sd.Peak;
            set {
                if (Peak != value) {
                    sd.Peak = value;
                    OnPropertyChanged();
                }
            }
        }
        #endregion

        public SoundReceiverViewModel() : base(FileExtension.Wav)
        {
            var enumerator = new MMDeviceEnumerator();
            var defaultDevice = enumerator.GetDefaultAudioEndpoint(DataFlow.Capture, Role.Console);
            CaptureDevices = new ObservableCollection<MMDevice>(enumerator.EnumerateAudioEndPoints(DataFlow.All, DeviceState.Active).AsEnumerable());
            SelectedDevice = CaptureDevices.FirstOrDefault(c => c.ID == defaultDevice.ID);
            RecordCommand = new DelegateCommand(Record);
            StopCommand = new DelegateCommand(Stop) { IsEnabled = false };
            TestCommand = new DelegateCommand(Test);
            startDT = DateTime.Now;
        }
        private void Record()
        {
            startCapturing(false);
        }
        private void Test()
        {
            startCapturing(true);
        }

        private void startCapturing(bool isTest = false)
        {
            try {
                if (SelectedDevice.DataFlow == DataFlow.Capture) {
                    capture = new WasapiCapture(SelectedDevice);
                    capture.WaveFormat = WaveFormat.CreateIeeeFloatWaveFormat(SampleRate, ChannelCount);
                } else
                    capture = new WasapiLoopbackCapture(SelectedDevice);
                capture.ShareMode = ShareModeIndex == 0 ? AudioClientShareMode.Shared : AudioClientShareMode.Exclusive;
                RecordLevel = SelectedDevice.AudioEndpointVolume.MasterVolumeLevelScalar;
                capture.StartRecording();
                capture.RecordingStopped += OnRecordingStopped;
                if (isTest) {
                    capture.DataAvailable += TestCaptureOnDataAvailable;
                } else {
                    capture.DataAvailable += CaptureOnDataAvailable;
                }
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
