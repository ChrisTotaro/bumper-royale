# Bumper Royale

Unity 6 URP game. Playable prototype: 3D bumper cars on a flat circular arena with no walls. Ram other cars off the edge; the last car remaining wins. Angled top-down camera. One human player against 5 AI bots.

**Target platform: mobile, landscape only.** Player Settings allow only Landscape Left and Right (auto-rotation). The primary controls are on-screen touch controls: a joystick at bottom-left and a BOOST button at bottom-right. Keyboard and gamepad also work, for testing in the editor. Design UI for a landscape phone (roughly 16:9 to 20:9) with large touch targets.

**Out of scope for now:** multiplayer, IAP, cosmetics, shop, progression, accounts, backend, ads, menus. Don't add these unless asked.

## Tech stack

- **Unity**: 6000.1.5f1 (Unity 6.1)
- **Render pipeline**: Universal Render Pipeline (URP) 17.1.0
  - Active pipeline asset: `Assets/Settings/PC_RPAsset.asset` (renderer: `PC_Renderer.asset`)
  - Mobile variants: `Mobile_RPAsset.asset` / `Mobile_Renderer.asset`
  - Global volume: `DefaultVolumeProfile.asset`, plus `UniversalRenderPipelineGlobalSettings.asset`
  - Linear color space
  - Use URP-compatible shaders and materials (e.g. `Universal Render Pipeline/Lit`, color via `_BaseColor`), not Built-in RP shaders.
- **Input**: Input System package 1.14.0, with **Active Input Handling set to the new Input System only**
  - The legacy `UnityEngine.Input` API (`Input.GetKey`, `Input.GetAxis`, etc.) is disabled and throws at runtime. Use `UnityEngine.InputSystem` instead.
  - UI needs `InputSystemUIInputModule` on the EventSystem (`StandaloneInputModule` throws).
  - Project-wide actions: `Assets/InputSystem_Actions.inputactions`, read via `InputSystem.actions`
    - `Player` map: Move, Look, Attack, Interact, Crouch, Jump, Previous, Next, Sprint
    - `UI` map: Navigate, Submit, Cancel, Point, Click, RightClick, MiddleClick, ScrollWheel, TrackedDevicePosition, TrackedDeviceOrientation
    - Control schemes: Keyboard&Mouse, Gamepad, Touch, Joystick, XR
- **Scripting**: C#, API compatibility .NET Standard 2.1. Unity 6 API names: `Rigidbody.linearVelocity`/`linearDamping`/`angularDamping`, `PhysicsMaterial`, `PhysicsMaterialCombine`.
- **UI**: uGUI with legacy `UnityEngine.UI.Text` and the built-in `LegacyRuntime.ttf` font. TMP Essentials are not imported.
- **Key packages** (`Packages/manifest.json`):
  - `com.unity.render-pipelines.universal` 17.1.0
  - `com.unity.inputsystem` 1.14.0
  - `com.unity.ai.navigation` 2.0.16 (NavMesh, not used yet)
  - `com.unity.ugui` 2.0.0 (UI)
  - `com.unity.timeline` 1.8.7
  - `com.unity.test-framework` 1.5.1
  - `com.unity.visualscripting` 1.9.7
  - `com.unity.multiplayer.center` 1.0.0
  - `com.ivanmurzak.unity.mcp` 0.93.2 (Unity MCP plugin, installed from the OpenUPM scoped registry)
  - IDE integrations: Rider, Visual Studio
- **Project settings changed for this game**:
  - **Run In Background is on.** Otherwise Play mode freezes whenever the editor is unfocused.
  - **Enter Play Mode Options skip the domain reload.** A domain reload disconnects the MCP plugin. Because of this, any script with static state must reset it in a `[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]` method, as `CarElimination` does.

## How to run

Open `Assets/Scenes/Arena.unity` and press Play.

| Action | Touch (mobile) | Keyboard | Gamepad |
|---|---|---|---|
| Forward / reverse | Joystick (bottom-left) up/down | W/S or Up/Down | Left stick |
| Steer | Joystick left/right | A/D or Left/Right | Left stick |
| Boost (has a cooldown) | BOOST button (bottom-right) | Space (`Player/Jump`) | South button |
| Restart | Tap Restart on the result panel | Click | |

