using BSc_Thesis.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO.Ports;
using System.Linq;
using System.Management;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;

namespace BSc_Thesis.ViewModels
{
    class ComCaptureViewModel : ViewModelBase
    {

        private int portBitRate = 9600;
        private int dataBits = 8;
        private SerialPort SP1 = new SerialPort();
        private bool isDtr = true;
        private ObservableCollection<Port> ports;
        private Port portValue;
        private string stopBitsValue;
        private string handshakeValue;
        private string parityValue;
        private string comPortLog;
        private bool active = false;
        public bool IsPortActive {
            get => active;
            set {
                if (active != value) {
                    active = value;
                    OnPropertyChanged();
                }
            }
        }

        public string ComPortLog {
            get => comPortLog;
            set {
                if (comPortLog != value) {
                    comPortLog = value;
                    OnPropertyChanged();
                }
            }
        }
        public ObservableCollection<string> Parity { get; } = new ObservableCollection<string>() { "Even", "Mark", "None", "Odd", "Space" };
        public ObservableCollection<string> Handshake { get; } = new ObservableCollection<string>() { "None", "RequestToSend", "RequestToSendXOnXOff", "XOnXOff" };
        public ObservableCollection<string> StopBits { get; } = new ObservableCollection<string>() { "None", "One", "OnePointFive", "Two" };
        public DelegateCommand RefreshPortsCommand { get; }
        public DelegateCommand TurnListeningCommand { get; }
        public DelegateCommand ClearLogCommand { get; }
        public string StopBitsValue {
            get => stopBitsValue;
            set {
                if (stopBitsValue != value) {
                    stopBitsValue = value;
                    OnPropertyChanged();
                }
            }
        }
        public string HandshakeValue {
            get => handshakeValue;
            set {
                if (handshakeValue != value) {
                    handshakeValue = value;
                    OnPropertyChanged();
                }
            }
        }
        public string ParityValue {
            get => parityValue;
            set {
                if (parityValue != value) {
                    parityValue = value;
                    OnPropertyChanged();
                }
            }
        }
        public Port PortValue {
            get => portValue;
            set {
                if (portValue != value) {
                    portValue = value;
                    OnPropertyChanged(); }
            }
        }
        public ObservableCollection<Port> Port {
            get => ports;
            set {
                if (ports != value) {
                    ports = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool IsDtr {
            get => isDtr;
            set {
                if (isDtr != value) {
                    isDtr = value;
                    OnPropertyChanged();
                }
            }
        }

        public int DataBits {
            get => dataBits;
            set {
                if (dataBits != value) {
                    dataBits = value;
                    OnPropertyChanged();
                }
            }
        }

        public int PortBitRate {
            get => portBitRate;
            set {
                if (portBitRate != value) {
                    portBitRate = value;
                    OnPropertyChanged();
                }
            }
        }

        public ComCaptureViewModel()
        {
            RefreshPortsCommand = new DelegateCommand(refreshPorts);
            TurnListeningCommand = new DelegateCommand(turnListening);
            ClearLogCommand = new DelegateCommand(clearLog);
            refreshPorts();
        }

        private void clearLog()
        {
            ComPortLog = string.Empty;
        }

        private void turnListening()
        {
            if (!IsPortActive) {
                try {
                    if (PortValue == null)
                        throw new Exception("Please choose valid Port");
                    SP1.PortName = PortValue.ToString();
                    SP1.BaudRate = PortBitRate;
                    SP1.Parity = (Parity) Enum.Parse(typeof(Parity), ParityValue);
                    SP1.DataBits = DataBits;
                    SP1.StopBits = (StopBits) Enum.Parse(typeof(StopBits), StopBitsValue);
                    SP1.Handshake = (Handshake) Enum.Parse(typeof(Handshake), HandshakeValue);
                    SP1.DtrEnable = IsDtr;
                    SP1.DataReceived += new SerialDataReceivedEventHandler(DataReceivedHandler);
                    SP1.Open();
                } catch (Exception e) {
                    MessageBox.Show(e.Message);
                }
            } else if (SP1.IsOpen)
                SP1.Close();
            IsPortActive = SP1.IsOpen?true:false;
        }

        private void DataReceivedHandler(object sender, SerialDataReceivedEventArgs e)
        {
            SerialPort sp = (SerialPort) sender;
            string indata = sp.ReadExisting();
            Application.Current.Dispatcher.Invoke(System.Windows.Threading.DispatcherPriority.Normal, new Action(() => ComPortLog += indata));
        }

        private void refreshPorts()
        {
            using (var searcher = new ManagementObjectSearcher("SELECT * FROM WIN32_SerialPort")) {
                string[] portnames = SerialPort.GetPortNames();
                var x = searcher.Get().Cast<ManagementBaseObject>().ToList();
                Port = new ObservableCollection<Port>((from n in portnames
                                                       join p in x on n equals p["DeviceID"].ToString() into np
                                                       select new Port() { Name = n }));
            }
        }
    }
}
