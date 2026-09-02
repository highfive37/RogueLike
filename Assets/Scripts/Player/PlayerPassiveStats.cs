using UnityEngine;

// Accumulates permanent stat bonuses from passive item pickups. Unlike
// active items, passive items apply immediately and can be picked up
// repeatedly - there is no "owned" list here, just running totals that other
// systems (PlayerMovement, MeleeWeapon/RangedWeapon, Projectile) read from.
//
// All four bonuses are flat additive amounts in the same unit as their
// underlying base stat (damage, units/sec, units, attacks/sec) - Attack
// Speed used to be a multiplier (percent-of-base), but that made it a
// different shape from the other three (different stacking math, its own
// special-cased pip bar formula) for no strong reason at this project's
// current scope (only two fixed weapons, no need yet for a bonus that
// scales with weapon speed).
public class PlayerPassiveStats : MonoBehaviour
{
    public float AttackDamageBonus { get; private set; }
    public float MoveSpeedBonus { get; private set; }
    public float ProjectileLifetimeBonus { get; private set; }
    public float AttackSpeedBonus { get; private set; }

    // Separate from the four bonuses above since those are summed floats with no way to back
    // out "how many pickups" (different types/amounts). Exists for the Floor Complete result
    // screen's "Passive Items: N" stat.
    public int AppliedCount { get; private set; }

    public void Apply(PassiveItemData data)
    {
        AppliedCount++;

        switch (data.Type)
        {
            case PassiveItemType.AttackPower:
                AttackDamageBonus += data.Amount;
                break;
            case PassiveItemType.MoveSpeed:
                MoveSpeedBonus += data.Amount;
                break;
            case PassiveItemType.ProjectileRange:
                ProjectileLifetimeBonus += data.Amount;
                break;
            case PassiveItemType.AttackSpeed:
                AttackSpeedBonus += data.Amount;
                break;
        }
    }
}
