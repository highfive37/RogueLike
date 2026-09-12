using UnityEngine;

// Which of the two obstacle shapes a placement instantiates. There is still no
// per-direction piece catalog (see Docs/AI_Decision_Log.md 2026-09-12 "조각 선택 없이
// 블록 반복으로 단순화") - every Solo placement is the same full-tile block, and every
// Connector placement is the same thin segment, just with one of 2 fixed facings. The
// shape of a connected run of cells comes entirely from how many Connector segments a
// pattern's adjacent fence cells produce, not from any per-cell mesh decision.
public enum ObstacleShape
{
    Solo,
    Connector,
}

// One obstacle instance in a room, in the same tile-grid convention RoomController's
// BuildFloor already uses (LocalCell.x/y are the ix/iz floor-tile indices, not world
// units) - RoomController converts to a local position at build time, and the same
// cell coordinate is what a future grid-based A* would mark blocked, with no extra
// translation needed.
//
// Solo: LocalCell is the cell the full-tile block sits on; RotationY is a random
// facing (0/90/180/270) purely for visual variety.
//
// Connector: LocalCell is the lower of the 2 cells the thin segment spans (smaller x,
// then smaller z) - see Docs/AI_Decision_Log.md 2026-09-12 "얇은 연결 세그먼트로 전환".
// RotationY is not random here, it encodes which of the 2 possible neighbours the
// segment reaches: 90 means it spans to LocalCell + (1,0) (east, along local X), 0
// means it spans to LocalCell + (0,1) (north, along local Z). A junction cell (corner/
// T/cross) simply gets more than one Connector placement sharing the same LocalCell,
// one per direction it connects - the pieces overlap at the shared cell and read as one
// continuous shape with no corner/T-piece mesh needed.
[System.Serializable]
public class ObstaclePlacement
{
    [SerializeField] private ObstacleShape shape;
    [SerializeField] private Vector2Int localCell;
    [SerializeField] private int variantIndex;
    [SerializeField] private float rotationY;

    public ObstacleShape Shape => shape;
    public Vector2Int LocalCell => localCell;
    public int VariantIndex => variantIndex;
    public float RotationY => rotationY;

    public ObstaclePlacement(ObstacleShape shape, Vector2Int localCell, int variantIndex, float rotationY)
    {
        this.shape = shape;
        this.localCell = localCell;
        this.variantIndex = variantIndex;
        this.rotationY = rotationY;
    }
}
