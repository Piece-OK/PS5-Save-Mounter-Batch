using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace PS4Saves;

public static class BuildInfo
{
    public const string Version = "1.1-beta1";
    public const string PayloadFile = "savemounter.elf";
}
public static class UserLabel
{
    public static string Display(string name, int id) => $"{name} [{id:x}]";
}

public static class BackupNames
{
    // Labels are optional additions. Preserve the original title ID even when a label is unsafe or absent.
    public static string Label(string value)
    {
        string clean = Regex.Replace(value ?? "", @"[\s<>:""/\\|?*\p{C}]+", "_").Trim('_', '.', ' ');
        if (clean.Length > 64) clean = clean[..64].TrimEnd('.', ' ', '_');
        if (string.IsNullOrEmpty(clean)) return "";
        if (Regex.IsMatch(clean, @"^(CON|PRN|AUX|NUL|COM[1-9]|LPT[1-9])(?:\.|$)", RegexOptions.IgnoreCase)) clean = "_" + clean;
        return clean;
    }
    public static string Game(string id, string title, bool includeTitle)
    {
        LocalNames.Validate(id);
        string label = includeTitle && !string.Equals(id, title?.Trim(), StringComparison.OrdinalIgnoreCase) ? Label(title) : "";
        return string.IsNullOrEmpty(label) ? id : id + "-" + label;
    }
    public static string Run(string root, string user, int userId, DateTime time)
    {
        string label = Label(user);
        if (string.IsNullOrEmpty(label)) label = "User_" + userId.ToString("x");
        string path = Path.Combine(root, time.ToString("yyyy_MM_dd-HHmmss") + "-[" + label + "]");
        string unique = path;
        for (int n = 2; Directory.Exists(unique) || File.Exists(unique); n++) unique = path + "-" + n;
        return unique;
    }
}

public class DumpOptions
{
    public bool IncludeGameTitle { get; init; }
    public bool UserInRunName { get; init; }
    public string ConsoleUserName { get; init; }
}

public sealed class BatchTiming
{
    public string Status { get; set; } = "Ready";
    public bool IsSuccessful(int completed, int total) => Status == "Complete" && total > 0 && completed == total;
    public string LastEta { get; private set; } = "estimating…";
    public string Display(TimeSpan elapsed, int completed, int total, bool running)
    {
        static string Time(TimeSpan t) => $"{(int)t.TotalHours:00}:{t.Minutes:00}:{t.Seconds:00}";
        if (Status == "Running" && completed > 0 && completed < total)
            LastEta = "~" + Time(TimeSpan.FromSeconds(elapsed.TotalSeconds / completed * (total - completed)));
        string eta = Status == "Complete" ? "Remaining 00:00:00"
            : Status == "Running" ? "Remaining " + LastEta
            : Status == "Cancelling" ? "Last ETA " + LastEta + " • finishing cleanup"
            : Status is "Cancelled" or "Failed" ? "Last ETA " + LastEta + " (stopped)"
            : "Remaining —";
        return $"{Status} • Elapsed {Time(elapsed)} • {eta} • {completed}/{total} saves";
    }
}
