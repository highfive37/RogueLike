using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace RogueLike.Tests
{
    // Locks the 2026-09-14 fix: PlayerCombat used to trigger the attack animation the instant the
    // attack button was pressed, regardless of whether the weapon actually fired right then. A
    // ranged press made while still on cooldown gets buffered instead (InputBuffer, see
    // RangedWeapon.Attack) and the real Fire() only happens later inside Tick() - the animation
    // used to play at the press anyway, drifting out of sync with the actual projectile (reported:
    // attack animation and projectile appearing on different presses, projectile sometimes never
    // appearing at all). IWeapon.OnFired exists so callers hook the real fire/hit moment instead.
    public class WeaponOnFiredTimingTests
    {
        private GameObject _owner;
        private RangedWeaponData _data;

        [SetUp]
        public void SetUp()
        {
            _owner = new GameObject("TEST_Owner");
            _data = ScriptableObject.CreateInstance<RangedWeaponData>();
            SetField(_data, "projectileSpawnDistance", 1f);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_owner);
            Object.DestroyImmediate(_data);
        }

        [Test]
        public void RangedWeapon_Attack_OffCooldown_InvokesOnFiredImmediately()
        {
            var weapon = new RangedWeapon(_owner.transform, _data, null, null);
            bool fired = false;
            weapon.OnFired = () => fired = true;

            weapon.Attack();

            Assert.That(fired, Is.True);
        }

        [Test]
        public void RangedWeapon_Attack_OnCooldown_DoesNotInvokeOnFiredUntilTheBufferedShotActuallyFires()
        {
            float cooldown = GameBalance.Player.rangedFireCooldown;
            Assert.That(cooldown, Is.GreaterThan(InputBuffer.DefaultWindowSeconds),
                "test presses near the end of cooldown, inside the buffer window - assumes cooldown outlasts the window (true for the current balance data)");

            var weapon = new RangedWeapon(_owner.transform, _data, null, null);
            int fireCount = 0;
            weapon.OnFired = () => fireCount++;

            weapon.Attack(); // off cooldown - fires immediately
            Assert.That(fireCount, Is.EqualTo(1));

            // Advance to just inside the buffer window before cooldown clears - a "near miss"
            // press here is exactly what InputBuffer exists to rescue (see its own doc comment).
            weapon.Tick(cooldown - 0.05f);
            weapon.Attack(); // still (barely) on cooldown - must NOT fire yet
            Assert.That(fireCount, Is.EqualTo(1),
                "a buffered press must not invoke OnFired until the weapon actually fires");

            weapon.Tick(0.06f); // crosses the remaining cooldown, well inside the buffer window

            Assert.That(fireCount, Is.EqualTo(2),
                "OnFired should fire exactly when the buffered shot actually goes off, inside Tick()");
        }

        [Test]
        public void RangedWeapon_Attack_PastBufferWindow_NeverInvokesOnFiredForThatPress()
        {
            float cooldown = GameBalance.Player.rangedFireCooldown;
            Assert.That(cooldown, Is.GreaterThan(InputBuffer.DefaultWindowSeconds),
                "test assumes the fire cooldown outlasts the input buffer window (true for the current balance data)");

            var weapon = new RangedWeapon(_owner.transform, _data, null, null);
            int fireCount = 0;
            weapon.OnFired = () => fireCount++;

            weapon.Attack();
            Assert.That(fireCount, Is.EqualTo(1));

            weapon.Attack(); // buffered
            weapon.Tick(InputBuffer.DefaultWindowSeconds + 0.01f); // buffer window expires before cooldown does
            weapon.Tick(cooldown); // cooldown clears later, but the buffered press is already gone

            Assert.That(fireCount, Is.EqualTo(1),
                "a press that falls outside the buffer window should never fire - matches the reported " +
                "'projectile sometimes never comes out' symptom");
        }

        [Test]
        public void MeleeWeapon_Attack_AlwaysInvokesOnFiredImmediately()
        {
            var data = ScriptableObject.CreateInstance<MeleeWeaponData>();
            var weapon = new MeleeWeapon(_owner.transform, data, null);
            bool fired = false;
            weapon.OnFired = () => fired = true;

            weapon.Attack();

            Assert.That(fired, Is.True);
            Object.DestroyImmediate(data);
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
    }
}
