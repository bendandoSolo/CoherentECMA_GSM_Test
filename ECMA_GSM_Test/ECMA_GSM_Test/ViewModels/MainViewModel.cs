﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;
using System.Windows.Input;
using System.Threading;
using System.IO.Ports;
using System.Collections.ObjectModel;
using System.Text.RegularExpressions;

namespace ECMA_GSM_Test.ViewModels
{
    class MainViewModel : INotifyPropertyChanged
    {
        #region Properties
        public ICommand StartCommand { get; set; }
        public ICommand RefreshSerialPortsCommand { get; set; }
        public ICommand StopCommand { get; set; }
        Timer timer = null;
        DateTime startOfTest;
        public event PropertyChangedEventHandler PropertyChanged;
        CancellationTokenSource cancellationTokensource;

        //initially we make a call from serial port 109 which is a CSDN Modem, at 9600
        //we dial our GPRS modem and COM100, and send and recieve data noting an errors.
        SerialPort FirstModemSerialPort;
        SerialPort SecondModemSerialPort;

        int FirstModemTimesSent = 0;
        int SecondModemTimesSent = 0;
        int FirstModemTimesRecieved = 0;
        int SecondModemTimesRecieved = 0;
        int FirstModemTimesRecievedIncorrectly = 0;
        int SecondModemTimesRecievedIncorrectly = 0;
        int FirstModemTimesRecievedNothing = 0;
        int SecondModemTimesRecievedNothing = 0;



        private bool recordOn;
        public bool RecordOn{
            get { return recordOn; }
            set{
                if (recordOn != value){
                    recordOn = value;
                    OnPropertyChanged(new PropertyChangedEventArgs("RecordOn"));}
            }
        }
    
        private readonly int[] baudRates = { 115200, 57600, 38400, 19200, 9600, 4800, 2400, 1200, 600, 300 };

        public int[] BaudRates
        {
            get { return baudRates; }
        }

        private string selectedSerialPort;
        public string SelectedSerialPort{
            get { return selectedSerialPort; }
            set{
                if (selectedSerialPort != value){
                    selectedSerialPort = value;
                    OnPropertyChanged(new PropertyChangedEventArgs("SelectedSerialPort"));}
            }
        }

        //we need second selected serial ports...

        public string PhoneNumberTextBox
        {
            get;
            set;
        }

        public string InitialStringTextBox
        {
            get;
            set;
        }

        private string outgoingSerialPort;
        public string OutgoingSerialPort
        {
            get { return outgoingSerialPort; }
            set
            {
                if (outgoingSerialPort != value)
                {
                    outgoingSerialPort = value;
                    OnPropertyChanged(new PropertyChangedEventArgs("OutgoingSerialPort"));
                }
            }
        }


        private string gSMSerialPort;
        public string GSMSerialPort
        {
            get { return gSMSerialPort; }
            set
            {
                if (gSMSerialPort != value)
                {
                    gSMSerialPort = value;
                    OnPropertyChanged(new PropertyChangedEventArgs("GSMSerialPort"));
                }
            }
        }

        private int outgoingBaudRate;
        public int OutgoingBaudRate
        {
            get { return outgoingBaudRate; }
            set
            {
                if (outgoingBaudRate != value)
                {
                    outgoingBaudRate = value;
                    OnPropertyChanged(new PropertyChangedEventArgs("BaudRate"));
                }
            }
        }


        private int gSMBaudRate;
        public int GSMBaudRate
        {
            get { return gSMBaudRate; }
            set
            {
                if (gSMBaudRate != value)
                {
                    gSMBaudRate = value;
                    OnPropertyChanged(new PropertyChangedEventArgs("GSMBaudRate"));
                }
            }
        }


        private bool canStart;
        public bool CanStart{
            get { return canStart; }
            set{
                if (canStart != value){
                    canStart = value;
                    OnPropertyChanged(new PropertyChangedEventArgs("CanStart"));
                    OnPropertyChanged(new PropertyChangedEventArgs("CanCancel"));}
            }
        }

        public bool CanCancel { get { return !canStart; } }

        public ObservableCollection<string> SerialPorts{
            get;
            private set;
        }

        private string resultText;
        public string ResultText{
            get { return resultText; }
            set{
                if (resultText != value)
                {
                    resultText = value;
                    OnPropertyChanged(new PropertyChangedEventArgs("ResultText"));}
            }
        }
        #endregion

