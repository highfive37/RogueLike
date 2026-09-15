using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

// Enter the Gungeon-style minimap: a room stays hidden until a room next to it has been visited,
// at which point only a connector line hints it exists - no silhouette until the player actually
// walks in and it shows its real type/cleared color (2026-09-15: Discovered rooms used to render
// a dim type-less icon too; user feedback dropped that, keeping just the connector as the "there's
// something over there" hint). Wired exactly like
// RoomTransitionController - StageManager hands over the full room list
// once the floor is built, and this subscribes directly to each room's
// RoomEntered/Cleared events plus each door's TargetRoom to build the
// connector graph. This component's own RectTransform is a small fixed-size
// viewport (RectMask2D clips anything outside it); icons/connectors live at
// fixed grid-based positions under a "Content" child that gets re-centered
// every time the player enters a new room, so the current room's icon
// always sits at the viewport's center and only nearby rooms are visible -
// a local scrolling minimap rather than the whole floor at once.
public class MinimapUI : MonoBehaviour
{
    private enum RevealState
    {
        Hidden,
        Discovered,
        Visited
    }

    [SerializeField] private GameObject iconPrefab;
    [SerializeField] private GameObject connectorPrefab;
    [SerializeField] private float cellSpacing = 40f;
    [SerializeField] private float connectorThickness = 4f;

    // Matches MinimapIcon.prefab's authored RectTransform.sizeDelta - a square/
    // rectangle room's longer side maps to exactly this (same footprint as the
    // old fixed-size icon), the shorter side shrinks proportionally.
    private const float IconBaseSize = 24f;

    [SerializeField] private Color discoveredColor = new Color(0.35f, 0.35f, 0.35f, 0.7f);
    [SerializeField] private Color currentColor = Color.yellow;
    [SerializeField] private Color clearedColor = new Color(0.25f, 0.8f, 0.35f, 1f);
    [SerializeField] private Color startClearedColor = new Color(0.45f, 0.45f, 0.85f, 1f);
    [SerializeField] private Color bossClearedColor = new Color(0.75f, 0.25f, 0.25f, 1f);
    [SerializeField] private Color itemClearedColor = new Color(0.85f, 0.75f, 0.2f, 1f);
    [SerializeField] private Color connectorColor = new Color(0.6f, 0.6f, 0.6f, 0.6f);

    private readonly Dictionary<int, RoomController> _roomsById = new Dictionary<int, RoomController>();
    // 1 image for a square/rectangle room, 2 for an L-shape (its two non-overlapping
    // sub-rectangles - see BuildIcons) - all images of a room always share one color/
    // visibility state, so every read/write below just loops the array.
    private readonly Dictionary<int, Image[]> _iconsByRoomId = new Dictionary<int, Image[]>();
    private readonly Dictionary<int, RevealState> _revealByRoomId = new Dictionary<int, RevealState>();
    private readonly Dictionary<(int, int), Image> _connectorsByPair = new Dictionary<(int, int), Image>();

    private int _currentRoomId = -1;
    private RectTransform _content;

    // Safe to call more than once (StageManager.AdvanceToNextFloor rebuilds the minimap for
    // each new floor) - clears out the previous floor's icons/connectors/subscriptions first.
    public void Initialize(IEnumerable<RoomController> rooms)
    {
        ResetState();

        RoomController[] roomArray = rooms.ToArray();

        var contentObj = new GameObject("Content", typeof(RectTransform));
        _content = (RectTransform)contentObj.transform;
        _content.SetParent(transform, false);
        _content.anchorMin = new Vector2(0.5f, 0.5f);
        _content.anchorMax = new Vector2(0.5f, 0.5f);
        _content.pivot = new Vector2(0.5f, 0.5f);
        _content.sizeDelta = Vector2.zero;
        _content.anchoredPosition = Vector2.zero;

        foreach (RoomController room in roomArray)
        {
            _roomsById[room.RoomId] = room;
            _revealByRoomId[room.RoomId] = RevealState.Hidden;
            room.RoomEntered += HandleRoomEntered;
            room.Cleared += HandleRoomCleared;
        }

        BuildConnectors(roomArray);
        BuildIcons(roomArray);

        RefreshAll();
    }

    private void ResetState()
    {
        foreach (RoomController room in _roomsById.Values)
        {
            room.RoomEntered -= HandleRoomEntered;
            room.Cleared -= HandleRoomCleared;
        }

        if (_content != null)
        {
            Destroy(_content.gameObject);
            _content = null;
        }

        _roomsById.Clear();
        _iconsByRoomId.Clear();
        _revealByRoomId.Clear();
        _connectorsByPair.Clear();
        _currentRoomId = -1;
    }

    private Vector2 GridToLocal(Vector2 gridPosition)
    {
        return gridPosition * cellSpacing;
    }

