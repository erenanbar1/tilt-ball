using System;
using UnityEngine;

// Tracks the run's state — nothing about which level is playing (LevelManager)
// or obstacle content. WinTrigger/LoseTrigger report outcomes here; SceneLoader
// listens for the transition and does the actual scene swap.
//
// Lives in Bootstrap, which loads once and is never unloaded, so this is a
// true persistent singleton reachable from every other scene without an
// inspector-dragged reference across scene boundaries.
public enum GameState { Playing, Win, Lose, Pause }

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

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
}
