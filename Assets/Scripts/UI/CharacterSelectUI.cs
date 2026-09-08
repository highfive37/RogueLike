using System;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// Shown once at the very start of a run, before the player can act: lets the
// player pick their starting weapon archetype (Melee or Ranged).
public class CharacterSelectUI : MonoBehaviour
{
    [SerializeField] private GameObject panel;
    [SerializeField] private Button meleeButton;
    [SerializeField] private Button rangedButton;
    [SerializeField] private CharacterArchetypeData meleeArchetype;
    [SerializeField] private CharacterArchetypeData rangedArchetype;

    private Action<CharacterArchetypeData> _onChosen;
    private Coroutine _armRoutine;

    private void Awake()
    {
        panel.SetActive(false);
        meleeButton.onClick.AddListener(() => HandleChosen(meleeArchetype));
        rangedButton.onClick.AddListener(() => HandleChosen(rangedArchetype));
    }

    public void Show(Action<CharacterArchetypeData> onChosen)
    {
        _onChosen = onChosen;
        panel.SetActive(true);

        // The click/keypress that opened this screen (e.g. the title's Start
        // button) can still be physically held, with a melee/ranged button now
        // sitting right under the cursor - the release would land an instant,
        // unintended pick. Keep the buttons inert until that gesture is fully
        // released. Hover feedback still works; only the click is gated.
        SetButtonsInteractable(false);
        if (EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(null);
        }

        if (_armRoutine != null)
        {
            StopCoroutine(_armRoutine);
        }

        _armRoutine = StartCoroutine(ArmAfterInputReleased());
    }

    private IEnumerator ArmAfterInputReleased()
    {
        // Let at least one frame pass so the opening click finishes its own
        // up/click processing before anything here re-enables.
        yield return null;

        while (IsConfirmInputHeld())
        {
            yield return null;
        }

        yield return null;

        SetButtonsInteractable(true);
        _armRoutine = null;
    }

    private static bool IsConfirmInputHeld()
    {
        if (Mouse.current != null && Mouse.current.leftButton.isPressed)
        {
            return true;
        }

        Keyboard keyboard = Keyboard.current;
        return keyboard != null
            && (keyboard.enterKey.isPressed
                || keyboard.numpadEnterKey.isPressed
                || keyboard.spaceKey.isPressed);
    }

    private void SetButtonsInteractable(bool value)
    {
        meleeButton.interactable = value;
        rangedButton.interactable = value;
    }

    private void HandleChosen(CharacterArchetypeData archetype)
    {
        if (_armRoutine != null)
        {
            StopCoroutine(_armRoutine);
            _armRoutine = null;
        }

        panel.SetActive(false);
        _onChosen?.Invoke(archetype);
    }
}
