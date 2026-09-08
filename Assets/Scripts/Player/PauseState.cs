using System.Collections.Generic;
using UnityEngine;

// Time.timeScale == 0f is how the game pauses (EquipMenuController's ESC menu, and now
// FloorCompleteUI's result screen). Every Update/FixedUpdate-driven behavior already freezes
// for free once timeScale is zeroed, but Input System action callbacks (performed/canceled)
// fire regardless of timeScale - so any button-triggered player action (attack, dodge roll,
// active item activation) needs to check IsPaused explicitly to avoid executing while paused.
//
// RequestPause/ReleasePause track *who* currently wants the game paused (a plain reference-
// counted set, keyed by the requesting object) rather than each pause source setting
// Time.timeScale directly - two independent sources (ESC menu, Floor Complete screen) used to
// each unconditionally write Time.timeScale on open/close with no awareness of the other, so
// closing one while the other was still up would incorrectly resume the game. Only the last
// owner releasing actually restores Time.timeScale to 1.
public static class PauseState
{
    private static readonly HashSet<object> _owners = new HashSet<object>();

    public static bool IsPaused => Time.timeScale <= 0f;

    public static void RequestPause(object owner)
    {
        _owners.Add(owner);
        Time.timeScale = 0f;
    }

    public static void ReleasePause(object owner)
    {
        _owners.Remove(owner);

        if (_owners.Count == 0)
        {
            Time.timeScale = 1f;
        }
    }

    // Drop every pause owner and resume, regardless of who is still holding a
    // pause. For scene teardown (GameFlow): a scene load neither runs the
    // remaining owners' ReleasePause nor resets the engine-global Time.timeScale,
    // so without this the next scene can start frozen.
    public static void ForceResume()
    {
        _owners.Clear();
        Time.timeScale = 1f;
    }
}
