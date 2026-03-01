#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;

namespace RDOnline.Editor
{
    /// <summary>
    /// 查找项目中未被任何其他资源引用的资源，便于清理删除。
    /// 菜单：Tools -> 查找未使用资源
    /// </summary>
    public class UnusedAssetsFinder : EditorWindow
    {
        private Vector2 _scrollPos;
        private List<string> _unusedPaths = new List<string>();
        private HashSet<int> _selectedIndices = new HashSet<int>();
        private string _searchFilter = "";
        private bool _isScanning;
        private string _statusText = "";
        private int _totalScanned;
        private int _totalAssets;

        [Header("过滤选项")]
        private bool _excludeResources = true;
        private bool _excludeStreamingAssets = true;
        private bool _excludeEditor = true;
        private bool _excludeScenesInBuild = true;
        private bool _excludeScripts = false;
        private bool _excludePlugins = false;
        private bool _excludeMaterials = false;

        private const string AssetsFolder = "Assets";

        [MenuItem("Tools/查找未使用资源")]
        public static void ShowWindow()
        {
            var win = GetWindow<UnusedAssetsFinder>("未使用资源");
            win.minSize = new Vector2(400, 300);
        }

        private void OnGUI()
        {
            EditorGUILayout.Space(4);
            DrawOptions();
            EditorGUILayout.Space(4);

            if (GUILayout.Button(_isScanning ? "扫描中..." : "开始扫描", GUILayout.Height(28)))
            {
                if (!_isScanning)
                    StartScan();
            }

            if (!string.IsNullOrEmpty(_statusText))
            {
                EditorGUILayout.HelpBox(_statusText, MessageType.Info);
            }
            EditorGUILayout.HelpBox(
                "“未使用”指在资源依赖中未被引用。Resources/StreamingAssets 内资源可能被代码按路径加载，删除前请确认。",
                MessageType.None);

            EditorGUILayout.Space(4);

            if (_unusedPaths.Count > 0)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField($"未使用资源: {_unusedPaths.Count} 个", EditorStyles.boldLabel);
                if (GUILayout.Button("全选", GUILayout.Width(50)))
                {
                    _selectedIndices.Clear();
                    for (int i = 0; i < _unusedPaths.Count; i++) _selectedIndices.Add(i);
                }
                if (GUILayout.Button("取消全选", GUILayout.Width(60)))
                    _selectedIndices.Clear();
                EditorGUILayout.EndHorizontal();

                _searchFilter = EditorGUILayout.TextField("筛选", _searchFilter);

                _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);
                var filtered = GetFilteredList();
                for (int i = 0; i < filtered.Count; i++)
                {
                    int realIndex = _unusedPaths.IndexOf(filtered[i]);
                    bool selected = _selectedIndices.Contains(realIndex);
                    bool newSelected = EditorGUILayout.ToggleLeft(filtered[i], selected);
                    if (newSelected != selected)
                    {
                        if (newSelected) _selectedIndices.Add(realIndex);
                        else _selectedIndices.Remove(realIndex);
                    }
                }
                EditorGUILayout.EndScrollView();

