using UnityEngine;

// Time.timeScale == 0f is how EquipMenuController pauses the game. Every
// Update/FixedUpdate-driven behavior already freezes for free once
// timeScale is zeroed, but Input System action callbacks (performed/
// canceled) fire regardless of timeScale - so any button-triggered player
// action (attack, dodge roll, active item activation) needs to check this
// explicitly to avoid executing while the pause menu is open.
public static class PauseState
{
    public static bool IsPaused => Time.timeScale <= 0f;
}