        public MainViewModel(){
            SerialPorts = new ObservableCollection<string>();
            RecordOn = false;
            CanStart = true;
        }

        public void OnPropertyChanged(PropertyChangedEventArgs e){
            if (PropertyChanged != null)
                PropertyChanged(this, e);
        }

        public void InitialiseSerialPorts(string lastSelectedSerialPort){
            var serialPorts = SerialPort.GetPortNames();
            SerialPorts.Clear();
            foreach (var port in serialPorts) { SerialPorts.Add(port); }
            if (SerialPorts.Count(p => p == lastSelectedSerialPort) > 0) OutgoingSerialPort = lastSelectedSerialPort;
            else
            {
                OutgoingSerialPort = SerialPorts.FirstOrDefault();
                GSMSerialPort = SerialPorts.FirstOrDefault();
            }
        }
     
        public void InitialiseBaudRates()
        {
            OutgoingBaudRate = BaudRates[0];
            GSMBaudRate = BaudRates[0];
        }


        public async void StartServiceTEST(object o)
        {
            try
            {
                SecondModemSerialPort = new SerialPort(GSMSerialPort, GSMBaudRate, Parity.None, 8, StopBits.One);
                SecondModemSerialPort.Open();
                SecondModemSerialPort.DtrEnable = true;
                CanStart = false;
                cancellationTokensource = new CancellationTokenSource();
                CancellationToken ct = cancellationTokensource.Token;
                timer = new Timer(_ => { RecordOn = !RecordOn; }, null, 0, 1000);

                await Task.Run(() =>
                {
                    Send("AT" + InitialStringTextBox, SecondModemSerialPort);
                    if (AwaitSerialResponse(5000, SecondModemSerialPort) != "OK")
                    {
                        ResultText += "Error connecting to outgoing modem.\n";
                        StopService(this);
                    }
                    else
                    {
                        ResultText = "we have a connection";
                        Thread.Sleep(30000);
                        while(true)
                        {
                            // string response = AwaitSerialResponse(2000, SecondModemSerialPort);
                            // ResultText += "my responses: " + response;
                            Send("The quick brown fox jumps over the lazy dog\n", SecondModemSerialPort, true);//we send from 
                            Thread.Sleep(5000);
                        }



                    }

                }, cancellationTokensource.Token);
            }
            catch (Exception e)
            {
                ResultText = e.ToString();
            }
       }

