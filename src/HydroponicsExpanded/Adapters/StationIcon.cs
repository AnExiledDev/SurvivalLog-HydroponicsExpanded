using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using HydroponicsExpanded.Core;

namespace HydroponicsExpanded.Adapters;

/// <summary>
/// Puts each station's icon where the game's web UI looks for pictures, next to the game's own furniture icons. An icon
/// is written only when the file there is missing or differs, so a mod update brings its new icon.
/// </summary>
internal static class StationIcon
{
    /// <summary>Call on the main thread, since Unity gives the folder's path only there. Returns what happened.</summary>
    public static string Place()
    {
        var folder = Path.Combine(Application.streamingAssetsPath, "WebUI", StationRows.WebIconFolder);
        var notes = new List<string>();

        foreach (var kind in StationRows.All) notes.Add(Place(folder, kind.Name, kind.IconFile));

        notes.Add(Place(folder, "Hydroponic Station (Large)", StationRows.LargeIconFile));

        return string.Join("; ", notes);
    }

    private static string Place(string folder, string name, string iconFile)
    {
        try
        {
            var file = Path.Combine(folder, iconFile);
            var icon = StationModel.Resource(iconFile);

            if (File.Exists(file) && File.ReadAllBytes(file).SequenceEqual(icon)) return $"the {name}'s icon is in place";

            Directory.CreateDirectory(folder);
            File.WriteAllBytes(file, icon);

            return $"the {name}'s icon was written to {file}";
        }
        catch (Exception ex)
        {
            return $"the {name}'s icon could not be written, so it shows without one: {ex.Message}";
        }
    }
}
