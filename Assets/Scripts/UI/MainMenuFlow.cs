using UnityEngine;
using UnityEngine.UI;

// Lives in the MainMenu scene. Routes through SceneLoader like every other
// scene transition — no direct SceneManager calls here.
public class MainMenuFlow : MonoBehaviour
{
    public Button playButton;
    public Button tallButton;
    public RectTransform title;

    void Awake()
    {
        if (playButton != null) playButton.onClick.AddListener(Play);
        if (tallButton != null) tallButton.onClick.AddListener(PlayTall);
    }

    void Start()
    {
        // Same drop-in-with-overshoot used by WinScreen/GameOver's titles, plus
        // a gentle idle breathing loop once it lands since this one stays on
        // screen (WinScreen/GameOver's titles don't need it — the level reloads
        // before it would be noticeable).
        ScreenEntranceAnimator.AnimateTitle(title, idleAfter: true);
    }

    // The mode is set here, on the way in, and everything downstream — which
    // levels LevelSelect lists, which unlock track gates them, which gameplay
    // scene loads — follows from it.
    public void Play() => StartMode(GameMode.Classic);
    public void PlayTall() => StartMode(GameMode.Tall);

    void StartMode(GameMode mode)
    {
        AudioManager.PlayClick();
        if (GameManager.Instance != null) GameManager.Instance.SetMode(mode);
        SceneLoader.Instance.GoToLevelSelect();
    }
}
