using UnityEngine;
using UnityEngine.UI;

// Lives in the PauseMenu scene, loaded additively over Gameplay when GameManager
// enters the Pause state (SceneLoader has already set Time.timeScale to 0 by the
// time this scene is up). Both buttons leave that state the same way — back to
// Playing — which is what closes the menu; the one that navigates then asks
// SceneLoader for the scene it wants.
public class PauseController : MonoBehaviour
{
    public Button resumeButton;
    public Button mainMenuButton;

    void Start()
    {
        if (resumeButton != null) resumeButton.onClick.AddListener(Resume);
        if (mainMenuButton != null) mainMenuButton.onClick.AddListener(GoToMainMenu);
    }

    void Resume()
    {
        AudioManager.PlayClick();
        Unpause();
    }

    void GoToMainMenu()
    {
        AudioManager.PlayClick();
        Unpause();
        SceneLoader.Instance.GoToMainMenu();
    }

    // Leaving Pause is what closes the menu and restores timeScale. Without it
    // the state would stay Pause and the next press of the pause button would be
    // swallowed as a no-op change.
    void Unpause()
    {
        if (GameManager.Instance != null) GameManager.Instance.SetState(GameState.Playing);
    }
}
