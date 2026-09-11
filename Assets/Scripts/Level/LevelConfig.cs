using UnityEngine;

// One playable level. Create via Tools > Tilt Ball > New Classic/Tall Level
// (which also makes the obstacle prefab and registers the level) — see
// README "Adding a level". Classic vs Tall is decided by climbHeight alone.
[CreateAssetMenu(fileName = "LevelConfig", menuName = "Game/Level Config")]
public class LevelConfig : ScriptableObject
{
    // Position within its mode's list: unlock order and the number shown on
    // the level-select node (index + 1). The catalogue sorts by this.
    public int levelIndex;

    // All obstacles for this level, grouped under one prefab and spawned into
    // ObstaclesRoot by LevelController. Null means the level has none (e.g. Level 1).
    public GameObject obstaclesPrefab;

    // Art behind the play area. Null keeps the scene's default background.
    public Sprite backgroundSprite;

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

    public bool IsTall => climbHeight > 0f;
    public GameMode Mode => IsTall ? GameMode.Tall : GameMode.Classic;
}
