using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

// Debugging aid for the procedural floor generator (Assets/Scripts/Stage/ProceduralFloorGenerator.cs):
// draws a seed's room graph as a 2D diagram entirely in Edit Mode, no Play Mode/scene needed, since
// ProceduralFloorSource is pure C#. Node positions come straight from RoomData.GridPosition (not a
// force-directed relayout) on purpose - the whole point is to see the real grid, including overlaps
// and merged-cell rooms sitting between integer cells, not an abstract topology diagram. Same
// left-detail/right-canvas-ish IMGUI approach as DataValidationWindow.cs, no UI Toolkit dependency.
public class FloorGraphWindow : EditorWindow
{
    private const float NodeBaseSize = 64f;
    private const float NodeMinSize = 36f;
    private const float NodeMaxSize = 110f;
    private const float CanvasPadding = 70f;
    private const float DetailPanelWidth = 260f;
    private const float SelectionRingExpand = 4f;

    private string _seedText = "0";
    private int _floorNumber = 1;
    // Built once per Generate() click - holds all 3 floors for the seed so switching the Floor
    // dropdown re-renders instantly from the cache instead of needing a re-generate + re-click
    // per floor (2026-09-15 user feedback).
    private ProceduralFloorSource _currentSource;
    private FloorLayout _layout;
    private string _statusMessage;
    private HashSet<int> _reachableRoomIds = new HashSet<int>();
    private int _selectedRoomId = -1;
    private Vector2 _detailScroll;

    [MenuItem("RogueLike/Floor Graph Visualizer")]
    private static void Open()
    {
        GetWindow<FloorGraphWindow>("Floor Graph");
    }

    private void OnGUI()
    {
        DrawToolbar();

        using (new GUILayout.HorizontalScope())
        {
            DrawCanvas();
            DrawDetailPanel();
        }
    }

    private void DrawToolbar()
    {
        using (new GUILayout.HorizontalScope(EditorStyles.toolbar))
        {
            GUILayout.Label("Seed", GUILayout.Width(34f));
            _seedText = EditorGUILayout.TextField(_seedText, EditorStyles.toolbarTextField, GUILayout.Width(70f));

            if (GUILayout.Button("Random", EditorStyles.toolbarButton, GUILayout.Width(60f)))
            {
                _seedText = UnityEngine.Random.Range(0, RunConfig.MaxSeedValue + 1).ToString();
            }

            GUILayout.Space(10f);
            GUILayout.Label("Floor", GUILayout.Width(32f));
            int newFloorNumber = EditorGUILayout.IntPopup(_floorNumber, new[] { "1", "2", "3" }, new[] { 1, 2, 3 },
                EditorStyles.toolbarPopup, GUILayout.Width(50f));
            if (newFloorNumber != _floorNumber)
            {
                _floorNumber = newFloorNumber;
                if (_currentSource != null)
                {
                    ApplyFloorSelection();
                }
            }

            GUILayout.Space(10f);
            if (GUILayout.Button("Generate", EditorStyles.toolbarButton, GUILayout.Width(70f)))
            {
                Generate();
            }

            GUILayout.FlexibleSpace();

            if (_layout != null)
            {
                int unreachable = _layout.Rooms.Count - _reachableRoomIds.Count;
                string summary = unreachable == 0
                    ? $"{_layout.Rooms.Count} room(s), all reachable"
                    : $"{_layout.Rooms.Count} room(s), {unreachable} UNREACHABLE";
                GUILayout.Label(summary, EditorStyles.miniLabel);
            }
        }
    }

