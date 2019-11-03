// Copyright (c) 2016 Michael Dorough

namespace ViSiGenie4DSystems.Async.Enumeration
{
    /// <summary>
    /// Per Visi-Genie Reference Manual, Paragraph 4.3. Object Summary Table
    /// Document Date: 20th May 2015 Document Revision: 1.11
    /// </summary>
    public enum ObjectType
    {
        [EnumDescription("Dip Switch")]
        Dipswitch = 0,

        [EnumDescription("Knob")]
        Knob = 1,

        [EnumDescription("Rocker Switch")]
        Rockerswitch = 2,

        [EnumDescription("Rotary Switch")]
        Rotaryswitch = 3,

        [EnumDescription("Slider")]
        Slider = 4,

        [EnumDescription("Track Bar")]
        Trackbar = 5,

        [EnumDescription("Window Button")]
        Winbutton = 6,

        [EnumDescription("Angular Meter")]
        AngularMeter = 7,

        [EnumDescription("Cool Guage")]
        Coolgauge = 8,

        [EnumDescription("Customer Digits")]
        Customdigits = 9,

        [EnumDescription("Form")]
        Form = 10,

        [EnumDescription("Guage")]
        Gauge = 11,

        [EnumDescription("Image")]
        Image = 12,

        [EnumDescription("Keyboard")]
        Keyboard = 13,

        [EnumDescription("LED")]
        Led = 14,

        [EnumDescription("LED Digits")]
        Leddigits = 15,

        [EnumDescription("Meter")]
        Meter = 16,

        [EnumDescription("Strings")]
        Strings = 17,

        [EnumDescription("Thermometer")]
        Thermometer = 18,

        [EnumDescription("Under LED")]
        Uderled = 19,

        [EnumDescription("Video")]
        Video = 20,

        [EnumDescription("Static Text")]
        StaticText = 21,

        [EnumDescription("Sound")]
        Sound = 22,

        [EnumDescription("Timer")]
        Timer = 23,

        [EnumDescription("Spectrum")]
        Spectrum = 24,

        [EnumDescription("Scope")]
        Scope = 25,

        [EnumDescription("Tank")]
        Tank = 26,

        [EnumDescription("User Image")]
        UserImage = 27,

        [EnumDescription("Pin Output")]
        PinOutput = 28,

        [EnumDescription("Pin Input")]
        PinInput = 29,

        [EnumDescription("Button 4D")]
        Button4D = 30,

        [EnumDescription("Anl Button")]
        AnlButton = 31,

        [EnumDescription("Color Picker")]
        ColorPicker = 32,

        [EnumDescription("User Button")]
        UserButton = 33,
    };
}
