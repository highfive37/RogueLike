using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace RogueLike.Tests
{
    // Locks the 2026-09-09 rule that an enemy shot and a player / ally shot pass
    // straight through one another: no mutual cancel, no damage, no despawn.
    // Projectile.OnTriggerEnter returns early when the other collider belongs to
    // another Projectile. (A deliberate cancel / blank mechanic can be added later
    // - this test would then need to move with it.)
    public class ProjectileTests
    {
        private readonly TestObjects _t = new TestObjects();

        [TearDown]
        public void TearDown() => _t.DestroyAll();

        [UnityTest]
        public IEnumerator TwoOpposingProjectiles_Overlapping_BothPassThroughAndStayActive()
        {
            GameObject enemyOwner = _t.Track(new GameObject("TEST_EnemyOwner"));
            GameObject playerOwner = _t.Track(new GameObject("TEST_PlayerOwner"));

            // Start overlapping (centres 0.6 apart, combined trigger radius 1.0) and
            // heading into each other - the exact situation that used to despawn both.
            Projectile enemyShot = BuildProjectile(new Vector3(0f, 0f, -0.3f), Vector3.forward, enemyOwner.transform);
            Projectile playerShot = BuildProjectile(new Vector3(0f, 0f, 0.3f), Vector3.back, playerOwner.transform);

            Physics.SyncTransforms();

            // Several physics steps, still well inside the 0.2s projectile lifetime, so
            // any trigger overlap between the two would have been processed by now.
            for (int i = 0; i < 4; i++)
            {
                yield return new WaitForFixedUpdate();
            }

            Assert.That(enemyShot != null && enemyShot.gameObject.activeSelf, Is.True,
                "enemy projectile should pass through the player projectile, not be cancelled");
            Assert.That(playerShot != null && playerShot.gameObject.activeSelf, Is.True,
                "player projectile should pass through the enemy projectile, not be cancelled");
        }

        // 2026-09-13: confirms the "walls/obstacles already block projectiles" assumption
        // from RoomObstacle work (non-trigger collider, no Rigidbody - same combo the wall
        // cubes use) actually deactivates the projectile via OnTriggerEnter's generic
        // "hit anything that isn't the owner or another Projectile" branch. That branch
        // never inspects owner identity, so this covers enemy-fired and player-fired
        // projectiles identically - both go through this same Projectile/OnTriggerEnter code.
        [UnityTest]
        public IEnumerator Projectile_HitsStaticWall_DespawnsInsteadOfPassingThrough()
        {
            GameObject owner = _t.Track(new GameObject("TEST_Owner"));
            _t.BuildStaticBlocker(new Vector3(0f, 0f, 1f), new Vector3(1f, 3f, 3f));

            Projectile shot = BuildProjectile(Vector3.zero, Vector3.forward, owner.transform);

            Physics.SyncTransforms();

            for (int i = 0; i < 4; i++)
            {
                yield return new WaitForFixedUpdate();
                if (shot == null || !shot.gameObject.activeSelf)
                {
                    break;
                }
            }

            Assert.That(shot != null && shot.gameObject.activeSelf, Is.False,
                "projectile should despawn on hitting a static wall/obstacle collider, not pass through");
        }

        // Mirrors Projectile_Basic.prefab: trigger SphereCollider r=0.5 on a
        // kinematic, no-gravity Rigidbody. Built inactive so Initialize lands after
        // Awake, like the pooled instances the game hands out.
        private Projectile BuildProjectile(Vector3 position, Vector3 direction, Transform owner)
        {
            var go = new GameObject("TEST_Projectile");
            go.SetActive(false);
            go.transform.position = position;

            var sphere = go.AddComponent<SphereCollider>();
            sphere.radius = 0.5f;
            sphere.isTrigger = true;

            var rb = go.AddComponent<Rigidbody>();
            rb.useGravity = false;
            rb.isKinematic = true;

            go.AddComponent<PooledObject>();
            var projectile = go.AddComponent<Projectile>();

            go.SetActive(true);
            projectile.Initialize(direction, 5f, owner);
            return _t.Track(projectile);
        }
    }
}
