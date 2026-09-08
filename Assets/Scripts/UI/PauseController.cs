using UnityEngine;
using UnityEngine.UI;

// Lives in the PauseMenu scene, loaded additively over Gameplay by SceneLoader
// reacting to GameManager.SetState(GameState.Pause) (which has already set
// Time.timeScale to 0 by the time this scene is up).
public class PauseController : MonoBehaviour
{
    public Button resumeButton;
    public Button restartButton;
    public Button mainMenuButton;

    void Start()
    {
        if (resumeButton != null) resumeButton.onClick.AddListener(Resume);
        if (restartButton != null) restartButton.onClick.AddListener(Restart);
        if (mainMenuButton != null) mainMenuButton.onClick.AddListener(GoToMainMenu);
    }

    // Setting Playing is what tells SceneLoader to unload the pause overlay
    // and restore Time.timeScale — see SceneLoader.HandleStateChanged.
    void Resume()
    {
        if (GameManager.Instance != null) GameManager.Instance.SetState(GameState.Playing);
    }

    void Restart()
    {
        if (GameManager.Instance != null) GameManager.Instance.SetState(GameState.Playing);
        SceneLoader.Instance.RetryLevel();
    }

    void GoToMainMenu()
    {
        if (GameManager.Instance != null) GameManager.Instance.SetState(GameState.Playing);
        SceneLoader.Instance.GoToMainMenu();
    }
}