    private void Generate()
    {
        _selectedRoomId = -1;
        _statusMessage = null;

        if (!RunConfig.TryParseSeedOverride(_seedText, out int seed))
        {
            _statusMessage = $"Seed must be a whole number from 0 to {RunConfig.MaxSeedValue}.";
            _layout = null;
            _currentSource = null;
            return;
        }

        try
        {
            // Builds all 3 floors for this seed up front (ProceduralFloorSource's constructor
            // already generates the whole run, not just one floor) - cached in _currentSource so
            // the Floor dropdown can switch between them instantly afterward.
            _currentSource = new ProceduralFloorSource(seed);
            ApplyFloorSelection();
        }
        catch (Exception e)
        {
            // ProceduralFloorGenerator self-validates via FloorGraph.Validate and throws on a
            // structurally broken graph - surface that failure directly rather than showing a
            // half-built diagram.
            _statusMessage = $"Generation failed: {e.Message}";
            _layout = null;
            _currentSource = null;
        }

        Repaint();
    }

    // Switches the displayed floor from the already-generated _currentSource cache - no
    // regeneration, so this is cheap enough to call directly from the Floor dropdown's onChange.
    private void ApplyFloorSelection()
    {
        if (_currentSource == null)
        {
            return;
        }

        _layout = _currentSource.Floors[_floorNumber - 1];
        _reachableRoomIds = ComputeReachable(_layout);
        _selectedRoomId = -1;
        _statusMessage = null;
        Repaint();
    }

    // Mirrors FloorGraph.Validate's own BFS (Assets/Scripts/Stage/FloorGraph.cs) so a room that
    // would fail validation is identifiable individually here, not just as a pass/fail count.
    private static HashSet<int> ComputeReachable(FloorLayout layout)
    {
        Dictionary<int, RoomData> byId = layout.Rooms.ToDictionary(r => r.RoomId);
        var reached = new HashSet<int> { layout.StartRoomId };
        var queue = new Queue<int>();
        queue.Enqueue(layout.StartRoomId);

        while (queue.Count > 0)
        {
            if (!byId.TryGetValue(queue.Dequeue(), out RoomData room))
            {
                continue;
            }

            foreach (RoomDoorConnection conn in room.Connections)
            {
                if (reached.Add(conn.TargetRoomId))
                {
                    queue.Enqueue(conn.TargetRoomId);
                }
            }
        }

        return reached;
    }

