using System;

// Plain JsonUtility DTOs for Assets/StreamingAssets/Balance/PassiveItems.json.
// JsonUtility can't deserialize a top-level JSON array, so the file wraps
// the list in a single "items" field. "type" is a string (e.g.
// "AttackPower") rather than the enum's raw int, purely so the file stays
// readable/hand-editable - GameBalance maps the string to PassiveItemType
// itself at load time.
[Serializable]
public class PassiveItemEntryData
{
    public string type;
    public float amount;
    public int capCount;
}

[Serializable]
public class PassiveItemBalanceData
{
    public PassiveItemEntryData[] items;
}
