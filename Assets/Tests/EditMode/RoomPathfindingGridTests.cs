using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace RogueLike.Tests
{
    // 2026-09-13: locks RoomPathfindingGrid's A* - pure/local-space like RoomBoundsTests, no
    // scene or RoomController needed. Room-local cell coordinates match ObstaclePlacement's
    // own LocalCell convention throughout.
    public class RoomPathfindingGridTests
    {
        private const float TileSize = 4f;

        private static List<ObstaclePlacement> NoObstacles() => new List<ObstaclePlacement>();

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
        public void TryFindPath_SoloObstacleInTheWay_RoutesAround()
        {
            // 5x5 cells, Solo block sitting directly on the straight line from (0,2) to (4,2).
            var obstacles = new List<ObstaclePlacement>
            {
                new ObstaclePlacement(ObstacleShape.Solo, new Vector2Int(2, 2), 0, 0f),
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
        public void TryFindPath_ConnectorBlocksOnlyTheSpecificEdge_NotTheWholeCell()
        {
            // Connector spans (1,0)-(2,0) (RotationY 90 = east neighbour) - blocks that one
            // connection only. Straight line along z=0 must detour, but (1,0) itself must
            // still be reachable and usable from another direction (e.g. via (1,1)).
            var obstacles = new List<ObstaclePlacement>
            {
                new ObstaclePlacement(ObstacleShape.Connector, new Vector2Int(1, 0), 0, 90f),
            };
            var grid = new RoomPathfindingGrid(RoomFootprint.Square(20f), obstacles, TileSize);

            Assert.That(grid.IsWalkable(new Vector2Int(1, 0)), Is.True, "Connector must not block the cell itself, only the edge");

            var path = new List<Vector2Int>();
            bool found = grid.TryFindPath(new Vector2Int(0, 0), new Vector2Int(2, 0), path);

            Assert.That(found, Is.True);
            // The direct (0,0)->(1,0)->(2,0) route (3 cells) crosses the blocked edge, so the
            // shortest surviving path must be longer - it has to detour through another row.
            Assert.That(path.Count, Is.GreaterThan(3));
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
        public void TryFindPath_GoalFullySurroundedBySoloObstacles_ReturnsFalse()
        {
            var obstacles = new List<ObstaclePlacement>
            {
                new ObstaclePlacement(ObstacleShape.Solo, new Vector2Int(1, 2), 0, 0f),
                new ObstaclePlacement(ObstacleShape.Solo, new Vector2Int(3, 2), 0, 0f),
                new ObstaclePlacement(ObstacleShape.Solo, new Vector2Int(2, 1), 0, 0f),
                new ObstaclePlacement(ObstacleShape.Solo, new Vector2Int(2, 3), 0, 0f),
            };
            var grid = new RoomPathfindingGrid(RoomFootprint.Square(20f), obstacles, TileSize);
            var path = new List<Vector2Int>();

            bool found = grid.TryFindPath(new Vector2Int(0, 0), new Vector2Int(2, 2), path);

            Assert.That(found, Is.False);
            Assert.That(path, Is.Empty);
        }
    }
}
