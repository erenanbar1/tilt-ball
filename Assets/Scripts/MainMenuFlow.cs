using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenuFlow : MonoBehaviour
{
    public string firstLevelSceneName = "Level";
    public Button playButton;

    void Awake()
    {
        if (playButton != null) playButton.onClick.AddListener(Play);
    }

    public void Play()
    {
        SceneManager.LoadScene(firstLevelSceneName);
    }
}
