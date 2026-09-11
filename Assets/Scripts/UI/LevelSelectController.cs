using UnityEngine;
using UnityEngine.UI;

// The Classic levels as a single vertical path that scrolls: level 1 at the
// bottom, the newest at the top, connected by a lit trail that runs out where
// progress does. Nodes are built here rather than authored in the scene because
// there is one per level and the list grows.
//
// Tall isn't on the path at all — it hangs off the bottom-right corner as a side
// quest, fixed to the screen while the path scrolls underneath.
public class LevelSelectController : MonoBehaviour
{
    [Header("Scrolling path")]
    public ScrollRect scrollRect;
    public RectTransform content;

    [Header("Node art")]
    // One neutral sprite for every node — locked vs. reached vs. current is all
    // colour tint from here down, the same trick the trail already used, rather
    // than separate art per state.
    public Sprite nodeSprite;
    public Sprite glowSprite;
    public Sprite pathSprite;
    public Font labelFont;

    [Header("Layout, in canvas units")]
    public float nodeSize = 190f;
    public float spacing = 320f;
    public float bottomPadding = 300f;
    public float topPadding = 420f;
    public float pathWidth = 26f;
    public float glowScale = 1.7f;

    [Header("Trail colours")]
    // White to match the new node art's own colour rather than tinting it gold —
    // the sprite is already the finished look, this just dims the unreached half.
    public Color pathReachedColor = Color.white;
    public Color pathLockedColor = new Color(0.4f, 0.4f, 0.46f, 1f);

    [Header("Node colours")]
    public Color unlockedNodeColor = Color.white;
    public Color lockedNodeColor = new Color(0.4f, 0.4f, 0.46f, 1f);
    // Dark on the unlocked node's near-white art rather than the old gold's
    // white-on-gold — white numerals would vanish against this lighter sprite.
    public Color unlockedLabelColor = new Color(0.18f, 0.18f, 0.24f, 1f);
    public Color lockedLabelColor = new Color(0.6f, 0.6f, 0.68f, 1f);

    [Header("Side quest")]
    public Button tallButton;

    public Button backButton;

    [Header("Settings")]
    public Button settingsButton;
    public SettingsPanelController settingsPanel;

    void Start()
    {
        if (backButton != null) backButton.onClick.AddListener(Back);
        if (tallButton != null) tallButton.onClick.AddListener(PlayTall);
        if (settingsButton != null && settingsPanel != null) settingsButton.onClick.AddListener(settingsPanel.Open);

        var lm = LevelManager.Instance;
        if (lm == null || content == null) return;

        BuildPath(lm.classicLevels);
    }

    void BuildPath(LevelConfig[] levels)
    {
        if (levels == null || levels.Length == 0) return;

        // Detached before Destroy, which only takes effect at end of frame — a
        // rebuild would otherwise be reading and stacking on top of the old nodes.
        for (int i = content.childCount - 1; i >= 0; i--)
        {
            var stale = content.GetChild(i);
            stale.SetParent(null, false);
            Destroy(stale.gameObject);
        }

        float height = bottomPadding + (levels.Length - 1) * spacing + topPadding;
        content.sizeDelta = new Vector2(content.sizeDelta.x, height);

        int reached = SaveManager.Instance != null
            ? SaveManager.Instance.HighestUnlocked(GameMode.Classic)
            : levels.Length - 1;

        // Segments first so every node draws on top of the trail it sits on.
        for (int i = 0; i < levels.Length - 1; i++)
        {
            float from = bottomPadding + i * spacing;
            var seg = NewImage("Path_" + i, pathSprite, content);
            seg.color = (i + 1) <= reached ? pathReachedColor : pathLockedColor;
            var r = seg.rectTransform;
            r.anchorMin = r.anchorMax = new Vector2(0.5f, 0f);
            r.pivot = new Vector2(0.5f, 0f);
            r.sizeDelta = new Vector2(pathWidth, spacing);
            r.anchoredPosition = new Vector2(0f, from);
        }

        for (int i = 0; i < levels.Length; i++)
        {
            BuildNode(levels[i], i, bottomPadding + i * spacing, i <= reached, i == reached);
        }

        ScrollTo(bottomPadding + Mathf.Clamp(reached, 0, levels.Length - 1) * spacing, height);
    }

    void BuildNode(LevelConfig level, int index, float y, bool unlocked, bool isCurrent)
    {
        // The glow is its own object behind the node so it can overflow the
        // node's bounds without scaling the button itself.
        if (isCurrent && glowSprite != null)
        {
            var glow = NewImage("Glow_" + index, glowSprite, content);
            glow.raycastTarget = false;
            var gr = glow.rectTransform;
            gr.anchorMin = gr.anchorMax = new Vector2(0.5f, 0f);
            gr.pivot = new Vector2(0.5f, 0.5f);
            gr.sizeDelta = new Vector2(nodeSize * glowScale, nodeSize * glowScale);
            gr.anchoredPosition = new Vector2(0f, y);
        }

        var img = NewImage("Level_" + (index + 1), nodeSprite, content);
        img.color = unlocked ? unlockedNodeColor : lockedNodeColor;
        var rt = img.rectTransform;
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(nodeSize, nodeSize);
        rt.anchoredPosition = new Vector2(0f, y);

        var label = new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        var lrt = label.GetComponent<RectTransform>();
        lrt.SetParent(rt, false);
        lrt.anchorMin = Vector2.zero;
        lrt.anchorMax = Vector2.one;
        lrt.offsetMin = Vector2.zero;
        lrt.offsetMax = Vector2.zero;
        var text = label.GetComponent<Text>();
        text.text = (index + 1).ToString();
        text.font = labelFont;
        text.fontSize = Mathf.RoundToInt(nodeSize * 0.42f);
        text.fontStyle = FontStyle.Bold;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = unlocked ? unlockedLabelColor : lockedLabelColor;
        text.raycastTarget = false;

        var button = img.gameObject.AddComponent<Button>();
        button.targetGraphic = img;
        button.interactable = unlocked;
        if (!unlocked) return;

        int captured = index;
        button.onClick.AddListener(() =>
        {
            AudioManager.PlayClick();
            if (!LevelManager.Instance.Select(GameMode.Classic, captured)) return;
            SceneLoader.Instance.LoadGameplay();
        });
    }

    Image NewImage(string name, Sprite sprite, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        go.transform.SetParent(parent, false);
        var img = go.GetComponent<Image>();
        img.sprite = sprite;
        return img;
    }

    // Opens on the level the player is actually up to instead of at level 1,
    // which would mean scrolling past everything already cleared.
    void ScrollTo(float y, float contentHeight)
    {
        if (scrollRect == null || scrollRect.viewport == null) return;

        float viewport = scrollRect.viewport.rect.height;
        float range = contentHeight - viewport;
        if (range <= 0f) return;

        scrollRect.verticalNormalizedPosition = Mathf.Clamp01((y - viewport * 0.5f) / range);
    }

    void PlayTall()
    {
        AudioManager.PlayClick();

        var lm = LevelManager.Instance;
        var tall = lm != null ? lm.tallLevels : null;
        if (tall == null || tall.Length == 0) return;

        int index = SaveManager.Instance != null
            ? Mathf.Clamp(SaveManager.Instance.HighestUnlocked(GameMode.Tall), 0, tall.Length - 1)
            : 0;

        if (!lm.Select(GameMode.Tall, index)) return;
        SceneLoader.Instance.LoadGameplay();
    }

    void Back()
    {
        AudioManager.PlayClick();
        SceneLoader.Instance.GoToMainMenu();
    }
}
