// Copyright (c) 2016 Michael Dorough
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ViSiGenie4DSystems.Async.Specification
{
    /// <summary>
    /// The checksum is a means for the host to verify if the 4D Systems message received or sent
    /// is valid. This is calculated by XOR’ing all bytes in the message from (and including) the 
    /// Command byte to the last parameter byte. 
    /// 
    /// Then, the result is appended to the end to yield the checksum byte. 
    /// 
    /// If the message is correct, XOR’ing all the bytes(including the checksum byte) 
    /// will produce a result of zero. Checking the integrity of a message using 
    /// the checksum byte shall be handled by implementations of this interface.
    /// 
    /// Abstracted from the Visi-Genie Reference Manual.
    /// 
    /// </summary>
    public interface ICalculateChecksum
    {
        uint CalculateChecksum();
    }
}
