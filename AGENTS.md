# AGENTS.md - Proyecto Gamificacion Nexus

## Project Overview
- **Engine**: Unity 6000.3.8f1 (Unity 6)
- **Render Pipeline**: URP 17.3.0
- **Target**: VR (OpenXR + XR Interaction Toolkit 3.3.1)
- **Root**: `Nexus/` subdirectory contains the Unity project
- **Solution**: `Nexus/Nexus.slnx` (VS Code default); `Proyecto-Gamificacion-Nexus.sln` at repo root

## Key Directories
- `Nexus/Assets/Scripts/` — custom game scripts (entry point logic)
- `Nexus/Assets/Scenes/` — `Neon High City.unity` (main), `SampleScene.unity`
- `Nexus/Assets/_DLNK/` — third-party city asset pack (gitignored, do NOT commit)
- `Nexus/Assets/Samples/XR Interaction Toolkit/` — XR toolkit starter samples (do not modify directly)
- `Nexus/Assets/TextMesh Pro/` — TMP essentials
- `Nexus/Packages/manifest.json` — Unity package dependencies

## Custom Scripts (Assets/Scripts/)
| Script | Purpose |
|---|---|
| `DriveDataLoader.cs` | Downloads JSON from Google Drive at runtime, saves to `persistentDataPath/variables_abstraccion.json`. Static `DataReady` flag + events for cross-scene coordination. |
| `ButtonSpawner.cs` | Reads JSON from DriveDataLoader, dynamically spawns UI buttons with ponderacion-based coloring. |
| `ProgresoAbstraccion.cs` | Tracks progress when buttons with `ponderacion == 1` are eliminated. Locks ScrollView at 100%. |
| `TrafficManager.cs` | Singleton controlling flying traffic speed. `TrafficManager.Instance.SetVelocidad(0f-2f)`. |
| `Cinematic_1_Controller.cs` | Controls initial cinematic camera switch + traffic congestion effect. |
| `TeleportIndicatorTrigger.cs` | XR teleport trigger zone. |
| `Traffic/MovementController.cs` | Movement script for traffic car clones (referenced by TrafficManager). |

## Architecture Notes
- `DriveDataLoader` → `ButtonSpawner` → `ProgresoAbstraccion` is the core gameplay data flow
- Communication uses static events (`OnDataLoaded`, `OnBotonObjetivoEliminado`) and singleton (`TrafficManager.Instance`)
- `RandomObjectSpawner` is used for traffic car spawning (likely in XR Interaction Toolkit samples or a custom script in scenes)
- Cinematic uses Unity Playables (`PlayableDirector`) + Cinemachine virtual cameras

## Commands & Workflow
- **Open project**: Open `Nexus/` folder in Unity Hub (not the repo root)
- **IDE**: VS Code with `visualstudiotoolsforunity.vstuc` extension recommended
- **No build/test/lint CI** — this is a Unity editor-driven project; verification is done in-editor
- **Unity MCP**: configured at `http://localhost:8080/mcp` via `opencode.json` (for AI-assisted Unity editing)

## Git Branches
- `main` — stable
- `Escena0` — current working branch (checked out)
- `PrimeraParte` — completed milestone
- `Rendimiento-Optimizacion` — performance work

## Gotchas
- `Assets/_DLNK/` is gitignored (large third-party city asset). Other devs need this asset separately to open the scene fully.
- `.csproj` files are Unity-generated; do not edit manually.
- `Library/`, `Temp/`, `Logs/`, `UserSettings/` are gitignored — normal Unity cache dirs.
- JSON data file lives in `Application.persistentDataPath` (platform-specific), not in project directory.
- Bezi plugin (`com.bezi.sidekick`) is present for 3D design sync — do not remove unless intentional.

## Existing Config Sources
- `Nexus/opencode.json` — OpenCode config with Unity MCP server
- `Nexus/.vscode/settings.json` — VS Code file associations and exclusions
- `Nexus/Packages/manifest.json` — authoritative package list
