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
    public Color lightColor = new Color(1f, 0.4f, 0.2f); 
    public float lightRange = 8f; // 照亮范围稍微扩大
    public float lightIntensity = 20.0f; // 大幅提高基础光照强度。
    public Vector3 lightOffset = new Vector3(0f, -0.2f, -0.6f);
    [Header("NPC事件配置")]
    public float lightUpDelay = 3.0f;
    [Tooltip("当灯笼成功点亮后触发的事件（可拖拽小微的对话触发器到这里，调用它的 SetConditionMet 方法）")]
    public UnityEngine.Events.UnityEvent onLanternsLit = new UnityEngine.Events.UnityEvent();
    // 是否已经被点亮，防止重复触发
    private bool isLit = false;
    // 保留老版射线交互用于兼容测试
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
        //如果拖拽了父节点，遍历父节点下的灯笼
        if (lanternsParent != null)
        {
            foreach (Transform lantern in lanternsParent)
            {
                ApplyLanternMaterial(lantern.gameObject);
            }
        }
        //如果通过Tag查找
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
        // 触发"灯笼点亮"事件，通知外界
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
                
                if (unlitMaterial != null && mats[i] != null && mats[i].name.Contains(unlitMaterial.name))
                {
                    mats[i] = litMaterial;
                    materialReplaced = true;
                }
            }
            // 如果Inspector没填 unlitMaterial 或者没匹配上，退回老办法
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
                fakeLightObj.transform.position = renderer.bounds.center + lanternObj.transform.TransformDirection(lightOffset);
                fakeLightObj.transform.SetParent(lanternObj.transform);
                Light pLight = fakeLightObj.AddComponent<Light>();
                pLight.type = LightType.Point;
                pLight.color = lightColor;
                pLight.range = lightRange;
                pLight.intensity = lightIntensity;
                // 性能杀手开关：绝对不要开阴影
                pLight.shadows = LightShadows.None;
                // 改回Auto逐像素渲染，因为古建筑墙面往往是整块的大模型底面数网格，强制顶点照明会导致墙砖无法受光
                pLight.renderMode = LightRenderMode.Auto;
            }
        }
    }
}
