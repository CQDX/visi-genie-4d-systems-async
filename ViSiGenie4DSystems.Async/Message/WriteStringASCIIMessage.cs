// Copyright(c) 2016 Michael Dorough
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

using ViSiGenie4DSystems.Async.Enumeration;
using ViSiGenie4DSystems.Async.Specification;

namespace ViSiGenie4DSystems.Async.Message
{
    /// <summary>
    /// Per Visi-Genie Reference Manual, 3.1.3.3 Write String (ASCII) Message
    /// Document Date: 20th May 2015 Document Revision: 1.11
    /// 
    /// A place holder for ASCII string objects can be defined and created in the Genie project. In
    /// order to display a dynamic string, the host can send this Write String (ASCII) message along
    /// with the string object index and then the string to be displayed. The maximum string length is
    /// 80 characters.
    /// 
    /// Note1: The ASCII characters are 1 byte each.
    /// Note2: The String should be null terminated.
    /// Note3: Refer to the application notes for detailed information on Strings and their usage.
    /// Reference: http://www.4dsystems.com.au/productpages/ViSi-Genie/downloads/Visi-Genie_refmanual_R_1_11.pdf
    /// </summary>
    public class WriteStringAsciiMessage
        : WriteMessage,
          IWriteStringMessage,
          ICalculateChecksum,
          IByteOrder<string>,
          IToHexString,
          IDebug
    {
        public WriteStringAsciiMessage()
        {
            this.Checksum = 0;
            this.Command = Command.WriteStr;
        }

        public WriteStringAsciiMessage(int strIndex)
            : this()
        {
            this.StrIndex = strIndex;
        }

        public WriteStringAsciiMessage(int strIndex, string displayMessage)
            : this(strIndex)
        {
            this.PackBytes(displayMessage);
        }

        public WriteStringAsciiMessage(WriteStringAsciiMessage otherWriteStringAsciiMessage)
            : this()
        {
            this.Command = otherWriteStringAsciiMessage.Command;
            this.StrIndex = otherWriteStringAsciiMessage.StrIndex;
            this.StrLen = otherWriteStringAsciiMessage.StrLen;
            this.Str = otherWriteStringAsciiMessage.Str;
        }

        /// <summary>
        /// WRITE STRING (ASCII) Command Code
        /// </summary>
        public Command Command { get; set; }

        /// <summary>
        /// This byte specifies the index or the item number of the ASCII String Object
        /// </summary>
        public int StrIndex { get; set; }

        public void PackBytes(string displayMessage)
        {
            this.Str = Encoding.ASCII.GetBytes(displayMessage);

            this.StrLen = Convert.ToUInt32(this.Str.Length + 1);
        }

        /// <summary>
        /// Length of the string characters, including the null terminator.
        /// Genie reference manual incorrect "4 bytes + the number of string char including null" 
        /// </summary>
        public uint StrLen { get; set; }

        /// <summary>
        /// ASCII String characters. Host must append null terminator
        /// </summary>
        public byte[] Str { get; set; }

        /// <summary>
        /// Checksum of the data structure
        /// </summary>
        public uint Checksum { get; set; }

        /// <summary>
        /// Computes check sum of this data structure
        /// </summary>
        /// <returns></returns>
        public uint CalculateChecksum()
        {
            uint workingChecksum = (uint)this.Command;

            workingChecksum ^= (uint)this.StrIndex;

            workingChecksum ^= this.StrLen;

            foreach (var c in this.Str)
            {
                workingChecksum ^= c;
            }

            //gtx did not include null despite documentation
            byte nullByte = (byte)0;  //tack on null byte, this C#

            workingChecksum ^= nullByte;

            return workingChecksum;
        }

        #region IMPLEMENTATION OF ABSTRACT METHODS

        /// <summary>
        /// Converts WriteStringASCIIMessage to byte array. 
        /// Uses a List<byte> stack lifo to dyanamically allocate byte[] array.
        /// </summary>
        /// <returns>byte[] array to be sent to display</returns>
        public override byte[] ToByteArray()
        {
            this.Checksum = this.CalculateChecksum();

            var stack = new List<byte>
            {
                Convert.ToByte(this.Command), Convert.ToByte(this.StrIndex), Convert.ToByte(this.StrLen)
            };

            foreach (var c in this.Str)
            {
                stack.Add(c);
            }

            stack.Add((byte)0); //tack on null byte, LOL the good old days of C++. Do you remember Zortech C++ and Oregon C++ compilers from 1991?

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
                sb.Append($"0x{b:X2}");
            }
            return sb.ToString();
        }

        public void Write()
        {
            Debug.Write($"WriteStringASCIIMessage {ToHexString()}");
        }

        public void WriteLine()
        {
            Debug.WriteLine($"WriteStringASCIIMessage {ToHexString()}");
        }
    }
}
