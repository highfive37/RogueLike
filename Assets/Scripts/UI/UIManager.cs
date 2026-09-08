using System;
using UnityEngine;
using UnityEngine.UI;

// Single point of contact for the gameplay scene's UI screens. Gameplay code
// (RoomFlowManager, EquipMenuController) goes through this instead of holding
// direct references to each individual UI component. Character select is not
// here anymore - it lives in the Title scene (TitleScreenController).
public class UIManager : MonoBehaviour
{
    [SerializeField] private ActiveItemEquipUI equipSelectionUI;
    [SerializeField] private FloorCompleteUI floorCompleteUI;
    [SerializeField] private SystemNotificationUI systemNotificationUI;
    [SerializeField] private StageManager stageManager;
    [SerializeField] private Text stageText;

    // Relayed from FloorCompleteUI's own event so RoomFlowManager never needs a direct
    // reference to it - stays consistent with the facade this class is meant to be.
    public event Action FloorCompleteAutoAdvanceRequested;

    private void OnEnable()
    {
        stageManager.StageChanged += HandleStageChanged;
        stageManager.FloorDataCorrupted += HandleFloorDataCorrupted;
        floorCompleteUI.AutoAdvanceRequested += HandleFloorCompleteAutoAdvance;
    }

    private void OnDisable()
    {
        stageManager.StageChanged -= HandleStageChanged;
        stageManager.FloorDataCorrupted -= HandleFloorDataCorrupted;
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

    private void HandleStageChanged(int roomId)
    {
        // "stage-floor", e.g. 1-3. Only one stage exists yet, so the left number is always 1.
        stageText.text = $"{stageManager.CurrentStageNumber}-{stageManager.CurrentFloorNumber}";
    }

    private void HandleFloorDataCorrupted(int floorNumber)
    {
        systemNotificationUI.Show(
            $"{stageManager.CurrentStageNumber}-{floorNumber} 층 데이터(Floor{floorNumber}.json)가 손상되었습니다.\n파일을 수정해 주세요.");
    }

    private void HandleFloorCompleteAutoAdvance()
    {
        FloorCompleteAutoAdvanceRequested?.Invoke();
    }
}
