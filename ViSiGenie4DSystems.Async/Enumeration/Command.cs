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
        READ_OBJ = 0,
        [EnumDescription("WRITE OBJECT")]
        WRITE_OBJ = 1,
        [EnumDescription("WRITE STRING")]
        WRITE_STR = 2,
        [EnumDescription("WRITE STRING UNICODE")]
        WRITE_STRU = 3,
        [EnumDescription("WRITE CONTRAST")]
        WRITE_CONTRAST = 4,
        [EnumDescription("REPORT OBJECT")]
        REPORT_OBJ = 5,
        [EnumDescription("REPORT EVENT")]
        REPORT_EVENT = 7,
        [EnumDescription("WRITE MAGIC BYTES")]
        WRITE_MAGIC_BYTES = 8,
        [EnumDescription("WRITE MAGIC DBYTES")]
        WRITE_MAGIC_DBYTES = 9,
        [EnumDescription("WRITE MAGIC EVENT BYTES")]
        WRITE_MAGIC_EVENT_BYTES = 10,
        [EnumDescription("WRITE MAGIC EVENT DBYTES")]
        WRITE_MAGIC_EVENT_DBYTES = 11
    }
}
