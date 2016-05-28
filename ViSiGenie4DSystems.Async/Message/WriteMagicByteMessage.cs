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
    /// Per Visi-Genie Reference Manual, 3.1.3.8 Write Magic Bytes
    /// Document Date: 20th May 2015 Document Revision: 1.11
    /// 
    /// This command can be used to send an array of bytes to a magic object. The magic object can
    /// process the bytes in any way you want it to as there is no restrictions on the format of the
    /// information sent.
    /// 
    /// Note1: The maximum number of bytes that can be sent at once is set by the ‘Maximum String
    /// Length’ setting in Workshop under File, Options, Genie.
    /// Note2: A Workshop PRO license is required to use this capability.
    /// </summary>
    public class WriteMagicByteMessage
        : WriteMessage,
          IWriteMagicByteMessage,
          ICalculateChecksum,
          IByteOrder<byte[]>,
          IToHexString,
          IDebug
    {
        public WriteMagicByteMessage()
        {
            this.Checksum = 0;
            this.Command = Command.WRITE_MAGIC_BYTES;
        }

        public WriteMagicByteMessage(int objectIndex)
            : this()
        {
            this.ObjectIndex = objectIndex;
        }

        /// <summary>
        /// Helper constructor to write 32-bit value. 
        /// For example, LEDDigit
        /// </summary>
        /// <param name="objectIndex"></param>
        /// <param name="valueToDisplay"></param>
        public WriteMagicByteMessage(int objectIndex, uint valueToDisplay)
            : this(objectIndex)
        {
            byte[] bytes = BitConverter.GetBytes(valueToDisplay);
            this.PackBytes(bytes);
        }

        public WriteMagicByteMessage(int objectIndex, byte[] magicBytes)
           : this(objectIndex)
        {
            this.PackBytes(magicBytes);
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
                sb.Append(String.Format("0x{0} ", b.ToString("X2")));
            }
            return sb.ToString();
        }

        public void Write()
        {
            Debug.Write(String.Format("WriteMagicByteMessage {0}", ToHexString()));
        }

        public void WriteLine()
        {
            Debug.WriteLine(String.Format("WriteMagicByteMessage {0}", ToHexString()));
        }
    }
}
