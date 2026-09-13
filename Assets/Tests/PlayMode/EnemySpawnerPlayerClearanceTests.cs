using NUnit.Framework;
using UnityEngine;

namespace RogueLike.Tests
{
    // 2026-09-13: an authored enemy spawn point only ever accounted for room geometry, not
    // wherever the player actually stood when the room activated - in a small/melee room this
    // could spawn an enemy overlapping the player, and the resulting physics push could leave
    // the player drifting indefinitely (see EnemyLineOfSightTests' sibling investigation in
    // DevLog.md 2026-09-13). EnemySpawner.SpawnRoom now pushes the spawn point clear of the
    // player before instantiating there.
    public class EnemySpawnerPlayerClearanceTests
    {
        private readonly TestObjects _t = new TestObjects();

        [TearDown]
        public void TearDown() => _t.DestroyAll();

        [Test]
        public void SpawnRoom_AuthoredPointCoincidesWithPlayer_PushesEnemyClearOfPlayer()
        {
            GameObjectPool pool = BuildEnemyPool();
            EnemySpawner spawner = BuildSpawner(pool);

            Vector3 playerPosition = new Vector3(2f, 0f, 3f);
            _t.MakePlayer(playerPosition);

            var entries = new[] { new StageEnemyEntry(EnemyVariantType.Melee, playerPosition) };
            RoomController room = _t.BuildRoom(Vector3.zero, 16f, entries, spawner);

            room.EnterRoom();
            Physics.SyncTransforms();

            GameObject enemyInstance = pool.transform.GetChild(0).gameObject;
            float distance = Vector3.Distance(enemyInstance.transform.position, playerPosition);

            // Combined capsule radii (0.5 + 0.5) plus the clearance margin - anything less and
            // the two capsules still start interpenetrated.
            Assert.That(distance, Is.GreaterThanOrEqualTo(0.99f));
        }

        [Test]
        public void SpawnRoom_AuthoredPointFarFromPlayer_LeavesSpawnPointUntouched()
        {
            GameObjectPool pool = BuildEnemyPool();
            EnemySpawner spawner = BuildSpawner(pool);

            _t.MakePlayer(new Vector3(-6f, 0f, -6f));

            var authoredPosition = new Vector3(5f, 0f, 5f);
            var entries = new[] { new StageEnemyEntry(EnemyVariantType.Melee, authoredPosition) };
            RoomController room = _t.BuildRoom(Vector3.zero, 16f, entries, spawner);

            room.EnterRoom();
            Physics.SyncTransforms();

            GameObject enemyInstance = pool.transform.GetChild(0).gameObject;

            Assert.That(enemyInstance.transform.position, Is.EqualTo(authoredPosition));
        }

        // Prefab source carries only a CapsuleCollider (radius 0.5, matching the real enemy
        // capsules) - SpawnRoom's EnemyStateMachine/HealthComponent wiring is optional
        // (TryGetComponent), so this stays minimal.
        private GameObjectPool BuildEnemyPool()
        {
            var prefabSource = _t.Track(new GameObject("TEST_EnemyPrefab"));
            prefabSource.SetActive(false);
            var capsule = prefabSource.AddComponent<CapsuleCollider>();
            capsule.radius = 0.5f;

            var poolGo = _t.Track(new GameObject("TEST_EnemyPool"));
            poolGo.SetActive(false);
            var pool = poolGo.AddComponent<GameObjectPool>();
            TestObjects.SetPrivateField(pool, "prefab", prefabSource);
            poolGo.SetActive(true);
            return pool;
        }

        private EnemySpawner BuildSpawner(GameObjectPool meleePool)
        {
            var variantPool = new EnemyVariantPool();
            TestObjects.SetPrivateField(variantPool, "variant", EnemyVariantType.Melee);
            TestObjects.SetPrivateField(variantPool, "pool", meleePool);

            var go = _t.Track(new GameObject("TEST_EnemySpawner"));
            go.SetActive(false);
            var spawner = go.AddComponent<EnemySpawner>();
            TestObjects.SetPrivateField(spawner, "variantPools", new[] { variantPool });
            go.SetActive(true);
            return spawner;
        }
    }
}
