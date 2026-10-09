# Bumper Royale

Unity 6 game project using URP. The project is at an early stage: apart from the Unity MCP plugin, it holds only the URP template (sample scene, render pipeline settings, input actions). There are no game scripts yet.

## Tech stack

- **Unity**: 6000.1.5f1 (Unity 6.1)
- **Render pipeline**: Universal Render Pipeline (URP) 17.1.0
  - Active pipeline asset: `Assets/Settings/PC_RPAsset.asset` (renderer: `PC_Renderer.asset`)
  - Mobile variants: `Mobile_RPAsset.asset` / `Mobile_Renderer.asset`
  - Global volume: `DefaultVolumeProfile.asset`, plus `UniversalRenderPipelineGlobalSettings.asset`
  - Linear color space
  - Use URP-compatible shaders and materials (e.g. `Universal Render Pipeline/Lit`), not Built-in RP shaders.
- **Input**: Input System package 1.14.0, with **Active Input Handling set to the new Input System only**
  - The legacy `UnityEngine.Input` API (`Input.GetKey`, `Input.GetAxis`, etc.) is disabled and throws at runtime. Use `UnityEngine.InputSystem` instead.
  - Project-wide actions: `Assets/InputSystem_Actions.inputactions`
    - `Player` map: Move, Look, Attack, Interact, Crouch, Jump, Previous, Next, Sprint
    - `UI` map: Navigate, Submit, Cancel, Point, Click, RightClick, MiddleClick, ScrollWheel, TrackedDevicePosition, TrackedDeviceOrientation
    - Control schemes: Keyboard&Mouse, Gamepad, Touch, Joystick, XR
- **Scripting**: C#, API compatibility .NET Standard 2.1
- **Key packages** (`Packages/manifest.json`):
  - `com.unity.render-pipelines.universal` 17.1.0
  - `com.unity.inputsystem` 1.14.0
  - `com.unity.ai.navigation` 2.0.16 (NavMesh)
  - `com.unity.ugui` 2.0.0 (UI)
  - `com.unity.timeline` 1.8.7
  - `com.unity.test-framework` 1.5.1
  - `com.unity.visualscripting` 1.9.7
  - `com.unity.multiplayer.center` 1.0.0
  - `com.ivanmurzak.unity.mcp` 0.93.2 (Unity MCP plugin, installed from the OpenUPM scoped registry)
  - IDE integrations: Rider, Visual Studio

## Project layout

- `Assets/Scenes/SampleScene.unity` is the only scene and the only entry in Build Settings.
- `Assets/Settings/` holds URP pipeline, renderer and volume assets.
- `Assets/Scripts/<Feature>/` is where game code goes (see rules below).
- `Assets/InputSystem_Actions.inputactions` holds the project-wide input actions.
- `Assets/Plugins/NuGet/` holds NuGet dependencies of the Unity MCP plugin. Do not modify them.
- `Assets/TutorialInfo/` is URP template readme content and can be removed.

## Rules

1. **Scripts go in `Assets/Scripts`, organized by feature.** Each feature gets its own subfolder, such as `Assets/Scripts/Player/`, `Assets/Scripts/Arena/` or `Assets/Scripts/UI/`. Don't put scripts in the `Assets/` root or group them by type (e.g. a single `Managers/` folder).
2. **Never hand-edit `.unity`, `.prefab`, `.asset` or `.meta` files.** Use the Unity MCP tools (`ai-game-developer` server) instead:
   - Scenes: `scene-open`, `scene-get-data`, `scene-save`, `scene-create`
   - GameObjects and components: `gameobject-find`, `gameobject-create`, `gameobject-modify`, `gameobject-component-add`, `gameobject-component-modify`
   - Prefabs: `assets-prefab-create`, `assets-prefab-open`, `assets-prefab-save`, `assets-prefab-close`
   - Assets and materials: `assets-find`, `assets-get-data`, `assets-modify`, `assets-material-create`, `assets-move`, `assets-delete`
   
   Unity generates and maintains `.meta` files. Never create, edit or delete them by hand.
3. **After code changes, check the Unity console for compile errors and fix them before finishing.** Run `assets-refresh` (or use `script-update-or-create`, which triggers compilation), then call `console-get-logs` and filter for errors. Fix every compile error you introduced and recheck until the console is clean. A task is not done while the project fails to compile.
