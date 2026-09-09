using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

// Sole source of truth for stage/floor layout - the room graph of every floor
// (grid positions, sizes, door connections, enemy composition), loaded from
// plain JSON under Assets/StreamingAssets/Balance/Stage/Floors/FloorN.json so a
// whole new floor can be authored with a text editor (copy FloorN.json ->
// FloorN+1.json, fill it in, flip the previous last floor's "isLastFloor" to
// false) without the Unity Editor or any code change.
//
// Same conventions as GameBalance: lazy-loaded on first access, reset once per
// Play session via RuntimeInitializeOnLoadMethod, enum fields authored as name
// strings and parsed with Enum.TryParse, unknown JSON keys (_guide/_fieldGuide)
// ignored. A missing or malformed FloorN.json degrades rather than crashes: that
// one floor is replaced by a built-in 3-room emergency layout and flagged
// IsCorrupted so StageManager can surface a system popup when the player reaches
// it - the rest of the run is unaffected.
//
// Floor count = how many FloorN.json exist, counting up from Floor1 until one is
// missing. "Which floor is last" is decided by that position (the last existing
// file), NOT by the authored "isLastFloor" flag - the flag is validated against
// position and a mismatch only logs a warning. This keeps a forgotten flag flip
// from stranding the player when a new last floor is added.
public static class GameFloors
{
    private const string FloorsFolder = "Balance/Stage/Floors";
    private const int MaxFloors = 99; // safety cap on the FloorN.json enumeration

    private static bool _loaded;
    private static List<FloorLayout> _floors;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void ResetForNewSession()
    {
        _loaded = false;
        _floors = null;
    }

    public static IReadOnlyList<FloorLayout> Floors
    {
        get
        {
            EnsureLoaded();
            return _floors;
        }
    }

    private static void EnsureLoaded()
    {
        if (_loaded)
        {
            return;
        }

        _loaded = true;
        _floors = LoadAllFloors();
    }

    private static List<FloorLayout> LoadAllFloors()
    {
        return LoadAllFloorsFrom(Path.Combine(Application.streamingAssetsPath, FloorsFolder));
    }

    // Split from LoadAllFloors so EditMode tests can point it at a fixture directory instead of
    // the real StreamingAssets tree.
    internal static List<FloorLayout> LoadAllFloorsFrom(string dir)
    {
        // Discover FloorN.json, N = 1..MaxFloors, stopping at the first gap.
        var paths = new List<string>();
        for (int n = 1; n <= MaxFloors; n++)
        {
            string path = Path.Combine(dir, $"Floor{n}.json");
            if (File.Exists(path))
            {
                paths.Add(path);
                continue;
            }

            // A file past the gap? Almost certainly a misnamed file - warn, but still stop here.
            if (File.Exists(Path.Combine(dir, $"Floor{n + 1}.json")))
            {
                Debug.LogWarning($"GameFloors: Floor{n}.json is missing but Floor{n + 1}.json exists - every floor after the gap is ignored. Fix the file numbering.");
            }

            break;
        }

        var result = new List<FloorLayout>();

        if (paths.Count == 0)
        {
            Debug.LogError($"GameFloors: no FloorN.json found under '{dir}'. Falling back to a single built-in emergency floor - the game runs but has no authored content.");
            result.Add(EmergencyFloor(1, isLast: true));
            return result;
        }

        for (int i = 0; i < paths.Count; i++)
        {
            result.Add(LoadFloor(paths[i], floorNumber: i + 1, isLastByPosition: i == paths.Count - 1));
        }

        return result;
    }

    private static FloorLayout LoadFloor(string path, int floorNumber, bool isLastByPosition)
    {
        string json;
        try
        {
            json = File.ReadAllText(path);
        }
        catch (Exception e)
        {
            Debug.LogError($"GameFloors: failed to read '{path}' ({e.Message}). Using a built-in emergency layout for floor {floorNumber} - fix the file and re-run.");
            return EmergencyFloor(floorNumber, isLastByPosition);
        }

        return ParseFloorJson(json, floorNumber, isLastByPosition);
    }

