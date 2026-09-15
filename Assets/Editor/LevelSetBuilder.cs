using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// The 20-level campaign as data. Each level is a list of placements against the
// same geometry LevelController uses at runtime: the ball rides the stick from
// y ≈ -5.3 up to the summit (4.99 + the level's extra length), reaching
// x ∈ [-3.4, 3.4], and the winning hole sits one unit under the summit.
//
// Tools > Tilt Ball > Build Level Set rebuilds Level_01..Level_20 — layout
// prefab plus config, same names LevelDesignTools exports under, so any level
// can still be opened with Edit Selected Level afterwards — validates that no
// losing hole overlaps anything else, then points GameManager.allLevels in
// Bootstrap at the set. A level that fails validation is reported and skipped;
// nothing of it is written.
public static class LevelSetBuilder
{
    const string LayoutFolder = "Assets/Levels/ObstaclePrefabs";
    const string ConfigsFolder = "Assets/Levels/Configs";
    const string BootstrapScene = "Assets/Scenes/end-to-end/Bootstrap.unity";
    const string ProfilePath = "Assets/Config/ScreenFit/GameplayScreenFit.asset";

    // From the Gameplay scene's rig: the stick starts at y -5.51 with the ball
    // 0.18 above it, climbs 10.5 on a Classic level, and the ball can reach
    // 3.43 either side of centre. The hole sits at 4 on a Classic level.
    const float ClassicHoleY = 4f;
    const float ClassicSummitBallY = 5.17f;
    const float StartBallY = -5.33f;
    const float ReachX = 3.43f;
    const float PlayHalfWidth = 3.9375f;
    const float Margin = 0.05f;

    class Item
    {
        public string prefab;
        public float x, y, scale = 1f, rot;
        public Action<GameObject> setup;
        public Vector2 travel;      // Oscillator half-amplitude; zero = static
        public float period, phase;
        public bool IsHole => prefab.StartsWith("LoseHole_");
        public bool Moves => travel != Vector2.zero;
    }

    class Level
    {
        public string name;
        public float length;
        public float holeX;
        public bool tutorial;
        public Item[] items;
    }

    // ---- placement vocabulary ------------------------------------------------

    static Item H(int shape, float x, float y, float s = 1.4f, float rot = 0f)
        => new Item { prefab = "LoseHole_" + shape, x = x, y = y, scale = s, rot = rot };

    static Item Bumper(float x, float y, float kick = 5f)
        => new Item { prefab = "Bumper", x = x, y = y, setup = go => { go.GetComponent<BumperObstacle>().kickSpeed = kick; } };

    static Item Wind(float x, float y, float w, int dir, float strength = 5f, float period = 0f, float duty = 0.5f, float phase = 0f, float h = 1.2f)
        => new Item { prefab = "WindZone", x = x, y = y, setup = go =>
        {
            var z = go.GetComponent<WindZone>();
            z.size = new Vector2(w, h); z.direction = dir; z.strength = strength;
            z.period = period; z.dutyCycle = duty; z.phase = phase;
            z.Layout();
        } };

    static Item Ice(float x, float y, float w, float h = 1.4f)
        => new Item { prefab = "IcePatch", x = x, y = y, setup = go => { var p = go.GetComponent<IcePatch>(); p.size = new Vector2(w, h); p.Layout(); } };

    static Item Magnet(float x, float y, float s = 1.3f, float strength = 7f)
        => new Item { prefab = "Magnet", x = x, y = y, scale = s, setup = go => { go.GetComponent<MagnetObstacle>().strength = strength; } };

    static Item Laser(float x, float y, float width, float on = 1f, float off = 1.4f, float phase = 0f)
        => new Item { prefab = "LaserGate", x = x, y = y, setup = go =>
        {
            var g = go.GetComponent<LaserGate>();
            g.width = width; g.onDuration = on; g.offDuration = off; g.phase = phase;
            g.Layout();
        } };

    static Item Jet(float x, float y) => new Item { prefab = "JetBoost", x = x, y = y };
    static Item Shield(float x, float y) => new Item { prefab = "Shield", x = x, y = y };

