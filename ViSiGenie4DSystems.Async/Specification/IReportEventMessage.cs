// Copyright (c) 2016 Michael Dorough
using ViSiGenie4DSystems.Async.Enumeration;

namespace ViSiGenie4DSystems.Async.Specification
{
    /// <summary>
    /// See Visi-Genie Reference Manual
    /// 
    /// Per 3.1.3.7 Report Event Message
    /// </summary>
    public interface IReportEventMessage
    {
        /// <summary>
        /// REPORT EVENT Command Code
        /// </summary>
        Command Command { get; set; }

        /// <summary>
        /// Refer to Object ID table for the relevant codes
        /// </summary>
        ObjectType ObjectType { get; set; }

        /// <summary>
        /// This byte specifies the index or the item number of the Object that
        /// caused the event
        /// </summary>
        int ObjectIndex { get; set; }

        /// <summary>
        /// Most significant byte of the 2 byte VALUE
        /// </summary>
        uint Msb { get; set; }

        /// <summary>
        /// Least significant byte of the 2 byte VALUE
        /// </summary>
        uint Lsb { get; set; }

        /// <summary>
        /// Checksum byte
        /// </summary>
        uint Checksum { get; set; }
    }
}
