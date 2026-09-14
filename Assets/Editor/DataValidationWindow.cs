using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

// One-stop game data browser/editor: left column lists every IValidatable ScriptableObject asset
// in the project (grouped by type, search-filterable, issues from DataValidationScanner flagged
// with a warning marker), right column embeds that asset's actual editable Inspector
// (Editor.CreateEditor + OnInspectorGUI - the same field drawing, Undo support and serialization
// Unity's own Inspector uses) plus its live validation messages. No Project-window round trip -
// browse, see what's wrong, and fix the field, all in one window. The RogueLike/Validate All Game
// Data console command (DataValidationMenu.cs) still exists separately for a quick one-shot dump.
public class DataValidationWindow : EditorWindow
{
    private List<DataValidationScanner.AssetEntry> _allAssets = new List<DataValidationScanner.AssetEntry>();
    private HashSet<ScriptableObject> _assetsWithIssues = new HashSet<ScriptableObject>();
    private int _issueCount;
    private string _searchFilter = "";
    private Vector2 _listScroll;
    private Vector2 _inspectorScroll;
    private ScriptableObject _selectedAsset;
    private Editor _selectedEditor;
    private bool _hasScanned;

    [MenuItem("RogueLike/Data Validation Window")]
    private static void Open()
    {
        var window = GetWindow<DataValidationWindow>("Game Data");
        window.Rescan();
    }

    private void OnDisable()
    {
        ClearSelection();
    }

    private void OnGUI()
    {
        DrawToolbar();

        if (!_hasScanned)
        {
            EditorGUILayout.HelpBox("Click Rescan to load every game data asset in the project.", MessageType.Info);
            return;
        }

        using (new GUILayout.HorizontalScope())
        {
            DrawAssetList();
            DrawInspector();
        }
    }

    private void DrawToolbar()
    {
        using (new GUILayout.HorizontalScope(EditorStyles.toolbar))
        {
            if (GUILayout.Button("Rescan", EditorStyles.toolbarButton, GUILayout.Width(70f)))
            {
                Rescan();
            }

            GUILayout.Space(8f);
            _searchFilter = EditorGUILayout.TextField(_searchFilter, EditorStyles.toolbarSearchField, GUILayout.Width(220f));
            GUILayout.FlexibleSpace();

            if (_hasScanned)
            {
                GUILayout.Label($"{_allAssets.Count} asset(s), {_issueCount} issue(s)", EditorStyles.miniLabel);
            }
        }
    }

    private static GUIStyle _categoryHeaderStyle;
    private static GUIStyle CategoryHeaderStyle => _categoryHeaderStyle ??= new GUIStyle(EditorStyles.boldLabel) { fontSize = 13 };

    private void DrawAssetList()
    {
        using (new GUILayout.VerticalScope(GUILayout.Width(260f)))
        {
            _listScroll = EditorGUILayout.BeginScrollView(_listScroll);

            // category -> type name -> assets, so the list reads as "Player / CharacterArchetypeData
            // / MeleeArchetype" instead of one flat alphabetical dump of every type.
            var byCategory = new Dictionary<string, SortedDictionary<string, List<DataValidationScanner.AssetEntry>>>();

            foreach (DataValidationScanner.AssetEntry entry in _allAssets)
            {
                if (!string.IsNullOrEmpty(_searchFilter) &&
                    entry.Asset.name.IndexOf(_searchFilter, StringComparison.OrdinalIgnoreCase) < 0)
                {
                    continue;
                }

                string category = DataValidationCategories.Categorize(entry.Asset);
                if (!byCategory.TryGetValue(category, out SortedDictionary<string, List<DataValidationScanner.AssetEntry>> byType))
                {
                    byType = new SortedDictionary<string, List<DataValidationScanner.AssetEntry>>(StringComparer.Ordinal);
                    byCategory[category] = byType;
                }

                string typeName = entry.Asset.GetType().Name;
                if (!byType.TryGetValue(typeName, out List<DataValidationScanner.AssetEntry> list))
                {
                    list = new List<DataValidationScanner.AssetEntry>();
                    byType[typeName] = list;
                }

                list.Add(entry);
            }

            foreach (string category in DataValidationCategories.Order)
            {
                if (!byCategory.TryGetValue(category, out SortedDictionary<string, List<DataValidationScanner.AssetEntry>> byType))
                {
                    continue;
                }

                EditorGUILayout.LabelField(category, CategoryHeaderStyle);
                EditorGUI.indentLevel++;

                foreach (KeyValuePair<string, List<DataValidationScanner.AssetEntry>> group in byType)
                {
                    EditorGUILayout.LabelField(group.Key, EditorStyles.boldLabel);
                    EditorGUI.indentLevel++;

                    foreach (DataValidationScanner.AssetEntry entry in group.Value)
                    {
                        bool hasIssue = _assetsWithIssues.Contains(entry.Asset);
                        bool isSelected = entry.Asset == _selectedAsset;
                        string label = hasIssue ? $"⚠ {entry.Asset.name}" : entry.Asset.name;
                        GUIStyle style = isSelected ? EditorStyles.boldLabel : EditorStyles.label;

                        if (GUILayout.Button(label, style))
                        {
                            SelectAsset(entry.Asset);
                        }
                    }

                    EditorGUI.indentLevel--;
                }

                EditorGUI.indentLevel--;
                EditorGUILayout.Space(6f);
            }

            EditorGUILayout.EndScrollView();
        }
    }

    private void DrawInspector()
    {
        using (new GUILayout.VerticalScope())
        {
            if (_selectedAsset == null)
            {
                EditorGUILayout.HelpBox("Select an asset on the left to edit it here.", MessageType.Info);
                return;
            }

            EditorGUILayout.LabelField(_selectedAsset.name, EditorStyles.boldLabel);
            EditorGUILayout.LabelField(AssetDatabase.GetAssetPath(_selectedAsset), EditorStyles.miniLabel);

            if (_selectedAsset is IValidatable validatable)
            {
                List<string> liveErrors = DataValidation.CollectIssues(validatable);
                foreach (string error in liveErrors)
                {
                    EditorGUILayout.HelpBox(error, MessageType.Warning);
                }
            }

            EditorGUILayout.Space();

            _inspectorScroll = EditorGUILayout.BeginScrollView(_inspectorScroll);
            if (_selectedEditor != null)
            {
                _selectedEditor.OnInspectorGUI();
            }
            EditorGUILayout.EndScrollView();
        }
    }

    private void Rescan()
    {
        _allAssets = DataValidationScanner.FindAllValidatableAssets();

        List<DataValidationScanner.Issue> issues = DataValidationScanner.Scan(out _);
        _issueCount = issues.Count;
        _assetsWithIssues = new HashSet<ScriptableObject>();
        foreach (DataValidationScanner.Issue issue in issues)
        {
            _assetsWithIssues.Add((ScriptableObject)issue.Asset);
        }

        _hasScanned = true;
        Repaint();
    }

    private void SelectAsset(ScriptableObject asset)
    {
        if (_selectedAsset == asset)
        {
            return;
        }

        ClearSelection();
        _selectedAsset = asset;
        _selectedEditor = Editor.CreateEditor(asset);
    }

    private void ClearSelection()
    {
        if (_selectedEditor != null)
        {
            DestroyImmediate(_selectedEditor);
            _selectedEditor = null;
        }

        _selectedAsset = null;
    }
}
