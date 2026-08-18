using UnityEngine;

public abstract class SkillItemData : ScriptableObject
{
    [SerializeField] private string skillName;

    public string SkillName => skillName;

    public abstract ISkillItem CreateSkill(Transform owner, SkillContext context, int level);
}
