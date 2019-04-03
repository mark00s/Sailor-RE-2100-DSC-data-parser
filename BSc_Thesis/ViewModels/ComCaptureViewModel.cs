using BSc_Thesis.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Management;
using System.Text.RegularExpressions;
using System.Timers;
using System.Windows;

namespace BSc_Thesis.ViewModels
{
    class ComCaptureViewModel : FileManagerViewModel, IOpenable
    {
        #region Fields
        private int portBitRate = 4800;
        private SerialPort SP1 = new SerialPort();
        private bool isDtr = true;
        private ObservableCollection<string> portNames;
        private Port portValue;
        private Port port;
        private string comPortTemp = String.Empty;
        private Regex messageRegex = new Regex(@"Incoming[\w\W]+?> \?");
        private string comPortLog;
        private string receivedCalls;
        private string currentFileName;
        private Timer resolverTimer;
        #endregion

        #region Properties

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
        /// <summary>
        ///---------------------------------------------------------------
        /// </summary>
        /// 
        public Port PortValue {
            get => portValue;
            set {
                if (portValue != value) {
                    portValue = value;
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

        public string ReceivedCalls {
            get => receivedCalls;
            set {
                if (receivedCalls != value) {
                    receivedCalls = value;
                    OnPropertyChanged();
                }
            }
        }
      
        public DelegateCommand RefreshPortsCommand { get; }
        public DelegateCommand OpenCommand { get; }
        public ObservableCollection<string> Parity { get; } = new ObservableCollection<string>() { "Even", "Mark", "None", "Odd", "Space" };
        public ObservableCollection<string> Handshake { get; } = new ObservableCollection<string>() { "None", "RequestToSend", "RequestToSendXOnXOff", "XOnXOff" };
        public ObservableCollection<string> StopBits { get; } = new ObservableCollection<string>() { "None", "One", "OnePointFive", "Two" };
        public DelegateCommand TurnListeningCommand { get; }
        public DelegateCommand ClearLogCommand { get; }

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
            resolverTimer.Enabled = true;
            OpenCommand = new DelegateCommand(Open);
            refreshPorts();
        }

        public void Open()
        {
            if (SelectedFile != null)
                Process.Start(Path.Combine(OutputFolder, SelectedFile));
        }

        private void clearLog()
        {
            ReceivedCalls = string.Empty;
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
            IsPortActive = SP1.IsOpen ? true : false;
        }

        private void dataResolver(Object source, ElapsedEventArgs e)
        {
            while (true) {
                var r = messageRegex.Match(comPortTemp);
                if (!r.Success)
                    break;
                if (comPortLog.Length > r.Index + r.Length + 1) {
                    comPortTemp = string.Empty;
                } else {
                    comPortTemp = comPortTemp.Substring(r.Index + r.Length + 1);
                }
                string[] m = r.Value.Replace("\r", "").Split('\n');
                string result = string.Empty;
                foreach (string s in m) {
                    if (s != string.Empty && !s.Contains('>')) {
                        if (s.Contains('=')) {
                            var s2 = s.Replace(" ", "").Split('=');
                            if (s2[0] == "Nature") {
                                s2[1] = resolveDistressCode(s2[1]);
                            }
                            if (s2[0] == "Eos") {
                                s2[1] = resolveEndOfSequence(s2[1]);
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
                if (currentFileName != string.Empty) {
                    currentFileName += " " + String.Format("{0:dd.MM.yyy - HH-mm-ss.fff}.txt", DateTime.Now);
                    if (!File.Exists(OutputFolder + "\\" + currentFileName)) {
                        File.WriteAllText(OutputFolder + "\\" + currentFileName, result);
                    }
                    currentFileName = string.Empty;
                }
                ReceivedCalls += result + "------------------------------\n";
            }
        }

        private void DataReceivedHandler(object sender, SerialDataReceivedEventArgs e)
        {
            SerialPort sp = (SerialPort) sender;
            string indata = sp.ReadExisting();
            Application.Current.Dispatcher.Invoke(System.Windows.Threading.DispatcherPriority.Normal, new Action(() => ComPortLog += indata));
            comPortTemp += indata;
        }

        private string resolveEndOfSequence(string code)
        {
            if (code == "117")
                return "RQ Acknowledge required";
            else if (code == "122")
                return "BQ Acknowledge respond";
            else if (code == "127")
                return "Other calls";
            return code;
        }

        private string resolveDistressCode(string code)
        {
            switch (code) {
                case "100":
                    return "Fire, explosion";
                case "101":
                    return "Flooding";
                case "102":
                    return "Colision";
                case "103":
                    return "Grounding";
                case "104":
                    return "Listing, capsizing";
                case "105":
                    return "Sinking";
                case "106":
                    return "Disable and adrift";
                case "107":
                    return "Undesined distress";
                case "108":
                    return "Abandoning ship";
                case "112":
                    return "EPIRB emision";
            }
            return code;
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
