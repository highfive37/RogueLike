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
//
// The player's own death (HealthComponent.Health.Died) is handled here too rather than in a
// separate controller: it is the same shape of event as FloorCompleted - "this attempt just
// ended, show the result screen" - just the losing outcome instead of the winning one, and this
// class already reaches into the Player's components (see _playerDodgeRoll below) to react to
// end-of-run moments.
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
    private HealthComponent _playerHealth;
    private PlayerInputActions _playerInputActions;
    private Rigidbody _playerRigidbody;
    private CapsuleCollider _playerCapsule;
    private PlayerAnimationController _playerAnimation;

    private void Awake()
    {
        _playerDodgeRoll = playerPassiveStats != null ? playerPassiveStats.GetComponent<PlayerDodgeRoll>() : null;
        _pickupNotificationUI = playerPassiveStats != null ? playerPassiveStats.GetComponentInChildren<PickupNotificationUI>(true) : null;
        _playerHealth = playerPassiveStats != null ? playerPassiveStats.GetComponent<HealthComponent>() : null;
        _playerInputActions = playerPassiveStats != null ? playerPassiveStats.GetComponent<PlayerInputActions>() : null;
        _playerRigidbody = playerPassiveStats != null ? playerPassiveStats.GetComponent<Rigidbody>() : null;
        _playerCapsule = playerPassiveStats != null ? playerPassiveStats.GetComponent<CapsuleCollider>() : null;
        _playerAnimation = playerPassiveStats != null ? playerPassiveStats.GetComponentInChildren<PlayerAnimationController>() : null;
    }

    private void OnEnable()
    {
        stageManager.BossRoomCleared += HandleBossRoomCleared;
        stageManager.FloorCompleted += HandleFloorCompleted;
        uiManager.FloorCompleteAutoAdvanceRequested += HandleAutoAdvanceRequested;

        if (_playerHealth != null)
        {
            _playerHealth.Health.Died += HandlePlayerDied;
        }
    }

    private void OnDisable()
    {
        stageManager.BossRoomCleared -= HandleBossRoomCleared;
        stageManager.FloorCompleted -= HandleFloorCompleted;
        uiManager.FloorCompleteAutoAdvanceRequested -= HandleAutoAdvanceRequested;

        if (_playerHealth != null)
        {
            _playerHealth.Health.Died -= HandlePlayerDied;
        }
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

        // Force Idle before FloorCompleteUI.Show() below pauses (PauseState.RequestPause sets
        // Time.timeScale = 0) - without this, walking into the Floor Exit while still holding a
        // move direction freezes PlayerMovement.FixedUpdate() (no fixed steps while paused)
        // before its edge-triggered isMoving check ever fires again, leaving the Animator stuck
        // showing Run's last frame. The Animator itself keeps evaluating through the pause
        // (PlayerAnimationController forces UnscaledTime), so it would visibly keep looping the
        // run cycle in place instead of just holding a frame - reads as "still walking" while
        // stationary (2026-09-15 user report).
        _playerAnimation?.PlayIdle();

        uiManager.ShowFloorComplete(BuildCurrentStats(), stageManager.IsLastFloor);
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

    // The losing counterpart to HandleFloorCompleted: same "run just ended" shape, same stats,
    // different screen. Cancels any in-progress roll for the same reason FloorCompleted does
    // (timeScale is about to hit 0, which would otherwise freeze PlayerDodgeRoll.FixedUpdate
    // mid-roll and let it resume uncontrolled on retry).
    private void HandlePlayerDied()
    {
        if (_playerDodgeRoll != null)
        {
            _playerDodgeRoll.CancelDodgeRoll();
        }

        if (_pickupNotificationUI != null)
        {
            _pickupNotificationUI.HideImmediate();
        }

        if (_playerInputActions != null)
        {
            _playerInputActions.enabled = false;
        }

        PauseState.RequestPause(this);

        // Freeze the body in place rather than the old scripted capsule tip-over (removed) - the
        // Death clip on the player's visual now carries the "died" read. PlayerAnimationController
        // forces its Animator to UnscaledTime specifically so that clip keeps playing through the
        // Time.timeScale=0 RequestPause just set.
        if (_playerRigidbody != null)
        {
            // Zero the velocities before flipping isKinematic - Rigidbody logs a warning if you
            // try to set linear/angularVelocity on a body that's already kinematic.
            _playerRigidbody.linearVelocity = Vector3.zero;
            _playerRigidbody.angularVelocity = Vector3.zero;
            _playerRigidbody.isKinematic = true;
        }

        if (_playerCapsule != null)
        {
            _playerCapsule.enabled = false;
        }

        _playerAnimation?.PlayDeath();

        uiManager.ShowGameOver(BuildCurrentStats());
    }

    // Debug-only (F5/F6 via DebugOverlayController): pops the exact real result screen on
    // demand, so it can be visually checked without actually dying or reaching the last floor.
    // Routes through the same handlers the real events use rather than duplicating their
    // pause/stats/UI-call logic, so the debug preview can never drift from the real thing.
    public void DebugTriggerGameOver()
    {
        HandlePlayerDied();
    }

    public void DebugTriggerLastFloorComplete()
    {
        if (_playerDodgeRoll != null)
        {
            _playerDodgeRoll.CancelDodgeRoll();
        }

        if (_pickupNotificationUI != null)
        {
            _pickupNotificationUI.HideImmediate();
        }

        uiManager.ShowFloorComplete(BuildCurrentStats(), isLastFloor: true);
    }

    // Shared by HandleFloorCompleted (win) and HandlePlayerDied (loss) - same run stats, just
    // shown on different result screens.
    private FloorCompleteUI.FloorCompleteStats BuildCurrentStats()
    {
        return new FloorCompleteUI.FloorCompleteStats
        {
            RoomsCleared = stageManager.RoomsClearedCount,
            PassiveItemsCount = playerPassiveStats.AppliedCount,
            ActiveItemName = playerActiveItemInventory.EquippedItem != null ? playerActiveItemInventory.EquippedItem.ItemName : "None",
            ElapsedSeconds = stageManager.ElapsedTime
        };
    }
}
