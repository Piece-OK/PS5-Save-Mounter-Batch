using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace PS5SavefileBatcher;

// Name facts from the same JSON endpoint used by the official compatibility page.
// Queries contain only a validated missing CUSA ID; no save/user data is uploaded.
public sealed class Ps4TitleLookup(Func<string, CancellationToken, Task<string>> fetch = null)
{
    public const string SourceUrl = "https://shadps4.net/compatibility/";
    public const string Endpoint = "https://shadps4.net/scripts/search.php?q=";
    private static readonly HttpClient http = new();
    public sealed class CachedTitle
    {
        public string Title { get; set; }
        public DateTime CheckedUtc { get; set; }
    }
    public static string Parse(string id, string json)
    {
        using var document = JsonDocument.Parse(json);
        if (!document.RootElement.TryGetProperty("games", out var games) || games.ValueKind != JsonValueKind.Array)
            throw new IOException("Unexpected shadPS4 title response");
        foreach (var game in games.EnumerateArray())
            if (game.TryGetProperty("code", out var code) && code.ValueKind == JsonValueKind.String &&
                string.Equals(code.GetString(), id, StringComparison.OrdinalIgnoreCase) &&
                game.TryGetProperty("title", out var title) && title.ValueKind == JsonValueKind.String &&
                GameTitleCatalog.IsUsableTitle(id, title.GetString())) return title.GetString().Trim();
        return "";
    }
    private static async Task<string> Fetch(string id, CancellationToken token)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, Endpoint + Uri.EscapeDataString(id));
        request.Headers.UserAgent.ParseAdd("PS5SavefileBatcherBatch/" + BuildInfo.Version);
        using var response = await http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, token);
        response.EnsureSuccessStatusCode();
        if (response.Content.Headers.ContentLength > 1024*1024) throw new IOException("shadPS4 response exceeds size limit");
        using var stream = await response.Content.ReadAsStreamAsync(token);
        using var bytes = new MemoryStream(); byte[] buffer = new byte[16384]; int n;
        while ((n = await stream.ReadAsync(buffer, token)) > 0)
        {
            if (bytes.Length + n > 1024*1024) throw new IOException("shadPS4 response exceeds size limit");
            bytes.Write(buffer, 0, n);
        }
        return Encoding.UTF8.GetString(bytes.ToArray());
    }
    public async Task<Dictionary<string,string>> Load(IEnumerable<string> ids, string cachePath, Action<string> log, CancellationToken token)
    {
        var cache = new Dictionary<string,CachedTitle>(StringComparer.OrdinalIgnoreCase);
        try
        {
            if (new FileInfo(cachePath).Length <= 8*1024*1024)
                foreach (var item in JsonSerializer.Deserialize<Dictionary<string,CachedTitle>>(await File.ReadAllTextAsync(cachePath, token)))
                    if (Regex.IsMatch(item.Key, @"^CUSA\d{5}$") && item.Value != null) cache[item.Key] = item.Value;
        }
        catch (OperationCanceledException) { throw; }
        catch { }
        var result = new Dictionary<string,string>(StringComparer.OrdinalIgnoreCase);
        var missing = ids.Distinct(StringComparer.OrdinalIgnoreCase).Where(id => Regex.IsMatch(id, @"^CUSA\d{5}$")).ToArray();
        if (missing.Length == 0) return result;
        foreach (var id in missing)
            if (cache.TryGetValue(id, out var saved) && GameTitleCatalog.IsUsableTitle(id, saved.Title)) result[id] = saved.Title;
        log("PS4 title source: shadPS4 • queries contain missing CUSA IDs only • " + SourceUrl);
        using var budget = CancellationTokenSource.CreateLinkedTokenSource(token); budget.CancelAfter(TimeSpan.FromSeconds(30));
        foreach (var id in missing)
        {
            token.ThrowIfCancellationRequested();
            cache.TryGetValue(id, out var prior);
            if (prior != null && GameTitleCatalog.IsUsableTitle(id, prior.Title)) result[id] = prior.Title;
            if (prior != null && prior.CheckedUtc <= DateTime.UtcNow && DateTime.UtcNow-prior.CheckedUtc < TimeSpan.FromDays(7)) continue;
            if (budget.IsCancellationRequested) { log("PS4 title lookup time limit reached; keeping available names."); break; }
            try
            {
                using var timeout = CancellationTokenSource.CreateLinkedTokenSource(budget.Token); timeout.CancelAfter(TimeSpan.FromSeconds(12));
                log("Looking up PS4 title: " + id);
                string title = Parse(id, await (fetch ?? Fetch)(id, timeout.Token));
                cache[id] = new() { Title=title, CheckedUtc=DateTime.UtcNow };
                if (title.Length > 0) result[id] = title;
                else result.Remove(id);
            }
            catch (Exception ex) when (!token.IsCancellationRequested)
            { log("shadPS4 lookup unavailable; retaining cached/local names: " + ex.Message); break; }
        }
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(cachePath));
            await File.WriteAllTextAsync(cachePath + ".tmp", JsonSerializer.Serialize(cache, DumpRunner.JsonOptions), token);
            File.Move(cachePath + ".tmp", cachePath, true);
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex) { log("Could not cache PS4 titles: " + ex.Message); }
        return result;
    }
}
