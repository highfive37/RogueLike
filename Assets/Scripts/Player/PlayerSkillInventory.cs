using System.Collections.Generic;
using UnityEngine;

// Tracks which skill items the player has actually acquired and at what level.
// Separate from PrototypeSkillSwitcher: the 1/2/3 debug keys can force-equip
// any skill for testing, but that never implies ownership. Ownership only
// changes through Acquire() (upgrade UI) or the starting skill below.
public class PlayerSkillInventory : MonoBehaviour
{
    [SerializeField] private SkillItemData startingSkill;
    [SerializeField] private SkillController skillController;

    private readonly List<OwnedSkill> _owned = new List<OwnedSkill>();

    public IReadOnlyList<OwnedSkill> Owned => _owned;
    public SkillItemData EquippedSkill { get; private set; }

    private void Awake()
    {
        if (startingSkill != null)
        {
            _owned.Add(new OwnedSkill(startingSkill, 1));
        }
    }

    private void Start()
    {
        if (startingSkill != null)
        {
            skillController.Equip(startingSkill, 1);
            EquippedSkill = startingSkill;
        }
    }

    public int GetLevel(SkillItemData data)
    {
        for (int i = 0; i < _owned.Count; i++)
        {
            if (_owned[i].Data == data)
            {
                return _owned[i].Level;
            }
        }

        return 0;
    }

    public bool IsOwned(SkillItemData data)
    {
        return GetLevel(data) > 0;
    }

    public void Acquire(SkillItemData data)
    {
        for (int i = 0; i < _owned.Count; i++)
        {
            if (_owned[i].Data == data)
            {
                _owned[i] = new OwnedSkill(data, _owned[i].Level + 1);
                return;
            }
        }

        _owned.Add(new OwnedSkill(data, 1));
    }

    public void Equip(SkillItemData data)
    {
        int level = GetLevel(data);
        if (level <= 0)
        {
            return;
        }

        skillController.Equip(data, level);
        EquippedSkill = data;
    }
}
