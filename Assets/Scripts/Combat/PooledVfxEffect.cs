using UnityEngine;

// Self-managed lifetime for a pooled one-shot VFX burst - returns itself to the pool once its
// particles are actually done, rather than Destroy(), so CombatVfxLibrary's effect pools reuse
// instances instead of allocating/freeing a GameObject on every hit/shot/explosion (2026-09-15,
// user request).
//
// Drives every child ParticleSystem explicitly via GetComponentsInChildren rather than relying
// on the root having one, or on each child's own authored Play On Awake setting - a pooled
// instance's repeated SetActive(true)/(false) cycling can't be trusted to replay every child
// correctly on its own. Contrast with FlamethrowerActiveItem's persistent always-attached
// effect, which explicitly disables Play On Awake instead since it Play()/Stop()s the same
// instance rather than pooling short bursts.
//
// Completion is driven by each system's actual live particleCount every frame, instead of a
// precomputed duration+startLifetime timer (the original approach here) or ParticleSystem.
// IsAlive() (tried next, also wrong - see below). Both alternatives read a system's authored
// "Duration" field as if it meant "how long until the burst is visually done," but Duration is
// really just the module's playback/loop window and is frequently set to a generous, unrelated
// value (seen as long as 5s on a one-shot burst that actually finishes fading in under a
// second) by whoever authored the source pack. That mismatch caused two real bugs: MuzzleFlash.
// prefab's root system had `loop = true`, so mid-way through the precomputed window it
// restarted its burst before the timer had elapsed, reading as "the flash plays twice"; and
// FleshImpacts/vfx_Impact_01's long Duration values inflated the window well past when the
// burst had actually finished fading, reading as "the hit effect lingers too long." Switching
// the timer to IsAlive() does NOT fix the second bug - IsAlive() is itself gated by Duration for
// a non-looping system (confirmed live: a burst-only system with Duration=5 still reports
// IsAlive()==true 2s after its particles were long gone, because Unity treats "still within the
// playback window" as alive independent of whether anything is left to render). particleCount
// has no such gating: it only reflects what's actually still on screen, so it can't be thrown
// off by an oversized or looping Duration regardless of what the source pack authored.
[RequireComponent(typeof(PooledObject))]
public class PooledVfxEffect : MonoBehaviour
{
    private ParticleSystem[] _particleSystems;
    private PooledObject _pooledObject;

    // True for exactly the first Update() after OnEnable() - skips the completion check that
    // one frame so a freshly Play()'d burst (whose particles the engine hasn't simulated into
    // existence yet) can't read as "already empty" and bounce straight back to the pool.
    private bool _justEnabled;

    private void Awake()
    {
        _pooledObject = GetComponent<PooledObject>();
        _particleSystems = GetComponentsInChildren<ParticleSystem>(true);

        // A one-shot pooled VFX must never loop - a looping system keeps re-triggering its
        // burst pattern (see MuzzleFlash's "flashes twice" bug above) instead of finishing once.
        // Force it off regardless of how the source prefab authored it.
        foreach (ParticleSystem ps in _particleSystems)
        {
            ParticleSystem.MainModule main = ps.main;
            if (main.loop)
            {
                main.loop = false;
            }
        }
    }

    // Fires every time GameObjectPool.Get() reactivates this instance, not just on first spawn.
    private void OnEnable()
    {
        _justEnabled = true;

        foreach (ParticleSystem ps in _particleSystems)
        {
            ps.Play();
        }
    }

    private void Update()
    {
        if (_justEnabled)
        {
            _justEnabled = false;
            return;
        }

        foreach (ParticleSystem ps in _particleSystems)
        {
            if (ps.particleCount > 0)
            {
                return;
            }
        }

        _pooledObject.ReturnToPool();
    }

    // Clean slate before this instance sits in the pool - stop and clear rather than leaving
    // particles frozen mid-fade for whatever visual state it happened to be released in.
    private void OnDisable()
    {
        foreach (ParticleSystem ps in _particleSystems)
        {
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }
    }
}
