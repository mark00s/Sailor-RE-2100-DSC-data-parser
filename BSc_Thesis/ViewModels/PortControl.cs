﻿using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BSc_Thesis.ViewModels
{
    class PortControl
    {
        SerialPort SP;

        public PortControl(string portName, int baudRate, Parity parity, int dataBits, StopBits stopbits, Handshake handshake, bool dtrEnable)
        {
            SP = new SerialPort(portName);
            SP.BaudRate = baudRate;
            SP.Parity = parity;
            SP.DataBits = dataBits;
            SP.StopBits = stopbits;
            SP.Handshake = handshake;
            SP.DtrEnable = dtrEnable;
        }

        public void openPort()
        {
            try
            {
                SP.Open();
            }
            catch (IOException e)
            {
                throw;
            }
        }

    }
}