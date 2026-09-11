using UnityEngine;

// Bootstrap's only job: hand off once every core system has initialized.
// Start (not Awake) so GameManager/SceneLoader/AudioManager/SaveManager/
// LevelManager have all already run their own Awake first. Normally that's
// the main menu; the editor's "Play This Level" jumps straight into gameplay.
public class BootstrapRunner : MonoBehaviour
{
    void Start()
    {
        if (LevelManager.Instance != null && LevelManager.Instance.StartedFromDebug)
            SceneLoader.Instance.LoadGameplay();
        else
            SceneLoader.Instance.GoToMainMenu();
    }
}
