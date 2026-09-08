using UnityEngine;
using UnityEngine.UI;

// Lives on Gameplay's Canvas. Wires the pause button through code, since
// SceneLoader lives in a different scene (Bootstrap) and can't be dragged
// into an Inspector UnityEvent across scene boundaries.
public class GameplayHUD : MonoBehaviour
{
    public Button pauseButton;

    void Start()
    {
        if (pauseButton != null) pauseButton.onClick.AddListener(Pause);
    }

    void Pause()
    {
        AudioManager.PlayClick();
        SceneLoader.Instance.ShowPauseMenu();
    }
}
