using System;
using System.IO;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace RogueLike.Tests
{
    // Locks GameFloors' JSON parse + structural validation + degrade-to-emergency behaviour.
    // A broken floor graph must never throw or load "half working" - it must fall back to the
    // built-in emergency layout with IsCorrupted = true so StageManager can warn the player.
    public class GameFloorsTests
    {
        // Start(1) --East--> Combat(2) --East--> Boss(3), connections mirrored both ways.
        private const string ValidFloor =
            "{ \"isLastFloor\": true, \"startRoomId\": 1, \"rooms\": [" +
            "{ \"roomId\": 1, \"roomType\": \"Start\", \"gridPosition\": {\"x\":0,\"y\":0}, \"roomSize\": 16, \"enemies\": [], \"connections\": [{\"direction\":\"East\",\"targetRoomId\":2}] }," +
            "{ \"roomId\": 2, \"roomType\": \"Combat\", \"gridPosition\": {\"x\":1,\"y\":0}, \"roomSize\": 40, \"enemies\": [{\"variant\":\"Melee\",\"spawnPosition\":{\"x\":0,\"y\":1,\"z\":0}}], \"connections\": [{\"direction\":\"West\",\"targetRoomId\":1},{\"direction\":\"East\",\"targetRoomId\":3}] }," +
            "{ \"roomId\": 3, \"roomType\": \"Boss\", \"gridPosition\": {\"x\":2,\"y\":0}, \"roomSize\": 40, \"enemies\": [{\"variant\":\"Boss\",\"spawnPosition\":{\"x\":0,\"y\":1.5,\"z\":0}}], \"connections\": [{\"direction\":\"West\",\"targetRoomId\":2}] }" +
            "] }";

        private static FloorLayout ParseExpectingEmergency(string json)
        {
            LogAssert.Expect(LogType.Error, new Regex("GameFloors: failed to parse floor"));
            return GameFloors.ParseFloorJson(json, 1, true);
        }

        // --- valid ---

        [Test]
        public void ParseFloorJson_Valid_LoadsGraph_NotCorrupted()
        {
            FloorLayout f = GameFloors.ParseFloorJson(ValidFloor, 1, true);

            Assert.That(f.IsCorrupted, Is.False);
            Assert.That(f.Rooms.Count, Is.EqualTo(3));
            Assert.That(f.StartRoomId, Is.EqualTo(1));
            Assert.That(f.IsLastFloor, Is.True);
        }

        [Test]
        public void ParseFloorJson_FileOrderWinsOverAuthoredIsLastFlag()
        {
            // JSON says isLastFloor: true, but position says it is not the last -> position wins,
            // with a warning.
            LogAssert.Expect(LogType.Warning, new Regex("isLastFloor"));
            FloorLayout f = GameFloors.ParseFloorJson(ValidFloor, 1, isLastByPosition: false);

            Assert.That(f.IsCorrupted, Is.False);
            Assert.That(f.IsLastFloor, Is.False);
        }

        // --- structural faults all degrade to emergency (3-room, IsCorrupted) ---

        [Test]
        public void ParseFloorJson_NotJson_DegradesToEmergency()
        {
            FloorLayout f = ParseExpectingEmergency("this is not json");
            Assert.That(f.IsCorrupted, Is.True);
            Assert.That(f.Rooms.Count, Is.EqualTo(3));
        }

        [Test]
        public void ParseFloorJson_LiteralNull_DegradesToEmergency()
        {
            Assert.That(ParseExpectingEmergency("null").IsCorrupted, Is.True);
        }

        [Test]
        public void ParseFloorJson_NoRooms_DegradesToEmergency()
        {
            Assert.That(ParseExpectingEmergency("{ \"startRoomId\": 1, \"rooms\": [] }").IsCorrupted, Is.True);
        }

        [Test]
        public void ParseFloorJson_NoStartRoom_DegradesToEmergency()
        {
            Assert.That(ParseExpectingEmergency(ValidFloor.Replace("\"Start\"", "\"Combat\"")).IsCorrupted, Is.True);
        }

        [Test]
        public void ParseFloorJson_NoBossRoom_DegradesToEmergency()
        {
            Assert.That(ParseExpectingEmergency(ValidFloor.Replace("\"Boss\"", "\"Combat\"")).IsCorrupted, Is.True);
        }

        [Test]
        public void ParseFloorJson_StartRoomIdNotInRooms_DegradesToEmergency()
        {
            Assert.That(ParseExpectingEmergency(ValidFloor.Replace("\"startRoomId\": 1", "\"startRoomId\": 999")).IsCorrupted, Is.True);
        }

        [Test]
        public void ParseFloorJson_DuplicateRoomId_DegradesToEmergency()
        {
            // make room 3 also id 2
            Assert.That(ParseExpectingEmergency(ValidFloor.Replace("\"roomId\": 3", "\"roomId\": 2")).IsCorrupted, Is.True);
        }

        [Test]
        public void ParseFloorJson_UnknownRoomType_DegradesToEmergency()
        {
            Assert.That(ParseExpectingEmergency(ValidFloor.Replace("\"Combat\"", "\"Lobby\"")).IsCorrupted, Is.True);
        }

        [Test]
        public void ParseFloorJson_UnknownEnemyVariant_DegradesToEmergency()
        {
            Assert.That(ParseExpectingEmergency(ValidFloor.Replace("\"Melee\"", "\"Sniper\"")).IsCorrupted, Is.True);
        }

        [Test]
        public void ParseFloorJson_DanglingConnection_DegradesToEmergency()
        {
            // room 2's East connection now points at a room that does not exist
            Assert.That(ParseExpectingEmergency(ValidFloor.Replace("\"East\",\"targetRoomId\":3", "\"East\",\"targetRoomId\":42")).IsCorrupted, Is.True);
        }

        [Test]
        public void ParseFloorJson_UnreachableRoom_DegradesToEmergency()
        {
            // Add a 4th room that points into the graph, but nothing points back to it -> it is
            // never reached by a BFS from the start room.
            string withOrphan = ValidFloor.Replace(
                "{ \"roomId\": 3, \"roomType\": \"Boss\"",
                "{ \"roomId\": 4, \"roomType\": \"Combat\", \"gridPosition\": {\"x\":1,\"y\":1}, \"roomSize\": 40, \"enemies\": [], \"connections\": [{\"direction\":\"South\",\"targetRoomId\":2}] }," +
                "{ \"roomId\": 3, \"roomType\": \"Boss\"");
            Assert.That(ParseExpectingEmergency(withOrphan).IsCorrupted, Is.True);
        }

        // --- multi-file discovery (LoadAllFloorsFrom) ---

        private string _dir;

        [SetUp]
        public void SetUp()
        {
            _dir = Path.Combine(Path.GetTempPath(), "RogueLikeFloorTests_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_dir);
        }

        [TearDown]
        public void TearDown()
        {
            if (_dir != null && Directory.Exists(_dir))
            {
                Directory.Delete(_dir, true);
            }
        }

        [Test]
        public void LoadAllFloorsFrom_TwoFiles_CountsThem_LastByPosition()
        {
            File.WriteAllText(Path.Combine(_dir, "Floor1.json"), ValidFloor.Replace("\"isLastFloor\": true", "\"isLastFloor\": false"));
            File.WriteAllText(Path.Combine(_dir, "Floor2.json"), ValidFloor);

            var floors = GameFloors.LoadAllFloorsFrom(_dir);

            Assert.That(floors.Count, Is.EqualTo(2));
            Assert.That(floors[0].IsLastFloor, Is.False);
            Assert.That(floors[1].IsLastFloor, Is.True);
        }

        [Test]
        public void LoadAllFloorsFrom_EmptyDir_ReturnsSingleEmergencyFloor()
        {
            LogAssert.Expect(LogType.Error, new Regex("no FloorN.json found"));

            var floors = GameFloors.LoadAllFloorsFrom(_dir);

            Assert.That(floors.Count, Is.EqualTo(1));
            Assert.That(floors[0].IsCorrupted, Is.True);
        }

        [Test]
        public void LoadAllFloorsFrom_StopsAtGap_AndWarns()
        {
            File.WriteAllText(Path.Combine(_dir, "Floor1.json"), ValidFloor);
            File.WriteAllText(Path.Combine(_dir, "Floor3.json"), ValidFloor); // Floor2 missing

            LogAssert.Expect(LogType.Warning, new Regex("Floor2.json is missing"));

            var floors = GameFloors.LoadAllFloorsFrom(_dir);

            Assert.That(floors.Count, Is.EqualTo(1));
        }
    }
}
