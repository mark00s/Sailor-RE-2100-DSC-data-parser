using BSc_Thesis.Models;
using System;
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
    class ComCaptureViewModel : ViewModelBase
    {
        #region Fields
        private int portBitRate = 4800;
        private int dataBits = 8;
        private SerialPort SP1 = new SerialPort();
        private bool isDtr = true;
        private ObservableCollection<Port> ports;
        private Port portValue;
        private string stopBitsValue;
        private string handshakeValue;
        private string parityValue;
        private string comPortTemp = String.Empty;
        private Regex messageRegex = new Regex(@"Incoming[\w\W]+?> \?");
        private string comPortLog;
        private string receivedCalls;
        private string selectedFile;
        private string currentFileName;
        private bool active = false;
        private Timer resolverTimer;
        private string outputFolder;
        private FileSystemWatcher watcher = new FileSystemWatcher();

        #endregion

        #region Properties
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

        public string SelectedFile
        {
            get => selectedFile;
            set {
                if (selectedFile != value) {
                    selectedFile = value;
                    OnPropertyChanged();
                }
            }
            
        }

        public string OutputFolder {
            get => outputFolder;
            set {
                if (outputFolder != value) {
                    outputFolder = value;
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
                
        public ObservableCollection<string> Parity { get; } = new ObservableCollection<string>() { "Even", "Mark", "None", "Odd", "Space" };
        public ObservableCollection<string> Handshake { get; } = new ObservableCollection<string>() { "None", "RequestToSend", "RequestToSendXOnXOff", "XOnXOff" };
        public ObservableCollection<string> StopBits { get; } = new ObservableCollection<string>() { "None", "One", "OnePointFive", "Two" };
        public DelegateCommand RefreshPortsCommand { get; }
        public DelegateCommand OpenCommand { get; }
        public DelegateCommand DeleteCommand { get; }
        public ObservableCollection<string> Files { get; }

        public DelegateCommand OpenFolderCommand { get; }
        public DelegateCommand SelectFolderCommand { get; }
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
        #endregion

        public ComCaptureViewModel()
        {
            Files = new ObservableCollection<string>();
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
            DeleteCommand = new DelegateCommand(Delete);
            OpenCommand = new DelegateCommand(Open);
            OpenFolderCommand = new DelegateCommand(OpenFolder);
            SelectFolderCommand = new DelegateCommand(SelectFolder);
            foreach (var file in Directory.GetFiles(OutputFolder))
                if (Path.GetExtension(file) == ".txt")
                    Files.Add(Path.GetFileName(file));
            watcher.Path = OutputFolder;
            watcher.Changed += new FileSystemEventHandler(OnChanged);
            watcher.Created += new FileSystemEventHandler(OnChanged);
            watcher.Deleted += new FileSystemEventHandler(OnChanged);
            watcher.Renamed += new RenamedEventHandler(OnRenamed);
            watcher.EnableRaisingEvents = true;
            refreshPorts();
        }

        private void OnChanged(object source, FileSystemEventArgs e)
        {
            Application.Current.Dispatcher.BeginInvoke(
                System.Windows.Threading.DispatcherPriority.Background,
                new Action(() => {
                    Files.Clear();
                    foreach (var file in Directory.GetFiles(OutputFolder))
                        if (Path.GetExtension(file) == ".txt")
                            Files.Add(Path.GetFileName(file));
                    OnPropertyChanged("Files");
                }));
        }

        private void Open()
        {
            if (selectedFile != null)
                Process.Start(Path.Combine(OutputFolder, SelectedFile));
        }

        private void SelectFolder()
        {
            System.Windows.Forms.FolderBrowserDialog Dialog = new System.Windows.Forms.FolderBrowserDialog();
            while (Dialog.ShowDialog() != System.Windows.Forms.DialogResult.OK) {
                Dialog.Reset();
            }
            OutputFolder = Dialog.SelectedPath;
            watcher.Path = OutputFolder;
            watcher.Changed += new FileSystemEventHandler(OnChanged);
            watcher.Created += new FileSystemEventHandler(OnChanged);
            watcher.Deleted += new FileSystemEventHandler(OnChanged);
            watcher.Renamed += new RenamedEventHandler(OnRenamed);
            watcher.EnableRaisingEvents = true;
            Application.Current.Dispatcher.BeginInvoke(
            System.Windows.Threading.DispatcherPriority.Background,
            new Action(() => {
                Files.Clear();
                foreach (var file in Directory.GetFiles(OutputFolder))
                    if (Path.GetExtension(file) == ".txt")
                        Files.Add(Path.GetFileName(file));
                OnPropertyChanged("Files");
            }));
        }

        private void OnRenamed(object source, RenamedEventArgs e)
        {
            Application.Current.Dispatcher.BeginInvoke(
            System.Windows.Threading.DispatcherPriority.Background,
            new Action(() => {
                Files.Clear();
                foreach (var file in Directory.GetFiles(OutputFolder))
                    if (Path.GetExtension(file) == ".txt")
                        Files.Add(Path.GetFileName(file));
                OnPropertyChanged("Files");
            }));
        }

        private void Delete()
        {
            if (SelectedFile != null) {
                try {
                    File.Delete(Path.Combine(OutputFolder, SelectedFile));
                    Files.Remove(SelectedFile);
                    SelectedFile = Files.FirstOrDefault();
                } catch (Exception) {
                    MessageBox.Show("Could not delete File");
                }
            }
        }

        private void OpenFolder()
        {
            Process.Start(OutputFolder);
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
            IsPortActive = SP1.IsOpen?true:false;
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
                Port = new ObservableCollection<Port>((from n in portnames
                                                       join p in x on n equals p["DeviceID"].ToString() into np
                                                       select new Port() { Name = n }));
            }
        }
    }
}
