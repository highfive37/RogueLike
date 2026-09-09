using NUnit.Framework;
using UnityEngine;

namespace RogueLike.Tests
{
    // Locks the object-pool behaviour, including the 2026-08-31 double-return guard that stops
    // an InvalidOperationException when a projectile's trigger hit and its lifetime expiry both
    // try to return it in the same frame.
    public class PoolTests
    {
        private readonly TestObjects _t = new TestObjects();

        [TearDown]
        public void TearDown() => _t.DestroyAll();

        [Test]
        public void PooledObject_ReturnToPool_CalledTwice_IsSafe_AndDeactivatesOnce()
        {
            var go = _t.Track(new GameObject("TEST_Pooled"));
            var po = go.AddComponent<PooledObject>();
            po.NotifyTakenFromPool(); // armed, as if just handed out

            Assert.DoesNotThrow(() =>
            {
                po.ReturnToPool();
                po.ReturnToPool(); // second call must be a no-op, not a throw
            });
            Assert.That(go.activeSelf, Is.False);
        }

        [Test]
        public void PooledObject_NotifyTakenFromPool_ReArmsTheReturnGuard()
        {
            var go = _t.Track(new GameObject("TEST_Pooled"));
            var po = go.AddComponent<PooledObject>();

            po.NotifyTakenFromPool();
            po.ReturnToPool();      // released
            go.SetActive(true);
            po.NotifyTakenFromPool(); // handed out again -> guard reset

            Assert.DoesNotThrow(() => po.ReturnToPool());
            Assert.That(go.activeSelf, Is.False);
        }

        [Test]
        public void GameObjectPool_Get_ReturnsActiveInstance_ParentedUnderPool()
        {
            GameObjectPool pool = BuildPool(out _);

            GameObject a = pool.Get(new Vector3(1f, 2f, 3f), Quaternion.identity);

            Assert.That(a, Is.Not.Null);
            Assert.That(a.activeSelf, Is.True);
            Assert.That(a.transform.parent, Is.EqualTo(pool.transform));
            Assert.That(a.transform.position, Is.EqualTo(new Vector3(1f, 2f, 3f)));
        }

        [Test]
        public void GameObjectPool_ReleaseThenGet_ReusesTheSameInstance()
        {
            GameObjectPool pool = BuildPool(out _);

            GameObject a = pool.Get(Vector3.zero, Quaternion.identity);
            int id = a.GetInstanceID();
            pool.Release(a);
            Assert.That(a.activeSelf, Is.False);

            GameObject b = pool.Get(Vector3.one, Quaternion.identity);

            Assert.That(b.GetInstanceID(), Is.EqualTo(id)); // recycled, not freshly instantiated
            Assert.That(b.activeSelf, Is.True);
        }

        // GameObjectPool.prefab is a [SerializeField] private with no setter - build on an
        // inactive object, inject it, then activate so Awake sees it.
        private GameObjectPool BuildPool(out GameObject prefabSource)
        {
            prefabSource = _t.Track(GameObject.CreatePrimitive(PrimitiveType.Cube));
            prefabSource.name = "TEST_PoolPrefab";
            prefabSource.SetActive(false); // a disabled template so pooled copies start disabled

            var go = _t.Track(new GameObject("TEST_Pool"));
            go.SetActive(false);
            var pool = go.AddComponent<GameObjectPool>();
            TestObjects.SetPrivateField(pool, "prefab", prefabSource);
            go.SetActive(true);
            return pool;
        }
    }
}
