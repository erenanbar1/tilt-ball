# Tilt Ball — Architecture

This document describes how the shipping game is put together. The live game lives entirely under
`Assets/Scenes/end-to-end/`; `Assets/Scenes/_Archive/` and `Assets/Scenes/TiltBallScene.unity` are earlier
layouts kept for reference and are **not** part of the current architecture (see [Cleanup history](#cleanup-history)).

Engine: Unity `6000.3.12f1`, Universal Render Pipeline, new Input System, 2D physics.
Third-party: [PrimeTween](https://github.com/KyryloKuzyk/PrimeTween) (via OpenUPM) drives every animation in the game — no `Animator`/coroutine tweening is used.

## 1. Scene graph and navigation

The game is one persistent scene plus a set of additively-swapped "screen" scenes — never a single-scene reload.

```mermaid
flowchart TD
    Bootstrap["Bootstrap<br/>(loaded once, never unloaded)"] -->|BootstrapRunner.Start| MainMenu
    MainMenu -->|Play| LevelSelect
    LevelSelect -->|pick unlocked level| Gameplay
    Gameplay -->|GameManager: Win| WinScreen
    Gameplay -->|GameManager: Lose| GameOver
    WinScreen -->|Next Level| Gameplay
    GameOver -->|Retry| Gameplay
    Gameplay -.->|Pause button, additive overlay| PauseMenu
    PauseMenu -.->|Resume| Gameplay
    PauseMenu -->|Main Menu| MainMenu
    MainMenu -.-> LevelSelect
    LevelSelect -->|Back| MainMenu
```

**`SceneLoader`** (in Bootstrap) is the *only* script allowed to call `SceneManager` APIs — every button handler
routes through `SceneLoader.Instance` (or, for pause, through `GameManager`; see below) instead. Its
`SwapTo(name)` unloads whatever "main" scene is currently up and loads the new one additively, so Bootstrap (and
anything else additive) is never touched. `PauseMenu` is the one exception to the swap model: it's loaded
*additively on top of* Gameplay rather than swapped in as a main scene — which is also why the PauseMenu scene
has no `Main Camera`/`EventSystem` of its own; it relies on Gameplay's still being loaded underneath it.

Pause is driven through `GameManager`, the same way Win/Lose are: `GameplayHUD`'s pause button calls
`GameManager.SetState(GameState.Pause)`; `SceneLoader.HandleStateChanged` reacts to that (and to a transition
back to `GameState.Playing`) by loading/unloading the PauseMenu scene additively and toggling `Time.timeScale`.
`GameManager` itself never touches scenes or timescale — it only holds state and fires `OnStateChanged`;
`SceneLoader` is still the sole place scene/timescale mechanics happen.

Each of the five "main" scenes (MainMenu, LevelSelect, Gameplay, WinScreen, GameOver) is self-contained: its own
`Main Camera`, `EventSystem`, `Canvas`, and one small controller script that wires up that scene's buttons.

## 2. Bootstrap and the persistent singletons

`Bootstrap.unity` is listed first in Build Settings and holds five root objects, each a classic
`Instance`-guarded MonoBehaviour singleton (`Awake` destroys any duplicate):

| Script | Responsibility |
|---|---|
| `AudioManager` | One looping `AudioSource` for music, one one-shot source for SFX, so SFX never interrupts music. Also owns menu-click sound and win/lose ducking (below). |
| `SceneLoader` | Owns all scene transitions (§1) and the pause overlay. |
| `GameManager` | Tracks `CurrentState` (`Playing/Win/Lose/Pause`), the selected `currentLevel`/`allLevels`, and fires `OnStateChanged`. Holds no obstacle/geometry knowledge, and never touches scenes or `Time.timeScale` itself — purely run state. |
| `SaveManager` | Persists `HighestUnlockedLevelIndex` via `PlayerPrefs`; monotonically increasing. |
| `BootstrapRunner` | The handoff: in `Start()` (guaranteed to run after every other object's `Awake`) calls `SceneLoader.Instance.GoToMainMenu()`. |

`SceneLoader` subscribes to `GameManager.OnStateChanged` in its own `Start()` and reacts to `Win`/`Lose` by
swapping to WinScreen/GameOver — this is the one place gameplay outcome and scene navigation are connected.

`AudioManager` subscribes to the same event to know when a run returns to `Playing` (Retry and Next Level both
set that state before loading their next scene), which is its cue to fade the music back up. The four outcome
sounds (`winHole`, `winScreen`, `loseHole`, `loseScreen`) are triggered directly by the scripts that need them —
`WinTrigger`/`LoseTrigger` call `PlayWinHole`/`PlayLoseHole` as the ball starts falling in, `WinScreenController`/
`GameOverController` call `PlayWinScreen`/`PlayLoseScreen` from their own `Start()` — rather than through the
state-change subscription, since by the time a `Win`/`Lose` state actually lands the ball has already fallen. The
two hole sounds also duck the music (down over `duckDuration`, back up over `restoreDuration`, both driven by
PrimeTween on unscaled time so the pause menu's `Time.timeScale = 0` can't stall a fade partway); the screen
sounds land once the theme is already ducked. Every menu button in the game (Play, level buttons, pause/resume/
restart/main-menu, retry, next level, back) calls the static `AudioManager.PlayClick()` — the tilt controls,
which are held rather than clicked, deliberately don't.

A static-utility sixth piece, **`PerformanceSettings`**, runs via
`[RuntimeInitializeOnLoadMethod(BeforeSceneLoad)]` before Bootstrap even loads: it pins `targetFrameRate = 60`,
disables vSync, and matches `Time.fixedDeltaTime` to 60 Hz. This exists because iOS caps frame rate at 30fps
unless `targetFrameRate` is set explicitly.

### A load-bearing project setting

**Enter Play Mode Options are enabled with both domain reload and scene reload disabled**
(`ProjectSettings/EditorSettings.asset: m_EnterPlayModeOptions: 3`). This is why `BallOnPlatformController`
re-applies its kinematic-body setup every `FixedUpdate` (driven off the `Rigidbody2D`'s own state) rather than
once in `Awake` — as an `[ExecuteAlways]` component it's already "awake" from Edit mode, and `Awake` never fires
again when Play mode starts without a domain reload. (The former other example here, a static-field reset on the
now-removed `LevelLoader`, no longer applies — see [Cleanup history](#cleanup-history).)

## 3. Level system

`LevelConfig` (`ScriptableObject`, `Assets/Levels/Configs/`) holds `levelId`, `levelIndex`, and an
`obstaclesPrefab`. Eleven configs exist (`Level_01`…`Level_10`, `lvl11`). `ballStartPosition`,
`winningHolePosition` and `timeLimit` are placeholder fields — not read by anything yet, since ball/hole
placement and timing are currently identical across levels and stay scene-authored.

Flow: `LevelSelectController` (in the LevelSelect scene) builds one button per entry in
`GameManager.allLevels`, disabling any beyond `SaveManager.IsUnlocked(index)`. Picking one sets
`GameManager.currentLevel` and calls `SceneLoader.LoadGameplay()`. In the Gameplay scene, **`LevelController`**
reads `GameManager.currentLevel` in `Awake` and instantiates its `obstaclesPrefab` under an `ObstaclesRoot`
transform. On win, `WinScreenController` advances to `allLevels[index + 1]` (looping back to level 0 at the end)
and calls `SaveManager.UnlockLevel(nextIndex)` before loading Gameplay again.

Obstacle prefabs live in `Assets/Levels/ObstaclePrefabs/` (`Level_02_Obstacles.prefab` … `Level_10_Obstacles.prefab`,
plus one for level 11 whose asset name got mangled to `Level_!1.prefab`); Level 1 has none, matching the
`obstaclesPrefab == null` early-out in `LevelController`.

## 4. Gameplay mechanics

```mermaid
flowchart LR
    subgraph Input
        KB[Keyboard arrows]
        TB[TouchTiltButton x2]
    end
    KB --> SC[StickController]
    TB -->|SetLeftHeld/SetRightHeld| SC
    SC -->|leftY/rightY, tilt angle| Platform[Stick / Platform transform]
    Platform -->|left/right point line, slope| Ball[BallOnPlatformController]
    SC -.-> PR[PulleyRotator]
    SC -.-> SV[StringVisual]
    Ball -->|OnTriggerStay2D| WT[WinTrigger]
    Ball -->|OnTriggerStay2D| LT[LoseTrigger]
    WT -->|GameManager.SetState Win| GM[GameManager]
    LT -->|GameManager.SetState Lose| GM
```

**`StickController`** owns the tiltable platform. Each end (`leftY`/`rightY`) rises/falls independently at
`riseSpeed`/`fallSpeed` toward `held` input, clamped to `[minOffset, maxOffset]`; tilt angle is derived *linearly*
from the height difference (not `atan2`, so turn rate stays constant even near vertical) and clamped to
`maxTiltAngle`. Input is `keyboard-arrow-keys OR touch`: `leftHeld = keyLeft || touchLeftHeld`, letting keyboard
(dev/editor) and touch coexist without either overwriting the other.

**`BallOnPlatformController`** deliberately does **not** let Unity's Rigidbody2D solver handle the ball resting
on the platform — it explicitly `Physics2D.IgnoreCollision`s the ball against every platform collider and drives
the ball's position itself: a 1-DOF simulation along the `leftPoint`–`rightPoint` line
(`v += slope * rollAcceleration * platformLength * dt`, exponential damping via a half-life, end-stop bounce),
ported 1:1 from an HTML prototype the game is based on. The ball is `Rigidbody2D.Kinematic` with gravity off;
Unity physics only re-enters the picture for the trigger colliders on the win/lose holes.

**Win/Lose** are structurally identical `OnTriggerStay2D` checks that both play the same three-part PrimeTween
fall-in sequence (pull to hole center, spin, shrink) before reporting to `GameManager`, and both fire their
`AudioManager` hole sound (§2) the instant the ball is judged sufficiently contained, so the sound lands with the
fall rather than the screen transition a beat later. They differ in how "contained enough" is measured:
- `WinTrigger` (on `WinningHole.prefab`) assumes a circular hole and just compares ball-to-hole-center distance
  against the fill sprite's radius (default containment: half in).
- `LoseTrigger` (on the 20 `LoseHole_N.prefab` variants) samples points around the ball's rim against the hole's
  actual `Collider2D` shape, since these holes are irregular blobs a single radius would describe badly (default
  containment: fully in).

Both resolve their `stick`/`gameManager` references via `FindFirstObjectByType` when left unwired, so either
prefab can be dropped into a new level's obstacle prefab without hand-wiring.

**Purely cosmetic followers**, none of which write back to gameplay state: `PulleyRotator` (spins a pulley wheel
to match `StickController`'s current held/speed state), `StringVisual` (`LineRenderer` between two anchor
transforms), `HoleOutline` (traces a `LineRenderer` around a hole's own collider so its capture boundary is
visible), `BackgroundFitter` (stretches one shared background sprite to exactly fill whatever camera it's given,
so `Background.prefab` works unmodified across all 11 levels' cameras), and `MrBallIdle` (breathing/arm-sway idle
loop + procedural shadow for the mascot).

## 5. UI layer

Each screen scene has one thin controller (`MainMenuFlow`, `LevelSelectController`, `PauseController`,
`GameOverController`, `WinScreenController`, `GameplayHUD`) whose only job is wiring `Button.onClick` to
`SceneLoader`/`GameManager`/`SaveManager` calls — no scene ever calls `SceneManager` directly.

Two shared utilities back every screen:
- **`ScreenEntranceAnimator`** — a static PrimeTween helper for the drop-in-with-overshoot title animation and
  the pop-in-then-pulse button animation, used identically by `WinScreenController`, `GameOverController`, and
  `MainMenuFlow`. Its `AnimateTitle` takes an optional `idleAfter` flag that starts a gentle yoyo scale breathing
  loop once the drop-in lands; only `MainMenuFlow`'s title uses it, since that's the one title that stays on
  screen (WinScreen/GameOver reload out before an idle loop would read).
- **`SafeArea`** — adjusts a `RectTransform`'s anchors to `Screen.safeArea` every frame, so notches, the Dynamic
  Island, and iOS slide-over/split-view are handled live rather than once at launch.

Gameplay's pause button is `Assets/Prefabs/PauseButton.prefab` — a plain `Button`/`Image`, wired to
`GameplayHUD.pauseButton` in the inspector — sitting in front of a decorative `TopBar` image (`HUD_TopBar.png`)
on the Gameplay canvas. `Assets/Art/Settings_Button.png` was added alongside it but isn't placed in any scene or
referenced by any script yet — there is no settings menu.

## 6. Persistence

The only persisted state is one integer: `SaveManager.HighestUnlockedLevelIndex`, stored in `PlayerPrefs` and
only ever raised (finishing an earlier level again can't lock out later ones). There is no save data for
settings, timings, or scores.

## 7. Build & release tooling

`Assets/Editor/IOSBuildStamper.cs` is an `IPreprocessBuildWithReport`/`IPostprocessBuildWithReport` hook, active
for iOS builds only:
- **Preprocess**: increments a persistent build number (stored in `PlayerSettings.iOS.buildNumber`) and saves
  immediately, so a failed build can't hand its number to the next attempt.
- **Postprocess**: stamps `CFBundleDisplayName` in the generated Xcode project's `Info.plist` to a short
  `"tilt <version> (<build>)"` (kept short because iOS truncates home-screen labels around 12 characters), and
  appends a per-version suffix to the iOS bundle identifier (e.g. `com.DefaultCompany.tilt-ball.v1-1`) directly
  in the generated Xcode project — `PlayerSettings` itself is never rewritten, so the suffix can't stack across
  builds. This lets different app *versions* install side-by-side on one test device.

## Cleanup history

The following dead code was identified while writing this document (confirmed unused by GUID, not just by name)
and has since been removed:

- **`LevelLoader`** — duplicated `LevelController`'s obstacle-spawning job with its own, disconnected progression
  system. It was referenced only by `Assets/Scenes/TiltBallScene.unity` and `Assets/Scenes/_Archive/Level.unity`,
  both outside `Scenes/end-to-end/`. Those two archived scenes now show a missing-script warning on that
  component if opened in the editor — expected, since they're legacy layouts, not the shipping game.
- **`TouchTiltControls`** — an unreferenced alternate implementation of touch tilt input; `TouchControls.prefab`
  uses `TouchTiltButton` instead.
- **`Assets/Physics/BouncyBall.physicsMaterial2D`** — unreferenced by any prefab, scene, or asset.

`GameState.Pause` was briefly removed in this same pass (it was unused at the time) and then reinstated once
pausing was rerouted through `GameManager` — see §2.
