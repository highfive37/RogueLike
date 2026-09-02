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
    [SerializeField] private FloorCompleteUI floorCompleteUI;
    [SerializeField] private StageManager stageManager;
    [SerializeField] private Text stageText;

    // Relayed from FloorCompleteUI's own event so RoomFlowManager never needs a direct
    // reference to it - stays consistent with the facade this class is meant to be.
    public event Action FloorCompleteAutoAdvanceRequested;

    private void OnEnable()
    {
        stageManager.StageChanged += HandleStageChanged;
        floorCompleteUI.AutoAdvanceRequested += HandleFloorCompleteAutoAdvance;
    }

    private void OnDisable()
    {
        stageManager.StageChanged -= HandleStageChanged;
        floorCompleteUI.AutoAdvanceRequested -= HandleFloorCompleteAutoAdvance;
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

    public void ShowFloorComplete(FloorCompleteUI.FloorCompleteStats stats, bool isLastFloor)
    {
        floorCompleteUI.Show(stats, isLastFloor);
    }

    public void HideFloorComplete()
    {
        floorCompleteUI.Hide();
    }

    public void ReleaseFloorCompletePause()
    {
        floorCompleteUI.ReleasePause();
    }

    private void HandleStageChanged(int stageIndex)
    {
        stageText.text = $"Stage{stageManager.CurrentFloorNumber}";
    }

    private void HandleFloorCompleteAutoAdvance()
    {
        FloorCompleteAutoAdvanceRequested?.Invoke();
    }
}
