using System.Collections.Generic;
using UnityEngine;

// Authored reference set of modular_space (Kenney) tile prefabs used to build a
// room's floor/walls/corners at runtime (RoomController.BuildGeometry). Kept as
// a ScriptableObject asset (not Resources.Load by string path) so RoomController
// takes an explicit serialized reference - matches the project's "swap art later
// without touching consumers" pattern used by IFloorLayoutProvider.
[CreateAssetMenu(fileName = "RoomTileSet", menuName = "RogueLike/Room Tile Set")]
public class RoomTileSet : ScriptableObject, IValidatable
{
    [SerializeField] private GameObject floorTile;
    [SerializeField] private GameObject wallTile;
    [SerializeField] private GameObject wallHalfTile;
    [SerializeField] private GameObject cornerTile;

    public GameObject FloorTile => floorTile;
    public GameObject WallTile => wallTile;
    public GameObject WallHalfTile => wallHalfTile;
    public GameObject CornerTile => cornerTile;

    public void Validate(List<string> errors)
    {
        if (floorTile == null)
        {
            errors.Add("Floor Tile is not assigned.");
        }

        if (wallTile == null)
        {
            errors.Add("Wall Tile is not assigned.");
        }

        if (wallHalfTile == null)
        {
            errors.Add("Wall Half Tile is not assigned.");
        }

        if (cornerTile == null)
        {
            errors.Add("Corner Tile is not assigned.");
        }
    }

    private void OnValidate()
    {
        DataValidation.LogIssues(this, this);
    }
}
