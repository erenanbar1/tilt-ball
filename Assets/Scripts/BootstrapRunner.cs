using UnityEngine;

// Bootstrap's only job: hand off to MainMenu once every core system has
// initialized. Start (not Awake) so GameManager/SceneLoader/AudioManager/
// SaveManager have all already run their own Awake first.
public class BootstrapRunner : MonoBehaviour
{
    void Start()
    {
        SceneLoader.Instance.GoToMainMenu();
    }
}
