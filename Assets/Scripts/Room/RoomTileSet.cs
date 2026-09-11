using UnityEngine;

// Authored reference set of modular_space (Kenney) tile prefabs used to build a
// room's floor/walls/corners at runtime (RoomController.BuildGeometry). Kept as
// a ScriptableObject asset (not Resources.Load by string path) so RoomController
// takes an explicit serialized reference - matches the project's "swap art later
// without touching consumers" pattern used by IFloorLayoutProvider.
[CreateAssetMenu(fileName = "RoomTileSet", menuName = "RogueLike/Room Tile Set")]
public class RoomTileSet : ScriptableObject
{
    [SerializeField] private GameObject floorTile;
    [SerializeField] private GameObject wallTile;
    [SerializeField] private GameObject wallHalfTile;
    [SerializeField] private GameObject cornerTile;

    public GameObject FloorTile => floorTile;
    public GameObject WallTile => wallTile;
    public GameObject WallHalfTile => wallHalfTile;
    public GameObject CornerTile => cornerTile;
}
