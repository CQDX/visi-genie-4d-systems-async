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
    /// 3.1.3.1 Read Object Status Message
    /// </summary>
    public interface IReadObjectStatusMessage
    {
        /// <summary>
        /// READ OBJECT Command Code.
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
        /// Checksum byte
        /// </summary>
        uint Checksum { get; set; }
    }
}
