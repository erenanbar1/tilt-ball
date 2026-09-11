using UnityEngine;

// The ordered level lists LevelManager plays from. Never edited by hand: the
// editor rebuilds it from every LevelConfig under Assets/Levels/Configs
// whenever one is added, removed or moved (LevelCatalogBuilder), sorting by
// levelIndex and splitting Classic/Tall by climbHeight.
public class LevelCatalog : ScriptableObject
{
    public LevelConfig[] classicLevels;
    public LevelConfig[] tallLevels;

    public LevelConfig[] LevelsFor(GameMode mode) => mode == GameMode.Tall ? tallLevels : classicLevels;
}
