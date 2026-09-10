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
// scenes. The one bit of carried run state beyond the archetype now lives in
// RunConfig (seed + floor-source mode).
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

    // The active run's seed + floor-source mode. Set at run start, read by
    // StageManager when it builds the floors. Null when the gameplay scene is
    // entered directly in the editor - EnsureRun() covers that.
    public static RunConfig CurrentRun { get; private set; }

    // Real runs generate their floors procedurally from the seed. Json (the
    // hand-authored FloorN.json set) stays reachable as a debug/reference mode by
    // setting FloorSourceMode.Json on a RunConfig - nothing does that yet.
    private const FloorSourceMode DefaultFloorSource = FloorSourceMode.Procedural;

    // Title "Start" -> an archetype was chosen -> load into gameplay with a fresh
    // random-seeded run, unless the title screen's seed field parsed to an override
    // (replaying a specific seed).
    public static void StartRun(CharacterArchetypeData archetype, int? seedOverride = null)
    {
        PendingArchetype = archetype;
        CurrentRun = seedOverride.HasValue
            ? new RunConfig(seedOverride.Value, DefaultFloorSource)
            : RunConfig.NewRandom(DefaultFloorSource);
        LoadScene(GameplayScene);
    }

    // "다시하기" on the clear screen: straight back into a fresh gameplay scene,
    // reusing the archetype but with a NEW seed (a fresh dungeon, Isaac-style).
    // A "replay this seed" option is later work. The scene reload resets the rest
    // of the run - Player, StageManager, pools, GameFloors' cache.
    public static void RetryRun()
    {
        CurrentRun = RunConfig.NewRandom(DefaultFloorSource);
        LoadScene(GameplayScene);
    }

    // "타이틀로" on the clear screen: back to square one, archetype + run cleared.
    public static void ReturnToTitle()
    {
        PendingArchetype = null;
        CurrentRun = null;
        LoadScene(TitleScene);
    }

    // For entering the gameplay scene directly in the editor (no title): there is
    // no run config, so give it a default random one.
    public static RunConfig EnsureRun()
    {
        return CurrentRun ??= RunConfig.NewRandom(DefaultFloorSource);
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
