using UnityEngine;
using UnityEngine.SceneManagement;

// Owns which LevelConfig is active and spawns its obstacles into ObstaclesRoot.
// Knows nothing about GameManager — LevelFlow drives progression by calling
// LoadNext/Reload, the same way it used to drive SceneManager directly.
public class LevelLoader : MonoBehaviour
{
    [Header("Authoring — drag a config here and press Play to test just this level")]
    public LevelConfig currentLevel;

    [Header("Full-game progression — every level, in order")]
    public LevelConfig[] allLevelsInOrder;

    public Transform obstaclesRoot;

    // Set by LoadNext/Reload before reloading the scene; -1 means "use
    // currentLevel as authored", so dragging a config in and pressing Play
    // still works for testing a single level in isolation.
    private static int pendingLevelIndex = -1;

    // This project runs with domain reload disabled, so without this a static
    // left over from a previous Play session (e.g. progressed to level 3 via
    // Continue) would silently override whatever's dragged into currentLevel
    // the next time Play is pressed. SubsystemRegistration runs at the start
    // of every Play session regardless of domain reload settings.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetPendingLevelOnPlaySessionStart()
    {
        pendingLevelIndex = -1;
    }

    private GameObject spawnedObstacles;

    void Awake()
    {
        if (pendingLevelIndex >= 0 && allLevelsInOrder != null && pendingLevelIndex < allLevelsInOrder.Length)
        {
            currentLevel = allLevelsInOrder[pendingLevelIndex];
        }

        SpawnObstacles();
    }

    void SpawnObstacles()
    {
        if (spawnedObstacles != null) Destroy(spawnedObstacles);
        if (currentLevel == null || currentLevel.obstaclesPrefab == null || obstaclesRoot == null) return;

        spawnedObstacles = Instantiate(currentLevel.obstaclesPrefab, obstaclesRoot);
    }

    // Hooked by LevelFlow.Continue().
    public void LoadNext(bool loop)
    {
        var scene = SceneManager.GetActiveScene();
        if (scene.buildIndex < 0)
        {
            Debug.LogWarning("LoadNext needs '" + scene.name + "' to be listed and ticked in Build Settings.", this);
            return;
        }

        int index = CurrentIndex() + 1;
        if (allLevelsInOrder != null && index >= allLevelsInOrder.Length)
        {
            index = loop ? 0 : CurrentIndex();
        }

        pendingLevelIndex = index;
        SceneManager.LoadScene(scene.buildIndex);
    }

    // Hooked by LevelFlow.Replay().
    public void Reload()
    {
        var scene = SceneManager.GetActiveScene();
        if (scene.buildIndex < 0)
        {
            Debug.LogWarning("Reload needs '" + scene.name + "' to be listed and ticked in Build Settings.", this);
            return;
        }

        pendingLevelIndex = CurrentIndex();
        SceneManager.LoadScene(scene.buildIndex);
    }

    int CurrentIndex()
    {
        if (pendingLevelIndex >= 0) return pendingLevelIndex;
        if (allLevelsInOrder != null && currentLevel != null)
        {
            int found = System.Array.IndexOf(allLevelsInOrder, currentLevel);
            if (found >= 0) return found;
        }
        return 0;
    }
}
