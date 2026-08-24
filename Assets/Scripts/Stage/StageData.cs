using System.Collections.Generic;

// Plain data carried between an IStageDataProvider and StageManager/EnemySpawner.
// Engine-agnostic on purpose so future JSON/server-backed providers can build
// one without touching Unity-only types beyond Vector3.
public class StageData
{
    public int StageIndex { get; }
    public bool IsFinalStage { get; }
    public IReadOnlyList<StageEnemyEntry> Enemies { get; }

    public StageData(int stageIndex, bool isFinalStage, IReadOnlyList<StageEnemyEntry> enemies)
    {
        StageIndex = stageIndex;
        IsFinalStage = isFinalStage;
        Enemies = enemies;
    }
}
