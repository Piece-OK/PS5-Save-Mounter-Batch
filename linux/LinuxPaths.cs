namespace PS4Saves.Linux;

public sealed record LinuxPaths(string Config, string Cache, string Logs, string Backups)
{
    public string LegacyBackups { get; init; }
    public string SettingsFile => Path.Combine(Config, "settings.json");
    public static LinuxPaths Current => FromEnvironment(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), Environment.GetEnvironmentVariable);
    public static LinuxPaths FromEnvironment(string home, Func<string, string> env)
    {
        string Root(string key, string fallback) => env(key) is string v && Path.IsPathFullyQualified(v) ? v : Path.Combine(home, fallback);
        const string app = "ps5-save-mounter-batch";
        string image = env("APPIMAGE");
        string appDirectory = !string.IsNullOrWhiteSpace(image) && Path.IsPathFullyQualified(image)
            ? Path.GetDirectoryName(image) : AppContext.BaseDirectory;
        return new(Path.Combine(Root("XDG_CONFIG_HOME", ".config"), app), Path.Combine(Root("XDG_CACHE_HOME", ".cache"), app),
            Path.Combine(Root("XDG_STATE_HOME", ".local/state"), app, "logs"), Path.Combine(appDirectory, "PS5-Backups")) { LegacyBackups = Path.Combine(home, "PS5 Backups") };
    }
    public void EnsureDirectories()
    {
        foreach (var path in new[] { Config, Cache, Logs }) Directory.CreateDirectory(path);
    }
    public string ResolveDestination(string value) => Path.GetFullPath(string.IsNullOrWhiteSpace(value) ? Backups : Path.IsPathFullyQualified(value) ? value : Path.Combine(Backups, value));
}
