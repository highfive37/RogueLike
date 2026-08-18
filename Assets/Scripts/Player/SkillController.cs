using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(PlayerInputActions))]
public class SkillController : MonoBehaviour
{
    [SerializeField] private SkillItemData defaultSkill;
    [SerializeField] private GameObjectPool projectilePool;
    [SerializeField] private GameObjectPool grenadePool;

    private InputAction _skillAction;
    private SkillContext _context;
    private ISkillItem _equippedSkill;

    public float CooldownProgress => _equippedSkill?.CooldownProgress ?? 0f;
    public float CooldownRemaining => _equippedSkill?.CooldownRemaining ?? 0f;

    private void Awake()
    {
        _skillAction = GetComponent<PlayerInputActions>().FindAction("Skill");
        _context = new SkillContext(projectilePool, grenadePool);

        if (defaultSkill != null)
        {
            Equip(defaultSkill, 1);
        }
    }

    private void OnEnable()
    {
        _skillAction.performed += OnSkillPressed;
        _skillAction.canceled += OnSkillReleased;
    }

    private void OnDisable()
    {
        _skillAction.performed -= OnSkillPressed;
        _skillAction.canceled -= OnSkillReleased;
    }

    private void Update()
    {
        _equippedSkill?.Tick(Time.deltaTime);
    }

    public void Equip(SkillItemData data, int level)
    {
        _equippedSkill = data.CreateSkill(transform, _context, level);
    }

    private void OnSkillPressed(InputAction.CallbackContext context)
    {
        _equippedSkill?.OnActivatePressed();
    }

    private void OnSkillReleased(InputAction.CallbackContext context)
    {
        _equippedSkill?.OnActivateReleased();
    }
}
