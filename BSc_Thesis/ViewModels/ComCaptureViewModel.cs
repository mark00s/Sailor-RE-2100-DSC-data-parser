using BSc_Thesis.Models;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Management;
using System.Text.RegularExpressions;
using System.Timers;
using System.Windows;

namespace BSc_Thesis.ViewModels
{
    class ComCaptureViewModel : FileManagerViewModel
    {
        #region Fields
        private int portBitRate = 4800;
        private SerialPort sp = new SerialPort();
        private DistressDataResolver ddr = new DistressDataResolver();
        private bool isDtr = true;
        private ObservableCollection<string> portNames;
        private string portName;
        private Port port;
        private string comPortTemp = String.Empty;
        private Regex messageRegex = new Regex(@"Incoming[\w\W]+?> \?");
        private string receivedCalls;
        private string currentFileName;
        private Timer resolverTimer;
        #endregion

        #region Properties
        public DelegateCommand RefreshPortsCommand { get; }
        public ObservableCollection<string> Parity { get; } = new ObservableCollection<string>() { "Even", "Mark", "None", "Odd", "Space" };
        public ObservableCollection<string> Handshake { get; } = new ObservableCollection<string>() { "None", "RequestToSend", "RequestToSendXOnXOff", "XOnXOff" };
        public ObservableCollection<string> StopBits { get; } = new ObservableCollection<string>() { "None", "One", "OnePointFive", "Two" };
        public DelegateCommand TurnListeningCommand { get; }
        public DelegateCommand ClearLogCommand { get; }
        public string StopBitsValue {
            get => port.StopBitsValue;
            set {
                if (port.StopBitsValue != value) {
                    port.StopBitsValue = value;
                    OnPropertyChanged();
                }
            }
        }
        public string HandshakeValue {
            get => port.HandshakeValue;
            set {
                if (port.HandshakeValue != value) {
                    port.HandshakeValue = value;
                    OnPropertyChanged();
                }
            }
        }
        public string ParityValue {
            get => port.ParityValue;
            set {
                if (port.ParityValue != value) {
                    port.ParityValue = value;
                    OnPropertyChanged();
                }
            }
        }
        public bool IsPortActive {
            get => port.Active;
            set {
                if (port.Active != value) {
                    port.Active = value;
                    OnPropertyChanged();
                }
            }
        }
        public string PortName {
            get => portName;
            set {
                if (portName != value) {
                    portName = value;
                    OnPropertyChanged();
                }
            }
        }
        public string ReceivedCalls {
            get => receivedCalls;
            set {
                if (receivedCalls != value) {
                    receivedCalls = value;
                    OnPropertyChanged();
                }
            }
        }
      
        public ObservableCollection<string> PortNames {
            get => portNames;
            set {
                if (portNames != value) {
                    portNames = value;
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
            get => port.DataBits;
            set {
                if (port.DataBits != value) {
                    port.DataBits = value;
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
        #endregion

        public ComCaptureViewModel() : base(FileExtension.Txt)
        {
            port = new Port();
            OutputFolder = Path.Combine(Path.GetTempPath(), "BsC_Recordings");
            if (!Directory.Exists(OutputFolder))
                Directory.CreateDirectory(OutputFolder);
            RefreshPortsCommand = new DelegateCommand(refreshPorts);
            TurnListeningCommand = new DelegateCommand(turnListening);
            ClearLogCommand = new DelegateCommand(clearLog);
            resolverTimer = new Timer(50);
            resolverTimer.Elapsed += dataResolver;
            resolverTimer.AutoReset = true;
            refreshPorts();
        }
        private void clearLog()
        {
            ReceivedCalls = string.Empty;
        }

        private void turnListening()
        {
            if (!IsPortActive) {
                try {
                    if (PortName == null)
                        throw new Exception("Please choose valid Port");
                    sp.PortName = PortName;
                    sp.BaudRate = PortBitRate;
                    sp.Parity = (Parity) Enum.Parse(typeof(Parity), ParityValue);
                    sp.DataBits = DataBits;
                    sp.StopBits = (StopBits) Enum.Parse(typeof(StopBits), StopBitsValue);
                    sp.Handshake = (Handshake) Enum.Parse(typeof(Handshake), HandshakeValue);
                    sp.DtrEnable = IsDtr;
                    sp.DataReceived += new SerialDataReceivedEventHandler(DataReceivedHandler);
                    sp.Open();
                    resolverTimer.Enabled = true;

                } catch (Exception e) {
                    MessageBox.Show(e.Message);
                }
            } else if (sp.IsOpen) {
                sp.Close();
                resolverTimer.Enabled = false;
            }
            IsPortActive = sp.IsOpen ? true : false;
        }

        private void writeTextToFile(string text)
        {
            if (currentFileName != string.Empty) {
                currentFileName += " " + String.Format("{0:dd.MM.yyy - HH-mm-ss.fff}.txt", DateTime.Now);
                if (!File.Exists(OutputFolder + "\\" + currentFileName)) {
                    File.WriteAllText(OutputFolder + "\\" + currentFileName, text);
                }
                currentFileName = string.Empty;
            }
        }

        private void dataResolver(Object source, ElapsedEventArgs e)
        {
            while (comPortTemp != string.Empty) {
                var regexResult = messageRegex.Match(comPortTemp);
                if (!regexResult.Success)
                    break;
                if (comPortTemp.Length > regexResult.Index + regexResult.Length + 1) {
                    comPortTemp = string.Empty;
                } else {
                    comPortTemp = comPortTemp.Substring(regexResult.Index + regexResult.Length + 1);
                }
                string[] m = regexResult.Value.Replace("\r", "").Split('\n');
                string result = string.Empty;
                foreach (string s in m) {
                    if (s != string.Empty && !s.Contains('>')) {
                        if (s.Contains('=')) {
                            var s2 = s.Replace(" ", "").Split('=');
                            if (s2[0] == "Nature") {
                                s2[1] = ddr.ResolveDistressCode(s2[1]);
                            }
                            if (s2[0] == "Eos") {
                                s2[1] = ddr.ResolveEndOfSequence(s2[1]);
                            }
                            if (s2[0] == "Cat") {
                                s2[1] = ddr.ResolveCategory(s2[1]);
                            }
                            if (s2[0] == "Pos") {
                                s2[1] = ddr.ResolveCategory(s2[1]);
                                string[] s3 = s2[1].Split(',');
                                Services.MessengerHub.PublishAsync<GeoMessage>(new GeoMessage(this, s3[0], s3[1]));
                            }
                            result += s2[0] + ": " + s2[1] + '\n';
                        } else {
                            var s2 = s.Split(' ');
                            if (s.Contains("Incoming")) {
                                result += "Type: " + s2[1] + '\n';
                                currentFileName += s2[1];
                            } else {
                                result += s2[0] + ": " + s2[1] + '\n';
                            }
                        }
                    }
                }
                writeTextToFile(result);
                ReceivedCalls += result + "------------------------------\n";
            }
        }

        private void DataReceivedHandler(object sender, SerialDataReceivedEventArgs e)
        {
            SerialPort sp = (SerialPort) sender;
            string indata = sp.ReadExisting();
            comPortTemp += indata;
        }

        private void refreshPorts()
        {
            using (var searcher = new ManagementObjectSearcher("SELECT * FROM WIN32_SerialPort")) {
                string[] portnames = SerialPort.GetPortNames();
                var x = searcher.Get().Cast<ManagementBaseObject>().ToList();
                PortNames = new ObservableCollection<string>((from n in portnames join p in x on n
                                                          equals p["DeviceID"].ToString() 
                                                          into np select n ));
            }
        }
    }
}
