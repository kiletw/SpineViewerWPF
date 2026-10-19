using System.Collections;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using SpineViewerWPF.Application;

namespace SpineViewerWPF.Wpf;

// TASK-068: folder browser for the left panel. Lists skeleton files, filters by
// name, and fills in Runtime and animation details in the background. Details
// load only for rows the list actually shows (the first read of an item's
// Detail queues it), one file at a time, and are cached by path and last write
// time so rescans and the subfolder toggle reuse them; Refresh clears the cache.
// Adding to the scene goes through the Shell's Add layer path.
public sealed class AssetBrowserViewModel : INotifyPropertyChanged, IDisposable
{
    private readonly AssetService? assetService;
    private readonly UserSettingsStore settings;
    private readonly Func<string?> chooseFolder;
    private readonly Func<IReadOnlyList<string>, Task> addToScene;
    private readonly Func<string, Task> openAsset;
    private CancellationTokenSource? scanCancellation;
    private string? folder;
    private bool includeSubfolders;
    private string filter = "";
    private string status = "";
    private bool isScanning;
    private bool loaded;
    private IReadOnlyList<AssetBrowserItem>? filteredItems;
    private readonly Dictionary<string, CachedDetail> detailCache = new(StringComparer.OrdinalIgnoreCase);
    private readonly Queue<AssetBrowserItem> pendingDetails = new();
    private readonly object detailGate = new();
    private int scanGeneration;
    private bool detailsRunning;

