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
    // another migration. (Tall levels don't need winningHolePosition: the hole
    // rides up with the summit, see LevelController.ApplyLevelGeometry.)
    public Vector3 ballStartPosition;
    public Vector3 winningHolePosition;
    public float timeLimit;

    [Header("Tall levels")]
    // How far each end of the stick may rise above its spawn height — becomes
    // StickController.maxOffset. Zero means "leave whatever the scene authored",
    // which is what every Classic level wants.
    public float climbHeight = 0f;
    // World Y of the level's bottom edge. With climbHeight it gives the camera
    // the column it may scroll within.
    public float levelFloorY = -7f;
    // Headroom above the stick's highest reachable point, so the summit isn't
    // flush against the top of the frame.
    public float ceilingPadding = 0.75f;
}
