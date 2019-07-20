namespace BSc_Thesis.Models
{
    class SoundData
    {
        public int BitDepth { get; set; }
        public int ChannelCount { get; set; }
        public int ShareModeIndex { get; set; }
        public int SampleRate { get; set; }
        public float PeakLevel { get; set; }
        public float RecordLevel { get; set; }
        public float Peak { get; set; }
    }
}
