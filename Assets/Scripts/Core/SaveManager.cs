using UnityEngine;

// Persists level-unlock progress across app sessions via PlayerPrefs. Lives in
// Bootstrap alongside the other managers.
public class SaveManager : MonoBehaviour
{
    public static SaveManager Instance { get; private set; }

    const string UnlockedKey = "HighestUnlockedLevelIndex";

    public int HighestUnlockedLevelIndex { get; private set; }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        HighestUnlockedLevelIndex = PlayerPrefs.GetInt(UnlockedKey, 0);
    }

    public bool IsUnlocked(int levelIndex) => levelIndex <= HighestUnlockedLevelIndex;

    // Only ever raises the stored progress — finishing an earlier level again
    // can't lock out further ones.
    public void UnlockLevel(int levelIndex)
    {
        if (levelIndex <= HighestUnlockedLevelIndex) return;
        HighestUnlockedLevelIndex = levelIndex;
        PlayerPrefs.SetInt(UnlockedKey, HighestUnlockedLevelIndex);
        PlayerPrefs.Save();
    }
}
