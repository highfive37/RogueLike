using UnityEngine;

// Common base for anything a floor pickup (ItemPickup) can hand to the
// player - both ActiveItemData (equip-and-activate) and PassiveItemData
// (permanent stat bonus, applied immediately on pickup) derive from this so
// ItemPickup/ItemDropManager don't need to know which kind they're holding.
public abstract class ItemData : ScriptableObject
{
    [SerializeField] private string skillName;

    // Per-instance pickup tint (ItemPickup applies this to visualRenderer.material.color)
    // so every item - active or passive - reads as visually distinct on the floor.
    [SerializeField] private Color color = Color.white;

    public string ItemName => skillName;
    public Color Color => color;

    public abstract void OnAcquired(GameObject player);
}
