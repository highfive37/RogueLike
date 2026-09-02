using System;
using UnityEngine;

// Floor Exit trigger - mirrors DoorController's thin event-only pattern: this only knows
// "the player walked in," not what that means (Floor Complete, next floor, result screen -
// entirely StageManager's/RoomFlowManager's business). The prefab is authored inactive (not
// disabled via code) so it's neither visible nor interactable until Activate() is called when
// the boss room clears - picking up rewards first is never required, only reaching this trigger.
public class FloorExitController : MonoBehaviour
{
    private bool _entered;

    public event Action Entered;

    public void Activate()
    {
        gameObject.SetActive(true);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (_entered || !other.CompareTag("Player"))
        {
            return;
        }

        _entered = true;
        Entered?.Invoke();
    }
}
