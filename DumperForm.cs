using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PS5SavefileBatcher;
public class DumperForm : Form
{
    private readonly TextBox host = new() { Width = 150, AccessibleName = "PS5 address" };
    private readonly NumericUpDown loaderPort = Port(9021);
    private readonly CheckBox deploy = new() { Text = "Send SAVEMOUNTER.ELF payload", Checked = true, AutoSize = true };
    private readonly Label loaderLabel = Label("ELF loader port");
    private readonly ListBox users = new() { Width = 340, Height = 82, IntegralHeight = false, SelectionMode = SelectionMode.One, AccessibleName = "Choose a console user" };
    private readonly CheckBox includeTitle = new() { Text = "Include game title in backup folder names", AutoSize = true };
    private readonly CheckBox onlineTitles = new() { Text = "Look up game titles online", AutoSize = true };
    private readonly TextBox destination = new() { Width = 650, AccessibleName = "Download folder" };
    private readonly DataGridView table = new()
    {
        Dock = DockStyle.Fill, ReadOnly = true, AllowUserToAddRows = false, AllowUserToDeleteRows = false,
        AllowUserToResizeRows = false, RowHeadersVisible = false, SelectionMode = DataGridViewSelectionMode.FullRowSelect,
        MultiSelect = false, BackgroundColor = Color.White, AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
        AccessibleName = "Games and saves"
    };
    private readonly TextBox logBox = new() { Dock = DockStyle.Fill, Multiline = true, ReadOnly = true, ScrollBars = ScrollBars.Vertical, Font = new Font("Consolas", 9), AccessibleName = "Activity log" };
    private readonly Label selection = new() { AutoSize = true, Text = "Load the library to choose games and individual saves." };
    private readonly Label timing = new() { AutoSize = true, Text = "Elapsed 00:00:00 • Remaining —" };
    private readonly Label status = new() { AutoSize = true, Text = "Start elfldr on your PS5, then connect. No separate FTP payload is needed." };
    private readonly BatchProgressBar progress = new() { Dock = DockStyle.Fill };
    private readonly Button connect = Button("Connect"), cancel = Button("Cancel safely"), load = Button("Load game library"),
        dump = Button("Dump selected saves"), all = Button("Select all"), none = Button("Unselect all");
    private readonly List<Control> busyControls = new();
    private readonly Stopwatch elapsed = new();
    private BatchTiming batchTiming = new();
    private readonly System.Windows.Forms.Timer timer = new() { Interval = 500 };
    private readonly ToolTip titleTooltip = new();
    private readonly List<GameGroup> library = new();
    private Dictionary<string, string> onlineCatalog = new(StringComparer.OrdinalIgnoreCase);
    private GameSortColumn sortColumn = GameSortColumn.Title;
    private bool sortDescending;
    private MounterClient client;
    private CancellationTokenSource cancellation;
    private bool busy, closing, transferReady;
    private int configuredMounterPort = 9090;
    private int? alternativeFtpPort;
    private int completed, total;
    private string connectedHost, lastRun;
    private readonly string settingsPath = Path.Combine(AppContext.BaseDirectory, "settings.json");
    private readonly string sessionLog = Path.Combine(AppContext.BaseDirectory, "Logs", "session_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + "_" + Guid.NewGuid().ToString("N")[..6] + ".log");
    private record UserChoice(int Id, string Name) { public override string ToString() => UserLabel.Display(Name, Id); }
    private record RowRef(GameGroup Group, SaveJob Save);
    private static NumericUpDown Port(int value) => new() { Minimum = 1, Maximum = 65535, Value = value, Width = 75 };
    private static Button Button(string text) => new() { Text = text, AutoSize = true, Height = 34, Padding = new Padding(6, 2, 6, 2) };
    private static FlowLayoutPanel Row() => new() { Dock = DockStyle.Fill, AutoSize = true, WrapContents = true, Padding = new Padding(0, 2, 0, 2) };
    private static Label Label(string text) => new() { Text = text, AutoSize = true, Margin = new Padding(3, 8, 6, 3) };

    public DumperForm()
    {
        Text = "PS5 Savefile Batcher — " + BuildInfo.Version; Font = new Font("Segoe UI", 10);
        ClientSize = new Size(1160, 880); MinimumSize = new Size(1050, 780); StartPosition = FormStartPosition.CenterScreen;
        DumperSettings s;
        try { s = JsonSerializer.Deserialize<DumperSettings>(File.ReadAllText(settingsPath)) ?? new(); } catch { s = new(); }
        host.Text = s.Host; configuredMounterPort = Math.Clamp(s.MounterPort, 1, 65535); loaderPort.Value = Math.Clamp(s.LoaderPort, 1, 65535);
        alternativeFtpPort = s.AlternativeFTPPort; deploy.Checked = s.SendPayload; includeTitle.Checked = s.IncludeGameTitle; onlineTitles.Checked = s.OnlineGameTitles;
        try { destination.Text = DumperSettings.ResolveDestination(s.Destination); } catch { destination.Text = DumperSettings.ResolveDestination("Backups"); }
        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(16), ColumnCount = 1, RowCount = 12 };
        for (int i = 0; i < 12; i++) layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles[7] = new RowStyle(SizeType.Percent, 70); layout.RowStyles[10] = new RowStyle(SizeType.Percent, 30);
        layout.Controls.Add(new Label { Text = "PS5 SAVEFILE BATCHER", Font = new Font("Segoe UI", 20, FontStyle.Bold), AutoSize = true }, 0, 0);
        var connection = Row();
        connection.Controls.AddRange(new Control[] { Label("PS5 address"), host, deploy, loaderLabel, loaderPort, connect });
        deploy.Margin = new Padding(10, 8, 8, 3);
        var loader = Row();
        var titleSource = new LinkLabel { Text = "Game title source: KytyPS5", AutoSize = true, Margin = new Padding(10, 8, 3, 3) };
        titleSource.LinkClicked += (_, _) => Process.Start(new ProcessStartInfo(GameTitleCatalog.SourceUrl) { UseShellExecute = true });
        var homebrewSource = new LinkLabel { Text = "Homebrew title source: Homebrew-catalog", AutoSize = true, Margin = new Padding(10, 8, 3, 3) };
        homebrewSource.LinkClicked += (_, _) => Process.Start(new ProcessStartInfo(GameTitleCatalog.HomebrewSourceUrl) { UseShellExecute = true });
        var ps4Source = new LinkLabel { Text = "PS4 title source: shadPS4", AutoSize = true, Margin = new Padding(10, 8, 3, 3) };
        ps4Source.LinkClicked += (_, _) => Process.Start(new ProcessStartInfo(Ps4TitleLookup.SourceUrl) { UseShellExecute = true });
        loader.Controls.AddRange(new Control[] { onlineTitles, titleSource, homebrewSource, ps4Source });
        titleTooltip.SetToolTip(onlineTitles, "KytyPS5 and Homebrew-catalog lists; missing CUSA IDs are queried on shadPS4. User names and save files are not sent.");
        var userRow = Row(); userRow.Controls.AddRange(new Control[] { Label("Choose a console user"), users, load });
        var folder = Row(); var browse = Button("Browse…"); var reset = Button("Use app Backups");
        folder.Controls.AddRange(new Control[] { Label("Download to"), destination, browse, reset, includeTitle });
        var actions = Row(); Button expand = Button("Expand all"), collapse = Button("Collapse all");
        actions.Controls.AddRange(new Control[] { all, none, expand, collapse, dump, cancel });
        table.Columns.Add(new DataGridViewCheckBoxColumn { HeaderText = "Choose", Name = "Choose", ThreeState = true, ValueType = typeof(CheckState), FillWeight = 8 });
        table.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Game / save — click a game to expand", FillWeight = 45 });
        table.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Title ID", FillWeight = 16 });
        table.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Platform", FillWeight = 9 });
        table.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Save count / selected", FillWeight = 22 });
        foreach (DataGridViewColumn column in table.Columns) column.SortMode = column.Index == 0 ? DataGridViewColumnSortMode.NotSortable : DataGridViewColumnSortMode.Programmatic;
        table.ColumnHeaderMouseClick += (_, e) =>
        {
            if (busy || e.ColumnIndex < 1) return;
            var chosen = (GameSortColumn)(e.ColumnIndex-1);
            sortDescending = chosen == sortColumn && !sortDescending; sortColumn = chosen;
            SortLibrary(); Render();
        };
        table.RowTemplate.Height = 28; table.ColumnHeadersHeight = 34;
        layout.Controls.Add(connection, 0, 1); layout.Controls.Add(loader, 0, 2); layout.Controls.Add(userRow, 0, 3);
        layout.Controls.Add(folder, 0, 4); layout.Controls.Add(actions, 0, 5); layout.Controls.Add(selection, 0, 6);
        layout.Controls.Add(table, 0, 7); layout.Controls.Add(progress, 0, 8);
        var timeRow = Row(); timeRow.Controls.Add(timing);
        Button open = Button("Open backup folder"), logs = Button("Open logs");
        timeRow.Controls.AddRange(new Control[] { open, logs });
        layout.Controls.Add(timeRow, 0, 9); layout.Controls.Add(logBox, 0, 10); layout.Controls.Add(status, 0, 11);
        Controls.Add(layout);
        busyControls.AddRange(new Control[] { connection, loader, userRow, folder, table, all, none, expand, collapse, dump });
        deploy.CheckedChanged += (_, _) => UpdateControls();
        onlineTitles.CheckedChanged += async (_, _) =>
        {
            SaveSettings();
            if (busy || library.Count == 0) return;
            if (!onlineTitles.Checked) { ApplyTitles(); Log("Online titles disabled; restored console/save titles. Unknown titles are blank."); }
            else await Operation(async () => { await LoadOnlineTitles(); await LoadPs4Titles(library); ApplyTitles(); Log("Online titles enabled; refreshed the loaded library."); }, true);
        };
        users.SelectedIndexChanged += (_, _) => { library.Clear(); Render(); };
        connect.Click += async (_, _) => await Operation(async () =>
        {
            if (client != null && transferReady)
            {
                await Task.Run(() => client.Disconnect()); client = null; transferReady = false; users.Items.Clear(); library.Clear(); Render();
                Log("Disconnected; payload stopped."); return;
            }
            SaveSettings(); var settings = ReadSettings(); bool send = deploy.Checked;
            if (client == null)
            {
                try { client = await Task.Run(() => DumperConnection.Connect(settings, send, Log)); connectedHost = settings.Host; }
                catch (Exception ex) { throw new ConnectionFailure("Could not connect to the PS5. Check the IP address and make sure the ELF loader or Save Mounter is running on the configured port.", ex); }
            }
            try { await Task.Run(() => Ftp().TestConnection()); }
            catch (Exception ex) { throw new ConnectionFailure("Connected to the PS5, but the FTP transfer connection failed. Check your FTP service or AlternativeFTPPort in settings.json, then click Retry transfer.", ex); }
            transferReady = true;
            var found = await Task.Run(() => client.GetUsers()); users.Items.Clear();
            foreach (var u in found) users.Items.Add(new UserChoice(u.id, u.name));
            if (users.Items.Count > 0) users.SelectedIndex = 0;
            Log("Transfer connection verified in binary mode.");
            Log($"Ready: {found.Length} local users found. Select a user, then Load game library.");
        });
        load.Click += async (_, _) => await Operation(async () =>
        {
            int uid = UserId(); var ids = await Task.Run(() => client.ListSaves(uid));
            Dictionary<string, string> names = new();
            try { names = await Task.Run(() => client.GetGameTitles()); } catch (Exception ex) { Log("Game titles unavailable: " + ex.Message); }
            if (onlineTitles.Checked) await LoadOnlineTitles();
            var groups = new List<GameGroup>(); int n = 0;
            foreach (var id in ids.Distinct())
            {
                cancellation.Token.ThrowIfCancellationRequested(); Log($"Loading game {++n}/{ids.Length}: {id}");
                var found = await Task.Run(() => client.Search(uid, id));
                found = found.Where(x => !string.IsNullOrWhiteSpace(x.DirName)).ToArray();
                if (found.Length == 0) { Log("Skipping game folder with no saves: " + id); continue; }
                string title = GameTitleCatalog.Resolve(id, new Dictionary<string,string>(), names, found);
                groups.Add(new GameGroup { Id = id, LocalTitle = title, Saves = found.Select(x => new SaveJob(uid, id, x.DirName, title, x.Subtitle)).ToArray() });
            }
            if (onlineTitles.Checked) await LoadPs4Titles(groups);
            library.Clear(); library.AddRange(groups); ApplyTitles();
            Log($"Loaded {library.Count} games and {library.Sum(x => x.Saves.Length)} saves for user {uid:x}.");
        }, true);
        browse.Click += (_, _) =>
        {
            using var d = new FolderBrowserDialog { Description = "Choose the download folder", UseDescriptionForTitle = true, SelectedPath = destination.Text };
            if (d.ShowDialog(this) == DialogResult.OK) { destination.Text = d.SelectedPath; SaveSettings(); }
        };
        reset.Click += (_, _) => { destination.Text = DumperSettings.ResolveDestination("Backups"); SaveSettings(); };
        all.Click += (_, _) => { foreach (var g in library) g.SelectAll(true); Render(); };
        none.Click += (_, _) => { foreach (var g in library) g.SelectAll(false); Render(); };
        expand.Click += (_, _) => { foreach (var g in library) g.Expanded = true; Render(); };
        collapse.Click += (_, _) => { foreach (var g in library) g.Expanded = false; Render(); };
        table.CellClick += (_, e) =>
        {
            if (busy || e.RowIndex < 0) return; var r = (RowRef)table.Rows[e.RowIndex].Tag;
            if (e.ColumnIndex == 0)
            {
                if (r.Save == null)
                {
                    r.Group.SelectAll(r.Group.State != CheckState.Checked);
                }
                else r.Group.Toggle(r.Save.Directory);
                Render();
            }
            else if (r.Save == null) { r.Group.Expanded = !r.Group.Expanded; Render(); }
        };
        dump.Click += async (_, _) => await Operation(async () =>
        {
            UserId();
            var jobs = library.SelectMany(g => g.Saves.Where(j => g.Selected.Contains(j.Directory))).ToArray();
            if (jobs.Length == 0) throw new InvalidOperationException("Check at least one game or save first.");
            SaveSettings(); var transfer = Ftp(); await Task.Run(() => transfer.TestConnection()); var token = cancellation.Token;
            var selectedUser = (UserChoice)users.SelectedItem;
            var options = new DumpOptions { IncludeGameTitle = includeTitle.Checked, UserInRunName = true, ConsoleUserName = selectedUser.Name };
            lastRun = BackupNames.Run(DumperSettings.ResolveDestination(destination.Text), selectedUser.Name, selectedUser.Id, DateTime.Now);
            completed = 0; total = jobs.Length; progress.Maximum = total; progress.Value = 0;
            batchTiming = new() { Status = "Running" }; UpdateTime();
            elapsed.Restart(); timer.Start(); Log($"Starting {total} selected saves → {lastRun}");
            try
            {
                var receipt = await Task.Run(() => new DumpRunner(new PayloadMount(client), transfer).Run(jobs, lastRun, Log,
                    (n, t) => BeginInvoke((Action)(() => { completed = n; progress.Value = n; UpdateTime(); })), token, options));
                completed = receipt.Saves.Count(x => x.Status == "Complete");
                batchTiming.Status = receipt.Status;
                Log($"{receipt.Status}: {receipt.Saves.Count(x => x.Status == "Complete")}/{total} saves. Report: {Path.Combine(lastRun, "report.json")}");
                if (receipt.Status != "Complete")
                {
                    client.Dispose(); client = null; transferReady = false;
                    Log("Connection closed after interruption. Completed saves and error report are preserved.");
                }
            }
            finally
            {
                if (batchTiming.Status is "Running" or "Cancelling") batchTiming.Status = token.IsCancellationRequested ? "Cancelled" : "Failed";
                elapsed.Stop(); timer.Stop(); progress.Style = ProgressBarStyle.Blocks; progress.Value = completed; UpdateTime();
            }
        }, true);
        cancel.Click += async (_, _) => await RequestCancellation();
        open.Click += (_, _) => OpenFolder(lastRun ?? DumperSettings.ResolveDestination(destination.Text));
        logs.Click += (_, _) => OpenFolder(Path.GetDirectoryName(sessionLog));
        timer.Tick += (_, _) => UpdateTime();
        FormClosing += async (_, e) =>
        {
            if (closing) return;
            if (busy) { e.Cancel = true; await RequestCancellation(); Log("Please wait for cleanup, then close again."); return; }
            SaveSettings();
            if (client != null)
            {
                e.Cancel = true; closing = true; Enabled = false;
                await Task.Run(() => client.Disconnect()); client = null; Close();
            }
        };
        FormClosed += (_, _) => { timer.Dispose(); titleTooltip.Dispose(); };
        UpdateControls(); Log("Save Mounter development build " + BuildInfo.Version + " ready. Logs: " + sessionLog);
    }
    private void OpenFolder(string path)
    {
        try { Directory.CreateDirectory(path); Process.Start(new ProcessStartInfo(path) { UseShellExecute = true }); }
        catch (Exception ex) { Log(ex.Message); }
    }
    private void Render()
    {
        int first = table.FirstDisplayedScrollingRowIndex;
        table.Rows.Clear();
        foreach (var g in library)
        {
            int i = table.Rows.Add(g.State, (g.Expanded ? "▾ " : "▸ ") + g.Title, g.Id, g.Platform, $"{g.Saves.Length} saves / {g.Selected.Count} selected");
            table.Rows[i].Tag = new RowRef(g, null); table.Rows[i].DefaultCellStyle.BackColor = Color.FromArgb(237, 242, 248);
            if (g.Expanded) foreach (var j in g.Saves)
                {
                    i = table.Rows.Add(g.Selected.Contains(j.Directory) ? CheckState.Checked : CheckState.Unchecked,
                        "      " + j.Directory + (string.IsNullOrEmpty(j.Subtitle) ? "" : " — " + j.Subtitle), "", g.Platform, "");
                    table.Rows[i].Tag = new RowRef(g, j);
                }
        }
        if (first >= 0 && first < table.Rows.Count) table.FirstDisplayedScrollingRowIndex = first;
        selection.Text = $"{library.Count} games • {library.Sum(g => g.Saves.Length)} saves • {library.Sum(g => g.Selected.Count)} selected • applies to the selected console user";
    }
    private async Task LoadOnlineTitles()
    {
        var catalog = new GameTitleCatalog();
        string cache = Path.Combine(AppContext.BaseDirectory, "Cache");
        var commercial = await catalog.Load(Path.Combine(cache, "kyty-game-titles.json"), true, Log, cancellation.Token);
        var homebrew = await catalog.LoadHomebrew(Path.Combine(cache, "homebrew-game-titles.json"), true, Log, cancellation.Token);
        onlineCatalog = commercial;
        foreach (var item in homebrew) onlineCatalog[item.Key] = item.Value;
    }
    private void ApplyTitles()
    {
        IReadOnlyDictionary<string,string> titles = onlineTitles.Checked ? onlineCatalog : new Dictionary<string,string>();
        foreach (var group in library) group.ApplyTitle(titles);
        SortLibrary();
        Render();
    }
    private void SortLibrary()
    {
        GameSorting.Sort(library, sortColumn, sortDescending);
        foreach (DataGridViewColumn column in table.Columns)
            column.HeaderCell.SortGlyphDirection = column.Index == (int)sortColumn+1 ? (sortDescending ? SortOrder.Descending : SortOrder.Ascending) : SortOrder.None;
    }
    private async Task LoadPs4Titles(IEnumerable<GameGroup> groups)
    {
        var missing = groups.Where(g => g.Platform == "PS4" && string.IsNullOrEmpty(g.LocalTitle) && !onlineCatalog.ContainsKey(g.Id)).Select(g => g.Id);
        var titles = await new Ps4TitleLookup().Load(missing, Path.Combine(AppContext.BaseDirectory,"Cache","shadps4-game-titles.json"), Log, cancellation.Token);
        foreach (var item in titles) onlineCatalog[item.Key] = item.Value;
    }
    private void UpdateTime()
    {
        timing.Text = batchTiming.Display(elapsed.Elapsed, completed, total, elapsed.IsRunning);
        progress.SetOutcome(batchTiming, completed, total);
    }
    private async Task RequestCancellation()
    {
        var source = cancellation;
        if (source == null || source.IsCancellationRequested) return;
        cancel.Enabled = false;
        if (elapsed.IsRunning) { batchTiming.Status = "Cancelling"; progress.Style = ProgressBarStyle.Marquee; UpdateTime(); }
        Log("Cancellation requested; waiting for transfer and unmount cleanup.");
        // FTP Abort callbacks may block. Keep the UI and elapsed clock responsive during cleanup.
        await Task.Run(() => { try { source.Cancel(); } catch (ObjectDisposedException) { } });
    }
    private void UpdateControls()
    {
        connect.Text = client == null ? "Connect" : transferReady ? "Disconnect" : "Retry transfer";
        host.Enabled = client == null && !busy;
        deploy.Enabled = client == null && !busy;
        loaderLabel.Enabled = loaderPort.Enabled = deploy.Checked && client == null && !busy;
        load.Enabled = users.Enabled = client != null && transferReady && !busy;
        cancel.Enabled = busy && cancellation != null;
    }
    private DumperSettings ReadSettings()
    {
        string dest = destination.Text.Trim();
        if (string.Equals(DumperSettings.ResolveDestination(dest).TrimEnd('\\'), DumperSettings.ResolveDestination("Backups").TrimEnd('\\'), StringComparison.OrdinalIgnoreCase)) dest = "Backups";
        return new() { Host = host.Text.Trim(), SendPayload = deploy.Checked, MounterPort = configuredMounterPort, LoaderPort = (int)loaderPort.Value,
            AlternativeFTPPort = alternativeFtpPort, IncludeGameTitle = includeTitle.Checked, OnlineGameTitles = onlineTitles.Checked, Destination = dest };
    }
    private void SaveSettings()
    {
        try { File.WriteAllText(settingsPath, JsonSerializer.Serialize(ReadSettings(), DumpRunner.JsonOptions)); }
        catch (Exception ex) { Log("Could not save settings: " + ex.Message); }
    }
    private SaveFtp Ftp() => new(connectedHost, new DumperSettings { AlternativeFTPPort = alternativeFtpPort }.TransferPort(client.EmbeddedFtpPort));
    private int UserId()
    {
        if (client == null || users.SelectedItem is not UserChoice u) throw new InvalidOperationException("Connect and select a console user first.");
        return u.Id;
    }
    private async Task Operation(Func<Task> action, bool canCancel = false)
    {
        if (busy) return;
        busy = true; cancellation = new();
        foreach (var c in busyControls) c.Enabled = false; cancel.Enabled = canCancel;
        try { await action(); }
        catch (Exception ex)
        {
            Log((cancellation.IsCancellationRequested ? "Cancelled: " : "Error: ") + ex);
            if (client != null && !client.IsConnected) { client.Dispose(); client = null; transferReady = false; }
            if (ex is ConnectionFailure) MessageBox.Show(this, ex.Message, "Connection failed", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
        finally
        {
            busy = false; cancellation.Dispose(); cancellation = null;
            foreach (var c in busyControls) c.Enabled = true; UpdateControls();
        }
    }
    private void Log(string text)
    {
        if (IsDisposed) return;
        if (InvokeRequired) { BeginInvoke((Action)(() => Log(text))); return; }
        status.Text = text.Length > 135 ? text[..132] + "…" : text;
        string line = $"{DateTime.Now:O} {text}{Environment.NewLine}";
        logBox.AppendText($"{DateTime.Now:HH:mm:ss} {text}{Environment.NewLine}");
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(sessionLog)); File.AppendAllText(sessionLog, line);
            if (lastRun != null && Directory.Exists(lastRun)) File.AppendAllText(Path.Combine(lastRun, "activity.log"), line);
        }
        catch { }
    }

}
