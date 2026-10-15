using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Themes.Fluent;
using Avalonia.Threading;
using Microsoft.Data.Sqlite;
using System.Text.Json;

[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("Linux.Tests")]

namespace PS4Saves.Linux;

internal static class Program
{
    public static string SmokeOutput;
    [STAThread]
    public static int Main(string[] args)
    {
        try
        {
            if (args.Length > 0)
            {
                if (args[0] == "--version") { Console.WriteLine(BuildInfo.Version + " (Linux)"); return 0; }
                if (args[0] is "--help" or "-h") { Console.WriteLine("PS5 Save Mounter Batch (Linux)\nNo arguments: open GUI\n--version\n--help\n--smoke [result.txt]  Open the real GUI, check SQLite and close; no console connection\n--inventory result.json host\n--dump result.json host userHex game save1,save2 destination\nConnection/deployment preferences: XDG_CONFIG_HOME/ps5-save-mounter-batch/settings.json\n--dump writes backups and temporarily mounts saves; Ctrl+C cancels with unmount cleanup."); return 0; }
                if (args[0] == "--smoke" && args.Length <= 2) SmokeOutput = args.Length == 2 ? Path.GetFullPath(args[1]) : Path.Combine(LinuxPaths.Current.Cache, "smoke-result.txt");
                else return Diagnostics.Run(args);
            }
            BuildAvaloniaApp().StartWithClassicDesktopLifetime(Array.Empty<string>());
            return Environment.ExitCode;
        }
        catch (Exception ex) { Console.Error.WriteLine(ex); return 1; }
    }
    public static AppBuilder BuildAvaloniaApp() => AppBuilder.Configure<LinuxApp>().UsePlatformDetect();
}

public sealed class LinuxApp : Application
{
    public override void Initialize() => Styles.Add(new FluentTheme());
    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var window = new MainWindow(); desktop.MainWindow = window;
            if (Program.SmokeOutput != null)
            {
                // Allow visible layout and desktop integrations to initialize before testing controls.
                window.Opened += (_, _) => DispatcherTimer.RunOnce(() =>
                {
                    try
                    {
                        if (!window.IsVisible || window.Bounds.Width < 100 || window.Content == null) throw new Exception("GUI did not become visible");
                        window.VerifySmokeSelection();
                        using var db = new SqliteConnection("Data Source=:memory:"); db.Open();
                        using var cmd = db.CreateCommand(); cmd.CommandText = "SELECT sqlite_version()";
                        Directory.CreateDirectory(Path.GetDirectoryName(Program.SmokeOutput));
                        File.WriteAllText(Program.SmokeOutput, $"GUI startup/layout OK; selection OK; header sorting/reversal OK; visible sources OK; completion colors OK; SQLite {cmd.ExecuteScalar()}; runtime {Environment.Version}; backup default {LinuxPaths.Current.Backups}; no console connection\n");
                        if (Environment.GetEnvironmentVariable("PS5_SMOKE_SCREENSHOT") is string screenshot)
                            DispatcherTimer.RunOnce(() =>
                            {
                                try
                                {
                                    using var image = new Avalonia.Media.Imaging.RenderTargetBitmap(new PixelSize((int)window.Bounds.Width, (int)window.Bounds.Height));
                                    image.Render(window); image.Save(screenshot, new Avalonia.Media.Imaging.PngBitmapEncoderOptions()); window.Close();
                                }
                                catch (Exception ex) { Console.Error.WriteLine(ex); Environment.ExitCode = 1; desktop.Shutdown(1); }
                            }, TimeSpan.FromMilliseconds(300));
                        else window.Close();
                    }
                    catch (Exception ex) { Console.Error.WriteLine(ex); Environment.ExitCode = 1; desktop.Shutdown(1); }
                }, TimeSpan.FromSeconds(3));
            }
        }
        base.OnFrameworkInitializationCompleted();
    }
}

