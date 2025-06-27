using System;

namespace PS4Saves;

public static class Offsets
{
    public static readonly string[] Firmwares = ["3.20", "7.40", "8.20", "10.01"];
    public static string SelectedFirmware { get; set; } = string.Empty; // updated by Connect button
  
    public static ulong sceUserServiceGetInitialUser => SelectedFirmware switch
    {
        "2.50" => 0x32E0,
		"3.20" => 0x32D0,
        "4.03" => 0x3290,
        "5.xx" => 0x33B0,
        "6.02" => 0x33B0,
        "7.40" => 0x33C0,
        "8.20" => 0x33B0,
        "10.01" => 0x3370,
        _ => throw new Exception("Unsupported firmware (did you select an item from the dropdown?)")
    };

    public static ulong sceUserServiceGetLoginUserIdList => SelectedFirmware switch
    {
        "2.50" => 0x2A50,
        "3.20" => 0x2A50,
		"4.03" => 0x2A50,
        "5.xx" => 0x2B00,
        "6.02" => 0x2B00,
        "7.40" => 0x2B10,
        "8.20" => 0x2B00,
        "10.01" => 0x2AF0,
        _ => throw new Exception("Unsupported firmware (did you select an item from the dropdown?)")
    };

    public static ulong sceUserServiceGetUserName => SelectedFirmware switch
    {
        "2.50" => 0x46A0,
		"3.20" => 0x4690,
        "4.03" => 0x46E0,
        "5.xx" => 0x4830,
        "6.02" => 0x4830,
        "7.40" => 0x4730,
        "8.20" => 0x4710,
        "10.01" => 0x4650,
        _ => throw new Exception("Unsupported firmware (did you select an item from the dropdown?)")
    };

    public static ulong sceSaveDataMount => SelectedFirmware switch
    {
        //"2.50" => 0x2FE00,
		"3.20" => 0x31DB0,
        "4.03" => 0x32250,
        //"5.xx" => 0x321B0,
        "6.02" => 0x32A20,
        "7.40" => 0x32660,
        "8.20" => 0x32960,
        "10.01" => 0x32B20,
        _ => throw new Exception("Unsupported firmware (did you select an item from the dropdown?)")
    };

    public static ulong sceSaveDataUmount => SelectedFirmware switch
    {
        "2.50" => 0x302D0,
		"3.20" => 0x32560,
        "4.03" => 0x32A70,
        "5.xx" => 0x32680,
        "6.02" => 0x331C0,
        "7.40" => 0x32DE0,
        "8.20" => 0x330A0,
        "10.01" => 0x332A0,
        _ => throw new Exception("Unsupported firmware (did you select an item from the dropdown?)")
    };

    public static ulong sceSaveDataDirNameSearch => SelectedFirmware switch
    {
        "2.50" => 0x310B0,
		"3.20" => 0x33340,
        "4.03" => 0x33850,
        "5.xx" => 0x33460,
        "6.02" => 0x33FA0,
        "7.40" => 0x33BC0,
        "8.20" => 0x33E60,
        "10.01" => 0x34060,
        _ => throw new Exception("Unsupported firmware (did you select an item from the dropdown?)")
    };

    public static ulong sceSaveDataTransferringMount => SelectedFirmware switch
    {
        "2.50" => 0x30180,
		"3.20" => 0x32410,
        "4.03" => 0x328F0,
        "5.xx" => 0x32530,
        "6.02" => 0x33070,
        "7.40" => 0x32CA0,
        "8.20" => 0x32F70,
        "10.01" => 0x33170,
        _ => throw new Exception("Unsupported firmware (did you select an item from the dropdown?)")
    };

    public static ulong sceSaveDataInitialize3 => SelectedFirmware switch
    {
        "2.50" => 0x2F970,
		"3.20" => 0x31C00,
        "4.03" => 0x320A0,
        "5.xx" => 0x31D20,
        "6.02" => 0x32870,
        "7.40" => 0x324B0,
        "8.20" => 0x327B0,
        "10.01" => 0x32970,
        _ => throw new Exception("Unsupported firmware (did you select an item from the dropdown?)")
    };
}
