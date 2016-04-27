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
        SerialPort OutgoingPSDNSerialPort;
        SerialPort GPRSModemSerialPort;

        private bool recordOn;
        public bool RecordOn{
            get { return recordOn; }
            set{
                if (recordOn != value){
                    recordOn = value;
                    OnPropertyChanged(new PropertyChangedEventArgs("RecordOn"));}
            }
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
            if (SerialPorts.Count(p => p == lastSelectedSerialPort) > 0) SelectedSerialPort = lastSelectedSerialPort;
            else SelectedSerialPort = SerialPorts.FirstOrDefault();
        }
     
        public async void StartService(object o){
            ResultText = "Starting service";
            try {//we are currently using GSM Modems 
                OutgoingPSDNSerialPort = new SerialPort("COM2", 115200, Parity.None, 8, StopBits.One);
                OutgoingPSDNSerialPort.Open();
                OutgoingPSDNSerialPort.DtrEnable = true;
                GPRSModemSerialPort = new SerialPort("COM100", 115200, Parity.None, 8, StopBits.One);
                GPRSModemSerialPort.Open();
                GPRSModemSerialPort.DtrEnable = true;
                CanStart = false;
                cancellationTokensource = new CancellationTokenSource();
                CancellationToken ct = cancellationTokensource.Token;
                timer = new Timer(_ => { RecordOn = !RecordOn; }, null, 0, 1000);

                await Task.Run(() => {
                Send("AT", OutgoingPSDNSerialPort);
                if (AwaitSerialResponse(5000) != "OK") {
                    ResultText += "Error connecting to outgoing modem.\n";
                    StopService(this);
                }
                else {
                    ResultText += "PSDN responding";
                    Send("AT", GPRSModemSerialPort);
                    if (AwaitSerialResponse(5000, false) != "OK") {
                        ResultText += "Error connecting to GSM modem.\n";
                        StopService(this);
                    }
                    else {
                        ResultText += "GSM Modem responding\n";
                        Send("ATD07468761662", OutgoingPSDNSerialPort);
                        string response = AwaitSerialResponse(50000).ToUpper();
                        if (response != "CONNECT") {
                            ResultText += "failed to connect";
                            ClosePorts();
                        }
                        else {
                            ResultText += "CONNECTED Beginning Test" + "\n";
                            startOfTest = DateTime.Now;
                            int FirstModemTimesSent = 0;
                            int SecondModemTimesSent = 0;
                            int FirstModemTimesRecieved = 0;
                            int SecondModemTimesRecieved = 0;
                            int FirstModemTimesRecievedNothing = 0;
                            int SecondModemTimesRecievedNothing = 0;

                            OutgoingPSDNSerialPort.DiscardInBuffer();
                            GPRSModemSerialPort.DiscardInBuffer();
                            while (true) {
                                try {
                                   
                                    Send("The quick brown fox jumps over the lazy dog", OutgoingPSDNSerialPort);//we send from 
                                        Thread.Sleep(900);
                                        response = AwaitSerialResponse(1000, false);
                                        ResultText += response;
                                    switch (response) //we check for results from second modem
                                    {
                                        case "The quick brown fox jumps over the lazy dog":
                                            SecondModemTimesRecieved++;
                                            ResultText += "This is correct";
                                            break;
                                        case "NO RESPONSE":
                                            SecondModemTimesRecievedNothing++;
                                            break;
                                        default:
                                            break;
                                    }
                                    FirstModemTimesSent++;
   
                                    Send("The quick brown fox jumps over the lazy dog", GPRSModemSerialPort);//we send from 
                                        Thread.Sleep(900);
                                        response = AwaitSerialResponse(1000, true);
                                        ResultText += response;
                                        switch (response ) //we check for results from second modem
                                    {
                                        case "The quick brown fox jumps over the lazy dog":
                                            FirstModemTimesRecieved++;
                                            ResultText += "This is correct";
                                            break;
                                        case "NO RESPONSE":
                                            FirstModemTimesRecievedNothing++;
                                            break;
                                        default:
                                            break;
                                    }
                                    SecondModemTimesSent++;
                                }
                                catch (Exception e)
                                {
                                    ResultText += "\n\nError: " + e;
                                    cancellationTokensource.Cancel();
                                }


                                if (ct.IsCancellationRequested) {
                                    ResultText += "we are cancelling the test\n";
                                        if (OutgoingPSDNSerialPort.IsOpen && GPRSModemSerialPort.IsOpen) { ClosePorts(); }
                                        DisplayResults(FirstModemTimesSent,SecondModemTimesSent, FirstModemTimesRecieved,
                                         SecondModemTimesRecieved,FirstModemTimesRecievedNothing, SecondModemTimesRecievedNothing);
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
                ResultText += "CANNOT OPEN SERIAL PORT\n";
                CanStart = true;
            }
        }

        public void StopService(object o){
            cancellationTokensource.Cancel();
            //ClosePorts();
        }

        public void RefreshSerialPorts(object o){
            InitialiseSerialPorts(null);
        }

        private void DisplayResults(int FirstModemTimesSent,int SecondModemTimesSent,int FirstModemTimesRecieved,int SecondModemTimesRecieved,
            int FirstModemTimesRecievedNothing,int SecondModemTimesRecievedNothing)
        {
            ResultText = "RESULTS\n";

            ResultText += "FirstModem Sent " + FirstModemTimesSent + " times, recieved correctly " + FirstModemTimesRecieved + " times, recieved nothing: " + FirstModemTimesRecievedNothing + "\n";
            ResultText += "SecondModem Sent " + SecondModemTimesSent + " times, recieved correctly " + SecondModemTimesRecieved + " times, recieved nothing: " + SecondModemTimesRecievedNothing + "\n";
            

            //DateTime ellapsedTime = startOfTest - DateTime.Now;

            ResultText += "Start Time: " + startOfTest + "End Time " + DateTime.Now + "\n";
            if (DateTime.Now.Subtract(startOfTest).TotalHours > 0)
                ResultText += Math.Truncate(DateTime.Now.Subtract(startOfTest).TotalHours) + " hours, " + Math.Truncate((DateTime.Now.Subtract(startOfTest).TotalMinutes) % 60) + " minutes";
            else
            {
                if ((DateTime.Now.Subtract(startOfTest).TotalMinutes) % 60 > 0)
                    ResultText +=  Math.Truncate((DateTime.Now.Subtract(startOfTest).TotalMinutes) % 60) + " minutes";
            }

        }



        private void ClosePorts()
        {
            OutgoingPSDNSerialPort.Close();
            GPRSModemSerialPort.Close();
            CanStart = true;
            ResultText += "Stopping Service\n";
            timer.Dispose();
        }

        #region serialCommunications

        protected void Send(string sentText, SerialPort serialPortToSend){
            ResultText += "sending..." + sentText + "\n";
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
            int n = OutgoingPSDNSerialPort.Read(serialPortReadByteArray, 0, serialPortReadByteArray.Length); //read to byte array
            data.AddRange(serialPortReadByteArray.Take(n));
            string response = ASCIIEncoding.ASCII.GetString(data.ToArray(), 0, data.Count);
            Regex.Replace(response, "\x63", String.Empty);
            ResultText += "response from serial port: " + response;
        }



        //by default psdn...
        private string AwaitSerialResponse(int MaximumWaitTimeMs, bool IsPSDN = true){
            string responseToCommand = null;
            int EllapsedMs = 0;
            byte[] serialPortReadByteArray = new byte[20000];
            var data = new List<byte>();
            SerialPort serialPortWeAreInterstedIn = GPRSModemSerialPort;
            if (IsPSDN)
                serialPortWeAreInterstedIn = OutgoingPSDNSerialPort;
            while (MaximumWaitTimeMs > EllapsedMs)
            {
                if (serialPortWeAreInterstedIn.BytesToRead > 0){
                    Thread.Sleep(10); //we wait another 1ms incase the buffer is still being written to..
                    int n = serialPortWeAreInterstedIn.Read(serialPortReadByteArray, 0, serialPortReadByteArray.Length); //read to byte array
                    data.AddRange(serialPortReadByteArray.Take(n));
                    responseToCommand = ASCIIEncoding.ASCII.GetString(data.ToArray(), 0, data.Count);
                    Regex.Replace(responseToCommand, "\x63", String.Empty);
                    return responseToCommand.Trim();
                }
                Thread.Sleep(10);
                EllapsedMs+=10;
            }
            return "NO RESPONSE";
        }

        #endregion
    }
}
