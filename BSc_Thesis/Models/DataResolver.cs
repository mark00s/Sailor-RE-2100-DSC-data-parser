using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Timers;

namespace BSc_Thesis.Models
{
    class DataResolver : ViewModelBase
    {
        private Port port;
        public string ComPortTemp {
            get => port.ComPortTemp;
            set {
                if (port.ComPortTemp != value)
                    port.ComPortTemp = value;
            }
        }

        public string ReceivedCalls {
            get => port.ReceivedCalls;
            set {
                if (port.ReceivedCalls != value) {
                    port.ReceivedCalls = value;
                    OnPropertyChanged("ReceivedCalls");
                }
            }
        }


    }
}