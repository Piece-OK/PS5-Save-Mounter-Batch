using System;

namespace PS4Saves;

public static class Offsets
{
    public static readonly string[] Firmwares = ["2.50", "3.20", "4.xx", "5.xx", "6.xx"];
    public static string SelectedFirmware { get; set; } = string.Empty; // updated by fwVersionComboBox
  
    public static ulong sceUserServiceGetInitialUser => SelectedFirmware switch
    {
        "2.50" => 0x32E0,
		"3.20" => 0x32D0,
        "4.xx" => 0x3290,
        "5.xx" => 0x33B0,
        "6.xx" => 0x33B0,
        _ => throw new Exception("Unsupported firmware (did you select an item from the dropdown?)")
    };

    public static ulong sceUserServiceGetLoginUserIdList => SelectedFirmware switch
    {
        "2.50" => 0x2A50,
        "3.20" => 0x2A50,
        "4.xx" => 0x2A50,   
        "5.xx" => 0x2B00,
        "6.xx" => 0x2B00,
        _ => throw new Exception("Unsupported firmware (did you select an item from the dropdown?)")
    };

    public static ulong sceUserServiceGetUserName => SelectedFirmware switch
    {
        "2.50" => 0x46A0,
		"3.20" => 0x4690,
        "4.xx" => 0x46E0,   
        "5.xx" => 0x4830,
        "6.xx" => 0x4830,
        _ => throw new Exception("Unsupported firmware (did you select an item from the dropdown?)")
    };

    public static ulong sceSaveDataMount2 => SelectedFirmware switch
    {
        "2.50" => 0x2FE00,
		"3.20" => 0x32090,
        "4.xx" => 0x31470,   
        "5.xx" => 0x321B0,
        "6.xx" => 0x32D00,
        _ => throw new Exception("Unsupported firmware (did you select an item from the dropdown?)")
    };

    public static ulong sceSaveDataUmount => SelectedFirmware switch
    {
        "2.50" => 0x302D0,
		"3.20" => 0x32560,
        "4.xx" => 0x31940,   
        "5.xx" => 0x32680,
        "6.xx" => 0x331C0,
        _ => throw new Exception("Unsupported firmware (did you select an item from the dropdown?)")
    };

    public static ulong sceSaveDataDirNameSearch => SelectedFirmware switch
    {
        "2.50" => 0x310B0,
		"3.20" => 0x33340,
        "4.xx" => 0x32720,    
        "5.xx" => 0x33460,
        "6.xx" => 0x33FA0,
        _ => throw new Exception("Unsupported firmware (did you select an item from the dropdown?)")
    };

    public static ulong sceSaveDataTransferringMount => SelectedFirmware switch
    {
        "2.50" => 0x30180,
		"3.20" => 0x32410,
        "4.xx" => 0x317F0,  
        "5.xx" => 0x32530,
        "6.xx" => 0x33070,
        _ => throw new Exception("Unsupported firmware (did you select an item from the dropdown?)")
    };

    public static ulong sceSaveDataInitialize3 => SelectedFirmware switch
    {
        "2.50" => 0x2F970,
		"3.20" => 0x31C00,
        "4.xx" => 0x30FE0, 
        "5.xx" => 0x31D20,
        "6.xx" => 0x32870,
        _ => throw new Exception("Unsupported firmware (did you select an item from the dropdown?)")
    };
}
