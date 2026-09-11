using UnityEngine;

// Persists level-unlock progress across app sessions via PlayerPrefs. Lives in
// Bootstrap alongside the other managers.
//
// Each mode keeps its own track, so clearing Tall levels never unlocks Classic
// ones or the reverse. The Classic key is spelled exactly as it always was —
// renaming it would read back zero and silently reset every existing player.
public class SaveManager : MonoBehaviour
{
    public static SaveManager Instance { get; private set; }

    const string UnlockedKey = "HighestUnlockedLevelIndex";
    const string TallUnlockedKey = "Tall_HighestUnlockedLevelIndex";

    public int HighestUnlockedLevelIndex { get; private set; }
    public int HighestUnlockedTallLevelIndex { get; private set; }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        // Both tracks are loaded here rather than on demand, so this doesn't
        // depend on GameManager having woken up first.
        HighestUnlockedLevelIndex = PlayerPrefs.GetInt(UnlockedKey, 0);
        HighestUnlockedTallLevelIndex = PlayerPrefs.GetInt(TallUnlockedKey, 0);
    }

    public bool IsUnlocked(int levelIndex) => IsUnlocked(ActiveMode(), levelIndex);

    public bool IsUnlocked(GameMode mode, int levelIndex) => levelIndex <= HighestUnlocked(mode);

    public void UnlockLevel(int levelIndex) => UnlockLevel(ActiveMode(), levelIndex);

    // Only ever raises the stored progress — finishing an earlier level again
    // can't lock out further ones.
    public void UnlockLevel(GameMode mode, int levelIndex)
    {
        if (levelIndex <= HighestUnlocked(mode)) return;

        if (mode == GameMode.Tall)
        {
            HighestUnlockedTallLevelIndex = levelIndex;
            PlayerPrefs.SetInt(TallUnlockedKey, levelIndex);
        }
        else
        {
            HighestUnlockedLevelIndex = levelIndex;
            PlayerPrefs.SetInt(UnlockedKey, levelIndex);
        }
        PlayerPrefs.Save();
    }

    public int HighestUnlocked(GameMode mode) =>
        mode == GameMode.Tall ? HighestUnlockedTallLevelIndex : HighestUnlockedLevelIndex;

    // Falls back to Classic if LevelManager isn't up yet, which matches how the
    // game behaved before there was a second mode.
    static GameMode ActiveMode() =>
        LevelManager.Instance != null ? LevelManager.Instance.Mode : GameMode.Classic;
}
