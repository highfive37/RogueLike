using UnityEngine;

// Trigger hitbox for ChargeStrikePattern's dash - a plain C# pattern class can't receive
// OnTriggerEnter itself, so this small companion component lives as an (authored-inactive) child
// of the boss, and the pattern arms/disarms it around the dash window instead of the old "check
// distance once when a post-dash pause ends" approach (replaced outright, not layered alongside
// it - see DevLog 2026-09-15 for why running both at once would have let damage and knockback
// resolve at different moments). One-shot per arm - disarms itself the instant it lands a hit, so
// overlapping the player for multiple physics steps in a row can't multi-hit.
[RequireComponent(typeof(BoxCollider))]
public class EnemyChargeHitbox : MonoBehaviour
{
    private static Material _debugBoxMaterial;

    private BoxCollider _collider;
    private Transform _owner;
    private float _damage;
    private float _knockbackMultiplier;
    private bool _hasHit;

    // Lazily created only while armed with the F1 debug overlay on (same
    // DebugOverlayController.IsDebugVisible flag FlamethrowerActiveItem's range outline uses),
    // torn down the instant either condition stops holding - so this hitbox's actual judgment
    // volume can be screenshotted for README purposes instead of guessed at from code.
    private GameObject _debugVisual;

    private void Awake()
    {
        _collider = GetComponent<BoxCollider>();
        _collider.isTrigger = true;
    }

    public void Arm(Transform owner, float damage, float knockbackMultiplier = 1f)
    {
        _owner = owner;
        _damage = damage;
        _knockbackMultiplier = knockbackMultiplier;
        _hasHit = false;
        gameObject.SetActive(true);
    }

    public void Disarm()
    {
        gameObject.SetActive(false);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (_hasHit || !other.CompareTag("Player"))
        {
            return;
        }

        if (!other.TryGetComponent<IDamageable>(out var damageable))
        {
            return;
        }

        _hasHit = true;
        damageable.TakeDamage(_damage, _owner, _owner.forward * _knockbackMultiplier);
        Disarm();
    }

    // Only runs while armed (SetActive(true) between Arm/Disarm) - checked every frame so the
    // box appears/disappears immediately if the overlay is toggled mid-dash.
    private void Update()
    {
        UpdateDebugVisual();
    }

    private void OnDisable()
    {
        DestroyDebugVisual();
    }

    private void UpdateDebugVisual()
    {
        if (!DebugOverlayController.IsDebugVisible)
        {
            DestroyDebugVisual();
            return;
        }

        if (_debugVisual == null)
        {
            _debugVisual = GameObject.CreatePrimitive(PrimitiveType.Cube);
            _debugVisual.name = "ChargeHitboxDebug";
            Destroy(_debugVisual.GetComponent<Collider>()); // visual only, must not itself collide
            _debugVisual.GetComponent<MeshRenderer>().sharedMaterial = GetDebugBoxMaterial();
        }

        // Matches this object's own BoxCollider bounds exactly (center/size in local space,
        // transformed by this transform's full position/rotation/scale) rather than the owner's -
        // this hitbox already sits wherever ChargeStrikePattern positions/orients it on the boss.
        Transform box = _debugVisual.transform;
        box.SetPositionAndRotation(transform.TransformPoint(_collider.center), transform.rotation);
        box.localScale = Vector3.Scale(_collider.size, transform.lossyScale);
    }

    private void DestroyDebugVisual()
    {
        if (_debugVisual == null)
        {
            return;
        }

        Destroy(_debugVisual);
        _debugVisual = null;
    }

    private static Material GetDebugBoxMaterial()
    {
        if (_debugBoxMaterial == null)
        {
            // Sprites/Default alpha-blends its vertex color out of the box - same shader
            // FlamethrowerActiveItem's debug outline uses, just translucent here instead of an
            // opaque line so the box reads as a volume, not just an edge.
            _debugBoxMaterial = new Material(Shader.Find("Sprites/Default"));
            _debugBoxMaterial.color = new Color(1f, 0f, 0f, 0.35f);
        }

        return _debugBoxMaterial;
    }
}
