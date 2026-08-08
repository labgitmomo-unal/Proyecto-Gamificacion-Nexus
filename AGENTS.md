# AGENTS.md — Proyecto Gamificacion Nexus

## Project
- **Engine**: Unity 6000.3.8f1 (Unity 6) / **URP** 17.3.0 / **VR**: OpenXR + XR Interaction Toolkit 3.3.1
- **Root**: `Nexus/` subdirectory (open this in Unity Hub, not the repo root)
- **Solution**: `Nexus/Nexus.slnx` (VS Code default `dotnet.defaultSolution`)
- **IDE**: VS Code with `visualstudiotoolsforunity.vstuc` extension

## Naming Conventions
| Element | Convention | Example |
|---------|-----------|---------|
| Unity objects (GameObject, prefab, folder) | PascalCase + underscore for spaces | `Objeto_Ejemplo`, `Mecanica_Salto` |
| Scripts & C# classes | English, same PascalCase + underscore | `Jump_Mechanic.cs`, `Speed_Booster.cs` |
| Methods, vars, serialized fields | camelCase | `damageAmount`, `ApplyBuff()` |
| Design principle | Single-responsibility, configurable from one place | Evitar cambios dispersos, todo parametrizable |

## Scenes (Build Settings order)
| Index | Scene | Notes |
|-------|-------|-------|
| 0 | `Auth_Final.unity` | Authentication/login scene |
| 1 | `Neon High City.unity` | Main gameplay scene |

## Core Data Flow
`Auth_Final` —(loads additively)→ `Neon High City`
`DriveDataLoader` → `ButtonSpawner` → `ProgresoAbstraccion` (gameplay data)
Auth via `AuthManager.cs` (login/register panels, TMP input, scene management)

## Custom Scripts (`Assets/Scripts/`)
| Script | Role |
|---|---|
| `DriveDataLoader.cs` | Downloads JSON from Google Drive at runtime → `persistentDataPath/variables_abstraccion.json`. Static `DataReady` flag + `OnDataLoaded` event. |
| `ButtonSpawner.cs` | Reads JSON, spawns UI buttons with ponderacion-based coloring |
| `ProgresoAbstraccion.cs` | Tracks progress (buttons with ponderacion==1 eliminated). Locks ScrollView at 100%. |
| `AuthManager.cs` | Login/register UI logic, scene transition |
| `Read_Json/Read_Json.cs` | Static helper: reads JSON via `DriveDataLoader.ReadLocalJson()`, extracts unique categories |
| `Second challenge/` (7 scripts) | Category matching challenge: `Category_Spawner`, `Category_Item_Button`, `Category_Challenger_Manager`, `Relation_Manager`, `Instantiate_Categories`, `Challenge_Progress`, `Timer_2` |
| `Traffic/TrafficManager.cs` | Singleton controlling flying traffic speed via `SetVelocidad(0–2f)` |
| `Traffic/TrafficCleanup.cs` | Cleans up far-away traffic clones using `MovementController` + `RandomObjectSpawner` |
| `RandomObjectSpawner.cs` | Spawns flying cars from the Car Line Spawner prefab (`_DLNK/`). Uses `optionalScripts` to copy MovementController values. |
| `Cinematic_1_Controller.cs` | Cinematic camera switch + traffic congestion. Uses `PlayableDirector` + Cinemachine. |
| `QuestOptimizer.cs` | Quest-specific performance tuning |
| `AdaptiveQuality.cs` | Dynamic render scale / shadow distance based on FPS |
| `ShaderPrewarm.cs` | Shader warmup on start |
| `FlyingCar.cs` | Orbital movement for decorative cars |
| `FloatAnimation.cs` | Generic floating/bobbing animation |
| `Omitir.cs` | Skip cinematic via VR controller button |
| `Timer.cs` | Countdown timer (challenge 1) |
| `VRCanvasKeyboard.cs` / `AutoKeyboardLink.cs` | VR keyboard input helper |
| `TeleportIndicatorTrigger.cs` | XR teleport zone |

## Architecture Notes
- Communication: static events (`OnDataLoaded`, `OnBotonObjetivoEliminado`) + singleton (`TrafficManager.Instance`)
- `MovementController` is defined in the third-party city asset (`Assets/_DLNK/`); `RandomObjectSpawner.cs` is a custom replacement in `Assets/Scripts/`
- Cinematic uses Unity Playables (`PlayableDirector`) + Cinemachine virtual cameras
- `Oculus` folder present in Assets (OVR integration alongside OpenXR)

## Git Branches
- `main` — stable
- `Escena0` — current working branch
- `PrimeraParte`, `Grafo`, `Integracion`, `feat-handsVR`, `feature/modulo-2-abstraccion-(Holograma3D)` — feature/milestone branches

## Gotchas
- `Assets/_DLNK/` is gitignored (large third-party city asset). Devs need this asset separately.
- JSON data file lives in `Application.persistentDataPath` (platform-specific), not in the project directory.
- `.csproj` / `.sln` files are Unity-generated; do not edit manually.
- `Library/`, `Temp/`, `Logs/`, `UserSettings/` are gitignored — normal Unity cache dirs.
- `McpUnitySettings.json` lives in `ProjectSettings/` — configures Unity MCP server.
- Bezi plugin (`com.bezi.sidekick` / `.bezisidekick/`) present for 3D design sync — do not remove.

## Config Sources
- `Nexus/opencode.json` — OpenCode config (Unity MCP at `http://127.0.0.1:8081/mcp`)
- `Nexus/.vscode/settings.json` — VS Code file associations and exclusions
- `Nexus/Packages/manifest.json` — authoritative package list
