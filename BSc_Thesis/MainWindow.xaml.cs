using BSc_Thesis.Models;
using BSc_Thesis.ViewModels;
using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Management;
using System.Text;
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
        List<Port> ports;
        SerialPort SP1 = new SerialPort();
        public MainWindow()
        {
            InitializeComponent();
            getPorts();
            var porto = SerialPort.GetPortNames();
            Combo.ItemsSource = ports;
            Combo.SelectedValuePath = "Name";
        }

        private void getPorts()
        {
            using (var searcher = new ManagementObjectSearcher("SELECT * FROM WIN32_SerialPort"))
            {
                string[] portnames = SerialPort.GetPortNames();
                var x = searcher.Get().Cast<ManagementBaseObject>().ToList();
                ports = (from n in portnames
                         join p in x on n equals p["DeviceID"].ToString() into np
                         from p in np.DefaultIfEmpty()
                         select new Port() { Name = n, Desc = p != null ? p["Description"].ToString() : "Brak Opisu" }).ToList();
            }

        }
        private void ChangeComListening(object sender, RoutedEventArgs e)
       {
            try
            {
                SP1.PortName = "COM3";
                SP1.BaudRate = 9600;
                SP1.Parity = Parity.None;
                SP1.DataBits = 8;
                SP1.StopBits = StopBits.One;
                SP1.Handshake = Handshake.RequestToSend;
                SP1.DtrEnable = true;
                SP1.DataReceived += new SerialDataReceivedEventHandler(DataReceivedHandler);
                SP1.Open();
            }
            catch (Exception e2)
            {
                textBox.Text = e2.Message;
            }

        }

        private void addText(string data)
        {
            textBox.Text += data;
        }

        private void DataReceivedHandler(object sender, SerialDataReceivedEventArgs e)
        {
            SerialPort sp = (SerialPort)sender;
            string indata = sp.ReadExisting();
            //Console.WriteLine("Data Received:");
            //Console.Write(indata);
            Application.Current.Dispatcher.Invoke(System.Windows.Threading.DispatcherPriority.Normal, new Action(() => textBox.Text += indata ));
        }

    }
}
