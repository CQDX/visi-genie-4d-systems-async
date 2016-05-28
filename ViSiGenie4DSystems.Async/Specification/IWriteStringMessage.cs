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
    /// 3.1.3.3 Write String (ASCII) Message 
    /// 3.1.3.4 Write String (Unicode) Message
    /// </summary>
    public interface IWriteStringMessage
    {
        /// <summary>
        /// Command code
        /// </summary>
        Command Command { get; set; }

        /// <summary>
        /// This byte specifies the index or the item number of the String Object
        /// </summary>
        int StrIndex { get; set; }

        /// <summary>
        /// The number of string characters (including the null terminator).
        /// </summary>
        uint StrLen { get; set; }

        /// <summary>
        /// Host must append null terminator
        /// </summary>
        byte[] Str { get; set; }

        /// <summary>
        /// Checksum byte
        /// </summary>
        uint Checksum { get; set; }
    }
}
