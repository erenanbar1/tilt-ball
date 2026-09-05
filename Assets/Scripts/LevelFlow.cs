using UnityEngine;
using UnityEngine.SceneManagement;

// Owns what the player sees once a run ends and where the buttons on it lead.
// WinTrigger and LoseTrigger only report the outcome; a level needs nothing wired
// to them, because the panels and the scene changes all live here.
//
// Levels advance by build order: Continue loads the next scene in Build Settings,
// Replay reloads the current one. Dropping a new level into Build Settings is
// therefore the only step needed to add it to the run.
public class LevelFlow : MonoBehaviour
{
    [Header("Screens — hidden until the run ends")]
    public GameObject winPanel;
    public GameObject losePanel;

    [Header("Flow")]
    // Wraps back to the first level after the last one, so Continue always leads
    // somewhere while there is no real ending screen yet.
    public bool loopAfterLastLevel = true;

    void Awake()
    {
        // Whatever state they were left in while editing, a run always starts clean.
        if (winPanel != null) winPanel.SetActive(false);
        if (losePanel != null) losePanel.SetActive(false);
    }

    // Looked up rather than cached in a static: this project runs with domain
    // reload disabled, where a stale static survives between play sessions. This
    // only ever runs once, at the end of a level.
    public static void NotifyWin()
    {
        var flow = FindFirstObjectByType<LevelFlow>();
        if (flow != null && flow.winPanel != null) flow.winPanel.SetActive(true);
    }

    public static void NotifyLose()
    {
        var flow = FindFirstObjectByType<LevelFlow>();
        if (flow != null && flow.losePanel != null) flow.losePanel.SetActive(true);
    }

    // Hooked to the Continue button on the win screen.
    public void Continue()
    {
        int next = SceneManager.GetActiveScene().buildIndex + 1;
        if (next >= SceneManager.sceneCountInBuildSettings)
        {
            if (!loopAfterLastLevel) { Replay(); return; }
            next = 0;
        }
        SceneManager.LoadScene(next);
    }

    // Hooked to the Replay button on the lose screen.
    public void Replay()
    {
        var scene = SceneManager.GetActiveScene();
        if (scene.buildIndex >= 0)
        {
            SceneManager.LoadScene(scene.buildIndex);
            return;
        }

        // A scene played straight out of the editor without being in Build
        // Settings has no build index, and reloading it is simply not possible —
        // say so plainly rather than letting LoadScene(-1) throw.
        Debug.LogWarning("Replay needs '" + scene.name + "' to be listed and ticked in Build Settings.", this);
    }
}
