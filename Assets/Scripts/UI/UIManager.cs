using System;
using UnityEngine;
using UnityEngine.UI;

// Single point of contact for the game's UI screens. Gameplay code
// (RoomFlowManager, CharacterSelectController) goes through this instead of
// holding direct references to each individual UI component.
public class UIManager : MonoBehaviour
{
    [SerializeField] private UpgradeSelectionUI upgradeSelectionUI;
    [SerializeField] private EquipSelectionUI equipSelectionUI;
    [SerializeField] private CharacterSelectUI characterSelectUI;
    [SerializeField] private ClearOverlayUI clearOverlayUI;
    [SerializeField] private StageManager stageManager;
    [SerializeField] private Text stageText;

    private void OnEnable()
    {
        stageManager.StageChanged += HandleStageChanged;
    }

    private void OnDisable()
    {
        stageManager.StageChanged -= HandleStageChanged;
    }

    public void ShowUpgradeSelection(Action<SkillItemData> onChosen)
    {
        upgradeSelectionUI.Show(onChosen);
    }

    public void ShowEquipSelection(PlayerSkillInventory inventory, Action onConfirmed)
    {
        equipSelectionUI.Show(inventory, onConfirmed);
    }

    public void ShowCharacterSelect(Action<CharacterArchetypeData> onChosen)
    {
        characterSelectUI.Show(onChosen);
    }

    public void ShowClearOverlay()
    {
        clearOverlayUI.Show();
    }

    private void HandleStageChanged(int stageIndex)
    {
        stageText.text = $"Stage {stageIndex}";
    }
}
