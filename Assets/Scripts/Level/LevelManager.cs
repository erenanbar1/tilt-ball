using System;
using UnityEngine;

// Which set of levels the player is working through. Classic levels fit one
// screen; Tall levels are several screens high and scroll. The mode picks the
// level list and the unlock track — everything downstream reads it rather than
// being told about it.
public enum GameMode { Classic, Tall }

// Owns the level catalogue and the selection: which mode is active, which
// level is current, and what "next" means. Selection only changes through
// Select/Advance so mode and level can never disagree. Nothing here knows
// about scenes or run state — SceneLoader and GameManager do those.
//
// Manager vs. controller: this is the persistent "which level" (lives in
// Bootstrap, survives every scene swap); LevelController is the per-scene
// "apply it to the rig".
public class LevelManager : MonoBehaviour
{
    public static LevelManager Instance { get; private set; }

    public LevelConfig[] classicLevels;
    public LevelConfig[] tallLevels;

    public GameMode Mode { get; private set; } = GameMode.Classic;
    public LevelConfig Current { get; private set; }

    public LevelConfig[] Levels => LevelsFor(Mode);
    public int CurrentIndex => Current == null ? -1 : Array.IndexOf(Levels, Current);

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        if (classicLevels != null && classicLevels.Length > 0) Current = classicLevels[0];
    }

    public LevelConfig[] LevelsFor(GameMode mode) => mode == GameMode.Tall ? tallLevels : classicLevels;

    public bool Select(GameMode mode, int index)
    {
        var levels = LevelsFor(mode);
        if (levels == null || index < 0 || index >= levels.Length) return false;

        Mode = mode;
        Current = levels[index];
        return true;
    }

    // Moves to the next level in the active mode's list, unlocking it, and
    // wraps to the first after the last. Stays within the mode, so finishing a
    // Tall level leads to the next Tall one rather than back into Classic.
    public bool Advance()
    {
        var levels = Levels;
        if (levels == null || levels.Length == 0) return false;

        int next = (CurrentIndex + 1) % levels.Length;
        if (SaveManager.Instance != null) SaveManager.Instance.UnlockLevel(Mode, next);

        Current = levels[next];
        return true;
    }
}
