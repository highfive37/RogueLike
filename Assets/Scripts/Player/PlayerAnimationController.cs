using UnityEngine;

// Thin bridge between the player's gameplay scripts (PlayerMovement/PlayerDodgeRoll/PlayerCombat/
// RoomFlowManager) and an Animator on the player's visual child - same pattern as
// EnemyAnimationController, just with the player's own state names (Idle/Run/Roll/Attack/Shoot/
// Death) since the player isn't driven by a single FSM the way enemies are.
//
// Idle/Run/Roll/Death play on the base layer (layer 0, full body). Attack/Shoot instead play on
// a second "UpperBody" layer (Override blend, masked to spine+arms via AM_UpperBody.mask) so an
// attack no longer has to fight over the same full-body state as locomotion - the base layer
// keeps driving Idle/Run/Roll on the legs the whole time, and the upper body layer just overlays
// the swing/shot on top, weight 1 only while it plays. This also fixes a real bug the old
// single-layer setup had: attacking while already moving never changed PlayerMovement's own
// isMoving state, so nothing re-triggered PlayRun() once the attack clip ended, leaving the
// Animator stuck on Attack's last frame while the character kept walking. With the layered setup
// there's nothing to "resume" - the base layer never stopped showing Run, so dropping the upper
// layer's weight back to 0 when the clip finishes reveals it automatically.
//
// updateMode is forced to UnscaledTime in Awake: RoomFlowManager zeroes Time.timeScale on death
// (PauseState.RequestPause) before playing the Death clip, and a normal Animator would freeze on
// frame 0 right along with everything else - matching why the old placeholder fall-over routine
// it replaces ran on Time.unscaledDeltaTime.
public class PlayerAnimationController : MonoBehaviour
{
    private const float CrossFadeDuration = 0.1f;
    private const int BaseLayer = 0;
    private const int UpperBodyLayer = 1;

    [SerializeField] private Animator animator;

    private bool _upperBodyActive;
    private int _upperBodyTriggerFrame = -1;

    private void Awake()
    {
        if (animator == null)
        {
            animator = GetComponent<Animator>();
        }

        if (animator != null)
        {
            animator.updateMode = AnimatorUpdateMode.UnscaledTime;
        }
    }

    private void Update()
    {
        if (!_upperBodyActive || animator == null)
        {
            return;
        }

        // Animator.Play()/CrossFadeInFixedTime() don't take effect until the Animator's own next
        // internal evaluation pass - querying GetCurrentAnimatorStateInfo/IsInTransition in the
        // same frame the trigger was issued can still return the PREVIOUS shot's stale info
        // (normalizedTime already >=1, left over from the clip that just finished). If that
        // happens to be the exact frame a new attack was triggered, this completion-check would
        // immediately see "already finished" and drop the weight back to 0 before the Animator
        // ever gets a chance to apply the new pose - the attack fires, but the animation never
        // becomes visible even once. Skipping the check on the trigger's own frame guarantees at
        // least one real Animator evaluation happens first, so this always reads fresh state.
        if (Time.frameCount == _upperBodyTriggerFrame)
        {
            return;
        }

        // Drop the upper body layer back to 0 weight the instant its one-shot clip finishes
        // playing, so the (already-correct) base layer locomotion shows through on its own -
        // no explicit "what should the arms be doing now" bookkeeping needed here.
        if (animator.IsInTransition(UpperBodyLayer))
        {
            return;
        }

        if (animator.GetCurrentAnimatorStateInfo(UpperBodyLayer).normalizedTime >= 1f)
        {
            animator.SetLayerWeight(UpperBodyLayer, 0f);
            _upperBodyActive = false;
        }
    }

    public void PlayIdle() => Play("Idle");
    public void PlayRun() => Play("Run");
    public void PlayRoll() => Play("Roll");
    public void PlayDeath() => Play("Death");
    public void PlayHit() => Play("Hit");
    public void PlayAttack() => PlayUpperBody("Attack");
    public void PlayShoot() => PlayUpperBody("Shoot");

    private void Play(string stateName) => PlayOnLayer(stateName, BaseLayer);

    private void PlayUpperBody(string stateName)
    {
        if (animator == null)
        {
            return;
        }

        animator.SetLayerWeight(UpperBodyLayer, 1f);
        PlayOnLayer(stateName, UpperBodyLayer);
        _upperBodyActive = true;
        _upperBodyTriggerFrame = Time.frameCount;
    }

    // CrossFading into a state that is already the target layer's current state is unreliable -
    // Animator sometimes treats it as "already there" and silently ignores the retrigger instead
    // of restarting the clip (this is how back-to-back Shoot presses landing close together -
    // Gun_Shoot's 0.6s clip against a 0.65s fire cooldown leaves only ~0.05s of margin - could
    // fire the projectile while the arm-raise animation itself never visibly replayed; the same
    // risk applies to Hit re-triggering on a fresh hit while still playing out the last one).
    // Force an explicit restart to frame 0 in that case; otherwise cross-fade in as normal from
    // whatever else the layer was doing (e.g. Attack -> Shoot, or Run -> Hit).
    private void PlayOnLayer(string stateName, int layer)
    {
        if (animator == null)
        {
            return;
        }

        int hash = Animator.StringToHash(stateName);
        bool alreadyInState = !animator.IsInTransition(layer)
            && animator.GetCurrentAnimatorStateInfo(layer).shortNameHash == hash;

        if (alreadyInState)
        {
            animator.Play(hash, layer, 0f);
        }
        else
        {
            animator.CrossFadeInFixedTime(stateName, CrossFadeDuration, layer);
        }
    }
}
