using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;
using System.Linq;

public class FindUnusedScripts : EditorWindow
{
    [MenuItem("工具/查找未使用的脚本")]
    public static void ShowWindow()
    {
        GetWindow<FindUnusedScripts>("查找未使用的脚本");
    }

    private List<string> unusedScripts = new List<string>();

    private void OnGUI()
    {
        if (GUILayout.Button("Scan Project for Unused Scripts"))
        {
            unusedScripts = FindUnreferencedScripts();
        }

        if (unusedScripts.Count > 0)
        {
            GUILayout.Label($"Found {unusedScripts.Count} unused scripts:", EditorStyles.boldLabel);
            
            using (new GUILayout.ScrollViewScope(Vector2.zero))
            {
                foreach (string scriptPath in unusedScripts)
                {
                    EditorGUILayout.SelectableLabel(scriptPath, EditorStyles.textField, GUILayout.Height(EditorGUIUtility.singleLineHeight));
                }
            }
        }
    }

    private List<string> FindUnreferencedScripts()
    {
        // 1. 获取所有脚本
        string[] allScriptGuids = AssetDatabase.FindAssets("t:MonoScript", new[] { "Assets/Script", "Assets/Scripts" });
        Dictionary<string, string> scriptGuidToPath = new Dictionary<string, string>();
        
        foreach (string guid in allScriptGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (!path.Contains("/Editor/") && !path.Contains("Plugins")) 
            {
                scriptGuidToPath[guid] = path;
            }
        }

        // 2. 获取所有可能挂载脚本的文件 (预制体、场景、ScriptableObject等)
        string[] allAssetGuids = AssetDatabase.FindAssets("t:Prefab t:Scene t:ScriptableObject", new[] { "Assets" });
        
        foreach (string assetGuid in allAssetGuids)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(assetGuid);
            string[] dependencies = AssetDatabase.GetDependencies(assetPath, false);

            foreach (string dependency in dependencies)
            {
                string dependencyGuid = AssetDatabase.AssetPathToGUID(dependency);
                if (scriptGuidToPath.ContainsKey(dependencyGuid))
                {
                    // 被引用了，从字典中移除
                    scriptGuidToPath.Remove(dependencyGuid);
                }
            }
        }

        return scriptGuidToPath.Values.ToList();
    }
}