#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

/// <summary>
/// 快速修复工具：修复显示为紫色的烟雾效果
/// </summary>
public class FixPurpleSmoke : EditorWindow
{
    [MenuItem("工具/修复紫色烟雾效果")]
    public static void ShowWindow()
    {
        GetWindow<FixPurpleSmoke>("修复紫色烟雾");
    }

    private void OnGUI()
    {
        GUILayout.Label("🔧 紫色烟雾修复工具", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        EditorGUILayout.HelpBox(
            "如果您的烟雾显示为紫色，说明粒子系统缺少正确的材质。\n\n" +
            "使用下面的按钮可以一键修复所有场景中的烟雾效果。",
            MessageType.Info);

        EditorGUILayout.Space();

        // 修复当前选中的对象
        if (Selection.activeGameObject != null)
        {
            var smokeEffect = Selection.activeGameObject.GetComponent<IncenseSmokeEffect>();
            if (smokeEffect != null)
            {
                if (GUILayout.Button("🔧 修复当前选中的烟雾", GUILayout.Height(40)))
                {
                    FixSmoke(Selection.activeGameObject);
                }
            }
            else
            {
                EditorGUILayout.HelpBox("当前选中的对象没有IncenseSmokeEffect组件", MessageType.Warning);
            }
        }
        else
        {
            EditorGUILayout.HelpBox("请选择一个带有烟雾效果的对象", MessageType.Warning);
        }

        EditorGUILayout.Space();

        // 修复场景中所有的烟雾
        if (GUILayout.Button("🔧 修复场景中所有烟雾效果", GUILayout.Height(40)))
        {
            FixAllSmokeInScene();
        }

        EditorGUILayout.Space();

        // 手动创建材质
        GUILayout.Label("创建烟雾材质", EditorStyles.boldLabel);
        if (GUILayout.Button("➕ 创建并保存烟雾材质"))
        {
            CreateSmokeMaterial();
        }

        EditorGUILayout.Space();
        EditorGUILayout.HelpBox(
            "修复步骤：\n" +
            "1. 选中显示紫色的烟雾对象\n" +
            "2. 点击'修复当前选中的烟雾'\n" +
            "3. 运行游戏查看效果\n\n" +
            "或者：\n" +
            "直接点击'修复场景中所有烟雾效果'",
            MessageType.None);
    }

    private void FixSmoke(GameObject obj)
    {
        var smokeEffect = obj.GetComponent<IncenseSmokeEffect>();
        if (smokeEffect != null)
        {
            // 重新配置粒子系统（会自动设置材质）
            smokeEffect.SendMessage("ConfigureParticleSystem");

            // 标记为已修改
            EditorUtility.SetDirty(obj);
            EditorUtility.SetDirty(smokeEffect);

            Debug.Log($"[修复成功] 已修复 '{obj.name}' 的烟雾效果");
            EditorUtility.DisplayDialog("修复成功", $"已修复 {obj.name} 的烟雾效果！", "确定");
        }
    }

    private void FixAllSmokeInScene()
    {
        int fixedCount = 0;
        IncenseSmokeEffect[] allSmokeEffects = GameObject.FindObjectsOfType<IncenseSmokeEffect>();

        foreach (var smokeEffect in allSmokeEffects)
        {
            // 重新配置
            smokeEffect.SendMessage("ConfigureParticleSystem");
            EditorUtility.SetDirty(smokeEffect.gameObject);
            EditorUtility.SetDirty(smokeEffect);
            fixedCount++;
        }

        if (fixedCount > 0)
        {
            Debug.Log($"[批量修复] 已修复场景中的 {fixedCount} 个烟雾效果");
            EditorUtility.DisplayDialog("修复完成", $"已成功修复 {fixedCount} 个烟雾效果！", "确定");
        }
        else
        {
            EditorUtility.DisplayDialog("未找到", "场景中没有找到烟雾效果对象", "确定");
        }
    }

    private void CreateSmokeMaterial()
    {
        // 保存路径
        string path = EditorUtility.SaveFilePanelInProject(
            "保存烟雾材质",
            "SmokeMaterial",
            "mat",
            "选择保存位置"
        );

        if (!string.IsNullOrEmpty(path))
        {
            // 创建材质
            Material smokeMaterial = new Material(Shader.Find("Particles/Standard Unlit"));
            smokeMaterial.color = Color.white;
            smokeMaterial.SetFloat("_Mode", 3);
            smokeMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            smokeMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            smokeMaterial.SetInt("_ZWrite", 0);
            smokeMaterial.DisableKeyword("_ALPHATEST_ON");
            smokeMaterial.EnableKeyword("_ALPHABLEND_ON");
            smokeMaterial.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            smokeMaterial.renderQueue = 3000;

            // 保存材质
            AssetDatabase.CreateAsset(smokeMaterial, path);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"[创建成功] 已创建烟雾材质: {path}");
            EditorUtility.DisplayDialog("创建成功", $"烟雾材质已保存到:\n{path}", "确定");

            // 选中新建的材质
            Selection.activeObject = smokeMaterial;
        }
    }
}
#endif
