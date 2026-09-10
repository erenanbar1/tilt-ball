# Tilt Ball — Level Design: Obstacles, Boosters & Progression

This document covers the obstacle/booster set added on top of the original lose-hole levels, the nine new
levels built from them (Classic 11–18 and Tall 02), and the difficulty curve they form. Where it helps, numbers
are quoted in world units — the board is 7.4 units wide (ball x ∈ [-3.4, 3.4]), the stick climbs from y = -4.47
to 6.03, the ball is 0.44 wide, and the winning hole sits at (0, 4.0).

## 1. The design space

Everything in this game happens along one line: the ball is a **1-DOF body on the stick** (it can only roll
left/right along it) and the stick only ever **rises, falls, and tilts**. The original obstacle, the lose hole,
is a *place on the board* — it punishes being at the wrong x when the stick passes a given y.

The new set widens that space along three axes the holes never touched:

| Axis | What the player has to manage | Obstacles / boosters on it |
|---|---|---|
| **Momentum** — the ball's speed along the stick is no longer only the player's | pre-empt / counter a push | Bumper, Wind, Magnet, Ice |
| **Time** — the board changes while you climb | wait, or commit at the right moment | Laser Gate, gusting Wind, drifting hole (Oscillator) |
| **Resources** — one-off help that changes the odds | route through, or around, the pickup | Jet Boost, Shield |

All of them talk to the ball through one tiny API added to `BallOnPlatformController` (`AddImpulse`,
`AddAcceleration`, `RegisterSurface`, `SignedOffsetAlong`), so the 1-DOF simulation stays the only thing that
moves the ball — nothing here re-introduces Unity's rigidbody solver.

## 2. Obstacles

### 2.1 Bumper — `BumperObstacle` · `Prefabs/Obstacles/Bumper`
An orange pinball bumper. On contact the ball is thrown along the stick **away from the bumper's centre** at
`kickSpeed` (4–5 u/s, roughly a full stick-length before damping eats it). Which side of the bumper the ball is on
decides the direction, so a bumper in the middle of a route can't be rolled through — only around.
*Reads as:* "don't touch". *Difficulty knobs:* `kickSpeed`, placement relative to holes (a bumper that throws you
*into* a hole is a trap; one that throws you *away* is a shove).

### 2.2 Wind Zone — `WindZone` · `Prefabs/Obstacles/WindZone`
A tinted band with a fan at the upwind end and chevrons showing the direction. While the ball is inside it is
pushed at `strength` u/s² (5–7). Standing still needs a ~10–15° counter-tilt; letting go carries the ball to the
end stop in about a second. With `period` > 0 the fan **gusts** — on for `dutyCycle` of each period, idle the rest
(rotor stops, chevrons dim), which turns a steering problem into a timing one. Size and direction are set on the
component (`size`, `direction`); the band, fan and chevrons re-lay themselves out.

### 2.3 Magnet — `MagnetObstacle` · `Prefabs/Obstacles/Magnet`
A red horseshoe with a pulsing ring showing its reach. Inside the ring the ball is dragged along the stick toward
the magnet (strongest at the centre, `minFalloff` of it at the rim). A magnet on its own only nudges; **the danger
is always the hole placed between the ball's route and the magnet**. Because the pull is continuous, it also makes
holding still on the far side of it a skill check. Reach = the CircleCollider2D radius (2.0–2.2 used here).

### 2.4 Laser Gate — `LaserGate` + `LaserBeam` · `Prefabs/Obstacles/LaserGate`
Two emitters with a beam between them that cycles `onDuration` / `offDuration`. Touching a **live** beam destroys
the ball (flash-and-burst, `BallHazard.Zap`); an idle beam is drawn faint so its span is always readable. The beam
**flickers for `warmUp` seconds** before going live, so a ball halfway through has a beat to commit or back off.
`phase` offsets the cycle so two gates alternate. `width` sizes the whole thing. Unlike every other obstacle, a
laser is *nothing* when timed right — it's the purest timing gate in the set.

### 2.5 Ice Patch — `IcePatch` · `Prefabs/Obstacles/IcePatch`
A pale sheet. While the ball is on it its roll acceleration is ×1.5 and its damping half-life ×6 (0.5 s → 3 s):
the ball picks up speed faster and keeps it, so the tiniest tilt sends it skating to the end stop. Ice never
kills by itself — it makes **whatever is on or just past it** hard to avoid. The sheet is drawn behind holes so
the traps inside stay readable.

### Modifier: Oscillator
`Oscillator` slides any object ±`travel` on a sine with `period`. Attached to a lose hole it makes a **drifting
hole** — the only way a hole becomes a timing obstacle. Used in Level 17 and Tall 02; works on bumpers and magnets
too.

## 3. Boosters

Both are pickups (`Pickup` base: idle bob, pop-and-vanish, fires once) collected by rolling the ball into them.