    // Parse + structural validation of one floor's JSON, split from file I/O so EditMode tests
    // can feed strings directly. Any structural fault (bad JSON, missing Start/Boss, duplicate
    // or dangling room ids, an unreachable room) degrades to the emergency layout with
    // IsCorrupted = true rather than throwing out of here.
    internal static FloorLayout ParseFloorJson(string json, int floorNumber, bool isLastByPosition)
    {
        try
        {
            FloorDto dto = JsonUtility.FromJson<FloorDto>(json);
            if (dto == null)
            {
                throw new Exception("parsed floor was null.");
            }

            if (dto.rooms == null || dto.rooms.Length == 0)
            {
                throw new Exception("floor has no rooms.");
            }

            // File order is authoritative for "is last"; the authored flag is only checked.
            if (dto.isLastFloor != isLastByPosition)
            {
                Debug.LogWarning($"GameFloors: Floor{floorNumber}.json has \"isLastFloor\": {dto.isLastFloor.ToString().ToLowerInvariant()}, but by file order it is {(isLastByPosition ? "" : "not ")}the last floor. Using file order - update the flag to match.");
            }

            var rooms = new List<RoomData>(dto.rooms.Length);
            var seenIds = new HashSet<int>();
            foreach (RoomDto roomDto in dto.rooms)
            {
                RoomData room = ConvertRoom(roomDto);
                if (!seenIds.Add(room.RoomId))
                {
                    throw new Exception($"duplicate roomId {room.RoomId}.");
                }

                rooms.Add(room);
            }

            bool hasStart = false;
            bool hasBoss = false;
            bool startRoomExists = false;
            foreach (RoomData room in rooms)
            {
                hasStart |= room.Type == RoomType.Start;
                hasBoss |= room.Type == RoomType.Boss;
                startRoomExists |= room.RoomId == dto.startRoomId;
            }

            if (!hasStart)
            {
                throw new Exception("floor has no Start room.");
            }

            if (!hasBoss)
            {
                throw new Exception("floor has no Boss room (the Floor Exit is placed there).");
            }

            if (!startRoomExists)
            {
                throw new Exception($"startRoomId {dto.startRoomId} is not one of the floor's rooms.");
            }

            ValidateGraph(rooms, dto.startRoomId);

            return new FloorLayout(floorNumber, isLastByPosition, isCorrupted: false, dto.startRoomId, rooms);
        }
        catch (Exception e)
        {
            Debug.LogError($"GameFloors: failed to parse floor {floorNumber} ({e.Message}). Using a built-in emergency layout - fix the file and re-run.");
            return EmergencyFloor(floorNumber, isLastByPosition);
        }
    }

    // Every connection must point at a real room, and every room must be reachable from the
    // start room by following connections. A dangling or disconnected graph reads as a softlock
    // in play (a room you can't leave, or can never enter) rather than a crash, so it's caught
    // here and treated like any other structural fault -> emergency floor.
    private static void ValidateGraph(List<RoomData> rooms, int startRoomId)
    {
        var byId = new Dictionary<int, RoomData>(rooms.Count);
        foreach (RoomData room in rooms)
        {
            byId[room.RoomId] = room;
        }

        foreach (RoomData room in rooms)
        {
            foreach (RoomDoorConnection conn in room.Connections)
            {
                if (!byId.ContainsKey(conn.TargetRoomId))
                {
                    throw new Exception($"room {room.RoomId} connects to non-existent room {conn.TargetRoomId}.");
                }
            }
        }

        var reached = new HashSet<int> { startRoomId };
        var queue = new Queue<int>();
        queue.Enqueue(startRoomId);
        while (queue.Count > 0)
        {
            foreach (RoomDoorConnection conn in byId[queue.Dequeue()].Connections)
            {
                if (reached.Add(conn.TargetRoomId))
                {
                    queue.Enqueue(conn.TargetRoomId);
                }
            }
        }

        if (reached.Count != rooms.Count)
        {
            throw new Exception($"{rooms.Count - reached.Count} room(s) unreachable from start room {startRoomId} by following connections.");
        }
    }

