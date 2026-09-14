using UnityEngine;
using UnityEngine.Serialization;

[CreateAssetMenu(fileName = "LevelConfig", menuName = "Game/Level Config")]
public class LevelConfig : ScriptableObject
{
    public string levelId;  
    public int levelIndex;

    // Everything placed in the level — the winning hole and every obstacle —
    // grouped under one prefab and spawned into LevelController.layoutRoot in
    // absolute world coordinates. A level with no layout has nothing to win
    // with, so only a design scene ever runs without one.
    [FormerlySerializedAs("obstaclesPrefab")]
    public GameObject layoutPrefab;

    // Art behind the level, covering whatever the camera fit leaves beyond the
    // play area. Null keeps the scene's default background.
    public Sprite backgroundSprite;

    // Not read by anything yet — ball placement and level timing are currently
    // identical across every level and stay scene-authored. Kept here as
    // placeholders so a level that needs to vary them later doesn't require
    // another migration.
    public Vector3 ballStartPosition;
    public float timeLimit;

    [Header("Level size")]
    // Vertical extent of the play area, in world units. The one number that
    // decides whether a level plays as Classic or Tall: 0 (or anything up to
    // one screenful — ScreenFitProfile.designLength) is a Classic level, the
    // scene exactly as authored. Anything larger is a Tall level: the stick's
    // travel, the pulleys and the ceiling all rise by the surplus, and the
    // camera scrolls to follow the climb. The hole doesn't move — it's wherever
    // the layout put it.
    public float levelLength = 0f;
}
