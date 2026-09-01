using UnityEngine;

// Reacts to StageManager's FloorCompleted event to show the final Clear
// overlay once the boss room is cleared. The old post-clear reward flow
// (upgrade pick -> equip screen, forced after every combat room) has been
// removed: clearing a room no longer interrupts play. Skill acquisition will
// move to a floor-drop pickup (future item-room work); equipping an owned
// skill is now available on demand via EquipMenuController (ESC).
public class RoomFlowManager : MonoBehaviour
{
    [SerializeField] private UIManager uiManager;
    [SerializeField] private StageManager stageManager;

    private void OnEnable()
    {
        stageManager.FloorCompleted += HandleFloorCompleted;
    }

    private void OnDisable()
    {
        stageManager.FloorCompleted -= HandleFloorCompleted;
    }

    private void HandleFloorCompleted()
    {
        uiManager.ShowClearOverlay();
    }
}
