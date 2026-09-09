using System.Collections;
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

    // Duration of the placeholder capsule "collapse" - a scripted tip-over, not physics, since
    // the player is still an unanimated primitive capsule at this stage. Revisit once real
    // character art/animation replaces it (see CLAUDE.md placeholder-geometry note).
    private const float DeathFallDuration = 0.45f;
    private const float DeathFallAngle = 90f;

    private PlayerDodgeRoll _playerDodgeRoll;
    private PickupNotificationUI _pickupNotificationUI;
    private HealthComponent _playerHealth;
    private PlayerInputActions _playerInputActions;
    private Rigidbody _playerRigidbody;
    private CapsuleCollider _playerCapsule;

    private void Awake()
    {
        _playerDodgeRoll = playerPassiveStats != null ? playerPassiveStats.GetComponent<PlayerDodgeRoll>() : null;
        _pickupNotificationUI = playerPassiveStats != null ? playerPassiveStats.GetComponentInChildren<PickupNotificationUI>(true) : null;
        _playerHealth = playerPassiveStats != null ? playerPassiveStats.GetComponent<HealthComponent>() : null;
        _playerInputActions = playerPassiveStats != null ? playerPassiveStats.GetComponent<PlayerInputActions>() : null;
        _playerRigidbody = playerPassiveStats != null ? playerPassiveStats.GetComponent<Rigidbody>() : null;
        _playerCapsule = playerPassiveStats != null ? playerPassiveStats.GetComponent<CapsuleCollider>() : null;
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

        if (_playerRigidbody != null && _playerCapsule != null)
        {
            StartCoroutine(PlayerFallOverRoutine());
        }

        uiManager.ShowGameOver(BuildCurrentStats());
    }

    // Scripted tip-over, not physics (see the class-level note on DeathFallDuration): rotates
    // around the capsule's base rather than its transform origin (the capsule's own center) so
    // it visually topples onto the ground instead of pivoting through it. Runs on unscaled time
    // because PauseState.RequestPause above already zeroed Time.timeScale.
    private IEnumerator PlayerFallOverRoutine()
    {
        _playerRigidbody.isKinematic = true;

        Transform playerTransform = _playerRigidbody.transform;
        Vector3 pivot = playerTransform.position - Vector3.up * (_playerCapsule.height / 2f);
        Vector3 axis = playerTransform.right;

        float rotated = 0f;
        float elapsed = 0f;
        while (elapsed < DeathFallDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float target = Mathf.Lerp(0f, DeathFallAngle, Mathf.Clamp01(elapsed / DeathFallDuration));
            playerTransform.RotateAround(pivot, axis, target - rotated);
            rotated = target;
            yield return null;
        }

        playerTransform.RotateAround(pivot, axis, DeathFallAngle - rotated);
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
