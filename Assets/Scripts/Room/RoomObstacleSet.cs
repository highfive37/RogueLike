using System.Collections.Generic;
using UnityEngine;

// Authored reference set of standalone obstacle prefabs RoomController instantiates from
// at Initialize() time: Prefabs is for Solo-kind placements (today just Block_Stone from
// cube_world_kit, a single plain block placed with a random facing) and ConnectorPrefab
// is the one thin segment shape used for every Connector-kind placement (Block_Stone
// rescaled to 1x4x4, see Docs/AI_Decision_Log.md 2026-09-12 "얇은 연결 세그먼트로 전환") -
// both share the same "same mesh repeated, no per-piece catalog" philosophy. Two earlier
// solo lineups were tried and dropped: cyberpunk_game_kit furniture (AC/Fence/Computer/
// Support - read as out-of-place set dressing) and, briefly, Rock1/Rock2 plus the 4
// fence-connector meshes reused standalone (the connector shapes read as "cut off" rail
// segments when not actually connected to anything). Same "explicit serialized
// reference, not Resources.Load by string path" pattern as RoomTileSet. Every prefab is
// expected to already carry a solid, non-trigger collider with no Rigidbody - that alone
// is enough to block the player (WallClamp treats any such collider like a wall) and
// projectiles (Projectile.OnTriggerEnter reacts to any non-owner, non-projectile
// collider), exactly like the existing wall tiles.
[CreateAssetMenu(fileName = "RoomObstacleSet", menuName = "RogueLike/Room Obstacle Set")]
public class RoomObstacleSet : ScriptableObject
{
    [SerializeField] private GameObject[] prefabs;
    [SerializeField] private GameObject connectorPrefab;

    public IReadOnlyList<GameObject> Prefabs => prefabs;
    public GameObject ConnectorPrefab => connectorPrefab;
}
