using UnityEngine;
using UnityEngine.EventSystems;

// Sits on the same GameObject as MinimapUI (which already carries a raycastable Image for its own
// RectMask2D background) purely to open FullFloorMapUI on click - deliberately NOT added to
// MinimapUI.cs itself so that file stays a pure display component with no knowledge of the
// full-screen map (2026-09-15 - keeps the two features from touching each other's files).
public class MinimapMapOpenTrigger : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] private FullFloorMapUI fullFloorMapUI;

    public void OnPointerClick(PointerEventData eventData)
    {
        if (PauseState.IsPaused)
        {
            return;
        }

        fullFloorMapUI.Open();
    }
}
