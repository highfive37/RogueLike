using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "EnemyData", menuName = "RogueLike/Enemies/Enemy Data")]
public class EnemyData : ScriptableObject, IValidatable
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 3f;
    [SerializeField] private float patrolSpeed = 1.5f;
    [SerializeField] private float patrolRadius = 3f;
    [SerializeField] private float idleDuration = 1.5f;

    // Perpendicular sine-wave sway added on top of straight-line chase/patrol/retreat movement
    // (see EnemyMovementFlavor) - a stoic heavy enemy can leave this at 0, an erratic one can
    // push it high without touching any movement code.
    [Header("Movement Flavor")]
    [SerializeField] private float wobbleFrequency = 2f;
    [SerializeField] private float wobbleAmplitude = 0.2f;

    [Header("Detection")]
    [SerializeField] private float detectionRange = 6f;
    [SerializeField] private float leashRange = 9f;
    // How long (seconds) EnemyChaseState keeps heading toward the last position the player was
    // seen at after losing line of sight, before giving up and returning to Patrol.
    [SerializeField] private float searchDuration = 4f;
    // When true, EnemyChaseState skips the last-known-position/predicted/look-around search
    // entirely while blind and just keeps re-pathing toward the player's real current position
    // every repath cycle - a "never loses track" enemy that always knows where you are, only
    // blocked by needing to route around obstacles (see EnemyRelentlessMeleeData). Combined
    // with a very large LeashRange/SearchDuration so it never gives up within the room either.
    [SerializeField] private bool tracksLivePositionWhileBlind = false;

    [Header("Attack")]
    [SerializeField] private AttackPatternData[] attackPatterns;

    // stunDuration doubles as the hit-stagger length applied on every incoming hit (see
    // EnemyStateMachine.HandleDamageTaken) - a heavy/boss enemy can set this low so it barely
    // flinches instead of being lockable.
    [Header("Status")]
    [SerializeField] private float stunDuration = 0.15f;

    // Fraction of the baseline knockback distance this enemy resists on hit, 0 = full knockback,
    // 1 = immune. Currently the sole input to knockback distance - once a player-side "knockback
    // power" stat exists, that will combine with this instead of the fixed baseline constant in
    // EnemyStateMachine.
    [SerializeField, Range(0f, 1f)] private float knockbackResistance = 0f;

    public float MoveSpeed => moveSpeed;
    public float PatrolSpeed => patrolSpeed;
    public float PatrolRadius => patrolRadius;
    public float IdleDuration => idleDuration;
    public float DetectionRange => detectionRange;
    public float LeashRange => leashRange;
    public float SearchDuration => searchDuration;
    public bool TracksLivePositionWhileBlind => tracksLivePositionWhileBlind;
    public float WobbleFrequency => wobbleFrequency;
    public float WobbleAmplitude => wobbleAmplitude;
    public AttackPatternData[] AttackPatterns => attackPatterns;
    public float StunDuration => stunDuration;
    public float KnockbackResistance => knockbackResistance;

    public void Validate(List<string> errors)
    {
        if (moveSpeed <= 0f)
        {
            errors.Add($"Move Speed should be positive (currently {moveSpeed}).");
        }

        if (patrolSpeed <= 0f)
        {
            errors.Add($"Patrol Speed should be positive (currently {patrolSpeed}).");
        }

        if (patrolRadius < 0f)
        {
            errors.Add($"Patrol Radius should not be negative (currently {patrolRadius}).");
        }

        if (idleDuration < 0f)
        {
            errors.Add($"Idle Duration should not be negative (currently {idleDuration}).");
        }

        if (detectionRange <= 0f)
        {
            errors.Add($"Detection Range should be positive (currently {detectionRange}).");
        }

        if (leashRange < detectionRange)
        {
            errors.Add($"Leash Range ({leashRange}) should be at least Detection Range ({detectionRange}), or the enemy gives up chasing before it can even see the player.");
        }

        if (searchDuration < 0f)
        {
            errors.Add($"Search Duration should not be negative (currently {searchDuration}).");
        }

        if (stunDuration < 0f)
        {
            errors.Add($"Stun Duration should not be negative (currently {stunDuration}).");
        }

        if (attackPatterns == null || attackPatterns.Length == 0)
        {
            errors.Add("Attack Patterns is empty - this enemy will never attack.");
        }
        else
        {
            for (int i = 0; i < attackPatterns.Length; i++)
            {
                if (attackPatterns[i] == null)
                {
                    errors.Add($"Attack Patterns[{i}] is not assigned.");
                }
            }
        }
    }

    private void OnValidate()
    {
        DataValidation.LogIssues(this, this);
    }
}
