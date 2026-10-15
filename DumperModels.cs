using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
#if WINDOWS
using System.Windows.Forms;
#endif

namespace PS4Saves;
#if !WINDOWS
public enum CheckState { Unchecked, Checked, Indeterminate }
#endif
public class DumperSettings
{
    public string Host { get; set; } = "";
    public bool SendPayload { get; set; } = true;
    public int MounterPort { get; set; } = 9090;
    public int LoaderPort { get; set; } = 9021;
    public int? AlternativeFTPPort { get; set; }
    public bool IncludeGameTitle { get; set; }
    public bool OnlineGameTitles { get; set; } = true;
    public string Destination { get; set; } = "Backups";
    public int TransferPort(int embeddedPort) => AlternativeFTPPort is null or 0 ? embeddedPort
        : AlternativeFTPPort is > 0 and <= 65535 ? AlternativeFTPPort.Value : throw new ArgumentException("AlternativeFTPPort must be 1–65535, or null for embedded FTP");
    public static string ResolveDestination(string value) =>
        Path.GetFullPath(Path.IsPathFullyQualified(value) ? value : Path.Combine(AppContext.BaseDirectory, value));
}
public static class DumperConnection
{
    public static MounterClient Connect(DumperSettings s, bool deploy, Action<string> log)
    {
        if (string.IsNullOrWhiteSpace(s.Host) || Uri.CheckHostName(s.Host) == UriHostNameType.Unknown)
            throw new ArgumentException("Enter a console IP address or hostname, without ftp:// or a port");
        if (deploy)
        {
            log("Sending savemounter.elf to the PS5 ELF loader…");
            ElfldrClient.SendElf(s.Host, s.LoaderPort, File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, BuildInfo.PayloadFile)));
        }
        var client = new MounterClient();
        try
        {
            client.Connect(s.Host, s.MounterPort, deploy ? 10 : 1);
            client.NegotiateBatchProtocol();
            log($"Connected: PS5 firmware {client.GetFirmwareVersion()} • Save Mounter development build {client.PayloadVersion} • transfer port {s.TransferPort(client.EmbeddedFtpPort)}");
            return client;
        }
        catch { client.Dispose(); throw; }
    }
}
public sealed class ConnectionFailure(string message, Exception inner) : IOException(message, inner);
public sealed class GameGroup
{
    public string Id { get; init; }
    public string Platform => GameSorting.Platform(Id);
    public string Title { get; private set; } = "";
    public string LocalTitle { get; init; } = "";
    public SaveJob[] Saves { get; set; }
    public void ApplyTitle(IReadOnlyDictionary<string, string> online)
    {
        Title = online.TryGetValue(Id, out string title) && GameTitleCatalog.IsUsableTitle(Id, title) ? title.Trim() : LocalTitle;
        Saves = Saves.Select(s => s with { Title = Title }).ToArray();
    }
    public HashSet<string> Selected { get; } = new(StringComparer.Ordinal);
    public bool Expanded { get; set; }
    public CheckState State => Selected.Count == 0 ? CheckState.Unchecked : Selected.Count == Saves.Length ? CheckState.Checked : CheckState.Indeterminate;
    public void SelectAll(bool value) { Selected.Clear(); if (value) foreach (var save in Saves) Selected.Add(save.Directory); }
    public void Toggle(string directory) { if (!Saves.Any(s => s.Directory == directory)) throw new ArgumentException("Unknown save"); if (!Selected.Remove(directory)) Selected.Add(directory); }
}
