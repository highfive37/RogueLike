using System;
using UnityEngine;
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
    }

    private void HandleChosen(CharacterArchetypeData archetype)
    {
        panel.SetActive(false);
        _onChosen?.Invoke(archetype);
    }
}
