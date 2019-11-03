// Copyright (c) 2016 Michael Dorough
using System;
using System.Diagnostics;
using System.Text;
using ViSiGenie4DSystems.Async.Enumeration;
using ViSiGenie4DSystems.Async.Specification;


namespace ViSiGenie4DSystems.Async.Message
{
    /// <summary>
    /// Per Visi-Genie Reference Manual, Paragraph 3.1.3.7 Report Event Message
    /// Document Date: 20th May 2015 Document Revision: 1.11
    /// 
    /// When designing the Genie display application in Workshop, each Object can be
    /// configured to report its status change without the host having to poll it(see Read Object
    /// Status message). If the object’s ‘Event Handler’ is set to ‘Report Event’ in the ‘Event’ tab,
    /// the display will transmit the object’s status upon any change.For example, Slider 3 object
    /// was set from 0 to 50 by the user.
    /// 
    /// Reference: http://www.4dsystems.com.au/productpages/ViSi-Genie/downloads/Visi-Genie_refmanual_R_1_11.pdf
    /// </summary>
    public class ReportEventMessage
        : ReadMessage,
          IReportEventMessage,
          ICalculateChecksum,
          IByteOrder<uint>,
          IToHexString,
          IDebug
    {
        private ReportEventMessage()
        {
            this.Checksum = 0;
            this.Command = Command.ReportEvent;
        }

        public ReportEventMessage(byte[] message)
            : this()
        {
            this.ObjectType = (ObjectType)message[1];
            this.ObjectIndex = (int)message[2];
            this.Msb = (uint)message[3];
            this.Lsb = (uint)message[4];
            this.Checksum = (uint)message[5];
        }

        /// <summary>
        /// This byte indicates the command code. Some commands will have more parameters than
        /// others.The table below outlines the available commands and their relevant parameters.
        /// </summary>
        public Command Command { get; set; }

        /// <summary>
        ///  Identifies its kind 
        /// </summary>
        public ObjectType ObjectType { get; set; }

        /// <summary>
        ///  Differentiate between objects of the same kind.
        /// </summary>
        public int ObjectIndex { get; set; }

        /// <summary>
        /// The most significant byte of the value to be sent to meter 
        /// </summary>
        public uint Msb { get; set; }

        /// <summary>
        /// The least significant byte of the value to be sent to meter 
        /// </summary>
        public uint Lsb { get; set; }

        /// <summary>
        /// Combines the MSB and LSB into one word.
        /// </summary>
        /// <param name="reportValue"></param>
        public void PackBytes(uint reportValue)
        {
            this.Lsb = reportValue  & 0xFF;
            this.Msb = (reportValue >> 8) & 0xFF;
        }

        public uint Checksum { get; set; }

        /// <summary>
        /// Computes check sum of this data structure
        /// </summary>
        /// <returns></returns>
        public uint CalculateChecksum()
        {
            uint workingChecksum = (uint)this.Command;

            workingChecksum ^= (uint)this.ObjectType;

            workingChecksum ^= (uint)this.ObjectIndex;

            workingChecksum ^= this.Msb;

            workingChecksum ^= this.Lsb;

            return workingChecksum;
        }

        #region IMPLEMENTATION OF ABSTRACT METHODS
        public override byte[] ToByteArray()
        {
            this.Checksum = this.CalculateChecksum();

            byte[] bytes = new byte[6];

            bytes[0] = Convert.ToByte(this.Command);
            bytes[1] = Convert.ToByte(this.ObjectType);
            bytes[2] = Convert.ToByte(this.ObjectIndex);
            bytes[3] = Convert.ToByte(this.Msb);
            bytes[4] = Convert.ToByte(this.Lsb);
            bytes[5] = Convert.ToByte(this.Checksum);

            return bytes;
        }
        #endregion

        public string ToHexString()
        {
            StringBuilder sb = new StringBuilder();
            byte[] bytes = this.ToByteArray();
            foreach (var b in bytes)
            {
                sb.Append($"0x{b:X2}");
            }
            return sb.ToString();
        }

        public override string ToString()
        {
            string valueUtf8 = ConvertToUtf8(this.ToByteArray());
            return valueUtf8;
        }

        private string ConvertToUtf8(byte[] bytes)
        {
            return System.Text.Encoding.UTF8.GetString(bytes);
        }

        public void Write()
        {
            Debug.Write($"ReportEventMessage {ToHexString()}");
        }

        public void WriteLine()
        {
            Debug.WriteLine($"ReportEventMessage {ToHexString()}");
        }
    }
}
