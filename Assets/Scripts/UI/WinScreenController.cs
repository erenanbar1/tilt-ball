using PrimeTween;
using UnityEngine;
using UnityEngine.UI;

// Lives in the WinScreen scene, swapped in by SceneLoader when GameManager
// fires GameState.Win.
public class WinScreenController : MonoBehaviour
{
    public RectTransform title;
    public Button nextLevelButton;
    public CanvasGroup backdrop;
    public float backdropFadeDuration = 0.22f;

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
