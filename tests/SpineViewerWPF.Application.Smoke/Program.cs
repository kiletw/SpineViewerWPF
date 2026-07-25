using SpineRuntime.V41;
using SpineViewerWPF.Application;
using SpineViewerWPF.Core;
using SpineViewerWPF.Wpf;

var root = Path.Combine(Path.GetTempPath(), $"SpineViewerWPF-{Guid.NewGuid():N}");
Directory.CreateDirectory(root);

try
{
    var skeleton = Path.Combine(root, "hero.json");
    var project = Path.Combine(root, "hero.spineviewer.json");
    File.WriteAllText(skeleton, "source-must-not-change");

    var expected = new ViewerProjectDocument(
        1, skeleton, Path.Combine(root, "hero.atlas"), "walk", "armor",
        12.5, -4, 1.25, 15, true, false, true, 0.75, 0.6, "Dark");
    var store = new ViewerProjectStore();
    store.Save(project, expected);

    Assert(store.Load(project) == expected, "Project did not round-trip.");
    Assert(File.ReadAllText(skeleton) == "source-must-not-change", "Source skeleton was modified.");
    Assert(File.ReadAllText(project).Contains("\"schemaVersion\": 1"), "Schema version was not serialized.");
    Expect<ArgumentException>(() => store.Save(Path.Combine(root, "unsafe.json"), expected));
    Expect<InvalidDataException>(() => store.Save(project, expected with { TrackAlpha = double.NaN }));

    var uiProject = Path.Combine(root, "ui.spineviewer.json");
    var viewModel = new ShellViewModel(WorkspaceState.Ready, true, store, _ => uiProject);
    viewModel.SelectedAnimation = "walk";
    Assert(viewModel.IsDirty && viewModel.UndoCommand.CanExecute(null), "Edit did not set dirty/undo state.");
    viewModel.UndoCommand.Execute(null);
    Assert(!viewModel.IsDirty && viewModel.SelectedAnimation == "idle", "Undo did not restore the saved state.");
    viewModel.RedoCommand.Execute(null);
    viewModel.SaveCommand.Execute(null);
    Assert(!viewModel.IsDirty && File.Exists(uiProject), "Save did not clear dirty state.");
    Assert(store.Load(uiProject).SelectedAnimation == "walk", "UI edit was not saved.");

    var fixture = Path.Combine(Directory.GetCurrentDirectory(), "tests", "fixtures", "v41-minimal", "minimal.json");
    var realProject = Path.Combine(root, "real.spineviewer.json");
    var realViewModel = new ShellViewModel(
        WorkspaceState.Empty,
        true,
        store,
        _ => realProject,
        new AssetService(new SpineV41Adapter()));
    await realViewModel.OpenAssetAsync(fixture);
    Assert(realViewModel.State == WorkspaceState.Ready, "Real fixture did not reach Ready.");
    Assert(realViewModel.FilteredAnimations.SequenceEqual(["move"]), "Real animation metadata was not mapped.");
    Assert(realViewModel.Skins.SequenceEqual(["default"]), "Real skin metadata was not mapped.");
    Assert(realViewModel.RuntimeLabel == "Runtime 4.1" && realViewModel.Duration == 1, "Runtime or duration was not mapped.");
    realViewModel.ModelX = 12;
    realViewModel.SaveCommand.Execute(null);
    var realDocument = store.Load(realProject);
    Assert(realDocument.SkeletonPath == Path.GetFullPath(fixture), "Real skeleton path was not saved.");

    realViewModel.ModelY = 4;
    await realViewModel.OpenAssetAsync(Path.Combine(root, "missing.json"));
    Assert(realViewModel.State == WorkspaceState.Ready && realViewModel.IsDirty, "Dirty replacement was not canceled.");
    realViewModel.UndoCommand.Execute(null);

    await realViewModel.OpenAssetAsync(Path.Combine(root, "missing.json"));
    Assert(realViewModel.State == WorkspaceState.Failed, "Missing input did not reach Failed.");
    Assert(realViewModel.SkeletonFileName == "minimal.json", "Failed replacement discarded the prior document.");

    var unsupportedSkeleton = Path.Combine(root, "unsupported.json");
    File.WriteAllText(unsupportedSkeleton, "{}");
    File.WriteAllText(Path.ChangeExtension(unsupportedSkeleton, ".atlas"), "");
    var unsupportedViewModel = new ShellViewModel(
        WorkspaceState.Empty,
        true,
        store,
        _ => Path.Combine(root, "unsupported.spineviewer.json"),
        new AssetService(new UnsupportedRuntimeAdapter()));
    await unsupportedViewModel.OpenAssetAsync(unsupportedSkeleton);
    Assert(unsupportedViewModel.State == WorkspaceState.Unsupported, "Unsupported input did not reach Unsupported.");

    File.WriteAllText(project, File.ReadAllText(project).Replace("\"schemaVersion\": 1", "\"schemaVersion\": 2"));
    Expect<InvalidDataException>(() => store.Load(project));

    Console.WriteLine("Viewer project, real asset open, edit history, failure states, and source isolation passed.");
}
finally
{
    Directory.Delete(root, true);
}

static void Assert(bool condition, string message)
{
    if (!condition) throw new InvalidOperationException(message);
}

static void Expect<T>(Action action) where T : Exception
{
    try
    {
        action();
    }
    catch (T)
    {
        return;
    }

    throw new InvalidOperationException($"Expected {typeof(T).Name}.");
}

sealed class UnsupportedRuntimeAdapter : IRuntimeAdapter
{
    public string RuntimeLine => "4.1";
    public InspectResult Inspect(string skeletonPath, string atlasPath, bool overridden, CancellationToken cancellationToken) =>
        throw new NotSupportedException("Unsupported fixture.");
    public void Render(RenderRequest request, CancellationToken cancellationToken) => throw new NotSupportedException();
}
