using System;
using System.Collections.Generic;

// "Is this room graph structurally usable" check, shared by every floor source:
// GameFloors (validating a parsed FloorN.json) and ProceduralFloorGenerator
// (self-checking its own output before returning it). A dangling or disconnected
// graph reads as a run softlock in play - a room you can't leave, or can never
// enter - so it is caught here rather than shipped.
public static class FloorGraph
{
    // Throws on the first structural fault found. Callers that want to fall back
    // (parse -> emergency layout, generate -> retry) catch it.
    public static void Validate(IReadOnlyList<RoomData> rooms, int startRoomId)
    {
        if (rooms == null || rooms.Count == 0)
        {
            throw new Exception("floor has no rooms.");
        }

        var byId = new Dictionary<int, RoomData>(rooms.Count);
        foreach (RoomData room in rooms)
        {
            if (!byId.TryAdd(room.RoomId, room))
            {
                throw new Exception($"duplicate roomId {room.RoomId}.");
            }
        }

        if (!byId.ContainsKey(startRoomId))
        {
            throw new Exception($"startRoomId {startRoomId} is not one of the floor's rooms.");
        }

        // Every connection must point at a real room.
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

        // Every room must be reachable from the start room by following connections.
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
}
