// Abstraction over where stage data comes from. Today: StageDataAsset
// (ScriptableObject) via ScriptableObjectStageDataProvider. Later: an
// external file or a server response, behind the same interface, so
// StageManager never has to change.
public interface IStageDataProvider
{
    int StageCount { get; }
    StageData GetStage(int stageIndex);
}
