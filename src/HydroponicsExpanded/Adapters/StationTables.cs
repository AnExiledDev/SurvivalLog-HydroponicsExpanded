using System;
using System.Collections.Generic;
using System.Linq;
using GameCore.HotUpdate;
using GameCore.HotUpdate.Battle.Logic;
using Il2CppInterop.Runtime.InteropTypes;
using HydroponicsExpanded.Core;
using Il2CppDictionary = Il2CppSystem.Collections.Generic;
using Il2CppInts = Il2CppSystem.Collections.Generic.List<int>;
using ExiledToolkit.Game;

namespace HydroponicsExpanded.Adapters;

/// <summary>
/// Writes the stations' rows and text into the game's tables once they are loaded, then rebuilds the game's lookups
/// that were made from them, and brings the stations into a run: the flowerpot shops before the disaster sell them next
/// to the grow box of the same size. It writes again when the game loads a table afresh or a setting changes, over its
/// own rows where they already are. It also points the Hydroponic Station (Large) at the mod's model while that setting is
/// on, and every few seconds unlocks the stations' recipes a run has earned: each at its Planting level, and the Small and
/// Medium along with the Large Station's.
/// </summary>
internal sealed class StationTables
{
    private const float CheckSeconds = 1f;
    private const float RevealSeconds = 5f;

    // The six row tables, the shop lists, and the two languages.
    private const int Watched = 9;

    private readonly Action<string> _log;
    private readonly IntPtr[] _seen = new IntPtr[Watched];
    private readonly IntPtr[] _applied = new IntPtr[Watched];
    private readonly List<Reveal> _reveals = new();
    private HashSet<IntPtr> _ours = new();
    private IReadOnlyList<StationSettings>? _appliedSettings;
    private bool? _appliedNewLook;
    private LargeLook? _largeStock;
    private float _nextCheckAt;
    private float _nextRevealAt;

    public StationTables(Action<string> log)
    {
        _log = log;
    }

    public void Tick(float now)
    {
        if (now >= _nextRevealAt)
        {
            _nextRevealAt = now + RevealSeconds;
            UnlockRecipes();
        }

        if (now < _nextCheckAt) return;

        _nextCheckAt = now + CheckSeconds;

        var config = GameSingleton.Existing<ConfigManager>();
        var english = config?.customCache?.EnglishLanguageMap;
        var chinese = config?.customCache?.ChineseLanguageMap;

        if (config == null || english == null || chinese == null) return;

        KeepDyeable(config);

        var tables = new Tables(config);

        if (!tables.HasRows) return;

        tables.Pointers(_seen, english, chinese);

        var settings = CurrentSettings.Current;
        var isNewLook = CurrentSettings.NewLook;

        if (ReferenceEquals(settings, _appliedSettings) && isNewLook == _appliedNewLook && _seen.AsSpan().SequenceEqual(_applied)) return;

        var isChange = _appliedSettings != null && !ReferenceEquals(settings, _appliedSettings);

        _seen.CopyTo(_applied, 0);
        _appliedSettings = settings;
        _appliedNewLook = isNewLook;
        Write(config, tables, english, chinese, settings);
        Step("Large Station look", () => SetLargeLook(tables, isNewLook));

        // A plant growing in a station worked out its growth time when it was planted; a new growth boost reaches it now.
        if (isChange) Step("plants already growing", RegrowPlanted);
    }

    /// <summary>
    /// Has every plant growing in one of the stations work out its growth time again from the rows as they are now. The
    /// game's step for a power change does this and keeps the share of growth already done.
    /// </summary>
    private void RegrowPlanted()
    {
        var furnitures = GameSingleton.Existing<BattleLogicWorld>()?._AgentManager?.GetAllFurnitures();

        if (furnitures == null) return;

        var plantRows = StationRows.All.Select(kind => kind.Ids.Plant).ToHashSet();
        var count = 0;

        for (var i = 0; i < furnitures.Count; i++)
        {
            if (furnitures[i] is not { } furniture || AgentTools.GetAgentComponent<PlantComponent>(furniture) is not { } plant) continue;
            if (!plantRows.Contains(plant.FurniturePlantConfigId)) continue;

            plant.RecalcGrowthTimeForPowerChange();
            count++;
        }

        if (count > 0) _log($"{count} stations' plants brought up to the new growth boost");
    }