        public async void StartService(object o){
            try {
                FirstModemSerialPort = new SerialPort(OutgoingSerialPort, OutgoingBaudRate, Parity.None, 8, StopBits.One);
                FirstModemSerialPort.Open();
                FirstModemSerialPort.DtrEnable = true;
                FirstModemSerialPort.RtsEnable = true;
                SecondModemSerialPort = new SerialPort(GSMSerialPort, GSMBaudRate , Parity.None, 8, StopBits.One);
                SecondModemSerialPort.Open();
                SecondModemSerialPort.DtrEnable = true;
                SecondModemSerialPort.RtsEnable = true;
                CanStart = false;
                cancellationTokensource = new CancellationTokenSource();
                CancellationToken ct = cancellationTokensource.Token;
                timer = new Timer(_ => { RecordOn = !RecordOn; }, null, 0, 1000);

                await Task.Run(() => {
                Send("AT" + InitialStringTextBox, FirstModemSerialPort);
                if (AwaitSerialResponse(5000, FirstModemSerialPort) != "OK") {
                    ResultText += "Error connecting to outgoing modem.\n";
                    StopService(this);
                }
                else {
                    ResultText += "1st Modem responding to AT"+ InitialStringTextBox + " on " + FirstModemSerialPort.PortName + "\n";
                    Send("AT", SecondModemSerialPort);
                    if (AwaitSerialResponse(5000, SecondModemSerialPort) != "OK") {
                        ResultText += "Error connecting to 2nd modem.\n";
                        StopService(this);
                    }
                    else {
                        ResultText += "2nd Modem responding to AT" + " on " + SecondModemSerialPort.PortName + "\nDIALING ON " + FirstModemSerialPort.PortName + "\n";                           //07468761662
                            Send("ATD" + PhoneNumberTextBox, FirstModemSerialPort);
                        string response = AwaitSerialResponse(50000,FirstModemSerialPort).ToUpper();
                        if (!response.StartsWith("CONNECT")) {
                                if (response.Contains("NO CARRIER"))ResultText += "NO CARRIER";
                                else ResultText += "failed to connect error: " + response;
                            ClosePorts();
                        }
                        else {
                            ResultText += "CONNECTED BEGINING TEST" + "\n";
                            startOfTest = DateTime.Now;

                            FirstModemTimesSent = 0;
                            SecondModemTimesSent = 0;
                            FirstModemTimesRecieved = 0;
                            SecondModemTimesRecieved = 0;
                            FirstModemTimesRecievedIncorrectly = 0;
                            SecondModemTimesRecievedIncorrectly = 0;
                            FirstModemTimesRecievedNothing = 0;
                            SecondModemTimesRecievedNothing = 0;


                                FirstModemSerialPort.DiscardInBuffer();
                            SecondModemSerialPort.DiscardInBuffer();
                            while (true) {
                                try {
                                        SendFromFirstSerialPort();
                                        SendFromSecondSerialPort();

                                    }
                                catch (Exception e)
                                {
                                    ResultText += "\n\nError: " + e;
                                    cancellationTokensource.Cancel();
                                }

                                if (ct.IsCancellationRequested) {
                                    ResultText += "we are cancelling the test\n";
                                        if (FirstModemSerialPort.IsOpen || SecondModemSerialPort.IsOpen) { ClosePorts(); }
                                        DisplayResults(FirstModemTimesSent, SecondModemTimesSent, FirstModemTimesRecieved,
                                        SecondModemTimesRecieved, FirstModemTimesRecievedNothing, SecondModemTimesRecievedNothing, FirstModemTimesRecievedIncorrectly, SecondModemTimesRecievedIncorrectly);
                                        timer.Dispose();
                                                    break;
                                                }
                                            }
                                    }
                        }
                    }
                }, cancellationTokensource.Token);
            }
            catch (UnauthorizedAccessException ex){
                if (FirstModemSerialPort.IsOpen)
                ResultText += "CANNOT OPEN GSM SERIAL PORT\n";
                else
                    ResultText += "CANNOT OPEN OUTGOING SERIAL PORT\n";
                CanStart = true;
            }
        }


        public void StopService(object o){
            cancellationTokensource.Cancel();
            ClosePorts();
        }

        public void RefreshSerialPorts(object o){
            InitialiseSerialPorts(null);
        }

        private void DisplayResults(int FirstModemTimesSent,int SecondModemTimesSent,int FirstModemTimesRecieved,int SecondModemTimesRecieved,
            int FirstModemTimesRecievedNothing,int SecondModemTimesRecievedNothing,int FirstModemTimesRecievedIncorrectly,int SecondModemTimesRecievedIncorrectly)
        {
            ResultText = "TEST RESULTS:\n";
            ResultText += "Transmissions to 1st Modem:  " + FirstModemTimesSent + " Correct Responses: " + FirstModemTimesRecieved + " Incorrect Responses: " + FirstModemTimesRecievedIncorrectly + " Response Timeout: " + FirstModemTimesRecievedNothing + "\n";
            ResultText += "Transmissions to 2nd Modem: " + SecondModemTimesSent + " Correct Responses: " + SecondModemTimesRecieved + " Incorrect Responses: " + SecondModemTimesRecievedIncorrectly + " Response Timeout: " + SecondModemTimesRecievedNothing + "\n";
            ResultText += "Start Time: " + startOfTest + "    End Time " + DateTime.Now + "\n";
            if (DateTime.Now.Subtract(startOfTest).TotalHours > 0)
                ResultText += Math.Truncate(DateTime.Now.Subtract(startOfTest).TotalHours) + " hours, " + Math.Truncate((DateTime.Now.Subtract(startOfTest).TotalMinutes) % 60) + " minutes";
            else
            {
                if ((DateTime.Now.Subtract(startOfTest).TotalMinutes) % 60 > 0)
                    ResultText +=  Math.Truncate((DateTime.Now.Subtract(startOfTest).TotalMinutes) % 60) + " minutes\n";
            }
        }

