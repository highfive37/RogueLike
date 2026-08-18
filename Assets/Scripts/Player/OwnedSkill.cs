public readonly struct OwnedSkill
{
    public readonly SkillItemData Data;
    public readonly int Level;

    public OwnedSkill(SkillItemData data, int level)
    {
        Data = data;
        Level = level;
    }
}
