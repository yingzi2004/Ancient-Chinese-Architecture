using UnityEngine;
using UnityEditor;
using UnityEngine.UI;

/// <summary>
/// 祈福灯系统自动设置窗口
/// 帮助快速设置祈福灯系统
/// </summary>
public class PrayerLanternSetupWindow : EditorWindow
{
    private GameObject selectedObject;
    private bool createUI = true;
    private bool createSpawnPoint = true;

    [MenuItem("GameObject/祈福灯系统/自动设置", false, 10)]
    public static void ShowWindow()
    {
        GetWindow<PrayerLanternSetupWindow>("祈福灯系统设置");
    }

    void OnGUI()
    {
        GUILayout.Label("祈福灯系统快速设置", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        // 选中对象
        EditorGUILayout.BeginHorizontal();
        GUILayout.Label("父对象:", GUILayout.Width(100));
        selectedObject = (GameObject)EditorGUILayout.ObjectField(selectedObject, typeof(GameObject), true);
        EditorGUILayout.EndHorizontal();

        if (selectedObject == null)
        {
            EditorGUILayout.HelpBox("请选择一个父对象（或留空使用场景根目录）", MessageType.Info);
        }

        EditorGUILayout.Space();

        // 选项
        createUI = EditorGUILayout.Toggle("创建UI面板", createUI);
        createSpawnPoint = EditorGUILayout.Toggle("创建生成点", createSpawnPoint);

        EditorGUILayout.Space();

        // 设置按钮
        if (GUILayout.Button("自动设置系统", GUILayout.Height(40)))
        {
            SetupSystem();
        }

        EditorGUILayout.Space();

        // 说明
        EditorGUILayout.HelpBox(
            "此工具将自动创建：\n" +
            "1. PrayerLanternManager - 管理祈福灯生成\n" +
            "2. PrayerLanternUI - 祈福输入界面\n" +
            "3. LanternSpawnPoint - 祈福灯生成位置\n\n" +
            "注意：需要手动创建祈福灯预制体并关联到Manager",
            MessageType.Info
        );
    }

    void SetupSystem()
    {
        GameObject parent = selectedObject;

        if (parent == null)
        {
            // 创建一个父对象
            parent = new GameObject("PrayerLanternSystem");
        }

        // 创建Manager
        CreateManager(parent);

        // 创建UI
        if (createUI)
        {
            CreateUI(parent);
        }

        // 创建生成点
        if (createSpawnPoint)
        {
            CreateSpawnPoint(parent);
        }

        // 标记场景为已修改
        EditorUtility.SetDirty(parent);
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());

        Debug.Log("祈福灯系统设置完成！");
    }

    void CreateManager(GameObject parent)
    {
        GameObject managerObj = new GameObject("PrayerLanternManager");
        managerObj.transform.SetParent(parent.transform);

        PrayerLanternManager manager = managerObj.AddComponent<PrayerLanternManager>();

        // 设置默认值
        manager.maxLanterns = 50;
        manager.autoSpawn = true;
        manager.autoSpawnInterval = 5f;

        Debug.Log("已创建 PrayerLanternManager");
    }

    void CreateUI(GameObject parent)
    {
        GameObject uiObj = new GameObject("PrayerLanternUI");
        uiObj.transform.SetParent(parent.transform);

        Canvas canvas = uiObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        uiObj.AddComponent<CanvasScaler>();
        uiObj.AddComponent<GraphicRaycaster>();

        PrayerLanternUI ui = uiObj.AddComponent<PrayerLanternUI>();

        // 创建EventSystem（如果不存在）
        if (UnityEngine.EventSystems.EventSystem.current == null)
        {
            GameObject eventSystem = new GameObject("EventSystem");
            eventSystem.AddComponent<UnityEngine.EventSystems.EventSystem>();
            eventSystem.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
        }

        Debug.Log("已创建 PrayerLanternUI Canvas");
    }

    void CreateSpawnPoint(GameObject parent)
    {
        GameObject spawnPoint = new GameObject("LanternSpawnPoint");
        spawnPoint.transform.SetParent(parent.transform);
        spawnPoint.transform.localPosition = new Vector3(0, 1f, 5);

        Debug.Log("已创建 LanternSpawnPoint");
    }
}

/// <summary>
/// 祈福灯编辑器扩展
/// </summary>
[CustomEditor(typeof(PrayerLanternManager))]
public class PrayerLanternManagerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        PrayerLanternManager manager = (PrayerLanternManager)target;

        EditorGUILayout.Space();

        if (GUILayout.Button("清除所有祈福数据", GUILayout.Height(30)))
        {
            if (EditorUtility.DisplayDialog("确认清除",
                "确定要清除所有保存的祈福数据吗？此操作不可撤销！", "确定", "取消"))
            {
                if (Application.isPlaying)
                {
                    manager.ClearAllPrayers();
                    EditorUtility.DisplayDialog("成功", "祈福数据已清除", "确定");
                }
                else
                {
                    EditorUtility.DisplayDialog("提示", "请在运行模式下执行此操作", "确定");
                }
            }
        }

        EditorGUILayout.Space();

        if (Application.isPlaying)
        {
            EditorGUILayout.HelpBox(
                $"当前祈福数量: {manager.GetPrayerCount()}\n" +
                $"活动祈福灯数量: {(manager as PrayerLanternManager)?.GetPrayerCount() ?? 0}",
                MessageType.Info
            );
        }
    }
}
