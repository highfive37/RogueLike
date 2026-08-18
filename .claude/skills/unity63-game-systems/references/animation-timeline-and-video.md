# Animation, Playables, Timeline, and Video

## Scope

Connects Animator state machines, clips, parameters, root motion, Playables, Timeline 1.8, animation rigging, and VideoPlayer to clear authority and update flow.

Read this reference when the request involves:

- implementing Animator controllers, clips, transitions, root motion, or IK
- using Timeline 1.8, Playables, cutscenes, or VideoPlayer
- coordinating gameplay state with animation and cinematic sequencing

## Contents

- [Core concepts](#core-concepts)
- [Verified Unity 6.3 facts](#verified-unity-63-facts)
- [Official-fact-based design and implementation interpretation](#official-fact-based-design-and-implementation-interpretation)
- [Selection criteria](#selection-criteria)
- [Risks and review checks](#risks-and-review-checks)
- [Related official API](#related-official-api)
- [Official sources](#official-sources)

## Core concepts

- **Animation system**
- **Animator Controller**
- **Playables**
- **Timeline**
- **Video**
- **Animator**
- **PlayableDirector**
- **VideoPlayer**
- **Animation Rigging**

## Verified Unity 6.3 facts

- Unity’s animation system uses AnimationClips, Animator components, RuntimeAnimatorControllers, state machines, parameters, layers, transitions, and optional Avatar/retargeting features. ([official source](https://docs.unity3d.com/6000.3/Documentation/Manual/AnimationSection.html))
- Animator evaluation timing, update mode, culling, root motion, and controller state affect how animation interacts with gameplay and physics. ([official source](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Animator.html))
- Unity 6.3 adds `Animator.ResetControllerState`, documented in the 6.3 changes, to reset the controller state. ([official source](https://docs.unity3d.com/6000.3/Documentation/Manual/WhatsNewUnity63.html))
- Playables provide a graph-based runtime API used by animation, audio, and Timeline-related systems. ([official source](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Playables.PlayableGraph.html))
- Timeline 1.8 is a released package for Unity 6.3 and sequences tracks/clips through a PlayableDirector and TimelineAsset. ([official source](https://docs.unity3d.com/6000.3/Documentation/Manual/pack-safe.html))
- Animation Rigging 1.4 is a released Unity 6.3 package for rig constraints layered with animation. ([official source](https://docs.unity3d.com/6000.3/Documentation/Manual/pack-safe.html))
- VideoPlayer plays video from a VideoClip or URL and exposes preparation, playback, time, frame, audio-output, and render-target controls. ([official source](https://docs.unity3d.com/6000.3/Documentation/Manual/Video.html))
- PlayableDirector controls a PlayableAsset with time, extrapolation, update, binding, play, pause, stop, and evaluation APIs. ([official source](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Playables.PlayableDirector.html))
- Animation events invoke methods at clip times and create name/signature coupling that must be maintained during refactors. ([official source](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/AnimationEvent.html))

## Official-fact-based design and implementation interpretation

The statements in this section are engineering interpretations derived from the cited official behavior; they are not presented as additional Unity guarantees.

- **Interpretation:** Choose one authority per property: gameplay, Animator root motion, physics, Timeline, and procedural rigging must not simultaneously write the same transform without a defined blend/order.
- **Interpretation:** Translate Animator parameters into a small animation-facing adapter; domain state should not be spread across string parameter writes from many systems.
- **Interpretation:** Precompute parameter hashes and centralize names to reduce typo/refactor risk, then test transitions and interruption rules in representative controller states.
- **Interpretation:** Treat Timeline as an authored sequence controller and expose explicit signals/commands for gameplay handoff instead of polling arbitrary director time.
- **Interpretation:** Prepare video asynchronously and design buffering/error/skip behavior; a prepared clip is a state transition, not merely a `Play` call.
- **Interpretation:** Use animation rigging as a post-animation constraint layer with documented targets/weights; isolate package types if optional.

## Selection criteria

| Situation | Prefer | Reason / boundary |
|---|---|---|
| Stateful character animation | Animator Controller + adapter | Transitions/layers and parameter state are appropriate. |
| Procedural runtime blend graph | Playables | Explicit graph and evaluation control. |
| Authored cinematic sequence | Timeline 1.8 + PlayableDirector | Track/clip authoring and bindings are primary. |
| Video surface/cutscene | VideoPlayer | Explicit prepare, output, audio, and error states. |

## Risks and review checks

- Do not let gameplay and root motion both own translation without a reconciliation policy.
- Do not use Animator string parameters from many unrelated classes.
- Do not assume a transition completes before a state interruption or controller change.
- Do not use AnimationEvents for critical domain authority without validation and fallback; they are authored clip callbacks.
- Do not assume Timeline bindings remain valid after prefab/scene restructuring without tests.
- Do not block the main thread while waiting for remote video preparation.

## Related official API

| API | Signature or surface | Scope | Use and constraints |
|---|---|---|---|
| [`UnityEngine.Animator.SetTrigger`](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Animator.SetTrigger.html) | `public void SetTrigger(int id); public void SetTrigger(string name)` | Runtime | Sets a trigger parameter; integer hash avoids repeated name lookup. |
| [`UnityEngine.Animator.StringToHash`](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Animator.StringToHash.html) | `public static int StringToHash(string name)` | Runtime | Converts a parameter/state name to an integer hash. |
| [`UnityEngine.Animator.ResetControllerState`](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Animator.ResetControllerState.html) | `public void ResetControllerState()` | Runtime | Resets controller state in Unity 6.3. |
| [`UnityEngine.Playables.PlayableDirector.Play`](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Playables.PlayableDirector.Play.html) | `public void Play()` | Runtime | Starts the bound PlayableAsset. |
| [`UnityEngine.Playables.PlayableDirector.Evaluate`](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Playables.PlayableDirector.Evaluate.html) | `public void Evaluate()` | Runtime | Evaluates the graph at the current time immediately. |
| [`UnityEngine.Video.VideoPlayer.Prepare`](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Video.VideoPlayer.Prepare.html) | `public void Prepare()` | Runtime | Begins preparation; observe prepareCompleted or isPrepared. |
| [`UnityEngine.Video.VideoPlayer.errorReceived`](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Video.VideoPlayer-errorReceived.html) | `public event EventHandler errorReceived` | Runtime | Reports playback/preparation errors. |

### Package prerequisites

- `com.unity.timeline@1.8` — documented as a Unity 6.3 released package. [Package documentation](https://docs.unity3d.com/Packages/com.unity.timeline@1.8/manual/index.html); [Unity 6.3 compatibility evidence](https://docs.unity3d.com/6000.3/Documentation/Manual/pack-safe.html).
- `com.unity.animation.rigging@1.4` — documented as a Unity 6.3 released package. [Package documentation](https://docs.unity3d.com/Packages/com.unity.animation.rigging@1.4/manual/index.html); [Unity 6.3 compatibility evidence](https://docs.unity3d.com/6000.3/Documentation/Manual/pack-safe.html).
- Do not treat these package APIs as installed in every project. Confirm the package in `Packages/manifest.json` before using its types.

## Official sources

- Animation Section — https://docs.unity3d.com/6000.3/Documentation/Manual/AnimationSection.html
- Video — https://docs.unity3d.com/6000.3/Documentation/Manual/Video.html
- Whats New Unity63 — https://docs.unity3d.com/6000.3/Documentation/Manual/WhatsNewUnity63.html
- Pack Safe — https://docs.unity3d.com/6000.3/Documentation/Manual/pack-safe.html
- Animator — https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Animator.html
- Animator.Set Trigger — https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Animator.SetTrigger.html
- Animator.String To Hash — https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Animator.StringToHash.html
- Animator.Reset Controller State — https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Animator.ResetControllerState.html
- Animation Event — https://docs.unity3d.com/6000.3/Documentation/ScriptReference/AnimationEvent.html
- Playables.Playable Graph — https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Playables.PlayableGraph.html
- Playables.Playable Director — https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Playables.PlayableDirector.html
- Playables.Playable Director.Play — https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Playables.PlayableDirector.Play.html
- Playables.Playable Director.Evaluate — https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Playables.PlayableDirector.Evaluate.html
- Video.Video Player — https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Video.VideoPlayer.html
- Video.Video Player.Prepare — https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Video.VideoPlayer.Prepare.html
- Video.Video Player Error Received — https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Video.VideoPlayer-errorReceived.html
- Com.Unity.Timeline@1.8 — https://docs.unity3d.com/Packages/com.unity.timeline@1.8/manual/index.html
- Com.Unity.Animation.Rigging@1.4 — https://docs.unity3d.com/Packages/com.unity.animation.rigging@1.4/manual/index.html

## Evidence classification

- Sections labeled **Verified Unity 6.3 facts** and **Related official API** are target-version official-document facts.
- Sections labeled **Interpretation** are design or implementation judgments derived from those facts.
- Behavior not supported by the listed sources is outside this reference and must be verified before use.
