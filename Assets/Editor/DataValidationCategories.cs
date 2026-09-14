using UnityEngine;

// Maps each authored ScriptableObject data type to a coarse domain bucket purely for
// DataValidationWindow's asset list grouping - editor-presentation concern only, so it lives here
// rather than on the runtime types themselves. Add a case here whenever a new SO base type is
// introduced; everything else falls into "Other" instead of failing to appear.
internal static class DataValidationCategories
{
    public const string Player = "Player";
    public const string Enemy = "Enemy";
    public const string Items = "Items";
    public const string Room = "Room";
    public const string Other = "Other";

    // Display order in the asset list - not alphabetical, reads the way a designer thinks about
    // the game's systems (who's acting, what they fight, what they pick up, where it happens).
    public static readonly string[] Order = { Player, Enemy, Items, Room, Other };

    public static string Categorize(ScriptableObject asset)
    {
        switch (asset)
        {
            case WeaponData _:
            case CharacterArchetypeData _:
                return Player;
            case EnemyData _:
            case AttackPatternData _:
                return Enemy;
            case ItemData _:
                return Items;
            case RoomTileSet _:
            case RoomObstacleSet _:
                return Room;
            default:
                return Other;
        }
    }
}
