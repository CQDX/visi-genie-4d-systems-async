// Copyright (c) 2016 Michael Dorough
using ViSiGenie4DSystems.Async.Enumeration;

namespace ViSiGenie4DSystems.Async.Message
{
    public class ReportMagicDoubleBytesMessage : ReportMagicBytesMessage
    {

        /// <summary>
        /// Supports base construction of a ReportMagicByteMessaobject. 
        /// </summary>
        public ReportMagicDoubleBytesMessage()
        {
            this.Command = Command.WriteMagicEventDbytes;
        }

        public ReportMagicDoubleBytesMessage(int objectIndex, int lengthOfMagicBytes, byte[] magicBytes, uint checksum)
           : this()
        {
            this.ObjectIndex = objectIndex;
            this.Length = lengthOfMagicBytes;
            this.PackBytes(magicBytes);

            this.Checksum = checksum;
        }
    }
}
