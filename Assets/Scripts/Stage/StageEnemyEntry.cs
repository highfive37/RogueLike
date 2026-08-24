using UnityEngine;

[System.Serializable]
public class StageEnemyEntry
{
    [SerializeField] private EnemyVariantType variant;
    [SerializeField] private Vector3 spawnPosition;

    public EnemyVariantType Variant => variant;
    public Vector3 SpawnPosition => spawnPosition;

    public StageEnemyEntry(EnemyVariantType variant, Vector3 spawnPosition)
    {
        this.variant = variant;
        this.spawnPosition = spawnPosition;
    }
}
