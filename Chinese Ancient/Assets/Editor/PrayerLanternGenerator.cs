using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;

/// <summary>
/// 祈福灯3D模型生成器
/// 一键生成完整的祈福灯预制体
/// </summary>
public class PrayerLanternGenerator : EditorWindow
{
    private Material lanternMaterial;
    private ParticleSystem fireParticlePrefab;

    [MenuItem("GameObject/祈福灯系统/生成祈福灯模型", false, 11)]
    public static void ShowWindow()
    {
        GetWindow<PrayerLanternGenerator>("祈福灯模型生成器");
    }

    void OnGUI()
    {
        GUILayout.Label("祈福灯3D模型生成器", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        EditorGUILayout.HelpBox(
            "此工具将创建一个完整的祈福灯预制体，包括：\n" +
            "• 灯笼主体（纸质材质）\n" +
            "• 竹制框架\n" +
            "• 火焰粒子效果\n" +
            "• 点光源\n" +
            "• 所有必需的组件",
            MessageType.Info
        );

        EditorGUILayout.Space();

        // 材质选择
        EditorGUILayout.BeginHorizontal();
        GUILayout.Label("灯笼材质（可选）:", GUILayout.Width(150));
        lanternMaterial = (Material)EditorGUILayout.ObjectField(lanternMaterial, typeof(Material), false);
        EditorGUILayout.EndHorizontal();

        // 火焰粒子预设选择
        EditorGUILayout.BeginHorizontal();
        GUILayout.Label("火焰粒子预设（可选）:", GUILayout.Width(150));
        fireParticlePrefab = (ParticleSystem)EditorGUILayout.ObjectField(fireParticlePrefab, typeof(ParticleSystem), false);
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space();

        // 生成按钮
        GUI.backgroundColor = new Color(0.4f, 0.8f, 1f);
        if (GUILayout.Button("生成祈福灯预制体", GUILayout.Height(50)))
        {
            GeneratePrayerLantern();
        }
        GUI.backgroundColor = Color.white;

        EditorGUILayout.Space();

        // 说明
        EditorGUILayout.HelpBox(
            "生成的预制体会保存在 Assets/Prefabs/PrayerLantern.prefab\n" +
            "材质会保存在 Assets/Materials/PrayerLantern.mat",
            MessageType.None
        );
    }

    void GeneratePrayerLantern()
    {
        // 创建主对象
        GameObject lanternObj = new GameObject("PrayerLantern_Preview");

        // 1. 创建灯笼主体
        CreateLanternBody(lanternObj);

        // 2. 创建灯笼框架
        CreateLanternFrame(lanternObj);

        // 3. 创建火焰效果
        CreateFireEffect(lanternObj);

        // 4. 添加光源
        CreateLight(lanternObj);

        // 5. 添加必要的组件
        AddComponents(lanternObj);

        // 6. 创建材质（如果没有提供）
        if (lanternMaterial == null)
        {
            lanternMaterial = CreateLanternMaterial();
        }

        // 应用材质
        ApplyMaterial(lanternObj, lanternMaterial);

        // 选中新创建的对象
        Selection.activeGameObject = lanternObj;

        // 标记场景为已修改
        EditorUtility.SetDirty(lanternObj);
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene()
        );

        Debug.Log("祈福灯模型生成完成！");

        // 询问是否保存为预制体
        if (EditorUtility.DisplayDialog("保存预制体",
            "祈福灯模型已生成！\n\n是否保存为预制体？", "保存", "稍后手动保存"))
        {
            SaveAsPrefab(lanternObj);
        }
    }

    /// <summary>
    /// 创建灯笼主体
    /// </summary>
    void CreateLanternBody(GameObject parent)
    {
        // 创建主体
        GameObject body = new GameObject("LanternBody");
        body.transform.SetParent(parent.transform);
        body.transform.localPosition = Vector3.zero;

        // 添加圆柱体网格
        MeshFilter meshFilter = body.AddComponent<MeshFilter>();
        MeshRenderer meshRenderer = body.AddComponent<MeshRenderer>();

        // 创建灯笼形状的网格（顶部小，底部大的圆柱体）
        Mesh mesh = CreateLanternMesh();
        meshFilter.mesh = mesh;

        // 添加碰撞体
        MeshCollider collider = body.AddComponent<MeshCollider>();
        collider.convex = true;
        collider.sharedMesh = mesh;
    }

