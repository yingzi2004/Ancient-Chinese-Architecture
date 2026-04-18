using UnityEngine;

public class LanternLighter : MonoBehaviour, IInteractable
{
    [Header("灯笼组配置")]
    [Tooltip("【方式一】存放所有大院红灯笼的父节点（如果有的话拖进来）")]
    public Transform lanternsParent;

    [Tooltip("【方式二】如果散落在场景各处不能移动，填入它们的 Tag 标签名称")]
    public string lanternTag = "Lantern";

    [Tooltip("灯笼亮起时替换的发光材质 (带有 Emission 属性)")]
    public Material litMaterial;

    [Tooltip("原本的普通材质 (可选，用于开发测试还原用)")]
    public Material unlitMaterial;

    [Header("火折子拾取逻辑")]
    [Tooltip("火折子模型是否在拾取后隐藏或销毁")]
    public bool destroyOnPickup = true;
    
    [Header("性能优化照明配置")]
    [Tooltip("是否在灯笼亮起时，动态生成普通光源照亮墙面")]
    public bool addOptimizedLight = true;
    public Color lightColor = new Color(1f, 0.4f, 0.2f); // 暖红偏橙色
    public float lightRange = 8f; // 照亮范围稍微扩大
    public float lightIntensity = 3.0f; // 提高基础光照强度供观感测试

    // 是否已经被点亮，防止重复触发
    private bool isLit = false;

    // 实现了您游戏内的 IInteractable 接口，用于准星射线交互！
    public void Interact()
    {
        if (!isLit)
        {
            TurnOnAllLanterns();
            
            // 拾取后发光火折子消失
            if (destroyOnPickup)
            {
                gameObject.SetActive(false); 
            }
        }
    }

    /// <summary>
    /// 被触发或者被 NPC 任务系统调用时，执行全体灯笼材质替换
    /// </summary>
    public void TurnOnAllLanterns()
    {
        if (litMaterial == null) return;

        // 方式一：如果拖拽了父节点，遍历父节点下的灯笼
        if (lanternsParent != null)
        {
            foreach (Transform lantern in lanternsParent)
            {
                ApplyLanternMaterial(lantern.gameObject);
            }
        }

        // 方式二：如果通过 Tag 查找（专门对付无法移动的预制体子级）
        if (!string.IsNullOrEmpty(lanternTag))
        {
            GameObject[] taggedLanterns = GameObject.FindGameObjectsWithTag(lanternTag);
            foreach (GameObject lantern in taggedLanterns)
            {
                ApplyLanternMaterial(lantern);
            }
        }

        Debug.Log("大院掌灯完成：所有灯笼已成功替换为自发光材质！");
        isLit = true; 
    }

    // 核心换材质的方法封装提炼
    private void ApplyLanternMaterial(GameObject lanternObj)
    {
        MeshRenderer renderer = lanternObj.GetComponent<MeshRenderer>();
        
        if (renderer != null)
        {
            // 复用材质，节省开销
            renderer.sharedMaterial = litMaterial;

            // 同步点亮内嵌的辅助点光源
            Light[] partialLights = lanternObj.GetComponentsInChildren<Light>(true);
            if (partialLights.Length > 0)
            {
                foreach (Light light in partialLights)
                {
                    light.enabled = true;
                }
            }
            else if (addOptimizedLight)
            {
                // 如果模型原本没有内嵌的点光源，则动态生成一个用来打亮周围环境的光源
                GameObject fakeLightObj = new GameObject("FakeOptimizedLight");
                fakeLightObj.transform.SetParent(lanternObj.transform);
                // 稍微往外或者往下偏移一点，防止产生的光被灯笼自己的模型完全吃掉
                fakeLightObj.transform.localPosition = new Vector3(0, -0.5f, 0);

                Light pLight = fakeLightObj.AddComponent<Light>();
                pLight.type = LightType.Point;
                pLight.color = lightColor;
                pLight.range = lightRange;
                pLight.intensity = lightIntensity;
                
                // 性能杀手开关：绝对不要开阴影！(只要不开产生阴影，URP处理几十个点光源也是小菜一碟)
                pLight.shadows = LightShadows.None; 
                // 改回Auto逐像素渲染，因为古建筑墙面往往是整块的大模型底面数网格，强制顶点照明会导致墙砖无法受光
                pLight.renderMode = LightRenderMode.Auto; 
            }
        }
    }
}
