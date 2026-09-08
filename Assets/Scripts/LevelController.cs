using UnityEngine;

// Lives on the Level root in the Gameplay scene. Reads GameManager.currentLevel
// — set by LevelSelect before Gameplay was loaded — and spawns its obstacles
// into obstaclesRoot. No progression-tracking of its own: GameManager is the
// persistent source of truth for which level is active, since it lives in
// Bootstrap and survives every scene swap.
public class LevelController : MonoBehaviour
{
    public Transform obstaclesRoot;

    void Awake()
    {
        SpawnObstacles();
    }

    void SpawnObstacles()
    {
        var currentLevel = GameManager.Instance != null ? GameManager.Instance.currentLevel : null;
        if (currentLevel == null || currentLevel.obstaclesPrefab == null || obstaclesRoot == null) return;

        Instantiate(currentLevel.obstaclesPrefab, obstaclesRoot);
    }
}
