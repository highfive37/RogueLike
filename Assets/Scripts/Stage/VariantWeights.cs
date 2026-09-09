using UnityEngine;

// Relative spawn weights for the four combat enemy variants on a floor. Boss is
// excluded on purpose - it is placed once, in the boss room, not rolled here.
public readonly struct VariantWeights
{
    public readonly int Melee;
    public readonly int Ranged;
    public readonly int ErraticMelee;
    public readonly int BurstRanged;

    public VariantWeights(int melee, int ranged, int erraticMelee, int burstRanged)
    {
        // Negatives would break the weighted pick; clamp defensively.
        Melee = Mathf.Max(0, melee);
        Ranged = Mathf.Max(0, ranged);
        ErraticMelee = Mathf.Max(0, erraticMelee);
        BurstRanged = Mathf.Max(0, burstRanged);
    }

    public int Total => Melee + Ranged + ErraticMelee + BurstRanged;

    // Weighted draw from the enemy stream. If every weight is 0, falls back to Melee.
    public EnemyVariantType Pick(IRng rng)
    {
        int total = Total;
        if (total <= 0)
        {
            return EnemyVariantType.Melee;
        }

        int roll = rng.Range(0, total);
        if ((roll -= Melee) < 0) return EnemyVariantType.Melee;
        if ((roll -= Ranged) < 0) return EnemyVariantType.Ranged;
        if ((roll -= ErraticMelee) < 0) return EnemyVariantType.ErraticMelee;
        return EnemyVariantType.BurstRanged;
    }
}
