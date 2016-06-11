// Copyright (c) 2016 Michael Dorough
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using ViSiGenie4DSystems.Async.Specification;

namespace ViSiGenie4DSystems.Async.Message
{
    /// <summary>
    /// The apex of inbound messages received from a 4D Systems LCD display.
    /// Accomodates read message specializations per the Visi-Genie Reference Manual.
    /// Reference: http://www.4dsystems.com.au/productpages/ViSi-Genie/downloads/Visi-Genie_refmanual_R_1_11.pdf
    /// </summary>
    abstract public class ReadMessage
        : IMessageFormat
    {
        abstract public byte[] ToByteArray();
    }
}
