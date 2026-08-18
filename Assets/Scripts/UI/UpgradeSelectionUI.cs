using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

// Screen 1 of the post-room-clear flow: 3 random slots, each offering either
// a brand-new skill (Lv1) or the next level of one already owned.
public class UpgradeSelectionUI : MonoBehaviour
{
    [SerializeField] private GameObject panel;
    [SerializeField] private UpgradeSlotView[] slots;
    [SerializeField] private SkillItemData[] allSkills;
    [SerializeField] private PlayerSkillInventory playerInventory;

    private Action<SkillItemData> _onChosen;

    private void Awake()
    {
        panel.SetActive(false);

        for (int i = 0; i < slots.Length; i++)
        {
            slots[i].Clicked += HandleSlotClicked;
        }
    }

    public void Show(Action<SkillItemData> onChosen)
    {
        _onChosen = onChosen;

        List<SkillItemData> pool = new List<SkillItemData>(allSkills);
        for (int i = 0; i < slots.Length; i++)
        {
            if (pool.Count == 0)
            {
                pool.AddRange(allSkills);
            }

            int index = Random.Range(0, pool.Count);
            SkillItemData picked = pool[index];
            pool.RemoveAt(index);

            int nextLevel = playerInventory.GetLevel(picked) + 1;
            slots[i].SetOption(picked, nextLevel);
        }

        panel.SetActive(true);
    }

    private void HandleSlotClicked(SkillItemData data)
    {
        panel.SetActive(false);
        _onChosen?.Invoke(data);
    }
}
