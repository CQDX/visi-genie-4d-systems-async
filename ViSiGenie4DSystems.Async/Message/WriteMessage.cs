// Copyright (c) 2016 Michael Dorough
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using ViSiGenie4DSystems.Async.Specification;

namespace ViSiGenie4DSystems.Async.Message
{
    /// <summary>
    /// The apex of outbound byte data sent to a 4D Systems display. 
    /// WriteMessage accomodates various specializations per the Visi-Genie Reference Manual.
    /// </summary>
    /// <remarks>
    /// Notice <see cref="Host"/> Send method is written in terms of this abstract base class.
    /// </remarks>
    abstract public class WriteMessage
        : IMessageFormat
    {
        abstract public byte[] ToByteArray();
    }
}
