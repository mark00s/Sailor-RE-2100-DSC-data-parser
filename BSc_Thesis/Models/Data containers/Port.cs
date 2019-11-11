using System.Collections.ObjectModel;

namespace BSc_Thesis.Models
{
    class Port
    {
        public string Name { get; set; }
        public string StopBitsValue { get; set; }
        public string HandshakeValue { get; set; }
        public string ParityValue { get; set; }
        public bool Active { get; set; }
        public int DataBits { get; set; } = 8;
        public string ComPortLog { get; set; }
        public string OutputFolder { get; set; }
        public string CurrentFileName { get; set; }
        public string ComPortTemp { get; set; }
        public string ReceivedCalls { get; set; }
        public string PortName { get; set; }


        public Port()
        {
            ComPortTemp = string.Empty;
        }
    }
}
