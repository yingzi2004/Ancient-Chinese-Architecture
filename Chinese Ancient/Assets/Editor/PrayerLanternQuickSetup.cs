using UnityEngine;
using UnityEditor;

/// <summary>
/// 祈福灯系统快速设置工具（简化版）
/// </summary>
public class PrayerLanternQuickSetup
{
    [MenuItem("GameObject/创建祈福灯系统", false, 10)]
    public static void CreatePrayerLanternSystem()
    {
        // 创建父对象
        GameObject systemParent = new GameObject("PrayerLanternSystem");

        // 创建管理器
        GameObject managerObj = new GameObject("PrayerLanternManager");
        managerObj.transform.SetParent(systemParent.transform);
        PrayerLanternManager manager = managerObj.AddComponent<PrayerLanternManager>();

        // 创建生成点
        GameObject spawnPoint = new GameObject("LanternSpawnPoint");
        spawnPoint.transform.SetParent(systemParent.transform);
        spawnPoint.transform.localPosition = new Vector3(0, 1f, 5);

        // 关联生成点
        manager.spawnPoint = spawnPoint.transform;

        // 选中新创建的对象
        Selection.activeGameObject = systemParent;

        // 标记为已修改
        EditorUtility.SetDirty(systemParent);
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene()
        );

        Debug.Log("✅ 祈福灯系统创建成功！\n" +
                   "• PrayerLanternManager - 管理器\n" +
                   "• LanternSpawnPoint - 生成点\n\n" +
                   "下一步：创建祈福灯预制体（使用下面的菜单选项）");
    }

    [MenuItem("GameObject/创建祈福灯模型", false, 11)]
    public static void CreatePrayerLanternModel()
    {
        // 创建灯笼对象
        GameObject lantern = new GameObject("PrayerLantern");

        // 创建主体
        GameObject body = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        body.name = "Body";
        body.transform.SetParent(lantern.transform);
        body.transform.localPosition = Vector3.zero;
        body.transform.localScale = new Vector3(0.8f, 1f, 0.8f);

        // 设置主体材质
        Material bodyMat = new Material(Shader.Find("Standard"));
        bodyMat.color = new Color(1f, 0.9f, 0.7f, 0.9f);
        bodyMat.SetFloat("_Mode", 3);
        bodyMat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        bodyMat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        bodyMat.SetInt("_ZWrite", 0);
        bodyMat.DisableKeyword("_ALPHATEST_ON");
        bodyMat.EnableKeyword("_ALPHABLEND_ON");
        bodyMat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        bodyMat.renderQueue = 3000;
        body.GetComponent<Renderer>().material = bodyMat;

        // 创建火焰点光源
        GameObject lightObj = new GameObject("LanternLight");
        lightObj.transform.SetParent(lantern.transform);
        lightObj.transform.localPosition = Vector3.zero;

        Light light = lightObj.AddComponent<Light>();
        light.type = LightType.Point;
        light.color = new Color(1f, 0.8f, 0.6f);
        light.intensity = 2f;
        light.range = 10f;

        // 创建火焰粒子
        GameObject fireObj = new GameObject("Fire");
        fireObj.transform.SetParent(lantern.transform);
        fireObj.transform.localPosition = new Vector3(0, -0.3f, 0);

        ParticleSystem ps = fireObj.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.startLifetime = 1f;
        main.startSpeed = 0.5f;
        main.startSize = 0.2f;
        main.startColor = new Color(1f, 0.6f, 0.2f, 1f);
        main.maxParticles = 50;

        var emission = ps.emission;
        emission.rateOverTime = 20;

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 10f;
        shape.radius = 0.05f;

        var colorOverLifetime = ps.colorOverLifetime;
        colorOverLifetime.enabled = true;

        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new GradientColorKey[] {
                new GradientColorKey(new Color(1f, 0.8f, 0.2f, 1f), 0f),
                new GradientColorKey(new Color(1f, 0.4f, 0f, 0f), 1f)
            },
            new GradientAlphaKey[] {
                new GradientAlphaKey(1f, 0f),
                new GradientAlphaKey(0f, 1f)
            }
        );
        colorOverLifetime.color = gradient;

        // 添加PrayerLantern脚本
        PrayerLantern lanternScript = lantern.AddComponent<PrayerLantern>();
        lanternScript.lanternLight = light;
        lanternScript.fireParticle = ps;
        lanternScript.lanternRenderer = body.GetComponent<Renderer>();

        // 添加碰撞体
        BoxCollider collider = lantern.AddComponent<BoxCollider>();
        collider.size = new Vector3(1.2f, 1.2f, 1.2f);
        collider.isTrigger = true;

        // 选中新创建的灯笼
        Selection.activeGameObject = lantern;

        // 标记为已修改
        EditorUtility.SetDirty(lantern);
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene()
        );

        // 询问是否保存为预制体
        if (EditorUtility.DisplayDialog("保存预制体",
            "祈福灯模型已创建！\n\n是否保存为预制体？", "保存", "手动保存"))
        {
            SaveAsPrefab(lantern, bodyMat);
        }

        Debug.Log("✅ 祈福灯模型创建成功！\n" +
                   "• 灯笼主体（半透明）\n" +
                   "• 点光源（暖色）\n" +
                   "• 火焰粒子效果\n" +
                   "• PrayerLantern脚本\n" +
                   "• 碰撞体");
    }

    static void SaveAsPrefab(GameObject lantern, Material material)
    {
        // 确保文件夹存在
        string prefabPath = "Assets/Prefabs";
        if (!System.IO.Directory.Exists(prefabPath))
        {
            System.IO.Directory.CreateDirectory(prefabPath);
        }

        string materialPath = "Assets/Materials";
        if (!System.IO.Directory.Exists(materialPath))
        {
            System.IO.Directory.CreateDirectory(materialPath);
        }

        // 保存材质
        string matPath = $"{materialPath}/LanternMaterial.mat";
        AssetDatabase.CreateAsset(material, matPath);

        // 保存预制体
        string finalPath = $"{prefabPath}/PrayerLantern.prefab";

        // 如果已存在，先删除
        if (AssetDatabase.LoadAssetAtPath<GameObject>(finalPath) != null)
        {
            AssetDatabase.DeleteAsset(finalPath);
        }

        PrefabUtility.SaveAsPrefabAsset(lantern, finalPath);

        // 关联到Manager
        PrayerLanternManager manager = Object.FindObjectOfType<PrayerLanternManager>();
        if (manager != null)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(finalPath);
            manager.lanternPrefab = prefab;
            EditorUtility.SetDirty(manager);
        }

        // 选中预制体
        Selection.activeObject = AssetDatabase.LoadAssetAtPath<GameObject>(finalPath);

        // 删除场景中的原始灯笼对象
        GameObject.DestroyImmediate(lantern);

        Debug.Log($"✅ 预制体已保存到: {finalPath}\n" +
                   $"✅ 材质已保存到: {matPath}\n" +
                   $"✅ 场景中的原始对象已删除");
    }
}
