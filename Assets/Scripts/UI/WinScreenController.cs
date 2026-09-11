using PrimeTween;
using UnityEngine;
using UnityEngine.UI;

// Lives in the WinScreen scene, layered additively on top of the still-running
// Gameplay scene by SceneLoader when GameManager fires GameState.Win (Gameplay
// itself isn't unloaded until the player actually leaves) — so backdrop can
// fade in over the real level instead of a blank camera clear.
//
// This scene carries no camera of its own — same reasoning as PauseMenu, which
// has never needed one: with Gameplay still loaded and rendering, wiring a
// second Base camera on top runs into URP's camera-stacking rules (a Base
// camera's Clear Flags "Don't Clear" isn't a reliable see-through in URP the
// way it was in the built-in pipeline). Binding this Canvas to whatever's
// already active sidesteps that entirely, and Gameplay's camera is already
// correctly fit to the device by CameraAspectFit.
public class WinScreenController : MonoBehaviour
{
    public RectTransform title;
    public Button nextLevelButton;
    public CanvasGroup backdrop;
    public float backdropFadeDuration = 0.22f;

    void Awake()
    {
        var canvas = GetComponent<Canvas>();
        if (canvas != null) canvas.worldCamera = Camera.main;
    }

    void Start()
    {
        if (AudioManager.Instance != null) AudioManager.Instance.PlayWinScreen();

        if (backdrop != null)
        {
            backdrop.alpha = 0f;
            Tween.Alpha(backdrop, 0f, 1f, backdropFadeDuration, Ease.OutQuad);
        }

        ScreenEntranceAnimator.AnimateTitle(title);
        ScreenEntranceAnimator.AnimateButton(nextLevelButton);

        if (nextLevelButton != null) nextLevelButton.onClick.AddListener(NextLevel);
    }

    void NextLevel()
    {
        AudioManager.PlayClick();

        var gm = GameManager.Instance;
        // Advances within whichever mode's list is active, so finishing a Tall
        // level leads to the next Tall one rather than back into Classic.
        var levels = gm != null ? gm.CurrentLevels : null;
        if (levels == null || levels.Length == 0) return;

        int nextIndex = gm.CurrentLevelIndex() + 1;
        if (nextIndex >= levels.Length) nextIndex = 0; // loop back to the first level

        if (SaveManager.Instance != null) SaveManager.Instance.UnlockLevel(nextIndex);

        gm.currentLevel = levels[nextIndex];
        gm.SetState(GameState.Playing);
        SceneLoader.Instance.LoadGameplay();
    }
}
