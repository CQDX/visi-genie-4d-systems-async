// Copyright(c) 2016 Michael Dorough
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Windows.Devices.SerialCommunication;

using ViSiGenie4DSystems.Async.Enumeration;

namespace ViSiGenie4DSystems.Async.SerialComm
{
    public class PortDef
    {
        /// <summary>
        /// A container for the client app to specify its serial communications protocol.
        /// 
        /// This class is used by the Host singleton class in the method:
        /// 
        ///     public async Task Connect(string deviceId, PortDef portDef)
        /// 
        /// 
        /// </summary>
        /// <param name="baudRate"></param>
        /// <param name="serialParity"></param>
        /// <param name="serialStopBitCount"></param>
        /// <param name="dataBits"></param>
        public PortDef(BaudRate baudRate, SerialParity serialParity = SerialParity.None, SerialStopBitCount serialStopBitCount = SerialStopBitCount.One, ushort dataBits = 8)
        {
            this.BaudRate = baudRate;
            this.SerialParity = serialParity;
            this.SerialStopBitCount = serialStopBitCount;
            this.DataBits = dataBits;
        }

        public BaudRate BaudRate { get; set; }

        public SerialParity SerialParity { get; set; }

        public SerialStopBitCount SerialStopBitCount { get; set; }

        public ushort DataBits { get; set; }
    }
}
