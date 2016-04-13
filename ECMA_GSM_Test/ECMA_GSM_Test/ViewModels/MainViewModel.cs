using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.ComponentModel;
using System.Windows.Input;
using System.Threading;
using System.IO.Ports;
using System.Collections.ObjectModel;

namespace ECMA_GSM_Test.ViewModels
{
    class MainViewModel : INotifyPropertyChanged
    {
        #region Properties
        public ICommand StartCommand { get; set; }
        public ICommand RefreshSerialPortsCommand { get; set; }
        public ICommand StopCommand { get; set; }

        Timer timer = null;


        public event PropertyChangedEventHandler PropertyChanged;

        public void OnPropertyChanged(PropertyChangedEventArgs e)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, e);
        }



        CancellationTokenSource cancellationTokensource;
        SerialPort serialPort;

        private bool recordOn;
        public bool RecordOn
        {
            get { return recordOn; }
            set
            {
                if (recordOn != value)
                {
                    recordOn = value;
                    OnPropertyChanged(new PropertyChangedEventArgs("RecordOn"));
                }
            }
        }

        private string selectedSerialPort;
        public string SelectedSerialPort
        {
            get { return selectedSerialPort; }
            set
            {
                if (selectedSerialPort != value)
                {
                    selectedSerialPort = value;
                    OnPropertyChanged(new PropertyChangedEventArgs("SelectedSerialPort"));
                }
            }
        }

        private bool canStart;
        public bool CanStart
        {
            get { return canStart; }
            set
            {
                if (canStart != value)
                {
                    canStart = value;
                    OnPropertyChanged(new PropertyChangedEventArgs("CanStart"));
                    OnPropertyChanged(new PropertyChangedEventArgs("CanCancel"));
                }
            }
        }

        public bool CanCancel { get { return !canStart; } }

        public ObservableCollection<string> SerialPorts
        {
            get;
            private set;
        }

        private string resultText;
        public string ResultText
        {
            get { return resultText; }
            set
            {
                if (resultText != value)
                {
                    resultText = value;
                    OnPropertyChanged(new PropertyChangedEventArgs("ResultText"));
                }
            }
        }
        #endregion



        public MainViewModel()
        {
            SerialPorts = new ObservableCollection<string>();
            RecordOn = false;
            CanStart = true;
        }

        public void InitialiseSerialPorts(string lastSelectedSerialPort)
        {
            var serialPorts = SerialPort.GetPortNames();
            SerialPorts.Clear();
            foreach (var port in serialPorts) { SerialPorts.Add(port); }
            if (SerialPorts.Count(p => p == lastSelectedSerialPort) > 0) SelectedSerialPort = lastSelectedSerialPort;
            else SelectedSerialPort = SerialPorts.FirstOrDefault();
        }
       

        public void StartService(object o)
        {
            ResultText += "Starting service";
            timer = new Timer(_ => { RecordOn = !RecordOn; }, null, 0, 1000);
        }



        public void StopService(object o)
        {
            cancellationTokensource.Cancel();
            ResultText += "Stopping Service";
            timer.Dispose();
        }


        public void RefreshSerialPorts(object o)
        {
            InitialiseSerialPorts(null);
        }



    }
}
