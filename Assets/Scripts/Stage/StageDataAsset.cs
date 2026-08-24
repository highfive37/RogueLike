using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "StageData", menuName = "RogueLike/Stages/Stage Data")]
public class StageDataAsset : ScriptableObject
{
    [SerializeField] private bool isFinalStage;
    [SerializeField] private List<StageEnemyEntry> enemies = new List<StageEnemyEntry>();

    public bool IsFinalStage => isFinalStage;
    public IReadOnlyList<StageEnemyEntry> Enemies => enemies;

    public StageData ToStageData(int stageIndex)
    {
        return new StageData(stageIndex, isFinalStage, enemies);
    }
}
