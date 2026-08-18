using System;
using UnityEngine;
using UnityEngine.UI;

public class UpgradeSlotView : MonoBehaviour
{
    [SerializeField] private Text label;
    [SerializeField] private Button button;

    public SkillItemData Data { get; private set; }
    public event Action<SkillItemData> Clicked;

    private void Awake()
    {
        button.onClick.AddListener(HandleClick);
    }

    public void SetOption(SkillItemData data, int level)
    {
        Data = data;
        label.text = $"{data.SkillName}\nLv{level}";
    }

    private void HandleClick()
    {
        Clicked?.Invoke(Data);
    }
}
