using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace PS4Saves;

// Fetches title facts only. No website/emulator code, covers or compatibility reports are incorporated.
public sealed class GameTitleCatalog
{
    public const string SourceUrl = "https://github.com/KytyPS5/kytyps5.github.io/blob/main/src/data/games.json";
    public const string DownloadUrl = "https://raw.githubusercontent.com/KytyPS5/kytyps5.github.io/main/src/data/games.json";
    public const string HomebrewSourceUrl = "https://github.com/blackbearreloaded/ps5-homebrew-catalog";
    public const string HomebrewDownloadUrl = "https://homebrew.page/api/v1/index.json";
    private const int Limit = 8 * 1024 * 1024;
    private static readonly HttpClient http = new() { Timeout = TimeSpan.FromSeconds(12) };
    public static Dictionary<string, string> Parse(string json)
    {
        using var doc = JsonDocument.Parse(json);
        if (doc.RootElement.ValueKind != JsonValueKind.Array) throw new IOException("Unexpected game title database format");
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var row in doc.RootElement.EnumerateArray())
        {
            if (!row.TryGetProperty("name", out var name) || name.ValueKind != JsonValueKind.String) continue;
            string title = name.GetString()?.Trim();
            if (string.IsNullOrEmpty(title) || title.Length > 256 || title.Any(char.IsControl)) continue;
            void Add(JsonElement id)
            {
                if (id.ValueKind != JsonValueKind.String) return;
                string key = id.GetString().ToUpperInvariant().Replace("-", "");
                if (Regex.IsMatch(key, @"^(PPSA|CUSA)\d{5}$")) result.TryAdd(key, title);
            }
            if (row.TryGetProperty("titleId", out var primary)) Add(primary);
            if (row.TryGetProperty("allTitleIds", out var ids) && ids.ValueKind == JsonValueKind.Array)
                foreach (var id in ids.EnumerateArray()) Add(id);
        }
        if (result.Count == 0) throw new IOException("Game title database is empty");
        return result;
    }
    public async Task<Dictionary<string, string>> Load(string cachePath, bool enabled, Action<string> log, CancellationToken token)
        => await LoadSource(cachePath, enabled, log, token, false);
    public async Task<Dictionary<string, string>> LoadHomebrew(string cachePath, bool enabled, Action<string> log, CancellationToken token)
        => await LoadSource(cachePath, enabled, log, token, true);
    public static Dictionary<string, string> ParseHomebrew(string json)
    {
        using var doc = JsonDocument.Parse(json);
        if (!doc.RootElement.TryGetProperty("apps", out var apps) || apps.ValueKind != JsonValueKind.Array)
            throw new IOException("Unexpected homebrew title index format");
        var titles = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var app in apps.EnumerateArray())
        {
            if (!app.TryGetProperty("titleid", out var id) || id.ValueKind != JsonValueKind.String ||
                !app.TryGetProperty("name", out var name) || name.ValueKind != JsonValueKind.String) continue;
            string key = id.GetString().ToUpperInvariant(), title = name.GetString()?.Trim();
            if (Regex.IsMatch(key, @"^(PPSA|CUSA)\d{5}$") && IsUsableTitle(key, title)) titles.TryAdd(key, title);
        }
        if (titles.Count == 0) throw new IOException("Homebrew title index is empty");
        return titles;
    }
    private async Task<Dictionary<string, string>> LoadSource(string cachePath, bool enabled, Action<string> log, CancellationToken token, bool homebrew)
    {
        string label = homebrew ? "PS5 Homebrew Catalog" : "KytyPS5";
        string source = homebrew ? HomebrewSourceUrl : SourceUrl;
        string url = homebrew ? HomebrewDownloadUrl : DownloadUrl;
        Dictionary<string, string> ParseSource(string json) => homebrew ? ParseHomebrew(json) : Parse(json);
        if (!enabled) return new(StringComparer.OrdinalIgnoreCase);
        Dictionary<string, string> cached = null;
        try
        {
            if (new FileInfo(cachePath).Length <= Limit) cached = ParseSource(await File.ReadAllTextAsync(cachePath, token));
            if (cached != null && DateTime.UtcNow - File.GetLastWriteTimeUtc(cachePath) < TimeSpan.FromDays(7))
            { log($"Game titles: {label} cached database • " + source); return cached; }
        }
        catch (OperationCanceledException) { throw; }
        catch { }
        try
        {
            log($"Fetching game titles from {label} • " + source);
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(token);
            timeout.CancelAfter(TimeSpan.FromSeconds(12));
            var fetchToken = timeout.Token;
            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.UserAgent.ParseAdd("PS5SaveMounterBatch/" + BuildInfo.Version);
            using var response = await http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, fetchToken);
            response.EnsureSuccessStatusCode();
            if (response.Content.Headers.ContentLength > Limit) throw new IOException("Title database exceeds size limit");
            using var stream = await response.Content.ReadAsStreamAsync(fetchToken);
            using var bytes = new MemoryStream();
            byte[] buffer = new byte[65536]; int n;
            while ((n = await stream.ReadAsync(buffer, fetchToken)) > 0)
            {
                if (bytes.Length + n > Limit) throw new IOException("Title database exceeds size limit");
                bytes.Write(buffer, 0, n);
            }
            string json = System.Text.Encoding.UTF8.GetString(bytes.ToArray());
            var titles = ParseSource(json);
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(cachePath));
                await File.WriteAllTextAsync(cachePath + ".tmp", json, token);
                File.Move(cachePath + ".tmp", cachePath, true);
            }
            catch (OperationCanceledException) { throw; }
            catch (Exception ex) { log("Could not cache game titles: " + ex.Message); }
            log($"Game titles: {titles.Count} IDs from {label}."); return titles;
        }
        catch (Exception ex) when (!token.IsCancellationRequested)
        { log("Online titles unavailable; using cached/console/save metadata: " + ex.Message); return cached ?? new(StringComparer.OrdinalIgnoreCase); }
    }
    public static string Resolve(string id, IReadOnlyDictionary<string, string> online, IReadOnlyDictionary<string, string> console, IEnumerable<SearchResult> saves)
    {
        bool Valid(string value) => IsUsableTitle(id, value);
        if (online.TryGetValue(id, out string title) && Valid(title)) return title.Trim();
        if (console.TryGetValue(id, out title) && Valid(title)) return title.Trim();
        return saves.Select(s => s.Title).FirstOrDefault(Valid)?.Trim() ?? "";
    }
    public static bool IsUsableTitle(string id, string value)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Length > 256 || value.Any(char.IsControl)) return false;
        string title = Regex.Replace(value.Trim(), @"\s+", " ");
        if (string.Equals(title, id, StringComparison.OrdinalIgnoreCase) || Regex.IsMatch(title, @"^(PPSA|CUSA)-?\d{5}$", RegexOptions.IgnoreCase)) return false;
        return !new[] { "Gespeicherte Daten", "Speicherdaten", "Saved Data", "Save Data", "Saved Game Data", "Savedata", "Unknown", "Unbekannt" }
            .Contains(title, StringComparer.OrdinalIgnoreCase);
    }
}
