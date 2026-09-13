using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;

namespace RogueLike.Tests
{
    // 2026-09-13: locks the fix for "player keeps drifting forever after a physics contact
    // (e.g. a spawn-time overlap with an enemy) pushes it". PlayerMovement used to drive
    // Rigidbody.MovePosition to its own current position on zero input, which never cleared
    // any velocity a contact had already imparted - and the Rigidbody has linearDamping 0, so
    // nothing else would clear it either. It now drives Rigidbody.linearVelocity directly,
    // which must zero out any pre-existing velocity the instant there is no input.
    public class PlayerMovementResidualVelocityTests
    {
        private readonly TestObjects _t = new TestObjects();

        [TearDown]
        public void TearDown() => _t.DestroyAll();

        [UnityTest]
        public IEnumerator FixedUpdate_WithNoInput_ZeroesOutPreExistingVelocity()
        {
            PlayerMovement movement = BuildPlayerMovement();
            Rigidbody rb = movement.GetComponent<Rigidbody>();

            // Simulate the residual push a physics contact (the reported spawn-overlap bug)
            // would leave on the Rigidbody.
            rb.linearVelocity = new Vector3(3f, 0f, 0f);

            yield return new WaitForFixedUpdate();

            Assert.That(rb.linearVelocity, Is.EqualTo(Vector3.zero));
        }

        private PlayerMovement BuildPlayerMovement()
        {
            var go = new GameObject("TEST_PlayerMovement");
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

            go.SetActive(true);
            return _t.Track(movement);
        }
    }
}
