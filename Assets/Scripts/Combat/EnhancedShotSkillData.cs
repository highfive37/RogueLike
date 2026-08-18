using UnityEngine;

[CreateAssetMenu(fileName = "EnhancedShotSkillData", menuName = "RogueLike/Skills/Enhanced Shot")]
public class EnhancedShotSkillData : SkillItemData
{
    [SerializeField] private float damage = 12f;
    [SerializeField] private float cooldown = 2f;

    public float Damage => damage;
    public float Cooldown => cooldown;

    public override ISkillItem CreateSkill(Transform owner, SkillContext context, int level)
    {
        return new EnhancedShotSkill(owner, this, context.ProjectilePool, level);
    }
}
