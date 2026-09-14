using System.Collections.Generic;
using UnityEngine;

// Range = trigger distance to start the dash from. WindUpDuration (inherited from
// AttackPatternData) is repurposed here as the pause AFTER the dash lands, not a telegraph
// before it starts - see ChargeStrikePattern for why.
[CreateAssetMenu(fileName = "ChargeStrikeData", menuName = "RogueLike/Enemies/Attack Patterns/Charge Strike")]
public class ChargeStrikePatternData : AttackPatternData
{
    [SerializeField] private float dashSpeed = 10f;

    // Chance this pattern is actually offered each time its cooldown clears - a low-probability
    // "surprise gap closer" is meant to feel rare, not routine. Rolled once per cooldown cycle by
    // ChargeStrikePattern itself (not every frame it happens to be ready) - see there for why a
    // per-frame roll would make "low probability" meaningless.
    [SerializeField, Range(0f, 1f)] private float selectionChance = 1f;

    public float DashSpeed => dashSpeed;
    public float SelectionChance => selectionChance;

    public override IAttackPattern CreatePattern(EnemyAttackContext context)
    {
        return new ChargeStrikePattern(this);
    }

    public override void Validate(List<string> errors)
    {
        base.Validate(errors);

        if (dashSpeed <= 0f)
        {
            errors.Add($"Dash Speed should be positive (currently {dashSpeed}).");
        }
    }
}
