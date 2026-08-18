using UnityEngine;

[CreateAssetMenu(fileName = "GrenadeSkillData", menuName = "RogueLike/Skills/Grenade")]
public class GrenadeSkillData : SkillItemData
{
    [SerializeField] private float fuseTime = 2.5f;
    [SerializeField] private float throwDistance = 2.5f;
    [SerializeField] private float throwTravelTime = 0.25f;
    [SerializeField] private float explosionRadius = 1f;
    [SerializeField] private float damage = 15f;
    [SerializeField] private float cooldown = 3f;

    public float FuseTime => fuseTime;
    public float ThrowDistance => throwDistance;
    public float ThrowTravelTime => throwTravelTime;
    public float ExplosionRadius => explosionRadius;
    public float Damage => damage;
    public float Cooldown => cooldown;

    public override ISkillItem CreateSkill(Transform owner, SkillContext context, int level)
    {
        return new GrenadeSkill(owner, this, context.GrenadePool, level);
    }
}
