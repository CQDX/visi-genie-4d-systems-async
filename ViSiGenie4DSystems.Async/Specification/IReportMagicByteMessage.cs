// Copyright (c) 2016 Michael Dorough
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using ViSiGenie4DSystems.Async.Enumeration;

namespace ViSiGenie4DSystems.Async.Specification
{
    /// <summary>
    /// An interface for the command can be used to send an array of bytes to the host from a magic object.
    /// </summary>
    interface IReportMagicByteMessage
    {
        /// <summary>
        /// WRITE Magic Bytes Command Code
        /// </summary>
        Command Command { get; set; }

        /// <summary>
        /// This byte specifies the index of the Magic Object
        /// </summary>
        int ObjectIndex { get; set; }

        /// <summary>
        /// The number of bytes which contain data to be sent to display
        /// </summary>
        int Length { get; set; }

        /// <summary>
        /// An array of bytes
        /// </summary>
        byte[] Bytes { get; set; }

        /// <summary>
        /// Checksum byte
        /// </summary>
        uint Checksum { get; set; }
    }
}
