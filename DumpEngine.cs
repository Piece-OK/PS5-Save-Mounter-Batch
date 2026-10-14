using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Net.Sockets;

namespace PS4Saves;

public record SaveJob(int UserId, string Game, string Directory, string Title = "", string Subtitle = "");
public record RemoteEntry(string Name, bool IsDirectory, long Size);
public record FileReceipt(string Path, long Bytes, string Sha256);
public class SaveReceipt
{
    public SaveJob Save { get; set; }
    public string Status { get; set; } = "Pending";
    public string Error { get; set; }
    public string MountPath { get; set; }
    public bool? UnmountConfirmed { get; set; }
    public List<FileReceipt> Files { get; set; } = new();
}
public class RunReceipt
{
    public string ConsoleUserName { get; set; }
    public bool IncludeGameTitle { get; set; }
    public DateTime StartedUtc { get; set; } = DateTime.UtcNow;
    public DateTime? FinishedUtc { get; set; }
    public string Status { get; set; } = "Running";
    public List<SaveReceipt> Saves { get; set; } = new();
}
public interface ISaveMount
{
    string Mount(SaveJob job);
    void Unmount();
}
public sealed class PayloadMount(MounterClient client) : ISaveMount
{
    public string Mount(SaveJob j) => client.Mount(j.UserId, j.Game, j.Directory);
    public void Unmount() => client.Unmount();
}
public interface ISaveTransfer
{
    void DownloadTree(string remote, string local, List<FileReceipt> files, Action<string> log, CancellationToken token);
}

