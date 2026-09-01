using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// Lists every owned active item and lets the player pick a different one to
// equip. Opened/closed on demand via EquipMenuController (ESC) rather than
// being forced after a room clear.
public class ActiveItemEquipUI : MonoBehaviour
{
    [SerializeField] private GameObject panel;
    [SerializeField] private ActiveItemEquipSlotView[] slots;
    [SerializeField] private Button confirmButton;
    [SerializeField] private PlayerStatsPanelUI statsPanel;

    private PlayerActiveItemInventory _inventory;
    private ActiveItemData _selected;
    private Action _onConfirmed;

    private void Awake()
    {
        // Don't call panel.SetActive(false) here: this component lives on
        // panel itself, and panel starts inactive in the saved scene. Because
        // an inactive GameObject's Awake() is deferred, the first-ever
        // Show() -> panel.SetActive(true) call is what triggers this exact
        // Awake() as part of that same activation - a SetActive(false) here
        // would immediately turn it back off mid-cascade, before the
        // activation reaches StatsPanel's own Awake() (which builds the pip
        // bars), silently leaving them null. panel's saved inactive state is
        // already the source of truth for "hidden at scene start."
        confirmButton.onClick.AddListener(HandleConfirm);

        for (int i = 0; i < slots.Length; i++)
        {
            slots[i].Clicked += HandleSlotClicked;
        }
    }

    public void Show(PlayerActiveItemInventory inventory, PlayerPassiveStats passiveStats, Action onConfirmed)
    {
        _inventory = inventory;
        _onConfirmed = onConfirmed;
        _selected = inventory.EquippedItem;

        // Activate the panel before touching its children: StatsPanel starts
        // inactive, so its Awake() (which builds the pip bars) is deferred by
        // Unity until this exact moment - calling Refresh() any earlier would
        // hit that not-yet-initialized state on the very first time this menu
        // opens in a session (confirmed live: Awake() only fired once this
        // SetActive(true) actually ran).
        panel.SetActive(true);

        statsPanel.Refresh(passiveStats);

        IReadOnlyList<OwnedActiveItem> owned = inventory.Owned;
        for (int i = 0; i < slots.Length; i++)
        {
            if (i < owned.Count)
            {
                slots[i].gameObject.SetActive(true);
                bool isEquipped = owned[i].Data == inventory.EquippedItem;
                slots[i].SetOption(owned[i].Data, owned[i].Level, isEquipped);
            }
            else
            {
                slots[i].gameObject.SetActive(false);
            }
        }
    }

    public void Hide()
    {
        panel.SetActive(false);
    }

    private void HandleSlotClicked(ActiveItemData data)
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
