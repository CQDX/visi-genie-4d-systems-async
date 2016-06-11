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
    /// Per Visi-Genie Reference Manual, 3.1.3.5 Write Contrast Message
    /// Document Date: 20th May 2015 Document Revision: 1.11
    /// 
    /// The write message that lets you adjust the 4D System's display contrast.
    /// 
    /// Reference: http://www.4dsystems.com.au/productpages/ViSi-Genie/downloads/Visi-Genie_refmanual_R_1_11.pdf
    /// </summary>
    public class WriteContrastMessage
        : WriteMessage,
          IWriteContrastMessage,
          ICalculateChecksum,
          IByteOrder<uint>,
          IToHexString,
          IDebug
    {
        public WriteContrastMessage()
        {
            this.Checksum = 0;
            this.Command = Command.WRITE_CONTRAST;
        }

        public WriteContrastMessage(uint contrastValue)
            : this()
        {
            this.PackBytes(contrastValue);
        }

        /// <summary>
        /// WRITE OBJECT Command Code
        /// </summary>
        public Command Command { get; set; }

        /// <summary>
        /// Contrast value: 0 to 15
        /// </summary>
        public uint LSB { get; set; }

        public void PackBytes(uint value)
        {
            this.LSB = (value >> 0) & 0xFF;
        }

        /// <summary>
        /// Checksum byte
        /// </summary>
        public uint Checksum { get; set; }

        public uint CalculateChecksum()
        {
            uint workingChecksum = (uint)this.Command;

            workingChecksum ^= this.LSB;

            return workingChecksum;
        }

        #region IMPLEMENTATION OF ABSTRACT METHODS

        /// <summary>
        /// Converts WriteContrastMessage to byte array. 
        /// Checksum is placed at last element in byte array.
        /// </summary>
        /// <returns></returns>
        override public byte[] ToByteArray()
        {
            this.Checksum = this.CalculateChecksum();

            byte[] bytes = new byte[3];

            bytes[0] = Convert.ToByte(this.Command);
            bytes[1] = Convert.ToByte(this.LSB);
            bytes[2] = Convert.ToByte(this.Checksum);

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

        /// <summary>
        /// Backward compatibility: One-byte codes are used only for the ASCII values 0 through 127. 
        /// In this case the UTF-8 code has the same value as the ASCII code. 
        /// The high-order bit of these codes is always 0. This means that ASCII text is valid UTF-8, 
        /// and UTF-8 can be used for parsers expecting 8-bit extended ASCII even if they are not designed for UTF-8.
        /// </summary>
        /// <param name="bytes"></param>
        /// <returns></returns>
        private string ConvertToUtf8(byte[] bytes)
        {
            return System.Text.Encoding.UTF8.GetString(bytes);
        }

        public void Write()
        {
            Debug.Write(String.Format("WriteContrastMessage {0}", ToHexString()));
        }

        public void WriteLine()
        {
            Debug.WriteLine(String.Format("WriteContrastMessage {0}", ToHexString()));
        }
    }
}
