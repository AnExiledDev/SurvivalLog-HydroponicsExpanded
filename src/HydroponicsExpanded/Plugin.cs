using System.Linq;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;
using HydroponicsExpanded.Adapters;
using HydroponicsExpanded.Core;

namespace HydroponicsExpanded;

[BepInPlugin(Guid, "Hydroponics Expanded", Version)]
[BepInDependency(OptionalLive.ToolkitGuid, BepInDependency.DependencyFlags.SoftDependency)]
public sealed class Plugin : BasePlugin
{
    private const string Guid = "com.dev.survivallog.hydroponicsexpanded";
    private const string Version = "0.1.2";

    public override void Load()
    {
        if (!OptionalLive.IsToolkitInstalled) Log.LogInfo(OptionalLive.MissingNote);

        _uninstallPrep = Config.Bind("Uninstall", "UninstallPrep", false,
            "Turn this on before you remove Hydroponics Expanded. Once a save is loaded, every Small or Medium Hydroponic Station Package becomes the Plant Grow Box Package of its size, in your bags, in storage and on the floor, in every place the save holds, and the shops stop selling the stations. Placed and packed stations are always saved as grow boxes, so they need nothing. Then save the game, quit, and remove HydroponicsExpanded.dll. LogOutput.log lists what was changed. Turning this off again does not turn the packages back.\n" +
            "移除水培扩展模组之前请先打开此项。读取存档后，背包、储物和地上的每个小型或中型水培站包裹都会变成同尺寸的植物灯培育箱包裹，存档里的每个地点都一样，商店也不再出售水培站。已安装和已打包的水培站本来就按植物灯培育箱保存，不受影响。然后存档、退出游戏，再删除 HydroponicsExpanded.dll。改了哪些东西写在 LogOutput.log 里。再关掉此项，包裹也不会变回来。");

        var newLook = Config.Bind("LargeStation", "NewLook", true,
            "Gives the game's Hydroponic Station (Large) the same look as the Small and Medium ones. Turn it off for the game's own look. A change shows on stations placed or loaded afterwards: load the save again to see it on the ones already standing.\n" +
            "让游戏自带的全光谱水培站（大）换上和小型、中型水培站一样的模型。关掉则用游戏原本的样子。改动对之后放置或读取的水培站生效：重新读取存档即可看到已摆放的水培站的变化。");

        BindSettings(StationRows.Small, "Plant Grow Box (Small)", "植物灯培育箱（小）", 25, 1);
        BindSettings(StationRows.Medium, "Plant Grow Box (Medium)", "植物灯培育箱（中）", 50, 3);
        OptionalLive.Watch(newLook, isOn => CurrentSettings.NewLook = isOn);
        OptionalLive.Watch(_uninstallPrep, isOn =>
        {
            UninstallPrep.Set(isOn);
            if (isOn) Log.LogInfo("Uninstall prep is on: load a save to turn the station packages into grow box packages");
        });

        var tables = new StationTables(message => Log.LogInfo(message));
        var harmony = new Harmony(Guid);
        var iconPlaced = false;

        SavePatches.Apply(harmony, message => Log.LogWarning(message));
        ShopStock.Apply(harmony, message => Log.LogWarning(message));
        StationModel.Apply(harmony, message => Log.LogInfo(message));

        StationTicker.Log = message => Log.LogError(message);
        UninstallPrep.Log = message => Log.LogInfo(message);
        StationTicker.Check = now =>
        {
            if (!iconPlaced)
            {
                iconPlaced = true;
                Log.LogInfo(StationIcon.Place());
            }

            tables.Tick(now);
            UninstallPrep.Tick(now);
        };
        AddComponent<StationTicker>();
        Log.LogInfo($"Hydroponics Expanded {Version} loaded");
    }

    /// <summary>One section of settings per station. Every one applies live: the station's rows are rewritten when one changes.</summary>
    private void BindSettings(StationKind kind, string growBox, string growBoxChinese, int growBoxPrice, int growBoxPower)
    {
        var section = kind.Size + "Station";
        var name = kind.Name;
        var chinese = kind.Texts.First(text => text.Key == kind.Key("Name")).Zh;
        var defaults = kind.Defaults;

        var power = Config.Bind(section, "PowerCost", defaults.PowerCost, new ConfigDescription(
            $"Power the {name} draws while it runs. The Hydroponic Station (Large) draws 6 and the {growBox} {growBoxPower}.\n" +
            $"{chinese}运行时的耗电量。全光谱水培站（大）耗电 6，{growBoxChinese}耗电 {growBoxPower}。",
            new AcceptableValueRange<int>(0, 50)));
        var growth = Config.Bind(section, "GrowthBoost", defaults.GrowthBoost, new ConfigDescription(
            $"How much faster plants grow in the {name} while it has power: 0.5 is half as fast again, the same as the Hydroponic Station (Large). The {growBox} gives 0.3.\n" +
            $"{chinese}通电时植物生长加快多少：0.5 表示快一半，和全光谱水培站（大）一样。{growBoxChinese}是 0.3。",
            new AcceptableValueRange<float>(0f, 5f)));
        var materials = Config.Bind(section, "Materials", defaults.Materials, new ConfigDescription(
            $"What crafting a {name} Package takes, written as item id:count pairs split by commas. 20102 is Container, 20104 Wire, 20210 Electronic Components, 20361 Iron Rod and 20362 Iron Parts. Up to {MaterialList.MostKinds} different items. A list the game cannot use (an unknown item, or the same materials as another recipe) falls back to the default, and LogOutput.log says why.\n" +
            $"制作{chinese}包裹所需的材料，写成「物品id:数量」，用逗号隔开。20102 是容器，20104 是铁丝，20210 是电器元件，20361 是铁棍，20362 是铁制零件。最多 {MaterialList.MostKinds} 种物品。游戏用不了的清单（物品不存在，或和别的配方材料相同）会改用默认值，原因写在 LogOutput.log 里。",
            null, "ModManager.Control=ItemCounts", $"ModManager.MaxKinds={MaterialList.MostKinds}", $"ModManager.MaxCount={MaterialList.MostOfOne}", $"ModManager.Makes={kind.Ids.Item}"));
        var price = Config.Bind(section, "ShopPrice", defaults.ShopPrice, new ConfigDescription(
            $"What the flowerpot shops ask for a {name} before the disaster. The {growBox} costs {growBoxPrice} there and the Hydroponic Station (Large) 150.\n" +
            $"灾难前花盆商店卖{chinese}的价格。{growBoxChinese}在那里卖 {growBoxPrice}，全光谱水培站（大）卖 150。",
            new AcceptableValueRange<int>(0, 9999)));
        var stock = Config.Bind(section, "ShopStock", defaults.ShopStock, new ConfigDescription(
            $"How many {name}s each flowerpot shop that sells the {growBox} has for sale before the disaster. 0 takes them off the shelves. A shop that already has more than this is brought down to it; stations already bought stay yours.\n" +
            $"灾难前每家出售{growBoxChinese}的花盆商店有几台{chinese}出售。0 表示不卖。已有更多存货时会减到这个数；已经买下的不受影响。",
            new AcceptableValueRange<int>(0, 20)));

        // Uninstall prep holds the stock at 0, so the shops stop listing the station.
        OptionalLive.Watch(() => CurrentSettings.Set(kind, new StationSettings(power.Value, growth.Value, materials.Value, price.Value, _uninstallPrep!.Value ? 0 : stock.Value)), power, growth, materials, price, stock, _uninstallPrep!);
    }

    private ConfigEntry<bool>? _uninstallPrep;
}
