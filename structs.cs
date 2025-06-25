using System;
using System.Runtime.InteropServices;
using System.Text;

namespace PS4Saves
{
    [StructLayout(LayoutKind.Explicit, Size = 80)]
    public struct SceSaveDataMount
    {
        [FieldOffset(0x0)] public int userId;
        [FieldOffset(0x8)] public ulong titleId;
        [FieldOffset(0x10)] public ulong dirName;
        [FieldOffset(0x18)] public ulong fingerprint;
        [FieldOffset(0x20)] public ulong blocks;
        [FieldOffset(0x28)] public uint mountMode;
    }

    [StructLayout(LayoutKind.Explicit, Size = 64)]
    public struct SceSaveDataMount2
    {
        [FieldOffset(0x0)] public int userId;
        [FieldOffset(0x8)] public ulong dirName;
        [FieldOffset(0x10)] public ulong blocks;
        [FieldOffset(0x18)] public uint mountMode;
    }
    [StructLayout(LayoutKind.Explicit, Size = 64)]
    public struct SceSaveDataMountResult
    {
        [FieldOffset(0x0)] public SceSaveDataMountPoint mountPoint;
        [FieldOffset(0x10)] public ulong requiredBlocks;
        [FieldOffset(0x18)] public uint mountStatus;

    }

    [StructLayout(LayoutKind.Sequential, Size = 16)]
    public struct SceSaveDataMountPoint
    {
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 16)]
        public string data;
    }

    [StructLayout(LayoutKind.Sequential, Size = 64)]
    public struct SceSaveDataTransferringMount
    {
        public int userId;
        public ulong titleId;
        public ulong dirName;
        public ulong fingerprint;
    }

    [StructLayout(LayoutKind.Sequential, Size = 16)]
    public struct SceSaveDataTitleId
    {
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 10)]
        public string data;
    }
    [StructLayout(LayoutKind.Sequential, Size = 32)]
    public struct SceSaveDataDirName
    {
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
        public string data;
    }
    [StructLayout(LayoutKind.Sequential, Size = 80)]
    public struct SceSaveDataFingerprint
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 65)]
        public byte[] data;
    }
    [StructLayout(LayoutKind.Explicit, Size = 64)]
    public struct SceSaveDataDirNameSearchCond
    {
        [FieldOffset(0x0)] public int userId;
        [FieldOffset(0x8)] public ulong titleId;
        [FieldOffset(0x10)] public ulong dirName;
        [FieldOffset(0x18)] public uint key;
        [FieldOffset(0x1C)] public uint order;
    }
    [StructLayout(LayoutKind.Explicit, Size = 56)]
    public struct SceSaveDataDirNameSearchResult
    {
        [FieldOffset(0x0)] public uint hitNum;
        [FieldOffset(0x8)] public ulong dirNames;
        [FieldOffset(0x10)] public uint dirNamesNum;
        [FieldOffset(0x14)] public uint setNum;
        [FieldOffset(0x18)] public ulong param;
        [FieldOffset(0x20)] public ulong infos;
    }
    [StructLayout(LayoutKind.Explicit, Size = 1328)]
    public struct SceSaveDataParam
    {
        [FieldOffset(0x0)]
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 128)]
        private byte[] _title;

        [FieldOffset(0x80)]
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 128)]
        private byte[] _subTitle;

        [FieldOffset(0x100)]
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 1024)]
        private byte[] _detail;

        [FieldOffset(0x500)]
        public uint userParam;

        [FieldOffset(0x508)]
        public long mtime;

        public string Title
        {
            get => GetUtf8String(_title);
            set => SetUtf8String(value, ref _title, 128);
        }

        public string SubTitle
        {
            get => GetUtf8String(_subTitle);
            set => SetUtf8String(value, ref _subTitle, 128);
        }

        public string Detail
        {
            get => GetUtf8String(_detail);
            set => SetUtf8String(value, ref _detail, 1024);
        }

        private static string GetUtf8String(byte[] buffer)
        {
            if (buffer == null) return string.Empty;
            int length = Array.IndexOf(buffer, (byte)0);
            if (length < 0) length = buffer.Length;
            return Encoding.UTF8.GetString(buffer, 0, length);
        }

        private static void SetUtf8String(string value, ref byte[] buffer, int maxLength)
        {
            if (buffer == null || buffer.Length != maxLength)
                buffer = new byte[maxLength];

            Array.Clear(buffer, 0, buffer.Length);

            if (string.IsNullOrEmpty(value))
                return;

            byte[] utf8Bytes = Encoding.UTF8.GetBytes(value);
            int count = Math.Min(utf8Bytes.Length, maxLength - 1); // Deixa 1 byte pro null terminator

            Array.Copy(utf8Bytes, buffer, count);
            buffer[count] = 0; // null-terminator
        }
    }
    [StructLayout(LayoutKind.Sequential, Size = 48)]
    public struct SceSaveDataSearchInfo
    {
        public ulong blocks;
        public ulong freeBlocks;
    }
}