    static Item Osc(this Item it, float tx, float period, float phase = 0f, float ty = 0f)
    {
        it.travel = new Vector2(tx, ty); it.period = period; it.phase = phase;
        return it;
    }

    static Level Lv(string name, float length, float holeX, params Item[] items)
        => new Level { name = name, length = length, holeX = holeX, items = items };

    // ---- the campaign --------------------------------------------------------
    //
    // Heights climb steadily; each new obstacle gets a level to itself before it
    // is mixed in. Every band leaves a gap of at least ~1.2 units for the ball
    // (0.44 wide) and hazards never sit within the first unit above the floor.

    static Level[] Levels() => new[]
    {
        // 1 — the tutorial: nothing to avoid, one hole off to the side so the
        // bar has to be tilted to reach it.
        new Level { name = "Level_01", length = 0f, holeX = 1.6f, tutorial = true, items = new Item[0] },

        // 2 — one hole straight above the start: sidestep it.
        Lv("Level_02", 0f, 0f,
            H(20, -0.7f, -1.2f, 1.7f),
            H(6, 2.2f, 1.6f, 1.4f)),

        // 3 — a zigzag: left, right, back to centre.
        Lv("Level_03", 0f, 0f,
            H(10, 1.4f, -2.0f, 1.4f),
            H(11, -1.6f, 0.6f, 1.4f),
            H(2, 1.6f, 2.9f, 1.3f)),

        // 4 — first climb past one screen; a slalom of five.
        Lv("Level_04", 19f, 0f,
            H(20, -0.7f, -2.4f, 1.6f),
            H(14, 2.0f, -0.2f, 1.5f),
            H(7, -2.2f, 1.8f, 1.4f),
            H(10, 0.9f, 2.0f, 1.3f),
            H(3, -1.0f, 4.4f, 1.3f)),

        // 5 — bumpers: one dead centre with the trap only on the right, and a
        // shield off the beaten path to introduce boosters.
        Lv("Level_05", 21f, 0f,
            Bumper(0f, -1.6f, 4f),
            H(8, 2.6f, -1.6f, 1.3f),
            H(17, -1.8f, 1.4f, 1.3f),
            Shield(-2.6f, 3.4f),
            H(6, 0.8f, 3.6f, 1.4f),
            H(20, 2.0f, 6.0f, 1.4f)),

        // 6 — wind: a fan pushes right, a hole waits just past it on the right.
        Lv("Level_06", 23f, 0f,
            H(14, -1.0f, -2.6f, 1.5f),
            Wind(0f, 0f, 7.2f, +1, 5f),
            H(11, 2.6f, 1.8f, 1.4f),
            H(2, -1.8f, 4.6f, 1.4f),
            Jet(2.8f, 5.0f),
            H(20, 1.2f, 7.2f, 1.4f)),

        // 7 — ice: skate through, then thread two holes right after it.
        Lv("Level_07", 25f, 0f,
            H(10, 1.6f, -2.4f, 1.4f),
            Ice(0f, 0.8f, 7.2f),
            H(7, -2.2f, 3.0f, 1.4f),
            H(3, 1.0f, 3.0f, 1.3f),
            H(6, -0.6f, 6.4f, 1.4f),
            H(15, 2.2f, 8.6f, 1.3f),
            H(20, -2.2f, 9.6f, 1.3f)),

        // 8 — magnet: dragged left on the way up, a hole waits in the well.
        Lv("Level_08", 27f, 0f,
            H(8, 1.8f, -2.6f, 1.4f),
            Magnet(-2.8f, 1.0f),
            Jet(2.9f, 3.0f),
            H(1, -2.2f, 4.4f, 1.4f),
            H(12, 1.4f, 5.6f, 1.3f),
            H(7, -1.2f, 8.4f, 1.4f),
            H(10, 1.8f, 10.6f, 1.3f),
            H(20, -2.0f, 11.8f, 1.3f)),

        // 9 — laser: the bypass on the right is plugged, so it's a timing gate.
        Lv("Level_09", 29f, 0f,
            H(20, -1.6f, -2.4f, 1.4f),
            Laser(-0.6f, 1.5f, 5.2f, 1.0f, 1.6f),
            H(8, 3.15f, 1.6f, 1.2f),
            H(17, 0.6f, 4.6f, 1.3f),
            Shield(2.6f, 6.0f),
            H(2, -2.2f, 6.6f, 1.4f),
            H(10, 1.4f, 9.4f, 1.3f),
            H(14, -1.4f, 11.2f, 1.4f),
            H(6, 1.4f, 13.4f, 1.3f)),

        // 10 — a hole that sweeps, a gusting fan, a bumper with a trap downwind.
        Lv("Level_10", 31f, 0f,
            H(13, -2.4f, -2.6f, 1.5f),
            H(20, 0f, 0.8f, 1.5f).Osc(2.0f, 3.2f),
            Wind(0f, 4.4f, 7.2f, -1, 5f, 3f, 0.55f),
            H(11, -2.6f, 6.2f, 1.3f),
            Bumper(1.4f, 9.0f, 4f),
            H(3, -1.6f, 9.0f, 1.3f),
            Jet(-2.8f, 11.0f),
            H(15, 2.2f, 12.4f, 1.3f),
            H(7, -0.8f, 14.8f, 1.4f)),

        // 11 — pinball: three bumpers, each with its landing spot mined.
        Lv("Level_11", 33f, 0.8f,
            Bumper(-1.4f, -1.8f),
            H(8, 2.6f, -1.8f, 1.3f),
            Bumper(1.6f, 1.4f),
            H(20, -2.6f, 1.4f, 1.3f),
            Bumper(-0.4f, 4.6f),
            H(10, 1.8f, 6.6f, 1.3f),
            H(14, -2.2f, 7.0f, 1.3f),
            Shield(2.8f, 8.2f),
            Ice(0f, 9.6f, 7.2f),
            H(6, 0.4f, 12.0f, 1.4f),
            H(3, -2.4f, 12.2f, 1.2f),
            H(2, 2.4f, 14.6f, 1.3f),
            H(11, -0.6f, 17.2f, 1.3f)),

        // 12 — deep freeze: three sheets of ice, holes right where you'd slide.
        Lv("Level_12", 35f, 0f,
            Ice(0f, -1.6f, 7.2f),
            H(10, 2.2f, 0.6f, 1.4f),
            H(14, -1.6f, 0.8f, 1.4f),
            Ice(0f, 4.0f, 7.2f),
            H(7, -0.8f, 6.2f, 1.4f),
            H(20, 2.4f, 6.4f, 1.3f),
            Jet(-2.8f, 8.4f),
            Magnet(2.9f, 9.6f, 1.2f),
            H(1, -1.2f, 12.2f, 1.4f),
            H(8, 2.6f, 12.6f, 1.3f),
            Ice(0f, 15.0f, 7.2f),
            H(6, 1.2f, 17.2f, 1.4f),
            H(3, -2.2f, 17.4f, 1.3f),
            H(11, -1.8f, 19.6f, 1.3f)),

        // 13 — twin magnets on opposite walls with a laser between them.
        Lv("Level_13", 37f, 0f,
            H(20, 0.6f, -2.6f, 1.4f),
            Magnet(-2.8f, 0.6f),
            H(17, -2.4f, 4.0f, 1.3f),
            Laser(0.8f, 6.4f, 4.6f, 1.1f, 1.3f),
            H(8, -2.9f, 6.6f, 1.2f),
            Shield(-2.8f, 9.4f),
            Magnet(2.8f, 9.8f),
            H(14, -1.0f, 13.0f, 1.4f),
            H(2, 2.2f, 13.2f, 1.3f),
            Bumper(0f, 16.2f),
            H(10, -2.2f, 18.8f, 1.3f),
            H(6, 2.2f, 19.0f, 1.3f),
            H(15, 1.4f, 21.6f, 1.3f)),

        // 14 — laser stack: three gates, bypasses plugged, phases alternating.
        Lv("Level_14", 39f, -0.6f,
            H(3, -1.6f, -2.4f, 1.4f),
            Laser(0.6f, 1.2f, 4.8f, 1.0f, 1.4f, 0f),
            H(20, -3.0f, 1.2f, 1.2f),
            Laser(-0.6f, 4.6f, 4.8f, 1.0f, 1.4f, 0.5f),
            H(8, 3.0f, 4.7f, 1.2f),
            Shield(0f, 7.6f),
            Wind(0f, 10.0f, 7.2f, +1, 5f, 2.8f, 0.5f),
            H(11, 2.6f, 12.0f, 1.3f),
            H(14, -1.4f, 12.4f, 1.3f),
            Laser(0f, 15.6f, 3.6f, 0.9f, 1.2f),
            H(1, -2.9f, 15.6f, 1.2f),
            H(13, 3.1f, 15.7f, 1.3f),
            Jet(-2.6f, 18.6f),
            H(10, 1.4f, 20.4f, 1.3f),
            H(7, -1.6f, 22.6f, 1.3f),
            H(6, 1.8f, 24.2f, 1.3f)),

        // 15 — gale: fans in both directions, a sweeping hole, a magnet wall.
        Lv("Level_15", 41f, -0.8f,
            Wind(0f, -2.0f, 7.2f, -1, 5f, 2.6f, 0.5f, 0f),
            H(10, -2.6f, -0.2f, 1.3f),
            Wind(0f, 2.2f, 7.2f, +1, 5.5f, 2.6f, 0.5f, 0.5f),
            H(11, 2.6f, 4.0f, 1.3f),
            H(20, 0f, 7.4f, 1.4f).Osc(2.2f, 3.0f),
            Magnet(-2.8f, 11.0f, 1.2f),
            H(2, 1.6f, 14.4f, 1.3f),
            H(14, -2.2f, 14.6f, 1.4f),
            Shield(2.8f, 16.0f),
            Wind(0f, 17.6f, 7.2f, -1, 6f),
            H(6, -2.4f, 19.6f, 1.3f),
            Bumper(1.2f, 21.8f),
            H(8, -1.9f, 21.9f, 1.2f),
            H(17, 1.0f, 25.0f, 1.3f)),

        // 16 — pinball wizard: a bumper on a rail, a gate with no way round.
        Lv("Level_16", 44f, 1.4f,
            Bumper(-1.2f, -1.4f),
            H(8, 2.7f, -1.4f, 1.3f),
            Bumper(1.4f, 1.2f),
            H(20, -2.7f, 1.2f, 1.3f),
            Bumper(0f, 5.0f).Osc(2.0f, 2.6f),
            H(14, -2.9f, 7.6f, 1.3f),
            H(3, 2.6f, 7.8f, 1.3f),
            Ice(0f, 10.8f, 7.2f),
            H(10, 0.6f, 13.0f, 1.4f),
            H(1, -2.6f, 13.2f, 1.4f),
            Laser(0f, 16.4f, 5.0f, 1.0f, 1.2f),
            Jet(0f, 19.0f),
            Bumper(-1.6f, 21.6f),
            Bumper(1.6f, 21.6f),
            H(7, -2.6f, 24.0f, 1.3f),
            H(2, 2.4f, 24.2f, 1.3f),
            H(11, 0.2f, 26.8f, 1.3f),
            H(13, -2.4f, 28.6f, 1.3f)),

        // 17 — deep freeze II: ice, magnets and a moving hole between lasers.
        Lv("Level_17", 47f, -0.4f,
            Ice(0f, -2.2f, 7.2f),
            H(10, -1.8f, 0.2f, 1.4f),
            H(14, 1.6f, 0.4f, 1.4f),
            Magnet(2.9f, 3.8f, 1.2f),
            H(8, 2.4f, 7.0f, 1.3f),
            Laser(-0.8f, 9.4f, 4.8f, 1.0f, 1.1f),
            H(20, 2.9f, 9.5f, 1.2f),
            Ice(0f, 12.6f, 7.2f),
            H(6, -0.4f, 14.8f, 1.4f),
            H(3, 2.4f, 15.0f, 1.3f),
            Shield(-2.8f, 17.4f),
            H(11, 0f, 19.6f, 1.3f).Osc(2.0f, 2.8f),
            Wind(0f, 22.8f, 7.2f, +1, 6f),
            H(17, 2.7f, 24.8f, 1.3f),
            Magnet(-2.9f, 26.6f, 1.2f),
            H(2, -2.0f, 30.0f, 1.3f),
            H(10, 1.6f, 30.4f, 1.3f)),

        // 18 — storm: gusts, two gates, holes that won't hold still.
        Lv("Level_18", 50f, 0.6f,
            Wind(0f, -2.4f, 7.2f, +1, 6f, 2.4f, 0.5f),
            H(11, 2.6f, -0.6f, 1.3f),
            H(20, -0.4f, 2.4f, 1.4f).Osc(1.8f, 2.6f),
            Laser(0.4f, 5.6f, 4.6f, 0.9f, 1.1f),
            H(8, -3.1f, 5.7f, 1.2f),
            Wind(0f, 8.8f, 7.2f, -1, 6f, 2.4f, 0.5f, 0.5f),
            H(14, -2.6f, 10.8f, 1.3f),
            H(3, 0.8f, 11.0f, 1.3f),
            Jet(2.8f, 13.2f),
            Bumper(-0.6f, 15.6f),
            H(10, 2.4f, 15.6f, 1.3f),
            Laser(-0.4f, 19.0f, 4.6f, 0.9f, 1.1f, 0.5f),
            H(13, 3.1f, 19.1f, 1.3f),
            Shield(0f, 22.0f),
            H(6, 0f, 24.8f, 1.3f).Osc(1.6f, 2.2f),
            Wind(0f, 27.6f, 7.2f, +1, 7f),
            H(2, 2.4f, 29.6f, 1.3f),
            H(17, -1.2f, 31.6f, 1.3f),
            H(7, 1.6f, 33.6f, 1.3f),
            H(20, -2.2f, 34.8f, 1.2f)),

        // 19 — the gauntlet: everything so far, one after another.
        Lv("Level_19", 53f, 1.2f,
            H(10, -1.6f, -2.6f, 1.4f),
            Bumper(1.6f, -0.4f),
            H(8, -2.8f, -0.4f, 1.2f),
            Ice(0f, 2.6f, 7.2f),
            H(14, -0.6f, 4.8f, 1.4f),
            H(20, 2.2f, 4.9f, 1.3f),
            Magnet(-2.9f, 8.2f, 1.2f),
            H(3, -2.9f, 12.1f, 1.2f),
            Laser(0.6f, 12.2f, 4.0f, 0.9f, 1.0f),
            Bumper(0f, 15.4f).Osc(2.2f, 2.4f),
            H(1, -2.8f, 17.8f, 1.3f),
            H(15, 2.4f, 18.0f, 1.3f),
            Wind(0f, 20.8f, 7.2f, -1, 6f, 2.2f, 0.55f),
            Shield(-2.8f, 21.9f),
            H(11, 0.4f, 23.6f, 1.3f).Osc(2.0f, 2.6f),
            Laser(-0.6f, 26.8f, 4.6f, 0.8f, 1.0f, 0.3f),
            H(13, 2.9f, 26.9f, 1.3f),
            Ice(0f, 29.6f, 7.2f),
            H(6, -1.0f, 31.8f, 1.4f),
            H(2, 2.2f, 32.0f, 1.3f),
            Jet(0f, 34.4f),
            H(7, -2.2f, 36.4f, 1.3f),
            H(10, 1.6f, 36.6f, 1.3f),
            H(20, -0.2f, 38.6f, 1.3f)),

        // 20 — the summit: a gate that sweeps, magnets on both walls, ice
        // right under the hole.
        Lv("Level_20", 56f, 0.6f,
            H(20, 0f, -2.2f, 1.4f).Osc(2.0f, 2.4f),
            Bumper(-1.8f, 1.0f),
            Bumper(1.8f, 1.0f),
            Laser(0f, 4.2f, 5.6f, 0.8f, 1.0f),
            Ice(0f, 7.2f, 7.2f),
            H(10, -2.2f, 9.4f, 1.3f),
            H(14, 0.4f, 9.6f, 1.4f),
            H(3, 2.8f, 9.5f, 1.2f),
            Shield(-2.6f, 12.4f),
            Magnet(2.9f, 13.0f),
            Laser(0f, 17.4f, 3.4f, 0.9f, 0.9f).Osc(1.6f, 3.0f),
            H(1, -2.6f, 20.2f, 1.3f),
            H(15, 2.4f, 20.4f, 1.3f),
            Wind(0f, 23.4f, 7.2f, +1, 7f, 2.0f, 0.6f),
            H(11, 2.7f, 25.4f, 1.3f),
            H(6, -0.6f, 28.0f, 1.3f).Osc(1.8f, 2.2f),
            Jet(2.8f, 30.0f),
            Magnet(-2.9f, 31.0f),
            Laser(-0.6f, 34.6f, 4.6f, 0.8f, 0.9f, 0.5f),
            H(13, 2.9f, 34.7f, 1.3f),
            Ice(0f, 37.6f, 7.2f),
            H(7, -1.4f, 39.8f, 1.4f),
            H(2, 1.8f, 40.0f, 1.3f),
            H(20, -2.6f, 41.8f, 1.2f)),
    };

