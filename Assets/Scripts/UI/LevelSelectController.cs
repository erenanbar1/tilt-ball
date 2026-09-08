using UnityEngine;
using UnityEngine.UI;

// Lists every level in the game, grouped by mode: the Classic set and the Tall
// set each get their own grid under their own heading, so both are on screen at
// once instead of behind a mode switch. Picking a level is what selects the
// mode, so nothing has to be chosen before arriving here.
public class LevelSelectController : MonoBehaviour
{
    public Transform classicContainer;
    public Transform tallContainer;
    public Button levelButtonPrefab; // needs a child UnityEngine.UI.Text for its label
    public Button backButton;

    void Start()
    {
        if (backButton != null) backButton.onClick.AddListener(Back);

        var gm = GameManager.Instance;
        if (gm == null || levelButtonPrefab == null) return;

        BuildSection(classicContainer, gm.allLevels, GameMode.Classic);
        BuildSection(tallContainer, gm.tallLevels, GameMode.Tall);
    }

    void BuildSection(Transform container, LevelConfig[] levels, GameMode mode)
    {
        if (container == null || levels == null) return;

        for (int i = 0; i < levels.Length; i++)
        {
            BuildButton(container, levels[i], i, mode);
        }
    }

    void BuildButton(Transform container, LevelConfig level, int index, GameMode mode)
    {
        var button = Instantiate(levelButtonPrefab, container);

        var label = button.GetComponentInChildren<Text>();
        if (label != null)
        {
            label.text = string.IsNullOrEmpty(level.levelId) ? (index + 1).ToString() : level.levelId;
        }

        // Gated by its own mode's progress rather than whichever mode happens to
        // be current, since both sets are listed side by side here.
        bool unlocked = SaveManager.Instance == null || SaveManager.Instance.IsUnlocked(mode, index);
        button.interactable = unlocked;
        if (!unlocked) return;

        button.onClick.AddListener(() =>
        {
            AudioManager.PlayClick();
            GameManager.Instance.SetMode(mode);
            GameManager.Instance.currentLevel = level;
            SceneLoader.Instance.LoadGameplay();
        });
    }

    void Back()
    {
        AudioManager.PlayClick();
        SceneLoader.Instance.GoToMainMenu();
    }
}
