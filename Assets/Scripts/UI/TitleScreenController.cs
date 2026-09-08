using UnityEngine;
using UnityEngine.UI;

// Drives the Title scene's two screens. Screen 1 is the title (background image +
// logo + Start button); Start switches to screen 2, character select, shown on a
// plain black fill - the whole title screen (background included) is turned off,
// so it reads as moving to the next UI rather than an overlay on the title art.
// Picking an archetype calls GameFlow.StartRun, which stashes the choice and
// loads the gameplay scene.
public class TitleScreenController : MonoBehaviour
{
    [SerializeField] private Button startButton;
    [SerializeField] private GameObject background;
    [SerializeField] private GameObject titleGroup;
    [SerializeField] private CharacterSelectUI characterSelectUI;

    private void Awake()
    {
        startButton.onClick.AddListener(HandleStartClicked);
        background.SetActive(true);
        titleGroup.SetActive(true);
    }

    private void HandleStartClicked()
    {
        background.SetActive(false);
        titleGroup.SetActive(false);
        characterSelectUI.Show(GameFlow.StartRun);
    }
}
