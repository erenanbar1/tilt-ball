using UnityEngine;
using UnityEngine.UI;

// Lives in the MainMenu scene. Routes through SceneLoader like every other
// scene transition — no direct SceneManager calls here.
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
        // Same drop-in-with-overshoot used by WinScreen/GameOver's titles.
        ScreenEntranceAnimator.AnimateTitle(title);
    }

    public void Play()
    {
        SceneLoader.Instance.GoToLevelSelect();
    }
}
