// Copyright(c) 2016 Michael Dorough
using System;
using System.Diagnostics;
using System.Text;
using ViSiGenie4DSystems.Async.Enumeration;
using ViSiGenie4DSystems.Async.Specification;

namespace ViSiGenie4DSystems.Async.Message
{
    /// <summary>
    /// Per Visi-Genie Reference Manual, 3.1.3.2 Write Object Value Message
    /// Document Date: 20th May 2015 Document Revision: 1.11
    /// 
    /// The host issues the Write Object command message when it wants to change the status of an
    /// individual object item.
    /// eference: http://www.4dsystems.com.au/productpages/ViSi-Genie/downloads/Visi-Genie_refmanual_R_1_11.pdf
    /// </summary>
    public class WriteObjectValueMessage
        : WriteMessage,
          IWriteObjectValueMessage,
          ICalculateChecksum,
          IByteOrder<uint>,
          IToHexString,
          IDebug
    {
        /// <summary>
        /// Default constructor
        /// </summary>
        public WriteObjectValueMessage()
        {
            this.Checksum = 0;
            this.Command = Command.WriteObj;
        }

        /// <summary>
        /// WriteObjectValueMessage helper constructor
        /// </summary> 
        /// <param name="objectType"></param>
        /// <param name="objectIndex"></param>
        public WriteObjectValueMessage(ObjectType objectType, int objectIndex)
            : this()
        {
            this.ObjectType = objectType;
            this.ObjectIndex = objectIndex;
            this.PackBytes(0); //Always 0
            this.Checksum = this.CalculateChecksum();
        }

        /// <summary>
        /// WriteObjectValueMessage helper constructor
        /// </summary>
        /// <param name="objectType"></param>
        /// <param name="objectIndex"></param>
        /// <param name="displayValue"></param>
        public WriteObjectValueMessage(ObjectType objectType, int objectIndex, uint displayValue)
            : this(objectType, objectIndex)
        {
            this.PackBytes(displayValue);
            this.Checksum = this.CalculateChecksum();
        }

        /// <summary>
        /// Copy some otherWriteObjectValueMessage to this
        /// </summary>
        /// <param name="otherWriteObjectValueMessage"></param>
        public WriteObjectValueMessage(WriteObjectValueMessage otherWriteObjectValueMessage)
        {
            this.Command = otherWriteObjectValueMessage.Command;
            this.ObjectType = otherWriteObjectValueMessage.ObjectType;
            this.ObjectIndex = otherWriteObjectValueMessage.ObjectIndex;
            this.Lsb = otherWriteObjectValueMessage.Lsb;
            this.Msb = otherWriteObjectValueMessage.Msb;
            this.Checksum = this.CalculateChecksum();
        }

        /// <summary>
        /// WRITE OBJECT Command Code
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
        public uint Msb { get; set; }

        /// <summary>
        /// Least significant byte of the 2 byte VALUE
        /// </summary>
        /// </summary>
        public uint Lsb { get; set; }

        /// <summary>
        /// Combines the MSB and LSB into one word.
        /// </summary>
        /// <param name="value"></param>
        public void PackBytes(uint value)
        {
            this.Lsb = (value >> 0) & 0xFF;
            this.Msb = (value >> 8) & 0xFF;
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

            workingChecksum ^= this.Msb;

            workingChecksum ^= this.Lsb;

            return workingChecksum;
        }

        #region IMPLEMENTATION OF ABSTRACT METHODS

        /// <summary>
        /// Converts WriteObjectValueMessage to byte array. 
        /// Checksum is placed at last element in byte array.
        /// </summary>
        /// <returns></returns>
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
                sb.Append(String.Format("0x{0}", b.ToString("X2")));
            }
            return sb.ToString();
        }

        /// <summary>
        /// Example: The message sent is formatted according to the following pattern:
        /// 
        /// Command  Object Type  Object Index  Value MSB  Value LSB  Checksum
        /// 01       05           00            00         28         2C
        /// 
        /// WRITE_OBJ Trackbar First 0x0028
        /// 
        /// http://www.4dsystems.com.au/downloads/Application-Notes/4D-AN-00106_R_1_0.pdf
        /// 
        /// </summary>
        /// <returns></returns>
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
            Debug.Write(String.Format("WriteObjectValueMessage {0}", ToHexString()));
        }

        public void WriteLine()
        {
            Debug.WriteLine(String.Format("WriteObjectValueMessage {0}", ToHexString()));
        }

    }
}
