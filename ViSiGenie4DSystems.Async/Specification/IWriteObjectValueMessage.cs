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
    /// See Visi-Genie Reference Manual
    /// 
    /// Per 3.1.3.2 Write Object Value Message
    /// </summary>
    public interface IWriteObjectValueMessage
    {
        /// <summary>
        /// WRITE OBJECT Command Code
        /// </summary>
        Command Command { get; set; }

        /// <summary>
        /// Object ID. Refer to Object ID table for the relevant codes
        /// </summary>
        ObjectType ObjectType { get; set; }

        /// <summary>
        /// This byte specifies the index or the item number of the Object
        /// </summary>
        int ObjectIndex { get; set; }

        /// <summary>
        /// Most significant byte of the 2 byte VALUE
        /// </summary>
        uint MSB { get; set; }

        /// <summary>
        /// Least significant byte of the 2 byte VALUE
        /// </summary>
        uint LSB { get; set; }

        /// <summary>
        /// Checksum byte
        /// </summary>
        uint Checksum { get; set; }
    }
}
