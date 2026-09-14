using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "RangedWeaponData", menuName = "RogueLike/Weapons/Ranged Weapon")]
public class RangedWeaponData : WeaponData
{
    [SerializeField] private float projectileSpawnDistance = 1f;

    public float ProjectileSpawnDistance => projectileSpawnDistance;

    // Damage/FireCooldown are hand-tunable balance numbers, so they read
    // from GameBalance (Assets/StreamingAssets/Balance/PlayerBaseStats.json)
    // instead of a serialized field - see WeaponData.Damage.
    public override float Damage => GameBalance.Player.rangedDamage;
    public float FireCooldown => GameBalance.Player.rangedFireCooldown;

    public override IWeapon CreateWeapon(Transform owner, GameObjectPool projectilePool, PlayerPassiveStats passiveStats)
    {
        return new RangedWeapon(owner, this, projectilePool, passiveStats);
    }

    public override void Validate(List<string> errors)
    {
        base.Validate(errors);

        if (projectileSpawnDistance <= 0f)
        {
            errors.Add($"Projectile Spawn Distance should be positive (currently {projectileSpawnDistance}).");
        }
    }
}
