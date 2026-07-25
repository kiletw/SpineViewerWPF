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
    var viewModel = new ShellViewModel(WorkspaceState.Ready, true, store, () => uiProject);
    viewModel.SelectedAnimation = "walk";
    Assert(viewModel.IsDirty && viewModel.UndoCommand.CanExecute(null), "Edit did not set dirty/undo state.");
    viewModel.UndoCommand.Execute(null);
    Assert(!viewModel.IsDirty && viewModel.SelectedAnimation == "idle", "Undo did not restore the saved state.");
    viewModel.RedoCommand.Execute(null);
    viewModel.SaveCommand.Execute(null);
    Assert(!viewModel.IsDirty && File.Exists(uiProject), "Save did not clear dirty state.");
    Assert(store.Load(uiProject).SelectedAnimation == "walk", "UI edit was not saved.");

    File.WriteAllText(project, File.ReadAllText(project).Replace("\"schemaVersion\": 1", "\"schemaVersion\": 2"));
    Expect<InvalidDataException>(() => store.Load(project));

    Console.WriteLine("Viewer project round-trip, edit history, validation, and source isolation passed.");
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
