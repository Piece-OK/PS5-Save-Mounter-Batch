using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Threading;
using Avalonia.VisualTree;
using System.Diagnostics;

namespace PS5SavefileBatcher.Linux;

public sealed class MainWindow : Window
{
    private readonly LinuxPaths paths = LinuxPaths.Current;
    private readonly TextBox host = new() { Width = 180, PlaceholderText = "PS5 IP Adress" };
    private readonly NumericUpDown loaderPort = Port(9021), ftpPort = new() { Minimum = 0, Maximum = 65535, Width = 150, Value = 0 };
    private readonly CheckBox deploy = new() { Content = "Deploy savemounter.elf" }, online = new() { Content = "online title lookup from:" }, includeTitle = new() { Content = "Include game title in folder names" };
    private readonly TextBox destination = new() { MinWidth = 330, HorizontalAlignment = HorizontalAlignment.Stretch };
    private readonly ComboBox users = new() { MinWidth = 250 };
    private readonly ComboBox sort = new() { Width = 140, ItemsSource = new[] { "Title", "Title ID", "Platform", "Save count" }, SelectedIndex = 0 };
    private readonly CheckBox descending = new() { Content = "Descending" };
    private readonly StackPanel games = new() { Spacing = 0 };
    private readonly Grid tableHeader = TableRow();
    private readonly Dictionary<GameSortColumn, Button> columnButtons = new();
    private readonly TextBox logBox = new() { IsReadOnly = true, AcceptsReturn = true, TextWrapping = Avalonia.Media.TextWrapping.Wrap, MinHeight = 110 };
    private readonly TextBlock status = new() { TextWrapping = Avalonia.Media.TextWrapping.Wrap }, selection = new(), timing = new();
    private readonly ProgressBar progress = new() { Minimum = 0, Maximum = 1, Height = 18 };
    private readonly Button connect = MakeButton("Connect"), load = MakeButton("Load library"), dump = MakeButton("Dump selected saves"), cancel = MakeButton("Cancel safely");
    private readonly List<Control> locked = new();
    private readonly List<GameGroup> library = new();
    private readonly Stopwatch elapsed = new();
    private readonly DispatcherTimer clock = new() { Interval = TimeSpan.FromMilliseconds(500) };
    private BatchTiming batchTiming = new();
    private CancellationTokenSource cancellation;
    private MounterClient client;
    private bool busy, closing, closeRequested;
    private int completed, total;
    private string connectedHost, sessionLog, lastRun;
    private Dictionary<string, string> catalog = new(StringComparer.OrdinalIgnoreCase);
    private sealed class GameCheckBox : CheckBox
    {
        protected override Type StyleKeyOverride => typeof(CheckBox);
        protected override void Toggle() => SetCurrentValue(IsCheckedProperty, IsChecked != true);
        // Exercise the same OnClick/Toggle path used by pointer and keyboard activation.
        public void SmokeClick() => OnClick();
    }
    private record UserChoice(int Id, string Name) { public override string ToString() => UserLabel.Display(Name, Id); }
    private static NumericUpDown Port(int value) => new() { Minimum = 1, Maximum = 65535, Width = 150, Value = value };
    private static Button MakeButton(string label) => new() { Content = label, Padding = new Thickness(12, 7) };
    private static TextBlock Label(string text) => new() { Text = text, VerticalAlignment = VerticalAlignment.Center };
    private static Grid TableRow() => new() { ColumnDefinitions = new ColumnDefinitions("70,*,160,95,180"), MinHeight = 36 };
    private static void Cell(Grid row, Control control, int column)
    {
        Grid.SetColumn(control, column); control.Margin = new Thickness(8, 2);
        control.VerticalAlignment = VerticalAlignment.Center; row.Children.Add(control);
    }
    private Button SourceLink(string label, string url)
    {
        var link = MakeButton(label); link.Tag = new Uri(url);
        link.Foreground = Avalonia.Media.Brushes.DodgerBlue;
        link.Background = Avalonia.Media.Brushes.Transparent;
        ToolTip.SetTip(link, url);
        link.Click += (_, _) =>
        {
            try { Process.Start(new ProcessStartInfo("xdg-open") { ArgumentList = { url }, UseShellExecute = false }); }
            catch (Exception ex) { Log("Could not open source: " + ex.Message); }
        };
        return link;
    }
    private static WrapPanel Row(params Control[] controls)
    {
        var row = new WrapPanel { Orientation = Orientation.Horizontal };
        foreach (var c in controls) { c.Margin = new Thickness(0, 0, 10, 8); c.VerticalAlignment = VerticalAlignment.Center; row.Children.Add(c); }
        return row;
    }

