namespace Z21Dashboard.Infrastructure;

internal static class Cvs
{
    internal const ushort ShortAddress = 1;
    internal const ushort VStart = 2;
    internal const ushort AccelerationRate = 3;
    internal const ushort DeaccelerationRate = 4;
    internal const ushort VHigh = 5;
    internal const ushort VMid = 6;
    internal const ushort ManufacturerVersionNbr = 7;
    internal const ushort Manufacturer = 8;
    internal const ushort PWMPeriod = 9;
    internal const ushort LongAddressHighByte = 17;
    internal const ushort LongAddressLowByte = 18;
    internal const ushort RailComConfiguration = 28;
    internal const ushort ConfigurationData1 = 29;
    internal const ushort KickStart = 65;
    internal const ushort ForwardTrim = 66;
    internal const ushort SpeedTableStart = 67; // First step
    internal const ushort SpeedTableEnd = 94;   // Last step (28)
    internal const ushort ReverseTrim = 95;

    internal const byte CV28_Bit0_Channel1Broadcast = 0b00000001;       // Decimal   1
    internal const byte CV28_Bit1_DataTransmissionAllowed = 0b00000010; // Decimal   2
    internal const byte CV28_Bit7_AutoLogin = 0b10000000;               // Decimal 128


    // Bitmasks for CV29
    internal const byte CV29_Bit0_ForwardDirection = 0b00000001;        // Decimal   1
    internal const byte CV29_Bit1_SpeedSteps = 0b00000010;              // Decimal   2
    internal const byte CV29_Bit2_DCAnalog = 0b00000100;                // Decimal   4
    internal const byte CV29_Bit3_RailCom = 0b00001000;                 // Decimal   8
    internal const byte CV29_Bit4_SpeedTable =  0b00010000;             // Decimal  16
    internal const byte CV29_Bit5_LongAddress = 0b00100000;             // Decimal  16
}
