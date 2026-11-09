using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using GameCore.HotUpdate;
using GameCore.HotUpdate.Battle;
using GameCore.HotUpdate.Battle.Show;
using GameCore.Scripts;
using HarmonyLib;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using UnityEngine;
using UnityEngine.Rendering;
using HydroponicsExpanded.Core;
using Numerics = System.Numerics;
using Object = UnityEngine.Object;

namespace HydroponicsExpanded.Adapters;

/// <summary>
/// Gives each station its own model. Its furniture row names a prefab the game does not have; when the game asks for
/// it, it gets a copy of the prefab of the grow box (or, for the Large Station's new look, the Large Station) it stands
/// in for, so every part the game looks for is there: the head bar, the collider, the animator, the plant spots and the
/// grow light the game switches with the power. The stock meshes are swapped for the station's, built from the model
/// file inside the DLL; the plant spots move onto the station's water bays, and the station's grow and status lights
/// hang under the game's grow light, so they go dark when the power does. The stock dye palette gets the station's
/// own painted materials on the game's dye shader. If a model cannot be built, the game gets the stock prefab, so the
/// station still works, looking like the grow box.
/// </summary>
internal static class StationModel
{
    private const string Folder = "Assets/RuntimeAssets/Prefabs/Furniture/";
    private const string ModelName = "HydroponicStation";

    /// <summary>The model's group of lit parts, and the stock part the game turns on and off with the power.</summary>
    private const string LightsName = "Lights";
    private const string GrowLedName = "GrowLED";

    // How far under the bar's lowest face the light sources go, so they light the plants and not the bar's inside.
    private const float LampBelowBar = 0.05f;
    private const string PlantLightName = "Plant_Light";

    /// <summary>The stock watering can and its splash, which the game shows while a player waters the pot.</summary>
    private const string WateringName = "WateringFX";

    private const string PlantSpotsPrefix = "PlantPos";

    /// <summary>The game's anchors for a plant that fills several spots, which it hangs among the spots.</summary>
    private const string PlantAnchorPrefix = "PlantAnchor_";

    /// <summary>The model's bay seats, one group per plant size: Seats_1 a pot per bay, Seats_2 and Seats_4 one big pot.</summary>
    private const string SeatsPrefix = "Seats_";
    private static readonly int[] SeatSizes = { 1, 2, 4 };
    private const float HeadBarAboveTop = 0.2f;

    /// <summary>Where the model's water bays are: each bay centre this far from the middle, each pot top this high.</summary>
    private const float BayOffset = 0.4525f;
    private const float PotTop = 0.535f;

    private static readonly IReadOnlyDictionary<string, Numerics.Vector3> Movers = SeatSizes.Select(size => SeatsPrefix + size).Append(LightsName)
        .ToDictionary(name => name, _ => Numerics.Vector3.UnitY);

    private static readonly Look[] Looks = StationRows.All.Select(kind => new Look(kind.Name, kind.ResName, kind.StockPrefab, kind.ModelFile))
        .Append(new Look("Hydroponic Station (Large)", StationRows.LargeResName, StationRows.LargeStockPrefab, StationRows.LargeModelFile))
        .ToArray();

    private static Action<string> _log = _ => { };
    private static GameObject? _holder;
    private static bool _seatsFailed;

    public static void Apply(Harmony harmony, Action<string> log)
    {
        _log = log;

        Patch(harmony, AccessTools.Method(typeof(BattleResource), "LoadPrefabWithFallback", new[] { typeof(string) }), prefix: nameof(GiveOurModel));
        Patch(harmony, AccessTools.Method(typeof(PreLoaderDataExtension), "AddChild"), prefix: nameof(SkipPreloadingOurModel));
        Patch(harmony, AccessTools.Method(typeof(AgentModel), "OnPlantCreateVisual", new[] { typeof(string), typeof(int) }), postfix: nameof(SeatsForThePlant));
    }

    /// <summary>The station whose prefab is at <paramref name="path"/>, or null.</summary>
    private static Look? LookAt(string? path)
    {
        for (var i = 0; i < Looks.Length; i++)
            if (Looks[i].Path == path) return Looks[i];

        return null;
    }

    private static bool GiveOurModel(string __0, ref GameObject __result)
    {
        if (LookAt(__0) is not { } look) return true;

        try
        {
            __result = Template(look) ?? BattleResource.LoadPrefabWithFallback(look.StockPath);
        }
        catch (Exception ex)
        {
            if (!look.HasFailed) _log($"the {look.Name}'s model could not be built, so it has the game's look: {ex}");

            look.HasFailed = true;
            __result = BattleResource.LoadPrefabWithFallback(look.StockPath);
        }

        return false;
    }

