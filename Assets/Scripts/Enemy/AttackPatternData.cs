using System.Collections.Generic;
using UnityEngine;

public abstract class AttackPatternData : ScriptableObject, IValidatable
{
    [SerializeField] private string patternName;
    [SerializeField] private float range = 1.2f;
    [SerializeField] private float cooldown = 1f;
    [SerializeField] private float damage = 5f;

    // Telegraph pause between Execute() being selected and the pattern actually landing its
    // effect (damage/projectile) - gives the player a fair, readable "tell" instead of getting
    // hit the instant the enemy comes in range and off cooldown.
    [SerializeField] private float windUpDuration = 0.35f;

    public string PatternName => patternName;
    public float Range => range;
    public float Cooldown => cooldown;
    public float Damage => damage;
    public float WindUpDuration => windUpDuration;

    public abstract IAttackPattern CreatePattern(EnemyAttackContext context);

    public virtual void Validate(List<string> errors)
    {
        if (string.IsNullOrWhiteSpace(patternName))
        {
            errors.Add("Pattern Name is empty.");
        }

        if (range <= 0f)
        {
            errors.Add($"Range should be positive (currently {range}).");
        }

        if (cooldown <= 0f)
        {
            errors.Add($"Cooldown should be positive (currently {cooldown}).");
        }

        if (damage < 0f)
        {
            errors.Add($"Damage should not be negative (currently {damage}).");
        }

        if (windUpDuration < 0f)
        {
            errors.Add($"Wind Up Duration should not be negative (currently {windUpDuration}).");
        }
    }

    private void OnValidate()
    {
        DataValidation.LogIssues(this, this);
    }
}
