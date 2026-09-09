using UnityEngine;
using UnityEngine.UI;

// A self-contained settings overlay: some button elsewhere in the scene calls
// Open(), tapping the backdrop calls Close(). Reused as-is in MainMenu and
// LevelSelect — screens with no GameState of their own, so this is a plain
// SetActive toggle rather than Gameplay's settings button, which goes through
// GameManager's Pause state to bring up a whole additive scene.
//
// The root object stays active always (Awake needs to run to wire the
// backdrop); only the overlay child — backdrop plus the toggle icons — is
// what's actually hidden until opened.
public class SettingsPanelController : MonoBehaviour
{
    public GameObject overlay;
    public Button backdropButton;

    void Awake()
    {
        if (backdropButton != null) backdropButton.onClick.AddListener(Close);
        if (overlay != null) overlay.SetActive(false);
    }

    public void Open()
    {
        AudioManager.PlayClick();
        if (overlay != null) overlay.SetActive(true);
    }

    void Close()
    {
        AudioManager.PlayClick();
        if (overlay != null) overlay.SetActive(false);
    }
}