    public MainWindow()
    {
        Title = "PS5 Savefile Batcher — Linux " + BuildInfo.Version;
        Width = 1200; Height = 920; MinWidth = 1050; MinHeight = 780;
        paths.EnsureDirectories();
        sessionLog = Path.Combine(paths.Logs, $"session_{DateTime.Now:yyyyMMdd_HHmmss}_{Guid.NewGuid():N}.log");
        var settings = SettingsStore.Load(paths);
        host.Text = settings.Host; deploy.IsChecked = settings.SendPayload;
        loaderPort.Value = Math.Clamp(settings.LoaderPort, 1, 65535);
        ftpPort.Value = Math.Clamp(settings.AlternativeFTPPort ?? 0, 0, 65535);
        online.IsChecked = settings.OnlineGameTitles; includeTitle.IsChecked = settings.IncludeGameTitle; destination.Text = settings.Destination;
        var browse = MakeButton("Browse…"); var reset = MakeButton("Parent Folder");
        ToolTip.SetTip(reset, "Reset to PS5-Backups beside the AppImage.");
        var all = MakeButton("Select all"); var none = MakeButton("Unselect all");
        var expand = MakeButton("Expand all"); var collapse = MakeButton("Collapse all");
        var open = MakeButton("Open backups"); var logs = MakeButton("Open logs");
        var connection = Row(Label("PS5 IP Adress"), host, deploy, Label("ELF loader port"), loaderPort, connect);
        var ports = Row(Label("FTP override (0 = embedded)"), ftpPort);
        var sources = Row(online, SourceLink("kytyPS5", GameTitleCatalog.SourceUrl), Label("|"),
            SourceLink("Homebrew-Catalog", GameTitleCatalog.HomebrewSourceUrl), Label("|"), SourceLink("ShadPS4", Ps4TitleLookup.SourceUrl));
        ToolTip.SetTip(online, "KytyPS5 / Homebrew Catalog and missing CUSA IDs via shadPS4. No save files or user names are uploaded.");
        var userRow = Row(Label("Console user"), users, load);
        var folder = new Grid { ColumnDefinitions = new ColumnDefinitions("Auto,*,Auto,Auto") };
        folder.Children.Add(Label("Backup folder  ")); Grid.SetColumn(destination, 1); folder.Children.Add(destination);
        Grid.SetColumn(browse, 2); folder.Children.Add(browse); Grid.SetColumn(reset, 3); folder.Children.Add(reset); folder.Margin = new Thickness(0, 0, 0, 10);
        var options = Row(includeTitle);
        var actions = Row(all, none, expand, collapse, dump, cancel);
        var header = new StackPanel { Spacing = 3, Children = { new TextBlock { Text = "PS5 SAVEFILE BATCHER", FontSize = 24, Margin = new Thickness(0, 0, 0, 12) }, connection, ports, sources, userRow, folder, options, actions, selection } };
        Cell(tableHeader, Label("Choose"), 0);
        string[] columns = { "Game / save", "Title ID", "Platform", "Save count / selected" };
        foreach (var column in Enum.GetValues<GameSortColumn>())
        {
            var button = MakeButton(columns[(int)column]); button.Tag = column;
            button.HorizontalAlignment = HorizontalAlignment.Stretch; button.Padding = new Thickness(0, 6);
            button.HorizontalContentAlignment = HorizontalAlignment.Left;
            button.Click += (_, _) =>
            {
                if (busy) return;
                descending.IsChecked = sort.SelectedIndex == (int)column && descending.IsChecked != true;
                sort.SelectedIndex = (int)column; Render();
            };
            columnButtons[column] = button; Cell(tableHeader, button, (int)column + 1);
        }
        var gameScroll = new ScrollViewer { Content = games, HorizontalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Disabled };
        var table = new Grid { RowDefinitions = new RowDefinitions("Auto,*"), Margin = new Thickness(0, 6, 0, 10) };
        table.Children.Add(tableHeader); Grid.SetRow(gameScroll, 1); table.Children.Add(gameScroll);
        var footer = new StackPanel { Spacing = 6, Children = { progress, Row(timing, open, logs), status } };
        var layout = new Grid { Margin = new Thickness(18), RowDefinitions = new RowDefinitions("Auto,*,Auto,150") };
        layout.Children.Add(header); Grid.SetRow(table, 1); layout.Children.Add(table); Grid.SetRow(footer, 2); layout.Children.Add(footer);
        Grid.SetRow(logBox, 3); layout.Children.Add(logBox); Content = layout;
        locked.AddRange(new Control[] { connection, ports, sources, userRow, folder, options, all, none, expand, collapse, dump, table });
        connect.Click += async (_, _) => await Operation(ConnectOrDisconnect);
        load.Click += async (_, _) => await Operation(LoadLibrary);
        dump.Click += async (_, _) => await Operation(DumpSelected);
        cancel.Click += async (_, _) => await RequestCancellation();
        browse.Click += async (_, _) =>
        {
            try
            {
                var result = await StorageProvider.OpenFolderPickerAsync(new() { Title = "Choose backup destination", AllowMultiple = false });
                if (result.Count > 0 && result[0].Path.IsFile) { destination.Text = result[0].Path.LocalPath; SaveSettings(); }
            }
            catch (Exception ex) { Log("Folder picker unavailable; type the destination path instead. " + ex.Message); }
        };
        reset.Click += (_, _) => { destination.Text = paths.Backups; SaveSettings(); };
        all.Click += (_, _) => { foreach (var g in library) g.SelectAll(true); Render(); };
        none.Click += (_, _) => { foreach (var g in library) g.SelectAll(false); Render(); };
        expand.Click += (_, _) => { foreach (var g in library) g.Expanded = true; Render(); };
        collapse.Click += (_, _) => { foreach (var g in library) g.Expanded = false; Render(); };
        users.SelectionChanged += (_, _) => { library.Clear(); Render(); UpdateControls(); };
        sort.SelectionChanged += (_, _) => Render(); descending.IsCheckedChanged += (_, _) => Render();
        deploy.IsCheckedChanged += (_, _) => UpdateControls();
        online.IsCheckedChanged += async (_, _) =>
        {
            if (busy || library.Count == 0) return;
            await Operation(async () => { if (online.IsChecked == true) await LookupTitles(library); ApplyTitles(); SaveSettings(); });
        };
        open.Click += (_, _) => OpenFolder(lastRun ?? paths.ResolveDestination(destination.Text));
        logs.Click += (_, _) => OpenFolder(paths.Logs);
        clock.Tick += (_, _) => UpdateTime();
        Closing += OnClosing;
        Closed += (_, _) => clock.Stop();
        Render(); UpdateControls(); UpdateTime();
        Log("Ready. Start elfldr on the PS5, then connect. No separate FTP payload is needed. No connection is made until Connect is clicked.");
        Log("Settings: " + paths.SettingsFile + " • Logs: " + paths.Logs);
    }

