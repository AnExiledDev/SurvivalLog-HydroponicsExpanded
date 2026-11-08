using System.Collections.Generic;
using System.ComponentModel;
using GameCore.HotUpdate;
using GameCore.HotUpdate.Battle.Logic;
using HydroponicsExpanded.Core;
using Il2CppInts = Il2CppSystem.Collections.Generic.List<int>;

namespace HydroponicsExpanded.Adapters;

/// <summary>
/// Test buttons for Cheat Menu's Mod tests tab. Cheat Menu finds this class by its name, so neither mod needs the other.
/// Each method is one button: its DisplayName is the label and its Description the help, as "English\nChinese". What
/// it returns is shown in Cheat Menu's log.
/// </summary>
public static class DevCheats
{
    private const string NotInGame = "load a save first";

    [DisplayName("Show stations\n查看水培站")]
    [Description("Shows each hydroponic station's rows as the game holds them now: power, plant count, growth boost, price, whether its recipe is unlocked, and which model the Large Station uses.\n显示游戏当前记录的每种水培站：耗电、可种植数、生长加速、价格、配方是否已解锁，以及大型水培站用的是哪个模型。")]
    public static string ShowStations()
    {
        var config = GameSingleton.Existing<ConfigManager>();
        var furniture = config?._Config_Furniture_Dict;
        var plants = config?._Config_FurniturePlant_Dict;
        var recipes = config?._Config_ProductionList_Dict;
        var tool = GameSingleton.Existing<BattleLogicWorld>()?._ToolTableManager;

        if (furniture == null || plants == null || recipes == null || tool == null) return NotInGame;

        var lines = new List<string>();

        foreach (var kind in StationRows.All)
        {
            var ids = kind.Ids;

            if (!furniture.ContainsKey(ids.Furniture) || !plants.ContainsKey(ids.Plant) || !recipes.ContainsKey(ids.Recipe))
            {
                lines.Add($"{kind.Name}: not in the game's tables yet (furniture {ids.Furniture}, plant row {ids.Plant}, recipe {ids.Recipe})");

                continue;
            }

            var row = furniture[ids.Furniture];
            var plant = plants[ids.Plant];
            var shown = tool._CraftUnlockedProductionIds_k__BackingField?.Contains(ids.Recipe) == true;

            lines.Add($"{kind.Name}: furniture {ids.Furniture}, power {row.PowerCost}, plants {plant.Capacity}, growth boost {plant.GrowthFaster}, price {row.FurniturePrice}, recipe {ids.Recipe} {(shown ? "unlocked" : "locked")} (Planting level {CurrentSettings.UnlockLevelOf(kind)})");
        }

        if (furniture.ContainsKey(StationRows.LargeFurniture))
            lines.Add($"Hydroponic Station (Large): model {furniture[StationRows.LargeFurniture].ResName} ({(CurrentSettings.NewLook ? "new look on" : "new look off")}), power {furniture[StationRows.LargeFurniture].PowerCost}");

        return string.Join("\n", lines);
    }

    [DisplayName("Unlock all recipes now\n立即解锁所有配方")]
    [Description("Unlocks both stations' recipes at the workbench now, without reaching their Planting level or getting the Hydroponic Station (Large)'s recipe first.\n不必达到种植等级，也不必先获得大型水培站的配方，立即在工作台解锁两种水培站的配方。")]
    public static string UnlockAllRecipes()
    {
        var tool = GameSingleton.Existing<BattleLogicWorld>()?._ToolTableManager;
        var recipes = GameSingleton.Existing<ConfigManager>()?._Config_ProductionList_Dict;

        if (tool == null || recipes == null) return NotInGame;

        var added = new List<string>();

        foreach (var kind in StationRows.All)
        {
            if (!recipes.ContainsKey(kind.Ids.Recipe)) return $"{kind.Name}: recipe {kind.Ids.Recipe} is not in the game's tables yet";
            if (tool._CraftUnlockedProductionIds_k__BackingField?.Contains(kind.Ids.Recipe) == true) continue;

            var ids = new Il2CppInts();

            ids.Add(kind.Ids.Recipe);
            tool.AddProductionCraftUnlocks(ids);
            added.Add(kind.Name);
        }

        return added.Count == 0 ? "both recipes were already unlocked" : $"unlocked now: {string.Join(", ", added)}";
    }
}