        private void ClosePorts()
        {
            if (FirstModemSerialPort.IsOpen)
            FirstModemSerialPort.Close();
            if (SecondModemSerialPort.IsOpen)
            SecondModemSerialPort.Close();
            CanStart = true;
            ResultText += "\nSTOPPING SERVICE\n";
            timer.Dispose();
        }

        #region serialCommunications

        private void SendFromFirstSerialPort()
        {
            Send("The quick brown fox jumps over the lazy dog", FirstModemSerialPort, false);//we send from 
            switch (AwaitSerialResponse(5000, SecondModemSerialPort)) //we check for results from second modem
            {
                case "The quick brown fox jumps over the lazy dog":
                    SecondModemTimesRecieved++;
                    break;
                case "RESPONSE TIMEOUT":
                    SecondModemTimesRecievedNothing++;
                    break;
                default:
                    SecondModemTimesRecievedIncorrectly++;
                    break;
            }
            SecondModemTimesSent++;
            DisplayResults(FirstModemTimesSent, SecondModemTimesSent, FirstModemTimesRecieved,
            SecondModemTimesRecieved, FirstModemTimesRecievedNothing, SecondModemTimesRecievedNothing, FirstModemTimesRecievedIncorrectly, SecondModemTimesRecievedIncorrectly);
        }

        private void SendFromSecondSerialPort()
        {
            Send("The quick brown fox jumps over the lazy dog", SecondModemSerialPort, false);//we send from 
            switch (AwaitSerialResponse(5000, FirstModemSerialPort)) //we check for results from second modem
            {
                case "The quick brown fox jumps over the lazy dog":
                    FirstModemTimesRecieved++;
                    break;
                case "RESPONSE TIMEOUT":
                    FirstModemTimesRecievedNothing++;
                    break;
                default:
                    FirstModemTimesRecievedIncorrectly++;
                    break;
            }
            FirstModemTimesSent++;
                DisplayResults(FirstModemTimesSent, SecondModemTimesSent, FirstModemTimesRecieved,
                SecondModemTimesRecieved, FirstModemTimesRecievedNothing, SecondModemTimesRecievedNothing, FirstModemTimesRecievedIncorrectly, SecondModemTimesRecievedIncorrectly);
        }

        protected void Send(string sentText, SerialPort serialPortToSend, bool printToScreen = true){
            if(printToScreen)
            ResultText += "  sending..." + sentText + "\n";
            byte[] data = ASCIIEncoding.UTF8.GetBytes(sentText + '\r');
            serialPortToSend.Write(data, 0, data.Length);
        }
        
        //really we should refactor 
        private void DataRecieved(object sender, SerialDataReceivedEventArgs e ){
            resultText += "we have recieved a response?";
            SerialPort serialPort = (SerialPort)sender;
            Thread.Sleep(25);   //wait incase we have some data still being transmitted
            byte[] serialPortReadByteArray = new byte[1000];
            var data = new List<byte>();
            int n = FirstModemSerialPort.Read(serialPortReadByteArray, 0, serialPortReadByteArray.Length); //read to byte array
            data.AddRange(serialPortReadByteArray.Take(n));
            string response = ASCIIEncoding.ASCII.GetString(data.ToArray(), 0, data.Count);
            Regex.Replace(response, "\x63", String.Empty);
            ResultText += "response from serial port: " + response;
        }

        private string AwaitSerialResponse(int MaximumWaitTimeMs, SerialPort watchedSerialPort){
            string responseToCommand = null;
            int EllapsedMs = 0;
            byte[] serialPortReadByteArray = new byte[4096];
            var data = new List<byte>();
            while (MaximumWaitTimeMs > EllapsedMs)
            {
                if (watchedSerialPort.BytesToRead > 0){
                    Thread.Sleep(100); //we wait another 1ms incase the buffer is still being written to..
                    int n = watchedSerialPort.Read(serialPortReadByteArray, 0, serialPortReadByteArray.Length); //read to byte array
                    data.AddRange(serialPortReadByteArray.Take(n));
                    responseToCommand = ASCIIEncoding.ASCII.GetString(data.ToArray(), 0, data.Count);
                    Regex.Replace(responseToCommand, "\x63", String.Empty);
                    return responseToCommand.Trim();
                }
                Thread.Sleep(10);
                EllapsedMs+=10;
            }
            return "RESPONSE TIMEOUT";
        }

        #endregion
    }
}
