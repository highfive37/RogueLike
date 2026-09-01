using UnityEngine;

[CreateAssetMenu(fileName = "FlamethrowerActiveItemData", menuName = "RogueLike/Active Items/Flamethrower")]
public class FlamethrowerActiveItemData : ActiveItemData
{
    [SerializeField] private float damagePerSecond = 3f;
    [SerializeField] private float maxDuration = 7f;
    [SerializeField] private float zoneLength = 3f;
    [SerializeField] private float zoneWidth = 1.5f;

    public float DamagePerSecond => damagePerSecond;
    public float MaxDuration => maxDuration;
    public float ZoneLength => zoneLength;
    public float ZoneWidth => zoneWidth;

    public override IActiveItem CreateActiveItem(Transform owner, ActiveItemContext context, int level)
    {
        return new FlamethrowerActiveItem(owner, this, level);
    }
}
