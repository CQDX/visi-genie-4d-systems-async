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
    /// Per Visi-Genie Reference Manual, Paragraph 3.1.3.1 Read Object Status Message
    /// Document Date: 20th May 2015 Document Revision: 1.11
    /// 
    /// The host sends this message to the display to determine the current value of a 
    /// specific object instance.
    /// 
    /// Upon receipt of this message the display will reply with either a NAK
    /// (in the case of an error) or the REPORT_OBJ message
    /// (0x05, Object-ID, Object Index, Value {msb}, Value {lsb}, checksum). 
    /// </summary>
    public class ReadObjectStatusMessage
        : WriteMessage,
          IReadObjectStatusMessage,
          ICalculateChecksum,
          IToHexString,
          IDebug
    {
        /// <summary>
        /// Default constructor
        /// </summary>
        public ReadObjectStatusMessage()
        {
            this.Checksum = 0;
            this.Command = Command.READ_OBJ;
        }

        /// <summary>
        /// ReadObjectStatusMessage helper constructor
        /// </summary>
        /// <param name="objectType"></param>
        /// <param name="objectIndex"></param>
        public ReadObjectStatusMessage(ObjectType objectType, int objectIndex)
            : this()
        {
            this.ObjectType = objectType;
            this.ObjectIndex = objectIndex;
            this.Checksum = this.CalculateChecksum();
        }

        /// <summary>
        /// Copy some displayMessage to this
        /// </summary>
        /// <param name="otherReadObjectValueMessage"></param>
        public ReadObjectStatusMessage(ReadObjectStatusMessage otherReadObjectValueMessage)
        {
            this.Command = otherReadObjectValueMessage.Command;
            this.ObjectType = otherReadObjectValueMessage.ObjectType;
            this.ObjectIndex = otherReadObjectValueMessage.ObjectIndex;
            this.Checksum = this.CalculateChecksum();
        }

        public Command Command { get; set; }

        public ObjectType ObjectType { get; set; }

        public int ObjectIndex { get; set; }

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

            return workingChecksum;
        }

        #region IMPLEMENTATION OF ABSTRACT METHODS

        /// <summary>
        /// Converts ReadObjectStatusMessage to byte array. 
        /// Checksum is placed at last element in byte array.
        /// </summary>
        /// <returns></returns>
        override public byte[] ToByteArray()
        {
            this.Checksum = this.CalculateChecksum();

            byte[] bytes = new byte[6];

            bytes[0] = Convert.ToByte(this.Command);
            bytes[1] = Convert.ToByte(this.ObjectType);
            bytes[2] = Convert.ToByte(this.ObjectIndex);

            bytes[3] = Convert.ToByte(this.Checksum);

            return bytes;
        }
        #endregion


        public string ToHexString()
        {
            StringBuilder sb = new StringBuilder();
            byte[] bytes = this.ToByteArray();
            foreach (var b in bytes)
            {
                sb.Append(String.Format("0x{0} ", b.ToString("X2")));
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
            Debug.Write(String.Format("ReportObjectStatusMessage {0}", ToHexString()));
        }

        public void WriteLine()
        {
            Debug.WriteLine(String.Format("ReportObjectStatusMessage {0}", ToHexString()));
        }

    }
}
