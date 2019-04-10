using System.Collections.ObjectModel;
using System.IO.Ports;

namespace BSc_Thesis.Models
{
    class Port
    {
        public string StopBitsValue { get; set; }
        public string HandshakeValue { get; set; }
        public string ParityValue { get; set; }
        public bool Active { get; set; }
        public int DataBits { get; set; } = 8;
        public string PortName { get; set; }
    }
}
