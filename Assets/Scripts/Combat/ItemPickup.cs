using UnityEngine;

// A physical pickup for an item dropped on the floor - either dropped by an
// enemy on death (ItemDropManager) or authored directly in a future item
// room. The player walks into its trigger collider to acquire it; what
// "acquire" means is delegated to the item itself (ItemData.OnAcquired) so
// this stays agnostic to active vs passive items. Passive item pickups are
// tinted per-instance via PassiveItemData.Color; active item pickups ignore
// visualRenderer entirely (their prefab's own material is used as-is).
public class ItemPickup : MonoBehaviour
{
    [SerializeField] private Renderer visualRenderer;

    private ItemData _item;

    public void SetItem(ItemData item)
    {
        _item = item;

        if (item is PassiveItemData passive && visualRenderer != null)
        {
            visualRenderer.material.color = passive.Color;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player") || _item == null)
        {
            return;
        }

        _item.OnAcquired(other.gameObject);

        PickupNotificationUI notification = other.GetComponentInChildren<PickupNotificationUI>(true);
        if (notification != null)
        {
            notification.Show(_item.ItemName);
        }

        Destroy(gameObject);
    }
}
