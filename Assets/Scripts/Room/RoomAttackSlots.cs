using System.Collections.Generic;
using UnityEngine;

// Room-local "who's standing where around the target" coordinator - keeps engaged enemies from
// all walking to the exact same point and physically jostling each other via PhysX contact
// resolution (2026-09-15, user report: enemies pile up and shake when several converge on the
// player at once; DevLog 2026-09-08 already flagged enemy-enemy collision cost as a real factor
// once local density gets high). Pure/scene-free like RoomPathfindingGrid, owned by
// RoomController.
//
// Engaged enemies are split into two independent rings by role - melee vs ranged, judged by
// EnemyStateMachine.MaxAttackRange - and evenly spaced by angle within whichever ring they
// joined. Angles are recomputed live from each ring's CURRENT membership (index/count), not
// fixed slot indices, so there is no fixed slot count to run out of - a ring with 3 members
// splits them 120 degrees apart, a ring with 12 members splits them 30 degrees apart, and it
// reflows smoothly (no snapping, since GetSlotPosition is read fresh every physics step) as
// enemies join or leave.
//
// Deliberately scoped to only the "approach/hold" positioning enemies use while just closing in
// or waiting between attacks (EnemyChaseState's target position) - momentary attack-pattern
// movement (a melee lunge, ranged kiting's own reposition, the boss's charge) is untouched, so
// this never overrides an already-tested, already-choreographed attack animation mid-flight.
public class RoomAttackSlots
{
    // Judged against EnemyStateMachine.MaxAttackRange rather than reading attack pattern types
    // directly - reuses data the enemy already computes at Start(), no new authoring needed.
    private const float MeleeRoleMaxRange = 2.5f;

    // Both radii are chosen to land just inside each role's typical attack range (melee ~1.2,
    // ranged ~6) so an enemy that reaches its slot is already close enough to attack, not stuck
    // idling just outside range - see EnemyChaseState's own stop-closing check, which compares
    // against MaxAttackRange and is otherwise unaffected by this class.
    private const float MeleeRingRadius = 1.1f;
    private const float RangedRingRadius = 5.5f;

    private readonly List<EnemyStateMachine> _meleeRing = new List<EnemyStateMachine>();
    private readonly List<EnemyStateMachine> _rangedRing = new List<EnemyStateMachine>();
    // Cached ring index per reserved enemy so GetSlotPosition is O(1) instead of re-scanning the
    // ring with List.IndexOf every call - the original version did exactly that, which made a
    // same-role swarm of N enemies cost O(N) per enemy per physics step (O(N^2) total). Measured
    // live (2026-09-15 benchmark, single-role Melee-only - the worst case for this, since a real
    // room mixes roles across two smaller rings): median frame time regressed from Before's
    // 5.11ms/4.43ms to 6.10ms/8.03ms at 100/200 enemies instead of improving like the 50-enemy
    // case did. Release() below is now the only O(ring size) operation, and only for however many
    // entries actually shifted, once per disengage - not every tick for every enemy.
    private readonly Dictionary<EnemyStateMachine, int> _indexByEnemy = new Dictionary<EnemyStateMachine, int>();

    // Idempotent - calling this on an enemy that already holds a slot (e.g. Chase re-entering
    // Chase after a brief Attack round-trip) is a safe no-op.
    public void Reserve(EnemyStateMachine enemy)
    {
        if (_indexByEnemy.ContainsKey(enemy))
        {
            return;
        }

        List<EnemyStateMachine> ring = RingFor(enemy);
        _indexByEnemy[enemy] = ring.Count;
        ring.Add(enemy);
    }

    // Safe to call on an enemy that never held a slot.
    public void Release(EnemyStateMachine enemy)
    {
        if (!_indexByEnemy.TryGetValue(enemy, out int index))
        {
            return;
        }

        List<EnemyStateMachine> ring = RingFor(enemy);
        ring.RemoveAt(index);
        _indexByEnemy.Remove(enemy);

        // Every member after the removed one just shifted down by one place - keep their cached
        // indices in sync.
        for (int i = index; i < ring.Count; i++)
        {
            _indexByEnemy[ring[i]] = i;
        }
    }

    // World position this enemy should currently stand at, orbiting `target`. Falls back to
    // `target` itself if this enemy hasn't reserved a slot (shouldn't normally happen - callers
    // reserve on entering Chase before ever asking for a position).
    public Vector3 GetSlotPosition(EnemyStateMachine enemy, Vector3 target)
    {
        if (!_indexByEnemy.TryGetValue(enemy, out int index))
        {
            return target;
        }

        List<EnemyStateMachine> ring = RingFor(enemy);
        float radius = ring == _meleeRing ? MeleeRingRadius : RangedRingRadius;
        float angle = (360f / ring.Count) * index * Mathf.Deg2Rad;
        Vector3 offset = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * radius;
        return target + offset;
    }

    private List<EnemyStateMachine> RingFor(EnemyStateMachine enemy)
    {
        return enemy.MaxAttackRange <= MeleeRoleMaxRange ? _meleeRing : _rangedRing;
    }
}
