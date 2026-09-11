using System;
using UnityEngine;

// Tracks the run's state and which level is selected — nothing about obstacle
// content itself. WinTrigger/LoseTrigger report outcomes here; SceneLoader
// listens for the transition and does the actual scene swap.
//
// Lives in Bootstrap, which loads once and is never unloaded, so this is a
// true persistent singleton reachable from every other scene without an
// inspector-dragged reference across scene boundaries.
public enum GameState { Playing, Win, Lose, Pause }

// Which set of levels the player is working through. Classic levels fit one
// screen; Tall levels are several screens high and scroll. The mode picks the
// level list, the unlock track and the gameplay scene — everything downstream
// reads it rather than being told about it.
public enum GameMode { Classic, Tall }

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Level selection — set by LevelSelect, read by Gameplay's LevelController")]
    public LevelConfig currentLevel;
    public LevelConfig[] allLevels;
    public LevelConfig[] tallLevels;

    public GameState CurrentState { get; private set; } = GameState.Playing;
    public GameMode CurrentMode { get; private set; } = GameMode.Classic;

    // The list the rest of the game should read. Everything that used to reach
    // for allLevels goes through here instead, so adding a mode didn't mean
    // teaching each caller about modes.
    public LevelConfig[] CurrentLevels => CurrentMode == GameMode.Tall ? tallLevels : allLevels;

    public event Action<GameState> OnStateChanged;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    public void SetState(GameState newState)
    {
        if (newState == CurrentState) return;
        CurrentState = newState;
        OnStateChanged?.Invoke(newState);
    }

    // Set by the Main Menu before it hands off to LevelSelect. No event: unlike
    // state, nothing reacts to a mode change — the next screen just reads it.
    public void SetMode(GameMode mode) => CurrentMode = mode;

    // Index of currentLevel within the active mode's list, or -1 if not
    // found/not set.
    public int CurrentLevelIndex()
    {
        var levels = CurrentLevels;
        if (currentLevel == null || levels == null) return -1;
        return Array.IndexOf(levels, currentLevel);
    }
}