    private static RoomData ConvertRoom(RoomDto dto)
    {
        if (!TryParseEnum(dto.roomType, out RoomType type))
        {
            throw new Exception($"room {dto.roomId}: unknown roomType '{dto.roomType}' (expected Start/Combat/Boss/Item).");
        }

        float size = dto.roomSize > 0f ? dto.roomSize : 40f;

        var enemies = new List<StageEnemyEntry>();
        if (dto.enemies != null)
        {
            foreach (EnemyDto enemyDto in dto.enemies)
            {
                if (!TryParseEnum(enemyDto.variant, out EnemyVariantType variant))
                {
                    throw new Exception($"room {dto.roomId}: unknown enemy variant '{enemyDto.variant}' (expected Melee/Ranged/Boss).");
                }

                enemies.Add(new StageEnemyEntry(variant, enemyDto.spawnPosition));
            }
        }

        var connections = new List<RoomDoorConnection>();
        if (dto.connections != null)
        {
            foreach (ConnectionDto connectionDto in dto.connections)
            {
                if (!TryParseEnum(connectionDto.direction, out DoorDirection direction))
                {
                    throw new Exception($"room {dto.roomId}: unknown door direction '{connectionDto.direction}' (expected North/South/East/West).");
                }

                connections.Add(new RoomDoorConnection(direction, connectionDto.targetRoomId));
            }
        }

        return new RoomData(dto.roomId, type, dto.gridPosition, size, enemies, connections);
    }

    private static bool TryParseEnum<T>(string value, out T result) where T : struct
    {
        return Enum.TryParse(value, out result) && Enum.IsDefined(typeof(T), result);
    }

    // Start --East-- Combat --East-- Boss in one row. Room ids are namespaced by
    // floor (floor*1000 + n) so an emergency floor never collides with a real one
    // that loaded fine on either side of it.
    private static FloorLayout EmergencyFloor(int floorNumber, bool isLast)
    {
        int baseId = floorNumber * 1000;
        int startId = baseId + 1;
        int combatId = baseId + 2;
        int bossId = baseId + 3;

        var start = new RoomData(startId, RoomType.Start, new Vector2Int(0, 0), 16f,
            Array.Empty<StageEnemyEntry>(),
            new[] { new RoomDoorConnection(DoorDirection.East, combatId) });

        var combat = new RoomData(combatId, RoomType.Combat, new Vector2Int(1, 0), 40f,
            new[]
            {
                new StageEnemyEntry(EnemyVariantType.Melee, new Vector3(6f, 1f, 0f)),
                new StageEnemyEntry(EnemyVariantType.Ranged, new Vector3(-6f, 1f, 0f)),
            },
            new[]
            {
                new RoomDoorConnection(DoorDirection.West, startId),
                new RoomDoorConnection(DoorDirection.East, bossId),
            });

        var boss = new RoomData(bossId, RoomType.Boss, new Vector2Int(2, 0), 40f,
            new[] { new StageEnemyEntry(EnemyVariantType.Boss, new Vector3(-10f, 1.5f, 10f)) },
            new[] { new RoomDoorConnection(DoorDirection.West, combatId) });

        return new FloorLayout(floorNumber, isLast, isCorrupted: true, startId,
            new List<RoomData> { start, combat, boss });
    }

    // JsonUtility DTOs - enum-typed fields are strings on purpose (JsonUtility
    // only understands enums as integers; Enum.TryParse in ConvertRoom does the
    // name -> enum step, same as GameBalance does for PassiveItemType). Vector2Int
    // / Vector3 round-trip natively as { "x":.., "y":.. [, "z":..] }.
    [Serializable]
    private class FloorDto
    {
        public bool isLastFloor;
        public int startRoomId;
        public RoomDto[] rooms;
    }

    [Serializable]
    private class RoomDto
    {
        public int roomId;
        public string roomType;
        public Vector2Int gridPosition;
        public float roomSize;
        public EnemyDto[] enemies;
        public ConnectionDto[] connections;
    }

    [Serializable]
    private class EnemyDto
    {
        public string variant;
        public Vector3 spawnPosition;
    }

    [Serializable]
    private class ConnectionDto
    {
        public string direction;
        public int targetRoomId;
    }
}
