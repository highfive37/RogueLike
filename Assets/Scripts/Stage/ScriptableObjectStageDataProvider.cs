public class ScriptableObjectStageDataProvider : IStageDataProvider
{
    private readonly StageDataAsset[] _stages;

    public ScriptableObjectStageDataProvider(StageDataAsset[] stages)
    {
        _stages = stages;
    }

    public int StageCount => _stages.Length;

    public StageData GetStage(int stageIndex)
    {
        return _stages[stageIndex - 1].ToStageData(stageIndex);
    }
}
