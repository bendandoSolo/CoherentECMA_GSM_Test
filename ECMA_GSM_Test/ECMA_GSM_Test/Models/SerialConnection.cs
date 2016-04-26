

//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using log4net;
//using Core.Utilities;
//using System.Threading;
//using System.Diagnostics;

using System.IO.Ports;

namespace ECMA_GSM_Test.Models
{
    public enum ModemResponse
    {
        None,
        OK,
        Connect,
        NoDialtone,
        NoCarrier,
        Busy,
        Error,
        PasswordChallenge
    }

    //we have modified this class to no longer be static as we have two different serial connections that we need to monitor for this test.
    public class SerialConnection
    {
        public const Parity DefaultParity = Parity.None;
        public const int DefaultDataBits = 8;
        public const StopBits DefaultStopBits = StopBits.One;
        public int BaudRate; //115200;


        //constructor
        public SerialConnection(int baudRate) {
            this.BaudRate = baudRate;
        }

        //send AT and get response.

        //send AT and await response giving a timeout...
    }


}
