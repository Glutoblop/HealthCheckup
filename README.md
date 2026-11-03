# Health Check Up

![version](https://img.shields.io/badge/version-0.1.0-blue) ![valheim](https://img.shields.io/badge/Valheim-1.0.21-orange) ![client](https://img.shields.io/badge/client--side-only-green)

A BepInEx plugin for Valheim.

Never guess when your next health tick is coming. Health Check Up adds a small radial cooldown icon next to the health bar that fills up as the next food regen tick approaches, and draws a translucent "ghost" segment on the health bar showing exactly how much that tick will heal.

## Features
- Radial cooldown ring beside the health bar, with an item icon in the middle (Minor Healing Mead by default, configurable).
- Ghost preview on the health bar of the upcoming heal, with an optional `+N` label, fading in and pulsing in the last 2 seconds.
- Hides (or dims) itself when a tick wouldn't heal you: full health, no regen food, or a debuff zeroing regen.
- Optional seconds-remaining text.
- Read-only: it only reads the local player's regen timer. No RPCs, no world data, no gameplay changes.

## Configuration (`BepInEx/config/jordan.valheim.healthcheckup.cfg`)
| Section | Key | Default | What it does |
|---|---|---|---|
| General | Enabled | true | Show the icon. |
| General | WhenInactive | Hide | Hide / Dim / Show when no tick would heal you. |
| General | DimAlpha | 0.3 | Opacity in Dim mode. |
| General | ShowSecondsText | false | Seconds-to-next-tick text under the icon. |
| Layout | OffsetX / OffsetY / Size | 8 / 0 / 32 | Position and size in HUD pixels. |
| Appearance | IconItem | MeadHealthMinor | Item prefab whose icon sits in the ring (empty = none). |
| Appearance | RingColor / RingBackgroundColor | red / translucent black | Ring colours. |
| Preview | EnablePreview | true | Ghost heal segment on the health bar. |
| Preview | PreviewColor / PreviewPulse | light green / true | Ghost colour and pulse. |
| Preview | ShowAmountText / AmountTextSize | true / 14 | `+N` label. |

## Install

### Easiest: Valheim Mod Installer
Download the [**Valheim Mod Installer**](https://github.com/Glutoblop/ValheimPlugins) and run it (`Start-Installer.bat` on Windows, `start-installer.sh` on Linux/macOS), point it at your Valheim folder, tick **Health Check Up** and click **Install / Update**. It installs BepInEx for you if needed.

### Manual
Download from the [latest release](https://github.com/Glutoblop/HealthCheckup/releases/latest) ([`HealthCheckUp.zip`](https://github.com/Glutoblop/HealthCheckup/releases/latest/download/HealthCheckUp.zip): unzip into your Valheim folder), or:

1. Install [BepInExPack_Valheim](https://thunderstore.io/c/valheim/p/denikson/BepInExPack_Valheim/) (5.4.2360 or newer) into your Valheim folder.
2. Download [`dist/HealthCheckUp.dll`](https://github.com/Glutoblop/HealthCheckup/raw/main/dist/HealthCheckUp.dll).
3. Copy it to `<Valheim>/BepInEx/plugins/HealthCheckUp/HealthCheckUp.dll`.
4. Launch the game once; settings are written to `BepInEx/config/jordan.valheim.healthcheckup.cfg`.

## Compatibility

| | |
|---|---|
| Plugin version | **0.1.0** |
| Last tested on Valheim | **1.0.21** (network version 41, Steam build 26249040) on 2026-11-03 |
| BepInExPack_Valheim | 5.4.2360 |
| Multiplayer | Client-side only: works on vanilla servers, other players don't need it |

Machine-readable versions live in [`plugin.json`](plugin.json). The installer reads it straight from `main` to tell users when an update is available, or when the mod hasn't been tested against their game version.

## Building

Requires the .NET SDK (8+) and a Valheim install with BepInEx.

```
dotnet build -c Release -p:GameDir="C:\Path\To\Valheim"
```

The build copies the DLL into `<GameDir>/BepInEx/plugins/HealthCheckUp` for testing, and into `dist/` (which is committed, and is what the installer downloads).

## Releasing an update

1. Bump `Version` in `src/Plugin.cs` **and** `version` in `plugin.json` (keep them identical).
2. If you tested on a newer Valheim, update the `valheim` block in `plugin.json` (version and network version are printed in `BepInEx/LogOutput.log`; the Steam build id is in `steamapps/appmanifest_892970.acf`).
3. Add a line to `changelog` in `plugin.json`.
4. `dotnet build -c Release`, then commit `dist/HealthCheckUp.dll` + `plugin.json` and push to `main`. Installer users see the update next time they open it.