    private DumperSettings ReadSettings() => new()
    {
        Host = host.Text?.Trim() ?? "", SendPayload = deploy.IsChecked == true, LoaderPort = (int)(loaderPort.Value ?? 9021),
        MounterPort = MounterClient.DefaultPort, AlternativeFTPPort = (int)(ftpPort.Value ?? 0),
        IncludeGameTitle = includeTitle.IsChecked == true, OnlineGameTitles = online.IsChecked == true,
        Destination = paths.ResolveDestination(destination.Text)
    };
    private void SaveSettings() { try { SettingsStore.Save(paths, ReadSettings()); } catch (Exception ex) { Log("Could not save settings: " + ex.Message); } }
    private SaveFtp Ftp() => new(connectedHost, ReadSettings().TransferPort(client.EmbeddedFtpPort));
    private UserChoice SelectedUser() => users.SelectedItem as UserChoice ?? throw new InvalidOperationException("Select a console user first.");
    private async Task ConnectOrDisconnect()
    {
        if (client != null) { await Disconnect(); return; }
        var s = ReadSettings(); SaveSettings();
        client = await Task.Run(() => DumperConnection.Connect(s, s.SendPayload, Log)); connectedHost = s.Host;
        cancellation.Token.ThrowIfCancellationRequested();
        var ftp = Ftp(); await Task.Run(() => TransferReadiness.Test(ftp, client));
        var found = await Task.Run(client.GetUsers);
        cancellation.Token.ThrowIfCancellationRequested();
        users.ItemsSource = found.Select(u => new UserChoice(u.id, u.name)).ToArray();
        users.SelectedIndex = found.Length > 0 ? 0 : -1;
        Log($"Connected; binary FTP verified. {found.Length} users found. Choose a user and load the library.");
    }
    private async Task Disconnect()
    {
        var c = client;
        if (c != null) await Task.Run(c.Disconnect);
        client = null; connectedHost = null; users.ItemsSource = null; library.Clear(); Render();
        Log("Disconnected; payload stopped.");
    }
    private async Task LoadLibrary()
    {
        var user = SelectedUser(); var token = cancellation.Token;
        var ids = await Task.Run(() => client.ListSaves(user.Id));
        var names = new Dictionary<string, string>();
        try { names = await Task.Run(client.GetGameTitles); }
        catch (Exception ex) { Log("Console game titles unavailable: " + ex.Message); }
        var groups = new List<GameGroup>();
        foreach (var id in ids.Distinct())
        {
            token.ThrowIfCancellationRequested(); Log("Loading " + id);
            var found = (await Task.Run(() => client.Search(user.Id, id))).Where(s => !string.IsNullOrWhiteSpace(s.DirName)).ToArray();
            if (found.Length == 0) continue;
            var title = GameTitleCatalog.Resolve(id, new Dictionary<string, string>(), names, found);
            groups.Add(new() { Id = id, LocalTitle = title, Saves = found.Select(s => new SaveJob(user.Id, id, s.DirName, title, s.Subtitle)).ToArray() });
        }
        if (online.IsChecked == true) await LookupTitles(groups);
        token.ThrowIfCancellationRequested(); library.Clear(); library.AddRange(groups); ApplyTitles();
        Log($"Loaded {library.Count} games and {library.Sum(g => g.Saves.Length)} saves.");
    }
    private async Task LookupTitles(IEnumerable<GameGroup> groups)
    {
        var token = cancellation.Token; var source = new GameTitleCatalog();
        catalog = await source.Load(Path.Combine(paths.Cache, "game-titles.json"), true, Log, token);
        foreach (var item in await source.LoadHomebrew(Path.Combine(paths.Cache, "homebrew-titles.json"), true, Log, token)) catalog.TryAdd(item.Key, item.Value);
        foreach (var item in await new Ps4TitleLookup().Load(groups.Select(g => g.Id).Where(id => !catalog.ContainsKey(id)), Path.Combine(paths.Cache, "ps4-titles.json"), Log, token)) catalog.TryAdd(item.Key, item.Value);
    }
    private void ApplyTitles()
    {
        var titles = online.IsChecked == true ? catalog : new Dictionary<string, string>();
        foreach (var g in library) g.ApplyTitle(titles); Render();
    }
    private void Render()
    {
        if (games == null) return;
        GameSorting.Sort(library, (GameSortColumn)Math.Max(sort.SelectedIndex, 0), descending.IsChecked == true);
        games.Children.Clear();
        string[] columns = { "Game / save", "Title ID", "Platform", "Save count / selected" };
        foreach (var pair in columnButtons)
            pair.Value.Content = columns[(int)pair.Key] + ((int)pair.Key == sort.SelectedIndex ? descending.IsChecked == true ? " ▼" : " ▲" : "");
        foreach (var g in library)
        {
            var row = TableRow(); row.Tag = g;
            var parent = new GameCheckBox { IsThreeState = true, IsChecked = g.State == CheckState.Checked ? true : g.State == CheckState.Unchecked ? false : null };
            ToolTip.SetTip(parent, "Select all saves for this game");
            parent.IsCheckedChanged += (_, _) => { g.SelectAll(parent.IsChecked != false); Render(); };
            Cell(row, parent, 0);
            var name = MakeButton((g.Expanded ? "▾  " : "▸  ") + (string.IsNullOrEmpty(g.Title) ? "Unknown title" : g.Title));
            name.HorizontalAlignment = HorizontalAlignment.Stretch; name.HorizontalContentAlignment = HorizontalAlignment.Left;
            name.Padding = new Thickness(2); name.Background = Avalonia.Media.Brushes.Transparent;
            ToolTip.SetTip(name, string.IsNullOrEmpty(g.Title) ? g.Id : g.Title);
            name.Click += (_, _) => { g.Expanded = !g.Expanded; Render(); };
            Cell(row, name, 1); Cell(row, Label(g.Id), 2); Cell(row, Label(g.Platform), 3);
            Cell(row, Label($"{g.Saves.Length} / {g.Selected.Count} selected"), 4);
            games.Children.Add(row);
            if (!g.Expanded) continue;
            foreach (var s in g.Saves)
            {
                var child = TableRow(); child.Tag = s;
                var box = new CheckBox { IsChecked = g.Selected.Contains(s.Directory) };
                box.IsCheckedChanged += (_, _) => { if (g.Selected.Contains(s.Directory) != (box.IsChecked == true)) g.Toggle(s.Directory); Render(); };
                Cell(child, box, 0);
                var text = Label("    " + s.Directory + (string.IsNullOrEmpty(s.Subtitle) ? "" : " — " + s.Subtitle));
                text.TextTrimming = Avalonia.Media.TextTrimming.CharacterEllipsis; ToolTip.SetTip(text, text.Text);
                Cell(child, text, 1); Cell(child, Label(g.Id), 2); Cell(child, Label(g.Platform), 3);
                games.Children.Add(child);
            }
        }
        selection.Text = library.Count == 0 ? "Load the library to choose games and individual saves." : $"{library.Count} games · {library.Sum(g => g.Saves.Length)} saves · {library.Sum(g => g.Selected.Count)} selected";
        UpdateControls();
    }
    private async Task DumpSelected()
    {
        var user = SelectedUser();
        var jobs = library.SelectMany(g => g.Saves.Where(s => g.Selected.Contains(s.Directory))).ToArray();
        if (jobs.Length == 0) throw new InvalidOperationException("Select at least one game or save first.");
        var settings = ReadSettings(); SaveSettings(); var ftp = Ftp(); await Task.Run(() => TransferReadiness.Test(ftp, client));
        var token = cancellation.Token; token.ThrowIfCancellationRequested();
        lastRun = BackupNames.Run(settings.Destination, user.Name, user.Id, DateTime.Now);
        completed = 0; total = jobs.Length; progress.Maximum = total; progress.Value = 0;
        batchTiming = new() { Status = "Running" }; elapsed.Restart(); clock.Start(); UpdateTime();
        Log($"Starting {total} saves → {lastRun}");
        try
        {
            var receipt = await Task.Run(() => new DumpRunner(new PayloadMount(client), ftp).Run(jobs, lastRun, Log,
                (n, _) => Dispatcher.UIThread.Post(() => { completed = n; progress.Value = n; UpdateTime(); }), token,
                new DumpOptions { IncludeGameTitle = settings.IncludeGameTitle, UserInRunName = true, ConsoleUserName = user.Name }));
            completed = receipt.Saves.Count(s => s.Status == "Complete"); batchTiming.Status = receipt.Status;
            Log($"{receipt.Status}: {completed}/{total} saves. Report: {Path.Combine(lastRun, "report.json")}");
            if (receipt.Status != "Complete") { client?.Dispose(); client = null; users.ItemsSource = null; library.Clear(); Render(); Log("Connection closed after interruption. Completed saves and report are preserved. Reconnect before retrying."); }
        }
        finally
        {
            if (batchTiming.Status is "Running" or "Cancelling") batchTiming.Status = token.IsCancellationRequested ? "Cancelled" : "Failed";
            elapsed.Stop(); clock.Stop(); progress.Value = completed; UpdateTime();
        }
    }
    private async Task Operation(Func<Task> action)
    {
        if (busy) return;
        busy = true; cancellation = new(); UpdateControls();
        try { await action(); }
        catch (Exception ex)
        {
            Log((cancellation.IsCancellationRequested ? "Cancelled: " : "Error: ") + ex.Message);
            // No operation is still running here: dispose only after batch cleanup has returned.
            client?.Dispose(); client = null; users.ItemsSource = null; library.Clear(); Render();
        }
        finally
        {
            busy = false; cancellation.Dispose(); cancellation = null; UpdateControls();
            if (closeRequested) Close();
        }
    }
    private async Task RequestCancellation()
    {
        var source = cancellation; if (source == null) return;
        cancel.IsEnabled = false;
        if (elapsed.IsRunning) batchTiming.Status = "Cancelling";
        Log("Cancellation requested. Waiting for the current request and unmount cleanup; do not stop the payload. Console requests can take up to 120 seconds.");
        await Task.Run(() => { try { source.Cancel(); } catch (ObjectDisposedException) { } });
    }
    private async void OnClosing(object sender, WindowClosingEventArgs e)
    {
        if (closing) return;
        if (busy) { e.Cancel = true; closeRequested = true; await RequestCancellation(); return; }
        SaveSettings();
        if (client != null)
        {
            e.Cancel = true; closing = true; IsEnabled = false;
            await Disconnect(); Close();
        }
    }
    private void UpdateControls()
    {
        foreach (var c in locked) c.IsEnabled = !busy;
        host.IsEnabled = deploy.IsEnabled = ftpPort.IsEnabled = !busy && client == null;
        loaderPort.IsEnabled = !busy && client == null && deploy.IsChecked == true;
        connect.Content = client == null ? "Connect" : "Disconnect";
        load.IsEnabled = users.IsEnabled = !busy && client != null;
        dump.IsEnabled = !busy && client != null && library.Any(g => g.Selected.Count > 0);
        cancel.IsEnabled = busy && cancellation != null && !cancellation.IsCancellationRequested;
    }
    private void UpdateTime()
    {
        timing.Text = batchTiming.Display(elapsed.Elapsed, completed, total, elapsed.IsRunning);
        progress.Foreground = batchTiming.IsSuccessful(completed, total) ? Avalonia.Media.Brushes.ForestGreen : Avalonia.Media.Brushes.DodgerBlue;
    }
    private void Log(string text)
    {
        if (!Dispatcher.UIThread.CheckAccess()) { Dispatcher.UIThread.Post(() => Log(text)); return; }
        status.Text = text;
        string line = $"{DateTime.Now:O} {text}{Environment.NewLine}";
        logBox.Text = (logBox.Text + line) is var full && full.Length > 100000 ? full[^80000..] : full;
        logBox.CaretIndex = logBox.Text.Length;
        try { File.AppendAllText(sessionLog, line); if (lastRun != null && Directory.Exists(lastRun)) File.AppendAllText(Path.Combine(lastRun, "activity.log"), line); }
        catch { /* A full disk must not interrupt unmount cleanup. */ }
    }
    private void OpenFolder(string path)
    {
        try { Directory.CreateDirectory(path); Process.Start(new ProcessStartInfo("xdg-open") { ArgumentList = { path }, UseShellExecute = false }); }
        catch (Exception ex) { Log("Could not open folder: " + ex.Message); }
    }
    public void VerifySmokeSelection()
    {
        if (client != null || busy) throw new Exception("Smoke must remain offline");
        if (host.PlaceholderText != "PS5 IP Adress" ||
            !this.GetVisualDescendants().OfType<TextBlock>().Any(t => t.Text == "PS5 IP Adress"))
            throw new Exception("Missing exact PS5 IP Adress label or placeholder");
        var visibleButtons = this.GetVisualDescendants().OfType<Button>().ToArray();
        foreach (var column in Enum.GetValues<GameSortColumn>())
            if (!visibleButtons.Any(b => Equals(b.Tag, column))) throw new Exception("Missing clickable table column: " + column);
        var resetButton = visibleButtons.SingleOrDefault(b => Equals(b.Content, "Parent Folder"));
        if (resetButton == null) throw new Exception("Missing Parent Folder button");
        destination.Text = Path.Combine(paths.Backups, "offline-custom");
        resetButton.RaiseEvent(new Avalonia.Interactivity.RoutedEventArgs(Button.ClickEvent));
        if (destination.Text != paths.Backups) throw new Exception("Parent Folder did not restore app-adjacent PS5-Backups");
        if (!Equals(online.Content, "online title lookup from:")) throw new Exception("Missing inline online title source label");
        if (this.GetVisualDescendants().OfType<TextBlock>().Any(t => t.Text == "Mounter port") ||
            this.GetVisualDescendants().OfType<NumericUpDown>().Count() != 2 || ReadSettings().MounterPort != MounterClient.DefaultPort)
            throw new Exception("Bundled mounter port must be fixed and have no GUI selector");
        foreach (string source in new[] { "kytyPS5", "Homebrew-Catalog", "ShadPS4" })
            if (!visibleButtons.Any(b => b.Content?.ToString()?.Contains(source) == true)) throw new Exception("Missing visible title source: " + source);
        var g = new GameGroup { Id = "PPSA00001", Saves = new[] { new SaveJob(1, "PPSA00001", "A"), new SaveJob(1, "PPSA00001", "B") }, Expanded = true };
        library.Add(g); Render();
        CheckBox Parent() => ((Grid)games.Children[0]).Children.OfType<CheckBox>().Single();
        Parent().ApplyTemplate();
        if (Parent().Template == null) throw new Exception("Group checkbox has no visible toolkit template");
        void ToggleParent() => ((GameCheckBox)Parent()).SmokeClick();
        ToggleParent();
        if (g.Selected.Count != 2 || Parent().IsChecked != true) throw new Exception("GUI group selection failed");
        ToggleParent();
        if (g.Selected.Count != 0 || Parent().IsChecked != false) throw new Exception("GUI checked group click did not deselect");
        ToggleParent();
        var child = ((Grid)games.Children[1]).Children.OfType<CheckBox>().Single(); child.IsChecked = false;
        if (g.Selected.Count != 1 || g.State != CheckState.Indeterminate || Parent().IsChecked != null || !g.Expanded) throw new Exception("GUI individual selection failed");
        ToggleParent();
        if (g.Selected.Count != 2 || Parent().IsChecked != true) throw new Exception("GUI mixed group click did not select all");
        var second = new GameGroup { Id = "CUSA00002", LocalTitle = "Alpha", Saves = new[] { new SaveJob(1, "CUSA00002", "ONE") } };
        var third = new GameGroup { Id = "PPSA00003", LocalTitle = "Zulu", Saves = new[] { new SaveJob(1, "PPSA00003", "ONE"), new SaveJob(1, "PPSA00003", "TWO"), new SaveJob(1, "PPSA00003", "THREE") } };
        library.Add(second); library.Add(third); ApplyTitles();
        void ClickColumn(GameSortColumn column) => columnButtons[column].RaiseEvent(new Avalonia.Interactivity.RoutedEventArgs(Button.ClickEvent));
        foreach (var column in Enum.GetValues<GameSortColumn>())
        {
            for (int direction = 0; direction < 2; direction++)
            {
                bool previous = descending.IsChecked == true; ClickColumn(column);
                bool reverse = descending.IsChecked == true;
                string[] expected = column switch
                {
                    GameSortColumn.Title => reverse ? new[] { third.Id, second.Id, g.Id } : new[] { second.Id, third.Id, g.Id },
                    GameSortColumn.Id or GameSortColumn.SaveCount => reverse ? new[] { third.Id, g.Id, second.Id } : new[] { second.Id, g.Id, third.Id },
                    GameSortColumn.Platform => reverse ? new[] { g.Id, third.Id, second.Id } : new[] { second.Id, g.Id, third.Id },
                    _ => throw new Exception("Unexpected sort column")
                };
                if (sort.SelectedIndex != (int)column || !games.Children.OfType<Grid>().Where(r => r.Tag is GameGroup).Select(r => ((GameGroup)r.Tag).Id).SequenceEqual(expected)) throw new Exception("GUI table order: " + column);
                if (direction == 1 && reverse == previous) throw new Exception("Header did not reverse sort: " + column);
                if (!g.Expanded || g.Selected.Count != 2) throw new Exception("Sorting lost selection/expansion");
            }
        }
        foreach (string state in new[] { "Complete", "Cancelled", "Failed", "Running", "Ready" })
        {
            batchTiming.Status = state; completed = total = 3; UpdateTime();
            if (Equals(progress.Foreground, Avalonia.Media.Brushes.ForestGreen) != (state == "Complete")) throw new Exception("GUI completion color: " + state);
        }
        if (connect.IsEnabled == false || client != null) throw new Exception("Offline smoke unexpectedly connected");
        if (Environment.GetEnvironmentVariable("PS5_SMOKE_SCREENSHOT") != null)
        {
            batchTiming.Status = "Complete"; completed = total = 3; progress.Maximum = 3; progress.Value = 3;
            UpdateTime(); Log("OFFLINE UI TEST DATA — no console connection, no backup performed. Green completion state is simulated for visual verification only.");
        }
        else { batchTiming = new(); completed = total = 0; UpdateTime(); library.Clear(); Render(); }
    }
}
