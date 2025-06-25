using System;
using System.Collections.Generic;

namespace PS4Saves;
public struct Patch
{
    public ulong Offset { get; }
    public ulong FunctionOffset { get; }
    public byte[] Bytes { get; }
    public byte SingleByte { get; } // Used if it's a single byte patch
    public bool IsSingleBytePatch { get; }

    // Constructor for byte array patches
    public Patch(ulong offset, byte[] bytes)
    {
        Offset = offset;
        Bytes = bytes;
        SingleByte = 0; // Not applicable for byte array patches
        IsSingleBytePatch = false;
    }

    // Constructor for single byte patches
    public Patch(ulong offset, byte singleByte)
    {
        Offset = offset;
        SingleByte = singleByte;
        Bytes = null; // Not applicable for single byte patches
        IsSingleBytePatch = true;
    }

    // Constructor for single byte patches
    public Patch(ulong offset, int functionOffset)
    {
        Offset = offset;
        Bytes = null; // Not applicable for single byte patches
        SingleByte = 0; // Not applicable for byte array patches
        FunctionOffset = (ulong)functionOffset;
        IsSingleBytePatch = false;
    }
}

public static class Patches
{
    private static readonly Dictionary<string, List<Patch>[]> FirmwarePatches = new Dictionary<string, List<Patch>[]>
    {
        {
            "3.20", new List<Patch>[]
            {
                // Shellcore patches
                [
                    new(0x1493043, (byte) 0x00), // 'sce_sdmemory' patch
                    new(0x148A099, (byte) 0x00), // 'sce_sdmemory1' patch
                    new(0x148A0A7, (byte) 0x00), // 'sce_sdmemory2' patch
                    new(0x15246EF, (byte) 0x00), // 'sce_sdmemory3' patch
                    new(0xB5FAF0, [0x48, 0x31, 0xC0, 0xC3]), // verify keystone patch
                    new(0x0FCFB0, [0x31, 0xC0, 0xC3]), // transfer mount permission patch eg mount foreign saves with write permission
                    new(0x181600, [0x31, 0xC0, 0xC3]), // patch psn check to load saves saves foreign to current account
                    new(0x10061C, (byte) 0xCD), // ^ (thanks to GRModSave_Username) different patch
                    new(0x0FF359, [0x90, 0x90, 0x90, 0x90, 0x90, 0x90]), // something something patches...
                    new(0x0FD33C, [0x90, 0x90, 0x90, 0x90, 0x90, 0x90]), // don't even remember doing this
                    new(0x0FCA14, [0x90, 0x90]), // nevah jump
                    new(0x0FCCC3, [0x90, 0xE9]) // always jump
                ],
                // Libc patches
                [
                    new(0x12, 0x78D60), // opendir
                    new(0x20, 0x78C10), // readdir
                    new(0x2E, 0x78920), // closedir
                    new(0x3C, 0x7A9E0)  // strcpy
                ]
            }
        },
        {
            "7.40", new List<Patch>[]
            {
                // Shellcore patches
                [
                    new(0x1AAD12E, (byte) 0x00), // 'sce_sdmemory' patch
                    new(0x1B0A257, (byte) 0x00), // 'sce_sdmemory1' patch
                    new(0x1AC19ED, (byte) 0x00), // 'sce_sdmemory2' patch
                    new(0x1A66751, (byte) 0x00), // 'sce_sdmemory3' patch
                    new(0xD014D0, [0x48, 0x31, 0xC0, 0xC3]), // verify keystone patch
                    new(0x1204E0, [0x31, 0xC0, 0xC3]), // transfer mount permission patch eg mount foreign saves with write permission
                    new(0x1C8520, [0x31, 0xC0, 0xC3]), // patch psn check to load saves saves foreign to current account
                    new(0x124267, [0xE9, 0x0C, 0x00, 0x00, 0x00]), // ^ (thanks to GRModSave_Username) different patch
                    new(0x122938, [0x90, 0x90, 0x90, 0x90, 0x90, 0x90]), // something something patches...
                    new(0x12085E, [0x90, 0x90, 0x90, 0x90, 0x90, 0x90]), // don't even remember doing this
                    new(0x11FF34, [0x90, 0x90]), // nevah jump
                    new(0x1201E2, [0x90, 0xE9]) // always jump
                ],
                // Libc patches
                [
                    new(0x12, 0x72BF0), // opendir
                    new(0x20, 0x72AA0), // readdir
                    new(0x2E, 0x727D0), // closedir
                    new(0x3C, 0x74830)  // strcpy
                ]
            }
        },
        {
            "8.20", new List<Patch>[]
            {
                // Shellcore patches
                [
                    new(0x1B21F98, (byte) 0x00), // 'sce_sdmemory' patch
                    new(0x1B8298F, (byte) 0x00), // 'sce_sdmemory1' patch
                    new(0x1B37B2D, (byte) 0x00), // 'sce_sdmemory2' patch
                    new(0x1AD8335, (byte) 0x00), // 'sce_sdmemory3' patch
                    new(0xD66930, [0x48, 0x31, 0xC0, 0xC3]), // verify keystone patch
                    new(0x11F1B0, [0x31, 0xC0, 0xC3]), // transfer mount permission patch eg mount foreign saves with write permission
                    new(0x1D2660, [0x31, 0xC0, 0xC3]), // patch psn check to load saves saves foreign to current account
                    new(0x122EAA, [0xE9, 0x05, 0x00, 0x00]), // ^ (thanks to GRModSave_Username) different patch
                    new(0x121598, [0x90, 0x90, 0x90, 0x90, 0x90, 0x90]), // something something patches...
                    new(0x122281, [0x90, 0x90, 0x90, 0x90, 0x90, 0x90]), // don't even remember doing this
                    new(0x11EC24, [0x90, 0x90]), // nevah jump
                    new(0x11EEC7, [0x90, 0xE9]) // always jump
                ],
                // Libc patches
                [
                    new(0x12, 0x760A0), // opendir
                    new(0x20, 0x75F50), // readdir
                    new(0x2E, 0x75C80), // closedir
                    new(0x3C, 0x77D20)  // strcpy
                ]
            }
        },
        {
            "10.01", new List<Patch>[]
            {
                // Shellcore patches
                [
                    new(0x1C1B5DA, (byte) 0x00), // 'sce_sdmemory' patch
                    new(0x1C8DC60, (byte) 0x00), // 'sce_sdmemory1' patch
                    new(0x1C05075, (byte) 0x00), // 'sce_sdmemory2' patch
                    new(0x1BEE286, (byte) 0x00), // 'sce_sdmemory3' patch
                    new(0xDCF260, [0x48, 0x31, 0xC0, 0xC3]), // verify keystone patch
                    new(0x1242D0, [0x31, 0xC0, 0xC3]), // transfer mount permission patch eg mount foreign saves with write permission
                    new(0x1E1C50, [0x31, 0xC0, 0xC3]), // patch psn check to load saves saves foreign to current account
                    new(0x127FAE, [0xE9, 0x05, 0x00, 0x00]), // ^ (thanks to GRModSave_Username) different patch
                    new(0x1266F8, [0x90, 0x90, 0x90, 0x90, 0x90, 0x90]), // something something patches...
                    new(0x12736B, [0x90, 0x90, 0x90, 0x90, 0x90, 0x90]), // don't even remember doing this
                    new(0x123D74, [0x90, 0x90]), // nevah jump
                    new(0x123FF2, [0x90, 0xE9]) // always jump
                ],
                // Libc patches
                [
                    new(0x12, 0x73AC0), // opendir
                    new(0x20, 0x73960), // readdir
                    new(0x2E, 0x73690), // closedir
                    new(0x3C, 0x757F0)  // strcpy
                ]
            }
        },
    };

    /// <summary>
    /// Retrieves the shellcore patches for a given firmware version.
    /// </summary>
    /// <param name="firmwareVersion">The firmware version string (e.g., "7.40").</param>
    /// <returns>A list of Patch objects for shellcore, or an empty list if no patches are defined for the version.</returns>
    public static List<Patch> GetShellcorePatches(string firmwareVersion)
    {
        if (FirmwarePatches.TryGetValue(firmwareVersion, out var patches))
        {
            return patches[0]; // Shellcore patches are at index 0
        }
        return [];
    }

    /// <summary>
    /// Retrieves the libc function patches for a given firmware version.
    /// </summary>
    /// <param name="firmwareVersion">The firmware version string (e.g., "7.40").</param>
    /// <returns>A list of Patch objects for libc functions, or an empty list if no patches are defined for the version.</returns>
    public static List<Patch> GetLibcPatches(string firmwareVersion)
    {
        if (FirmwarePatches.TryGetValue(firmwareVersion, out var patches))
        {
            return patches[1]; // Libc function patches are at index 1
        }
        return [];
    }
}