    private void Write(ConfigManager config, Tables tables, Il2CppDictionary.Dictionary<string, string> english, Il2CppDictionary.Dictionary<string, string> chinese, IReadOnlyList<StationSettings> settings)
    {
        var views = new Dictionary<Table, ITableView>
        {
            [Table.Item] = new TableView<Config_Item>(tables.Items!, () => new Config_Item()),
            [Table.Furniture] = new TableView<Config_Furniture>(tables.Furniture!, () => new Config_Furniture()),
            [Table.Plant] = new TableView<Config_FurniturePlant>(tables.Plants!, () => new Config_FurniturePlant()),
            [Table.Recipe] = new TableView<Config_ProductionList>(tables.Recipes!, () => new Config_ProductionList()),
            [Table.Action] = new TableView<Config_Action>(tables.Actions!, () => new Config_Action()),
            [Table.Effect] = new TableView<Config_Effect>(tables.Effects!, () => new Config_Effect()),
        };

        // Our rows from the last write, where they still are: a table loaded afresh has none of them.
        var before = _ours;

        _ours = new HashSet<IntPtr>();

        foreach (var kind in StationRows.All)
        foreach (var ids in Dyes.All.Select(dye => Dyes.Ids(kind.Ids, dye)).Prepend(kind.Ids))
        foreach (var (table, id) in RowsOf(ids))
            views[table].Keep(id, before, _ours);

        var written = new List<(StationKind Kind, StationSettings Settings, int[] Materials)>();
        var items = tables.Items!;
        var recipes = tables.Recipes!;
        var choices = StationChoices.Of(settings, items.ContainsKey, list => RecipeUsing(recipes, list));

        for (var i = 0; i < StationRows.All.Count; i++)
        {
            var kind = StationRows.All[i];
            var wanted = settings[i];
            var (_, materials, problem, back, returnsProblem, _, _) = choices[i];

            if (problem != null) _log($"{kind.Name}: {problem.Log}, so it takes its default materials");

            if (returnsProblem != null) _log($"{kind.Name}: {returnsProblem.Log}, so taking it down gives back {wanted.ReturnPercent}% of its materials");

            var plan = StationRows.Plan(kind, materials, wanted, back);
            var problems = StationRows.Problems(plan, (table, id) => views[table].SlotAt(id, _ours), list => RecipeUsing(recipes, list));

            if (problems.Count > 0)
            {
                foreach (var reason in problems) _log($"cannot add the {kind.Name}: {reason}");

                continue;
            }

            SetTexts(plan.Texts, english, chinese);

            var refused = new List<string>();

            foreach (var row in plan.Rows) refused.AddRange(views[row.Table].Write(row, _ours));

            foreach (var reason in refused) _log($"{kind.Name} field not set: {reason}");

            WriteDyed(kind, materials, views, recipes, english, chinese);
            written.Add((kind, wanted, materials));
        }

        if (written.Count == 0) return;

        // The recipe lookup is built once from the recipe table, and the "where to get it" lookups rebuild themselves
        // when this one is empty.
        config.InitProductionListMap();
        config._furnitureToPackageItems = null;
        // The coloured recipe lookups rebuild themselves when this is empty.
        config._dyedRecipeToBase = null;

        var kinds = written.Select(done => done.Kind).ToList();

        Step("shop lists", () => AddToShops(tables.Shops, written.Select(done => (done.Kind, done.Settings.ShopStock)).ToList()));
        ShopStock.Restock(written.Select(done => (done.Kind.Ids.Furniture, done.Settings.ShopStock)).ToList());
        KeepReveals(recipes, kinds);

        foreach (var (kind, wanted, materials) in written)
        {
            var ids = kind.Ids;

            _log($"{kind.Name} added: item {ids.Item}, furniture {ids.Furniture}, recipe {ids.Recipe}, plant row {ids.Plant}; power {wanted.PowerCost}, growth boost {wanted.GrowthBoost}, materials {MaterialText(materials)}, price {wanted.ShopPrice}, stock {wanted.ShopStock}");
        }
    }

    /// <summary>
    /// Writes a station's coloured packages, one for each dye whose grow box rows are there to copy. A dye that cannot
    /// be written is left out and the others still are: the plain station works without them.
    /// </summary>
    private void WriteDyed(StationKind kind, int[] materials, Dictionary<Table, ITableView> views, Il2CppDictionary.Dictionary<int, Config_ProductionList> recipes, Il2CppDictionary.Dictionary<string, string> english, Il2CppDictionary.Dictionary<string, string> chinese)
    {
        var skipped = new List<string>();

        foreach (var dye in Dyes.All)
        {
            var plan = new StationPlan(Dyes.Rows(kind, dye, materials), Dyes.Texts(kind, dye));
            var problems = StationRows.Problems(plan, (table, id) => views[table].SlotAt(id, _ours), list => RecipeUsing(recipes, list));

            if (problems.Count > 0)
            {
                skipped.Add($"{dye.En} ({string.Join("; ", problems)})");

                continue;
            }

            SetTexts(plan.Texts, english, chinese);

            foreach (var row in plan.Rows)
            foreach (var reason in views[row.Table].Write(row, _ours))
                _log($"{dye.En} {kind.Name} field not set: {reason}");
        }

        if (skipped.Count > 0) _log($"the {kind.Name} cannot be crafted in these colours: {string.Join(", ", skipped)}");
    }

