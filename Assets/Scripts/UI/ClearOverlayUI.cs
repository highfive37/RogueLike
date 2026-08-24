using UnityEngine;

// Temporary run-complete screen shown after the final stage is cleared.
// Only covers the screen and locks input; restart/scene-transition handling
// is a separate, later task.
public class ClearOverlayUI : MonoBehaviour
{
    [SerializeField] private GameObject panel;
    [SerializeField] private PlayerInputActions playerInputActions;

    private void Awake()
    {
        panel.SetActive(false);
    }

    public void Show()
    {
        panel.SetActive(true);

        if (playerInputActions != null)
        {
            playerInputActions.enabled = false;
        }
    }
}
