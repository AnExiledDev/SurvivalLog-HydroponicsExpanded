# Hydroponics Expanded

Adds a Small and a Medium Hydroponic Station to Survival Log, and gives the game's Hydroponic Station (Large) a new look to match them.

- **Hydroponic Station (Small)**: one small plant. It fits the Plant Grow Box (Small)'s spot.
- **Hydroponic Station (Medium)**: two small plants or one medium plant. It fits the Plant Grow Box (Medium)'s spot.
- **Hydroponic Station (Large)**: the game's own, four small plants, two medium or one large, with the new model. One setting gives it its old look back.

The stations work like the Large one: while powered, their full-spectrum grow lights make plants grow half as fast again, warm them, and keep pests, weeds and dryness away, so crops need no care until harvest. Without power the grow lights go out and the help stops. Each station has its own model and icon, and can be crafted in any of the game's 8 colours or dyed later like the game's own furniture: the orange frame takes the colour, and the tubs, steel and water stay as they are.

Each station's pots change with what is planted: a pot in every bay for small plants, one big pot over two bays for a medium plant, and one over all four for a large plant such as corn or watermelon, so the crop grows in the middle of its pot instead of over the bay walls. The grow lights hang high enough for the tallest crop each station takes, so the Large stands about 3.2 m tall.

## Getting them

- Planting level unlocks the recipes: the Small at level 3, the Medium at 4 and the Hydroponic Station (Large) at 5. Getting the Large's recipe, by its level or by the game's Hydroponic Cultivation talent, unlocks the Small and Medium too. A run already past a level gets the recipe when the save loads. Each level is a setting.
- The recipes are at the workbench next to the Large's.
- The Small's package takes 1 Container, 1 Wire, 1 Electronic Components and 1 Iron Rod. The Medium's takes 1 Container, 1 Wire, 2 Electronic Components and 1 Iron Rod.
- Before the disaster, the flowerpot shops that sell the grow box of the same size sell 2 of each station: the Small for 60 (Flowerpots, Small Flowerpots, Electronic Flowerpots) and the Medium for 100 (Flowerpots, Medium Flowerpots, Electronic Flowerpots).
- Taking a station down gives back about half of what its package took.
- **Test buttons**: with Cheat Menu installed, its Mod tests tab shows the stations' rows and looks, or unlocks their recipes right away.

## Power

The Hydroponic Station (Large) draws 6 for four plants. The Small draws 2 and the Medium 3 by default, and every station's power, growth boost, materials, shop price and shop stock are settings (see Settings).

This is an early test build.

## Settings

Settings live in `BepInEx\config\com.dev.survivallog.hydroponicsexpanded.cfg`, created the first time the game starts with the mod. Change them in game on the Mod Manager's Mods screen, or edit the file while the game is closed. See [CONFIG.md](CONFIG.md) for every setting. Without Exiled's Toolkit installed, changes made on the Mods screen take effect at the next game start instead.

## Save safety

The game saves each station as the grow box of its size, and the mod turns it back into a station when the save loads. Without the mod, your stations become grow boxes and the save still loads. The Large keeps being the game's own station, so it never needs anything.

## Uninstall

A station package in a bag or in storage would be lost without the mod, so do this first:

1. Turn on `UninstallPrep` (on the Mods screen, or in the config file while the game is closed).
2. Load your save. Every station package, wherever it is, becomes the grow box package of its size, and the shops stop selling stations. `BepInEx\LogOutput.log` says what was changed.
3. Save the game and quit.

Then delete `HydroponicsExpanded.dll` from the `BepInEx\plugins` folder. For a clean slate, also delete `com.dev.survivallog.hydroponicsexpanded.cfg` from `BepInEx\config`. Do this for each save you want to keep.

## Requirements

- Survival Log 1.1.20719.
- BepInEx for Survival Log.
- Optional: [Exiled's Toolkit](https://github.com/AnExiledDev/SurvivalLog-ExiledToolkit) 0.4.0 or newer. With it, settings changed on the Mod Manager's Mods screen take effect without restarting the game. Without it, they take effect at the next game start.
- Optional: [Mod Manager](https://www.nexusmods.com/survivallog/mods/73). With it, you can change every setting in game on its Mods screen and see what each one does, without opening the config file.

## Install

Copy `HydroponicsExpanded.dll` into `BepInEx/plugins`.

## Configuration

See [CONFIG.md](CONFIG.md).

## Feedback and bug reports

Found a bug, have an idea, or something feels off? Come say so on the Core Directive Discord: https://discord.gg/trTsyxUFqg . When reporting a bug, attach `BepInEx\LogOutput.log`.

To reach me directly, message `exiled.dev` on Discord. You can find me in the [official Survival Log Discord](https://discord.gg/MZGg4gQAUe). I'm also on X as [@AnExiledDev](https://x.com/AnExiledDev).

## License

MIT
