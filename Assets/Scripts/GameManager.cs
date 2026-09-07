using System;
using UnityEngine;

// Tracks the run's state and nothing else — no obstacle, prefab, or LevelConfig
// references. WinTrigger/LoseTrigger report outcomes here; LevelFlow listens for
// the transition to show the matching panel.
public enum GameState { Playing, Win, Lose, Pause }

public class GameManager : MonoBehaviour
{
    public GameState CurrentState { get; private set; } = GameState.Playing;

    public event Action<GameState> OnStateChanged;

    public void SetState(GameState newState)
    {
        if (newState == CurrentState) return;
        CurrentState = newState;
        OnStateChanged?.Invoke(newState);
    }
}
