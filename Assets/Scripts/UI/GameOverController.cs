using PrimeTween;
using UnityEngine;
using UnityEngine.UI;

// Lives in the GameOver scene, swapped in by SceneLoader when GameManager
// fires GameState.Lose.
public class GameOverController : MonoBehaviour
{
    public RectTransform title;
    public Button retryButton;
    public CanvasGroup backdrop;
    public float backdropFadeDuration = 0.22f;

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
        if (GameManager.Instance != null) GameManager.Instance.SetState(GameState.Playing);
        SceneLoader.Instance.RetryLevel();
    }
}