                EditorGUILayout.Space(4);
                EditorGUI.BeginDisabledGroup(_selectedIndices.Count == 0);
                if (GUILayout.Button($"删除选中的 {_selectedIndices.Count} 个资源", GUILayout.Height(24)))
                {
                    DeleteSelected();
                }
                EditorGUI.EndDisabledGroup();
            }
        }

        private List<string> GetFilteredList()
        {
            if (string.IsNullOrWhiteSpace(_searchFilter))
                return _unusedPaths;
            var lower = _searchFilter.Trim().ToLowerInvariant();
            return _unusedPaths.Where(p => p.ToLowerInvariant().Contains(lower)).ToList();
        }

        private void DrawOptions()
        {
            _excludeResources = EditorGUILayout.Toggle("排除 Resources 目录", _excludeResources);
            _excludeStreamingAssets = EditorGUILayout.Toggle("排除 StreamingAssets", _excludeStreamingAssets);
            _excludeEditor = EditorGUILayout.Toggle("排除 Editor 目录", _excludeEditor);
            _excludeScenesInBuild = EditorGUILayout.Toggle("排除已加入构建设的场景", _excludeScenesInBuild);
            _excludeScripts = EditorGUILayout.Toggle("排除脚本 (.cs)", _excludeScripts);
            _excludePlugins = EditorGUILayout.Toggle("排除 Plugins 目录", _excludePlugins);
            _excludeMaterials = EditorGUILayout.Toggle("排除材质球 (谨慎)", _excludeMaterials);
        }

        private void StartScan()
        {
            _isScanning = true;
            _unusedPaths.Clear();
            _selectedIndices.Clear();
            _statusText = "正在收集资源路径...";
            Repaint();

            EditorApplication.delayCall += () =>
            {
                try
                {
                    ScanUnusedAssets();
                }
                finally
                {
                    _isScanning = false;
                    _statusText = $"扫描完成。共扫描 {_totalScanned} 个资源，发现 {_unusedPaths.Count} 个未被引用的资源。";
                    Repaint();
                }
            };
        }

        private void ScanUnusedAssets()
        {
            string[] allGuids = AssetDatabase.FindAssets("", new[] { AssetsFolder });
            _totalAssets = allGuids.Length;
            var pathToReferrers = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);

            for (int i = 0; i < allGuids.Length; i++)
            {
                if (i % 30 == 0)
                {
                    _statusText = $"扫描依赖中... {i}/{_totalAssets}";
                    float progress = (float)i / Mathf.Max(1, _totalAssets);
                    if (EditorUtility.DisplayCancelableProgressBar("查找未使用资源", _statusText, progress))
                    {
                        EditorUtility.ClearProgressBar();
                        return;
                    }
                    Repaint();
                    if (EditorApplication.isCompiling)
                    {
                        EditorUtility.ClearProgressBar();
                        return;
                    }
                }

                string path = AssetDatabase.GUIDToAssetPath(allGuids[i]);
                if (string.IsNullOrEmpty(path) || path.StartsWith("Assets/") == false)
                    continue;

                string[] deps = AssetDatabase.GetDependencies(path, true);
                foreach (string dep in deps)
                {
                    if (string.IsNullOrEmpty(dep) || dep.StartsWith("Assets/") == false)
                        continue;
                    if (!pathToReferrers.ContainsKey(dep))
                        pathToReferrers[dep] = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    pathToReferrers[dep].Add(path);
                }
            }

            _totalScanned = allGuids.Length;
            var allPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < allGuids.Length; i++)
            {
                string p = AssetDatabase.GUIDToAssetPath(allGuids[i]);
                if (!string.IsNullOrEmpty(p) && p.StartsWith("Assets/") && !p.EndsWith(".meta", StringComparison.OrdinalIgnoreCase))
                    allPaths.Add(p);
            }

            var unused = new List<string>();
            foreach (string assetPath in allPaths)
            {
                if (AssetDatabase.IsValidFolder(assetPath))
                    continue;
                bool hasReferrer = pathToReferrers.TryGetValue(assetPath, out var referrers);
                bool noRealReferrer = !hasReferrer || referrers.Count == 0 ||
                    (referrers.Count == 1 && referrers.Contains(assetPath));
                if (noRealReferrer && !ShouldExclude(assetPath))
                    unused.Add(assetPath);
            }

            _unusedPaths = unused.OrderBy(p => p).ToList();
            EditorUtility.ClearProgressBar();
        }

        private bool ShouldExclude(string path)
        {
            string lower = path.Replace('\\', '/').ToLowerInvariant();
            if (_excludeResources && lower.Contains("/resources/"))
                return true;
            if (_excludeStreamingAssets && lower.Contains("/streamingassets/"))
                return true;
            if (_excludeEditor && lower.Contains("/editor/"))
                return true;
            if (_excludePlugins && lower.Contains("/plugins/"))
                return true;
            if (_excludeScripts && lower.EndsWith(".cs"))
                return true;
            if (_excludeMaterials && (lower.EndsWith(".mat") || lower.Contains(".mat")))
                return true;
            if (_excludeScenesInBuild && lower.EndsWith(".unity"))
            {
                if (EditorBuildSettings.scenes.Any(s => string.Equals(s.path, path, StringComparison.OrdinalIgnoreCase)))
                    return true;
            }
            if (path.EndsWith(".meta", StringComparison.OrdinalIgnoreCase))
                return true;
            return false;
        }

        private void DeleteSelected()
        {
            var toDelete = _selectedIndices.Select(i => _unusedPaths[i]).ToList();
            if (toDelete.Count == 0) return;
            if (!EditorUtility.DisplayDialog("确认删除", $"确定要删除选中的 {toDelete.Count} 个资源吗？此操作不可撤销。", "删除", "取消"))
                return;

            int deleted = 0;
            foreach (string path in toDelete)
            {
                if (AssetDatabase.DeleteAsset(path))
                    deleted++;
            }
            _selectedIndices.Clear();
            _unusedPaths.RemoveAll(toDelete.Contains);
            _statusText = $"已删除 {deleted} 个资源。";
            AssetDatabase.Refresh();
            Repaint();
        }
    }
}
#endif