The touch controls are Input System `OnScreenStick` and `OnScreenButton` components. They drive a virtual gamepad's `<Gamepad>/leftStick` and `<Gamepad>/buttonSouth`, which the project-wide `Player/Move` and `Player/Jump` actions already read. That's why `PlayerInputController` has no touch-specific code. In the editor you can use them with the mouse.

## Project layout

- `Assets/Scenes/Arena.unity` is the game scene and Build Settings index 0. `SampleScene.unity` is unused template content and can be removed.
- `Assets/Scripts/<Feature>/`: game code (see the architecture section below).
- `Assets/Prefabs/`: `PlayerCar.prefab` and `BotCar.prefab`. BotCar is a prefab variant of PlayerCar that swaps PlayerInputController for AIController and uses BotCarStats.
- `Assets/Data/`: `PlayerCarStats.asset`, `BotCarStats.asset` (CarStats), `CarSlippery.asset` (car PhysicsMaterial).
- `Assets/Materials/`: URP Lit materials (Arena, ArenaRim, Car_Player, Car_Bot1..5, CarBumper, CarDetail).
- `Assets/Settings/`: URP pipeline, renderer and volume assets.
- `Assets/Plugins/NuGet/`: NuGet dependencies of the Unity MCP plugin. Do not modify them.
- `Assets/TutorialInfo/`: URP template readme content. It can be removed.

## Architecture

All game code is in the `BumperRoyale.*` namespaces, which match the folders.

**Input is separate from movement.** `CarMotor` only takes inputs (`SetInput(throttle, steer)` and `InputEnabled`). `PlayerInputController` and `AIController` both drive it and `CarBoost.TryBoost()`. Controllers never touch the Rigidbody. Keep it this way: a new kind of driver should be another controller, not a change to CarMotor.

| Folder | Class | Role |
|---|---|---|
| `Car/` | `CarStats` | ScriptableObject holding every car and AI tuning value |
| `Car/` | `CarMotor` | Arcade Rigidbody handling in FixedUpdate. Driving forces apply only when a ground raycast hits, so a car off the arena tumbles freely. It also handles yaw-rate steering, lateral grip, coast drag, overspeed damping, lowered center of mass, and `Stun()` (temporary grip loss) |
| `Car/` | `CarBoost` | Forward velocity impulse queued for FixedUpdate. Exposes `IsBoosting` for its duration, a cooldown, and `CooldownProgress` for the HUD |
| `Car/` | `CarKnockback` | On a car-to-car collision, the rammer pushes the other car. The push grows with relative impact speed and with the rammer's share of the closing speed, is multiplied when the rammer is boosting, is capped, and stuns the victim so it slides. It uses the rammer's stats |
| `Car/` | `CarElimination` | Static registry of live cars (`Active`). Eliminates a car when it enters a `KillZone` or falls below a height. Raises the static `AnyEliminated` event, disables input, and destroys the car after a delay |
| `Car/` | `CarAppearance` | Renderers that show the team color. `SetColorMaterial()` is used by the spawner |
| `Player/` | `PlayerInputController` | Reads `Player/Move` and `Player/Jump` from the project-wide actions, with a keyboard fallback |
| `AI/` | `AIController` | Retargets periodically, preferring near cars and cars close to the edge (aggression makes the choice less random). It leads its aim and boosts when lined up and in range. Edge avoidance projects its velocity ahead (`aiEdgeLookahead`), then blends its heading toward the center and brakes or reverses |
| `Arena/` | `ArenaBounds` | Arena center and radius (static `Instance`), used by the AI and spawning |
| `Arena/` | `KillZone` | Marker on the trigger collider below the arena |
| `Match/` | `MatchManager` | Spawns the player and bots evenly on a circle facing the center. Runs Countdown → Playing → Ended, with a 3-2-1-GO countdown during which every motor has `InputEnabled = false`. The player being eliminated means Defeat; the player being the last car means Victory. `Restart()` reloads the scene |
| `CameraRig/` | `CameraController` | Fixed-angle camera that looks at a point lerped between the arena center and the player (`followFactor`) |
| `UI/` | `HUDController` | Polls MatchManager each frame: players remaining, countdown label, the boost cooldown shown as a radial fill on the BOOST button (`boostFill`), and a result panel with a Restart button (listener added in code) |
| `Setup/Editor/` | `BumperRoyaleSetup` | `Bumper Royale/...` menu items that create the CarStats assets and the car PhysicsMaterial and fix Build Settings. Each is safe to re-run |

