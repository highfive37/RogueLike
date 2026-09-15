using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// Full-screen "whole floor at once" map, opened by clicking the corner MinimapUI or pressing Tab -
// same pause-and-dim treatment as every other full-screen screen in this project (GameOverUI's
// dimGroup/contentGroup CanvasGroup fade, PauseState.RequestPause/ReleasePause). Draggable
// (left-click drag pans, no click-to-close - that was tried and dropped, see DevLog 2026-09-15)
// since a whole floor's room graph doesn't fit one screen at a comfortable icon size the way the
// small corner minimap's locally-scrolling view does.
//
// Deliberately shares NO code with MinimapUI (2026-09-15, explicit user call): both independently
// track their own Hidden/Discovered/Visited reveal state by subscribing to the same
// RoomController.RoomEntered/Cleared events and rebuild their own icons/connectors from the same
// room list StageManager hands both of them. A change to one file (including its L-shape notch
// icon-splitting math) can never silently affect the other, at the cost of near-duplicate code -
// an explicit trade-off, not an oversight.
[RequireComponent(typeof(RectMask2D))]
public class FullFloorMapUI : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    private enum RevealState
    {
        Hidden,
        Discovered,
        Visited
    }

    [SerializeField] private InputActionAsset menuActions;
    [SerializeField] private GameObject iconPrefab;
    [SerializeField] private GameObject connectorPrefab;
    [SerializeField] private RectTransform content;
    [SerializeField] private CanvasGroup contentGroup;
    [SerializeField] private CanvasGroup dimGroup;

    // Bigger than MinimapUI's 40/24 - this view has the whole screen to work with, not a small
    // corner viewport, so rooms can afford to read larger.
    [SerializeField] private float cellSpacing = 90f;
    [SerializeField] private float connectorThickness = 5f;
    private const float IconBaseSize = 48f;

    [SerializeField] private float fadeDuration = 0.25f;
    [SerializeField] private float dimMaxAlpha = 0.75f;

    // How far past the content's own bounding box a drag may still pull it before clamping -
    // enough slack that the edge room isn't glued to the viewport border, not so much the whole
    // floor can be dragged off into empty space.
    [SerializeField] private float panClampMargin = 150f;

    [SerializeField] private Color discoveredColor = new Color(0.35f, 0.35f, 0.35f, 0.7f);
    [SerializeField] private Color currentColor = Color.yellow;
    [SerializeField] private Color clearedColor = new Color(0.25f, 0.8f, 0.35f, 1f);
    [SerializeField] private Color startClearedColor = new Color(0.45f, 0.45f, 0.85f, 1f);
    [SerializeField] private Color bossClearedColor = new Color(0.75f, 0.25f, 0.25f, 1f);
    [SerializeField] private Color itemClearedColor = new Color(0.85f, 0.75f, 0.2f, 1f);
    [SerializeField] private Color connectorColor = new Color(0.6f, 0.6f, 0.6f, 0.6f);

    private readonly Dictionary<int, RoomController> _roomsById = new Dictionary<int, RoomController>();
    private readonly Dictionary<int, Image[]> _iconsByRoomId = new Dictionary<int, Image[]>();
    private readonly Dictionary<int, RevealState> _revealByRoomId = new Dictionary<int, RevealState>();
    private readonly Dictionary<(int, int), Image> _connectorsByPair = new Dictionary<(int, int), Image>();

    private int _currentRoomId = -1;
    private bool _isOpen;
    private Vector2 _contentBoundsMin;
    private Vector2 _contentBoundsMax;
    private Canvas _rootCanvas;
    private InputAction _toggleAction;
    private Coroutine _fadeRoutine;

    private void Awake()
    {
        _rootCanvas = GetComponentInParent<Canvas>();
        if (_rootCanvas != null)
        {
            _rootCanvas = _rootCanvas.rootCanvas;
        }

        _toggleAction = menuActions.FindActionMap("Menu").FindAction("ToggleMap");
        ApplyHidden();
    }

    private void OnEnable()
    {
        menuActions.Enable();
        _toggleAction.performed += OnTogglePerformed;
    }

    private void OnDisable()
    {
        _toggleAction.performed -= OnTogglePerformed;
        menuActions.Disable();
    }

    private void OnTogglePerformed(InputAction.CallbackContext context)
    {
        if (_isOpen)
        {
            Close();
            return;
        }

        if (PauseState.IsPaused)
        {
            // Another pause owner (ESC menu, floor-complete screen, ...) already has it - don't
            // steal pause ownership out from under it, mirrors EquipMenuController.Open().
            return;
        }

        Open();
    }

    // Safe to call more than once - StageManager re-initializes this for every new floor, same as
    // MinimapUI.Initialize.
    public void Initialize(IEnumerable<RoomController> rooms)
    {
        ResetState();

        RoomController[] roomArray = rooms.ToArray();
        foreach (RoomController room in roomArray)
        {
            _roomsById[room.RoomId] = room;
            _revealByRoomId[room.RoomId] = RevealState.Hidden;
            room.RoomEntered += HandleRoomEntered;
            room.Cleared += HandleRoomCleared;
        }

        BuildConnectors(roomArray);
        BuildIcons(roomArray);
        ComputeContentBounds(roomArray);
        RefreshAll();
    }

    private void ResetState()
    {
        foreach (RoomController room in _roomsById.Values)
        {
            room.RoomEntered -= HandleRoomEntered;
            room.Cleared -= HandleRoomCleared;
        }

        if (content != null)
        {
            foreach (Transform child in content)
            {
                Destroy(child.gameObject);
            }
        }

        _roomsById.Clear();
        _iconsByRoomId.Clear();
        _revealByRoomId.Clear();
        _connectorsByPair.Clear();
        _currentRoomId = -1;
    }

    public void Open()
    {
        if (_isOpen)
        {
            return;
        }

        _isOpen = true;
        PauseState.RequestPause(this);

        // Re-center on wherever the player currently is every time the map is opened - while
        // closed, HandleRoomEntered keeps _currentRoomId current without touching content's
        // position (nothing panning-related is visible then anyway).
        if (_currentRoomId >= 0 && _roomsById.TryGetValue(_currentRoomId, out RoomController current))
        {
            content.anchoredPosition = -GridToLocal(current.GridPosition);
        }

        dimGroup.blocksRaycasts = true;
        contentGroup.blocksRaycasts = true;

        RestartFade(true);
    }

    public void Close()
    {
        if (!_isOpen)
        {
            return;
        }

        _isOpen = false;
        PauseState.ReleasePause(this);

        RestartFade(false);
    }

    private void RestartFade(bool show)
    {
        if (_fadeRoutine != null)
        {
            StopCoroutine(_fadeRoutine);
        }

        _fadeRoutine = StartCoroutine(FadeRoutine(show));
    }

    // Time.unscaledDeltaTime, same as GameOverUI's fade - runs correctly under the
    // Time.timeScale = 0 this component itself just requested via PauseState.
    private IEnumerator FadeRoutine(bool show)
    {
        float startDim = dimGroup.alpha;
        float startContent = contentGroup.alpha;
        float targetDim = show ? dimMaxAlpha : 0f;
        float targetContent = show ? 1f : 0f;

        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / fadeDuration);
            dimGroup.alpha = Mathf.Lerp(startDim, targetDim, t);
            contentGroup.alpha = Mathf.Lerp(startContent, targetContent, t);
            yield return null;
        }

        dimGroup.alpha = targetDim;
        contentGroup.alpha = targetContent;

        if (!show)
        {
            dimGroup.blocksRaycasts = false;
            contentGroup.blocksRaycasts = false;
        }

        _fadeRoutine = null;
    }

    private void ApplyHidden()
    {
        dimGroup.alpha = 0f;
        dimGroup.blocksRaycasts = false;
        contentGroup.alpha = 0f;
        contentGroup.blocksRaycasts = false;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!_isOpen)
        {
            return;
        }

        float scale = _rootCanvas != null ? _rootCanvas.scaleFactor : 1f;
        Vector2 delta = eventData.delta / Mathf.Max(0.0001f, scale);
        content.anchoredPosition = ClampPan(content.anchoredPosition + delta);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
    }

    private Vector2 ClampPan(Vector2 desired)
    {
        Rect viewport = ((RectTransform)transform).rect;

        float minX = viewport.xMin - panClampMargin - _contentBoundsMax.x;
        float maxX = viewport.xMax + panClampMargin - _contentBoundsMin.x;
        float minY = viewport.yMin - panClampMargin - _contentBoundsMax.y;
        float maxY = viewport.yMax + panClampMargin - _contentBoundsMin.y;

        // A tiny floor (few rooms) can make min > max for an axis - there is no meaningful
        // panning room, so hold the midpoint instead of an inverted/invalid clamp.
        float x = minX <= maxX ? Mathf.Clamp(desired.x, minX, maxX) : (minX + maxX) * 0.5f;
        float y = minY <= maxY ? Mathf.Clamp(desired.y, minY, maxY) : (minY + maxY) * 0.5f;
        return new Vector2(x, y);
    }

    private void ComputeContentBounds(IReadOnlyCollection<RoomController> rooms)
    {
        if (rooms.Count == 0)
        {
            _contentBoundsMin = _contentBoundsMax = Vector2.zero;
            return;
        }

        float minX = float.MaxValue, maxX = float.MinValue, minY = float.MaxValue, maxY = float.MinValue;
        const float half = IconBaseSize * 0.5f;

        foreach (RoomController room in rooms)
        {
            Vector2 pos = GridToLocal(room.GridPosition);
            minX = Mathf.Min(minX, pos.x - half);
            maxX = Mathf.Max(maxX, pos.x + half);
            minY = Mathf.Min(minY, pos.y - half);
            maxY = Mathf.Max(maxY, pos.y + half);
        }

        _contentBoundsMin = new Vector2(minX, minY);
        _contentBoundsMax = new Vector2(maxX, maxY);
    }

    private Vector2 GridToLocal(Vector2 gridPosition)
    {
        return gridPosition * cellSpacing;
    }

    // Mirrors MinimapUI.BuildIcons - see that file for why an L-shaped footprint becomes 2
    // non-overlapping sub-rectangle pieces instead of 1.
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
        GameObject iconObj = Instantiate(iconPrefab, content);
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

                GameObject connectorObj = Instantiate(connectorPrefab, content);
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

        // Discovered (known only via a connector from a visited neighbor, never entered) shows no
        // icon at all - only the connector line hints it exists (2026-09-15 user feedback, same
        // change as MinimapUI). Connector visibility itself (RefreshAll) is untouched.
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
