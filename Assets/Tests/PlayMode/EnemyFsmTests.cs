using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace RogueLike.Tests
{
    // A couple of the core EnemyStateMachine transitions. Test enemies are built with an empty
    // attack-pattern set (MaxAttackRange 0), so Chase never hands off to Attack here - that
    // keeps these focused on the detection / stagger edges.
    public class EnemyFsmTests
    {
        private readonly TestObjects _t = new TestObjects();

        [TearDown]
        public void TearDown() => _t.DestroyAll();

        private static IEnemyState CurrentState(EnemyStateMachine esm)
        {
            return TestObjects.GetPrivateField<IEnemyState>(esm, "_currentState");
        }

        [UnityTest]
        public IEnumerator Enemy_EntersChase_WhenPlayerIsWithinDetectionRange()
        {
            _t.MakePlayer(new Vector3(3f, 0f, 0f)); // must exist before the enemy's Start()
            EnemyStateMachine enemy = _t.BuildEnemy(_t.MakeEnemyData(detectionRange: 6f), Vector3.zero);

            yield return null; // Start(): finds the player
            yield return null; // Update(): IdleState.Tick sees the player -> Chase

            Assert.That(CurrentState(enemy), Is.SameAs(enemy.ChaseState));
        }

        [UnityTest]
        public IEnumerator Enemy_StaysIdle_WhenPlayerIsBeyondDetectionRange()
        {
            _t.MakePlayer(new Vector3(50f, 0f, 0f)); // far outside detectionRange 6
            EnemyStateMachine enemy = _t.BuildEnemy(_t.MakeEnemyData(detectionRange: 6f), Vector3.zero);

            yield return null;
            yield return null;

            Assert.That(CurrentState(enemy), Is.SameAs(enemy.IdleState));
        }

        [UnityTest]
        public IEnumerator Enemy_EntersStunned_WhenItTakesDamage()
        {
            EnemyStateMachine enemy = _t.BuildEnemy(_t.MakeEnemyData(stunDuration: 0.15f), Vector3.zero);
            yield return null;

            var health = enemy.GetComponent<HealthComponent>();
            health.TakeDamage(10f, null); // fires HealthComponent.AnyDamageTaken -> HandleDamageTaken -> Stun

            Assert.That(CurrentState(enemy), Is.SameAs(enemy.StunnedState));
        }
    }
}
