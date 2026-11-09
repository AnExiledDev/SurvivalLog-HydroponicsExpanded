using System;
using System.Collections.Generic;
using System.Linq;

namespace HydroponicsExpanded.Core;

/// <summary>
/// Which parts of a stock prefab's grow lamp are its light sources, and where they hang on a station. A stock lamp
/// shines with a Unity light and shows its glow with a separate volumetric beam, so both have to move together.
/// </summary>
public static class Lamps
{
    private const string LightType = "UnityEngine.Light";

    /// <summary>The volumetric beam asset the game uses; its components all live in this namespace.</summary>
    private const string BeamNamespace = "VLB.";

    /// <summary>Whether a part holding components of these full type names is a light source: a light or a beam.</summary>
    public static bool IsSource(IEnumerable<string> componentTypes) =>
        componentTypes.Any(type => type == LightType || type.StartsWith(BeamNamespace, StringComparison.Ordinal));

    /// <summary>
    /// The height a light source at <paramref name="height"/> goes to on a station whose lamp hangs at
    /// <paramref name="underBar"/>. One hanging higher comes down to it; one lower, like the Large tank's side
    /// lights that shine across the water, stays where the game put it.
    /// </summary>
    public static float Height(float height, float underBar) => Math.Min(height, underBar);
}
