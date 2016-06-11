// Copyright (c) 2016 Michael Dorough
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using ViSiGenie4DSystems.Async.Enumeration;
using ViSiGenie4DSystems.Async.Specification;

namespace ViSiGenie4DSystems.Async.Message
{
    /// <summary>
    /// Per Visi-Genie Reference Manual, 3.1.3.6 Report Object Status Message
    /// Document Date: 20th May 2015 Document Revision: 1.11
    ///  
    /// This is the response message from the Display after the Host issues the Read Object
    /// Status message. The Display will respond back with the 2 byte value for the specific item
    /// of that object.
    /// 
    /// Reference: http://www.4dsystems.com.au/productpages/ViSi-Genie/downloads/Visi-Genie_refmanual_R_1_11.pdf
    /// </summary>
    public class ReportObjectStatusMessage
        : ReadMessage,
          IReportObjectStatusMessage,
          ICalculateChecksum,
          IByteOrder<uint>,
          IToHexString,
          IDebug
    {
        /// <summary>
        /// Default constructor
        /// </summary>
        private ReportObjectStatusMessage()
        {
            this.Checksum = 0;
            this.Command = Command.REPORT_OBJ;
        }

        public ReportObjectStatusMessage(byte[] message)
            : this()
        {
            this.ObjectType = (ObjectType)message[1];
            this.ObjectIndex = (int)message[2];
            this.MSB = (uint)message[3];
            this.LSB = (uint)message[4];
            this.Checksum = (uint)message[5];
        }

        /// <summary>
        /// REPORT OBJECT Command Code
        /// </summary>
        public Command Command { get; set; }

        /// <summary>
        /// Object ID. Refer to Object ID table for the relevant codes
        /// </summary>
        public ObjectType ObjectType { get; set; }

        /// <summary>
        /// This byte specifies the index or the item number of the Object
        /// </summary>
        public int ObjectIndex { get; set; }

        /// <summary>
        /// Most significant byte of the 2 byte VALUE
        /// </summary>
        public uint MSB { get; set; }

        /// <summary>
        /// Least significant byte of the 2 byte VALUE
        /// </summary>
        /// </summary>
        public uint LSB { get; set; }

        /// <summary>
        /// Combines the MSB and LSB into one word.
        /// </summary>
        /// <param name="value"></param>
        public void PackBytes(uint value)
        {
            this.LSB = (value >> 0) & 0xFF;
            this.MSB = (value >> 8) & 0xFF;
        }

        /// <summary>
        /// Checksum byte
        /// </summary>
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

            workingChecksum ^= this.MSB;

            workingChecksum ^= this.LSB;

            return workingChecksum;
        }

        #region IMPLEMENTATION OF ABSTRACT METHODS

        /// <summary>
        /// Converts ReportObjectStatusMessage to byte array. 
        /// Checksum is add to last element in byte array.
        /// </summary>
        /// <returns></returns>
        override public byte[] ToByteArray()
        {
            this.Checksum = this.CalculateChecksum();

            byte[] bytes = new byte[6];

            bytes[0] = Convert.ToByte(this.Command);
            bytes[1] = Convert.ToByte(this.ObjectType);
            bytes[2] = Convert.ToByte(this.ObjectIndex);
            bytes[3] = Convert.ToByte(this.MSB);
            bytes[4] = Convert.ToByte(this.LSB);
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
                sb.Append(String.Format("0x{0}", b.ToString("X2")));
            }
            return sb.ToString();
        }

        /// <summary>
        /// Yields a string representation of ReportObjectStatusMessage
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            string valueUtf8 = ConvertToUtf8(this.ToByteArray());
            return valueUtf8;
        }

        /// <summary>
        /// Support String override implementation
        /// </summary>
        /// <param name="bytes"></param>
        /// <returns></returns>
        private string ConvertToUtf8(byte[] bytes)
        {
            return System.Text.Encoding.UTF8.GetString(bytes);
        }

        public void Write()
        {
            Debug.Write(String.Format("WriteObjectValueMessage {0}", ToHexString()));
        }

        public void WriteLine()
        {
            Debug.WriteLine(String.Format("WriteObjectValueMessage {0}", ToHexString()));
        }
    }
}
