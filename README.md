### Tilt Ball Game !

# Tilt Ball 
Tilt the platform to guide the ball to the goal. Tilt Ball is a 2D Physics based game where you tilt a platform to left and right to control the balls movement. You try to dodge hole obstacles to react to the winning hole. 

Developed with Unity 6000.3.12f1

## Adding a level

Levels are data: a `LevelConfig` asset (`Assets/Levels/Configs/`) plus an obstacle prefab
(`Assets/Levels/ObstaclePrefabs/`). One gameplay scene plays every level; the config decides
whether it is *Classic* (one screen) or *Tall* (scrolls up a climb) by its `climbHeight`.
You never edit the level list by hand — `Assets/Levels/LevelCatalog.asset` rebuilds itself
from the Configs folder, ordered by `levelIndex`.

### 1. Create it

**Tools > Tilt Ball > New Classic Level** (or **New Tall Level**). This creates `Level_NN.asset`
and an empty `Level_NN_Obstacles.prefab`, links them, appends the level to the catalogue, and
opens the `Gameplay` scene with the new level ready to edit.

### 2. Build it in context

In the `Gameplay` scene, select the `Level` object. Its **Level Preview** component shows the
level's obstacle prefab spawned under `ObstaclesRoot` as `<name> (preview)`, with the stick,
ball, pulleys and winning hole around it, and gizmos for:

- the **play-area box** (yellow) — what every device is guaranteed to show; keep pieces inside it
- for Tall levels, the **summit** line (green) and ghost circles where the pulleys and winning
  hole will sit once the climb is applied

Drag pieces from `Assets/Prefabs/` — the `LoseHole_1…20` shape variants, and
`Obstacles/` (`Bumper`, `IcePatch`, `WindZone`, `LaserGate`, `Magnet`, `JetBoost`, `Shield`,
`BoostTrail`) — as children of the preview object and position them. Ordinary prefab editing
applies: undo works, and the Overrides dropdown lists added/removed pieces.

When it looks right press **Apply to Prefab** on the Level Preview component (or **Revert** to
throw the edits away). The Inspector tells you when there are unsaved edits. The preview itself
is never saved into the scene and never exists in Play mode — the game always spawns the prefab.

To edit an existing level, select its config in the Project window and use the Inspector's
**Open in Gameplay Scene** button (or right-click → *Tilt Ball > Open Level in Gameplay Scene*).

### 3. Play it

**Play This Level** (on the Level Preview or on the config) enters Play mode straight into that
level, skipping the menus. To test the real flow instead, **Tools > Tilt Ball > Progress >
Unlock All Levels** lets you reach any level from Level Select; **Reset Progress** clears it.

### Config fields

| Field | Meaning |
|---|---|
| `levelIndex` | Position in its mode's list (0-based) — unlock order and the number on the Level Select node. Keep them contiguous; the config Inspector warns if they aren't. |
| `obstaclesPrefab` | The layout. Empty = no obstacles (Level 1). |
| `backgroundSprite` | Art behind the play area; empty keeps the scene default. |
| `climbHeight` | `0` = Classic. `> 0` = Tall: how far each end of the stick may rise; the pulleys and hole are lifted by the same amount and the camera scrolls to follow. |
| `levelFloorY`, `ceilingPadding` | Tall only: bottom of the scrollable column and headroom above the summit. |

### Reordering or removing

Change `levelIndex` values (keep 0…N−1 contiguous) or delete the config; the catalogue rebuilds
on import. If it ever looks stale, **Tools > Tilt Ball > Rebuild Level Catalog**.
