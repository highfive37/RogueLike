using UnityEngine;

// The single place that knows the game's top-level flow: which scenes exist, how
// you move between them, and the one piece of state that has to survive a scene
// load - the archetype the player picked on the title screen, before the
// gameplay scene (and its Player) even exists.
//
// Static on purpose: there is exactly one game flow, it has no per-frame
// behaviour, and a button-click handler in any scene must be able to reach it
// without that scene holding a reference to a manager object. Nothing here is a
// MonoBehaviour, so there is no GameObject to place, lose, or duplicate across
// scenes. When a run needs more carried state (settings, meta-progression, a
// seed), this grows a small serializable "run config" rather than a pile of
// statics - not needed yet.
public static class GameFlow
{
    // Scene NAMES, not build indices: indices silently shift when the Build
    // Settings list is reordered, names don't. Both scenes must be in
    // File > Build Settings, Title first (index 0) so it is the startup scene.
    public const string TitleScene = "Title";
    public const string GameplayScene = "Prototype";

    // Set on the title screen the instant an archetype is chosen; read once by
    // the gameplay scene's CharacterSelectController when the Player spawns. It's
    // a ScriptableObject asset reference, so it survives the scene load intact
    // (assets are not unloaded between scenes). Null only when the gameplay scene
    // is entered directly in the editor without going through the title -
    // CharacterSelectController falls back to a serialized default for that.
    public static CharacterArchetypeData PendingArchetype { get; set; }

    // Title "Start" -> an archetype was chosen -> load into gameplay.
    public static void StartRun(CharacterArchetypeData archetype)
    {
        PendingArchetype = archetype;
        LoadScene(GameplayScene);
    }

    // "다시하기" on the clear screen: straight back into a fresh gameplay scene,
    // reusing the archetype already picked (no trip through the title). The scene
    // reload is what resets the whole run - Player, StageManager, pools, and
    // GameFloors' cached floor data all come back fresh.
    public static void RetryRun()
    {
        LoadScene(GameplayScene);
    }

    // "타이틀로" on the clear screen: back to square one, archetype cleared.
    public static void ReturnToTitle()
    {
        PendingArchetype = null;
        LoadScene(TitleScene);
    }

    private static void LoadScene(string sceneName)
    {
        // Fade out -> load -> fade in, through the persistent SceneTransitionController
        // overlay (created lazily on this first call, then reused). The fade runs on
        // unscaled time, so the outgoing scene can stay paused right up to the load:
        // PauseState.ForceResume is handed in as the on-activate step and fires only
        // once the screen is black. Time.timeScale is engine-global and a scene load
        // does NOT reset it, so the clear/game-over screen's pause (timeScale 0) has
        // to be dropped there or the next scene starts frozen. The load itself is
        // async inside the overlay; a real loading screen is still the Phase 5
        // (Addressables) upgrade.
        SceneTransitionController.EnsureExists().FadeAndLoad(sceneName, PauseState.ForceResume);
    }
}
