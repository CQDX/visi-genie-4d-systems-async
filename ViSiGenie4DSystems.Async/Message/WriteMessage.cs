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
    /// WriteMessage accomodates write message specializations per the Visi-Genie Reference Manual.
    /// 
    /// See runtime binding Async4D.SerialComm.Host which points high to this apex signature.
    /// 
    ///     public async Task<Acknowledgement> Send(string deviceId, WriteMessage writeMessage, int cancelAfterMillisecondsDelay=1000)
    /// 
    /// </summary>
    abstract public class WriteMessage
        : IMessageFormat
    {
        abstract public byte[] ToByteArray();
    }
}