### 3.1 Jet Boost — `JetBoostPickup` · `Prefabs/Obstacles/JetBoost`
Overdrives the rig for 3 s: rise speed ×2 (3.57 → 7.14 u/s), fall ×1.3 so tilting stays snappy. The pulleys glow
yellow and the ball trails sparks while it's live. A second pickup **extends** the timer rather than stacking.
It's the "dash" — through a laser window, across a gusting fan before it wakes, or past a magnet before it can
grab. Misused, it's a way to fling yourself into a hole twice as fast, which is exactly why it's placed where a
careful player might not want it.

### 3.2 Shield — `ShieldPickup` · `Prefabs/Obstacles/Shield`
Wraps the ball in a bubble. The next thing that would have ended the run — a lose hole *or* a laser — pops the
bubble instead and **throws the ball back along the stick away from the threat** (the hole ignores the ball for
0.6 s so it can get out). One hit, no stacking. The shield is the safety valve for the intro levels: it lets a
new player *feel* an obstacle kill them without losing the run.

## 4. Level progression

The curve is: **one new idea per level with a soft landing (11–15)**, then **combinations (16–18)**, then the
**Tall side-quest as a victory lap through everything (Tall 02)**. Every intro level carries a booster so the
player meets the two pickups early and in a low-stakes spot.

Difficulty is rated 1–10 against the existing ten (Level 10 ≈ 5).

| # | Name | New idea | Also uses | Booster | Diff |
|---|---|---|---|---|---|
| 11 | Bounce House | Bumper | 2 holes | Shield | 4 |
| 12 | Crosswind | Wind (steady → gusting) | 2 holes | Jet | 5 |
| 13 | Attraction | Magnet | 3 holes | Shield | 5 |
| 14 | Security Gate | Laser Gate | 3 holes | Jet | 6 |
| 15 | Thin Ice | Ice | 4 holes | Jet | 6 |
| 16 | Pinball Wizard | — | Wind ×2, Bumper ×2, 3 holes | Shield | 7 |
| 17 | Magnetic Storm | Drifting hole | Magnet ×2, Laser, 3 holes | Jet | 8 |
| 18 | The Gauntlet | — | Ice, Bumper, Wind, Magnet, Laser, 3 holes | Shield + Jet | 9 |
| T2 | Ascension (Tall) | — | all of the above, 34-unit climb | Shield + Jet | 9 |

### Level 11 — Bounce House · *Bumper*
```
 y  3.4   hole(16)               ← keeps the top honest
    1.8              bumper B                hole(14)
    0.2                     shield ◎
   -1.6                bumper A
   -4.5   [ ball starts at x=-0.67 ]
```
Bumper A sits on the ball's natural line. A player who just holds both buttons gets thrown left on first contact
— harmless, nothing is there — and has learnt the rule. Bumper B on the left lane throws them back toward the
middle. The shield is in the safe pocket between them; the right-hand hole is the first thing that can actually
punish, placed a full second's climb above the bumper so the reaction window is generous.

### Level 12 — Crosswind · *Wind*
Two bands: the lower one blows **right, steadily** and is deliberately tall (2.4 units — a full-speed climb
spends 0.7 s in it, enough to be carried ~2 units); the upper blows **left, gusting** (3 s period, 55 % on).
The lower band's hole sits exactly where a ball that didn't fight the wind exits it (verified: a bot that only
holds "up" is caught at x ≈ 1.4), and the Jet Boost is on the *upwind* side as the reward for counter-steering.
The upper band's hole is in the downwind corner. Teaches: counter-tilt to hold position, then read the fan for
the timing variant.

### Level 13 — Attraction · *Magnet*
Two magnets on opposite sides, each with a wide flat hole between it and the middle of the board. The rings
show the reach, and the ball's default line (x ≈ -0.67) is *inside* the lower one — a straight climb gets
dragged into the trap. That's intentional, and it's why the Shield sits directly on the default line at the very
start: a player who does nothing collects the bubble, gets pulled into the hole, and watches the shield pop and
throw them clear. Two mechanics demonstrated in one beat, no run lost. The upper magnet mirrors the trap on the
right, on the approach to the winning hole, and there the player has to tilt away in earnest.

### Level 14 — Security Gate · *Laser Gate*
Gate 1 spans the middle (1.0 s on / 1.6 s off) with small holes plugging both sides — the beam is the intended
route, the side gaps are a skill route. Gate 2 is wider, offset left, and half a cycle out of phase, with a hole
on its open right side. The Jet Boost between them makes gate 2 a dash rather than a wait. Teaches: watch the
flicker, cross on "off".

### Level 15 — Thin Ice · *Ice*
Two sheets, each with holes *on* the sheet. The lower sheet spans nearly the full width — the ball enters it at
the very start of the climb, so the first thing the player learns is that a stick that isn't level sends the ball
skating. The Jet Boost between the sheets is the first "is it worth it?" pickup: less time on ice, but a faster
rig is harder to keep level.

