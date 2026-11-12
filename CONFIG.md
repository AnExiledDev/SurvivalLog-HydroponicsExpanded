# Hydroponics Expanded settings

Settings live in `BepInEx\config\com.dev.survivallog.hydroponicsexpanded.cfg`, created the first time the game starts with the mod. Change them in game on the Mod Manager's Mods screen, where they take effect at once, or edit the file while the game is closed. Delete the file to get the defaults back.

## SmallStation and MediumStation

Each station has its own section, `[SmallStation]` and `[MediumStation]`, with the same seven settings. On the Mod Manager's Mods screen (0.14.0 or newer), `Materials` and `Returns` show as item cards you pick from every item in the game, and the screen says when the station cannot use a list.

| Setting | Small default | Medium default | What it does |
| --- | --- | --- | --- |
| `PowerCost` | `2` | `3` | Power the station draws while it runs. The Hydroponic Station (Large) draws 6, the Plant Grow Box (Small) 1 and the Plant Grow Box (Medium) 3. Any value from `0` to `50`. |
| `GrowthBoost` | `0.5` | `0.5` | How much faster plants grow in the station while it has power: `0.5` is 50% faster, the same as the Hydroponic Station (Large). The grow boxes give `0.3`. Any value from `0` to `5`. |
| `Materials` | `20102:1, 20104:1, 20210:1, 20361:1` | `20102:1, 20104:1, 20210:2, 20361:1` | What crafting the station's package takes, written as item id:count pairs split by commas. 20102 is Container, 20104 Wire, 20210 Electronic Components, 20361 Iron Rod and 20362 Iron Parts. Up to 7 different items and up to 99 of each. A list the game cannot use (one it cannot read, an item that does not exist, a dye, or the same materials as another recipe, the other station's included) falls back to the default, with a line in `BepInEx\LogOutput.log` saying why. Taking the station down gives back a share of these (see `Returns` and `ReturnPercent`). |
| `Returns` | blank | blank | What the station gives back when you take it down, written like `Materials`. Left blank, it gives back the share of its materials set by `ReturnPercent`. `none` gives nothing back. A list gives back exactly what it names, and it can name any item in the game. Up to 7 different items and up to 99 of each. A list the game cannot use (one it cannot read, or an item that does not exist) gives back the `ReturnPercent` share instead, with a line in `BepInEx\LogOutput.log` saying why. |
| `ShopPrice` | `60` | `100` | What the flowerpot shops ask for the station before the disaster. The Plant Grow Box (Small) costs 25 there, the Plant Grow Box (Medium) 50 and the Hydroponic Station (Large) 150. Any value from `0` to `9999`. |
| `ShopStock` | `2` | `2` | How many of the station each flowerpot shop has for sale before the disaster. These are the shops that sell the grow box of the same size. `0` takes them off the shelves. If a shop already has more than this, its stock drops to this number. Stations you already bought stay yours. Any value from `0` to `20`. |
| `UnlockLevel` | `3` | `4` | The Planting level that unlocks the station's recipe. Getting the Hydroponic Station (Large)'s recipe, by its level or by the Hydroponic Cultivation talent, also unlocks this one, whatever your Planting level. At `0`, only the Large's recipe unlocks it. A save already at this level gets the recipe when it loads. Any value from `0` to `5`. |

## LargeStation

| Setting | Default | What it does |
| --- | --- | --- |
| `NewLook` | `true` | Gives the game's Hydroponic Station (Large) the new model and icon that match the Small and Medium. Turn it off to keep its old look. A change shows on stations you place or load after it, so load the save again to see it on stations already placed. |
| `UnlockLevel` | `0` | The Planting level that unlocks the Hydroponic Station (Large)'s recipe, which also unlocks the Small and Medium ones. At `0`, the default, only the game's Hydroponic Cultivation talent unlocks it. A save already at this level gets the recipe when it loads. Any value from `0` to `5`. |

## Dismantle

| Setting | Default | What it does |
| --- | --- | --- |
| `ReturnPercent` | `50` | How much of its materials a station gives back when you take it down, as a percent of its recipe. `50` is about half, like other furniture in the game. At `50`, the Hydroponic Station (Medium) gives back 1 Container and 1 Electronic Components. `0` gives nothing back and `100` gives back everything. The total is rounded down, but it is always at least one item unless this is `0`. A station whose `Returns` list is filled in gives back that list instead. Any value from `0` to `100`. |

## Uninstall

| Setting | Default | What it does |
| --- | --- | --- |
| `UninstallPrep` | `false` | Turn this on before you remove the mod, then load your save. Every station package, in bags, in storage, on the floor and anywhere else in the save, becomes the grow box package of the same size, and the shops stop selling stations. Placed and packed stations are always saved as grow boxes, so they are already safe. The Small and Medium recipes are locked again. Then save the game, quit, and remove `HydroponicsExpanded.dll`. `BepInEx\LogOutput.log` says what was changed. Turning it off again does not change the packages back. |
