using System.Collections;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using SpineViewerWPF.Application;

namespace SpineViewerWPF.Wpf;

// TASK-068: folder browser for the left panel. Lists skeleton files, filters by
// name, and fills in Runtime and animation details in the background, one file
// at a time. Adding to the scene goes through the Shell's Add layer path.
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
        status = folder is null ? "Choose a folder to list Spine skeletons." : "";
        ChooseFolderCommand = new RelayCommand(ChooseFolder);
        RefreshCommand = new RelayCommand(() => _ = ScanAsync(), () => folder is not null && !isScanning);
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
    public string FolderLabel => folder ?? "No folder";
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
        ? $"{Items.Count} skeleton{(Items.Count == 1 ? "" : "s")}"
        : $"{FilteredItems.Count} of {Items.Count}";

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
        var root = folder;
        var recursive = includeSubfolders;
        SetScanning(true);
        Status = "Scanning…";
        try
        {
            var scan = await Task.Run(() => AssetBrowser.Scan(root, recursive, AssetBrowser.DefaultLimit, cancellation.Token), cancellation.Token);
            if (cancellation.IsCancellationRequested) return;
            Items = scan.Files.Select(path => new AssetBrowserItem(path, scan.Folder)).ToList();
            filteredItems = null;
            Changed(nameof(Items));
            Changed(nameof(FilteredItems));
            Changed(nameof(CountLabel));
            Status = scan.Truncated ? $"Showing the first {AssetBrowser.DefaultLimit} skeletons" : "";
            SetScanning(false);
            DetailsTask = LoadDetailsAsync(Items, cancellation.Token);
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

    // Background Runtime/animation details for the current list (tests await it).
    public Task DetailsTask { get; private set; } = Task.CompletedTask;

    public void Dispose() => scanCancellation?.Cancel();

    private async Task LoadDetailsAsync(IReadOnlyList<AssetBrowserItem> items, CancellationToken cancellationToken)
    {
        if (assetService is null) return;
        foreach (var item in items)
        {
            if (cancellationToken.IsCancellationRequested) return;
            try
            {
                var inspection = await Task.Run(() => assetService.Inspect(item.Path, null, null, cancellationToken), cancellationToken);
                var animations = inspection.Animations.Count;
                item.SetDetail(
                    $"{inspection.Runtime.DetectedExportVersion ?? inspection.Runtime.SelectedLine} · " +
                    (animations == 0 ? "setup pose" : $"{animations} animation{(animations == 1 ? "" : "s")}"),
                    !inspection.Success);
            }
            catch (OperationCanceledException)
            {
                return;
            }
            catch (Exception exception)
            {
                item.SetDetail(exception.Message, true);
            }
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
    private string detail = "…";
    private bool hasProblem;

    internal AssetBrowserItem(string path, string root)
    {
        Path = path;
        Name = System.IO.Path.GetFileName(path);
        var relative = System.IO.Path.GetRelativePath(root, System.IO.Path.GetDirectoryName(path) ?? root);
        Location = relative == "." ? "" : relative;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public string Path { get; }
    public string Name { get; }
    public string Location { get; }
    public string Detail => detail;
    public bool HasProblem => hasProblem;

    internal void SetDetail(string value, bool problem)
    {
        detail = value;
        hasProblem = problem;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Detail)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(HasProblem)));
    }

    public override string ToString() => Name;
}
