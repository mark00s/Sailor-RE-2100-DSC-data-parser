using BSc_Thesis.Models;
using System;
using System.Collections.ObjectModel;
using System.IO.Ports;
using System.Linq;
using System.Management;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace BSc_Thesis
{
    public partial class ComCaptureView : UserControl
    {
        static readonly Regex _regex = new Regex("[^0-9.-]+"); // Regex zezwalający na liczby
        public ComCaptureView()
        {
            InitializeComponent();
        }

        private void myUpDownControl_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = _regex.IsMatch(e.Text);
        }
    }
}
