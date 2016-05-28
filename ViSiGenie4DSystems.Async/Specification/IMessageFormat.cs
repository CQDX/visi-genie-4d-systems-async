// Copyright (c) 2016 Michael Dorough
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ViSiGenie4DSystems.Async.Specification
{
    /// <summary>
    /// A contract for display bound message I/O.
    /// </summary>
    public interface IMessageFormat
    {
        /// <summary>
        /// Byte format method required for any ViSi-Genie Message
        /// </summary>
        /// <returns></returns>
        byte[] ToByteArray();
    }
}
