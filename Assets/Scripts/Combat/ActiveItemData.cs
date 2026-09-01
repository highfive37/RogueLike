using UnityEngine;

public abstract class ActiveItemData : ItemData
{
    public abstract IActiveItem CreateActiveItem(Transform owner, ActiveItemContext context, int level);

    public override void OnAcquired(GameObject player)
    {
        if (player.TryGetComponent(out PlayerActiveItemInventory inventory))
        {
            inventory.Acquire(this);
        }
    }
}
