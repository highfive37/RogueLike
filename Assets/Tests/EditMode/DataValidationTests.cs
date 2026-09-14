using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace RogueLike.Tests
{
    // Covers IValidatable.Validate() on the ScriptableObject data types that own reference or
    // numeric-range checks (Assets/Scripts/Core/IValidatable.cs, DataValidation.cs). Exercises the
    // pure Validate(List<string>) method directly - the same method OnValidate() and the
    // Assets/Editor/DataValidationMenu.cs project scan both call - rather than going through the
    // Editor menu or Inspector.
    public class DataValidationTests
    {
        private readonly List<ScriptableObject> _created = new List<ScriptableObject>();

        [TearDown]
        public void TearDown()
        {
            foreach (ScriptableObject obj in _created)
            {
                Object.DestroyImmediate(obj);
            }

            _created.Clear();
        }

        private T Create<T>() where T : ScriptableObject
        {
            var instance = ScriptableObject.CreateInstance<T>();
            _created.Add(instance);
            return instance;
        }

        private static void SetField(object target, string name, object value)
        {
            System.Type type = target.GetType();

            while (type != null)
            {
                FieldInfo field = type.GetField(name, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                if (field != null)
                {
                    field.SetValue(target, value);
                    return;
                }

                type = type.BaseType;
            }

            Assert.Fail($"Field '{name}' not found on {target.GetType()} or its base types.");
        }

        [Test]
        public void EnemyData_DefaultValues_HasNoErrors()
        {
            var data = Create<EnemyData>();
            SetField(data, "attackPatterns", new AttackPatternData[] { Create<MeleeAttackPatternData>() });

            var errors = new List<string>();
            data.Validate(errors);

            Assert.That(errors, Is.Empty);
        }

        [Test]
        public void EnemyData_EmptyAttackPatterns_ReportsError()
        {
            var data = Create<EnemyData>();
            SetField(data, "attackPatterns", new AttackPatternData[0]);

            var errors = new List<string>();
            data.Validate(errors);

            Assert.That(errors, Has.Some.Contains("Attack Patterns is empty"));
        }

        [Test]
        public void EnemyData_NullAttackPatternElement_ReportsError()
        {
            var data = Create<EnemyData>();
            SetField(data, "attackPatterns", new AttackPatternData[] { null });

            var errors = new List<string>();
            data.Validate(errors);

            Assert.That(errors, Has.Some.Contains("Attack Patterns[0]"));
        }

        [Test]
        public void EnemyData_LeashRangeBelowDetectionRange_ReportsError()
        {
            var data = Create<EnemyData>();
            SetField(data, "attackPatterns", new AttackPatternData[] { Create<MeleeAttackPatternData>() });
            SetField(data, "detectionRange", 10f);
            SetField(data, "leashRange", 5f);

            var errors = new List<string>();
            data.Validate(errors);

            Assert.That(errors, Has.Some.Contains("Leash Range"));
        }

        [Test]
        public void RoomTileSet_MissingTiles_ReportsOneErrorPerMissingTile()
        {
            var set = Create<RoomTileSet>();

            var errors = new List<string>();
            set.Validate(errors);

            Assert.That(errors.Count, Is.EqualTo(4));
        }

        [Test]
        public void RoomTileSet_AllTilesAssigned_HasNoErrors()
        {
            var set = Create<RoomTileSet>();
            var tile = new GameObject("TEST_Tile");
            SetField(set, "floorTile", tile);
            SetField(set, "wallTile", tile);
            SetField(set, "wallHalfTile", tile);
            SetField(set, "cornerTile", tile);

            var errors = new List<string>();
            set.Validate(errors);

            Assert.That(errors, Is.Empty);
            Object.DestroyImmediate(tile);
        }

        [Test]
        public void RoomObstacleSet_MissingPrefabsAndConnector_ReportsBoth()
        {
            var set = Create<RoomObstacleSet>();

            var errors = new List<string>();
            set.Validate(errors);

            Assert.That(errors, Has.Some.Contains("Prefabs is empty"));
            Assert.That(errors, Has.Some.Contains("Connector Prefab"));
        }

        [Test]
        public void RoomObstacleSet_NullElementInPrefabs_ReportsIndex()
        {
            var set = Create<RoomObstacleSet>();
            var connector = new GameObject("TEST_Connector");
            SetField(set, "prefabs", new GameObject[] { null });
            SetField(set, "connectorPrefab", connector);

            var errors = new List<string>();
            set.Validate(errors);

            Assert.That(errors, Has.Some.Contains("Prefabs[0]"));
            Object.DestroyImmediate(connector);
        }

        [Test]
        public void CharacterArchetypeData_MissingNameAndWeapon_ReportsBoth()
        {
            var data = Create<CharacterArchetypeData>();

            var errors = new List<string>();
            data.Validate(errors);

            Assert.That(errors, Has.Some.Contains("Archetype Name"));
            Assert.That(errors, Has.Some.Contains("Starting Weapon"));
        }

        [Test]
        public void RangedWeaponData_NonPositiveSpawnDistance_ReportsError()
        {
            var data = Create<RangedWeaponData>();
            SetField(data, "weaponName", "Test Gun");
            SetField(data, "projectileSpawnDistance", 0f);

            var errors = new List<string>();
            data.Validate(errors);

            Assert.That(errors, Has.Some.Contains("Projectile Spawn Distance"));
        }

        [Test]
        public void AttackPatternData_NegativeCooldownAndOwnField_ReportsBoth()
        {
            var data = Create<MeleeAttackPatternData>();
            SetField(data, "patternName", "Test Lunge");
            SetField(data, "cooldown", -1f);
            SetField(data, "lungeSpeed", -1f);

            var errors = new List<string>();
            data.Validate(errors);

            Assert.That(errors, Has.Some.Contains("Cooldown"));
            Assert.That(errors, Has.Some.Contains("Lunge Speed"));
        }
    }
}