    /// <summary>The game preloads every furniture prefab a level uses from its asset bundles, and ours are in none.</summary>
    private static bool SkipPreloadingOurModel(string __1) => LookAt(__1) == null;

    /// <summary>
    /// The game has just put up the plant's models (or taken them down, given no model): the station shows the seats
    /// that fit the plant's size, so a watermelon grows out of one big pot instead of over the bay dividers.
    /// </summary>
    private static void SeatsForThePlant(AgentModel __instance, string __0, int __1)
    {
        try
        {
            var obj = __instance.Obj;

            if (obj == null) return;

            var root = obj.transform;
            var ours = root.Find(ModelName);

            if (ours == null) return;

            ShowSeats(ours, StationRows.PlantSize(SpotCount(root), __1, !string.IsNullOrEmpty(__0)));
        }
        catch (Exception ex)
        {
            if (!_seatsFailed) _log($"the station seats could not follow the plant: {ex}");

            _seatsFailed = true;
        }
    }

    /// <summary>The spots in the first plant spot group, the one the game plants in, leaving out its own anchors.</summary>
    private static int SpotCount(Transform root)
    {
        for (var g = 0; g < root.childCount; g++)
        {
            var group = root.GetChild(g);

            if (!group.name.StartsWith(PlantSpotsPrefix, StringComparison.Ordinal)) continue;

            var count = 0;

            for (var c = 0; c < group.childCount; c++)
                if (!group.GetChild(c).name.StartsWith(PlantAnchorPrefix, StringComparison.Ordinal)) count++;

            return count;
        }

        return 0;
    }

    /// <summary>Shows the seats for <paramref name="size"/> and hides the rest; a size the model has no seats for gets a pot per bay.</summary>
    private static void ShowSeats(Transform ours, int size)
    {
        if (ours.Find(SeatsPrefix + size) == null) size = 1;

        foreach (var each in SeatSizes)
        {
            var seats = ours.Find(SeatsPrefix + each);

            if (seats != null) seats.gameObject.SetActive(each == size);
        }
    }

    private static GameObject? Template(Look look)
    {
        if (look.Template != null && !look.Template.WasCollected) return look.Template;

        if (look.HasFailed) return null;

        var stock = BattleResource.LoadPrefabWithFallback(look.StockPath);

        if (stock == null)
        {
            _log($"the prefab {look.StockPath} is missing, so the {look.Name} has no model");
            look.HasFailed = true;

            return null;
        }

        look.Model ??= Glb.Read(Resource(look.ModelFile), Movers);

        if (_holder == null || _holder.WasCollected)
        {
            _holder = new GameObject("HydroponicsExpanded models");
            _holder.SetActive(false);
            Object.DontDestroyOnLoad(_holder);
        }

        var copy = Object.Instantiate(stock, _holder.transform);

        copy.name = look.ResName;
        Build(copy, look, look.Model);
        look.Template = copy;

        return copy;
    }