// LIST is deliberate: the PS5 FTP payload does not necessarily implement NLST or MLSD.
public sealed class SaveFtp(string host, int port, string user = "anonymous", string password = "") : ISaveTransfer
{
    // Test protocol readiness without implying a save should already be mounted.
    public void TestConnection()
    {
        using var tcp = new TcpClient();
        tcp.ConnectAsync(host, port).WaitAsync(TimeSpan.FromSeconds(5)).GetAwaiter().GetResult();
        tcp.ReceiveTimeout=10000; tcp.SendTimeout=10000;
        using var stream=tcp.GetStream();
        using var reader=new StreamReader(stream,Encoding.ASCII,false,1024,true);
        using var writer=new StreamWriter(stream,Encoding.ASCII,1024,true){NewLine="\r\n",AutoFlush=true};
        string ReadReply()
        {
            string line = reader.ReadLine();
            if (line == null || line.Length < 4) throw new IOException("FTP server closed the handshake");
            string code = line[..3];
            if (line[3] == '-')
                for (int i=0; ; i++)
                {
                    if (i >= 100 || (line=reader.ReadLine()) == null) throw new IOException("Incomplete FTP reply");
                    if (line.StartsWith(code + " ")) break;
                }
            return code;
        }
        void Expect(string code) { string actual=ReadReply(); if(actual!=code)throw new IOException("FTP handshake failed: expected "+code+", received "+actual); }
        Expect("220"); writer.WriteLine("USER " + user);
        string login = ReadReply();
        if (login == "331") { writer.WriteLine("PASS " + password); Expect("230"); }
        else if (login != "230") throw new IOException("FTP login failed: " + login);
        writer.WriteLine("TYPE I"); Expect("200"); writer.WriteLine("NOOP"); Expect("200");
        writer.WriteLine("QUIT"); Expect("221");
    }
    private FtpWebRequest Request(string path, string method)
    {
        var url = new UriBuilder("ftp", host, port) { Path = "/" }.Uri.AbsoluteUri.TrimEnd('/')
            + "/" + string.Join("/", path.TrimStart('/').Split('/').Select(Uri.EscapeDataString));
#pragma warning disable SYSLIB0014
        var req = (FtpWebRequest)WebRequest.Create(url);
#pragma warning restore SYSLIB0014
        req.Method = method;
        req.Credentials = new NetworkCredential(user, password);
        req.UseBinary = true;
        req.UsePassive = true;
        req.KeepAlive = false;
        req.Proxy = null;
        req.Timeout = 15000;
        req.ReadWriteTimeout = 30000;
        return req;
    }
    public static RemoteEntry ParseLine(string line)
    {
        var m = Regex.Match(line, @"^([d\-l])[rwxStTs\-]{9}\s+\d+\s+\S+\s+\S+\s+(\d+)\s+\S+\s+\d+\s+\S+\s+(.*)$");
        if (!m.Success) throw new IOException("Unsupported FTP listing: " + line);
        if (m.Groups[1].Value == "l") throw new IOException("Symbolic links are not followed: " + line);
        return new RemoteEntry(m.Groups[3].Value, m.Groups[1].Value == "d", long.Parse(m.Groups[2].Value));
    }
    public List<RemoteEntry> List(string path, CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        var req = Request(path.TrimEnd('/') + "/", WebRequestMethods.Ftp.ListDirectoryDetails);
        using var cancel = token.Register(req.Abort);
        using var response = (FtpWebResponse)req.GetResponse();
        using var reader = new StreamReader(response.GetResponseStream(), Encoding.UTF8);
        var result = new List<RemoteEntry>();
        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        string line;
        while ((line = reader.ReadLine()) != null)
        {
            token.ThrowIfCancellationRequested();
            if (string.IsNullOrWhiteSpace(line) || line.StartsWith("total ")) continue;
            var e = ParseLine(line);
            if (e.Name is "." or "..") continue;
            LocalNames.Validate(e.Name);
            if (!names.Add(e.Name))
                throw new IOException("Names collide on Windows: " + e.Name);
            result.Add(e);
        }
        return result;
    }
    public void DownloadTree(string remote, string local, List<FileReceipt> files, Action<string> log, CancellationToken token)
    {
        byte[] buffer = ArrayPool<byte>.Shared.Rent(128 * 1024);
        try { Walk(remote, local, "", files, log, token, 0, buffer); }
        finally { ArrayPool<byte>.Shared.Return(buffer, clearArray: true); }
    }
    private void Walk(string remote, string local, string relative, List<FileReceipt> files, Action<string> log, CancellationToken token, int depth, byte[] buffer)
    {
        if (depth > 64) throw new IOException("Save folder nesting exceeds 64 levels");
        Directory.CreateDirectory(local);
        foreach (var e in List(remote, token))
        {
            token.ThrowIfCancellationRequested();
            string remoteChild = remote.TrimEnd('/') + "/" + e.Name;
            string localChild = Path.Combine(local, e.Name);
            string relativeChild = relative + e.Name;
            if (e.IsDirectory) { Walk(remoteChild, localChild, relativeChild + "/", files, log, token, depth + 1, buffer); continue; }
            log($"Downloading {relativeChild} ({e.Size:N0} bytes)");
            string hash = "";
            for (int attempt = 1; ; attempt++)
            {
                try
                {
                    var req = Request(remoteChild, WebRequestMethods.Ftp.DownloadFile);
                    using var cancel = token.Register(req.Abort);
                    using var response = (FtpWebResponse)req.GetResponse();
                    using var input = response.GetResponseStream();
                    using var output = new FileStream(localChild, FileMode.Create, FileAccess.Write, FileShare.None);
                    using var digest = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
                    long count = 0;
                    int n;
                    while ((n = input.Read(buffer, 0, buffer.Length)) != 0)
                    {
                        token.ThrowIfCancellationRequested();
                        output.Write(buffer, 0, n);
                        digest.AppendData(buffer, 0, n);
                        count += n;
                    }
                    token.ThrowIfCancellationRequested();
                    if (count != e.Size) throw new IOException($"Size mismatch for {relativeChild}: expected {e.Size}, received {count}");
                    output.Flush(true);
                    hash = Convert.ToHexString(digest.GetHashAndReset());
                    break;
                }
                catch (Exception ex) when (!token.IsCancellationRequested && attempt < 3 && (ex is WebException || ex is IOException))
                { log($"Retry {attempt}/2: {relativeChild}: {ex.Message}"); }
            }
            files.Add(new FileReceipt(relativeChild, e.Size, hash));
        }
    }
}
public static class LocalNames
{
    public static void Validate(string value)
    {
        if (string.IsNullOrWhiteSpace(value) || value is "." or ".." || value.EndsWith('.') || value.EndsWith(' ')
            || value.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0 || value.Any(char.IsControl)
            || Regex.IsMatch(value, @"^(CON|PRN|AUX|NUL|COM[1-9]|LPT[1-9])(?:\.|$)", RegexOptions.IgnoreCase))
            throw new IOException("Cannot safely preserve this name on Windows: " + value);
    }
    public static void ValidateJob(SaveJob job)
    {
        Validate(job.Game); Validate(job.Directory);
        if (job.Game.Any(char.IsWhiteSpace) || job.Directory.Any(char.IsWhiteSpace))
            throw new IOException("The payload protocol cannot mount names containing whitespace");
    }
}
public sealed class DumpRunner(ISaveMount mount, ISaveTransfer ftp)
{
    public static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };
    public RunReceipt Run(IReadOnlyList<SaveJob> jobs, string runFolder, Action<string> log, Action<int,int> progress, CancellationToken token, DumpOptions options = null)
    {
        options ??= new();
        foreach (var job in jobs) LocalNames.ValidateJob(job);
        if (options.UserInRunName && jobs.Select(j => j.UserId).Distinct().Count() > 1)
            throw new IOException("A user-named run must contain only one console user's saves");
        if (jobs.Select(j=>$"{j.UserId:x}/{j.Game}/{j.Directory}").Distinct(StringComparer.OrdinalIgnoreCase).Count()!=jobs.Count)
            throw new IOException("Duplicate save paths in queue");
        if (Directory.Exists(runFolder)) throw new IOException("Run folder already exists; choose a new run folder");
        Directory.CreateDirectory(runFolder);
        var report = new RunReceipt { ConsoleUserName = options.ConsoleUserName, IncludeGameTitle = options.IncludeGameTitle, Saves = jobs.Select(j=>new SaveReceipt { Save=j }).ToList() };
        string reportPath = Path.Combine(runFolder, "report.json");
        void Persist()
        {
            File.WriteAllText(reportPath + ".tmp", JsonSerializer.Serialize(report, JsonOptions));
            File.Move(reportPath + ".tmp", reportPath, true);
        }
        Persist();
        try
        {
            for (int i = 0; i < jobs.Count; i++)
            {
                token.ThrowIfCancellationRequested();
                var job = jobs[i]; var receipt = report.Saves[i];
                string gameFolder = BackupNames.Game(job.Game, job.Title, options.IncludeGameTitle);
                string parent = options.UserInRunName ? Path.Combine(runFolder, gameFolder) : Path.Combine(runFolder, $"{job.UserId:x}", gameFolder);
                string final = Path.Combine(parent, job.Directory);
                string partial = Path.Combine(parent, job.Directory + ".partial");
                bool mountAttempted = false;
                bool cleanupOk = false;
                receipt.Status = "In progress";
                Persist();
                try
                {
                    log($"[{i+1}/{jobs.Count}] Mounting {job.Game} / {job.Directory}");
                    mountAttempted = true;
                    string remote = mount.Mount(job);
                    receipt.MountPath = remote;
                    // Do not copy /mnt/pfs wholesale: it may contain unrelated mounts.
                    string expected = $"/mnt/pfs/savedata_{job.UserId:x}_{job.Game}_{job.Directory}";
                    if (!string.Equals(remote?.TrimEnd('/'), expected, StringComparison.Ordinal))
                        throw new IOException("Unexpected mount path: " + remote);
                    token.ThrowIfCancellationRequested();
                    ftp.DownloadTree(remote, partial, receipt.Files, log, token);
                    token.ThrowIfCancellationRequested();
                }
                finally
                {
                    if (mountAttempted)
                    {
                        log("Unmounting " + job.Directory);
                        // Cleanup is intentionally independent of cancellation; never mount the next save after a failure.
                        try { mount.Unmount(); cleanupOk = true; receipt.UnmountConfirmed = true; }
                        catch { receipt.UnmountConfirmed = false; throw; }
                    }
                }
                if (!cleanupOk) throw new IOException("Unmount could not be confirmed");
                // Antivirus/indexing can briefly hold a newly written folder on Windows.
                // Retry promotion only, never remount or re-download an already completed save.
                for (int attempt=0; ; attempt++)
                {
                    try { Directory.Move(partial, final); break; }
                    catch (Exception ex) when (attempt<8 && (ex is IOException || ex is UnauthorizedAccessException))
                    { log("Waiting for Windows to release the completed download folder…"); Thread.Sleep(250*(attempt+1)); }
                }
                receipt.Status = "Complete";
                Persist();
                progress(i+1, jobs.Count);
                log($"Complete: {job.Directory} — {receipt.Files.Count} files, {receipt.Files.Sum(f=>f.Bytes):N0} bytes");
            }
            report.Status = "Complete";
        }
        catch (Exception ex)
        {
            var active = report.Saves.FirstOrDefault(s=>s.Status=="In progress");
            report.Status = token.IsCancellationRequested && active?.UnmountConfirmed != false ? "Cancelled" : "Failed";
            if (active != null) { active.Status = report.Status; active.Error = ex.ToString(); }
            log(report.Status + ": " + ex.Message);
            log("Queue stopped. Incomplete files remain in .partial folders. Reconnect before retrying if cleanup failed.");
        }
        finally { report.FinishedUtc = DateTime.UtcNow; Persist(); }
        return report;
    }
}
