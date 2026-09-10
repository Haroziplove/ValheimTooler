<h1 align="center">
  <br>
  <img src="./docs/images/valheimtooler_logo.png" alt="Valheim Tooler" width="300">
</h1>

<p align="center">
  <img src="https://img.shields.io/badge/version-1.12.0-blue" alt="Version 1.12.0">
  <img src="https://img.shields.io/badge/Valheim-1.0.7-green" alt="Valheim 1.0.7">
  <a href="https://github.com/Astropilot/ValheimTooler/blob/main/LICENSE">
    <img src="https://img.shields.io/github/license/Astropilot/ValheimTooler" alt="MIT License">
  </a>
</p>

<p align="center">
  <a href="#about">About</a> •
  <a href="#this-fork">This fork</a> •
  <a href="#install">Install</a> •
  <a href="#usage">Usage</a> •
  <a href="#contributing">Contributing</a> •
  <a href="#credits">Credits</a>
</p>

## About

ValheimTooler is a free tool with a large set of cheats and admin helpers for Valheim. It is for educational and private use. Some features will wreck a normal playthrough, especially on multiplayer servers. Use them with care.

This repository is a **community fork** of [Astropilot/ValheimTooler](https://github.com/Astropilot/ValheimTooler) by Yohann MARTIN (Astropilot). The original project last targeted Valheim 0.219.x and is not maintained for Valheim 1.0.

**This fork is 1.12.0** and is updated for **Valheim 1.0.7** (Deep North).

Features:

* **Player**
  * God Mode, you don't lose any more life
  * Unlimited stamina for you
  * Unlimited stamina for the other players
  * No stamina for the other players
  * Fly Mode, sweet creative mode 🕊️
  * Ghost Mode, the monsters cannot see you
  * No placement cost
  * Explore all your minimap (Irreversible on the world on which it is activated!)
  * Reset your minimap
  * Teleport a player to another player (works only if the player is visible on the minimap), to minimap marker or coordinates
  * Allow teleporting with restricted items
  * Instantly heals a player
  * Instantly heals all players
  * Activate a Guardian Power for you (including Kall and any extra `GP_*` powers)
  * Activate a Guardian Power for all players
  * Raise/Decrease a skill to any level
  * Tame all nearby creatures
  * Unlimited weight for inventory
  * Fast crafting
  * Remove your tombstones
* **Entities & Items**
  * Spawn any entity
  * Delete all drops on the ground
  * An Item Giver to add any item you want in your inventory
* **Terrain Shaper**
  * Level terrain
  * Lower terrain
  * Raise terrain
  * Reset modifications
  * Smooth terrain
* **Miscellaneous**
  * Inflict damage to a player (ignores the no-pvp mode)
  * Kill all entities except players
  * Kill all players
  * Auto-pin on the minimap the deposits
  * Send a event message to all players (the yellow one on the middle of the screen)
  * Send a chat message as any username
  * A ESP for players, monsters, pickables, deposits and drops with radius setting

**Warning**: On each feature that allows you to choose a player, the list will only include players who are at a certain distance from you. That is a game limitation, not a fork choice.

## This fork

| | Original | This fork |
| --- | --- | --- |
| Author | [Astropilot](https://github.com/Astropilot) (Yohann MARTIN) | [Haroziplove](https://github.com/Haroziplove) |
| Last official release | 1.11.x for Valheim 0.219.14 | **1.12.0 for Valheim 1.0.7** |
| Source | [Astropilot/ValheimTooler](https://github.com/Astropilot/ValheimTooler) | [Haroziplove/ValheimTooler](https://github.com/Haroziplove/ValheimTooler) |

1.12.0 changes for Valheim 1.0:

* Harmony no longer crashes when `InventoryGrid.OnRightClick` is missing (1.0 uses `OnLeftDown` / `OnRightDown`)
* Inventory, chat, pins, stamina, and terrain APIs updated for 1.0
* Item Giver keeps items whose preview icons fail (gray placeholder)
* Kall and other `GP_*` guardian powers are picked up from the game data

## Install

**Prerequisite:** [BepInEx for Valheim](https://valheim.thunderstore.io/package/denikson/BepInExPack_Valheim/) must already be installed. This plugin will not load without it. Thunderstore / r2modman is the easiest way.

Prebuilt 1.12.0 files are in [`Built DLLs`](./Built%20DLLs):

* `ValheimTooler.dll`
* `ValheimToolerMod.dll`
* `SharpConfig.dll`

1. Install BepInEx for Valheim first.
2. Copy all three files from [`Built DLLs`](./Built%20DLLs) into `BepInEx/plugins/ValheimTooler/`.
   * Steam install: `Valheim/BepInEx/plugins/ValheimTooler/`
   * Thunderstore / r2modman: your profile's `BepInEx/plugins/ValheimTooler/`
3. Start Valheim **modded**.
4. Wait until the **main menu**, then press **Del** to show or hide the window.

To build from source instead, compile `ValheimTooler` and `ValheimToolerMod` as **Release | x64** against your local Valheim install, then copy the same three DLLs into that plugin folder. Point the Valheim assembly references in the `.csproj` files at your own game install before building.

Astropilot's installer/launcher still lives in this repo. For an old pre-1.0 build you can use [the original releases](https://github.com/Astropilot/ValheimTooler/releases). Those builds do **not** work on Valheim 1.0.

If an install breaks the game, use Steam's "Verify integrity of game files".

## Usage

A config file is created on first launch:

* Launcher: next to `ValheimToolerLauncher.exe`
* BepInEx: `BepInEx/config/valheimtooler_settings.cfg`

You can change the toggle key (default Delete), whether the window starts visible, language, and feature shortcuts.

## Contributing

This fork is open for issues and pull requests on [Haroziplove/ValheimTooler](https://github.com/Haroziplove/ValheimTooler). Please follow the editorconfig rules.

## Credits

**Original project:** [ValheimTooler](https://github.com/Astropilot/ValheimTooler) by **Yohann MARTIN ([Astropilot](https://github.com/Astropilot))**, released under the MIT License. This fork keeps that license and exists only to keep the tool working on current Valheim.

Credits from the original README:

* [Guided Hacking](https://guidedhacking.com/threads/how-to-hack-unity-games-using-mono-injection-tutorial.11674/) and [Unknown Cheats](https://www.unknowncheats.me/forum/unity/285864-beginners-guide-hacking-unity-games.html)
* [wh0am15533](https://github.com/wh0am15533) — Unity Runtime DevTools and [SharpMonoInjector](https://github.com/wh0am15533/SharpMonoInjector)
* [KillerGoldFisch](https://github.com/KillerGoldFisch) — BepInEx loader approach
* [themaoci](https://github.com/themaoci) — Harmony patches, auto-pin, instant craft, restricted teleport
* [Gungnir](https://github.com/zambony/Gungnir) — Terrain Shaper
* [BepInEx](https://github.com/BepInEx/BepInEx) — configuration system
* [BastienMarais](https://github.com/BastienMarais) — testing and feature ideas
