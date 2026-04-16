#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

/// <summary>
/// 清理对话UI编辑器工具
/// 在Unity菜单中执行：Tools → Cleanup Dialogue UI
/// </summary>
public class CleanupOldDialogueUI
{
    [MenuItem("Tools/Cleanup Dialogue UI")]
    public static void CleanupDialogueUI()
    {
        int removedCount = 0;

        // 查找所有DialoguePanel
        GameObject[] panels = GameObject.FindObjectsOfType<GameObject>(true);
        foreach (GameObject obj in panels)
        {
            if (obj.name.Contains("DialoguePanel") ||
                obj.name.Contains("DialogueManager") ||
                obj.name.Contains("Canvas_AutoCreated"))
            {
                // 跳过Prefab
                if (UnityEditor.PrefabUtility.IsPartOfAnyPrefab(obj))
                    continue;

                Debug.Log($"删除对象: {obj.name}");
                Object.DestroyImmediate(obj);
                removedCount++;
            }
        }

        // 清理可能残留的TMP组件
        TMPro.TextMeshProUGUI[] tmpTexts = GameObject.FindObjectsOfType<TMPro.TextMeshProUGUI>(true);
        foreach (TMPro.TextMeshProUGUI tmp in tmpTexts)
        {
            if (tmp.transform.parent != null &&
                (tmp.transform.parent.name.Contains("DialoguePanel") ||
                 tmp.transform.parent.name.Contains("DialogueManager")))
            {
                Debug.Log($"删除TMP文本: {tmp.name}");
                Object.DestroyImmediate(tmp.gameObject);
                removedCount++;
            }
        }

        Debug.Log($"<color=green>清理完成！删除了 {removedCount} 个对象。</color>");
        Debug.Log("现在请重新运行游戏，会自动创建新的UI。");
    }
}
#endif