    /// <summary>
    /// Puts the stations' prefab names on the game's list of furniture that can be dyed, which it loads once a run
    /// starts, when the grow box or Large Station it stands in for is on it, and makes the game rebuild its lookup from
    /// it. A few lookups a second while they are there.
    /// </summary>
    private void KeepDyeable(ConfigManager config)
    {
        var names = config._dyeableResNames;

        if (names == null) return;

        var stale = false;

        foreach (var kind in StationRows.All)
            if (names.Contains(kind.StockPrefab) && !names.Contains(kind.ResName)) stale |= names.Add(kind.ResName);

        if (names.Contains(StationRows.LargeStockPrefab) && !names.Contains(StationRows.LargeResName)) stale |= names.Add(StationRows.LargeResName);

        // A lookup built before our rows were written lacks them too.
        var ids = config._dyeableFurnitureIds;
        var furniture = config._Config_Furniture_Dict;

        if (ids != null && furniture != null && config._dyeableSourceDict?.Pointer == furniture.Pointer)
            foreach (var kind in StationRows.All)
                if (furniture.ContainsKey(kind.Ids.Furniture) && !ids.Contains(kind.Ids.Furniture)) stale = true;

        if (!stale) return;

        // The game rebuilds its list of dyeable ids when this no longer points at the furniture table.
        config._dyeableSourceDict = null;
        _log("the stations can be dyed like the furniture they stand in for");
    }

    /// <summary>Lists each station in the shops that sell its grow box, with its stock, or takes it off when its stock is 0.</summary>
    private void AddToShops(Il2CppDictionary.Dictionary<int, Config_ShopFurniture>? shops, IReadOnlyList<(StationKind Kind, int Stock)> wanted)
    {
        if (shops == null || shops.Count == 0) return;

        foreach (var id in wanted.SelectMany(pair => pair.Kind.Shops).Distinct())
        {
            if (!shops.ContainsKey(id))
            {
                _log($"shop {id} is missing, so it does not sell the stations");

                continue;
            }

            var shop = shops[id];
            var here = wanted.Where(pair => pair.Kind.Shops.Contains(id)).Select(pair => (pair.Kind.Ids.Furniture, pair.Stock)).ToList();
            var ours = here.Select(pair => pair.Furniture).ToArray();

            if (Unlocks.Shop(ToArray(shop.ItemIdList), ToArray(shop.ItemCountList), ours, here) is not { } lists) continue;

            shop.ItemIdList = RowFields.ToList(lists.Ids);
            shop.ItemCountList = RowFields.ToList(lists.Counts);
        }
    }

    /// <summary>
    /// Points the Hydroponic Station (Large)'s furniture row and its package at the mod's model and icon, or back at the
    /// game's. The game's values are read the first time and kept, so turning it off puts back what was there.
    /// Stations already built keep the model they were built with until the save is loaded again.
    /// </summary>
    private void SetLargeLook(Tables tables, bool isNewLook)
    {
        var furniture = tables.Furniture!;
        var items = tables.Items!;

        if (!furniture.ContainsKey(StationRows.LargeFurniture) || !items.ContainsKey(StationRows.LargeItem))
        {
            _log($"furniture {StationRows.LargeFurniture} or item {StationRows.LargeItem} is missing, so the Hydroponic Station (Large) keeps its look");

            return;
        }

        var station = furniture[StationRows.LargeFurniture];
        var package = items[StationRows.LargeItem];

        // A table loaded afresh holds the game's values again, so they are read again from it.
        if (_largeStock == null || station.ResName != StationRows.LargeResName) _largeStock = new LargeLook(station.ResName, station.ICON, package.WebIcon);

        var look = isNewLook ? new LargeLook(StationRows.LargeResName, StationRows.LargeWebIcon, StationRows.LargeWebIcon) : _largeStock;

        if (station.ResName == look.ResName && station.ICON == look.Icon && package.WebIcon == look.WebIcon) return;

        station.ResName = look.ResName;
        station.ICON = look.Icon;
        package.WebIcon = look.WebIcon;
        _log(isNewLook ? "the Hydroponic Station (Large) has the new look" : "the Hydroponic Station (Large) has the game's look");
    }

