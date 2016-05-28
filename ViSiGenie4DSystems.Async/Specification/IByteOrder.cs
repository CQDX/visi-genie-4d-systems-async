// Copyright (c) 2016 Michael Dorough
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ViSiGenie4DSystems.Async.Specification
{
    /// <summary>
    /// A generic type parameter contravariant to support different byte ordering typing. 
    /// Abstracted from Section 3 of the Visi-Genie Reference Manual.
    /// </summary>
    public interface IByteOrder<in A>
    {
        void PackBytes(A value);

        byte[] ToByteArray();
    }
}
