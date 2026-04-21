using UnityEngine;

public class ItemGlowHighlight : MonoBehaviour
{
    [Header("发光设置")]
    public Color glowColor = new Color(1f, 0.6f, 0.2f);
    
    public float pulseSpeed = 1.5f;

    public float minIntensity = 0.2f;
    public float maxIntensity = 1.2f;

    [Header("光源设置 (可选)")]
    public bool addPointLight = true;
    public float lightRange = 2f;

    private Material originalMaterial;
    private Light pointLight;
    private float currentIntensity;

    void Start()
    {
        // 1. 获取自身的材质球，开启自发光宏
        Renderer rend = GetComponent<Renderer>();
        if (rend != null)
        {
            // 实例化材质，防止修改影响其他公用同材质的物体
            originalMaterial = rend.material;
            originalMaterial.EnableKeyword("_EMISSION");
            Debug.Log($"[{gameObject.name}] 已开启材质自发光");
        }
        else
        {
            Debug.LogWarning($"[{gameObject.name}] 没有找到Renderer组件，无法修改材质发光。");
        }

        // 2. 添加实际的物理光源（点光源），增加环境真实感
        if (addPointLight)
        {
            pointLight = gameObject.AddComponent<Light>();
            pointLight.type = LightType.Point;
            pointLight.color = glowColor;
            pointLight.range = lightRange;
            pointLight.intensity = minIntensity;
            // 提高渲染渲染优先级，防止光照被剔除
            pointLight.renderMode = LightRenderMode.ForcePixel; 
            Debug.Log($"[{gameObject.name}] 已添加辅助点光源");
        }
    }

    void Update()
    {
        // 使用正弦波函数 (Sine) 计算呼吸节奏，在极值之间平滑过渡
        // Mathf.Sin 返回 -1 到 1，将其映射到 0 到 1
        float sineWave = (Mathf.Sin(Time.time * pulseSpeed) + 1f) / 2f; 
        
        // 插值计算当前发光强度
        currentIntensity = Mathf.Lerp(minIntensity, maxIntensity, sineWave);

        // 1. 更新材质发光颜色
        if (originalMaterial != null)
        {
            // Unity HDR 颜色 = 颜色 * 强度
            Color finalColor = glowColor * currentIntensity;
            originalMaterial.SetColor("_EmissionColor", finalColor);
        }

        // 2. 更新灯光强度
        if (pointLight != null)
        {
            pointLight.intensity = currentIntensity;
        }
    }

    public void DisableGlow()
    {
        if (originalMaterial != null)
        {
            originalMaterial.SetColor("_EmissionColor", Color.black);
            originalMaterial.DisableKeyword("_EMISSION");
        }

        if (pointLight != null)
        {
            pointLight.enabled = false;
        }
        
        // 关闭脚本的Update更新
        this.enabled = false;
    }

    private void OnDestroy()
    {
        // 销毁时清理临时实例化的材质球，防止内存泄漏
        if (originalMaterial != null)
        {
            Destroy(originalMaterial);
        }
    }
}
