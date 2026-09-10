using UnityEngine;
using UnityEngine.UI;

// The "Restart Run" / "Return to Title" buttons, reused on every screen that offers
// them (the ESC pause menu, GameOverUI's ButtonsGroup, FloorCompleteUI's
// ClearOverlayPanel) instead of each screen wiring its own click handlers. That menu
// (EquipMenuController) already owns the pause; these just route to GameFlow, which
// force-resumes and loads the right scene. Without them the only way out of a run
// was to finish it.
//
// seedText is optional (null on any instance that doesn't have one wired) - it
// piggybacks on this component only because this is the one script every one of
// those screens already carries, not because seed display is conceptually part of
// "pause buttons". The run's seed never changes after StartRun, so setting it once
// is enough - but that has to happen in Start(), not Awake(). GameFlow.CurrentRun is
// only guaranteed populated once StageManager.Awake() (a different root GameObject)
// has run EnsureRun(); Unity does not guarantee Awake() order between separate root
// GameObjects, only that every Awake() in the scene finishes before any Start().
// Reading CurrentRun in this component's own Awake() raced that and intermittently
// read null (entering the gameplay scene without going through Title), permanently
// latching "SEED: ?" since this only runs once.
public class PauseMenuButtons : MonoBehaviour
{
    [SerializeField] private Button restartRunButton;
    [SerializeField] private Button returnToTitleButton;
    [SerializeField] private Text seedText;

    private void Awake()
    {
        if (restartRunButton != null)
        {
            restartRunButton.onClick.AddListener(GameFlow.RetryRun);
        }

        if (returnToTitleButton != null)
        {
            returnToTitleButton.onClick.AddListener(GameFlow.ReturnToTitle);
        }
    }

    private void Start()
    {
        if (seedText != null)
        {
            seedText.text = GameFlow.CurrentRun != null ? $"SEED: {GameFlow.CurrentRun.DisplaySeed}" : "SEED: ?";
        }
    }
}
