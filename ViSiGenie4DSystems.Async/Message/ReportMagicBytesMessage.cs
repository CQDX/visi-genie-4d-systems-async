// Copyright (c) 2016 Michael Dorough
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViSiGenie4DSystems.Async.Specification;
using ViSiGenie4DSystems.Async.Enumeration;

namespace ViSiGenie4DSystems.Async.Message
{
    /// <summary>
    /// This command can be used to send an array of bytes to the host from a magic object. The
    /// magic object can send the bytes in any desired format.The Magic object is responsible for the
    /// complete building of this message.
    /// Note1: A Workshop PRO license is required to use this capability.    /// Reference: http://www.4dsystems.com.au/productpages/ViSi-Genie/downloads/Visi-Genie_refmanual_R_1_11.pdf
    /// </summary>
    public class ReportMagicBytesMessage
           : ReadMessage,
            IReportMagicByteMessage,
            ICalculateChecksum,
            IByteOrder<byte[]>,
            IToHexString,
            IDebug
    {
        /// <summary>
        /// Supports base construction of a ReportMagicByteMessaobject. 
        /// </summary>
        public ReportMagicBytesMessage()
        {
            this.Command = Command.WRITE_MAGIC_EVENT_BYTES;
        }

        public ReportMagicBytesMessage(int objectIndex, int lengthOfMagicBytes, byte[] magicBytes, uint checksum)
           : this()
        {
            this.ObjectIndex = objectIndex;
            this.Length = lengthOfMagicBytes;
            this.PackBytes(magicBytes);

            this.Checksum = checksum;
        }

        /// <summary>
        /// WRITE Magic Bytes Command Code
        /// </summary>
        public Command Command { get; set; }

        /// <summary>
        /// Identifier of String on display
        /// </summary>
        public int ObjectIndex { get; set; }

        /// <summary>
        /// Length of the array of bytes
        /// </summary>
        public int Length { get; set; }

        /// <summary>
        /// An array of bytes
        /// </summary>
        public byte[] Bytes { get; set; }

        /// <summary>
        /// Pack bytes into internal representation required by magic byte data structure
        /// </summary>
        /// <param name="value"></param>
        public void PackBytes(byte[] bytes)
        {
            this.Bytes = bytes;
            this.Length = Bytes.Length;
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

            workingChecksum ^= (uint)this.ObjectIndex;

            workingChecksum ^= (uint)this.Length;

            for (int c = this.Length - 1; c >= 0; c--)
            {
                workingChecksum ^= this.Bytes[c];
            }
            return workingChecksum;
        }

        #region IMPLEMENTATION OF ABSTRACT METHODS

        /// <summary>
        /// Converts WriteMagicByteMessage to byte array. 
        /// Checksum is placed at last element in byte array.
        /// </summary>
        /// <returns></returns>
        override public byte[] ToByteArray()
        {
            this.Checksum = this.CalculateChecksum();

            var stack = new List<byte>();

            //Push
            stack.Add(Convert.ToByte(this.Command));

            stack.Add(Convert.ToByte(this.ObjectIndex));

            stack.Add(Convert.ToByte(this.Length));

            for (int c = this.Length - 1; c >= 0; c--)
            {
                stack.Add(this.Bytes[c]);
            }

            stack.Add(Convert.ToByte(this.Checksum));

            return stack.ToArray();
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

        virtual public void Write()
        {
            Debug.Write(String.Format("ReportMagicByteMessage {0}", ToHexString()));
        }

        virtual public void WriteLine()
        {
            Debug.WriteLine(String.Format("ReportMagicByteMessage {0}", ToHexString()));
        }
    }
}