    internal AssetBrowserViewModel(
        AssetService? assetService,
        UserSettingsStore settings,
        Func<string?> chooseFolder,
        Func<IReadOnlyList<string>, Task> addToScene,
        Func<string, Task> openAsset)
    {
        this.assetService = assetService;
        this.settings = settings;
        this.chooseFolder = chooseFolder;
        this.addToScene = addToScene;
        this.openAsset = openAsset;
        folder = settings.BrowserFolder;
        includeSubfolders = settings.BrowserIncludeSubfolders;
        status = folder is null ? Localized.Text("Status.ChooseAFolderToListSpine", "Choose a folder to list Spine skeletons.") : "";
        ChooseFolderCommand = new RelayCommand(ChooseFolder);
        RefreshCommand = new RelayCommand(Refresh, () => folder is not null && !isScanning);
        AddToSceneCommand = new ParameterCommand(parameter => _ = AddToSceneAsync(Paths(parameter)), parameter => Paths(parameter).Count > 0);
        OpenCommand = new ParameterCommand(parameter => _ = OpenAsync(Paths(parameter)), parameter => Paths(parameter).Count == 1);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public ICommand ChooseFolderCommand { get; }
    public ICommand RefreshCommand { get; }
    public ICommand AddToSceneCommand { get; }
    public ICommand OpenCommand { get; }
    public List<AssetBrowserItem> Items { get; private set; } = [];

    public string? Folder => folder;
    public string FolderLabel => folder ?? Localized.Text("Browser.NoFolder", "No folder");
    public bool IsScanning => isScanning;
    public string Status
    {
        get => status;
        private set
        {
            if (status == value) return;
            status = value;
            Changed();
        }
    }

    public bool IncludeSubfolders
    {
        get => includeSubfolders;
        set
        {
            if (includeSubfolders == value) return;
            includeSubfolders = value;
            settings.SetBrowser(folder, includeSubfolders);
            Changed();
            if (folder is not null) _ = ScanAsync();
        }
    }

    public string Filter
    {
        get => filter;
        set
        {
            var next = value ?? "";
            if (filter == next) return;
            filter = next;
            filteredItems = null;
            Changed();
            Changed(nameof(FilteredItems));
            Changed(nameof(CountLabel));
        }
    }

    // Cached like the Slots list (TASK-061) so detail updates do not rebuild rows.
    public IReadOnlyList<AssetBrowserItem> FilteredItems => filteredItems ??= Items
        .Where(item => item.Name.Contains(filter, StringComparison.OrdinalIgnoreCase))
        .ToArray();

    public string CountLabel => FilteredItems.Count == Items.Count
        ? Items.Count == 1
            ? Localized.Format("Browser.Skeleton", "{0} skeleton", Items.Count)
            : Localized.Format("Browser.Skeletons", "{0} skeletons", Items.Count)
        : Localized.Format("Browser.FilteredCount", "{0} of {1}", FilteredItems.Count, Items.Count);

    // Scans the remembered folder the first time the browser is shown.
    public void EnsureLoaded()
    {
        if (loaded) return;
        loaded = true;
        if (folder is not null && Directory.Exists(folder)) _ = ScanAsync();
    }

    public async Task SetFolderAsync(string path)
    {
        folder = Path.GetFullPath(path);
        loaded = true;
        settings.SetBrowser(folder, includeSubfolders);
        Changed(nameof(Folder));
        Changed(nameof(FolderLabel));
        await ScanAsync();
    }

    public async Task ScanAsync()
    {
        if (folder is null) return;
        scanCancellation?.Cancel();
        var cancellation = new CancellationTokenSource();
        scanCancellation = cancellation;
        int generation;
        lock (detailGate)
        {
            generation = ++scanGeneration;
            pendingDetails.Clear();
        }
        var root = folder;
        var recursive = includeSubfolders;
        SetScanning(true);
        Status = Localized.Text("Status.Scanning", "Scanning…");
        try
        {
            // Last write times are read with the scan so cached details can be
            // matched without touching each file again on the UI thread.
            var (scan, stamps) = await Task.Run(() =>
            {
                var result = AssetBrowser.Scan(root, recursive, AssetBrowser.DefaultLimit, cancellation.Token);
                return (result, result.Files.Select(LastWrite).ToArray());
            }, cancellation.Token);
            if (cancellation.IsCancellationRequested) return;
            Items = scan.Files.Select((path, index) => CreateItem(path, scan.Folder, stamps[index], generation)).ToList();
            filteredItems = null;
            Changed(nameof(Items));
            Changed(nameof(FilteredItems));
            Changed(nameof(CountLabel));
            Status = scan.Truncated ? Localized.Format("Status.ShowingTheFirstSkeletons", "Showing the first {0} skeletons", AssetBrowser.DefaultLimit) : "";
            SetScanning(false);
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or ArgumentException)
        {
            if (cancellation.IsCancellationRequested) return;
            Items = [];
            filteredItems = null;
            Changed(nameof(Items));
            Changed(nameof(FilteredItems));
            Changed(nameof(CountLabel));
            Status = exception.Message;
        }
        finally
        {
            if (ReferenceEquals(scanCancellation, cancellation)) SetScanning(false);
        }
    }

    // Background Runtime/animation details for requested rows (tests await it).
    public Task DetailsTask { get; private set; } = Task.CompletedTask;

    public void Dispose() => scanCancellation?.Cancel();

    private void Refresh()
    {
        lock (detailGate) detailCache.Clear();
        _ = ScanAsync();
    }

    private static DateTime LastWrite(string path)
    {
        try
        {
            return File.GetLastWriteTimeUtc(path);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            return DateTime.MinValue;
        }
    }

    private AssetBrowserItem CreateItem(string path, string root, DateTime lastWrite, int generation)
    {
        var item = new AssetBrowserItem(path, root, lastWrite, RequestDetail, generation);
        lock (detailGate)
        {
            if (detailCache.TryGetValue(path, out var cached) && cached.LastWrite == lastWrite)
                item.SetDetail(cached.Detail, cached.HasProblem);
        }
        return item;
    }

    // Called from AssetBrowserItem.Detail the first time a row reads it.
    private void RequestDetail(AssetBrowserItem item)
    {
        if (assetService is null) return;
        lock (detailGate)
        {
            if (item.Generation != scanGeneration) return;
            pendingDetails.Enqueue(item);
            if (detailsRunning) return;
            detailsRunning = true;
        }
        DetailsTask = LoadDetailsAsync(item.Generation, scanCancellation?.Token ?? CancellationToken.None);
    }

    private async Task LoadDetailsAsync(int generation, CancellationToken cancellationToken)
    {
        // Leave the property getter that requested the first item before working.
        await Task.Yield();
        while (true)
        {
            AssetBrowserItem item;
            lock (detailGate)
            {
                if (generation != scanGeneration || cancellationToken.IsCancellationRequested || pendingDetails.Count == 0)
                {
                    detailsRunning = false;
                    return;
                }
                item = pendingDetails.Dequeue();
            }
            string detail;
            bool problem;
            try
            {
                var inspection = await Task.Run(() => assetService!.Inspect(item.Path, null, null, cancellationToken), cancellationToken);
                var animations = inspection.Animations.Count;
                // Formatted after the await so localized text is read on the UI thread.
                detail = $"{inspection.Runtime.DetectedExportVersion ?? inspection.Runtime.SelectedLine} · " +
                    (animations == 0
                        ? Localized.Text("Status.SetupPose", "setup pose")
                        : animations == 1
                            ? Localized.Format("Browser.Animation", "{0} animation", animations)
                            : Localized.Format("Browser.Animations", "{0} animations", animations));
                problem = !inspection.Success;
            }
            catch (OperationCanceledException)
            {
                lock (detailGate) detailsRunning = false;
                return;
            }
            catch (Exception exception)
            {
                detail = exception.Message;
                problem = true;
            }
            lock (detailGate) detailCache[item.Path] = new CachedDetail(item.LastWrite, detail, problem);
            item.SetDetail(detail, problem);
        }
    }

    private void ChooseFolder()
    {
        if (chooseFolder() is { } chosen) _ = SetFolderAsync(chosen);
    }

    private Task AddToSceneAsync(IReadOnlyList<string> paths) => paths.Count == 0 ? Task.CompletedTask : addToScene(paths);

    private Task OpenAsync(IReadOnlyList<string> paths) => paths.Count == 1 ? openAsset(paths[0]) : Task.CompletedTask;

    // Accepts a single item or a ListBox SelectedItems collection.
    private static IReadOnlyList<string> Paths(object? parameter) => parameter switch
    {
        AssetBrowserItem item => [item.Path],
        IEnumerable items => items.OfType<AssetBrowserItem>().Select(item => item.Path).ToArray(),
        _ => []
    };

    private void SetScanning(bool value)
    {
        if (isScanning == value) return;
        isScanning = value;
        Changed(nameof(IsScanning));
        CommandManager.InvalidateRequerySuggested();
        ((RelayCommand)RefreshCommand).Refresh();
    }

    private void Changed([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}

public sealed class AssetBrowserItem : INotifyPropertyChanged
{
    private readonly Action<AssetBrowserItem>? requestDetail;
    private string detail = "…";
    private bool hasProblem;
    private bool hasDetail;
    private bool requested;

    internal AssetBrowserItem(string path, string root, DateTime lastWrite, Action<AssetBrowserItem>? requestDetail, int generation)
    {
        this.requestDetail = requestDetail;
        LastWrite = lastWrite;
        Generation = generation;
        Path = path;
        Name = System.IO.Path.GetFileName(path);
        var relative = System.IO.Path.GetRelativePath(root, System.IO.Path.GetDirectoryName(path) ?? root);
        Location = relative == "." ? "" : relative;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public string Path { get; }
    public string Name { get; }
    public string Location { get; }
    internal DateTime LastWrite { get; }
    internal int Generation { get; }

    // The first read (a row being shown) queues the background inspection.
    public string Detail
    {
        get
        {
            if (!hasDetail && !requested)
            {
                requested = true;
                requestDetail?.Invoke(this);
            }
            return detail;
        }
    }

    public bool HasProblem => hasProblem;

    // True once details are known; reading it does not request them.
    public bool HasDetail => hasDetail;

    internal void SetDetail(string value, bool problem)
    {
        detail = value;
        hasProblem = problem;
        hasDetail = true;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Detail)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(HasProblem)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(HasDetail)));
    }

    public override string ToString() => Name;
}

internal sealed record CachedDetail(DateTime LastWrite, string Detail, bool HasProblem);
