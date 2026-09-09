using PrimeTween;
using UnityEngine;
using UnityEngine.UI;

// Lives in the GameOver scene, layered additively on top of the still-running
// Gameplay scene by SceneLoader when GameManager fires GameState.Lose
// (Gameplay itself isn't unloaded until the player actually leaves) — so
// backdrop can fade in over the real level instead of a blank camera clear.
//
// This scene carries no camera of its own — same reasoning as PauseMenu, which
// has never needed one: with Gameplay still loaded and rendering, wiring a
// second Base camera on top runs into URP's camera-stacking rules (a Base
// camera's Clear Flags "Don't Clear" isn't a reliable see-through in URP the
// way it was in the built-in pipeline). Binding this Canvas to whatever's
// already active sidesteps that entirely, and Gameplay's camera is already
// correctly fit to the device by CameraAspectFit.
public class GameOverController : MonoBehaviour
{
    public RectTransform title;
    public Button retryButton;
    public CanvasGroup backdrop;
    public float backdropFadeDuration = 0.22f;

    void Awake()
    {
        var canvas = GetComponent<Canvas>();
        if (canvas != null) canvas.worldCamera = Camera.main;
    }

    void Start()
    {
        if (AudioManager.Instance != null) AudioManager.Instance.PlayLoseScreen();

        if (backdrop != null)
        {
            backdrop.alpha = 0f;
            Tween.Alpha(backdrop, 0f, 1f, backdropFadeDuration, Ease.OutQuad);
        }

        ScreenEntranceAnimator.AnimateTitle(title);
        ScreenEntranceAnimator.AnimateButton(retryButton);

        if (retryButton != null) retryButton.onClick.AddListener(Retry);
    }

    void Retry()
    {
        AudioManager.PlayClick();
        if (GameManager.Instance != null) GameManager.Instance.SetState(GameState.Playing);
        SceneLoader.Instance.RetryLevel();
    }
}