Scene contents (`Arena.unity`): Main Camera (CameraController), Directional Light, Arena (ArenaBounds, with Platform using a MeshCollider and a Rim with no collider), KillZone (BoxCollider trigger 200×2×200 at y = -6), MatchManager, HUD (Canvas), and EventSystem (InputSystemUIInputModule). Cars are **not** placed in the scene. MatchManager spawns them at runtime.

HUD hierarchy: the Canvas is Screen Space Overlay, with CanvasScaler set to scale with screen size (reference 1920×1080, match 0.5).
- `RemainingText`: top-left
- `CountdownText`: center
- `TouchControls`: full screen, and must stay below `ResultPanel` in sibling order so the panel blocks touches
  - `Joystick`: base circle, 340 px, centered (300, 270) from the bottom-left
    - `Knob`: 150 px, with `OnScreenStick`. Its `raycastPadding` of -95 makes the whole base grabbable
  - `BoostButton`: 260 px, centered (-300, 270) from the bottom-right, with `OnScreenButton`
    - `BoostFill`: radial fill
    - `Label`
- `ResultPanel`: `ResultTitle`, `ResultSubtitle`, `RestartButton`

Circles use the built-in `UI/Skin/Knob.psd` sprite.

## Where tuning values live

- **CarStats assets**: `Assets/Data/PlayerCarStats.asset` (player) and `Assets/Data/BotCarStats.asset` (bots). Each field has a tooltip.
  - Driving: `maxSpeed`, `reverseMaxSpeed`, `acceleration`, `coastDrag`, `overspeedDrag`
  - Steering: `steerSpeed`, `fullSteerSpeed`, `steerAtLowSpeedFactor`, `steerResponsiveness`, `lateralGrip`
  - Physics: `centerOfMassOffset`, `groundCheckDistance`
  - Boost: `boostForce`, `boostDuration`, `boostCooldown`
  - Knockback: `minImpactSpeed`, `knockbackBase`, `knockbackPerRelativeSpeed`, `boostKnockbackMultiplier`, `maxKnockback`, `knockbackUpward`, `knockbackStunDuration`, `stunnedGripFactor`
  - AI (read from BotCarStats): `aggression`, `targetRetargetInterval`, `edgeAvoidMargin`, `edgeAvoidStrength`, `aiEdgeLookahead`, `aiBoostRange`, `aiBoostAngle`, `aiSteerAngleForFullInput`
- **MatchManager** (scene object): `totalCars`, `spawnRadius`, `spawnHeight`, `botMaterials`, `countdownStepSeconds`, `goLabelSeconds`
- **ArenaBounds** (Arena object): `radius`. Keep it in sync with the Platform scale (30 → radius 15).
- **CameraController** (Main Camera): `pitch`, `yaw`, `distance`, `followFactor`, `smoothTime`
- **CarElimination** (car prefabs): `fallbackKillHeight`, `destroyDelay`
- **Touch controls** (scene, `HUD/TouchControls`): sizes and positions on the `Joystick` and `BoostButton` RectTransforms. The stick's throw distance is `Movement Range` on the `Knob`'s OnScreenStick (110). Boost ring colors are `boostReadyColor` and `boostChargingColor` on HUDController.
- **`Assets/Data/CarSlippery.asset`**: car friction (0.05, combine Minimum) and bounciness (0.15)
- **Rigidbody on the prefabs**: mass 10, interpolation, continuous dynamic collision

When you change a tuning default, update **both** the asset values and the field initializer in `CarStats.cs` (and the bot overrides in `BumperRoyaleSetup.CreateCarStatsAssets`), so new assets match.

