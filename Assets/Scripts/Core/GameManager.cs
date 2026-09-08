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

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Level selection — set by LevelSelect, read by Gameplay's LevelController")]
    public LevelConfig currentLevel;
    public LevelConfig[] allLevels;

    public GameState CurrentState { get; private set; } = GameState.Playing;

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

    // Index of currentLevel within allLevels, or -1 if not found/not set.
    public int CurrentLevelIndex()
    {
        if (currentLevel == null || allLevels == null) return -1;
        return Array.IndexOf(allLevels, currentLevel);
    }
}
