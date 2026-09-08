using UnityEngine;
using UnityEngine.UI;

// Spawns one button per level in the active mode's list, disabling any beyond
// what SaveManager has unlocked for that mode. Lives in the LevelSelect scene,
// which both modes share — the mode was already chosen on the Main Menu.
public class LevelSelectController : MonoBehaviour
{
    public Transform buttonContainer;
    public Button levelButtonPrefab; // needs a child UnityEngine.UI.Text for its label
    public Button backButton;

    void Start()
    {
        if (backButton != null) backButton.onClick.AddListener(Back);

        var levels = GameManager.Instance != null ? GameManager.Instance.CurrentLevels : null;
        if (levels == null || buttonContainer == null || levelButtonPrefab == null) return;

        for (int i = 0; i < levels.Length; i++)
        {
            BuildButton(levels[i], i);
        }
    }

    void BuildButton(LevelConfig level, int index)
    {
        var button = Instantiate(levelButtonPrefab, buttonContainer);

        var label = button.GetComponentInChildren<Text>();
        if (label != null)
        {
            label.text = string.IsNullOrEmpty(level.levelId) ? (index + 1).ToString() : level.levelId;
        }

        bool unlocked = SaveManager.Instance == null || SaveManager.Instance.IsUnlocked(index);
        button.interactable = unlocked;
        if (!unlocked) return;

        button.onClick.AddListener(() =>
        {
            AudioManager.PlayClick();
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
