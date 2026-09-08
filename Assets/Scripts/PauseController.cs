using UnityEngine;
using UnityEngine.UI;

// Lives in the PauseMenu scene, loaded additively over Gameplay by
// SceneLoader.ShowPauseMenu() (which has already set Time.timeScale to 0 by
// the time this scene is up).
public class PauseController : MonoBehaviour
{
    public Button resumeButton;
    public Button restartButton;
    public Button mainMenuButton;

    void Start()
    {
        if (resumeButton != null) resumeButton.onClick.AddListener(() => SceneLoader.Instance.HidePauseMenu());
        if (restartButton != null) restartButton.onClick.AddListener(Restart);
        if (mainMenuButton != null) mainMenuButton.onClick.AddListener(GoToMainMenu);
    }

    void Restart()
    {
        SceneLoader.Instance.HidePauseMenu();
        SceneLoader.Instance.RetryLevel();
    }

    void GoToMainMenu()
    {
        SceneLoader.Instance.HidePauseMenu();
        SceneLoader.Instance.GoToMainMenu();
    }
}
