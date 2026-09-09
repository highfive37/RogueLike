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
//
// Multi-floor progression: the floor list comes from GameFloors (one FloorLayout per
// FloorN.json under StreamingAssets/Balance/Stage/Floors/). AdvanceToNextFloor() tears down the
// current floor's room instances and rebuilds from the next layout IN THE SAME SCENE - no
// reload. This is deliberate: reloading the scene (like the Retry button still does for the
// final floor) would recreate the Player GameObject, wiping
// PlayerPassiveStats/PlayerActiveItemInventory progress and re-triggering
// CharacterSelectController's one-time Awake() lock. Keeping the Player alive across the
// transition avoids both problems for free.
public class StageManager : MonoBehaviour
{
    private const float PlayerStandHeight = 1f;

    // Stage index for the "stage-floor" HUD label (e.g. 1-3). Only one stage exists today, so
    // this is just authored to 1; it stays a field so a second stage can bump it without code.
    [SerializeField] private int stageNumber = 1;
    [SerializeField] private RoomController roomPrefab;
    [SerializeField] private EnemySpawner enemySpawner;
    [SerializeField] private ItemDropManager itemDropManager;
    [SerializeField] private RoomTransitionController roomTransitionController;
    [SerializeField] private MinimapUI minimapUI;
    [SerializeField] private float gridSpacing = 50f;

    // The Floor Exit is placed as a child of whichever room in this floor's graph is
    // RoomType.Boss, offset from that room's center by this local vector - not a per-room
    // authored thing like doors, since there's exactly one per floor.
    [SerializeField] private FloorExitController floorExitPrefab;
    [SerializeField] private Vector3 floorExitLocalOffset = new Vector3(10f, 0f, 0f);

    private IFloorLayoutProvider _provider;
    private IReadOnlyList<FloorLayout> _floors;
    private readonly Dictionary<int, RoomController> _roomsById = new Dictionary<int, RoomController>();
    private int _currentFloorIndex;
    private float _runStartTime;
    private float _floorStartTime;
    private int _cumulativeRoomsCleared;

    public event Action<int> StageChanged;
    public event Action RoomClearedForReward;

    // Fires when the player enters a floor whose FloorN.json failed to load and is running on a
    // built-in emergency layout - UIManager turns this into a "fix the data file" system popup.
    public event Action<int> FloorDataCorrupted;

    // Fires the instant the boss dies (room clears) - drives guaranteed reward spawn + Floor
    // Exit activation, but is NOT the end of the run. See FloorCompleted below.
    public event Action<RoomController> BossRoomCleared;

    // Fires only when the player actually walks into the (by-then-activated) Floor Exit - this
    // is the real "run is over" moment. Kept as the same name/shape it always had so
    // RoomFlowManager's existing subscription doesn't need to change, only what triggers it.
    public event Action FloorCompleted;

    public FloorExitController FloorExit { get; private set; }
    public bool IsLastFloor => _currentFloorIndex >= _floors.Count - 1;

    // 1-based, for display (StageChanged still carries the raw room id - useful internally for
    // per-room tracking - so the HUD reads this instead of that event's payload).
    public int CurrentFloorNumber => _currentFloorIndex + 1;

    // Stage index for the "stage-floor" HUD label (1-1, 1-2, ...). Constant at 1 for now.
    public int CurrentStageNumber => stageNumber;

    // Per-floor stats, reset on AdvanceToNextFloor() - what the Floor Complete screen shows.
    public int RoomsClearedCount { get; private set; }
    public float ElapsedTime => Time.time - _floorStartTime;

    // Whole-run cumulative totals, never reset - not surfaced in any UI yet, kept for when a
    // full-run summary is built later (see the UI Scene work in the project roadmap).
    public int TotalRoomsCleared => _cumulativeRoomsCleared;
    public float TotalElapsedTime => Time.time - _runStartTime;

    private void Awake()
    {
        _runStartTime = Time.time;
        _floorStartTime = Time.time;
        _floors = BuildFloorSource().Floors;
        BuildProvider();
    }

