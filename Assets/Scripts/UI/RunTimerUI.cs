using UnityEngine;
using UnityEngine.UI;

// Live run timer in the HUD. Shows the current floor's elapsed time
// (StageManager.ElapsedTime, which already resets on AdvanceToNextFloor) as
// mm:ss. Purely display - reads once per frame, no state of its own.
public class RunTimerUI : MonoBehaviour
{
    [SerializeField] private StageManager stageManager;
    [SerializeField] private Text timeText;

    private void Update()
    {
        if (stageManager == null || timeText == null)
        {
            return;
        }

        float t = Mathf.Max(0f, stageManager.ElapsedTime);
        int minutes = Mathf.FloorToInt(t / 60f);
        int seconds = Mathf.FloorToInt(t % 60f);
        timeText.text = $"{minutes:00}:{seconds:00}";
    }
}
