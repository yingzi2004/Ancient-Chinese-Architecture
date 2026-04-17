using UnityEngine;

/// <summary>
/// 香炉烟雾粒子效果控制器
/// 自动为香炉添加逼真的烟雾粒子效果
/// </summary>
[RequireComponent(typeof(ParticleSystem))]
public class IncenseSmokeEffect : MonoBehaviour
{
    [Header("--- 烟雾效果设置 ---")]
    [Tooltip("烟雾颜色")]
    public Color smokeColor = new Color(0.8f, 0.8f, 0.8f, 0.3f);

    [Tooltip("烟雾粒子数量")]
    [Range(50, 500)]
    public int maxParticles = 200;

    [Tooltip("烟雾上升速度")]
    [Range(0.1f, 3f)]
    public float riseSpeed = 0.3f;

    [Tooltip("烟雾扩散范围")]
    [Range(0.1f, 2f)]
    public float spreadRange = 0.5f;

    [Tooltip("烟雾最小尺寸")]
    [Range(0.1f, 2f)]
    public float minSize = 0.3f;

    [Tooltip("烟雾最大尺寸")]
    [Range(0.5f, 5f)]
    public float maxSize = 2.0f;

    [Tooltip("烟雾生命周期（秒）")]
    [Range(5f, 15f)]
    public float lifetime = 10f;

    [Tooltip("烟雾发射速率")]
    [Range(10, 100)]
    public int emissionRate = 30;

    [Tooltip("是否启用风力影响")]
    public bool enableWind = false;

    [Tooltip("风力方向")]
    public Vector3 windDirection = new Vector3(0.5f, 0, 0.5f);

    [Tooltip("风力强度")]
    [Range(0.1f, 2f)]
    public float windStrength = 0.3f;

    [Tooltip("是否在Start时自动播放")]
    public bool playOnAwake = true;

    [Tooltip("烟雾发射点偏移（相对于香炉中心）")]
    public Vector3 emissionOffset = Vector3.zero;

    [Header("--- 材质设置 ---")]
    [Tooltip("自定义烟雾材质（留空则使用默认粒子材质）")]
    public Material customSmokeMaterial;

    private ParticleSystem particleSystem;
    private ParticleSystem.MainModule mainModule;
    private ParticleSystem.EmissionModule emissionModule;
    private ParticleSystem.ShapeModule shapeModule;
    private ParticleSystem.VelocityOverLifetimeModule velocityModule;
    private ParticleSystem.ColorOverLifetimeModule colorModule;
    private ParticleSystem.SizeOverLifetimeModule sizeModule;
    private ParticleSystemRenderer particleRenderer;

    private void Awake()
    {
        particleSystem = GetComponent<ParticleSystem>();

        // 配置粒子系统
        ConfigureParticleSystem();
    }

    private void Start()
    {
        if (playOnAwake)
        {
            PlaySmoke();
        }
    }

