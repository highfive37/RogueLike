using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Drives the camera-led transition between rooms: hides the player, pans the
// camera from its current framing to the entry point of the room being
// entered, then reveals the player there and hands control back to
// QuarterViewCameraFollow. Wired up by StageManager once every room on the
// floor has been built and its doors connected; triggered by a
// DoorController's PassedThrough event.
public class RoomTransitionController : MonoBehaviour
{
    [SerializeField] private Transform player;
    [SerializeField] private Renderer[] playerRenderers;
    [SerializeField] private Rigidbody playerRigidbody;
    [SerializeField] private PlayerInputActions playerInputActions;
    [SerializeField] private QuarterViewCameraFollow cameraFollow;
    [SerializeField] private Camera roomCamera;
    [SerializeField] private float transitionDuration = 0.6f;

    private bool _isTransitioning;
    private PlayerDodgeRoll _playerDodgeRoll;

    private void Awake()
    {
        _playerDodgeRoll = player.GetComponent<PlayerDodgeRoll>();
    }

    public void Initialize(IEnumerable<RoomController> rooms)
    {
        foreach (RoomController room in rooms)
        {
            foreach (DoorController door in room.Doors)
            {
                door.PassedThrough += HandlePassedThrough;
            }
        }
    }

    // Called once by StageManager right before the floor's start room fires EnterRoom() - the
    // very first room a run enters is never reached through a door transition, so nothing else
    // ever moves the player there. Without this the player just stays wherever it was left in
    // the scene, which only ever matched the start room by coincidence (Floor 1's start room
    // happens to sit at world origin, same as the Player prefab's authored position).
    public void PlacePlayerAt(Vector3 position)
    {
        player.position = position;
    }

    private void HandlePassedThrough(DoorController door)
    {
        if (_isTransitioning || door.TargetRoom == null)
        {
            return;
        }

        // Found by which room it leads back to, not by direction - a multi-cell
        // merged room (Phase 3, see Docs/DevLog.md 2026-09-11) can have more than
        // one door facing the same compass direction, so "the opposite-direction
        // door" is no longer necessarily unique. TargetRoom identity always is.
        DoorController arrivalDoor = FindDoorTo(door.TargetRoom, door.OwnerRoom);
        Vector3 arrivalPoint = arrivalDoor != null ? arrivalDoor.EntryPoint.position : door.TargetRoom.transform.position;

        StartCoroutine(TransitionRoutine(door.TargetRoom, arrivalPoint));
    }

    private static DoorController FindDoorTo(RoomController room, RoomController target)
    {
        foreach (DoorController d in room.Doors)
        {
            if (d.TargetRoom == target)
            {
                return d;
            }
        }
        return null;
    }

    private IEnumerator TransitionRoutine(RoomController targetRoom, Vector3 arrivalPoint)
    {
        _isTransitioning = true;

        // A dodge-roll drives the Rigidbody from its own FixedUpdate; if one is still running it
        // would keep MovePosition()-ing the (now kinematic) body all through the camera pan,
        // fighting the transition and widening the Transform/body gap. Stop it up front.
        if (_playerDodgeRoll != null)
        {
            _playerDodgeRoll.CancelDodgeRoll();
        }

        playerInputActions.enabled = false;
        SetPlayerVisible(false);
        playerRigidbody.isKinematic = true;
        cameraFollow.enabled = false;

        Vector3 startFocus = player.position;
        Vector3 offset = cameraFollow.Offset;

        float elapsed = 0f;
        while (elapsed < transitionDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / transitionDuration);
            Vector3 focus = Vector3.Lerp(startFocus, arrivalPoint, t);
            roomCamera.transform.position = focus + offset;
            roomCamera.transform.LookAt(focus);
            yield return null;
        }

        player.position = arrivalPoint;
        // Auto Sync Transforms is off project-wide, so this Transform-only teleport leaves
        // Rigidbody.position stale until the next physics step. PlayerMovement.FixedUpdate reads
        // Rigidbody.position every tick and MovePosition()s toward it - a stale value would drag
        // the player off the entry point at a constant speed (opposite the way they came in).
        // Flush the teleport into the physics body now.
        Physics.SyncTransforms();
        targetRoom.EnterRoom();

        playerRigidbody.isKinematic = false;
        SetPlayerVisible(true);
        playerInputActions.enabled = true;
        cameraFollow.enabled = true;

        _isTransitioning = false;
    }

    private void SetPlayerVisible(bool visible)
    {
        foreach (Renderer r in playerRenderers)
        {
            r.enabled = visible;
        }
    }
}
