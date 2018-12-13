using BSc_Thesis.Models;
using BSc_Thesis.ViewModels;
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
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace BSc_Thesis
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        ObservableCollection<Port> ports;
        SerialPort SP1 = new SerialPort();
        static readonly Regex _regex = new Regex("[^0-9.-]+"); // Regex zezwalający na liczby

        ObservableCollection<string> parity = new ObservableCollection<string>() { "Even", "Mark", "None", "Odd", "Space" };
        ObservableCollection<string> handShake = new ObservableCollection<string>() { "None", "RequestToSend", "RequestToSendXOnXOff", "XOnXOff"};
        ObservableCollection<string> stopBits = new ObservableCollection<string>() { "None", "One", "OnePointFive", "Two"};

        public MainWindow()
        {
            InitializeComponent();
            getPorts();
            Combo.ItemsSource = ports;
            HandShakeCombo.ItemsSource = handShake;
            ParityCombo.ItemsSource = parity;
            StopBitsCombo.ItemsSource = stopBits;
        }

        private void getPorts()
        {
            using (var searcher = new ManagementObjectSearcher("SELECT * FROM WIN32_SerialPort"))
            {
                string[] portnames = SerialPort.GetPortNames();
                var x = searcher.Get().Cast<ManagementBaseObject>().ToList();
                ports = new ObservableCollection<Port>((from n in portnames
                         join p in x on n equals p["DeviceID"].ToString() into np
                         select new Port() { Name = n }));
            }

        }
        private void TurnOnComListening(object sender, RoutedEventArgs e)
       {
            try
            {
                if (Combo.SelectedIndex == -1 || HandShakeCombo.SelectedIndex == -1 || ParityCombo.SelectedIndex == -1 || StopBitsCombo.SelectedIndex == -1)
                    throw new Exception("Nie wybrano wartości ComboBox");
                SP1.PortName = Combo.SelectedItem.ToString();
                SP1.BaudRate = (int)bitrateUpDownControl.Value;
                SP1.Parity = (Parity) Enum.Parse(typeof(Parity), parity[ParityCombo.SelectedIndex]);
                SP1.DataBits = (int)databitsUpDownControl.Value;
                SP1.StopBits = (StopBits)Enum.Parse(typeof(StopBits), stopBits[StopBitsCombo.SelectedIndex]);
                SP1.Handshake = (Handshake)Enum.Parse(typeof(Handshake), handShake[HandShakeCombo.SelectedIndex]);
                SP1.DtrEnable = (bool)DtrCheckBox.IsChecked;
                SP1.DataReceived += new SerialDataReceivedEventHandler(DataReceivedHandler);
                SP1.Open();
            }
            catch (Exception e2)
            {
                textBox.Text = e2.Message;
            }
        }

        private void TurnOffComListening(object sender, RoutedEventArgs e)
        {
            if (SP1.IsOpen) SP1.Close();
        }

        private static bool IsTextAllowed(string text)
        {
            return !_regex.IsMatch(text);
        }

        private void DataReceivedHandler(object sender, SerialDataReceivedEventArgs e)
        {
            SerialPort sp = (SerialPort)sender;
            string indata = sp.ReadExisting();
            //Console.WriteLine("Data Received:");
            //Console.Write(indata);
            Application.Current.Dispatcher.Invoke(System.Windows.Threading.DispatcherPriority.Normal, new Action(() => textBox.Text += indata ));
        }

        private void myUpDownControl_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !IsTextAllowed(e.Text);
        }

        private void myUpDownControl_Pasting(object sender, DataObjectPastingEventArgs e)
        {
            if (e.DataObject.GetDataPresent(typeof(String)))
            {
                String text = (String)e.DataObject.GetData(typeof(String));
                if (!IsTextAllowed(text)) e.CancelCommand();
            }
            else
                e.CancelCommand();
        }

        private void RefreshPort_Click(object sender, RoutedEventArgs e)
        {
            getPorts();
            Combo.ItemsSource = ports;
        }
    }
}
