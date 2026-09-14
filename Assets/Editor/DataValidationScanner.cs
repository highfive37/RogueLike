using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

// Shared project-wide scan used by both the console command (DataValidationMenu.cs) and the
// dockable window (DataValidationWindow.cs), so the two presentations can't drift apart - the
// actual per-asset checks still live only in each ScriptableObject's own Validate()
// (Assets/Scripts/Core/IValidatable.cs).
internal static class DataValidationScanner
{
    public readonly struct Issue
    {
        public readonly string AssetPath;
        public readonly Object Asset;
        public readonly string Message;

        public Issue(string assetPath, Object asset, string message)
        {
            AssetPath = assetPath;
            Asset = asset;
            Message = message;
        }
    }

    public readonly struct AssetEntry
    {
        public readonly string AssetPath;
        public readonly ScriptableObject Asset;

        public AssetEntry(string assetPath, ScriptableObject asset)
        {
            AssetPath = assetPath;
            Asset = asset;
        }
    }

    // Every IValidatable ScriptableObject asset in the project, regardless of whether it
    // currently has an issue - the browsable universe for DataValidationWindow's asset list.
    public static List<AssetEntry> FindAllValidatableAssets()
    {
        var result = new List<AssetEntry>();
        string[] guids = AssetDatabase.FindAssets("t:ScriptableObject");

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var asset = AssetDatabase.LoadAssetAtPath<ScriptableObject>(path);

            if (asset is IValidatable)
            {
                result.Add(new AssetEntry(path, asset));
            }
        }

        return result;
    }

    public static List<Issue> Scan(out int assetsChecked)
    {
        var issues = new List<Issue>();
        List<AssetEntry> entries = FindAllValidatableAssets();
        assetsChecked = entries.Count;

        foreach (AssetEntry entry in entries)
        {
            List<string> errors = DataValidation.CollectIssues((IValidatable)entry.Asset);

            foreach (string error in errors)
            {
                issues.Add(new Issue(entry.AssetPath, entry.Asset, error));
            }
        }

        return issues;
    }
}
