using System;
using UnityEngine;
using UnityEngine.UI;

public class EquipSlotView : MonoBehaviour
{
    [SerializeField] private Text label;
    [SerializeField] private Button button;

    private int _level;

    public SkillItemData Data { get; private set; }
    public event Action<SkillItemData> Clicked;

    private void Awake()
    {
        button.onClick.AddListener(() => Clicked?.Invoke(Data));
    }

    public void SetOption(SkillItemData data, int level, bool isEquipped)
    {
        Data = data;
        _level = level;
        SetSelected(isEquipped);
    }

    public void SetSelected(bool selected)
    {
        label.text = selected ? $"[장착중] {Data.SkillName} Lv{_level}" : $"{Data.SkillName} Lv{_level}";
    }
}
