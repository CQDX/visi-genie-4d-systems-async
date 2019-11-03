// Copyright (c) 2016 Michael Dorough
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ViSiGenie4DSystems.Async.Enumeration
{
    /// <summary>
    /// Per Visi-Genie Reference Manual, Paragraph 3.1.2 Command
    /// Document Date: 20th May 2015 Document Revision: 1.11
    /// </summary>
    public enum Command
    {
        [EnumDescription("READ OBJECT")]
        ReadObj = 0,
        [EnumDescription("WRITE OBJECT")]
        WriteObj = 1,
        [EnumDescription("WRITE STRING")]
        WriteStr = 2,
        [EnumDescription("WRITE STRING UNICODE")]
        WriteStru = 3,
        [EnumDescription("WRITE CONTRAST")]
        WriteContrast = 4,
        [EnumDescription("REPORT OBJECT")]
        ReportObj = 5,
        [EnumDescription("REPORT EVENT")]
        ReportEvent = 7,
        [EnumDescription("WRITE MAGIC BYTES")]
        WriteMagicBytes = 8,
        [EnumDescription("WRITE MAGIC DBYTES")]
        WriteMagicDbytes = 9,
        [EnumDescription("WRITE MAGIC EVENT BYTES")]
        WriteMagicEventBytes = 10,
        [EnumDescription("WRITE MAGIC EVENT DBYTES")]
        WriteMagicEventDbytes = 11
    }
}