    /// <summary>
    /// 创建灯笼网格
    /// </summary>
    Mesh CreateLanternMesh()
    {
        // 使用圆柱体作为基础
        GameObject tempCylinder = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        Mesh cylinderMesh = tempCylinder.GetComponent<MeshFilter>().mesh;

        // 复制网格
        Mesh lanternMesh = Instantiate(cylinderMesh);

        // 修改顶点以创建灯笼形状（底部略大）
        Vector3[] vertices = lanternMesh.vertices;

        for (int i = 0; i < vertices.Length; i++)
        {
            float y = vertices[i].y;

            // 顶部保持较小
            if (y > 0.3f)
            {
                vertices[i] = new Vector3(vertices[i].x * 0.7f, y, vertices[i].z * 0.7f);
            }
            // 底部保持较大
            else if (y < -0.3f)
            {
                vertices[i] = new Vector3(vertices[i].x * 1.2f, y, vertices[i].z * 1.2f);
            }
        }

        lanternMesh.vertices = vertices;
        lanternMesh.RecalculateNormals();
        lanternMesh.RecalculateBounds();

        // 清理临时对象
        DestroyImmediate(tempCylinder);

        return lanternMesh;
    }

    /// <summary>
    /// 创建灯笼框架（竹制结构）
    /// </summary>
    void CreateLanternFrame(GameObject parent)
    {
        GameObject frame = new GameObject("LanternFrame");
        frame.transform.SetParent(parent.transform);
        frame.transform.localPosition = Vector3.zero;

        // 创建顶部框架环
        CreateTopRing(frame);

        // 创建底部框架环
        CreateBottomRing(frame);

        // 创建垂直支撑条（4根）
        CreateVerticalStrips(frame);
    }

    /// <summary>
    /// 创建顶部框架环
    /// </summary>
    void CreateTopRing(GameObject parent)
    {
        GameObject topRing = new GameObject("TopRing");
        topRing.transform.SetParent(parent.transform);
        topRing.transform.localPosition = new Vector3(0, 0.45f, 0);

        // 使用圆柱体作为框架
        GameObject ringPiece = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        ringPiece.name = "RingPiece";
        ringPiece.transform.SetParent(topRing.transform);
        ringPiece.transform.localPosition = Vector3.zero;
        ringPiece.transform.localRotation = Quaternion.Euler(0, 0, 90);
        ringPiece.transform.localScale = new Vector3(0.02f, 0.7f * 1.4f, 0.02f);

        // 移除碰撞体（框架不需要碰撞）
        DestroyImmediate(ringPiece.GetComponent<Collider>());

        // 添加框架材质
        Renderer renderer = ringPiece.GetComponent<Renderer>();
        if (renderer != null)
        {
            Material frameMaterial = new Material(Shader.Find("Standard"));
            frameMaterial.color = new Color(0.6f, 0.5f, 0.3f); // 竹子颜色
            renderer.material = frameMaterial;
        }
    }

    /// <summary>
    /// 创建底部框架环
    /// </summary>
    void CreateBottomRing(GameObject parent)
    {
        GameObject bottomRing = new GameObject("BottomRing");
        bottomRing.transform.SetParent(parent.transform);
        bottomRing.transform.localPosition = new Vector3(0, -0.45f, 0);

        // 使用圆柱体作为框架
        GameObject ringPiece = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        ringPiece.name = "RingPiece";
        ringPiece.transform.SetParent(bottomRing.transform);
        ringPiece.transform.localPosition = Vector3.zero;
        ringPiece.transform.localRotation = Quaternion.Euler(0, 0, 90);
        ringPiece.transform.localScale = new Vector3(0.02f, 1.0f * 1.4f, 0.02f);

        // 移除碰撞体
        DestroyImmediate(ringPiece.GetComponent<Collider>());

        // 添加框架材质
        Renderer renderer = ringPiece.GetComponent<Renderer>();
        if (renderer != null)
        {
            Material frameMaterial = new Material(Shader.Find("Standard"));
            frameMaterial.color = new Color(0.6f, 0.5f, 0.3f); // 竹子颜色
            renderer.material = frameMaterial;
        }
    }