    /// <summary>
    /// 配置粒子系统参数
    /// </summary>
    private void ConfigureParticleSystem()
    {
        // 获取各个模块
        mainModule = particleSystem.main;
        emissionModule = particleSystem.emission;
        shapeModule = particleSystem.shape;
        velocityModule = particleSystem.velocityOverLifetime;
        colorModule = particleSystem.colorOverLifetime;
        sizeModule = particleSystem.sizeOverLifetime;

        // === 主模块设置 ===
        mainModule.maxParticles = maxParticles;
        mainModule.startLifetime = lifetime;
        mainModule.startSpeed = riseSpeed;
        mainModule.startSize = new ParticleSystem.MinMaxCurve(minSize, maxSize);
        mainModule.startColor = smokeColor;
        mainModule.playOnAwake = playOnAwake;
        mainModule.loop = true;
        mainModule.prewarm = true;

        // 设置重力修饰符（负值让烟雾向上飘浮）
        var gravityModifier = -0.05f; // 负重力，让烟雾向上而不是向下
        mainModule.gravityModifier = gravityModifier;

        // === 发射模块设置 ===
        emissionModule.rateOverTime = emissionRate;

        // === 形状模块设置 ===
        shapeModule.shapeType = ParticleSystemShapeType.Cone;
        shapeModule.angle = 5f; // 窄锥形
        shapeModule.radius = 0.05f;
        shapeModule.length = 0.2f;

        // === 速度模块设置 ===
        if (enableWind)
        {
            velocityModule.enabled = true;
            velocityModule.space = ParticleSystemSimulationSpace.World;

            // 添加风力
            ParticleSystem.MinMaxCurve curveX = new ParticleSystem.MinMaxCurve();
            curveX.mode = ParticleSystemCurveMode.Constant;
            curveX.constant = windDirection.x * windStrength;
            velocityModule.x = curveX;

            ParticleSystem.MinMaxCurve curveZ = new ParticleSystem.MinMaxCurve();
            curveZ.mode = ParticleSystemCurveMode.Constant;
            curveZ.constant = windDirection.z * windStrength;
            velocityModule.z = curveZ;
        }
        else
        {
            velocityModule.enabled = false;
        }

        // === 颜色模块设置 ===
        colorModule.enabled = true;

        // 渐变透明度：从半透明到完全透明
        Gradient alphaGradient = new Gradient();
        alphaGradient.SetKeys(
            new GradientColorKey[] {
                new GradientColorKey(smokeColor, 0f),
                new GradientColorKey(smokeColor, 1f)
            },
            new GradientAlphaKey[] {
                new GradientAlphaKey(smokeColor.a * 0.8f, 0f),  // 开始时较浓
                new GradientAlphaKey(smokeColor.a * 0.4f, 0.3f), // 中等浓度
                new GradientAlphaKey(smokeColor.a * 0.1f, 0.7f), // 较淡
                new GradientAlphaKey(0f, 1f)                      // 完全消失
            }
        );
        colorModule.color = new ParticleSystem.MinMaxGradient(alphaGradient);

        // === 尺寸模块设置 ===
        sizeModule.enabled = true;

        // 烟雾逐渐变大
        AnimationCurve sizeCurve = new AnimationCurve();
        sizeCurve.AddKey(0f, 0.3f);    // 开始较小
        sizeCurve.AddKey(0.5f, 0.7f);  // 中间变大
        sizeCurve.AddKey(1f, 1.2f);    // 结束最大

        sizeModule.size = new ParticleSystem.MinMaxCurve(1f, sizeCurve);

        // === 旋转模块设置 ===
        var rotationModule = particleSystem.rotationOverLifetime;
        rotationModule.enabled = true;
        rotationModule.separateAxes = false;

        // 缓慢旋转
        ParticleSystem.MinMaxCurve rotationCurve = new ParticleSystem.MinMaxCurve();
        rotationCurve.mode = ParticleSystemCurveMode.Constant;
        rotationCurve.constant = 30f; // 每秒旋转30度
        rotationModule.z = rotationCurve;

        // === 渲染器设置 ===
        particleRenderer = particleSystem.GetComponent<ParticleSystemRenderer>();
        if (particleRenderer != null)
        {
            particleRenderer.renderMode = ParticleSystemRenderMode.Billboard;
            particleRenderer.sortingOrder = 1;
            particleRenderer.alignment = ParticleSystemRenderSpace.View;

            // 设置材质
            if (customSmokeMaterial != null)
            {
                // 使用自定义材质
                particleRenderer.material = customSmokeMaterial;
                Debug.Log("[香炉烟雾] 使用自定义烟雾材质");
            }
            else
            {
                // 尝试使用Unity内置的粒子着色器
                Shader particleShader = Shader.Find("Particles/Standard Unlit");
                if (particleShader != null)
                {
                    Material defaultMaterial = new Material(particleShader);
                    defaultMaterial.color = Color.white;
                    defaultMaterial.SetFloat("_Mode", 3); // Transparent
                    defaultMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                    defaultMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                    defaultMaterial.SetInt("_ZWrite", 0);
                    defaultMaterial.DisableKeyword("_ALPHATEST_ON");
                    defaultMaterial.EnableKeyword("_ALPHABLEND_ON");
                    defaultMaterial.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                    defaultMaterial.renderQueue = 3000;
                    defaultMaterial.EnableKeyword("_FADING_ON");
                    defaultMaterial.SetFloat("_FlipbookMode", 0);
                    defaultMaterial.SetFloat("_Glossiness", 0);
                    defaultMaterial.SetFloat("_SmoothnessTextureChannel", 0);
                    defaultMaterial.SetFloat("_Metallic", 0);

                    particleRenderer.material = defaultMaterial;
                    Debug.Log("[香炉烟雾] 使用Particles/Standard Unlit着色器");
                }
                else
                {
                    // 备用方案：使用Legacy着色器
                    Shader legacyShader = Shader.Find("Legacy Shaders/Particles/Alpha Blended");
                    if (legacyShader != null)
                    {
                        Material legacyMaterial = new Material(legacyShader);
                        particleRenderer.material = legacyMaterial;
                        Debug.Log("[香炉烟雾] 使用Legacy Alpha Blended着色器");
                    }
                    else
                    {
                        Debug.LogWarning("[香炉烟雾] 未找到合适的粒子着色器，烟雾可能显示异常！");
                    }
                }
            }

            // 禁用光照影响，让烟雾自发光
            particleRenderer.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
            particleRenderer.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;
            particleRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            particleRenderer.receiveShadows = false;
        }

        Debug.Log($"[香炉烟雾] 已配置烟雾效果 - 最大粒子数: {maxParticles}, 发射速率: {emissionRate}");
    }

