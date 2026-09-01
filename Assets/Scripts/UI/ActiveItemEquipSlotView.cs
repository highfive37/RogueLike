using System;
using UnityEngine;
using UnityEngine.UI;

public class ActiveItemEquipSlotView : MonoBehaviour
{
    [SerializeField] private Text label;
    [SerializeField] private Button button;

    private int _level;

    public ActiveItemData Data { get; private set; }
    public event Action<ActiveItemData> Clicked;

    private void Awake()
    {
        button.onClick.AddListener(() => Clicked?.Invoke(Data));
    }

    public void SetOption(ActiveItemData data, int level, bool isEquipped)
    {
        Data = data;
        _level = level;
        SetSelected(isEquipped);
    }

    public void SetSelected(bool selected)
    {
        label.text = selected ? $"[장착중] {Data.ItemName} Lv{_level}" : $"{Data.ItemName} Lv{_level}";
    }
}
