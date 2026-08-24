using UnityEngine;

// Stage progression is data-driven via StageManager/EnemySpawner. This class
// only orchestrates the flow around a room clear: show the upgrade pick,
// then the equip screen, then hand off to StageManager for the next stage
// (or the Clear overlay if the stage just cleared was the final one). All UI
// display goes through UIManager rather than direct panel references.
public class RoomFlowManager : MonoBehaviour
{
    [SerializeField] private RoomManager roomManager;
    [SerializeField] private UIManager uiManager;
    [SerializeField] private PlayerSkillInventory playerInventory;
    [SerializeField] private StageManager stageManager;
    [SerializeField] private EnemySpawner enemySpawner;
    [SerializeField] private PlayerInputActions playerInputActions;

    private void Start()
    {
        StageData firstStage = stageManager.BeginFirstStage();
        enemySpawner.SpawnStage(firstStage);
        roomManager.BeginRoom();
    }

    private void OnEnable()
    {
        roomManager.RoomCleared += HandleRoomCleared;
    }

    private void OnDisable()
    {
        roomManager.RoomCleared -= HandleRoomCleared;
    }

    private void HandleRoomCleared()
    {
        if (stageManager.IsCurrentStageFinal)
        {
            uiManager.ShowClearOverlay();
            return;
        }

        playerInputActions.enabled = false;
        uiManager.ShowUpgradeSelection(OnUpgradeChosen);
    }

    private void OnUpgradeChosen(SkillItemData chosen)
    {
        playerInventory.Acquire(chosen);
        uiManager.ShowEquipSelection(playerInventory, OnEquipConfirmed);
    }

    private void OnEquipConfirmed()
    {
        if (!stageManager.TryAdvance(out StageData nextStage))
        {
            Debug.LogWarning("RoomFlowManager: no next stage available after equip confirm.");
            return;
        }

        playerInputActions.enabled = true;
        enemySpawner.SpawnStage(nextStage);
        roomManager.BeginRoom();
    }
}
