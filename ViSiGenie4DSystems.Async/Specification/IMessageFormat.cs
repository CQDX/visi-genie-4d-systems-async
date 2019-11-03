// Copyright (c) 2016 Michael Dorough

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
