using UnityEngine;
using UnityEngine.UI;

// Lives in the MainMenu scene. Routes through SceneLoader like every other
// scene transition — no direct SceneManager calls here.
//
// One Play button, no mode choice: both modes are listed together on the
// LevelSelect screen, and picking a level there is what sets the mode.
public class MainMenuFlow : MonoBehaviour
{
    public Button playButton;
    public RectTransform title;

    void Awake()
    {
        if (playButton != null) playButton.onClick.AddListener(Play);
    }

    void Start()
    {
        // Same drop-in-with-overshoot used by WinScreen/GameOver's titles, plus
        // a gentle idle breathing loop once it lands since this one stays on
        // screen (WinScreen/GameOver's titles don't need it — the level reloads
        // before it would be noticeable).
        ScreenEntranceAnimator.AnimateTitle(title, idleAfter: true);
    }

    public void Play()
    {
        AudioManager.PlayClick();
        SceneLoader.Instance.GoToLevelSelect();
    }
}
