using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;

namespace RogueLike.Tests
{
    // Locks PlayerHitReaction's contract (2026-09-14): a hit shoves the player in the attacker-
    // supplied knockback direction (not a position-delta guess), suspends PlayerMovement for the
    // stun window, and - the tricky part - a second hit landing mid-reaction overwrites the first
    // outright (fresh direction, timers restarted from that moment) rather than blending or
    // queuing.
    public class PlayerHitReactionTests
    {
        private readonly TestObjects _t = new TestObjects();

        [TearDown]
        public void TearDown() => _t.DestroyAll();

        [UnityTest]
        public IEnumerator TakeDamage_KnocksBackInGivenDirection_AndSuspendsMovement()
        {
            var (health, reaction, movement, rb) = BuildPlayer();
            Vector3 start = rb.position;

            health.TakeDamage(10f, null, Vector3.right);

            Assert.That(reaction.IsHitStunned, Is.True);
            Assert.That(movement.enabled, Is.False);

            for (int i = 0; i < 6; i++)
            {
                yield return new WaitForFixedUpdate();
            }

            Assert.That(rb.position.x, Is.GreaterThan(start.x),
                "player should have moved in the +X knockback direction");
        }

        [UnityTest]
        public IEnumerator HitStun_EndsAfterStunDuration_MovementResumes()
        {
            var (health, reaction, movement, _) = BuildPlayer();

            health.TakeDamage(10f, null, Vector3.right);

            yield return new WaitForSeconds(0.45f);

            Assert.That(reaction.IsHitStunned, Is.False);
            Assert.That(movement.enabled, Is.True);
        }

        [UnityTest]
        public IEnumerator SecondHitDuringKnockback_OverridesDirectionAndRestartsTimer()
        {
            var (health, reaction, _, rb) = BuildPlayer();

            health.TakeDamage(10f, null, Vector3.right);

            for (int i = 0; i < 6; i++)
            {
                yield return new WaitForFixedUpdate();
            }

            Vector3 posAfterFirstHit = rb.position;
            Assert.That(posAfterFirstHit.x, Is.GreaterThan(0f), "sanity check: first hit's knockback actually moved the player");

            // A fresh hit lands well before the first hit's own stun would have expired -
            // it must fully take over, not blend with or queue after the first.
            health.TakeDamage(10f, null, Vector3.forward);

            for (int i = 0; i < 6; i++)
            {
                yield return new WaitForFixedUpdate();
            }

            Assert.That(rb.position.z, Is.GreaterThan(posAfterFirstHit.z),
                "the second hit's forward knockback should now be moving the player, overriding the first");

            // The override restarted the stun timer from the second hit, not the first - so at a
            // point that would already be past the FIRST hit's 0.4s window on its own, the player
            // must still be locked out (second hit's own window hasn't elapsed yet).
            Assert.That(reaction.IsHitStunned, Is.True);
        }

        [UnityTest]
        public IEnumerator Knockback_TowardStaticWall_DoesNotTunnelThrough()
        {
            var (health, _, _, rb) = BuildPlayer();
            _t.BuildStaticBlocker(new Vector3(0.6f, 0f, 0f), new Vector3(1f, 3f, 3f));
            Physics.SyncTransforms();

            health.TakeDamage(10f, null, Vector3.right);

            for (int i = 0; i < 20; i++)
            {
                yield return new WaitForFixedUpdate();
            }

            Assert.That(rb.position.x, Is.LessThan(0.5f),
                "knockback aimed at a wall should be clamped, not tunnel the player through it");
        }

        private (HealthComponent health, PlayerHitReaction reaction, PlayerMovement movement, Rigidbody rb) BuildPlayer()
        {
            var go = new GameObject("TEST_Player") { tag = "Player" };
            go.SetActive(false);

            var capsule = go.AddComponent<CapsuleCollider>();
            capsule.radius = 0.5f;
            capsule.height = 2f;

            var rb = go.AddComponent<Rigidbody>();
            rb.useGravity = false;
            rb.constraints = RigidbodyConstraints.FreezePositionY
                | RigidbodyConstraints.FreezeRotationX
                | RigidbodyConstraints.FreezeRotationZ;

            var inputActionsComponent = go.AddComponent<PlayerInputActions>();
#if UNITY_EDITOR
            var asset = UnityEditor.AssetDatabase.LoadAssetAtPath<InputActionAsset>(
                "Assets/InputActions/PlayerActions.inputactions");
            TestObjects.SetPrivateField(inputActionsComponent, "inputActions", asset);
#endif

            var movement = go.AddComponent<PlayerMovement>();
            var health = go.AddComponent<HealthComponent>();
            var reaction = go.AddComponent<PlayerHitReaction>();

            go.SetActive(true);
            _t.Track(reaction);
            return (health, reaction, movement, rb);
        }
    }
}