    /// <summary>
    /// 创建垂直支撑条
    /// </summary>
    void CreateVerticalStrips(GameObject parent)
    {
        for (int i = 0; i < 4; i++)
        {
            GameObject strip = new GameObject($"VerticalStrip_{i}");
            strip.transform.SetParent(parent.transform);

            float angle = i * 90f;
            float radius = 0.5f;

            strip.transform.localPosition = new Vector3(
                Mathf.Cos(angle * Mathf.Deg2Rad) * radius,
                0,
                Mathf.Sin(angle * Mathf.Deg2Rad) * radius
            );

            // 使用细长的立方体
            GameObject stripMesh = GameObject.CreatePrimitive(PrimitiveType.Cube);
            stripMesh.name = "StripMesh";
            stripMesh.transform.SetParent(strip.transform);
            stripMesh.transform.localPosition = Vector3.zero;
            stripMesh.transform.localScale = new Vector3(0.03f, 1f, 0.03f);

            // 移除碰撞体
            DestroyImmediate(stripMesh.GetComponent<Collider>());

            // 添加框架材质
            Renderer renderer = stripMesh.GetComponent<Renderer>();
            if (renderer != null)
            {
                Material frameMaterial = new Material(Shader.Find("Standard"));
                frameMaterial.color = new Color(0.6f, 0.5f, 0.3f); // 竹子颜色
                renderer.material = frameMaterial;
            }
        }
    }

    /// <summary>
    /// 创建火焰效果
    /// </summary>
    void CreateFireEffect(GameObject parent)
    {
        GameObject fireObj = new GameObject("Fire");
        fireObj.transform.SetParent(parent.transform);
        fireObj.transform.localPosition = new Vector3(0, -0.3f, 0);

        ParticleSystem ps = fireObj.AddComponent<ParticleSystem>();

        // 如果提供了粒子预设，使用它的配置
        if (fireParticlePrefab != null)
        {
            var main = ps.main;
            var sourceMain = fireParticlePrefab.main;

            main.duration = sourceMain.duration;
            main.loop = sourceMain.loop;
            main.startDelay = sourceMain.startDelay;
            main.startLifetime = sourceMain.startLifetime;
            main.startSpeed = sourceMain.startSpeed;
            main.startSize = sourceMain.startSize;
            main.startColor = sourceMain.startColor;
            main.maxParticles = sourceMain.maxParticles;
        }
        else
        {
            // 使用默认火焰配置
            var main = ps.main;
            main.duration = 1f;
            main.loop = true;
            main.startLifetime = 1f;
            main.startSpeed = 0.5f;
            main.startSize = 0.2f;
            main.startColor = new Color(1f, 0.6f, 0.2f, 1f); // 橙色火焰
            main.maxParticles = 50;

            // 发射器设置
            var emission = ps.emission;
            emission.rateOverTime = 20;

            // 形状设置
            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 10f;
            shape.radius = 0.05f;

            // 颜色渐变
            var colorOverLifetime = ps.colorOverLifetime;
            colorOverLifetime.enabled = true;

            Gradient gradient = new Gradient();
            gradient.SetKeys(
                new GradientColorKey[] {
                    new GradientColorKey(new Color(1f, 0.8f, 0.2f, 1f), 0f),
                    new GradientColorKey(new Color(1f, 0.4f, 0f, 0.8f), 0.5f),
                    new GradientColorKey(new Color(0.5f, 0.2f, 0f, 0f), 1f)
                },
                new GradientAlphaKey[] {
                    new GradientAlphaKey(1f, 0f),
                    new GradientAlphaKey(0.8f, 0.5f),
                    new GradientAlphaKey(0f, 1f)
                }
            );
            colorOverLifetime.color = gradient;

            // 大小渐变
            var sizeOverLifetime = ps.sizeOverLifetime;
            sizeOverLifetime.enabled = true;
            sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(0.3f, 1f);
        }

        // 设置为世界空间
        var mainModule = ps.main;
        mainModule.simulationSpace = ParticleSystemSimulationSpace.World;
    }

    /// <summary>
    /// 创建光源
    /// </summary>
    void CreateLight(GameObject parent)
    {
        GameObject lightObj = new GameObject("LanternLight");
        lightObj.transform.SetParent(parent.transform);
        lightObj.transform.localPosition = Vector3.zero;

        Light light = lightObj.AddComponent<Light>();
        light.type = LightType.Point;
        light.color = new Color(1f, 0.8f, 0.6f);
        light.intensity = 2f;
        light.range = 10f;
        light.shadows = LightShadows.Soft;
    }

