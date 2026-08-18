# Audio Sources, Mixers, Spatialization, and Scriptable Audio

## Scope

Covers clips, sources, listeners, mixers, snapshots, spatial audio, scheduling, import/load choices, profiling, and Unity 6.3’s Scriptable Audio Pipeline addition.

Read this reference when the request involves:

- playing music, sound effects, dialogue, or spatial audio
- designing AudioMixer routing, snapshots, scheduling, or voice limits
- evaluating the Unity 6.3 Scriptable Audio Pipeline

## Contents

- [Core concepts](#core-concepts)
- [Verified Unity 6.3 facts](#verified-unity-63-facts)
- [Official-fact-based design and implementation interpretation](#official-fact-based-design-and-implementation-interpretation)
- [Selection criteria](#selection-criteria)
- [Risks and review checks](#risks-and-review-checks)
- [Related official API](#related-official-api)
- [Official sources](#official-sources)

## Core concepts

- **Audio system**
- **AudioMixer**
- **3D spatial sound**
- **audio scheduling**
- **Scriptable Audio Pipeline**
- **AudioSource**
- **AudioClip**
- **AudioListener**
- **AudioMixer**

## Verified Unity 6.3 facts

- Unity audio includes AudioClips, AudioSources, AudioListeners, 3D spatial sound, real-time mixing/mastering, mixer groups, snapshots, effects, and profiling. ([official source](https://docs.unity3d.com/6000.3/Documentation/Manual/Audio.html))
- AudioSource controls clip playback, looping, volume/pitch, mixer routing, priority, spatial blend, distance attenuation, doppler, and related playback settings. ([official source](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/AudioSource.html))
- An active AudioListener receives the scene audio mix; normal applications should have one effective listener for output. ([official source](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/AudioListener.html))
- AudioMixer assets define group routing, exposed parameters, effects, snapshots, and transitions. ([official source](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Audio.AudioMixer.html))
- PlayScheduled uses DSP time for sample-accurate scheduling compared with frame-time triggering. ([official source](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/AudioSource.PlayScheduled.html))
- AudioClip import/load type, compression, preload, streaming, channels, and sample rate affect memory, CPU, latency, and quality. ([official source](https://docs.unity3d.com/6000.3/Documentation/Manual/Audio.html))
- Unity 6.3 introduces the Scriptable Audio Pipeline as a version-specific audio capability documented in the release changes. ([official source](https://docs.unity3d.com/6000.3/Documentation/Manual/WhatsNewUnity63.html))
- The Audio Profiler module exposes voices, CPU, memory, and playback behavior for diagnosis. ([official source](https://docs.unity3d.com/6000.3/Documentation/Manual/Audio.html))

## Official-fact-based design and implementation interpretation

The statements in this section are engineering interpretations derived from the cited official behavior; they are not presented as additional Unity guarantees.

- **Interpretation:** Route categories through mixer groups and control user-facing levels at the mixer, rather than multiplying unrelated AudioSource volumes from many systems.
- **Interpretation:** Own playback through a voice/service layer that applies priority, concurrency, pooling, fade, and cancellation policy while gameplay requests semantic cues.
- **Interpretation:** Use DSP scheduling for music stems, loops, and tightly synchronized cues; frame callbacks are not sample-accurate.
- **Interpretation:** Choose import/load settings per asset role: short repeated SFX, long music, dialogue, and ambient streams have different memory/CPU/latency tradeoffs.
- **Interpretation:** Make spatialization an authored contract with listener scale, minimum/maximum distance, spread, and attenuation tested in the target world.
- **Interpretation:** Treat the new Scriptable Audio Pipeline as version-specific and verify its exact maturity, API, platform, and performance constraints before production adoption.

## Selection criteria

| Situation | Prefer | Reason / boundary |
|---|---|---|
| Short latency-sensitive SFX | Preloaded/decompressed or suitable compressed clip after profiling | Optimize for latency and reuse within memory budget. |
| Long music/dialogue | Streaming or compressed-in-memory according to target tests | Balance memory, decode CPU, and seek latency. |
| Music synchronization | DSP-time scheduling | Avoid frame-jittered starts. |
| Mix state change | AudioMixer snapshot/parameter transition | Centralizes routing and effect state. |

## Risks and review checks

- Do not create an unbounded AudioSource for every play request.
- Do not place multiple active listeners unintentionally.
- Do not call Play every frame or restart a clip without an explicit retrigger policy.
- Do not use frame time for beat-perfect synchronization when DSP scheduling is required.
- Do not apply one import setting to all clips without measuring platform memory/CPU and latency.
- Do not treat a newly introduced 6.3 audio feature as universally production-ready without its exact official constraints.

## Related official API

| API | Signature or surface | Scope | Use and constraints |
|---|---|---|---|
| [`UnityEngine.AudioSource.PlayOneShot`](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/AudioSource.PlayOneShot.html) | `public void PlayOneShot(AudioClip clip, float volumeScale = 1.0F)` | Runtime | Plays a clip without replacing the AudioSource.clip property. |
| [`UnityEngine.AudioSource.PlayScheduled`](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/AudioSource.PlayScheduled.html) | `public void PlayScheduled(double time)` | Runtime | Schedules playback at AudioSettings.dspTime. |
| [`UnityEngine.AudioSettings.dspTime`](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/AudioSettings-dspTime.html) | `public static double dspTime { get; }` | Runtime | Audio system time based on processed samples. |
| [`UnityEngine.Audio.AudioMixer.SetFloat`](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Audio.AudioMixer.SetFloat.html) | `public bool SetFloat(string name, float value)` | Runtime | Sets an exposed mixer parameter when editable under documented snapshot constraints. |
| [`UnityEngine.Audio.AudioMixer.TransitionToSnapshots`](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Audio.AudioMixer.TransitionToSnapshots.html) | `public void TransitionToSnapshots(AudioMixerSnapshot[] snapshots, float[] weights, float timeToReach)` | Runtime | Blends multiple snapshots over time. |
| [`UnityEngine.AudioClip.LoadAudioData`](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/AudioClip.LoadAudioData.html) | `public bool LoadAudioData()` | Runtime | Requests clip sample data loading according to clip settings. |

## Official sources

- Audio — https://docs.unity3d.com/6000.3/Documentation/Manual/Audio.html
- Whats New Unity63 — https://docs.unity3d.com/6000.3/Documentation/Manual/WhatsNewUnity63.html
- Audio Source — https://docs.unity3d.com/6000.3/Documentation/ScriptReference/AudioSource.html
- Audio Listener — https://docs.unity3d.com/6000.3/Documentation/ScriptReference/AudioListener.html
- Audio.Audio Mixer — https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Audio.AudioMixer.html
- Audio Source.Play One Shot — https://docs.unity3d.com/6000.3/Documentation/ScriptReference/AudioSource.PlayOneShot.html
- Audio Source.Play Scheduled — https://docs.unity3d.com/6000.3/Documentation/ScriptReference/AudioSource.PlayScheduled.html
- Audio Settings Dsp Time — https://docs.unity3d.com/6000.3/Documentation/ScriptReference/AudioSettings-dspTime.html
- Audio.Audio Mixer.Set Float — https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Audio.AudioMixer.SetFloat.html
- Audio.Audio Mixer.Transition To Snapshots — https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Audio.AudioMixer.TransitionToSnapshots.html
- Audio Clip.Load Audio Data — https://docs.unity3d.com/6000.3/Documentation/ScriptReference/AudioClip.LoadAudioData.html

## Evidence classification

- Sections labeled **Verified Unity 6.3 facts** and **Related official API** are target-version official-document facts.
- Sections labeled **Interpretation** are design or implementation judgments derived from those facts.
- Behavior not supported by the listed sources is outside this reference and must be verified before use.
