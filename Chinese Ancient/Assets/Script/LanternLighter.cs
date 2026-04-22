using UnityEngine;
public class LanternLighter : MonoBehaviour, IInteractable
{
    [Header("灯笼组配置")]
    public Transform lanternsParent;
    public string lanternTag = "Lantern";
    public Material litMaterial;
    public Material unlitMaterial;
    [Header("火折子拾取逻辑")]
    public bool destroyOnPickup = true;
    [Header("性能优化照明配置")]
    public bool addOptimizedLight = true;
    public Color lightColor = new Color(1f, 0.4f, 0.2f); // 暖红偏橙色
    public float lightRange = 8f; // 照亮范围稍微扩大
    public float lightIntensity = 20.0f; // 大幅提高基础光照强度。在URP中如果开启了物理光照单位，5可能等于没亮，需要20甚至几百。
    public Vector3 lightOffset = new Vector3(0f, -0.2f, -0.6f);
    [Header("NPC事件配置")]
    public float lightUpDelay = 3.0f;
    [Tooltip("当灯笼成功点亮后触发的事件（可拖拽小微的对话触发器到这里，调用它的 SetConditionMet 方法）")]
    public UnityEngine.Events.UnityEvent onLanternsLit = new UnityEngine.Events.UnityEvent();
    // 是否已经被点亮，防止重复触发
    private bool isLit = false;
    // 保留老版射线交互用于兼容测试（可直接点亮）
    public void Interact()
    {
        if (!isLit)
        {
            TurnOnAllLanterns();
            if (destroyOnPickup) gameObject.SetActive(false);
        }
    }
    public void StartLightingSequence()
    {
        if (!isLit)
        {
            // 启动倒计时协程
            StartCoroutine(WaitAndLightUp(lightUpDelay));
        }
    }
    private System.Collections.IEnumerator WaitAndLightUp(float delay)
    {
        Debug.Log($"[剧情] 大伯拿到火折子，开始掌灯...等待 {delay} 秒...");
        yield return new WaitForSeconds(delay);
        TurnOnAllLanterns();
    }
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
        // 触发"灯笼点亮"事件，通知外界（比如小微的对话框触发器）
        if (onLanternsLit != null)
        {
            onLanternsLit.Invoke();
            Debug.Log("[剧情] 已向小微触发感叹的前置解锁信号。");
        }
    }
    // 核心换材质的方法封装提炼
    private void ApplyLanternMaterial(GameObject lanternObj)
    {
        MeshRenderer renderer = lanternObj.GetComponent<MeshRenderer>();
        if (renderer != null)
        {
            // 解决四个灯笼合并模型的"多材质乱序"问题：
            Material[] mats = renderer.sharedMaterials;
            bool materialReplaced = false;
            for (int i = 0; i < mats.Length; i++)
            {
                // 如果你在面板赋了原本的"纸罩材质(unlitMaterial)"，就精确定位纸罩所在的材质槽位进行替换
                // 使用 Contains 是为了防止运行时 Unity 自动给材质名加上 " (Instance)" 后缀导致匹配失败
                if (unlitMaterial != null && mats[i] != null && mats[i].name.Contains(unlitMaterial.name))
                {
                    mats[i] = litMaterial;
                    materialReplaced = true;
                }
            }
            // 如果Inspector没填 unlitMaterial 或者没匹配上，退回老办法（强制替换第0个元素）
            if (!materialReplaced && mats.Length > 0)
            {
                mats[0] = litMaterial;
            }
            // 将修改后的材质数组重新赋值回去
            renderer.sharedMaterials = mats;
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
                // 重点修复：购买的模型轴心(Pivot)通常在固定墙体的木架子上（深深插在墙里）
                // 所以绝对不能按 localPosition 设置为0，必须基于实际发光网格的中心位置往下、往外偏移！
                fakeLightObj.transform.position = renderer.bounds.center + lanternObj.transform.TransformDirection(lightOffset);
                fakeLightObj.transform.SetParent(lanternObj.transform);
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