    // One icon piece per room for a square/rectangle footprint (sized to its
    // Width/Depth aspect ratio instead of always a uniform square); an L-shape
    // footprint gets two pieces, decomposed into its two non-overlapping
    // sub-rectangles the same way RoomController's own BuildNotchWalls/
    // BuildNotchCorners think about the shape (see Docs/DevLog.md 2026-09-11) -
    // a full-width strip on the non-notched side, plus the remaining column
    // beside the notch.
    private void BuildIcons(IEnumerable<RoomController> rooms)
    {
        foreach (RoomController room in rooms)
        {
            RoomFootprint fp = room.Footprint;
            Vector2 basePos = GridToLocal(room.GridPosition);
            float scale = IconBaseSize / Mathf.Max(fp.Width, fp.Depth);

            if (fp.HasNotch)
            {
                int signX = fp.NotchSignX;
                int signZ = fp.NotchSignZ;

                Vector2 offsetA = new Vector2(0f, -signZ * fp.NotchDepth / 2f);
                Vector2 sizeA = new Vector2(fp.Width, fp.Depth - fp.NotchDepth);

                Vector2 offsetB = new Vector2(-signX * fp.NotchWidth / 2f, signZ * (fp.Depth - fp.NotchDepth) / 2f);
                Vector2 sizeB = new Vector2(fp.Width - fp.NotchWidth, fp.NotchDepth);

                _iconsByRoomId[room.RoomId] = new[]
                {
                    SpawnIconPiece($"RoomIcon_{room.RoomId}_A", basePos, offsetA, sizeA, scale),
                    SpawnIconPiece($"RoomIcon_{room.RoomId}_B", basePos, offsetB, sizeB, scale),
                };
            }
            else
            {
                _iconsByRoomId[room.RoomId] = new[]
                {
                    SpawnIconPiece($"RoomIcon_{room.RoomId}", basePos, Vector2.zero, new Vector2(fp.Width, fp.Depth), scale),
                };
            }
        }
    }

    private Image SpawnIconPiece(string name, Vector2 basePos, Vector2 offsetWorldUnits, Vector2 sizeWorldUnits, float scale)
    {
        GameObject iconObj = Instantiate(iconPrefab, _content);
        iconObj.name = name;

        RectTransform rt = (RectTransform)iconObj.transform;
        rt.anchoredPosition = basePos + offsetWorldUnits * scale;
        rt.sizeDelta = sizeWorldUnits * scale;

        return iconObj.GetComponent<Image>();
    }

    private void BuildConnectors(IEnumerable<RoomController> rooms)
    {
        var seenPairs = new HashSet<(int, int)>();

        foreach (RoomController room in rooms)
        {
            foreach (DoorController door in room.Doors)
            {
                if (door.TargetRoom == null)
                {
                    continue;
                }

                int a = room.RoomId;
                int b = door.TargetRoom.RoomId;
                (int, int) key = a < b ? (a, b) : (b, a);

                if (!seenPairs.Add(key))
                {
                    continue;
                }

                Vector2 posA = GridToLocal(room.GridPosition);
                Vector2 posB = GridToLocal(door.TargetRoom.GridPosition);
                Vector2 diff = posB - posA;

                GameObject connectorObj = Instantiate(connectorPrefab, _content);
                connectorObj.name = $"Connector_{a}_{b}";

                RectTransform rt = (RectTransform)connectorObj.transform;
                rt.anchoredPosition = (posA + posB) / 2f;
                rt.sizeDelta = new Vector2(diff.magnitude, connectorThickness);
                rt.localEulerAngles = new Vector3(0f, 0f, Mathf.Atan2(diff.y, diff.x) * Mathf.Rad2Deg);

                Image connectorImage = connectorObj.GetComponent<Image>();
                connectorImage.color = connectorColor;

                _connectorsByPair[key] = connectorImage;
            }
        }
    }

    private void HandleRoomEntered(RoomController room)
    {
        _currentRoomId = room.RoomId;
        Reveal(room.RoomId, RevealState.Visited);

        foreach (DoorController door in room.Doors)
        {
            if (door.TargetRoom != null)
            {
                Reveal(door.TargetRoom.RoomId, RevealState.Discovered);
            }
        }

        // Shift the whole content container so the current room's icon (which sits at
        // GridToLocal(room.GridPosition) in content-local space) lands exactly on the
        // viewport's center - a local scrolling minimap instead of a static full-floor view.
        _content.anchoredPosition = -GridToLocal(room.GridPosition);

        RefreshAll();
    }

    private void HandleRoomCleared(RoomController room)
    {
        RefreshAll();
    }

    private void Reveal(int roomId, RevealState state)
    {
        if (_revealByRoomId[roomId] < state)
        {
            _revealByRoomId[roomId] = state;
        }
    }

    private void RefreshAll()
    {
        foreach (RoomController room in _roomsById.Values)
        {
            RefreshIcon(room);
        }

        foreach (KeyValuePair<(int, int), Image> entry in _connectorsByPair)
        {
            (int a, int b) = entry.Key;
            bool visible = _revealByRoomId[a] != RevealState.Hidden && _revealByRoomId[b] != RevealState.Hidden;
            entry.Value.gameObject.SetActive(visible);
        }
    }

    private void RefreshIcon(RoomController room)
    {
        Image[] images = _iconsByRoomId[room.RoomId];
        RevealState reveal = _revealByRoomId[room.RoomId];

        // Discovered (known only via a connector from a visited neighbor, never entered) no
        // longer shows a silhouette at all - only the connector line hints it exists (2026-09-15
        // user feedback). Hidden and Discovered share the same "no icon" treatment here; the
        // connector's own visibility (RefreshAll) is untouched and still lights up as soon as
        // either endpoint stops being Hidden.
        if (reveal == RevealState.Hidden || reveal == RevealState.Discovered)
        {
            SetActive(images, false);
            return;
        }

        SetActive(images, true);

        if (room.RoomId == _currentRoomId)
        {
            SetColor(images, currentColor);
            return;
        }

        if (room.IsCleared)
        {
            SetColor(images, room.RoomType switch
            {
                RoomType.Start => startClearedColor,
                RoomType.Boss => bossClearedColor,
                RoomType.Item => itemClearedColor,
                _ => clearedColor,
            });
            return;
        }

        SetColor(images, discoveredColor);
    }

    private static void SetActive(Image[] images, bool active)
    {
        foreach (Image image in images)
        {
            image.gameObject.SetActive(active);
        }
    }

    private static void SetColor(Image[] images, Color color)
    {
        foreach (Image image in images)
        {
            image.color = color;
        }
    }
}
