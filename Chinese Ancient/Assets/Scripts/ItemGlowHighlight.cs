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

        float sineWave = (Mathf.Sin(Time.time * pulseSpeed) + 1f) / 2f;

        // 插值计算当前发光强度
        currentIntensity = Mathf.Lerp(minIntensity, maxIntensity, sineWave);

        if (originalMaterial != null)
        {
            // Unity HDR 颜色 = 颜色 * 强度
            Color finalColor = glowColor * currentIntensity;
            originalMaterial.SetColor("_EmissionColor", finalColor);
        }

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
        if (originalMaterial != null)
        {
            Destroy(originalMaterial);
        }
    }
}
