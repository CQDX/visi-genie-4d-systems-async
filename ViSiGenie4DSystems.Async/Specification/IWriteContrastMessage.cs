// Copyright (c) 2016 Michael Dorough
using ViSiGenie4DSystems.Async.Enumeration;

namespace ViSiGenie4DSystems.Async.Specification
{
    /// <summary>
    /// See Visi-Genie Reference Manual
    /// 
    /// Per 3.1.3.5 Write Contrast Message
    /// </summary>
    public interface IWriteContrastMessage
    {
        /// <summary>
        /// WRITE CONTRAST Command Code
        /// </summary>
        Command Command { get; set; }

        /// <summary>
        /// Contrast value: 0 to 15
        /// </summary>
        uint Lsb { get; set; }

        /// <summary>
        /// Checksum byte
        /// </summary>
        uint Checksum { get; set; }
    }
}