    /// <summary>
    /// 播放烟雾效果
    /// </summary>
    public void PlaySmoke()
    {
        particleSystem.Play();
        Debug.Log("[香炉烟雾] 烟雾效果已启动");
    }

    /// <summary>
    /// 停止烟雾效果
    /// </summary>
    public void StopSmoke()
    {
        particleSystem.Stop();
        Debug.Log("[香炉烟雾] 烟雾效果已停止");
    }

    /// <summary>
    /// 暂停烟雾效果
    /// </summary>
    public void PauseSmoke()
    {
        particleSystem.Pause();
        Debug.Log("[香炉烟雾] 烟雾效果已暂停");
    }

    /// <summary>
    /// 动态调整烟雾浓度
    /// </summary>
    public void SetSmokeIntensity(float intensity)
    {
        intensity = Mathf.Clamp01(intensity);
        mainModule = particleSystem.main;
        emissionModule = particleSystem.emission;

        // 调整发射速率
        emissionModule.rateOverTime = emissionRate * intensity;

        // 调整最大粒子数
        mainModule.maxParticles = Mathf.RoundToInt(maxParticles * intensity);

        Debug.Log($"[香炉烟雾] 烟雾浓度已调整至: {intensity * 100}%");
    }

    /// <summary>
    /// 在运行时更改烟雾颜色
    /// </summary>
    public void SetSmokeColor(Color newColor)
    {
        mainModule = particleSystem.main;
        mainModule.startColor = newColor;
        smokeColor = newColor;

        // 更新颜色渐变
        colorModule.enabled = true;
        Gradient alphaGradient = new Gradient();
        alphaGradient.SetKeys(
            new GradientColorKey[] {
                new GradientColorKey(newColor, 0f),
                new GradientColorKey(newColor, 1f)
            },
            new GradientAlphaKey[] {
                new GradientAlphaKey(newColor.a * 0.8f, 0f),
                new GradientAlphaKey(newColor.a * 0.4f, 0.3f),
                new GradientAlphaKey(newColor.a * 0.1f, 0.7f),
                new GradientAlphaKey(0f, 1f)
            }
        );
        colorModule.color = new ParticleSystem.MinMaxGradient(alphaGradient);

        Debug.Log($"[香炉烟雾] 烟雾颜色已更改");
    }

    /// <summary>
    /// 启用/禁用风力效果
    /// </summary>
    public void SetWindEnabled(bool enabled)
    {
        enableWind = enabled;
        ConfigureParticleSystem();
        Debug.Log($"[香炉烟雾] 风力效果已{(enabled ? "启用" : "禁用")}");
    }

    private void OnDrawGizmosSelected()
    {
        // 在Scene视图中绘制烟雾发射区域
        Gizmos.color = new Color(0.5f, 0.8f, 1f, 0.3f);
        Gizmos.DrawWireSphere(transform.position + emissionOffset, 0.1f);

        // 绘制风力方向
        if (enableWind)
        {
            Gizmos.color = Color.green;
            Vector3 windEnd = transform.position + windDirection.normalized * 2f;
            Gizmos.DrawLine(transform.position, windEnd);
            Gizmos.DrawWireSphere(windEnd, 0.1f);
        }
    }

#if UNITY_EDITOR
    /// <summary>
    /// 编辑器快捷方法：通过菜单添加烟雾效果到选中的GameObject
    /// </summary>
    [UnityEditor.MenuItem("GameObject/Effects/添加香炉烟雾效果")]
    private static void AddSmokeEffectToSelected()
    {
        if (UnityEditor.Selection.activeGameObject != null)
        {
            var selected = UnityEditor.Selection.activeGameObject;

            // 检查是否已有粒子系统
            var existingParticle = selected.GetComponent<ParticleSystem>();
            if (existingParticle == null)
            {
                existingParticle = selected.AddComponent<ParticleSystem>();
            }

            // 添加烟雾脚本
            var smokeEffect = selected.GetComponent<IncenseSmokeEffect>();
            if (smokeEffect == null)
            {
                smokeEffect = selected.AddComponent<IncenseSmokeEffect>();
            }

            Debug.Log($"[香炉烟雾] 已为 '{selected.name}' 添加烟雾效果");
        }
        else
        {
            Debug.LogWarning("[香炉烟雾] 请先选择一个GameObject");
        }
    }
#endif
}
