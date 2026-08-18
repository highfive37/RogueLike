using UnityEngine;

[CreateAssetMenu(fileName = "EnemyData", menuName = "RogueLike/Enemies/Enemy Data")]
public class EnemyData : ScriptableObject
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 3f;
    [SerializeField] private float patrolSpeed = 1.5f;
    [SerializeField] private float patrolRadius = 3f;
    [SerializeField] private float idleDuration = 1.5f;

    [Header("Detection")]
    [SerializeField] private float detectionRange = 6f;
    [SerializeField] private float leashRange = 9f;

    [Header("Attack")]
    [SerializeField] private AttackPatternData[] attackPatterns;

    [Header("Status")]
    [SerializeField] private float stunDuration = 2f;

    public float MoveSpeed => moveSpeed;
    public float PatrolSpeed => patrolSpeed;
    public float PatrolRadius => patrolRadius;
    public float IdleDuration => idleDuration;
    public float DetectionRange => detectionRange;
    public float LeashRange => leashRange;
    public AttackPatternData[] AttackPatterns => attackPatterns;
    public float StunDuration => stunDuration;
}
