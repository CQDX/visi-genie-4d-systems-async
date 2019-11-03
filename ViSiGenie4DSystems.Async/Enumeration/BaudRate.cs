// Copyright (c) 2016 Michael Dorough

namespace ViSiGenie4DSystems.Async.Enumeration
{
    /// <summary>
    /// The modulation rate of data transmission and express it as bits per second.
    /// Your 4D Systems Workshop 4 project's baud rate much match this library's baud rate. 
    /// </summary>
    public enum BaudRate
    {
        [EnumDescription("9600")]
        Bps9600 = 9600,
        [EnumDescription("14400")]
        Bps14400 = 14400,
        [EnumDescription("19200")]
        Bps19200 = 19200,
        [EnumDescription("31250")]
        Bps31250 = 31250,
        [EnumDescription("38400")]
        Bps38400 = 38400,
        [EnumDescription("56000")]
        Bps56000 = 56000,
        [EnumDescription("57600")]
        Bps57600 = 57600,
        [EnumDescription("115200")]
        Bps115200 = 115200

        //TODO: ADD FASTER BAUDS
    }
}
