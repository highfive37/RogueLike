using System;
using UnityEngine;

// A door built procedurally by RoomController at floor-build time, only for
// directions that have an authored connection - an unconnected direction is
// a plain wall instead. Room floor/walls are a temporary placeholder (will
// be replaced by a themed terrain asset later), so only the doorway itself
// carries a persistent visual: a translucent black marker that stays fully
// invisible while locked and becomes a 70%-alpha dark opening once unlocked,
// independent of whatever the wall ends up looking like. A separate
// invisible collider does the actual physical blocking while locked.
// PassedThrough fires when the player crosses the door's threshold while it
// is unlocked, driving the camera-led room transition (RoomTransitionController).
public class DoorController : MonoBehaviour
{
    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private const float LockedAlpha = 1f;
    private const float UnlockedAlpha = 0.3f;

    private Renderer _markerRenderer;
    private Collider _blocker;
    private MaterialPropertyBlock _propertyBlock;

    public DoorDirection Direction { get; private set; }
    public Transform EntryPoint { get; private set; }
    // The room this door was built on - set once at Setup() time so
    // RoomTransitionController can identify "which room does this door belong
    // to" without walking the transform hierarchy (Phase 3, see Docs/DevLog.md
    // 2026-09-11 - needed once a room's side can carry more than one door).
    public RoomController OwnerRoom { get; private set; }
    public RoomController TargetRoom { get; private set; }
    public bool IsConnected => TargetRoom != null;
    public bool IsLocked { get; private set; } = true;

    public event Action<DoorController> PassedThrough;

    public void Setup(RoomController ownerRoom, DoorDirection direction, Renderer markerRenderer, Collider blocker, Transform entryPoint)
    {
        OwnerRoom = ownerRoom;
        Direction = direction;
        _markerRenderer = markerRenderer;
        _blocker = blocker;
        EntryPoint = entryPoint;
        _propertyBlock = new MaterialPropertyBlock();
    }

    public void Configure(RoomController targetRoom)
    {
        TargetRoom = targetRoom;
    }

    public void Lock()
    {
        IsLocked = true;
        SetBlocking(true);
        SetMarkerAlpha(LockedAlpha);
    }

    public void Unlock()
    {
        IsLocked = false;
        SetBlocking(false);
        SetMarkerAlpha(UnlockedAlpha);
    }

    private void SetBlocking(bool blocking)
    {
        if (_blocker != null)
        {
            _blocker.enabled = blocking;
        }
    }

    private void SetMarkerAlpha(float alpha)
    {
        if (_markerRenderer == null)
        {
            return;
        }

        _markerRenderer.GetPropertyBlock(_propertyBlock);
        _propertyBlock.SetColor(BaseColorId, new Color(0f, 0f, 0f, alpha));
        _markerRenderer.SetPropertyBlock(_propertyBlock);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (IsConnected && !IsLocked && other.CompareTag("Player"))
        {
            PassedThrough?.Invoke(this);
        }
    }
}
