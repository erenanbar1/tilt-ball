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

    // Whichever single "main" scene is currently loaded (MainMenu, LevelSelect
    // or Gameplay) — swapping to a new one unloads this first. PauseMenu is
    // additive on top and tracked separately.
    string currentMainScene;
    bool pauseMenuLoaded;

    // Win/Lose are layered on top of the still-running Gameplay scene, the
    // same way PauseMenu is, rather than swapped to — WinScreenController and
    // GameOverController fade a translucent backdrop in over whatever's
    // behind them, which only reads as intended if that's the actual level
    // and not a blank camera clear. Gameplay is unloaded later, when the
    // player actually leaves via LoadGameplay/RetryLevel/GoToMainMenu/
    // GoToLevelSelect.
    string resultScreenScene;

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

    void HandleStateChanged(GameState state)
    {
        if (state == GameState.Win) ShowResultScreen(WinScreenScene);
        else if (state == GameState.Lose) ShowResultScreen(GameOverScene);
        else if (state == GameState.Pause) ShowPauseMenu();
        // Playing covers every way back out of the pause menu — resume, restart
        // and main menu alike — so none of them can leave it open behind them.
        else if (state == GameState.Playing) HidePauseMenu();
    }

    public void GoToMainMenu() { UnloadResultScreenIfAny(); SwapTo(MainMenuScene); }
    public void GoToLevelSelect() { UnloadResultScreenIfAny(); SwapTo(LevelSelectScene); }
    // One gameplay scene serves both modes: everything Tall-specific (climb
    // height, camera range, background) is data on the LevelConfig, applied by
    // LevelController at load.
    public void LoadGameplay() { UnloadResultScreenIfAny(); SwapTo(GameplayScene); }
    public void RetryLevel() { UnloadResultScreenIfAny(); SwapTo(GameplayScene); }

    // Private on purpose: pausing and unpausing go through GameManager's state so
    // there's one source of truth for whether the run is paused, the same as win
    // and lose. HandleStateChanged above is the only caller.
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

    // currentMainScene deliberately stays pointed at Gameplay while a result
    // screen is up — it's still the scene that gets unloaded once the player
    // actually leaves, same as always.
    void ShowResultScreen(string sceneName)
    {
        resultScreenScene = sceneName;
        SceneManager.LoadScene(sceneName, LoadSceneMode.Additive);
    }

    void UnloadResultScreenIfAny()
    {
        if (string.IsNullOrEmpty(resultScreenScene)) return;
        SceneManager.UnloadSceneAsync(resultScreenScene);
        resultScreenScene = null;
    }
}
