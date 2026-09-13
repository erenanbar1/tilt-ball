using UnityEngine;

[CreateAssetMenu(fileName = "LevelConfig", menuName = "Game/Level Config")]
public class LevelConfig : ScriptableObject
{
    public string levelId;  
    public int levelIndex;

    // All obstacles for this level, grouped under one prefab and spawned into
    // ObstaclesRoot by LevelController. Null means the level has none (e.g. Level 1).
    public GameObject obstaclesPrefab;

    // Art behind the level, covering whatever the camera fit leaves beyond the
    // play area. Null keeps the scene's default background.
    public Sprite backgroundSprite;

    // Not read by anything yet — ball/hole placement and level timing are
    // currently identical across every level and stay scene-authored. Kept here
    // as placeholders so a level that needs to vary them later doesn't require
    // another migration. (Longer levels don't need winningHolePosition: the hole
    // rides up with the summit, see LevelController.ApplyLevelGeometry.)
    public Vector3 ballStartPosition;
    public Vector3 winningHolePosition;
    public float timeLimit;

    [Header("Level size")]
    // Vertical extent of the play area, in world units. The one number that
    // decides whether a level plays as Classic or Tall: 0 (or anything up to
    // one screenful — ScreenFitProfile.designLength) is a Classic level, the
    // scene exactly as authored. Anything larger is a Tall level: the stick's
    // travel, the pulleys, the winning hole and the ceiling all rise by the
    // surplus, and the camera scrolls to follow the climb.
    public float levelLength = 0f;
}
