using UnityEngine;

[CreateAssetMenu(fileName = "FlamethrowerSkillData", menuName = "RogueLike/Skills/Flamethrower")]
public class FlamethrowerSkillData : SkillItemData
{
    [SerializeField] private float damagePerSecond = 3f;
    [SerializeField] private float maxDuration = 7f;
    [SerializeField] private float zoneLength = 3f;
    [SerializeField] private float zoneWidth = 1.5f;

    public float DamagePerSecond => damagePerSecond;
    public float MaxDuration => maxDuration;
    public float ZoneLength => zoneLength;
    public float ZoneWidth => zoneWidth;

    public override ISkillItem CreateSkill(Transform owner, SkillContext context, int level)
    {
        return new FlamethrowerSkill(owner, this, level);
    }
}
