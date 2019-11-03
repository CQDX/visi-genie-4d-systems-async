// Copyright (c) 2016 Michael Dorough

namespace ViSiGenie4DSystems.Async.Specification
{
    /// <summary>
    /// A generic type parameter contravariant to support different byte ordering typing. 
    /// Abstracted from Section 3 of the Visi-Genie Reference Manual.
    /// </summary>
    public interface IByteOrder<in TA>
    {
        void PackBytes(TA value);

        byte[] ToByteArray();
    }
}
