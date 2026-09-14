using System.Collections.Generic;
using UnityEngine;

// Shared plumbing for IValidatable ScriptableObjects: turns the pure Validate(List<string>) check
// into either an immediate Inspector warning (LogIssues, called from each asset's own
// OnValidate()) or a raw list for a full project scan (CollectIssues, called by the Editor menu
// command in Assets/Editor/DataValidationMenu.cs). Kept in the main assembly rather than the
// Editor-only one so every data asset's OnValidate() can call it without an editor-only
// reference.
public static class DataValidation
{
    public static void LogIssues(Object asset, IValidatable validatable)
    {
        List<string> errors = CollectIssues(validatable);

        foreach (string error in errors)
        {
            Debug.LogWarning($"{asset.name}: {error}", asset);
        }
    }

    public static List<string> CollectIssues(IValidatable validatable)
    {
        var errors = new List<string>();
        validatable.Validate(errors);
        return errors;
    }
}
