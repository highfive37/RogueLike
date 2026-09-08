using UnityEngine;
using UnityEngine.SceneManagement;

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
        // Time.timeScale is engine-global and a scene load does NOT reset it. The
        // clear screen pauses via PauseState (timeScale 0), so leaving from a
        // button there without clearing it first would start the next scene
        // frozen. ForceResume drops every outstanding pause owner and restores
        // timeScale before we go. Synchronous load for now - async + a loading
        // screen is the Phase 5 (Addressables) upgrade.
        PauseState.ForceResume();
        SceneManager.LoadScene(sceneName);
    }
}
