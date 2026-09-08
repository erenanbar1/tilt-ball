using UnityEngine;
using UnityEngine.UI;

// Spawns one button per level in GameManager.allLevels, disabling any beyond
// what SaveManager has unlocked. Lives in the LevelSelect scene.
public class LevelSelectController : MonoBehaviour
{
    public Transform buttonContainer;
    public Button levelButtonPrefab; // needs a child UnityEngine.UI.Text for its label
    public Button backButton;

    void Start()
    {
        if (backButton != null) backButton.onClick.AddListener(() => SceneLoader.Instance.GoToMainMenu());

        var levels = GameManager.Instance != null ? GameManager.Instance.allLevels : null;
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
            GameManager.Instance.currentLevel = level;
            SceneLoader.Instance.LoadGameplay();
        });
    }
}
