using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using Object = UnityEngine.Object;

namespace RogueLike.Tests
{
    // Shared scaffolding for PlayMode tests. Most game components take their config from
    // [SerializeField] private fields with no public setters, so tests build instances on an
    // INACTIVE GameObject, reflection-set the fields, then activate - Awake/OnEnable then run
    // once with the config already in place.
    internal sealed class TestObjects
    {
        private readonly List<Object> _spawned = new List<Object>();

        public T Track<T>(T obj) where T : Object
        {
            _spawned.Add(obj);
            return obj;
        }

        public void DestroyAll()
        {
            foreach (Object o in _spawned)
            {
                if (o != null)
                {
                    Object.DestroyImmediate(o is Component c ? c.gameObject : o);
                }
            }
            _spawned.Clear();
        }

        public EnemyData MakeEnemyData(
            float detectionRange = 6f,
            float leashRange = 9f,
            float moveSpeed = 3f,
            float stunDuration = 0.15f,
            float knockbackResistance = 0f)
        {
            var d = ScriptableObject.CreateInstance<EnemyData>();
            SetPrivateField(d, "attackPatterns", Array.Empty<AttackPatternData>());
            SetPrivateField(d, "detectionRange", detectionRange);
            SetPrivateField(d, "leashRange", leashRange);
            SetPrivateField(d, "moveSpeed", moveSpeed);
            SetPrivateField(d, "stunDuration", stunDuration);
            SetPrivateField(d, "knockbackResistance", knockbackResistance);
            SetPrivateField(d, "wobbleAmplitude", 0f); // keep position assertions clean
            return Track(d);
        }

        // Enemy GameObject with EnemyStateMachine (+ auto Rigidbody / HealthComponent), data
        // injected before Awake. Rigidbody set up like the real prefab (no gravity, Y + spin
        // frozen). Start() runs a frame later - yield once if the test needs player lookup.
        public EnemyStateMachine BuildEnemy(EnemyData data, Vector3 position)
        {
            var go = new GameObject("TEST_Enemy");
            go.SetActive(false);
            go.transform.position = position;

            var esm = go.AddComponent<EnemyStateMachine>();
            SetPrivateField(esm, "data", data);

            var rb = go.GetComponent<Rigidbody>();
            rb.useGravity = false;
            rb.constraints = RigidbodyConstraints.FreezePositionY
                | RigidbodyConstraints.FreezeRotationX
                | RigidbodyConstraints.FreezeRotationZ;

            go.SetActive(true);
            return Track(esm);
        }

        public RoomController BuildRoom(Vector3 center, float roomSize)
        {
            return BuildRoom(center, RoomFootprint.Square(roomSize));
        }

        public RoomController BuildRoom(Vector3 center, RoomFootprint footprint)
        {
            var go = new GameObject("TEST_Room");
            go.transform.position = center;
            var room = go.AddComponent<RoomController>(); // pulls in RoomManager via RequireComponent
            // BuildGeometry (Phase 2's tile-based room shell) needs a RoomTileSet -
            // AddComponent alone doesn't wire the one Room_Base.prefab carries, so load
            // the real asset directly (tests only run in-editor, matching this project's CI).
#if UNITY_EDITOR
            var tileSet = UnityEditor.AssetDatabase.LoadAssetAtPath<RoomTileSet>(
                "Assets/Prefabs/RoomTiles/RoomTileSet_ModularSpace.asset");
            SetPrivateField(room, "tileSet", tileSet);
#endif
            var data = new RoomData(1, RoomType.Combat, Vector2Int.zero, footprint,
                Array.Empty<StageEnemyEntry>(), Array.Empty<RoomDoorConnection>());
            room.Initialize(data, null, null);
            return Track(room);
        }

        public GameObject MakePlayer(Vector3 position)
        {
            var go = new GameObject("TEST_Player") { tag = "Player" };
            go.transform.position = position;
            return Track(go);
        }

        public static void SetPrivateField(object target, string name, object value)
        {
            ResolveField(target, name).SetValue(target, value);
        }

        public static T GetPrivateField<T>(object target, string name)
        {
            return (T)ResolveField(target, name).GetValue(target);
        }

        private static FieldInfo ResolveField(object target, string name)
        {
            FieldInfo f = target.GetType().GetField(name, BindingFlags.NonPublic | BindingFlags.Instance);
            if (f == null)
            {
                throw new ArgumentException($"no private instance field '{name}' on {target.GetType().Name}");
            }
            return f;
        }
    }
}
