using System;
using UnityEngine;

// Which set of levels the player is working through. Classic levels fit one
// screen; Tall levels are several screens high and scroll. The mode picks the
// level list and the unlock track — everything downstream reads it rather than
// being told about it.
public enum GameMode { Classic, Tall }

// Owns the level selection: which mode is active, which level is current, and
// what "next" means, reading the lists from LevelCatalog. Selection only
// changes through Select/Advance so mode and level can never disagree.
// Nothing here knows about scenes or run state — SceneLoader and GameManager
// do those.
//
// Manager vs. controller: this is the persistent "which level" (lives in
// Bootstrap, survives every scene swap); LevelController is the per-scene
// "apply it to the rig".
public class LevelManager : MonoBehaviour
{
    public static LevelManager Instance { get; private set; }

    public LevelCatalog catalog;

    public GameMode Mode { get; private set; } = GameMode.Classic;
    public LevelConfig Current { get; private set; }

    // True when the editor's "Play This Level" started this run: Bootstrap
    // then goes straight to gameplay instead of the main menu.
    public bool StartedFromDebug { get; private set; }

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

        var classic = LevelsFor(GameMode.Classic);
        if (classic != null && classic.Length > 0) Current = classic[0];

        TryConsumeDebugStart();
    }

    public LevelConfig[] LevelsFor(GameMode mode) => catalog != null ? catalog.LevelsFor(mode) : null;

    public bool Select(GameMode mode, int index)
    {
        var levels = LevelsFor(mode);
        if (levels == null || index < 0 || index >= levels.Length) return false;

        Mode = mode;
        Current = levels[index];
        return true;
    }

    public bool Select(LevelConfig level)
    {
        if (level == null) return false;
        var levels = LevelsFor(level.Mode);
        return Select(level.Mode, levels == null ? -1 : Array.IndexOf(levels, level));
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

#if UNITY_EDITOR
    const string DebugLevelKey = "TiltBall.DebugStartLevel";

    // Editor-only handshake for "Play This Level": the menu stores the level's
    // GUID in SessionState (survives the play-mode domain reload, never ships)
    // and enters play; Awake here picks it up once.
    public static void RequestDebugStart(LevelConfig level)
    {
        string path = UnityEditor.AssetDatabase.GetAssetPath(level);
        UnityEditor.SessionState.SetString(DebugLevelKey, UnityEditor.AssetDatabase.AssetPathToGUID(path));
    }

    void TryConsumeDebugStart()
    {
        string guid = UnityEditor.SessionState.GetString(DebugLevelKey, "");
        if (string.IsNullOrEmpty(guid)) return;
        UnityEditor.SessionState.EraseString(DebugLevelKey);

        var level = UnityEditor.AssetDatabase.LoadAssetAtPath<LevelConfig>(UnityEditor.AssetDatabase.GUIDToAssetPath(guid));
        if (level == null) return;

        if (Select(level)) StartedFromDebug = true;
        else Debug.LogWarning($"Play This Level: '{level.name}' is not in the LevelCatalog. Rebuild it via Tools > Tilt Ball > Rebuild Level Catalog.", level);
    }
#else
    void TryConsumeDebugStart() { }
#endif
}
