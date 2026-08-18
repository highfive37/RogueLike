using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// Screen 2 of the post-room-clear flow: lists every owned skill item, lets
// the player pick a different one to equip, then confirms into the next room.
public class EquipSelectionUI : MonoBehaviour
{
    [SerializeField] private GameObject panel;
    [SerializeField] private EquipSlotView[] slots;
    [SerializeField] private Button confirmButton;

    private PlayerSkillInventory _inventory;
    private SkillItemData _selected;
    private Action _onConfirmed;

    private void Awake()
    {
        panel.SetActive(false);
        confirmButton.onClick.AddListener(HandleConfirm);

        for (int i = 0; i < slots.Length; i++)
        {
            slots[i].Clicked += HandleSlotClicked;
        }
    }

    public void Show(PlayerSkillInventory inventory, Action onConfirmed)
    {
        _inventory = inventory;
        _onConfirmed = onConfirmed;
        _selected = inventory.EquippedSkill;

        IReadOnlyList<OwnedSkill> owned = inventory.Owned;
        for (int i = 0; i < slots.Length; i++)
        {
            if (i < owned.Count)
            {
                slots[i].gameObject.SetActive(true);
                bool isEquipped = owned[i].Data == inventory.EquippedSkill;
                slots[i].SetOption(owned[i].Data, owned[i].Level, isEquipped);
            }
            else
            {
                slots[i].gameObject.SetActive(false);
            }
        }

        panel.SetActive(true);
    }

    private void HandleSlotClicked(SkillItemData data)
    {
        _selected = data;
        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i].gameObject.activeSelf)
            {
                slots[i].SetSelected(slots[i].Data == data);
            }
        }
    }

    private void HandleConfirm()
    {
        if (_selected != null)
        {
            _inventory.Equip(_selected);
        }

        panel.SetActive(false);
        _onConfirmed?.Invoke();
    }
}
