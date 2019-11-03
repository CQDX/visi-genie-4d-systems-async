// Copyright (c) 2016 Michael Dorough
using ViSiGenie4DSystems.Async.Enumeration;

namespace ViSiGenie4DSystems.Async.Specification
{
    /// <summary>
    /// See Visi-Genie Reference Manual
    /// 
    /// Per 3.1.3.8 Write Magic Bytes
    /// </summary>
    public interface IWriteMagicByteMessage
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
