// Copyright (c) 2016 Michael Dorough
using System;
using System.Diagnostics;
using ViSiGenie4DSystems.Async.Enumeration;

namespace ViSiGenie4DSystems.Async.Message
{
    /// <summary>
    /// This command can be used to send an array of Double bytes to a magic object. The magic
    /// object can process the double bytes in any way you want it to as there is no restrictions on
    /// the format of the information sent.
    /// 
    /// Note1: The maximum number of double bytes that can be sent at once is set by the
    /// ‘Maximum String Length’ setting in Workshop under File, Options, Genie. The number is set
    /// inline with the guidelines for Unicode Strings.
    /// 
    /// Note2: A Workshop PRO license is required to use this capability
    /// Reference: http://www.4dsystems.com.au/productpages/ViSi-Genie/downloads/Visi-Genie_refmanual_R_1_11.pdf
    /// </summary>
    public class WriteMagicDoubleBytesMessage : WriteMagicByteMessage
    {
        /// <summary>
        /// Supports base construction of a WriteMagicDoubleBytesMessage object. 
        /// </summary>
        public WriteMagicDoubleBytesMessage()
        {
            this.Command = Command.WriteMagicEventDbytes;
            this.Bytes = null;
            this.Length = 0;
            this.Checksum = 0;
        }

        /// <summary>
        /// Supports partial construction of a WriteMagicDoubleBytesMessage object. 
        /// </summary>
        /// <param name="objectIndex">
        /// Specifies the index of the Magic Object per a particular Workshop PRO project layout.
        /// </param>
        public WriteMagicDoubleBytesMessage(int objectIndex)
            : this()
        {
            this.ObjectIndex = objectIndex;
        }

        /// <summary>
        /// Helper constructor to write 64-bit value. 
        /// For example, write to an LEDDigit on a Workshop PRO layout.
        /// </summary>
        /// <param name="objectIndex">
        /// Specifies the index of the Magic Object per a particular Workshop PRO project layout.
        /// </param>
        /// <param name="doubleValueToDisplay">
        /// Converts an unsigned long to the internal byte array.
        /// </param>
        public WriteMagicDoubleBytesMessage(int objectIndex, ulong doubleValueToDisplay)
            : base(objectIndex)
        {
            byte[] bytes = BitConverter.GetBytes(doubleValueToDisplay);
            this.PackBytes(bytes);
        }

        /// <summary>
        /// Client provides an applicable specific double byte representation as discussed in paragraph 3.1.3.9, Write Magic Double Bytes.
        /// See: http://www.4dsystems.com.au/productpages/ViSi-Genie/downloads/Visi-Genie_refmanual_R_1_11.pdf
        /// </summary>
        /// <param name="objectIndex">
        /// Specifies the index of the Magic Object per a particular 4D Workshop project layout.
        /// </param>
        /// <param name="arrayOfDoubleBytes">
        /// An array of double bytes.
        /// </param>
        public WriteMagicDoubleBytesMessage(int objectIndex, byte[] arrayOfDoubleBytes)
           : base(objectIndex)
        {
            this.PackBytes(arrayOfDoubleBytes);
        }


        public override void Write()
        {
            Debug.Write(String.Format("WriteMagicDoubleBytesMessage {0}", ToHexString()));
        }

        public override void WriteLine()
        {
            Debug.WriteLine(String.Format("WriteMagicDoubleBytesMessage {0}", ToHexString()));
        }
    }
}
