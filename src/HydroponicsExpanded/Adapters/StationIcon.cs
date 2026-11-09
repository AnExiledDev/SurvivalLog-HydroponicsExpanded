using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using HydroponicsExpanded.Core;

namespace HydroponicsExpanded.Adapters;

/// <summary>
/// Puts each station's icon, and one for each colour the Small and Medium can be crafted in, where the game's web UI
/// looks for pictures, next to the game's furniture icons. The coloured ones are made from the plain icon the way
/// the dye shader colours the model. An icon is written only when the file there is missing or differs, so a mod update
/// brings its new icon.
/// </summary>
internal static class StationIcon
{
    /// <summary>What the background writer has to say, held for the main thread: a worker never logs to the console.</summary>
    private static readonly ConcurrentQueue<string> Messages = new();

    /// <summary>Call on the main thread, since Unity gives the folder's path only there. The icons are made and written in the background.</summary>
    public static void Place()
    {
        var folder = Path.Combine(Application.streamingAssetsPath, "WebUI", StationRows.WebIconFolder);

        Task.Run(() =>
        {
            foreach (var kind in StationRows.All) Messages.Enqueue(Place(folder, kind));

            Messages.Enqueue(PlaceLarge(folder));
        });
    }

    /// <summary>Call on the main thread: logs what the background writer has finished so far.</summary>
    public static void Report(Action<string> log)
    {
        while (Messages.TryDequeue(out var message)) log(message);
    }

    private static string Place(string folder, StationKind kind)
    {
        try
        {
            var icon = StationModel.Resource(kind.IconFile);
            var plain = Write(folder, kind.IconFile, icon);

            try
            {
                var dyed = Paint.DyedIcons(Png.Decode(icon), Dyes.All.Select(dye => dye.Colour)).ToList();
                var written = Dyes.All.Where((dye, i) => Write(folder, Dyes.IconFile(kind, dye), Png.Encode(dyed[i]))).Count();

                return $"the {kind.Name}'s icon is {(plain ? "written" : "in place")}, and {written} of its 8 coloured icons were written";
            }
            catch (Exception ex)
            {
                return $"the {kind.Name}'s icon is {(plain ? "written" : "in place")}, but its coloured icons could not be made, so its coloured packages show without one: {ex.Message}";
            }
        }
        catch (Exception ex)
        {
            return $"the {kind.Name}'s icon could not be written, so it shows without one: {ex.Message}";
        }
    }

    // The Large Station's coloured packages are the game's, with the game's icons.
    private static string PlaceLarge(string folder)
    {
        const string name = "Hydroponic Station (Large)";

        try
        {
            return Write(folder, StationRows.LargeIconFile, StationModel.Resource(StationRows.LargeIconFile)) ? $"the {name}'s icon was written" : $"the {name}'s icon is in place";
        }
        catch (Exception ex)
        {
            return $"the {name}'s icon could not be written, so it shows without one: {ex.Message}";
        }
    }

    /// <summary>Writes the file unless it already holds these bytes. Returns whether it wrote.</summary>
    private static bool Write(string folder, string name, byte[] bytes)
    {
        var file = Path.Combine(folder, name);

        if (File.Exists(file) && File.ReadAllBytes(file).SequenceEqual(bytes)) return false;

        Directory.CreateDirectory(folder);
        File.WriteAllBytes(file, bytes);

        return true;
    }
}
