using System.Text;
using System.Text.Json;
using SpineViewerWPF.Core;

namespace SpineViewerWPF.Application;

public sealed class ViewerProjectStore
{
    public const int CurrentSchemaVersion = 1;
    public const string Extension = ".spineviewer.json";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        WriteIndented = true
    };

    public ViewerProjectDocument Load(string path)
    {
        var document = JsonSerializer.Deserialize<ViewerProjectDocument>(
            File.ReadAllText(ProjectPath(path), Encoding.UTF8),
            JsonOptions) ?? throw new InvalidDataException("Project file is empty.");
        Validate(document);
        return document;
    }

    public string Save(string path, ViewerProjectDocument document)
    {
        Validate(document);
        var destination = ProjectPath(path);
        var directory = Path.GetDirectoryName(destination) ?? ".";
        Directory.CreateDirectory(directory);
        var temporary = Path.Combine(directory, $".{Path.GetFileName(destination)}.{Guid.NewGuid():N}.tmp");

        try
        {
            File.WriteAllText(temporary, JsonSerializer.Serialize(document, JsonOptions), new UTF8Encoding(false));
            File.Move(temporary, destination, true);
            return destination;
        }
        finally
        {
            if (File.Exists(temporary)) File.Delete(temporary);
        }
    }

    private static string ProjectPath(string path)
    {
        if (string.IsNullOrWhiteSpace(path)) throw new ArgumentException("Project path is required.", nameof(path));
        var fullPath = Path.GetFullPath(path);
        if (!fullPath.EndsWith(Extension, StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException($"Project path must end with '{Extension}'.", nameof(path));
        return fullPath;
    }

    private static void Validate(ViewerProjectDocument document)
    {
        if (document.SchemaVersion != CurrentSchemaVersion)
            throw new InvalidDataException($"Unsupported project schema version: {document.SchemaVersion}.");
        if (string.IsNullOrWhiteSpace(document.SkeletonPath))
            throw new InvalidDataException("Skeleton path is required.");
        if (string.IsNullOrWhiteSpace(document.SelectedSkin))
            throw new InvalidDataException("Selected skin is required.");
        if (!Finite(document.ModelX, document.ModelY, document.ModelScale, document.ModelRotation, document.PlaybackSpeed, document.TrackAlpha))
            throw new InvalidDataException("Project contains a non-finite numeric value.");
        if (document.ModelScale is <= 0 or > 100)
            throw new InvalidDataException("Model scale must be greater than 0 and at most 100.");
        if (document.PlaybackSpeed is <= 0 or > 10)
            throw new InvalidDataException("Playback speed must be greater than 0 and at most 10.");
        if (document.TrackAlpha is < 0 or > 1)
            throw new InvalidDataException("Track alpha must be between 0 and 1.");
        if (document.BackgroundMode is not ("Checkerboard" or "Dark" or "Light"))
            throw new InvalidDataException($"Unsupported background mode: {document.BackgroundMode}.");
        if (document.SceneLayers is not null)
        {
            if (document.SceneLayers.Count is < 1 or > 8)
                throw new InvalidDataException("A scene must contain between one and eight layers.");
            foreach (var layer in document.SceneLayers)
            {
                if (string.IsNullOrWhiteSpace(layer.SkeletonPath) || string.IsNullOrWhiteSpace(layer.Animation) || string.IsNullOrWhiteSpace(layer.SelectedSkin))
                    throw new InvalidDataException("Scene layers require a skeleton, animation, and skin.");
                if (!Finite(layer.ModelX, layer.ModelY, layer.ModelScale, layer.ModelRotation, layer.Opacity))
                    throw new InvalidDataException("Scene layer contains a non-finite numeric value.");
                if (layer.ModelScale is <= 0 or > 100 || layer.Opacity is < 0 or > 1)
                    throw new InvalidDataException("Scene layer transform or opacity is outside the supported range.");
            }
        }
    }

    private static bool Finite(params double[] values) => values.All(double.IsFinite);
}
