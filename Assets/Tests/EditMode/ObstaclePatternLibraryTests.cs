using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace RogueLike.Tests
{
    // Validates every authored entry in ObstaclePatterns.json ONCE (patterns are fixed
    // data now, not a live per-seed procedural decision - see Docs/AI_Decision_Log.md
    // 2026-09-12 "패턴 라이브러리로 전환"), instead of the old seed-scan style used when
    // obstacle placement was still algorithmic. Square patterns are checked as authored
    // (no rotation needed - a square room has no notch to align to, though production
    // does apply a random facing for visual variety, which cannot change validity: the
    // clearance/notch rules are rotationally symmetric for a square footprint).
    // L-shape patterns are checked against all 4 possible notch corners, since production
    // rotates a NE-authored pattern to match whichever corner the room actually has.
    public class ObstaclePatternLibraryTests
    {
        private const float TileSize = 4f;
        private const float SquareSize = 28f;   // matches ProceduralFloorGenerator.CombatRoomSize
        private const float LShapeSize = 44f;   // matches ProceduralFloorGenerator.LMergeBounding
        private const float LShapeNotch = 20f;  // matches ProceduralFloorGenerator.LMergeNotch

        private static readonly RoomCorner[] AllCorners = { RoomCorner.NE, RoomCorner.SE, RoomCorner.SW, RoomCorner.NW };

        [Test]
        public void SquarePatterns_AllCellsRespectEdgeClearance()
        {
            RoomFootprint footprint = RoomFootprint.Square(SquareSize);
            int gridSize = ObstaclePatternLibrary.SquareGridSize;
            foreach (ObstaclePatternData pattern in ObstaclePatternLibrary.SquarePatterns)
            {
                AssertAllCellsValid(pattern, footprint, gridSize, $"square pattern '{pattern.Name}'");
            }
        }

        [Test]
        public void LShapePatterns_AllCellsRespectEdgeClearanceAndNotch_ForEveryNotchCorner()
        {
            int gridSize = ObstaclePatternLibrary.LShapeGridSize;
            foreach (RoomCorner corner in AllCorners)
            {
                RoomFootprint footprint = RoomFootprint.LShape(LShapeSize, LShapeSize, corner, LShapeNotch, LShapeNotch);
                int steps = StepsFor(corner);
                foreach (ObstaclePatternData pattern in ObstaclePatternLibrary.LShapePatterns)
                {
                    AssertAllCellsValid(RotatedPattern(pattern, gridSize, steps), footprint, gridSize,
                        $"L-shape pattern '{pattern.Name}' rotated for {corner}");
                }
            }
        }

        [Test]
        public void SquarePatterns_NeverDisconnectTheRoom()
        {
            RoomFootprint footprint = RoomFootprint.Square(SquareSize);
            int gridSize = ObstaclePatternLibrary.SquareGridSize;
            foreach (ObstaclePatternData pattern in ObstaclePatternLibrary.SquarePatterns)
            {
                var blocked = AllCells(pattern);
                Assert.That(ProceduralFloorGenerator.LeavesRoomConnected(footprint, gridSize, gridSize, blocked), Is.True,
                    $"square pattern '{pattern.Name}' seals off part of the room");
            }
        }

        [Test]
        public void LShapePatterns_NeverDisconnectTheRoom_ForEveryNotchCorner()
        {
            int gridSize = ObstaclePatternLibrary.LShapeGridSize;
            foreach (RoomCorner corner in AllCorners)
            {
                RoomFootprint footprint = RoomFootprint.LShape(LShapeSize, LShapeSize, corner, LShapeNotch, LShapeNotch);
                int steps = StepsFor(corner);
                foreach (ObstaclePatternData pattern in ObstaclePatternLibrary.LShapePatterns)
                {
                    var blocked = AllCells(RotatedPattern(pattern, gridSize, steps));
                    Assert.That(ProceduralFloorGenerator.LeavesRoomConnected(footprint, gridSize, gridSize, blocked), Is.True,
                        $"L-shape pattern '{pattern.Name}' rotated for {corner} seals off part of the room");
                }
            }
        }

        // Mirrors ProceduralFloorGenerator.RotateCell exactly (re-derived independently
        // rather than calling it, so this test would still catch a bug in either) to spot-
        // check the rotation used by the L-shape validity tests above.
        [Test]
        public void RotateCell_FullCircleReturnsToStart_AndIsCorrectAfterOneStep()
        {
            var cell = new Vector2Int(2, 8);
            const int gridSize = 11;
            Vector2Int afterFour = Rotate(Rotate(Rotate(Rotate(cell, gridSize), gridSize), gridSize), gridSize);
            Assert.That(afterFour, Is.EqualTo(cell));

            // (8,8) is deep inside a canonical NE void; rotating once should land deep
            // inside the SE void (ix high, iz low) - same derivation as NotchRotationSteps.
            var neVoidPoint = new Vector2Int(8, 8);
            Vector2Int rotatedOnce = Rotate(neVoidPoint, gridSize);
            Assert.That(rotatedOnce.x, Is.GreaterThanOrEqualTo(6));
            Assert.That(rotatedOnce.y, Is.LessThan(5));
        }

        private static Vector2Int Rotate(Vector2Int cell, int gridSize)
        {
            int center2 = gridSize - 1;
            int lx = 2 * cell.x - center2;
            int lz = 2 * cell.y - center2;
            int nlx = lz;
            int nlz = -lx;
            return new Vector2Int((nlx + center2) / 2, (nlz + center2) / 2);
        }

        private static int StepsFor(RoomCorner corner)
        {
            switch (corner)
            {
                case RoomCorner.NE: return 0;
                case RoomCorner.SE: return 1;
                case RoomCorner.SW: return 2;
                default: return 3; // NW
            }
        }

        private static ObstaclePatternData RotatedPattern(ObstaclePatternData pattern, int gridSize, int steps)
        {
            var solo = new List<Vector2Int>();
            foreach (Vector2Int c in pattern.SoloCells)
            {
                Vector2Int r = c;
                for (int i = 0; i < steps; i++) { r = Rotate(r, gridSize); }
                solo.Add(r);
            }
            var fence = new List<Vector2Int>();
            foreach (Vector2Int c in pattern.FenceCells)
            {
                Vector2Int r = c;
                for (int i = 0; i < steps; i++) { r = Rotate(r, gridSize); }
                fence.Add(r);
            }
            return new ObstaclePatternData(pattern.Name, pattern.Weight, solo, fence);
        }

        private static void AssertAllCellsValid(ObstaclePatternData pattern, RoomFootprint footprint, int gridSize, string label)
        {
            var floorCells = new HashSet<Vector2Int>(ProceduralFloorGenerator.FloorCells(footprint, gridSize, gridSize));
            const int clearance = ProceduralFloorGenerator.ObstacleEdgeClearanceCells;

            foreach (Vector2Int c in AllCells(pattern))
            {
                Assert.That(c.x, Is.InRange(clearance, gridSize - 1 - clearance), $"{label}: cell {c} outside edge clearance");
                Assert.That(c.y, Is.InRange(clearance, gridSize - 1 - clearance), $"{label}: cell {c} outside edge clearance");
                Assert.That(floorCells.Contains(c), Is.True, $"{label}: cell {c} is not real floor (notch void or corner)");
            }
        }

        private static HashSet<Vector2Int> AllCells(ObstaclePatternData pattern)
        {
            var set = new HashSet<Vector2Int>(pattern.SoloCells);
            foreach (Vector2Int c in pattern.FenceCells) { set.Add(c); }
            return set;
        }
    }
}