    private void DrawCanvas()
    {
        float canvasWidth = Mathf.Max(100f, position.width - DetailPanelWidth - 8f);
        Rect canvasRect = GUILayoutUtility.GetRect(canvasWidth, position.height - 22f,
            GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
        GUI.Box(canvasRect, GUIContent.none);

        if (_statusMessage != null)
        {
            GUI.Label(new Rect(canvasRect.x + 10f, canvasRect.y + 10f, canvasRect.width - 20f, 60f),
                _statusMessage, EditorStyles.wordWrappedLabel);
            return;
        }

        if (_layout == null)
        {
            GUI.Label(new Rect(canvasRect.x + 10f, canvasRect.y + 10f, canvasRect.width - 20f, 20f),
                "Enter a seed and click Generate.");
            return;
        }

        Dictionary<int, Vector2> screenPositions = ComputeScreenPositions(_layout.Rooms, canvasRect);

        DrawEdges(_layout.Rooms, screenPositions);
        foreach (RoomData room in _layout.Rooms)
        {
            DrawNode(room, screenPositions[room.RoomId]);
        }
    }

    // Grid -> screen: min/max GridPosition auto-fit into the canvas rect with fixed padding
    // (design choice - no pan/zoom, this project's floors are small enough to read at a glance).
    // GridPosition.y increases North (see ProceduralFloorGenerator.Dirs), so it maps to a
    // DEcreasing screen Y to put North at the top, like a map.
    private static Dictionary<int, Vector2> ComputeScreenPositions(IReadOnlyList<RoomData> rooms, Rect canvasRect)
    {
        var result = new Dictionary<int, Vector2>(rooms.Count);
        if (rooms.Count == 0)
        {
            return result;
        }

        float minX = rooms.Min(r => r.GridPosition.x);
        float maxX = rooms.Max(r => r.GridPosition.x);
        float minY = rooms.Min(r => r.GridPosition.y);
        float maxY = rooms.Max(r => r.GridPosition.y);
        float spanX = Mathf.Max(0.0001f, maxX - minX);
        float spanY = Mathf.Max(0.0001f, maxY - minY);

        float left = canvasRect.x + CanvasPadding;
        float right = canvasRect.xMax - CanvasPadding;
        float top = canvasRect.y + CanvasPadding;
        float bottom = canvasRect.yMax - CanvasPadding;

        foreach (RoomData room in rooms)
        {
            float tx = (room.GridPosition.x - minX) / spanX;
            float ty = (room.GridPosition.y - minY) / spanY;
            float screenX = Mathf.Lerp(left, right, tx);
            float screenY = Mathf.Lerp(bottom, top, ty); // inverted: North (max y) at the top
            result[room.RoomId] = new Vector2(screenX, screenY);
        }

        return result;
    }

    private void DrawEdges(IReadOnlyList<RoomData> rooms, Dictionary<int, Vector2> screenPositions)
    {
        Handles.BeginGUI();
        Handles.color = new Color(0.55f, 0.55f, 0.55f);

        foreach (RoomData room in rooms)
        {
            if (!screenPositions.TryGetValue(room.RoomId, out Vector2 from))
            {
                continue;
            }

            foreach (RoomDoorConnection conn in room.Connections)
            {
                // Every connection is authored on both rooms (A->B and B->A) - draw each pair once.
                if (conn.TargetRoomId < room.RoomId)
                {
                    continue;
                }

                if (screenPositions.TryGetValue(conn.TargetRoomId, out Vector2 to))
                {
                    Handles.DrawLine(from, to);
                }
            }
        }

        Handles.EndGUI();
    }

    private void DrawNode(RoomData room, Vector2 center)
    {
        float aspect = Mathf.Clamp(room.Footprint.Width / Mathf.Max(0.01f, room.Footprint.Depth), 0.35f, 3f);
        float w = Mathf.Clamp(NodeBaseSize * Mathf.Sqrt(aspect), NodeMinSize, NodeMaxSize);
        float h = Mathf.Clamp(NodeBaseSize / Mathf.Sqrt(aspect), NodeMinSize, NodeMaxSize);
        var rect = new Rect(center.x - w * 0.5f, center.y - h * 0.5f, w, h);

        bool unreachable = !_reachableRoomIds.Contains(room.RoomId);
        bool selected = room.RoomId == _selectedRoomId;

        // A flat EditorGUI.DrawRect fill instead of tinting GUI.Box's skinned texture - GUI.color
        // multiplying the box skin's built-in highlight/shadow gradient made every type read as a
        // washed-out or overly harsh version of the same color instead of a clean, distinguishable
        // one (2026-09-15 user feedback: "너무 쨍한 색은 아닌 걸로 구분"). A thin darker border on
        // top keeps the node readable as a box without needing the skin at all.
        EditorGUI.DrawRect(rect, RoomTypeColor(room.Type));
        DrawRectBorder(rect, new Color(0.15f, 0.15f, 0.15f, 0.6f), 1f);

        if (unreachable)
        {
            DrawRectBorder(rect, Color.red, 3f);
        }

        if (selected)
        {
            // Drawn as its own ring, expanded a few px OUTSIDE the node rect (not reusing rect
            // directly) and in a color no fill/other-state color is near (bright cyan) so it
            // reads clearly even stacked on top of the red unreachable border (2026-09-15 user
            // feedback: the previous thin white-on-node-edge outline was too faint to notice).
            Rect ring = new Rect(rect.x - SelectionRingExpand, rect.y - SelectionRingExpand,
                rect.width + SelectionRingExpand * 2f, rect.height + SelectionRingExpand * 2f);
            DrawRectBorder(ring, new Color(0.2f, 0.9f, 1f), 4f);
        }

        if (room.Footprint.HasNotch)
        {
            DrawNotchMarker(rect, room.Footprint);
        }

        GUI.Label(rect, room.RoomId.ToString(), CenteredLabelStyle);

        if (GUI.Button(rect, GUIContent.none, GUIStyle.none))
        {
            _selectedRoomId = room.RoomId;
        }
    }

    private static void DrawRectBorder(Rect rect, Color color, float thickness)
    {
        Handles.BeginGUI();
        Handles.color = color;
        Handles.DrawAAPolyLine(thickness, new Vector3(rect.xMin, rect.yMin), new Vector3(rect.xMax, rect.yMin),
            new Vector3(rect.xMax, rect.yMax), new Vector3(rect.xMin, rect.yMax), new Vector3(rect.xMin, rect.yMin));
        Handles.EndGUI();
    }

    // Diagonal cut across the notched corner - a schematic hint that this is an L-shaped room, not
    // a to-scale redraw of RoomFootprint's actual notch dimensions (the node itself is already a
    // simplified aspect-ratio box, not a to-scale footprint).
    private static void DrawNotchMarker(Rect rect, RoomFootprint footprint)
    {
        float cut = Mathf.Min(rect.width, rect.height) * 0.35f;
        float cornerX = footprint.NotchSignX > 0 ? rect.xMax : rect.xMin;
        float cornerY = footprint.NotchSignZ > 0 ? rect.yMin : rect.yMax; // screen Y is inverted vs world Z
        float alongX = cornerX + (footprint.NotchSignX > 0 ? -cut : cut);
        float alongY = cornerY + (footprint.NotchSignZ > 0 ? cut : -cut);

        Handles.BeginGUI();
        Handles.color = Color.black;
        Handles.DrawAAPolyLine(2f, new Vector3(alongX, cornerY), new Vector3(cornerX, alongY));
        Handles.EndGUI();
    }

    private static GUIStyle _centeredLabelStyle;
    private static GUIStyle CenteredLabelStyle => _centeredLabelStyle ??= new GUIStyle(EditorStyles.boldLabel)
    {
        alignment = TextAnchor.MiddleCenter,
        normal = { textColor = Color.black },
    };

    // Low saturation (0.3-0.4) so the 4 types read as distinct but muted, not the original
    // vivid/high-saturation set (2026-09-15 user feedback: "너무 쨍한 색은 아닌 걸로 구분").
    // HSV rather than raw RGB so relative brightness across types stays easy to keep even.
    private static Color RoomTypeColor(RoomType type)
    {
        switch (type)
        {
            case RoomType.Start: return Color.HSVToRGB(0.33f, 0.35f, 0.78f); // muted sage green
            case RoomType.Boss: return Color.HSVToRGB(0.02f, 0.40f, 0.75f); // muted terracotta
            case RoomType.Item: return Color.HSVToRGB(0.12f, 0.40f, 0.80f); // muted mustard/gold
            default: return Color.HSVToRGB(0.58f, 0.25f, 0.78f); // Combat - muted slate blue
        }
    }

    private void DrawDetailPanel()
    {
        using (new GUILayout.VerticalScope(GUILayout.Width(DetailPanelWidth)))
        {
            RoomData selected = _layout?.Rooms.FirstOrDefault(r => r.RoomId == _selectedRoomId);
            if (selected == null)
            {
                EditorGUILayout.HelpBox("Click a room node to see its details here.", MessageType.Info);
                return;
            }

            _detailScroll = EditorGUILayout.BeginScrollView(_detailScroll);

            EditorGUILayout.LabelField($"Room {selected.RoomId}", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Type", selected.Type.ToString());
            EditorGUILayout.LabelField("Grid Position", selected.GridPosition.ToString("F2"));
            EditorGUILayout.LabelField("Reachable from Start",
                _reachableRoomIds.Contains(selected.RoomId) ? "Yes" : "NO - unreachable");

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Footprint", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Size", $"{selected.Footprint.Width:F0} x {selected.Footprint.Depth:F0}");
            EditorGUILayout.LabelField("Notch", selected.Footprint.HasNotch
                ? $"{selected.Footprint.NotchCorner} ({selected.Footprint.NotchWidth:F0} x {selected.Footprint.NotchDepth:F0})"
                : "None");

            EditorGUILayout.Space();
            EditorGUILayout.LabelField($"Connections ({selected.Connections.Count})", EditorStyles.boldLabel);
            foreach (RoomDoorConnection conn in selected.Connections)
            {
                string sub = conn.SubCellIndex != 0 ? $" [sub {conn.SubCellIndex}]" : "";
                EditorGUILayout.LabelField($"  {conn.Direction} -> Room {conn.TargetRoomId}{sub}");
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField($"Enemies ({selected.Enemies.Count})", EditorStyles.boldLabel);
            foreach (var group in selected.Enemies.GroupBy(e => e.Variant))
            {
                EditorGUILayout.LabelField($"  {group.Key} x{group.Count()}");
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField($"Obstacles ({selected.Obstacles.Count})", EditorStyles.boldLabel);
            DrawObstacleMiniMap(selected);

            EditorGUILayout.EndScrollView();
        }
    }

    // Mirrors RoomController.TileSize (private there) - the tile grid ObstaclePlacement.LocalCell
    // is authored in (Assets/Scripts/Room/RoomController.cs).
    private const float ObstacleTileSize = 4f;
    private const float ObstacleMapMaxSize = 220f;
    private static readonly Color ObstacleVoidColor = new Color(0.1f, 0.1f, 0.1f);
    private static readonly Color ObstacleFloorColor = new Color(0.32f, 0.32f, 0.32f);
    private static readonly Color ObstacleSoloColor = Color.HSVToRGB(0.08f, 0.35f, 0.55f); // muted brown
    private static readonly Color ObstacleConnectorColor = Color.HSVToRGB(0.1f, 0.55f, 0.75f); // muted amber

    // Schematic top-down view of where this room's ObstaclePlacement entries actually sit on its
    // tile grid (2026-09-15 user feedback - the plain count alone didn't say where anything was).
    // Not a to-scale redraw of the real obstacle meshes, just which tile each placement occupies -
    // Solo as a filled block, Connector as a thin bar toward whichever neighbour its RotationY
    // encodes (see ObstaclePlacement.cs's doc comment). North is up, matching the main graph.
    private void DrawObstacleMiniMap(RoomData room)
    {
        int cellsX = Mathf.RoundToInt(room.Footprint.Width / ObstacleTileSize);
        int cellsZ = Mathf.RoundToInt(room.Footprint.Depth / ObstacleTileSize);
        if (cellsX <= 0 || cellsZ <= 0)
        {
            return;
        }

        float cellSize = Mathf.Min(ObstacleMapMaxSize / cellsX, ObstacleMapMaxSize / cellsZ);
        float mapWidth = cellSize * cellsX;
        float mapHeight = cellSize * cellsZ;

        Rect area = GUILayoutUtility.GetRect(mapWidth, mapHeight + 4f, GUILayout.ExpandWidth(true));
        float originX = area.x + Mathf.Max(0f, (area.width - mapWidth) * 0.5f);
        var mapRect = new Rect(originX, area.y, mapWidth, mapHeight);

        EditorGUI.DrawRect(mapRect, ObstacleVoidColor);

        bool[,] isVoid = ComputeNotchVoid(room.Footprint, cellsX, cellsZ);
        for (int ix = 0; ix < cellsX; ix++)
        {
            for (int iz = 0; iz < cellsZ; iz++)
            {
                if (isVoid[ix, iz])
                {
                    continue;
                }

                Rect cellRect = CellRect(mapRect, ix, iz, cellSize);
                EditorGUI.DrawRect(new Rect(cellRect.x + 1f, cellRect.y + 1f, cellRect.width - 2f, cellRect.height - 2f),
                    ObstacleFloorColor);
            }
        }

        foreach (ObstaclePlacement placement in room.Obstacles)
        {
            DrawObstacleMark(mapRect, placement, cellSize);
        }

        DrawRectBorder(mapRect, new Color(1f, 1f, 1f, 0.4f), 1.5f);

        EditorGUILayout.Space(2f);
        using (new GUILayout.HorizontalScope())
        {
            DrawLegendSwatch(ObstacleSoloColor, "Solo");
            GUILayout.Space(12f);
            DrawLegendSwatch(ObstacleConnectorColor, "Connector");
        }
    }

    // Same "cells inside the notch void have no floor" rule RoomController.BuildFloor applies
    // (Assets/Scripts/Room/RoomController.cs) - re-derived here rather than shared since that
    // method is private and scene-building-specific (it also carves out diagonal corner-tile
    // cells, which are still walkable floor, just a different mesh, so this only reproduces the
    // notch-void half of it).
    private static bool[,] ComputeNotchVoid(RoomFootprint footprint, int cellsX, int cellsZ)
    {
        var voidCells = new bool[cellsX, cellsZ];
        if (!footprint.HasNotch)
        {
            return voidCells;
        }

        int notchCellsX = Mathf.RoundToInt(footprint.NotchWidth / ObstacleTileSize);
        int notchCellsZ = Mathf.RoundToInt(footprint.NotchDepth / ObstacleTileSize);
        int signX = footprint.NotchSignX;
        int signZ = footprint.NotchSignZ;

        for (int ix = 0; ix < cellsX; ix++)
        {
            bool inNotchX = signX > 0 ? ix >= cellsX - notchCellsX : ix < notchCellsX;
            for (int iz = 0; iz < cellsZ; iz++)
            {
                bool inNotchZ = signZ > 0 ? iz >= cellsZ - notchCellsZ : iz < notchCellsZ;
                voidCells[ix, iz] = inNotchX && inNotchZ;
            }
        }

        return voidCells;
    }

    // ix=0 is the west-most column, iz=0 is the south-most row - iz increasing maps to
    // decreasing screen Y so north (higher iz, see ProceduralFloorGenerator.Dirs) ends up at the
    // top, matching the main canvas's own North-up convention.
    private static Rect CellRect(Rect mapRect, int ix, int iz, float cellSize)
    {
        float x = mapRect.x + ix * cellSize;
        float y = mapRect.yMax - (iz + 1) * cellSize;
        return new Rect(x, y, cellSize, cellSize);
    }

    private static Vector2 CellCenter(Rect mapRect, Vector2Int cell, float cellSize)
    {
        Rect r = CellRect(mapRect, cell.x, cell.y, cellSize);
        return new Vector2(r.x + r.width * 0.5f, r.y + r.height * 0.5f);
    }

    private static void DrawObstacleMark(Rect mapRect, ObstaclePlacement placement, float cellSize)
    {
        if (placement.Shape == ObstacleShape.Solo)
        {
            Vector2 center = CellCenter(mapRect, placement.LocalCell, cellSize);
            float size = cellSize * 0.7f;
            EditorGUI.DrawRect(new Rect(center.x - size * 0.5f, center.y - size * 0.5f, size, size), ObstacleSoloColor);
            return;
        }

        // Connector: RotationY 90 spans to LocalCell + (1,0) (east), 0 spans to LocalCell + (0,1)
        // (north) - see ObstaclePlacement.cs's doc comment for why those two are the only options.
        bool alongX = Mathf.Approximately(placement.RotationY, 90f);
        Vector2Int neighbour = placement.LocalCell + (alongX ? new Vector2Int(1, 0) : new Vector2Int(0, 1));

        Vector2 from = CellCenter(mapRect, placement.LocalCell, cellSize);
        Vector2 to = CellCenter(mapRect, neighbour, cellSize);

        Handles.BeginGUI();
        Handles.color = ObstacleConnectorColor;
        Handles.DrawAAPolyLine(cellSize * 0.3f, from, to);
        Handles.EndGUI();
    }

    private static void DrawLegendSwatch(Color color, string label)
    {
        Rect swatch = GUILayoutUtility.GetRect(10f, 10f, GUILayout.Width(10f));
        EditorGUI.DrawRect(swatch, color);
        GUILayout.Label(label, EditorStyles.miniLabel, GUILayout.ExpandWidth(false));
    }
}
