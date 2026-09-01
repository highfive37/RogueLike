public readonly struct OwnedActiveItem
{
    public readonly ActiveItemData Data;
    public readonly int Level;

    public OwnedActiveItem(ActiveItemData data, int level)
    {
        Data = data;
        Level = level;
    }
}
