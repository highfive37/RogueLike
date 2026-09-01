using System;
using UnityEngine;
using UnityEngine.UI;

// Single point of contact for the game's UI screens. Gameplay code
// (RoomFlowManager, CharacterSelectController, EquipMenuController) goes
// through this instead of holding direct references to each individual UI
// component.
public class UIManager : MonoBehaviour
{
    [SerializeField] private ActiveItemEquipUI equipSelectionUI;
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

    public void ShowEquipSelection(PlayerActiveItemInventory inventory, PlayerPassiveStats passiveStats, Action onConfirmed)
    {
        equipSelectionUI.Show(inventory, passiveStats, onConfirmed);
    }

    public void HideEquipSelection()
    {
        equipSelectionUI.Hide();
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