    private void KeepReveals(Il2CppDictionary.Dictionary<int, Config_ProductionList> recipes, IReadOnlyList<StationKind> kinds)
    {
        _reveals.Clear();

        foreach (var kind in kinds)
            if (recipes.ContainsKey(kind.Ids.Recipe)) _reveals.Add(new Reveal(kind, recipes[kind.Ids.Recipe]));
    }

    /// <summary>
    /// Unlocks each station's recipe once the run has earned it (see <see cref="Unlocks.RecipesDue"/>), in a run already
    /// going too, and lists an unlocked one again at the workbench after its materials setting changed, since the game
    /// lists a recipe by its materials. Uninstall prep stops it, so the recipes it takes back stay gone.
    /// </summary>
    private void UnlockRecipes()
    {
        if (_reveals.Count == 0 || UninstallPrep.IsOn) return;

        try
        {
            var world = GameSingleton.Existing<BattleLogicWorld>();
            var tool = world?._ToolTableManager;
            var skills = world?._ProficiencyManager;

            if (tool == null || skills == null) return;

            var unlocked = new HashSet<int>();
            var list = tool._CraftUnlockedProductionIds_k__BackingField;

            if (list != null)
                for (var i = 0; i < list.Count; i++) unlocked.Add(list[i]);

            var planting = skills.GetLevel(ProficiencyType.Plant);
            var ours = _reveals.Select(reveal => (reveal.Kind.Ids.Recipe, CurrentSettings.UnlockLevelOf(reveal.Kind))).ToList();
            var due = Unlocks.RecipesDue(planting, (StationRows.LargeRecipe, CurrentSettings.LargeUnlockLevel), ours, unlocked);

            if (due.Count > 0)
            {
                var ids = new Il2CppInts();

                foreach (var id in due) ids.Add(id);

                tool.AddProductionCraftUnlocks(ids);
                _log($"recipes {string.Join(", ", due)} unlocked at Planting level {planting}");
            }

            foreach (var reveal in _reveals)
            {
                var id = reveal.Kind.Ids.Recipe;
                var materials = reveal.Ours.MaterialList;

                if (!unlocked.Contains(id) || materials == null || tool.IsProductionKnownToPlayer(reveal.Ours, StationRows.MaterialKey(ToArray(materials)))) continue;

                var ids = new Il2CppInts();

                ids.Add(id);
                tool.AddProductionRecords(ids);
                _log($"{reveal.Kind.Name} recipe listed again at the workbench for its new materials");
            }
        }
        catch (Exception ex)
        {
            _reveals.Clear();
            _log($"cannot unlock the stations' recipes, so only the game's unlocks apply: {ex}");
        }
    }

    private void Step(string what, Action step)
    {
        try
        {
            step();
        }
        catch (Exception ex)
        {
            _log($"the stations' {what} could not be changed: {ex}");
        }
    }

    private int? RecipeUsing(Il2CppDictionary.Dictionary<int, Config_ProductionList> recipes, IReadOnlyList<int> materials) => RecipeUsing(recipes, materials, recipe => _ours.Contains(recipe.Pointer));

    /// <summary>The id of a recipe other than ours made of these materials in any order, or null.</summary>
    internal static int? RecipeUsing(Il2CppDictionary.Dictionary<int, Config_ProductionList> recipes, IReadOnlyList<int> materials, Func<Config_ProductionList, bool> isOurs)
    {
        var key = StationRows.MaterialKey(materials);

        foreach (var pair in recipes)
        {
            var recipe = pair.Value;

            if (recipe == null || isOurs(recipe) || recipe.MaterialList == null || recipe.MaterialList.Count != materials.Count) continue;

            if (StationRows.MaterialKey(ToArray(recipe.MaterialList)) == key) return pair.Key;
        }

        return null;
    }

    private static void SetTexts(IEnumerable<Text> texts, Il2CppDictionary.Dictionary<string, string> english, Il2CppDictionary.Dictionary<string, string> chinese)
    {
        foreach (var text in texts)
        {
            english[text.Key] = text.En;
            chinese[text.Key] = text.Zh;
        }
    }

    private static string MaterialText(IEnumerable<int> materials) => string.Join(", ", materials.GroupBy(id => id).Select(group => $"{group.Key}:{group.Count()}"));

    private static (Table Table, int Id)[] RowsOf(RowIds ids) => new[]
    {
        (Table.Item, ids.Item),
        (Table.Furniture, ids.Furniture),
        (Table.Plant, ids.Plant),
        (Table.Recipe, ids.Recipe),
        (Table.Action, ids.Action),
        (Table.Effect, ids.Effect),
    };

