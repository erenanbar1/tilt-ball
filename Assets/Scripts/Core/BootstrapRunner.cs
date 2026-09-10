using UnityEngine;

// Bootstrap's only job: hand off to MainMenu once every core system has
// initialized. Start (not Awake) so GameManager/SceneLoader/AudioManager/
// SaveManager have all already run their own Awake first.
public class BootstrapRunner : MonoBehaviour
{
    void Start()
    {
#if UNITY_EDITOR
        // A level being play-tested from the editor skips the menus, but still
        // arrives the normal way — through Bootstrap and SceneLoader — so every
        // system this scene owns is up first. Starting play in the gameplay scene
        // directly would leave GameManager, SceneLoader, AudioManager and
        // SaveManager missing entirely, and with them pausing, win/lose and sound.
        if (!string.IsNullOrEmpty(UnityEditor.SessionState.GetString(LevelController.PreviewLevelKey, string.Empty)))
        {
            // Level prefabs are tall-style, and the mode is what picks the
            // gameplay scene they belong in.
            if (GameManager.Instance != null) GameManager.Instance.SetMode(GameMode.Tall);
            SceneLoader.Instance.LoadGameplay();
            return;
        }
#endif
        SceneLoader.Instance.GoToMainMenu();
    }
}
