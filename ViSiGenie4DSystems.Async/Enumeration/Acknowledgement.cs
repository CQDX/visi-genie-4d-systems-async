// Copyright (c) 2016 Michael Dorough

namespace ViSiGenie4DSystems.Async.Enumeration
{
    /// <summary>
    /// Per Visi-Genie Reference Manual, Paragraph 3.1.3 Command Set Messages
    /// Document Date: 20th May 2015 Document Revision: 1.11
    /// 
    /// This section provides detailed information intended for programmers of the Host Controller. It contains the
    /// message formats of the commands that comprise the ViSi-Genie protocol. New commands may be added in
    /// future to expand the protocol.
    /// </summary>
    public enum Acknowledgement
    {
        /// <summary>
        /// Acknowledge byte (0x06); this byte is issued by the Display to the Host when the Display
        /// has correctly received the last message frame from the Host.
        /// The transmission message for this is a single byte: 0x06
        /// </summary>
        [EnumDescription("ACK")]
        Ack = 0x06,

        /// <summary>
        /// Not Acknowledge byte (0x15); this byte is issued by the receiver (Display or Host) to the
        /// sender (Host or Display) when the receiver has not correctly received the last message
        /// frame from the sender.
        /// The transmission message for this is a single byte: 0x15
        /// </summary>
        [EnumDescription("NAK")]
        Nak = 0x15,

        [EnumDescription("TIMEOUT")]
        Timeout = 0x00
    }
}
