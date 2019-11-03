// Copyright(c) 2016 Michael Dorough
using ViSiGenie4DSystems.Async.Enumeration;
using Windows.Devices.SerialCommunication;

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
        /// The Genie Standard Protocol provides a simple yet effective interface between the display and the host
        /// controller and all communications are reported over this bidirectional link. The protocol utilises only a handful
        /// of commands and is simple and easy to implement.
        /// 
        /// Serial data settings are:
        /// 8 Bits, No Parity, 1 Stop Bit.
        /// 
        /// Note: RS-232 handshaking signals (i.e., RTS, CTS, DTR, and DSR) are not supported by the ViSi-Genie protocols.
        /// Instead, only the RxD(received data), TxD(transmitted data), and signal ground are used.
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

        /// <summary>
        /// The baud rate for the display is selected from the Workshop Genie project. The user should match the same
        /// baud rate on the host side.
        /// </summary>
        public BaudRate BaudRate { get; set; }

        public SerialParity SerialParity { get; set; }

        public SerialStopBitCount SerialStopBitCount { get; set; }

        public ushort DataBits { get; set; }
    }
}
