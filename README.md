# Health Check Up

![version](https://img.shields.io/badge/version-0.1.3-blue) ![valheim](https://img.shields.io/badge/Valheim-1.0.21-orange) ![client](https://img.shields.io/badge/client--side-only-green)

A BepInEx plugin for Valheim.

Health Check Up draws a translucent segment on the health bar showing how much health the next food regeneration tick will restore.

## Features
- Ghost preview on the health bar of the upcoming heal, with an optional `+N` label, fading in and pulsing in the last 2 seconds.
- Hides when the next tick won't heal you, such as at full health or with no regen food.
- Client-side only; does not change gameplay.

## Configuration (`BepInEx/config/jordan.valheim.healthcheckup.cfg`)
| Section | Key | Default | What it does |
|---|---|---|---|
| General | Enabled | true | Enable Health Check Up. |
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
| Plugin version | **0.1.3** |
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