    /// <summary>
    /// 添加必要的组件
    /// </summary>
    void AddComponents(GameObject obj)
    {
        // 添加PrayerLantern脚本
        PrayerLantern lanternScript = obj.AddComponent<PrayerLantern>();

        // 自动关联组件引用
        lanternScript.lanternLight = obj.GetComponentInChildren<Light>();
        lanternScript.fireParticle = obj.GetComponentInChildren<ParticleSystem>();
        lanternScript.lanternRenderer = obj.GetComponentInChildren<MeshRenderer>();

        // 添加Rigidbody（用于物理效果，可选）
        Rigidbody rb = obj.AddComponent<Rigidbody>();
        rb.useGravity = false;
        rb.isKinematic = true;

        // 添加BoxCollider用于交互
        BoxCollider boxCollider = obj.AddComponent<BoxCollider>();
        boxCollider.size = new Vector3(1.2f, 1.2f, 1.2f);
        boxCollider.isTrigger = true;
    }

    /// <summary>
    /// 创建灯笼材质
    /// </summary>
    Material CreateLanternMaterial()
    {
        Material mat = new Material(Shader.Find("Standard"));

        // 设置半透明效果
        mat.SetFloat("_Mode", 3); // Transparent mode
        mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        mat.SetInt("_ZWrite", 0);
        mat.DisableKeyword("_ALPHATEST_ON");
        mat.EnableKeyword("_ALPHABLEND_ON");
        mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        mat.renderQueue = 3000;

        // 颜色
        mat.color = new Color(1f, 0.9f, 0.7f, 0.8f);

        // 发光
        mat.SetColor("_EmissionColor", new Color(0.3f, 0.2f, 0.1f));

        return mat;
    }

    /// <summary>
    /// 应用材质到灯笼主体
    /// </summary>
    void ApplyMaterial(GameObject obj, Material material)
    {
        MeshRenderer[] renderers = obj.GetComponentsInChildren<MeshRenderer>();
        foreach (Renderer renderer in renderers)
        {
            if (renderer.gameObject.name == "LanternBody")
            {
                renderer.sharedMaterial = material;
            }
        }
    }

    /// <summary>
    /// 保存为预制体
    /// </summary>
    void SaveAsPrefab(GameObject obj)
    {
        // 确保文件夹存在
        string prefabPath = "Assets/Prefabs";
        if (!Directory.Exists(prefabPath))
        {
            Directory.CreateDirectory(prefabPath);
        }

        string materialPath = "Assets/Materials";
        if (!Directory.Exists(materialPath))
        {
            Directory.CreateDirectory(materialPath);
        }

        // 保存材质
        if (lanternMaterial != null && !AssetDatabase.Contains(lanternMaterial))
        {
            string matPath = $"{materialPath}/PrayerLantern.mat";
            AssetDatabase.CreateAsset(lanternMaterial, matPath);
            Debug.Log($"材质已保存到: {matPath}");
        }

        // 保存预制体
        string finalPath = $"{prefabPath}/PrayerLantern.prefab";

        // 如果已存在，先删除
        if (AssetDatabase.LoadAssetAtPath<GameObject>(finalPath) != null)
        {
            if (EditorUtility.DisplayDialog("覆盖预制体",
                $"预制体 {finalPath} 已存在，是否覆盖？", "覆盖", "取消"))
            {
                AssetDatabase.DeleteAsset(finalPath);
            }
            else
            {
                Debug.Log("取消保存预制体");
                return;
            }
        }

        PrefabUtility.SaveAsPrefabAsset(obj, finalPath);
        Debug.Log($"祈福灯预制体已保存到: {finalPath}");

        // 选中预制体
        Selection.activeObject = AssetDatabase.LoadAssetAtPath<GameObject>(finalPath);

        // 自动关联到Manager
        AutoAssignToManager(finalPath);
    }

    /// <summary>
    /// 自动关联到PrayerLanternManager
    /// </summary>
    void AutoAssignToManager(string prefabPath)
    {
        PrayerLanternManager manager = FindObjectOfType<PrayerLanternManager>();
        if (manager != null)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab != null)
            {
                manager.lanternPrefab = prefab;

                // 标记为已修改
                EditorUtility.SetDirty(manager);

                Debug.Log("已自动将预制体关联到 PrayerLanternManager");
            }
        }
        else
        {
            Debug.LogWarning("未找到 PrayerLanternManager，请手动关联预制体");
        }
    }
}
