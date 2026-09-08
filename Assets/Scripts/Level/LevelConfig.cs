using UnityEngine;

[CreateAssetMenu(fileName = "LevelConfig", menuName = "Game/Level Config")]
public class LevelConfig : ScriptableObject
{
    public string levelId;
    public int levelIndex;

    // All obstacles for this level, grouped under one prefab and spawned into
    // ObstaclesRoot by LevelController. Null means the level has none (e.g. Level 1).
    public GameObject obstaclesPrefab;

    // Not read by anything yet — ball/hole placement and level timing are
    // currently identical across every level and stay scene-authored. Kept here
    // as placeholders so a level that needs to vary them later doesn't require
    // another migration.
    public Vector3 ballStartPosition;
    public Vector3 winningHolePosition;
    public float timeLimit;
}