internal static class Diagnostics
{
    internal static RunReceipt RunDump(DumpRunner runner, LinuxPaths paths, DumperSettings settings,
        SaveJob[] jobs, string destination, string userName, int userId, DateTime time, CancellationToken token)
        => runner.Run(jobs, BackupNames.Run(paths.ResolveDestination(destination), userName, userId, time), Console.Error.WriteLine, (_, _) => { }, token,
            new DumpOptions { IncludeGameTitle = settings.IncludeGameTitle, UserInRunName = true, ConsoleUserName = userName });

    public static int Run(string[] args)
    {
        if (!(args.Length == 3 && args[0] == "--inventory") && !(args.Length == 7 && args[0] == "--dump"))
            throw new ArgumentException("Unknown command or argument count; use --help. No connection attempted.");
        var paths = LinuxPaths.Current; paths.EnsureDirectories();
        var s = SettingsStore.Load(paths); s.Host = args[2];
        // Validate all local dump inputs before opening a connection.
        int uid = args[0] == "--dump" ? Convert.ToInt32(args[3], 16) : 0;
        var jobs = args[0] == "--dump" ? args[5].Split(',').Select(n => new SaveJob(uid, args[4], n)).ToArray() : Array.Empty<SaveJob>();
        foreach (var j in jobs) LocalNames.ValidateJob(j);
        string output = Path.GetFullPath(args[1]);
        using var cts = new CancellationTokenSource();
        ConsoleCancelEventHandler handler = (_, e) => { e.Cancel = true; cts.Cancel(); Console.Error.WriteLine("Cancelling; waiting for unmount cleanup…"); };
        Console.CancelKeyPress += handler;
        MounterClient client = null;
        try
        {
            client = DumperConnection.Connect(s, s.SendPayload, Console.Error.WriteLine);
            var users = client.GetUsers();
            if (args[0] == "--inventory")
            {
                var inventory = users.Select(u => { cts.Token.ThrowIfCancellationRequested(); return new { UserId = u.id.ToString("x"), u.name, Games = client.ListSaves(u.id).Select(g => { cts.Token.ThrowIfCancellationRequested(); return new { Id = g, Saves = client.Search(u.id, g).Select(v => new { v.DirName, v.Title, v.Subtitle }).ToArray() }; }).ToArray() }; }).ToArray();
                File.WriteAllText(output, JsonSerializer.Serialize(inventory, DumpRunner.JsonOptions));
                return 0;
            }
            var user = users.First(u => u.id == uid);
            var found = client.Search(uid, args[4]);
            if (jobs.Any(j => !found.Any(f => f.DirName == j.Directory))) throw new ArgumentException("Requested save not found");
            var receipt = RunDump(new DumpRunner(new PayloadMount(client), new SaveFtp(s.Host, s.TransferPort(client.EmbeddedFtpPort))), paths, s, jobs, args[6], user.name, uid, DateTime.Now, cts.Token);
            File.WriteAllText(output, JsonSerializer.Serialize(receipt, DumpRunner.JsonOptions));
            return receipt.Status == "Complete" ? 0 : 1;
        }
        finally { client?.Disconnect(); Console.CancelKeyPress -= handler; }
    }
}

public static class SettingsStore
{
    public static DumperSettings Load(LinuxPaths paths)
    {
        DumperSettings s;
        try { s = JsonSerializer.Deserialize<DumperSettings>(File.ReadAllText(paths.SettingsFile)) ?? new(); }
        catch { s = new() { Destination = paths.Backups }; }
        if (s.Destination == paths.LegacyBackups || s.Destination == "Backups") s.Destination = "";
        s.Destination = paths.ResolveDestination(s.Destination);
        return s;
    }
    public static void Save(LinuxPaths paths, DumperSettings s)
    {
        Directory.CreateDirectory(paths.Config);
        var stored = JsonSerializer.SerializeToNode(s, DumpRunner.JsonOptions);
        if (paths.ResolveDestination(s.Destination) == paths.Backups) stored["Destination"] = "";
        File.WriteAllText(paths.SettingsFile + ".tmp", stored.ToJsonString(DumpRunner.JsonOptions));
        File.Move(paths.SettingsFile + ".tmp", paths.SettingsFile, true);
    }
}
