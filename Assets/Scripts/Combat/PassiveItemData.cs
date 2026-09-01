using UnityEngine;

// A permanent stat bonus applied immediately on pickup - unlike active
// items, passive items are never "equipped" and are never removed from
// ItemDropManager's pool, so the same passive can drop and stack repeatedly.
[CreateAssetMenu(fileName = "PassiveItemData", menuName = "RogueLike/Passive Items/Passive Item")]
public class PassiveItemData : ItemData
{
    [SerializeField] private PassiveItemType type;
    [SerializeField] private Color color = Color.white;

    public PassiveItemType Type => type;
    public Color Color => color;

    // Amount/CapCount are hand-tunable balance numbers pulled from
    // GameBalance (Assets/StreamingAssets/Balance/PassiveItems.json)
    // instead of serialized fields, keyed by Type.
    //
    // PlayerStatsPanelUI computes the pip bar as (base + bonus) /
    // (base + CapCount * Amount), so the bar already starts partway full
    // from the base stat alone before any pickups.
    public float Amount => GameBalance.ResolvePassiveAmount(type);
    public int CapCount => GameBalance.ResolvePassiveCapCount(type);

    public override void OnAcquired(GameObject player)
    {
        if (player.TryGetComponent(out PlayerPassiveStats stats))
        {
            stats.Apply(this);
        }
    }
}
