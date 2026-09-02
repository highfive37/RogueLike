using System.Collections.Generic;
using UnityEngine;

// Tracks which active items the player has actually acquired and at what
// level. Separate from PrototypeActiveItemSwitcher: the 1/2/3 debug keys can
// force-equip any item for testing, but that never implies ownership.
// Ownership only changes through Acquire() (item pickups) or the starting
// item below.
//
// Acquire() auto-equips its item when nothing is currently equipped, so a
// player who starts with no starting item (and hasn't opened the equip menu
// yet) can immediately use the very first active item they pick up instead
// of pressing the activate key and getting no response. Once something is
// equipped, later pickups only change ownership - switching stays a
// deliberate ESC-menu action.
public class PlayerActiveItemInventory : MonoBehaviour
{
    [SerializeField] private ActiveItemData startingSkill;
    [SerializeField] private ActiveItemController skillController;

    private readonly List<OwnedActiveItem> _owned = new List<OwnedActiveItem>();

    public IReadOnlyList<OwnedActiveItem> Owned => _owned;
    public ActiveItemData EquippedItem { get; private set; }

    private void Awake()
    {
        if (startingSkill != null)
        {
            _owned.Add(new OwnedActiveItem(startingSkill, 1));
        }
    }

    private void Start()
    {
        if (startingSkill != null)
        {
            skillController.Equip(startingSkill, 1);
            EquippedItem = startingSkill;
        }
    }

    public int GetLevel(ActiveItemData data)
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

    public bool IsOwned(ActiveItemData data)
    {
        return GetLevel(data) > 0;
    }

    public void Acquire(ActiveItemData data)
    {
        bool shouldAutoEquip = EquippedItem == null;

        for (int i = 0; i < _owned.Count; i++)
        {
            if (_owned[i].Data == data)
            {
                _owned[i] = new OwnedActiveItem(data, _owned[i].Level + 1);
                if (shouldAutoEquip)
                {
                    Equip(data);
                }

                return;
            }
        }

        _owned.Add(new OwnedActiveItem(data, 1));
        if (shouldAutoEquip)
        {
            Equip(data);
        }
    }

    public void Equip(ActiveItemData data)
    {
        int level = GetLevel(data);
        if (level <= 0)
        {
            return;
        }

        skillController.Equip(data, level);
        EquippedItem = data;
    }
}
