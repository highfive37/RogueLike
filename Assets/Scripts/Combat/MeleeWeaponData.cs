using UnityEngine;

[CreateAssetMenu(fileName = "MeleeWeaponData", menuName = "RogueLike/Weapons/Melee Weapon")]
public class MeleeWeaponData : WeaponData
{
    // Damage/Range are hand-tunable balance numbers, so they read from
    // GameBalance (Assets/StreamingAssets/Balance/PlayerBaseStats.json)
    // instead of a serialized field - see WeaponData.Damage.
    public override float Damage => GameBalance.Player.meleeDamage;
    public float Range => GameBalance.Player.meleeRange;

    public override IWeapon CreateWeapon(Transform owner, GameObjectPool projectilePool, PlayerPassiveStats passiveStats)
    {
        return new MeleeWeapon(owner, this, passiveStats);
    }
}
