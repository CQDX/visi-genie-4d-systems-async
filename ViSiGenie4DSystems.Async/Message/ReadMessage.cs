// Copyright (c) 2016 Michael Dorough
using ViSiGenie4DSystems.Async.Specification;

namespace ViSiGenie4DSystems.Async.Message
{
    /// <summary>
    /// The apex of inbound messages received from a 4D Systems LCD display.
    /// Accomodate read message specializations per the Visi-Genie Reference Manual.
    /// Reference: http://www.4dsystems.com.au/productpages/ViSi-Genie/downloads/Visi-Genie_refmanual_R_1_11.pdf
    /// </summary>
    public abstract class ReadMessage
        : IMessageFormat
    {
        public abstract byte[] ToByteArray();
    }
}