    // Picks the floor graph source for this run. Only JsonFloorSource exists so
    // far, so today this is always the hand-authored FloorN.json data;
    // ProceduralFloorSource joins the switch in a later step and becomes the
    // default (RunConfig.FloorSource decides).
    private IFloorSource BuildFloorSource()
    {
        RunConfig run = GameFlow.EnsureRun();
        switch (run.FloorSource)
        {
            case FloorSourceMode.Json:
            default:
                return new JsonFloorSource();
        }
    }

    private void Start()
    {
        BuildFloor();
    }

    // Tears down the current floor's rooms and builds the next one from floors[] in place -
    // see the class comment for why this doesn't reload the scene.
    public void AdvanceToNextFloor()
    {
        if (IsLastFloor)
        {
            Debug.LogWarning("AdvanceToNextFloor called on the last floor - ignoring.");
            return;
        }

        TeardownCurrentFloor();

        _currentFloorIndex++;
        RoomsClearedCount = 0;
        _floorStartTime = Time.time;

        BuildProvider();
        BuildFloor();
    }

    private void BuildProvider()
    {
        _provider = new JsonFloorLayoutProvider(_floors[_currentFloorIndex]);
    }

    private void TeardownCurrentFloor()
    {
        foreach (RoomController room in _roomsById.Values)
        {
            room.RoomEntered -= HandleRoomEntered;
            room.Cleared -= HandleRoomCleared;
            Destroy(room.gameObject);
        }
        _roomsById.Clear();

        if (FloorExit != null)
        {
            FloorExit.Entered -= HandleFloorExitEntered;
            Destroy(FloorExit.gameObject);
            FloorExit = null;
        }
    }

    private void BuildFloor()
    {
        foreach (RoomData data in _provider.AllRooms)
        {
            Vector3 worldPosition = new Vector3(data.GridPosition.x * gridSpacing, 0f, data.GridPosition.y * gridSpacing);
            RoomController instance = Instantiate(roomPrefab, worldPosition, Quaternion.identity, transform);
            instance.Initialize(data, enemySpawner, itemDropManager);
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

        SpawnFloorExit();

        RoomController startRoom = _roomsById[_provider.StartRoomId];
        roomTransitionController.PlacePlayerAt(startRoom.transform.position + Vector3.up * PlayerStandHeight);
        startRoom.EnterRoom();

        // Flush the just-created room colliders and the player teleport into the physics scene
        // now. AdvanceToNextFloor() runs while Time.timeScale is 0 (the Floor Complete blackout),
        // so no FixedUpdate - and therefore no automatic physics sync - happens before the player
        // starts moving on the new floor. With Auto Sync Transforms off (project setting) that
        // left every rebuilt floor's walls non-collidable until the next simulation step.
        Physics.SyncTransforms();

        if (_floors[_currentFloorIndex].IsCorrupted)
        {
            FloorDataCorrupted?.Invoke(CurrentFloorNumber);
        }
    }

    private void SpawnFloorExit()
    {
        if (floorExitPrefab == null)
        {
            return;
        }

        foreach (RoomController room in _roomsById.Values)
        {
            if (room.RoomType != RoomType.Boss)
            {
                continue;
            }

            FloorExit = Instantiate(floorExitPrefab, room.transform);
            FloorExit.transform.localPosition = floorExitLocalOffset;
            FloorExit.Entered += HandleFloorExitEntered;
            return;
        }
    }

    private void HandleRoomEntered(RoomController room)
    {
        StageChanged?.Invoke(room.RoomId);
    }

    private void HandleRoomCleared(RoomController room)
    {
        RoomsClearedCount++;
        _cumulativeRoomsCleared++;

        if (room.RoomType == RoomType.Boss)
        {
            BossRoomCleared?.Invoke(room);
        }
        else if (room.RoomType == RoomType.Combat)
        {
            RoomClearedForReward?.Invoke();
        }
    }

    private void HandleFloorExitEntered()
    {
        FloorCompleted?.Invoke();
    }
}