### Level 16 — Pinball Wizard · *Bumper + Wind*
The lower wind pushes right into a bumper that throws the ball hard left — straight at a hole unless the player
brakes. A second bumper and a central hole break up the middle; the upper wind gusts left with a hole in the
corner. Shield on the right. This is the first level where two obstacles *chain*: the answer is to fight the wind
and never reach the bumper.

### Level 17 — Magnetic Storm · *Magnet + Laser + drifting hole*
Two magnets stacked on alternate sides, each with its trap hole; a full-width laser across the middle; a hole
that **drifts ±1.8 across the approach** to the winning hole every 3.2 s. Jet Boost low-left. Three different
timing/steering problems in one climb — the drifting hole is new here, deliberately introduced late where a
player has already seen every other piece.

### Level 18 — The Gauntlet · *everything*
Bottom to top: ice with a hole, a shield, a bumper, a gusting wind with a corner hole, a magnet with its trap,
a jet boost on the right, and a short laser gate directly under the winning hole. The gate's beam is only 2.6
wide, so it can be gone around on the left — into the magnet's reach. Difficulty 9: every mistake the previous
seven levels teach has a place here.

### Tall 02 — Ascension · *Tall side-quest*
A 34-unit climb (same as Tall 01) with the whole set laid out roughly every 3–3.5 units: ice → bumper → wind →
magnet → laser with plugged sides → jet boost → bumper pair with a hole between → gusting wind → shield → ice →
magnet → drifting hole → summit. The camera scroll means the player sees each obstacle arrive one at a time, so
it plays as a rhythm piece rather than a puzzle. Unlocks after Tall 01 through the existing Tall track.

## 5. Tuning reference

| Parameter | Value | Why |
|---|---|---|
| Bumper `kickSpeed` | 4 (intro) / 5 (mixed) | 4 carries ~3 units before damping — visible, not a full-length throw |
| Wind `strength` | 6 / 7 | terminal ~4.3–5 u/s; counter-tilt 12–15°. Band *height* matters as much: a straight climb crosses 1.2 units in 0.35 s, so intro bands are taller |
| Magnet `strength` / reach | 8–9 / 2.0–2.2 | inside reach the ball settles on the magnet in ~1 s |
| Laser on/off | 1.0/1.6 → 0.9/1.0 | a straight climb crosses the beam in ~0.2 s; off windows shrink with level |
| Ice multipliers | accel ×1.5, half-life ×6 | ball keeps ~80 % of its speed per second instead of ~25 % |
| Jet | ×2 rise, 3 s | crosses the whole board in ~1.5 s |
| Shield knock-back | 4.5 u/s, 0.6 s grace | clears a fully-swallowing hole before it can re-capture |
| Hole scales | 0.07–0.12 | the blob art varies a lot; sizes were chosen from measured collider bounds so every hole can actually contain the ball (holes 1 and 2 can't below 0.15 and aren't used) |

## 6. Files

- Scripts: `Assets/Scripts/Gameplay/Obstacles/` (BumperObstacle, WindZone, MagnetObstacle, LaserGate, LaserBeam,
  IcePatch, Oscillator, BallHazard) and `Assets/Scripts/Gameplay/Boosters/` (Pickup, JetBoostPickup,
  ShieldPickup, StickBoost, BallShield). `BallOnPlatformController` gained the external-influence API;
  `LoseTrigger` gained the shield check.
- Prefabs: `Assets/Prefabs/Obstacles/` — one per obstacle/booster, plus `BoostTrail`.
- Art: `Assets/Art/Obstacles/`, generated by **Tools ▸ Tilt Ball ▸ Generate Obstacle Art**
  (`Assets/Editor/ObstacleArtGenerator.cs`) — flat SDF-rasterised shapes in the game's palette; re-run after
  changing a colour or size and every prefab picks it up.
- Levels: `Assets/Levels/Configs/Level_11…18.asset`, `Tall_02.asset` and their `*_Obstacles.prefab`s; all
  registered in Bootstrap's `GameManager.allLevels` / `tallLevels`.

## 7. Verification

Each level was run in Play mode with a scripted "hold both buttons" bot (straight climb from the default ball
position) plus targeted scenarios per mechanic — bumper kick, shield collect → hole → pop/knock-back, wind drift,
laser zap on a live beam, ice damping (half-life 0.5 s → 3 s measured), jet boost (rise 3.57 → 7.14 for 3 s,
restored), magnet capture. All nine levels load with no console errors and end in a valid `Win`/`Lose`, and the
naive climb loses on every level except 15 and 17, where it stalls at the summit off-centre (as on Level 1 —
the last 0.7 units of steering into the winning hole are always the player's).

## 8. Ideas not built (next batch)
- **Gravity flip field** — a zone where the slope sign inverts; disorienting, best as a late one-off.
- **Portal pair** — two portals at the same height; entering one places the ball at the other's x.
- **Stick clamp** — a bar that stops one end of the stick rising past it, forcing a tilt.
- **Freeze pickup** — pauses lasers/oscillators/fans for a few seconds.
