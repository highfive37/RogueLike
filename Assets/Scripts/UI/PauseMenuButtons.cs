using UnityEngine;
using UnityEngine.UI;

// The "Restart Run" / "Return to Title" buttons on the ESC menu. That menu
// (EquipMenuController) already owns the pause; these just route to GameFlow,
// which force-resumes and loads the right scene. Without them the only way out
// of a run was to finish it.
public class PauseMenuButtons : MonoBehaviour
{
    [SerializeField] private Button restartRunButton;
    [SerializeField] private Button returnToTitleButton;

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
}