Tuning history: knockback stacks on top of the physics momentum transfer. Large `knockbackBase`/`PerRelativeSpeed`/`boostForce` values made boosted rams launch cars at about 40 m/s, and the arena emptied in seconds. The current values give roughly 3 eliminations in 45 s among bots.

## Rules

1. **Scripts go in `Assets/Scripts`, organized by feature.** Each feature gets its own subfolder, such as `Assets/Scripts/Car/`, `Assets/Scripts/Arena/` or `Assets/Scripts/UI/`. Don't put scripts in the `Assets/` root or group them by type (e.g. a single `Managers/` folder). Editor-only code goes in an `Editor/` subfolder of its feature (e.g. `Assets/Scripts/Setup/Editor/`).
2. **Never hand-edit `.unity`, `.prefab`, `.asset` or `.meta` files.** Use the Unity MCP tools (`ai-game-developer` server) instead:
   - Scenes: `scene-open`, `scene-get-data`, `scene-save`, `scene-create`
   - GameObjects and components: `gameobject-find`, `gameobject-create`, `gameobject-modify`, `gameobject-component-add`, `gameobject-component-modify`, `gameobject-component-destroy`
   - Prefabs: `assets-prefab-create`, `assets-prefab-open`, `assets-prefab-save`, `assets-prefab-close`, `assets-prefab-instantiate`
   - Assets and materials: `assets-find`, `assets-get-data`, `assets-modify`, `assets-material-create`, `assets-move`, `assets-delete`
   - Where MCP can't do something directly (creating ScriptableObject or PhysicsMaterial assets, Build Settings, private serialized references), add an idempotent `[MenuItem]` to `BumperRoyaleSetup` and run it, or use `script-execute` with Unity APIs (`SerializedObject`, `AssetDatabase`, `EditorSceneManager`).
   - Unity generates and maintains `.meta` files. Never create, edit or delete them by hand.
3. **After code changes, check the Unity console for compile errors and fix them before finishing.** Run `assets-refresh` (or use `script-update-or-create`, which triggers compilation), then call `console-get-logs` and filter for errors. Fix every compile error you introduced and recheck until the console is clean. A task is not done while the project fails to compile.
4. **Keep input separate from movement, and keep tuning in data.** Don't hard-code gameplay numbers in scripts. Add a field to `CarStats` (with a tooltip) or a serialized field on the owning component.
5. **Work in milestones.** After each one: confirm the console is clean, play-test, then `git commit` with a clear message.

## Testing through MCP

- Enter and exit Play mode with `script-execute`: `EditorApplication.isPlaying = true/false`.
- MCP calls return instantly, so to observe gameplay over time, register an `EditorApplication.update` callback in a `script-execute` class. Have it sample state (e.g. `CarElimination.Active` positions or `MatchManager.Instance.State`), `Debug.Log` with a unique prefix, and exit Play mode when done. Then wait in Bash and grep `%LOCALAPPDATA%/Unity/Editor/Editor.log` for that prefix.
- To screenshot the game view, render `Camera.main` into a `RenderTexture` during Play mode and write a PNG to the scratchpad. To include the HUD, temporarily switch the Canvas to `ScreenSpaceCamera` with `worldCamera = Camera.main`, then switch it back. Use a phone aspect (e.g. 1560×720) to check the mobile layout. `ScreenCapture.CaptureScreenshot` fails when the editor is in the background, and `screenshot-isolated` frames a single object, so neither is useful for the game view.
- To test the touch controls, call `OnScreenStick.OnPointerDown`/`OnDrag`/`OnPointerUp` and `OnScreenButton.OnPointerDown`/`OnPointerUp` with fake `PointerEventData`, then check how the player car moves.
- Save the scene before entering Play mode. If MCP disconnects (`Failed to invoke ... after 10 retries`), ask the user to focus Unity, leave Play mode, and reconnect the plugin.
- `AssetDatabase.CreateAsset` for a PhysicsMaterial needs a `.asset` extension. `.physicsMaterial` produces a broken DefaultAsset.