    private static int[] ToArray(Il2CppInts? list)
    {
        if (list == null) return Array.Empty<int>();

        var values = new int[list.Count];

        for (var i = 0; i < values.Length; i++) values[i] = list[i];

        return values;
    }

    private sealed record LargeLook(string ResName, string Icon, string WebIcon);

    private sealed record Reveal(StationKind Kind, Config_ProductionList Ours);

    /// <summary>The tables the mod writes, read from the game once a check.</summary>
    private sealed class Tables
    {
        public Tables(ConfigManager config)
        {
            Items = config._Config_Item_Dict;
            Furniture = config._Config_Furniture_Dict;
            Plants = config._Config_FurniturePlant_Dict;
            Recipes = config._Config_ProductionList_Dict;
            Actions = config._Config_Action_Dict;
            Effects = config._Config_Effect_Dict;
            Shops = config._Config_ShopFurniture_Dict;
        }

        public Il2CppDictionary.Dictionary<int, Config_Item>? Items { get; }
        public Il2CppDictionary.Dictionary<int, Config_Furniture>? Furniture { get; }
        public Il2CppDictionary.Dictionary<int, Config_FurniturePlant>? Plants { get; }
        public Il2CppDictionary.Dictionary<int, Config_ProductionList>? Recipes { get; }
        public Il2CppDictionary.Dictionary<int, Config_Action>? Actions { get; }
        public Il2CppDictionary.Dictionary<int, Config_Effect>? Effects { get; }
        public Il2CppDictionary.Dictionary<int, Config_ShopFurniture>? Shops { get; }

        /// <summary>Whether all six row tables are loaded. The others are optional: without one, its step is skipped.</summary>
        public bool HasRows => IsLoaded(Items) && IsLoaded(Furniture) && IsLoaded(Plants) && IsLoaded(Recipes) && IsLoaded(Actions) && IsLoaded(Effects);

        public void Pointers(IntPtr[] into, Il2CppObjectBase english, Il2CppObjectBase chinese)
        {
            into[0] = Pointer(Items);
            into[1] = Pointer(Furniture);
            into[2] = Pointer(Plants);
            into[3] = Pointer(Recipes);
            into[4] = Pointer(Actions);
            into[5] = Pointer(Effects);
            into[6] = Pointer(Shops);
            into[7] = english.Pointer;
            into[8] = chinese.Pointer;
        }

        private static bool IsLoaded<T>(Il2CppDictionary.Dictionary<int, T>? table) => table != null && table.Count > 0;

        private static IntPtr Pointer(Il2CppObjectBase? table) => table?.Pointer ?? IntPtr.Zero;
    }

    private interface ITableView
    {
        Slot SlotAt(int id, HashSet<IntPtr> ours);

        /// <summary>Carries the row at <paramref name="id"/> into <paramref name="next"/> when it was one of ours.</summary>
        void Keep(int id, HashSet<IntPtr> before, HashSet<IntPtr> next);

        /// <summary>Writes the row, over our own row at its id when there is one, and returns the fields that could not be set.</summary>
        List<string> Write(RowPlan plan, HashSet<IntPtr> ours);
    }

    private sealed class TableView<T> : ITableView where T : Il2CppObjectBase
    {
        private readonly Il2CppDictionary.Dictionary<int, T> _table;
        private readonly Func<T> _make;

        public TableView(Il2CppDictionary.Dictionary<int, T> table, Func<T> make)
        {
            _table = table;
            _make = make;
        }

        public Slot SlotAt(int id, HashSet<IntPtr> ours)
        {
            if (!_table.ContainsKey(id)) return Slot.Empty;

            return ours.Contains(_table[id].Pointer) ? Slot.Ours : Slot.Stock;
        }

        public void Keep(int id, HashSet<IntPtr> before, HashSet<IntPtr> next)
        {
            if (_table.ContainsKey(id) && _table[id] is { } row && before.Contains(row.Pointer)) next.Add(row.Pointer);
        }

        public List<string> Write(RowPlan plan, HashSet<IntPtr> ours)
        {
            // Writing over our own row keeps every reference the game already holds to it up to date.
            var row = SlotAt(plan.Id, ours) == Slot.Ours ? _table[plan.Id] : _make();

            RowFields.CopyAll(_table[plan.CopyFrom], row);

            var refused = plan.Fields.Select(field => RowFields.Set(row, field.Name, field.Value)).OfType<string>().ToList();

            _table[plan.Id] = row;
            ours.Add(row.Pointer);

            return refused;
        }
    }
}
