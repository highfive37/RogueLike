using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace RogueLike.Tests
{
    // 2026-09-30: RoomPathfindingGrid takes real obstacle Bounds now (not ObstaclePlacement),
    // at a cell size finer than the room's authoring tile size, so it can represent obstacle
    // footprints and any real gaps between them more precisely than the old per-tile block/
    // edge model could - see RoomController.SoloClampBounds for why a Solo obstacle's real
    // footprint is often smaller than the tile it's placed on. Pure/local-space like
    // RoomBoundsTests, no scene or RoomController needed.
    public class RoomPathfindingGridTests
    {
        private const float TileSize = 4f;

        private static List<Bounds> NoObstacles() => new List<Bounds>();

        // Mirrors RoomController.CellToLocal's centre formula, so a test can place an obstacle
        // Bounds exactly on a cell without duplicating RoomController's own (private) math.
        private static Vector3 CellCenter(Vector2Int cell, float roomSize, float cellSize)
        {
            float half = roomSize / 2f;
            return new Vector3(-half + cellSize / 2f + cell.x * cellSize, 0f, -half + cellSize / 2f + cell.y * cellSize);
        }

        [Test]
        public void TryFindPath_OpenRoom_FindsShortestManhattanPath()
        {
            var grid = new RoomPathfindingGrid(RoomFootprint.Square(20f), NoObstacles(), TileSize); // 5x5 cells
            var path = new List<Vector2Int>();

            bool found = grid.TryFindPath(new Vector2Int(0, 0), new Vector2Int(2, 0), path);

            Assert.That(found, Is.True);
            Assert.That(path.Count, Is.EqualTo(3)); // (0,0) (1,0) (2,0) inclusive
            Assert.That(path[0], Is.EqualTo(new Vector2Int(0, 0)));
            Assert.That(path[path.Count - 1], Is.EqualTo(new Vector2Int(2, 0)));
        }

        [Test]
        public void TryFindPath_ObstacleInTheWay_RoutesAround()
        {
            // 5x5 cells, a full-tile obstacle sitting directly on the straight line from (0,2) to (4,2).
            var obstacles = new List<Bounds>
            {
                new Bounds(CellCenter(new Vector2Int(2, 2), 20f, TileSize), new Vector3(TileSize, 0f, TileSize)),
            };
            var grid = new RoomPathfindingGrid(RoomFootprint.Square(20f), obstacles, TileSize);
            var path = new List<Vector2Int>();

            bool found = grid.TryFindPath(new Vector2Int(0, 2), new Vector2Int(4, 2), path);

            Assert.That(found, Is.True);
            CollectionAssert.DoesNotContain(path, new Vector2Int(2, 2));
            Assert.That(path[0], Is.EqualTo(new Vector2Int(0, 2)));
            Assert.That(path[path.Count - 1], Is.EqualTo(new Vector2Int(4, 2)));
        }

        [Test]
        public void TryFindPath_ThinObstacleBetweenTwoOpenCells_BlocksOnlyItsOwnFootprint_NotTheWholeTile()
        {
            // The precision fix this locks down: a real obstacle footprint can be much smaller
            // than a full tile (RoomController.SoloClampBounds), so at a finer-than-tile cell
            // size a thin obstacle must block only the cells it actually overlaps, leaving a
            // real gap next to it walkable - the old per-tile model blocked the whole tile
            // regardless of the obstacle's true size, hiding a passable gap between obstacles.
            const float cellSize = 1f;
            // 1-unit-thick wall spanning the room depth, exactly aligned to cell x=10's span
            // (local x in [0, 1)) so neither neighbour cell is ambiguously touched.
            var obstacles = new List<Bounds> { new Bounds(new Vector3(0.5f, 0f, 0f), new Vector3(1f, 0f, 20f)) };
            var grid = new RoomPathfindingGrid(RoomFootprint.Square(20f), obstacles, cellSize); // 20x20 cells

            Assert.That(grid.IsWalkable(new Vector2Int(9, 10)), Is.True, "a cell just short of the wall must stay open");
            Assert.That(grid.IsWalkable(new Vector2Int(10, 10)), Is.False, "the wall's own footprint must be blocked");
            Assert.That(grid.IsWalkable(new Vector2Int(11, 10)), Is.True, "a cell just past the wall must stay open");
        }

        [Test]
        public void TryFindPath_NotchCell_IsNeverWalkable()
        {
            var footprint = RoomFootprint.LShape(20f, 20f, RoomCorner.NE, 8f, 8f); // 5x5 cells, NE 2x2 notch cut
            var grid = new RoomPathfindingGrid(footprint, NoObstacles(), TileSize);

            // Bounding-box NE corner cell (4,4) sits inside the notch void.
            Assert.That(grid.IsWalkable(new Vector2Int(4, 4)), Is.False);
        }

        [Test]
        public void TryFindPath_GoalFullySurroundedByObstacles_ReturnsFalse()
        {
            var goal = new Vector2Int(2, 2);
            var obstacles = new List<Bounds>
            {
                new Bounds(CellCenter(new Vector2Int(1, 2), 20f, TileSize), new Vector3(TileSize, 0f, TileSize)),
                new Bounds(CellCenter(new Vector2Int(3, 2), 20f, TileSize), new Vector3(TileSize, 0f, TileSize)),
                new Bounds(CellCenter(new Vector2Int(2, 1), 20f, TileSize), new Vector3(TileSize, 0f, TileSize)),
                new Bounds(CellCenter(new Vector2Int(2, 3), 20f, TileSize), new Vector3(TileSize, 0f, TileSize)),
            };
            var grid = new RoomPathfindingGrid(RoomFootprint.Square(20f), obstacles, TileSize);
            var path = new List<Vector2Int>();

            bool found = grid.TryFindPath(new Vector2Int(0, 0), goal, path);

            Assert.That(found, Is.False);
            Assert.That(path, Is.Empty);
        }
    }
}
