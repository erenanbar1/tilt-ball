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
        var gm = GameManager.Instance;
        if (gm == null || gm.allLevels == null || gm.allLevels.Length == 0) return;

        int nextIndex = gm.CurrentLevelIndex() + 1;
        if (nextIndex >= gm.allLevels.Length) nextIndex = 0; // loop back to the first level

        if (SaveManager.Instance != null) SaveManager.Instance.UnlockLevel(nextIndex);

        gm.currentLevel = gm.allLevels[nextIndex];
        gm.SetState(GameState.Playing);
        SceneLoader.Instance.LoadGameplay();
    }
}
