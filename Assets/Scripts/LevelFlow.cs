using PrimeTween;
using UnityEngine;
using UnityEngine.UI;

// Owns what the player sees once a run ends and where the buttons on it lead.
// WinTrigger and LoseTrigger report the outcome to GameManager; this only
// listens for the resulting state change, so it never touches obstacle or
// level content directly.
//
// Levels advance through LevelLoader, which owns which LevelConfig is next.
// Dropping a new LevelConfig into LevelLoader's allLevelsInOrder is therefore
// the only step needed to add it to the run.
public class LevelFlow : MonoBehaviour
{
    [Header("Game state — found at runtime when left empty")]
    public GameManager gameManager;
    public LevelLoader levelLoader;

    [Header("Screens — hidden until the run ends")]
    public GameObject winPanel;
    public GameObject losePanel;

    [Header("Flow")]
    // Wraps back to the first level after the last one, so Continue always leads
    // somewhere while there is no real ending screen yet.
    public bool loopAfterLastLevel = true;

    [Header("Entrance animation")]
    // The backdrop fades rather than snapping on, so the end of a run doesn't
    // feel like a frame dropped.
    public float backdropFadeDuration = 0.22f;

    [Header("Title — drops in from above")]
    public float titleDropDistance = 520f;
    public float titleDropDuration = 0.55f;
    // A few degrees of tilt on the way in, unwound by the same overshoot, so the
    // lettering lands with a bit of swagger instead of sliding down flat.
    public float titleTiltDegrees = 7f;

    [Header("Button — pops in, then breathes")]
    public float buttonDelay = 0.26f;
    public float buttonPopDuration = 0.42f;
    // A slow idle pulse afterwards, so the button keeps asking to be pressed.
    public float buttonPulseScale = 1.05f;
    public float buttonPulseDuration = 0.9f;

    void Awake()
    {
        if (gameManager == null) gameManager = FindFirstObjectByType<GameManager>();
        if (levelLoader == null) levelLoader = FindFirstObjectByType<LevelLoader>();

        // Whatever state they were left in while editing, a run always starts clean.
        if (winPanel != null) winPanel.SetActive(false);
        if (losePanel != null) losePanel.SetActive(false);
    }

    void OnEnable()
    {
        if (gameManager != null) gameManager.OnStateChanged += HandleStateChanged;
    }

    void OnDisable()
    {
        if (gameManager != null) gameManager.OnStateChanged -= HandleStateChanged;
    }

    void HandleStateChanged(GameState state)
    {
        if (state == GameState.Win) ShowPanel(winPanel);
        else if (state == GameState.Lose) ShowPanel(losePanel);
    }

    void ShowPanel(GameObject panel)
    {
        if (panel == null) return;
        panel.SetActive(true);

        var group = panel.GetComponent<CanvasGroup>();
        if (group != null)
        {
            // Fading the group rather than the backdrop image keeps the dim at
            // whatever alpha it was designed with, instead of forcing it opaque.
            group.alpha = 0f;
            Tween.Alpha(group, 0f, 1f, backdropFadeDuration, Ease.OutQuad);
        }

        AnimateTitle(panel.transform.Find("Title") as RectTransform);
        AnimateButton(panel.GetComponentInChildren<Button>(true));
    }

    // Falls in from off the top and overshoots into place, unwinding a slight
    // tilt as it lands.
    void AnimateTitle(RectTransform title)
    {
        if (title == null) return;

        Vector2 resting = title.anchoredPosition;
        title.anchoredPosition = resting + new Vector2(0f, titleDropDistance);
        title.localEulerAngles = new Vector3(0f, 0f, titleTiltDegrees);

        Tween.UIAnchoredPosition(title, resting, titleDropDuration, Ease.OutBack);
        Tween.LocalEulerAngles(title, new Vector3(0f, 0f, titleTiltDegrees), Vector3.zero,
            titleDropDuration, Ease.OutBack);
    }

    // Pops in a beat after the title, then keeps breathing so it reads as the
    // thing to press.
    void AnimateButton(Button button)
    {
        if (button == null) return;

        Transform t = button.transform;
        t.localScale = Vector3.zero;

        Tween.Scale(t, 0f, 1f, buttonPopDuration, Ease.OutBack, startDelay: buttonDelay)
            .OnComplete(t, target =>
                Tween.Scale(target, 1f, buttonPulseScale, buttonPulseDuration, Ease.InOutSine,
                    cycles: -1, cycleMode: CycleMode.Yoyo));
    }

    // Hooked to the Continue button on the win screen.
    public void Continue()
    {
        if (levelLoader != null) levelLoader.LoadNext(loopAfterLastLevel);
    }

    // Hooked to the Replay button on the lose screen.
    public void Replay()
    {
        if (levelLoader != null) levelLoader.Reload();
    }
}
