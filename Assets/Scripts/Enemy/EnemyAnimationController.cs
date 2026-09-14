using UnityEngine;

// Thin bridge between EnemyStateMachine's FSM and an Animator on this enemy's visual child.
// Knows only state NAMES that must exist in whatever AnimatorController is assigned (Idle/Walk/
// Run/Attack/Shoot) - each enemy model's own controller supplies the actual clip per name, so
// this same component works unmodified for every enemy visual (2Legs/2Legs_Gun/Flying/
// Flying_Gun/Large_Gun). CrossFadeInFixedTime (not Play) so switching states - e.g. Chase's Run
// interrupting a Patrol Walk mid-stride - blends instead of popping.
//
// Optional on an enemy: EnemyStateMachine looks this up via GetComponentInChildren and no-ops if
// absent (e.g. a bare test double with no visual wired), so nothing here needs to defend against
// running standalone.
public class EnemyAnimationController : MonoBehaviour
{
    private const float CrossFadeDuration = 0.1f;

    [SerializeField] private Animator animator;

    private void Awake()
    {
        if (animator == null)
        {
            animator = GetComponent<Animator>();
        }
    }

    public void PlayIdle() => Play("Idle");
    public void PlayWalk() => Play("Walk");
    public void PlayRun() => Play("Run");
    public void PlayAttack() => Play("Attack");
    public void PlayShoot() => Play("Shoot");

    // "Death" is the canonical state name across every enemy AnimatorController regardless of
    // whether the source FBX clip itself is named "...Death" or "...Dead" - the build step that
    // creates these controllers normalizes both to a state named "Death".
    public void PlayDeath() => Play("Death");

    public void Play(EnemyAnimationCue cue)
    {
        switch (cue)
        {
            case EnemyAnimationCue.Attack:
                PlayAttack();
                break;
            case EnemyAnimationCue.Shoot:
                PlayShoot();
                break;
        }
    }

    private void Play(string stateName)
    {
        if (animator == null)
        {
            return;
        }

        animator.CrossFadeInFixedTime(stateName, CrossFadeDuration);
    }
}
