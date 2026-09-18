using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace RogueLike.Tests
{
    // Locks ChargeStrikePattern's telegraph + dash + hitbox sequence. 2026-09-15: hit resolution
    // moved from a single distance check at the end of a post-dash pause to a trigger hitbox
    // (EnemyChargeHitbox) armed for the whole dash - the old approach let the hit land well after
    // the dash had already visibly stopped, which read as "just getting bumped, no real attack
    // landing". The pattern's WindUpDuration field is a post-dash recovery hold, not a pre-hit
    // telegraph. 2026-09-18: TelegraphDuration is the actual pre-hit telegraph - a hold before the
    // dash even starts, during which nothing dash-related may happen yet.
    public class ChargeStrikePatternPhysicsTests
    {
        private readonly TestObjects _t = new TestObjects();
        private HealthComponent _playerHealth;
        private float _damageDealt;
        private System.Action<HealthComponent, float, Transform, Vector3> _handler;

        [SetUp]
        public void SetUp()
        {
            _damageDealt = 0f;
            _handler = (sender, amount, source, direction) =>
            {
                if (sender == _playerHealth)
                {
                    _damageDealt = amount;
                }
            };
            HealthComponent.AnyDamageTaken += _handler;
        }

        [TearDown]
        public void TearDown()
        {
            HealthComponent.AnyDamageTaken -= _handler;
            _t.DestroyAll();
        }

        [UnityTest]
        public IEnumerator Execute_HitboxLandsDamageDuringDash_NotJustOnce_ButOnlyOnce()
        {
            GameObject playerGo = _t.MakePlayer(new Vector3(6f, 0f, 0f));
            playerGo.AddComponent<CapsuleCollider>();
            playerGo.AddComponent<Rigidbody>().isKinematic = true;
            _playerHealth = playerGo.AddComponent<HealthComponent>();

            EnemyStateMachine enemy = _t.BuildEnemy(_t.MakeEnemyData(), Vector3.zero);
            yield return null; // Start(): populates Player/PlayerCollider/PlayerDamageable
            StopFsmAutoTick(enemy);
            AddChargeHitbox(enemy);

            var data = ScriptableObject.CreateInstance<ChargeStrikePatternData>();
            TestObjects.SetPrivateField(data, "range", 8f);
            TestObjects.SetPrivateField(data, "cooldown", 2.5f);
            TestObjects.SetPrivateField(data, "damage", 15f);
            TestObjects.SetPrivateField(data, "windUpDuration", 0.2f);
            TestObjects.SetPrivateField(data, "dashSpeed", 10f);
            TestObjects.SetPrivateField(data, "selectionChance", 1f);
            // Zeroed out - this test is about the dash/hitbox sequence, not the pre-dash
            // telegraph (see ChargeStrikePatternTelegraphTests), so the dash should begin on the
            // very first PhysicsTick.
            TestObjects.SetPrivateField(data, "telegraphDuration", 0f);
            _t.Track(data);

            var pattern = new ChargeStrikePattern(data);
            pattern.Execute(enemy);

            // Gap starts at 6, closing at 10/s. The hitbox's front face sits 1.5 units out from
            // the boss centre, so contact begins once the gap drops under ~2 - comfortably before
            // the dash itself stops (~0.49s, gap reaches the 1.1 contact buffer) or the 0.2s
            // recovery even starts.
            for (int i = 0; i < 30 && _damageDealt == 0f; i++)
            {
                pattern.PhysicsTick(enemy, Time.fixedDeltaTime);
                yield return new WaitForFixedUpdate();
            }

            Assert.That(_damageDealt, Is.EqualTo(15f), "the hitbox should have landed the hit during the dash itself");
            Assert.That(pattern.IsBusy, Is.True, "dash/recovery sequence shouldn't be fully done this early");

            // Keep ticking with the player still sitting inside the box's reach - must not
            // multi-hit even though the trigger could re-overlap across several physics steps.
            for (int i = 0; i < 10; i++)
            {
                pattern.PhysicsTick(enemy, Time.fixedDeltaTime);
                yield return new WaitForFixedUpdate();
            }

            Assert.That(_damageDealt, Is.EqualTo(15f), "must still be exactly one hit's worth of damage");
        }

        // 2026-09-18: Execute() no longer launches the dash on the spot - it holds for
        // TelegraphDuration first (facing the dash direction, warning VFX at the boss's head)
        // so the player has an actual tell before the boss commits. Nothing dash-related (motion,
        // collision-ignore, the hitbox) may start until that hold elapses.
        [UnityTest]
        public IEnumerator Execute_HoldsDuringTelegraph_ThenDashesOnceItElapses()
        {
            GameObject playerGo = _t.MakePlayer(new Vector3(6f, 0f, 0f));
            var playerCapsule = playerGo.AddComponent<CapsuleCollider>();
            playerGo.AddComponent<Rigidbody>().isKinematic = true;
            _playerHealth = playerGo.AddComponent<HealthComponent>();

            EnemyStateMachine enemy = _t.BuildEnemy(_t.MakeEnemyData(), Vector3.zero);
            yield return null;
            StopFsmAutoTick(enemy);
            AddChargeHitbox(enemy);

            var data = ScriptableObject.CreateInstance<ChargeStrikePatternData>();
            TestObjects.SetPrivateField(data, "range", 8f);
            TestObjects.SetPrivateField(data, "cooldown", 2.5f);
            TestObjects.SetPrivateField(data, "damage", 15f);
            TestObjects.SetPrivateField(data, "dashSpeed", 10f);
            TestObjects.SetPrivateField(data, "selectionChance", 1f);
            TestObjects.SetPrivateField(data, "telegraphDuration", 0.2f);
            _t.Track(data);

            var pattern = new ChargeStrikePattern(data);
            pattern.Execute(enemy);

            Assert.That(pattern.IsBusy, Is.True, "telegraphing counts as busy");
            Assert.That(Physics.GetIgnoreCollision(enemy.Capsule, playerCapsule), Is.False,
                "the dash hasn't started yet - nothing should be ignored during the telegraph");

            Vector3 positionBeforeTelegraph = enemy.transform.position;

            // 0.2s of telegraph at a ~0.02s fixed step is ~10 ticks - stop just short of that so
            // every tick below is still inside the hold.
            for (int i = 0; i < 8; i++)
            {
                pattern.PhysicsTick(enemy, Time.fixedDeltaTime);
                yield return new WaitForFixedUpdate();
            }

            Assert.That(enemy.transform.position, Is.EqualTo(positionBeforeTelegraph),
                "the boss must not move at all while still telegraphing");
            Assert.That(Physics.GetIgnoreCollision(enemy.Capsule, playerCapsule), Is.False,
                "still telegraphing - collision must still not be ignored");
            Assert.That(_damageDealt, Is.EqualTo(0f), "the hitbox must not be armed during the telegraph");

            // Push past the remaining telegraph time and into the dash.
            for (int i = 0; i < 30 && _damageDealt == 0f; i++)
            {
                pattern.PhysicsTick(enemy, Time.fixedDeltaTime);
                yield return new WaitForFixedUpdate();
            }

            Assert.That(_damageDealt, Is.EqualTo(15f), "the dash should land once the telegraph elapses");
        }

        // Interrupting before the boss has even committed to the dash (hit-stagger, death, or
        // anything else that can cut an attack short) must leave nothing armed or ignored behind -
        // there is nothing to unwind yet, but IsBusy must still clear so the FSM can move on.
        [UnityTest]
        public IEnumerator Interrupt_DuringTelegraph_ClearsBusyWithoutTouchingCollisionOrHitbox()
        {
            GameObject playerGo = _t.MakePlayer(new Vector3(3f, 0f, 0f));
            var playerCapsule = playerGo.AddComponent<CapsuleCollider>();
            playerGo.AddComponent<HealthComponent>();

            EnemyStateMachine enemy = _t.BuildEnemy(_t.MakeEnemyData(), Vector3.zero);
            yield return null;
            StopFsmAutoTick(enemy);
            AddChargeHitbox(enemy);

            var data = ScriptableObject.CreateInstance<ChargeStrikePatternData>();
            TestObjects.SetPrivateField(data, "range", 8f);
            TestObjects.SetPrivateField(data, "dashSpeed", 10f);
            TestObjects.SetPrivateField(data, "selectionChance", 1f);
            TestObjects.SetPrivateField(data, "telegraphDuration", 0.3f);
            _t.Track(data);

            var pattern = new ChargeStrikePattern(data);
            pattern.Execute(enemy);
            pattern.PhysicsTick(enemy, Time.fixedDeltaTime); // still well inside the 0.3s telegraph

            pattern.Interrupt();

            Assert.That(pattern.IsBusy, Is.False, "interrupting must clear the telegraph, not just the dash");
            Assert.That(Physics.GetIgnoreCollision(enemy.Capsule, playerCapsule), Is.False,
                "collision was never ignored yet - interrupting mid-telegraph must not touch it");
        }

        // 2026-09-15: a fast-closing dash could end up physically overlapping the player's
        // capsule by the time it stopped, and the solver's depenetration would shove them apart
        // over the next few steps. ChargeStrikePattern narrowly ignores collision between just
        // its own capsule and the player's (not a layer-wide ignore - the player capsule and its
        // projectiles share the same Default layer, so that would also blind ranged attacks
        // against this enemy for the whole dash+pause) for the duration of the attack.
        [UnityTest]
        public IEnumerator Execute_IgnoresCollisionWithPlayerThroughDashAndPause_RestoresAfterRecovery()
        {
            GameObject playerGo = _t.MakePlayer(new Vector3(3f, 0f, 0f));
            var playerCapsule = playerGo.AddComponent<CapsuleCollider>();
            // A Rigidbody is required here to match the real Player prefab - HasClearLineToPlayer
            // treats a hit against a collider with NO Rigidbody as blocking geometry (that's how
            // it tells walls apart from other bodies, see EnemyStateMachine.IsSightBlocked).
            playerGo.AddComponent<Rigidbody>().isKinematic = true;
            _playerHealth = playerGo.AddComponent<HealthComponent>();

            EnemyStateMachine enemy = _t.BuildEnemy(_t.MakeEnemyData(), Vector3.zero);
            yield return null; // Start(): populates Player/PlayerCollider/PlayerDamageable
            StopFsmAutoTick(enemy);
            AddChargeHitbox(enemy);

            var data = ScriptableObject.CreateInstance<ChargeStrikePatternData>();
            TestObjects.SetPrivateField(data, "range", 8f);
            TestObjects.SetPrivateField(data, "cooldown", 2.5f);
            TestObjects.SetPrivateField(data, "damage", 15f);
            TestObjects.SetPrivateField(data, "windUpDuration", 0.2f);
            TestObjects.SetPrivateField(data, "dashSpeed", 10f);
            TestObjects.SetPrivateField(data, "selectionChance", 1f);
            // Zeroed out - this test is about the dash/recovery sequence, not the pre-dash
            // telegraph (see ChargeStrikePatternTelegraphTests), so the dash begins on the very
            // first PhysicsTick.
            TestObjects.SetPrivateField(data, "telegraphDuration", 0f);
            _t.Track(data);

            var pattern = new ChargeStrikePattern(data);

            Assert.That(Physics.GetIgnoreCollision(enemy.Capsule, playerCapsule), Is.False,
                "should not be ignored before the attack starts");

            pattern.Execute(enemy);

            Assert.That(Physics.GetIgnoreCollision(enemy.Capsule, playerCapsule), Is.False,
                "should still not be ignored during the (zero-length) pre-dash telegraph");

            for (int i = 0; i < 45 && pattern.IsBusy; i++)
            {
                pattern.PhysicsTick(enemy, Time.fixedDeltaTime);
                yield return new WaitForFixedUpdate();

                if (i == 0)
                {
                    Assert.That(Physics.GetIgnoreCollision(enemy.Capsule, playerCapsule), Is.True,
                        "should be ignored the instant the dash actually starts");
                }
            }

            Assert.That(pattern.IsBusy, Is.False, "attack should have fully resolved by now");
            Assert.That(_damageDealt, Is.EqualTo(15f), "sanity check: the strike actually landed");
            Assert.That(Physics.GetIgnoreCollision(enemy.Capsule, playerCapsule), Is.False,
                "should be restored once the recovery hold ends");
        }

        [UnityTest]
        public IEnumerator Interrupt_MidDash_RestoresCollisionImmediately()
        {
            GameObject playerGo = _t.MakePlayer(new Vector3(3f, 0f, 0f));
            var playerCapsule = playerGo.AddComponent<CapsuleCollider>();
            playerGo.AddComponent<HealthComponent>();

            EnemyStateMachine enemy = _t.BuildEnemy(_t.MakeEnemyData(), Vector3.zero);
            yield return null;
            StopFsmAutoTick(enemy);

            var data = ScriptableObject.CreateInstance<ChargeStrikePatternData>();
            TestObjects.SetPrivateField(data, "range", 8f);
            TestObjects.SetPrivateField(data, "dashSpeed", 10f);
            TestObjects.SetPrivateField(data, "selectionChance", 1f);
            // Zeroed out so the dash (not just the telegraph) is what's actually mid-flight when
            // Interrupt() below is called - matches the test name.
            TestObjects.SetPrivateField(data, "telegraphDuration", 0f);
            _t.Track(data);

            var pattern = new ChargeStrikePattern(data);
            pattern.Execute(enemy);
            pattern.PhysicsTick(enemy, Time.fixedDeltaTime); // clears the (zero-length) telegraph, starts the dash
            Assert.That(Physics.GetIgnoreCollision(enemy.Capsule, playerCapsule), Is.True);

            pattern.Interrupt();

            Assert.That(Physics.GetIgnoreCollision(enemy.Capsule, playerCapsule), Is.False,
                "an interrupted attack must not leave collision permanently ignored");
        }

        // Mirrors Boss_Basic.prefab's authored "ChargeHitbox" child (box 1x2x1, offset 1 unit
        // forward) - TestObjects.BuildEnemy doesn't include one, so tests that need the real
        // dash-hit flow (not just the pure pattern-logic timing) add it themselves.
        // EnemyStateMachine.ChargeHitbox is only populated by Awake()'s GetComponentInChildren,
        // which already ran by the time a test can add this child - reflection sets the property's
        // backing field directly instead of relying on a second Awake pass.
        private static void AddChargeHitbox(EnemyStateMachine enemy)
        {
            var go = new GameObject("TEST_ChargeHitbox");
            go.transform.SetParent(enemy.transform, false);
            go.transform.localPosition = new Vector3(0f, 0f, 1f);

            var box = go.AddComponent<BoxCollider>();
            box.size = new Vector3(1f, 2f, 1f);
            box.isTrigger = true;

            var hitbox = go.AddComponent<EnemyChargeHitbox>();
            go.SetActive(false);

            TestObjects.SetPrivateField(enemy, "<ChargeHitbox>k__BackingField", hitbox);
        }

        private static void StopFsmAutoTick(EnemyStateMachine enemy)
        {
            // EnemyStateMachine's own FixedUpdate would otherwise keep ticking whatever state
            // OnEnable put it in (Idle, which can transition itself into Chase) on the same
            // Rigidbody this test drives manually via ChargeStrikePattern - null out the current
            // state so only this test's own PhysicsTick() calls move the enemy.
            TestObjects.SetPrivateField(enemy, "_currentState", null);
        }
    }
}
