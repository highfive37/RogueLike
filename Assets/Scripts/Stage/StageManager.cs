using System;
using UnityEngine;

// Tracks stage progression using data supplied by an IStageDataProvider.
// The concrete provider is ScriptableObject-backed today; a file- or
// server-backed provider can replace it later without this class changing.
// Enemy counts are never computed here from the stage index - they come
// entirely from whatever the provider returns for that stage.
public class StageManager : MonoBehaviour
{
    [SerializeField] private StageDataAsset[] stageAssets;

    private IStageDataProvider _provider;
    private int _currentStageIndex;

    public event Action<int> StageChanged;

    public bool IsCurrentStageFinal { get; private set; }

    private void Awake()
    {
        _provider = new ScriptableObjectStageDataProvider(stageAssets);
    }

    public StageData BeginFirstStage()
    {
        _currentStageIndex = 1;
        return LoadCurrentStage();
    }

    public bool TryAdvance(out StageData nextStage)
    {
        if (IsCurrentStageFinal || _currentStageIndex >= _provider.StageCount)
        {
            nextStage = null;
            return false;
        }

        _currentStageIndex++;
        nextStage = LoadCurrentStage();
        return true;
    }

    private StageData LoadCurrentStage()
    {
        StageData stage = _provider.GetStage(_currentStageIndex);
        IsCurrentStageFinal = stage.IsFinalStage;
        StageChanged?.Invoke(stage.StageIndex);
        return stage;
    }
}