    // ---- building --------------------------------------------------------------

    [MenuItem("Tools/Tilt Ball/Build Level Set (20 levels)")]
    static void BuildAll()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogError("[LevelSetBuilder] Leave Play mode first.");
            return;
        }
        var profile = AssetDatabase.LoadAssetAtPath<ScreenFitProfile>(ProfilePath);
        if (profile == null)
        {
            Debug.LogError("[LevelSetBuilder] Missing " + ProfilePath);
            return;
        }

        var levels = Levels();
        var built = new List<LevelConfig>();
        int failed = 0;
        InTempScene(scene =>
        {
            for (int i = 0; i < levels.Length; i++)
            {
                var config = Build(levels[i], i, profile.designLength, scene);
                if (config != null) built.Add(config);
                else failed++;
            }
        });
        AssetDatabase.SaveAssets();

        if (failed > 0)
        {
            Debug.LogError("[LevelSetBuilder] " + failed + " level(s) failed validation — Bootstrap left untouched. Fix the table and rebuild.");
            return;
        }
        AssignToBootstrap(built);
        Debug.Log("[LevelSetBuilder] Built " + built.Count + " levels and assigned them to GameManager.allLevels.");
    }

    [MenuItem("Tools/Tilt Ball/Validate Level Set (dry run)")]
    static void ValidateAll()
    {
        var profile = AssetDatabase.LoadAssetAtPath<ScreenFitProfile>(ProfilePath);
        var levels = Levels();
        int failed = 0;
        InTempScene(scene =>
        {
            for (int i = 0; i < levels.Length; i++)
            {
                var root = Spawn(levels[i], profile.designLength, scene, out _);
                try { if (!Validate(levels[i], root, profile.designLength)) failed++; }
                finally { UnityEngine.Object.DestroyImmediate(root); }
            }
        });
        Debug.Log("[LevelSetBuilder] Dry run: " + (levels.Length - failed) + " ok, " + failed + " failed.");
    }

    // The layouts are assembled in a throwaway additive scene so the scene the
    // designer has open is never dirtied by the temporary objects.
    static void InTempScene(Action<UnityEngine.SceneManagement.Scene> work)
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
        try { work(scene); }
        finally { EditorSceneManager.CloseScene(scene, true); }
    }

    static LevelConfig Build(Level level, int index, float designLength, UnityEngine.SceneManagement.Scene scene)
    {
        var root = Spawn(level, designLength, scene, out float holeY);
        try
        {
            if (!Validate(level, root, designLength)) return null;

            string prefabPath = LayoutFolder + "/" + level.name + "_Obstacles.prefab";
            var prefab = PrefabUtility.SaveAsPrefabAsset(root, prefabPath, out bool ok);
            if (!ok || prefab == null)
            {
                Debug.LogError("[LevelSetBuilder] " + level.name + ": failed to save " + prefabPath);
                return null;
            }

            string configPath = ConfigsFolder + "/" + level.name + ".asset";
            var config = AssetDatabase.LoadAssetAtPath<LevelConfig>(configPath);
            if (config == null)
            {
                config = ScriptableObject.CreateInstance<LevelConfig>();
                AssetDatabase.CreateAsset(config, configPath);
            }
            config.levelId = level.name;
            config.levelIndex = index;
            config.layoutPrefab = prefab;
            config.levelLength = level.length;
            config.backgroundSprite = null;
            EditorUtility.SetDirty(config);

            Debug.Log("[LevelSetBuilder] " + level.name + ": length " + level.length + ", hole at (" + level.holeX + ", " + holeY.ToString("F2") + "), " + level.items.Length + " placements.");
            return config;
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(root);
        }
    }

    // Builds the layout as loose objects in the open scene, exactly as
    // LevelDesignTools' Export would find them under LayoutRoot.
    static GameObject Spawn(Level level, float designLength, UnityEngine.SceneManagement.Scene scene, out float holeY)
    {
        float extra = LevelController.ExtraFor(level.length, designLength);
        holeY = ClassicHoleY + extra;

        var root = new GameObject(level.name + "_Obstacles");
        UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(root, scene);

        var hole = Place("Assets/Prefabs/WinningHole.prefab", root.transform, level.holeX, holeY, 1f, 0f);
        hole.name = "WinningHole";

        foreach (var item in level.items)
        {
            string path = item.IsHole ? "Assets/Prefabs/" + item.prefab + ".prefab" : "Assets/Prefabs/Obstacles/" + item.prefab + ".prefab";
            var go = Place(path, root.transform, item.x, item.y, item.scale, item.rot);
            item.setup?.Invoke(go);
            if (item.Moves)
            {
                var osc = go.AddComponent<Oscillator>();
                osc.travel = item.travel;
                osc.period = item.period;
                osc.phase = item.phase;
            }
            foreach (var c in go.GetComponentsInChildren<Component>(true))
                PrefabUtility.RecordPrefabInstancePropertyModifications(c);
        }

        if (level.tutorial)
        {
            var t = new GameObject("TiltTutorial").AddComponent<TiltTutorial>();
            t.transform.SetParent(root.transform, false);
            t.chevronSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Obstacles/Chevron.png");
            t.dotSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Obstacles/Dot.png");
        }
        return root;
    }

    static GameObject Place(string path, Transform parent, float x, float y, float scale, float rot)
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (prefab == null) throw new Exception("Missing prefab " + path);
        var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
        go.transform.localPosition = new Vector3(x, y, 0f);
        go.transform.localRotation = Quaternion.Euler(0f, 0f, rot);
        go.transform.localScale = prefab.transform.localScale * scale;
        return go;
    }

    // ---- validation ------------------------------------------------------------

    struct Footprint
    {
        public string name;
        public bool isHole, isWin;
        public Rect rect;       // world AABB of every collider and sprite, swept over any oscillation
    }

    static bool Validate(Level level, GameObject root, float designLength)
    {
        float extra = LevelController.ExtraFor(level.length, designLength);
        float topBallY = ClassicSummitBallY + extra;
        var errors = new List<string>();
        var warnings = new List<string>();

        // Collider bounds only follow the transforms once physics has synced.
        Physics2D.SyncTransforms();

        var prints = new List<Footprint>();
        int childIndex = 0;
        foreach (Transform child in root.transform)
        {
            var go = child.gameObject;
            if (go.GetComponent<TiltTutorial>() != null) continue;
            bool isWin = go.GetComponent<WinTrigger>() != null;
            bool isHole = go.GetComponent<LoseTrigger>() != null;
            Item item = isWin ? null : level.items[childIndex++];

            var r = Bounds(go, isHole);
            if (item != null && item.Moves)
            {
                r.xMin -= Mathf.Abs(item.travel.x); r.xMax += Mathf.Abs(item.travel.x);
                r.yMin -= Mathf.Abs(item.travel.y); r.yMax += Mathf.Abs(item.travel.y);
            }
            prints.Add(new Footprint { name = Describe(go, item), isHole = isHole, isWin = isWin, rect = r });
        }

        // Losing holes overlap nothing: not each other, not obstacles, not
        // pickups and not the winning hole.
        for (int a = 0; a < prints.Count; a++)
        {
            if (!prints[a].isHole) continue;
            for (int b = 0; b < prints.Count; b++)
            {
                if (a == b || (prints[b].isHole && b < a)) continue;
                if (Overlaps(prints[a].rect, prints[b].rect, Margin))
                    errors.Add(prints[a].name + " overlaps " + prints[b].name);
            }
        }

        // Every hazard is somewhere the ball can actually be, and none can
        // swallow the ball where it spawns.
        foreach (var p in prints)
        {
            if (p.isWin) continue;
            var r = p.rect;
            if (r.xMin < -PlayHalfWidth || r.xMax > PlayHalfWidth) warnings.Add(p.name + " sticks out of the play area horizontally");
            if (r.yMax > topBallY + 0.6f) warnings.Add(p.name + " is above where the ball can reach (" + topBallY.ToString("F2") + ")");
            if (p.isHole && r.yMin < StartBallY + 0.4f) errors.Add(p.name + " reaches down to the ball's spawn height");
            if (p.isHole && (r.xMax < -ReachX + 0.2f || r.xMin > ReachX - 0.2f)) warnings.Add(p.name + " is outside the ball's sideways reach");
        }

        foreach (var w in warnings) Debug.LogWarning("[LevelSetBuilder] " + level.name + ": " + w);
        foreach (var e in errors) Debug.LogError("[LevelSetBuilder] " + level.name + ": " + e);
        return errors.Count == 0;
    }

    // Colliders are the truth for holes: their sprites are square textures with
    // generous transparent padding around an irregular blob. Everything else is
    // measured by collider and sprite together, so a laser's emitters and a
    // magnet's field count as part of it.
    static Rect Bounds(GameObject go, bool isHole)
    {
        bool any = false;
        var b = new Bounds();
        foreach (var c in go.GetComponentsInChildren<Collider2D>(true)) Grow(ref b, ref any, c.bounds);
        if (!isHole)
        {
            foreach (var s in go.GetComponentsInChildren<SpriteRenderer>(true))
            {
                if (s.sprite == null) continue;
                Grow(ref b, ref any, s.bounds);
            }
        }
        if (!any) b = new Bounds(go.transform.position, Vector3.one * 0.2f);
        return new Rect(b.min.x, b.min.y, b.size.x, b.size.y);
    }

    static void Grow(ref Bounds b, ref bool any, Bounds add)
    {
        if (!any) { b = add; any = true; }
        else b.Encapsulate(add);
    }

    static bool Overlaps(Rect a, Rect b, float margin)
        => a.xMin < b.xMax + margin && a.xMax > b.xMin - margin && a.yMin < b.yMax + margin && a.yMax > b.yMin - margin;

    static string Describe(GameObject go, Item item)
    {
        var p = go.transform.localPosition;
        return go.name + "@(" + p.x.ToString("F1") + "," + p.y.ToString("F1") + ")" + (item != null && item.Moves ? "[moving]" : "");
    }

    // ---- bootstrap -------------------------------------------------------------

    // Bootstrap is opened additively, edited and closed again, so whatever the
    // designer has open stays open and untouched.
    static void AssignToBootstrap(List<LevelConfig> configs)
    {
        bool wasOpen = false;
        var scene = default(UnityEngine.SceneManagement.Scene);
        for (int i = 0; i < EditorSceneManager.sceneCount; i++)
        {
            var s = EditorSceneManager.GetSceneAt(i);
            if (s.path == BootstrapScene) { scene = s; wasOpen = true; }
        }
        if (!wasOpen) scene = EditorSceneManager.OpenScene(BootstrapScene, OpenSceneMode.Additive);

        GameManager gm = null;
        foreach (var go in scene.GetRootGameObjects())
        {
            gm = go.GetComponentInChildren<GameManager>(true);
            if (gm != null) break;
        }
        if (gm == null)
        {
            Debug.LogError("[LevelSetBuilder] No GameManager in " + BootstrapScene);
            if (!wasOpen) EditorSceneManager.CloseScene(scene, true);
            return;
        }
        var so = new SerializedObject(gm);
        var list = so.FindProperty("allLevels");
        list.arraySize = configs.Count;
        for (int i = 0; i < configs.Count; i++) list.GetArrayElementAtIndex(i).objectReferenceValue = configs[i];
        so.FindProperty("currentLevel").objectReferenceValue = configs[0];
        so.ApplyModifiedPropertiesWithoutUndo();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        if (!wasOpen) EditorSceneManager.CloseScene(scene, true);
    }
}
