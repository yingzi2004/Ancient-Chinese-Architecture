using UnityEngine;
using UnityEditor;

/// <summary>
/// 交互式祈福灯快速设置工具
/// </summary>
public class InteractiveLanternSetup
{
    [MenuItem("GameObject/创建交互式祈福灯", false, 12)]
    public static void CreateInteractiveLantern()
    {
        // 创建交互点
        GameObject interactPoint = new GameObject("InteractivePrayerLantern");

        // 添加脚本
        InteractivePrayerLantern script = interactPoint.AddComponent<InteractivePrayerLantern>();

        // 查找Manager和预制体
        PrayerLanternManager manager = Object.FindObjectOfType<PrayerLanternManager>();
        if (manager != null && manager.lanternPrefab != null)
        {
            script.lanternPrefab = manager.lanternPrefab;
        }

        // 设置默认位置
        interactPoint.transform.position = new Vector3(0, 1f, 5);

        // 选中新创建的对象
        Selection.activeGameObject = interactPoint;

        // 标记为已修改
        EditorUtility.SetDirty(interactPoint);
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene()
        );

        // 自动关闭Manager的自动生成
        if (manager != null)
        {
            manager.autoSpawn = false;
            EditorUtility.SetDirty(manager);
            Debug.Log("✅ 已自动关闭PrayerLanternManager的自动生成功能");
        }

        Debug.Log("✅ 交互式祈福灯创建成功！\n" +
                   "• 玩家靠近按F键放飞\n" +
                   "• 只有一个灯笼\n" +
                   "• 无火焰粒子效果\n\n" +
                   "提示：在Scene窗口调整位置，建议放在祈年殿前方");
    }
}
