// Copyright(c) 2016 Michael Dorough
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
    /// Per Visi-Genie Reference Manual, 3.1.3.4 Write String (Unicode) Message
    /// Document Date: 20th May 2015 Document Revision: 1.11
    /// 
    /// A place holder for Unicode string objects can be defined and created in the Genie project. In
    /// order to display a dynamic string, the host can send this Write String (Unicode) message
    /// along with the string object index and then the string to be displayed. The maximum string
    /// length is 80 characters.
    /// Note1: The Unicode characters are 2 bytes each.
    /// Note2: The String should be null terminated.
    /// Note3: Refer to the application notes for detailed information on Strings and their usage.
    /// </summary>
    public class WriteStringUnicodeMessage
     : WriteMessage,
        IWriteStringMessage,
        ICalculateChecksum,
        IByteOrder<string>,
        IToHexString,
        IDebug
    {
        public WriteStringUnicodeMessage()
        {
            this.Checksum = 0;
            this.Command = Command.WRITE_STRU;
        }

        public WriteStringUnicodeMessage(int strIndex)
            : this()
        {
            this.StrIndex = strIndex;
        }

        public WriteStringUnicodeMessage(int strIndex, string displayMessage)
            : this(strIndex)
        {
            this.PackBytes(displayMessage);
        }

        public WriteStringUnicodeMessage(WriteStringUnicodeMessage otherWriteStringUnicodeMessage)
            : this()
        {
            this.Command = otherWriteStringUnicodeMessage.Command;
            this.StrIndex = otherWriteStringUnicodeMessage.StrIndex;
            this.StrLen = otherWriteStringUnicodeMessage.StrLen;
            this.Str = otherWriteStringUnicodeMessage.Str;
        }

        /// <summary>
        /// WRITE STRING (UNICODE) Command Code
        /// </summary>
        public Command Command { get; set; }

        /// <summary>
        /// This byte specifies the index or the item number of the ASCII String Object
        /// </summary>
        public int StrIndex { get; set; }

        /// <summary>
        /// Encoding for the UTF-16 format using the little endian byte order
        /// TODO: Need to confirm / test.
        /// </summary>
        /// <param name="displayMessage"></param>
        public void PackBytes(string displayMessage)
        {
            //.NET framework uses UTF-16 to represent strings
            this.Str = Encoding.Unicode.GetBytes(displayMessage);

            this.StrLen = Convert.ToUInt32(this.Str.Length + 1);
        }

        /// <summary>
        /// Length of the string characters, including the null terminator.
        /// Message length is 4 bytes + the number of string characters (including the null terminator).
        /// uint is 4 bytes or in C this is an unsigned char
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
        override public byte[] ToByteArray()
        {
            this.Checksum = this.CalculateChecksum();

            var stack = new List<byte>();

            stack.Add(Convert.ToByte(this.Command));

            stack.Add(Convert.ToByte(this.StrIndex));

            stack.Add(Convert.ToByte(this.StrLen));

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
                sb.Append(String.Format("0x{0} ", b.ToString("X2")));
            }
            return sb.ToString();
        }

        public void Write()
        {
            Debug.Write(String.Format("WriteStringUnicodeMessage {0}", ToHexString()));
        }

        public void WriteLine()
        {
            Debug.WriteLine(String.Format("WriteStringUnicodeMessage {0}", ToHexString()));
        }

    }
}
