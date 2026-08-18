using UnityEngine;

public abstract class AttackPatternData : ScriptableObject
{
    [SerializeField] private string patternName;
    [SerializeField] private float range = 1.2f;
    [SerializeField] private float cooldown = 1f;
    [SerializeField] private float damage = 5f;

    public string PatternName => patternName;
    public float Range => range;
    public float Cooldown => cooldown;
    public float Damage => damage;

    public abstract IAttackPattern CreatePattern(EnemyAttackContext context);
}
