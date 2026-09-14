using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

// One-shot console dump over every IValidatable ScriptableObject asset - same underlying scan
// (DataValidationScanner) and Validate() checks (Assets/Scripts/Core/IValidatable.cs) as the
// dockable DataValidationWindow, just printed to the Console instead of kept in a window. Pick
// whichever fits how you're working; both stay in sync since they share the same scanner. Lives
// under Assets/Editor/ (Unity's "magic folder", auto-excluded from player builds) since the
// project has no Editor-only asmdef yet and these are the only scripts that need one.
internal static class DataValidationMenu
{
    [MenuItem("RogueLike/Validate All Game Data")]
    private static void ValidateAllGameData()
    {
        List<DataValidationScanner.Issue> issues = DataValidationScanner.Scan(out int assetsChecked);

        foreach (DataValidationScanner.Issue issue in issues)
        {
            Debug.LogWarning($"[{issue.AssetPath}] {issue.Asset.name}: {issue.Message}", issue.Asset);
        }

        Debug.Log($"RogueLike data validation: checked {assetsChecked} asset(s), found {issues.Count} issue(s).");
    }
}