    private static void Build(GameObject copy, Look look, Model model)
    {
        var root = copy.transform;
        var watering = root.GetComponentsInChildren<Transform>(true).FirstOrDefault(part => part.name == WateringName);
        var renderers = copy.GetComponentsInChildren<MeshRenderer>(true);
        var stockMaterial = renderers.Where(renderer => !IsUnder(renderer.transform, watering)).Select(renderer => renderer.sharedMaterial).FirstOrDefault(material => material != null);
        var layer = renderers.Length > 0 ? renderers[0].gameObject.layer : copy.layer;

        _log($"building the {look.Name} on {look.StockPath}: {renderers.Length} mesh renderers, layer {layer}, shader {(stockMaterial == null ? "none" : stockMaterial.shader.name)}");

        if (stockMaterial == null) throw new InvalidOperationException("the stock prefab has no material to copy");

        // The stock meshes go, but for the watering can the game shows while a player waters it; everything else on the
        // prefab stays, so the game finds what it looks for.
        var kept = new List<MeshRenderer>();

        foreach (var renderer in copy.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            if (!IsUnder(renderer.transform, watering)) Object.DestroyImmediate(renderer);

        foreach (var renderer in renderers)
        {
            if (IsUnder(renderer.transform, watering))
            {
                kept.Add(renderer);

                continue;
            }

            var filter = renderer.GetComponent<MeshFilter>();

            Object.DestroyImmediate(renderer);

            if (filter != null) Object.DestroyImmediate(filter);
        }

        foreach (var filter in copy.GetComponentsInChildren<MeshFilter>(true))
            if (!IsUnder(filter.transform, watering) && filter.GetComponent<MeshRenderer>() == null) Object.DestroyImmediate(filter);

        var materials = model.Materials.Select(material => MakeMaterial(stockMaterial, material, model.Images)).ToArray();
        var ours = new GameObject(ModelName);
        var scale = root.lossyScale;

        ours.layer = layer;
        ours.transform.SetParent(root, false);
        ours.transform.localRotation = Quaternion.identity;
        ours.transform.localScale = new UnityEngine.Vector3(1f / scale.x, 1f / scale.y, 1f / scale.z);

        var made = new List<MeshRenderer>();
        var holders = Pivots(ours.transform, model, layer);

        foreach (var part in model.Parts)
            for (var i = 0; i < part.Meshes.Count; i++) made.Add(MakeMesh(holders[part.Name], $"{part.Name}_{i}", part.Meshes[i], materials[part.Meshes[i].Material], layer));

        // The model's middle is the middle of its tub, which stands where the stock furniture's middle is, foot on the floor.
        var (min, max) = model.Bounds();

        ours.transform.localPosition = Divide(new UnityEngine.Vector3(0f, -min.Y, 0f), scale);

        var worldMin = ours.transform.TransformPoint(Unity(min));
        var worldMax = ours.transform.TransformPoint(Unity(max));

        foreach (var collider in copy.GetComponentsInChildren<BoxCollider>(true)) Fit(collider, worldMin, worldMax);

        MoveSpots(root, ours.transform, scale);
        HangLights(root, holders, look);
        LowerLamps(copy, ours.transform, model, watering, look);
        ShowSeats(ours.transform, 1);

        var animator = copy.GetComponent<ModelAnimator>();

        if (animator != null)
        {
            var list = new Il2CppSystem.Collections.Generic.List<MeshRenderer>();

            foreach (var renderer in made.Concat(kept)) list.Add(renderer);

            animator.Renderers = list;
            animator.Skinneds = new Il2CppSystem.Collections.Generic.List<SkinnedMeshRenderer>();

            if (animator.HeadBar != null)
            {
                var bar = animator.HeadBar.transform;

                bar.position = new UnityEngine.Vector3(bar.position.x, Math.Max(worldMin.y, worldMax.y) + HeadBarAboveTop, bar.position.z);
            }
        }
        else
        {
            _log($"the stock prefab has no model animator; the {look.Name} may not light up when picked");
        }

        SetDyes(copy, look.Name, model, materials, made);

        _log($"{look.Name} model built: {model.TriangleCount} triangles, {made.Count} meshes, {model.Materials.Count} materials");
    }

    /// <summary>
    /// Moves the stock plant spots onto the model's water bays. The game hangs each plant on a child of a PlantPos group,
    /// laid out for the stock pot; each goes to the bay on its side of the middle, at the top of the bay's pot.
    /// </summary>
    private static void MoveSpots(Transform root, Transform ours, UnityEngine.Vector3 scale)
    {
        var moved = 0;

        for (var g = 0; g < root.childCount; g++)
        {
            var group = root.GetChild(g);

            if (!group.name.StartsWith(PlantSpotsPrefix, StringComparison.Ordinal)) continue;

            for (var c = 0; c < group.childCount; c++)
            {
                var spot = group.GetChild(c);
                var local = root.InverseTransformPoint(spot.position);
                var (x, y, z) = StationRows.PlantSpot(local.x * scale.x, local.z * scale.z, BayOffset, PotTop);

                spot.position = ours.TransformPoint(new UnityEngine.Vector3(x, y, z));
                moved++;
            }
        }

        if (moved == 0) _log("the stock prefab has no plant spots; plants may float off the water bays");
    }

    /// <summary>
    /// Hangs the model's lit parts under the stock grow light, which the game finds by name and turns off when the
    /// station loses power. A prefab without one gets an empty one, so the lights still follow the power.
    /// </summary>
    private static void HangLights(Transform root, Dictionary<string, Transform> holders, Look look)
    {
        if (!holders.TryGetValue(LightsName, out var lights) || lights == holders[Glb.BodyName]) return;

        var plantLight = root.GetComponentsInChildren<Transform>(true).FirstOrDefault(part => part.name == PlantLightName);

        if (plantLight == null)
        {
            var made = new GameObject(PlantLightName) { layer = lights.gameObject.layer };

            made.transform.SetParent(root, false);
            plantLight = made.transform;
            _log($"the {look.Name}'s stock prefab has no grow light, so it gets one for its lights");
        }

        lights.SetParent(plantLight, true);
    }

    /// <summary>
    /// Brings the stock prefab's light sources down to just under the model's grow light bar. The grow box lamp hangs
    /// far above the Small and Medium bars, and shows its glow with a beam of its own beside the light, so both move.
    /// The Large tank's side lights already shine from low down and stay.
    /// </summary>
    private static void LowerLamps(GameObject copy, Transform ours, Model model, Transform? watering, Look look)
    {
        if (model.Lowest(GrowLedName) is not float bottom) return;

        var underBar = ours.TransformPoint(Unity(new Numerics.Vector3(0f, bottom - LampBelowBar, 0f))).y;
        var moved = new List<string>();

        foreach (var source in copy.GetComponentsInChildren<Transform>(true))
        {
            if (IsUnder(source, watering) || !Lamps.IsSource(source.GetComponents<Component>().Select(part => part.GetIl2CppType().FullName))) continue;

            var at = source.position;
            var height = Lamps.Height(at.y, underBar);

            if (height == at.y) continue;

            source.position = new UnityEngine.Vector3(at.x, height, at.z);
            moved.Add($"{source.name} from {at.y:0.00}");
        }

        _log(moved.Count == 0 ? $"the {look.Name}'s light sources all hang under its grow light already" : $"the {look.Name}'s light sources moved to {underBar:0.00} m, under its grow light: {string.Join(", ", moved)} m");
    }

    private static bool IsUnder(Transform part, Transform? parent)
    {
        if (parent == null) return false;

        for (var at = part; at != null; at = at.parent)
            if (at == parent) return true;

        return false;
    }

    /// <summary>
    /// A holder for each separate part of the model (the lit parts) at its own origin, hung on the part it belongs to.
    /// The body's meshes hang straight on the model.
    /// </summary>
    private static Dictionary<string, Transform> Pivots(Transform model, Model source, int layer)
    {
        var holders = new Dictionary<string, Transform> { [Glb.BodyName] = model };

        foreach (var part in source.Parts.Where(part => part.Name != Glb.BodyName))
        {
            var pivot = new GameObject(part.Name) { layer = layer };

            pivot.transform.SetParent(model, false);
            pivot.transform.localPosition = Unity(part.Pivot);
            holders[part.Name] = pivot.transform;
        }

        foreach (var part in source.Parts)
            if (part.Parent != null && holders.TryGetValue(part.Parent, out var parent)) holders[part.Name].SetParent(parent, true);

        return holders;
    }

    private static MeshRenderer MakeMesh(Transform parent, string name, ModelMesh source, Material material, int layer)
    {
        var piece = new GameObject(name) { layer = layer };

        piece.transform.SetParent(parent, false);

        var mesh = new Mesh { name = name };

        if (source.Positions.Length > 65535) mesh.indexFormat = IndexFormat.UInt32;

        mesh.vertices = source.Positions.Select(Unity).ToArray();
        mesh.normals = source.Normals.Select(Unity).ToArray();
        mesh.uv = source.Uvs.Select(uv => new UnityEngine.Vector2(uv.X, uv.Y)).ToArray();
        mesh.triangles = source.Triangles;
        mesh.RecalculateBounds();
        mesh.UploadMeshData(true);

        piece.AddComponent<MeshFilter>().sharedMesh = mesh;

        var renderer = piece.AddComponent<MeshRenderer>();

        renderer.sharedMaterial = material;

        return renderer;
    }

    /// <summary>
    /// Fills the stock dye palette with the station's own. Each painted material gets one copy per dye on the game's
    /// dye shader (SL/LitDye, taken from the stock dyed materials), sharing one grey base map and one mask
    /// (<see cref="Paint"/>), so the game colours the station the way it colours its own furniture. The game swaps them
    /// in when the player dyes it. The colours come from the stock palette.
    /// </summary>
    private static void SetDyes(GameObject copy, string name, Model model, Material[] materials, List<MeshRenderer> made)
    {
        var palette = copy.GetComponent<FurnitureDyePalette>();
        var stock = palette?.Entries != null && palette.Entries.Length > 0 ? palette.Entries[0]?.Variants : null;
        var shader = stock != null && stock.Length > 0 ? stock[0]?.shader : null;

        if (palette == null || stock == null || shader == null)
        {
            _log($"the stock prefab has no dyed materials, so the {name} cannot be dyed");

            return;
        }

        var colours = new List<Color>();

        for (var i = 0; i < stock.Length; i++)
        {
            var dyed = stock[i];

            if (dyed == null || !dyed.HasProperty("_DyeColor"))
            {
                _log($"the stock dye {i + 1} has no colour, so the {name} cannot be dyed");

                return;
            }

            colours.Add(dyed.GetColor("_DyeColor"));
        }

        var entries = new List<FurnitureDyePalette.DyeEntry>();

        for (var m = 0; m < model.Materials.Count; m++)
        {
            if (model.Materials[m].Image is not int image) continue;

            var picture = Png.Decode(model.Images[image]);

            if (Paint.PaintShare(picture) < Paint.MinimumShare) continue;

            var materialName = model.Materials[m].Name;
            var greyBase = MakeTexture(materialName + "_DyeBase", Paint.DyeBase(picture));
            var mask = MakeTexture(materialName + "_DyeMask", Paint.Mask(picture), linear: true);
            var variants = new Il2CppReferenceArray<Material>(colours.Count);

            for (var slot = 0; slot < colours.Count; slot++)
            {
                // Swapping the shader keeps every property both shaders share: our maps, colours and keywords.
                var variant = new Material(materials[m]) { name = $"{materials[m].name}_Dye{slot + 1:00}", shader = shader };

                foreach (var map in new[] { "_BaseMap", "_MainTex" })
                    if (variant.HasProperty(map)) variant.SetTexture(map, greyBase);

                variant.SetTexture("_DyeMask", mask);
                variant.SetColor("_DyeColor", colours[slot]);
                variants[slot] = variant;
            }

            entries.Add(new FurnitureDyePalette.DyeEntry { BaseMaterial = materials[m], Variants = variants });
        }

        palette.Entries = new Il2CppReferenceArray<FurnitureDyePalette.DyeEntry>(entries.ToArray());
        palette.Targets = new Il2CppReferenceArray<Renderer>(made.Count);

        for (var i = 0; i < made.Count; i++) palette.Targets[i] = made[i];

        _log($"{name} dyes: {entries.Count} painted materials on {shader.name}, {colours.Count} colours each");
    }

    private static Material MakeMaterial(Material stock, ModelMaterial source, IReadOnlyList<byte[]> images)
    {
        var material = new Material(stock) { name = ModelName + "_" + source.Name };

        foreach (var map in new[] { "_BumpMap", "_MetallicGlossMap", "_OcclusionMap", "_EmissionMap", "_DetailAlbedoMap", "_DetailNormalMap", "_SpecGlossMap" })
            if (material.HasProperty(map)) material.SetTexture(map, null);

        foreach (var keyword in new[] { "_NORMALMAP", "_METALLICSPECGLOSSMAP", "_OCCLUSIONMAP", "_EMISSION", "_DETAIL_MULX2", "_SPECGLOSSMAP" }) material.DisableKeyword(keyword);

        Texture texture = source.Image is int image ? MakeTexture(source.Name, Png.Decode(images[image])) : Texture2D.whiteTexture;

        foreach (var map in new[] { "_BaseMap", "_MainTex" })
        {
            if (!material.HasProperty(map)) continue;

            material.SetTexture(map, texture);
            material.SetTextureScale(map, UnityEngine.Vector2.one);
            material.SetTextureOffset(map, UnityEngine.Vector2.zero);
        }

        // glTF colours are linear; Unity takes material colours as sRGB and converts them itself.
        var colour = new Color(source.BaseColor.X, source.BaseColor.Y, source.BaseColor.Z, source.BaseColor.W).gamma;

        foreach (var name in new[] { "_BaseColor", "_Color" })
            if (material.HasProperty(name)) material.SetColor(name, colour);

        if (material.HasProperty("_Metallic")) material.SetFloat("_Metallic", source.Metallic);

        foreach (var name in new[] { "_Smoothness", "_Glossiness" })
            if (material.HasProperty(name)) material.SetFloat(name, 1f - source.Roughness);

        // The lights glow. glTF gives a linear colour times a strength; Unity wants the sRGB colour and converts it, so
        // the colour is converted at full brightness and then scaled.
        var bright = Math.Max(source.Emissive.X, Math.Max(source.Emissive.Y, source.Emissive.Z));

        if (bright > 0f && material.HasProperty("_EmissionColor"))
        {
            var glow = new Color(source.Emissive.X / bright, source.Emissive.Y / bright, source.Emissive.Z / bright).gamma * bright;

            material.EnableKeyword("_EMISSION");
            material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
            material.SetColor("_EmissionColor", glow);
        }

        return material;
    }

    /// <summary>
    /// Png rows run from the top down and Unity's from the bottom up, and glTF's uvs start at the top while Unity's
    /// start at the bottom: the two flips cancel, so the rows go in as they are.
    /// </summary>
    private static Texture2D MakeTexture(string name, Picture picture, bool linear = false)
    {
        // A mask is data, not colour, so it is stored linear; colour maps are sRGB.
        var texture = new Texture2D(picture.Width, picture.Height, TextureFormat.RGBA32, true, linear)
        {
            name = ModelName + "_" + name,
            wrapMode = TextureWrapMode.Repeat,
            filterMode = FilterMode.Trilinear,
        };
        var pixels = new Il2CppStructArray<Color32>(picture.Width * picture.Height);

        for (var p = 0; p < pixels.Length; p++)
            pixels[p] = new Color32(picture.Rgba[p * 4], picture.Rgba[p * 4 + 1], picture.Rgba[p * 4 + 2], picture.Rgba[p * 4 + 3]);

        texture.SetPixels32(pixels);
        texture.Apply(true, true);

        return texture;
    }

    private static void Fit(BoxCollider collider, UnityEngine.Vector3 worldMin, UnityEngine.Vector3 worldMax)
    {
        var min = new UnityEngine.Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
        var max = new UnityEngine.Vector3(float.MinValue, float.MinValue, float.MinValue);

        foreach (var corner in Corners(worldMin, worldMax))
        {
            var point = collider.transform.InverseTransformPoint(corner);

            min = UnityEngine.Vector3.Min(min, point);
            max = UnityEngine.Vector3.Max(max, point);
        }

        collider.center = (min + max) / 2f;
        collider.size = max - min;
    }

    private static IEnumerable<UnityEngine.Vector3> Corners(UnityEngine.Vector3 a, UnityEngine.Vector3 b)
    {
        for (var i = 0; i < 8; i++) yield return new UnityEngine.Vector3((i & 1) == 0 ? a.x : b.x, (i & 2) == 0 ? a.y : b.y, (i & 4) == 0 ? a.z : b.z);
    }

    private static UnityEngine.Vector3 Divide(UnityEngine.Vector3 v, UnityEngine.Vector3 by) => new(v.x / by.x, v.y / by.y, v.z / by.z);

    private static UnityEngine.Vector3 Unity(Numerics.Vector3 v) => new(v.X, v.Y, v.Z);

    /// <summary>One station's model: where the game asks for it, the stock prefab it is built on, and the copy once built.</summary>
    private sealed class Look
    {
        public Look(string name, string resName, string stockPrefab, string modelFile)
        {
            Name = name;
            ResName = resName;
            ModelFile = modelFile;
            Path = Folder + resName;
            StockPath = Folder + stockPrefab;
        }

        public string Name { get; }

        public string ResName { get; }

        public string ModelFile { get; }

        public string Path { get; }

        public string StockPath { get; }

        public GameObject? Template { get; set; }

        public Model? Model { get; set; }

        public bool HasFailed { get; set; }
    }

    internal static byte[] Resource(string name)
    {
        using var stream = typeof(StationModel).Assembly.GetManifestResourceStream(name) ?? throw new FileNotFoundException("the mod's DLL is missing " + name);
        using var copy = new MemoryStream();

        stream.CopyTo(copy);

        return copy.ToArray();
    }

    private static void Patch(Harmony harmony, System.Reflection.MethodInfo? target, string? prefix = null, string? postfix = null)
    {
        if (target == null)
        {
            _log("cannot find a model loading method; the stations may look like the grow boxes");

            return;
        }

        try
        {
            harmony.Patch(target, prefix: prefix == null ? null : new HarmonyMethod(typeof(StationModel), prefix), postfix: postfix == null ? null : new HarmonyMethod(typeof(StationModel), postfix));
        }
        catch (Exception ex)
        {
            _log($"cannot hook {target.DeclaringType?.Name}.{target.Name}: {ex.Message}");
        }
    }
}
