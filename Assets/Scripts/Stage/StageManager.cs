using System;
using System.Collections.Generic;
using UnityEngine;

// Builds and tracks a floor's room graph using data from an
// IFloorLayoutProvider. The concrete provider is ScriptableObject-backed
// today (a hand-authored fixed graph, Stage 2.5); a random/procedural
// provider can replace it later without this class changing. All rooms of
// the floor are instantiated up front and pre-placed on a world grid (grid
// cell pitch is fixed via gridSpacing; each room's own visual footprint
// comes from its RoomData.RoomSize, so a smaller room just leaves more empty
// space around it) so the player can freely backtrack into already-cleared
// rooms - only the room the player is currently in ever has active enemies,
// tracked per-instance by each room's own RoomManager.
public class StageManager : MonoBehaviour
{
    [SerializeField] private RoomDataAsset[] roomAssets;
    [SerializeField] private int startRoomId;
    [SerializeField] private RoomController roomPrefab;
    [SerializeField] private EnemySpawner enemySpawner;
    [SerializeField] private RoomTransitionController roomTransitionController;
    [SerializeField] private MinimapUI minimapUI;
    [SerializeField] private float gridSpacing = 50f;

    private IFloorLayoutProvider _provider;
    private readonly Dictionary<int, RoomController> _roomsById = new Dictionary<int, RoomController>();

    public event Action<int> StageChanged;
    public event Action RoomClearedForReward;
    public event Action FloorCompleted;

    public bool IsCurrentStageFinal { get; private set; }

    private void Awake()
    {
        _provider = new ScriptableObjectFloorLayoutProvider(roomAssets, startRoomId);
    }

    private void Start()
    {
        BuildFloor();
    }

    private void BuildFloor()
    {
        foreach (RoomData data in _provider.AllRooms)
        {
            Vector3 worldPosition = new Vector3(data.GridPosition.x * gridSpacing, 0f, data.GridPosition.y * gridSpacing);
            RoomController instance = Instantiate(roomPrefab, worldPosition, Quaternion.identity, transform);
            instance.Initialize(data, enemySpawner);
            instance.RoomEntered += HandleRoomEntered;
            instance.Cleared += HandleRoomCleared;
            _roomsById[data.RoomId] = instance;
        }

        foreach (RoomData data in _provider.AllRooms)
        {
            RoomController room = _roomsById[data.RoomId];
            foreach (RoomDoorConnection connection in data.Connections)
            {
                if (_roomsById.TryGetValue(connection.TargetRoomId, out RoomController target))
                {
                    room.ConnectDoor(connection.Direction, target);
                }
                else
                {
                    Debug.LogWarning($"Room {data.RoomId} connects to missing room id {connection.TargetRoomId}.");
                }
            }
        }

        roomTransitionController.Initialize(_roomsById.Values);
        minimapUI.Initialize(_roomsById.Values);

        _roomsById[_provider.StartRoomId].EnterRoom();
    }

    private void HandleRoomEntered(RoomController room)
    {
        StageChanged?.Invoke(room.RoomId);
    }

    private void HandleRoomCleared(RoomController room)
    {
        if (room.RoomType == RoomType.Boss)
        {
            IsCurrentStageFinal = true;
            FloorCompleted?.Invoke();
        }
        else if (room.RoomType == RoomType.Combat)
        {
            RoomClearedForReward?.Invoke();
        }
    }
}
