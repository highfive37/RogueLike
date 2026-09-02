using System;

// Plain JsonUtility DTOs for Assets/StreamingAssets/Balance/ItemDropTable.json.
// Same conventions as PassiveItemBalanceData: JsonUtility can't deserialize a
// top-level array, so weight lists are wrapped in a named field, and active
// items are keyed by their human-readable ItemName (they have no enum like
// PassiveItemType) while passive items are keyed by the PassiveItemType name.
[Serializable]
public class ActiveDropWeightEntry
{
    public string name;
    public float weight;
}

[Serializable]
public class PassiveDropWeightEntry
{
    public string type;
    public float weight;
}

[Serializable]
public class ItemDropTableData
{
    public float dropChance;
    public float activeCategoryWeight;
    public float passiveCategoryWeight;
    public ActiveDropWeightEntry[] activeWeights;
    public PassiveDropWeightEntry[] passiveWeights;
}
