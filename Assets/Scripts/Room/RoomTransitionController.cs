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

        DoorController arrivalDoor = door.TargetRoom.GetDoor(Opposite(door.Direction));
        Vector3 arrivalPoint = arrivalDoor != null ? arrivalDoor.EntryPoint.position : door.TargetRoom.transform.position;

        StartCoroutine(TransitionRoutine(door.TargetRoom, arrivalPoint));
    }

    private IEnumerator TransitionRoutine(RoomController targetRoom, Vector3 arrivalPoint)
    {
        _isTransitioning = true;

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

    private static DoorDirection Opposite(DoorDirection direction)
    {
        switch (direction)
        {
            case DoorDirection.North: return DoorDirection.South;
            case DoorDirection.South: return DoorDirection.North;
            case DoorDirection.East: return DoorDirection.West;
            default: return DoorDirection.East;
        }
    }
}
