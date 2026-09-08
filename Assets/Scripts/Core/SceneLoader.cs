using UnityEngine;
using UnityEngine.SceneManagement;

// Every scene transition in the game routes through here — no other script
// calls SceneManager.LoadScene/UnloadSceneAsync directly. Lives in Bootstrap
// alongside GameManager, so it's reachable from any scene as a singleton.
public class SceneLoader : MonoBehaviour
{
    public static SceneLoader Instance { get; private set; }

    const string MainMenuScene = "MainMenu";
    const string LevelSelectScene = "LevelSelect";
    const string GameplayScene = "Gameplay";
    const string PauseMenuScene = "PauseMenu";
    const string WinScreenScene = "WinScreen";
    const string GameOverScene = "GameOver";

    // Whichever single "main" scene is currently loaded (MainMenu, LevelSelect,
    // Gameplay, WinScreen or GameOver) — swapping to a new one unloads this
    // first. PauseMenu is additive on top and tracked separately.
    string currentMainScene;
    bool pauseMenuLoaded;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    // Subscribing in Start rather than OnEnable/Awake: Unity guarantees every
    // Awake in the scene has run before any Start, so GameManager.Instance is
    // reliably set by the time this runs.
    void Start()
    {
        if (GameManager.Instance != null) GameManager.Instance.OnStateChanged += HandleStateChanged;
    }

    void OnDestroy()
    {
        if (GameManager.Instance != null) GameManager.Instance.OnStateChanged -= HandleStateChanged;
    }

    // GameManager is the source of truth for whether the game is paused —
    // this just translates that into the actual scene load/unload and
    // timescale work. Pause() below is the only place anything requests Pause.
    void HandleStateChanged(GameState state)
    {
        if (state == GameState.Win) SwapTo(WinScreenScene);
        else if (state == GameState.Lose) SwapTo(GameOverScene);
        else if (state == GameState.Pause) ShowPauseMenu();
        else if (state == GameState.Playing) HidePauseMenu(); // no-op if not paused
    }

    public void GoToMainMenu() => SwapTo(MainMenuScene);
    public void GoToLevelSelect() => SwapTo(LevelSelectScene);
    public void LoadGameplay() => SwapTo(GameplayScene);
    public void RetryLevel() => SwapTo(GameplayScene);

    void ShowPauseMenu()
    {
        if (pauseMenuLoaded) return;
        pauseMenuLoaded = true;
        Time.timeScale = 0f;
        SceneManager.LoadScene(PauseMenuScene, LoadSceneMode.Additive);
    }

    void HidePauseMenu()
    {
        if (!pauseMenuLoaded) return;
        pauseMenuLoaded = false;
        Time.timeScale = 1f;
        SceneManager.UnloadSceneAsync(PauseMenuScene);
    }

    // Unloads whichever main scene is currently up (if any) and loads the new
    // one additively, so Bootstrap — and anything else additive, like an open
    // PauseMenu — is never touched.
    void SwapTo(string sceneName)
    {
        if (!string.IsNullOrEmpty(currentMainScene))
        {
            SceneManager.UnloadSceneAsync(currentMainScene);
        }
        SceneManager.LoadScene(sceneName, LoadSceneMode.Additive);
        currentMainScene = sceneName;
    }
}
