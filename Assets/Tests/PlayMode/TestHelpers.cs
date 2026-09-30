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
        // Matches RoomController's own TileSize - only used to size the default obstacle
        // prefab BuildRoom auto-provides below, not to duplicate any of RoomController's real
        // geometry logic.
        private const float DefaultObstacleTileSize = 4f;

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
            float knockbackResistance = 0f,
            float searchDuration = 4f,
            bool tracksLivePositionWhileBlind = false,
            float? attackRange = null)
        {
            var d = ScriptableObject.CreateInstance<EnemyData>();

            AttackPatternData[] patterns;
            if (attackRange.HasValue)
            {
                var pattern = Track(ScriptableObject.CreateInstance<MeleeAttackPatternData>());
                SetPrivateField(pattern, "range", attackRange.Value);
                patterns = new AttackPatternData[] { pattern };
            }
            else
            {
                patterns = Array.Empty<AttackPatternData>();
            }

            SetPrivateField(d, "attackPatterns", patterns);
            SetPrivateField(d, "detectionRange", detectionRange);
            SetPrivateField(d, "leashRange", leashRange);
            SetPrivateField(d, "moveSpeed", moveSpeed);
            SetPrivateField(d, "stunDuration", stunDuration);
            SetPrivateField(d, "knockbackResistance", knockbackResistance);
            SetPrivateField(d, "searchDuration", searchDuration);
            SetPrivateField(d, "tracksLivePositionWhileBlind", tracksLivePositionWhileBlind);
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

            // Matches Enemy_Basic.prefab's capsule - added before EnemyStateMachine so Awake's
            // TryGetComponent<CapsuleCollider> (which populates its Capsule property) sees it.
            var capsule = go.AddComponent<CapsuleCollider>();
            capsule.radius = 0.5f;
            capsule.height = 2f;

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

        public RoomController BuildRoom(Vector3 center, float roomSize, StageEnemyEntry[] enemies = null, EnemySpawner enemySpawner = null, IReadOnlyList<ObstaclePlacement> obstacles = null, RoomObstacleSet obstacleSet = null)
        {
            return BuildRoom(center, RoomFootprint.Square(roomSize), enemies, enemySpawner, obstacles, obstacleSet);
        }

        public RoomController BuildRoom(Vector3 center, RoomFootprint footprint, StageEnemyEntry[] enemies = null, EnemySpawner enemySpawner = null, IReadOnlyList<ObstaclePlacement> obstacles = null, RoomObstacleSet obstacleSet = null)
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
            // Pathfinding now blocks cells from real obstacle Bounds (RoomController.
            // BuildObstacles), not from ObstaclePlacement data alone (2026-09-30) - a caller
            // that passes obstacles but no obstacleSet would otherwise get real prefabs never
            // instantiated, _obstacleBounds left empty, and a pathfinding grid that sees no
            // obstacles at all. Default to a full-tile box so "just route around this
            // placement" tests keep working without every call site wiring one explicitly -
            // a test that cares about a smaller real footprint already passes its own.
            if (obstacleSet == null && obstacles != null && obstacles.Count > 0)
            {
                obstacleSet = MakeObstacleSet();
            }

            if (obstacleSet != null)
            {
                SetPrivateField(room, "obstacleSet", obstacleSet);
            }

            var data = new RoomData(1, RoomType.Combat, Vector2Int.zero, footprint,
                enemies ?? Array.Empty<StageEnemyEntry>(), Array.Empty<RoomDoorConnection>(), obstacles);
            room.Initialize(data, enemySpawner, null);
            return Track(room);
        }

        public GameObject MakePlayer(Vector3 position)
        {
            var go = new GameObject("TEST_Player") { tag = "Player" };
            go.transform.position = position;
            return Track(go);
        }

        // A RoomObstacleSet wired with a bare, otherwise-unused GameObject as its
        // ConnectorPrefab - enough for RoomController.BuildObstacles to actually run its real
        // Connector sizing/placement logic (it early-returns with obstacleSet == null
        // otherwise, which is why no earlier test caught the 2026-09-13 swapped-axis clamp
        // bug: none of them wired one). Instantiate() only needs a GameObject to clone, not a
        // saved asset, so this never touches the project's real obstacle prefabs. soloPrefab
        // defaults to a bare full-tile BoxCollider (2026-09-30) rather than staying unset - a
        // Solo placement with no real collider to measure would otherwise leave the
        // pathfinding grid seeing no obstacle at all (BuildObstacles' Solo branch also
        // early-returns with no prefabs configured). A test that cares about a smaller real
        // footprint still passes its own soloPrefab explicitly and gets that instead.
        public RoomObstacleSet MakeObstacleSet(GameObject soloPrefab = null)
        {
            var connectorPrefab = new GameObject("TEST_ConnectorPrefab");
            Track(connectorPrefab);

            if (soloPrefab == null)
            {
                soloPrefab = new GameObject("TEST_DefaultSoloPrefab");
                BoxCollider collider = soloPrefab.AddComponent<BoxCollider>();
                collider.size = new Vector3(DefaultObstacleTileSize, 3f, DefaultObstacleTileSize);
                Track(soloPrefab);
            }

            var set = ScriptableObject.CreateInstance<RoomObstacleSet>();
            SetPrivateField(set, "connectorPrefab", connectorPrefab);
            SetPrivateField(set, "prefabs", new[] { soloPrefab });

            return Track(set);
        }

        // Mirrors a real wall/room-obstacle collider: non-trigger BoxCollider, no Rigidbody.
        // That combination is what WallClamp/EnemyStateMachine.IsSightBlocked treat as static
        // blocking geometry, as opposed to dynamic bodies (player/enemy capsules) which are
        // non-trigger but DO carry a Rigidbody.
        public GameObject BuildStaticBlocker(Vector3 position, Vector3 size)
        {
            var go = new GameObject("TEST_StaticBlocker");
            go.transform.position = position;
            var box = go.AddComponent<BoxCollider>();
            box.size = size;
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
            // GetField alone only sees fields declared directly on this exact type - a private
            // field declared on a base class (e.g. AttackPatternData.range, set through a
            // MeleeAttackPatternData instance) is otherwise invisible without walking up the
            // hierarchy by hand.
            FieldInfo f = null;
            for (Type t = target.GetType(); t != null && f == null; t = t.BaseType)
            {
                f = t.GetField(name, BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly);
            }

            if (f == null)
            {
                throw new ArgumentException($"no private instance field '{name}' on {target.GetType().Name} or its base types");
            }
            return f;
        }
    }
}
