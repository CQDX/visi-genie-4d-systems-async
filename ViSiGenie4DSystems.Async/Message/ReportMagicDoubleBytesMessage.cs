// Copyright (c) 2016 Michael Dorough
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using ViSiGenie4DSystems.Async.Specification;
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
            this.Command = Command.WRITE_MAGIC_EVENT_DBYTES;
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
