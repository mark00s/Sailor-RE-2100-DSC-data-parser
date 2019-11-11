using NAudio.CoreAudioApi;
using NAudio.Wave;

namespace BSc_Thesis.Models
{
    class SoundData
    {
        public WasapiCapture Capture { get; set; }
        public WaveFileWriter Writer { get; set; }
        public string CurrentFileName { get; set; }
        public float Timeout { get; set; }
        public MMDevice SelectedDevice { get; set; }
        public int BitDepth { get; set; }
        public int ChannelCount { get; set; }
        public int ShareModeIndex { get; set; }
        public int SampleRate { get; set; }
        public int SampleTypeIndex { get; set; }
        public float PeakLevel { get; set; }
        public float RecordLevel { get; set; }
        public float Peak { get; set; }
        public SoundData()
        {
            Timeout = 2.0f;
        }
    }
}
