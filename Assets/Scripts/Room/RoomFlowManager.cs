using UnityEngine;

// Glues StageManager's flow events to gameplay/UI reactions. BossRoomCleared (boss just died)
// spawns the guaranteed boss reward and activates the Floor Exit - play continues uninterrupted.
// FloorCompleted (player actually walked into the Floor Exit) is the real end-of-run moment:
// assembles cheap stats and shows the result screen. The old post-clear reward flow (upgrade
// pick -> equip screen, forced after every combat room) was removed long ago: clearing a room
// no longer interrupts play, and neither does clearing the boss room by itself now.
//
// On the last floor, FloorCompleteUI's own Retry button handles everything (scene reload).
// On any earlier floor, this class owns what happens after FloorCompleteUI's countdown reaches
// 0: play the black-fade transition, rebuild the next floor mid-blackout, then release the
// pause once the fade-in finishes - see FloorTransitionUI/StageManager.AdvanceToNextFloor.
public class RoomFlowManager : MonoBehaviour
{
    [SerializeField] private UIManager uiManager;
    [SerializeField] private StageManager stageManager;
    [SerializeField] private ItemDropManager itemDropManager;
    [SerializeField] private PlayerPassiveStats playerPassiveStats;
    [SerializeField] private PlayerActiveItemInventory playerActiveItemInventory;
    [SerializeField] private FloorTransitionUI floorTransitionUI;

    private PlayerDodgeRoll _playerDodgeRoll;
    private PickupNotificationUI _pickupNotificationUI;

    private void Awake()
    {
        _playerDodgeRoll = playerPassiveStats != null ? playerPassiveStats.GetComponent<PlayerDodgeRoll>() : null;
        _pickupNotificationUI = playerPassiveStats != null ? playerPassiveStats.GetComponentInChildren<PickupNotificationUI>(true) : null;
    }

    private void OnEnable()
    {
        stageManager.BossRoomCleared += HandleBossRoomCleared;
        stageManager.FloorCompleted += HandleFloorCompleted;
        uiManager.FloorCompleteAutoAdvanceRequested += HandleAutoAdvanceRequested;
    }

    private void OnDisable()
    {
        stageManager.BossRoomCleared -= HandleBossRoomCleared;
        stageManager.FloorCompleted -= HandleFloorCompleted;
        uiManager.FloorCompleteAutoAdvanceRequested -= HandleAutoAdvanceRequested;
    }

    private void HandleBossRoomCleared(RoomController room)
    {
        itemDropManager.SpawnGuaranteedRewards(room.transform);
        stageManager.FloorExit.Activate();
    }

    private void HandleFloorCompleted()
    {
        // If the player rolled into the Floor Exit, end the roll now - before FloorCompleteUI
        // pauses (timeScale 0 stops the FixedUpdate that would otherwise finish the roll), or it
        // resumes uncontrolled in the next floor's start room.
        if (_playerDodgeRoll != null)
        {
            _playerDodgeRoll.CancelDodgeRoll();
        }

        // A pickup label triggered just before stepping into the Floor Exit would otherwise
        // hang above the player into the next floor's fade-in (its timer is paused with the game).
        if (_pickupNotificationUI != null)
        {
            _pickupNotificationUI.HideImmediate();
        }

        var stats = new FloorCompleteUI.FloorCompleteStats
        {
            RoomsCleared = stageManager.RoomsClearedCount,
            PassiveItemsCount = playerPassiveStats.AppliedCount,
            ActiveItemName = playerActiveItemInventory.EquippedItem != null ? playerActiveItemInventory.EquippedItem.ItemName : "None",
            ElapsedSeconds = stageManager.ElapsedTime
        };

        uiManager.ShowFloorComplete(stats, stageManager.IsLastFloor);
    }

    private void HandleAutoAdvanceRequested()
    {
        floorTransitionUI.PlayTransition(
            duringBlackout: () =>
            {
                stageManager.AdvanceToNextFloor();
                uiManager.HideFloorComplete();
            },
            onComplete: () =>
            {
                uiManager.ReleaseFloorCompletePause();
            });
    }
}
