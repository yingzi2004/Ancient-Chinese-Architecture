using System.Collections;
using UnityEngine;

public class IncenseInteraction : MonoBehaviour
{
    [Header("烧香动画设置")]
    [Tooltip("每次拜的旋转角度（根据你的香模型朝向，X、Y或者Z轴可能需要调整）")]
    public Vector3 bowAngle = new Vector3(30f, 0f, 0f);
    
    [Tooltip("每次拜的持续时间")]
    public float bowDuration = 1.0f;
    
    [Header("香炉目标位置")]
    [Tooltip("请在香炉物体下新建一个空物体作为插香的位置，并拖拽到这里")]
    public Transform incenseBurnerTarget;
    
    [Tooltip("香飞往香炉的持续时间")]
    public float flyDuration = 1.5f;

    [Header("音效设置")]
    [Tooltip("用来播放声音的组件（可选，不填将尝试自动获取）")]
    public AudioSource audioSource;
    [Tooltip("点击拜时的声音（此处放入“编钟声”）")]
    public AudioClip bianzhongSound;
    [Tooltip("完成插香时的声音（此处放入“钟声”）")]
    public AudioClip finishBellSound;

    [Header("特效设置")]
    [Tooltip("香头的位置（用来定烟飘出的起点。请在香底下建个空物体移到顶部并拖到这，不填默认位置）")]
    public Transform incenseTip;
    
    [Tooltip("烟雾的材质（解决粉块问题，请参考聊天框里的步骤创建一个材质并拖入这里）")]
    public Material smokeMaterial;

    [Header("联动与场景表现")]
    [Tooltip("如果需要香插完后双手合十拜三次，请将挂载了 PlayerHandsAnim 脚本的相机拖拽到这里")]
    public PlayerHandsAnim playerHandsAnim;

    private int currentClicks = 0;           // 当前点击次数
    private bool isAnimating = false;        // 防止动画过程中的连击
    private Quaternion originalRotation;     // 记录香一开始的旋转

    void Start()
    {
        // 记录香最初的旋转状态
        originalRotation = transform.rotation;
        
        // 自动获取AudioSource
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
        }
    }

    /// <summary>
    /// 当玩家准心对准香并按下点击键时，你的射线脚本可以调用这个方法。
    /// 同时，我也保留了OnMouseDown以方便你在不启动第一人称情况下的鼠标右键测试。
    /// </summary>
    public void Interact()
    {
        // 如果正在播放动画，或者香已经插好了（点击达到3次），就不再响应新点击
        if (isAnimating || currentClicks >= 3) return;

        currentClicks++;
        Debug.Log($"您已拜了第 {currentClicks} 次！");

        // 每次拜的时候播放编钟声
        if (audioSource != null && bianzhongSound != null)
        {
            audioSource.PlayOneShot(bianzhongSound);
        }

        // 根据点击次数播放不同动画
        if (currentClicks < 3)
        {
            // 第1、2次只拜
            StartCoroutine(BowAnimationRoutine());
        }
        else
        {
            // 第3次拜完直接飞入香炉
            StartCoroutine(FinalBowAndFlyRoutine());
        }
    }

    // 为了方便你在普通测试模式下直接用鼠标点击“香”也能触发
    private void OnMouseDown()
    {
        Interact();
    }

    // 拜一次的动画协程
    private IEnumerator BowAnimationRoutine()
    {
        isAnimating = true;

        // 计算往前探的旋转角度（通常是绕X轴旋转，如果模型倒了可以改成-30或者调整bowAngle参数）
        Quaternion forwardRotation = originalRotation * Quaternion.Euler(bowAngle);
        float halfDuration = bowDuration / 2f;

        // 1. 往前拜
        float elapsedTime = 0f;
        while (elapsedTime < halfDuration)
        {
            transform.rotation = Quaternion.Slerp(originalRotation, forwardRotation, elapsedTime / halfDuration);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        // 2. 往回直立
        elapsedTime = 0f;
        while (elapsedTime < halfDuration)
        {
            transform.rotation = Quaternion.Slerp(forwardRotation, originalRotation, elapsedTime / halfDuration);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        // 确保最终回正
        transform.rotation = originalRotation;
        isAnimating = false;
    }

    // 第3次拜并飞入香炉的动画协程
    private IEnumerator FinalBowAndFlyRoutine()
    {
        isAnimating = true;

        // 先执行最后一次“拜”
        Quaternion forwardRotation = originalRotation * Quaternion.Euler(bowAngle);
        float halfDuration = bowDuration / 2f;
        
        float elapsedTime = 0f;
        while (elapsedTime < halfDuration)
        {
            transform.rotation = Quaternion.Slerp(originalRotation, forwardRotation, elapsedTime / halfDuration);
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        
        elapsedTime = 0f;
        while (elapsedTime < halfDuration)
        {
            transform.rotation = Quaternion.Slerp(forwardRotation, originalRotation, elapsedTime / halfDuration);
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        transform.rotation = originalRotation;

        // 如果没有配置目标点，结束并在控制台提醒
        if (incenseBurnerTarget == null)
        {
            Debug.LogError("没有设置香炉目标点！无法飞入香炉。请在Inspector中设置 incenseBurnerTarget。");
            isAnimating = false;
            yield break;
        }

        Debug.Log("礼成！香开始飞入香炉。");

        // 接着飞入香炉
        Vector3 startPos = transform.position;
        Quaternion startRot = transform.rotation;
        elapsedTime = 0f;

        while (elapsedTime < flyDuration)
        {
            float t = elapsedTime / flyDuration;
            // 加入平滑插值（缓入缓出）让飞行动作更自然
            float smoothStep = t * t * (3f - 2f * t);

            transform.position = Vector3.Lerp(startPos, incenseBurnerTarget.position, smoothStep);
            transform.rotation = Quaternion.Slerp(startRot, incenseBurnerTarget.rotation, smoothStep);

            elapsedTime += Time.deltaTime;
            yield return null;
        }

        // 飞出后精确固定在香炉位置
        transform.position = incenseBurnerTarget.position;
        transform.rotation = incenseBurnerTarget.rotation;
        
        // 可选：插进去后可以让香成为香炉的子物体，这样移动香炉时香跟着走
        transform.parent = incenseBurnerTarget.parent;
        
        // 播放插完香后的钟声
        if (audioSource != null && finishBellSound != null)
        {
            audioSource.PlayOneShot(finishBellSound);
        }
        
        // 播放玩家双手合十动画
        if (playerHandsAnim != null)
        {
            playerHandsAnim.PlayPrayAnimation();
        }

        // --- 纯代码动态生成天坛祭天专属“青烟化星芒”螺旋上升特效 ---
        GenerateQingyanEffect();

        isAnimating = false;
    }

    /// <summary>
    /// 用代码在程序运行时动态拼装一个复杂的炫酷粒子系统
    /// </summary>
    private void GenerateQingyanEffect()
    {
        // 确定特效生成位置
        Vector3 spawnPos = incenseTip != null ? incenseTip.position : transform.position + Vector3.up * 0.3f;

        // ==========================================
        // 1. 创建主系统：青色螺旋上升的烟柱
        // ==========================================
        GameObject smokeObj = new GameObject("Qingyan_Spiral_VFX");
        smokeObj.transform.position = spawnPos;
        // 把粒子喷射方向朝上 (绕X转-90度后，Unity的局部Z轴变成世界上方)
        smokeObj.transform.rotation = Quaternion.Euler(-90, 0, 0); 
        smokeObj.transform.SetParent(transform); // 挂在香底，随着香炉可能移动

        ParticleSystem smokePS = smokeObj.AddComponent<ParticleSystem>();
        ParticleSystemRenderer smokeRenderer = smokeObj.GetComponent<ParticleSystemRenderer>();
        if (smokeMaterial != null) smokeRenderer.material = smokeMaterial;

        // 主设置 (Main)
        var main = smokePS.main;
        main.duration = 5f;
        main.loop = true;
        main.startLifetime = 3f;  // 烟能飘到半空的高度
        main.startSpeed = 0.5f;    // 基础向上抛送速度
        main.startSize = 0.15f;    // 烟泡初始粗细
        main.startColor = new Color(0.1f, 0.8f, 0.9f, 0.4f); // 祭天青色调 (半透明)
        main.simulationSpace = ParticleSystemSimulationSpace.World; // 烟与世界坐标绑定，不随香体转动
        main.gravityModifier = -0.05f; // 轻微负重力，产生向上拉的力量

        // 发射器 (Emission)
        var emission = smokePS.emission;
        emission.rateOverTime = 30f; // 烟流浓度

        // 形状 (Shape)
        var shape = smokePS.shape;
        shape.shapeType = ParticleSystemShapeType.Circle; // 使用园形让其有柱状感
        shape.radius = 0.015f; 

        // 运动轨迹：螺旋上升特效的核心 (Velocity Over Lifetime)
        var vol = smokePS.velocityOverLifetime;
        vol.enabled = true;
        vol.orbitalZ = 6f; // 绕中心的旋转角速度（形成盘旋）
        vol.orbitalX = 0f;
        vol.orbitalOffsetX = 0.04f; // 圆心偏移使盘旋范围变大
        vol.orbitalOffsetY = 0.04f;
        vol.y = 0.8f;      // 沿着垂直向上轴的爬升速度

        // 颜色消散 (Color Over Lifetime)
        var col = smokePS.colorOverLifetime;
        col.enabled = true;
        Gradient grad = new Gradient();
        grad.SetKeys(
            new GradientColorKey[] { new GradientColorKey(new Color(0.1f, 0.8f, 0.9f), 0.0f), new GradientColorKey(Color.white, 1.0f) },
            new GradientAlphaKey[] { new GradientAlphaKey(0f, 0.0f), new GradientAlphaKey(1f, 0.2f), new GradientAlphaKey(0f, 0.8f) }
        );
        col.color = grad;

        // 大小消散 (Size Over Lifetime)
        var sol = smokePS.sizeOverLifetime;
        sol.enabled = true;
        AnimationCurve sizeCurve = new AnimationCurve();
        sizeCurve.AddKey(0f, 1f); 
        sizeCurve.AddKey(1f, 0f); // 烟雾越往上越细，最终化无
        sol.size = new ParticleSystem.MinMaxCurve(1f, sizeCurve);


        // ==========================================
        // 2. 创建子系统：高处消散的星点光斑
        // ==========================================
        GameObject starObj = new GameObject("Dissipating_Stars");
        starObj.transform.SetParent(smokeObj.transform);
        // 定位在半空中（相对父物体Z轴上移大约2米的位置，也就是烟雾消失之处）
        starObj.transform.localPosition = new Vector3(0, 0, 2.0f);
        
        ParticleSystem starPS = starObj.AddComponent<ParticleSystem>();
        ParticleSystemRenderer starRenderer = starObj.GetComponent<ParticleSystemRenderer>();
        if (smokeMaterial != null) starRenderer.material = smokeMaterial;

        var starMain = starPS.main;
        starMain.startLifetime = 1.0f;
        starMain.startSpeed = 0.1f;
        starMain.startSize = new ParticleSystem.MinMaxCurve(0.02f, 0.05f); // 粒子非常小，像星光
        starMain.startColor = new Color(0.7f, 1f, 1f, 1f); // 高亮青白色
        starMain.gravityModifier = -0.05f; 

        var starEmission = starPS.emission;
        starEmission.rateOverTime = 20f; // 产生20颗/秒的散落星芒

        var starShape = starPS.shape;
        starShape.shapeType = ParticleSystemShapeType.Sphere; // 散发形
        starShape.radius = 0.4f;                              // 分布在一个球形空间化开

        var starColor = starPS.colorOverLifetime;
        starColor.enabled = true;
        Gradient starGrad = new Gradient();
        starGrad.SetKeys(
            new GradientColorKey[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(new Color(0.1f, 0.8f, 0.9f), 1f) },
            new GradientAlphaKey[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.8f), new GradientAlphaKey(0f, 1f) }
        );
        starColor.color = starGrad;

        // 星点闪烁 (借助Size Over Lifetime的波峰波谷)
        var starSize = starPS.sizeOverLifetime;
        starSize.enabled = true;
        AnimationCurve starSizeCurve = new AnimationCurve();
        starSizeCurve.AddKey(0f, 0f);
        starSizeCurve.AddKey(0.5f, 1f); // 突然亮起变大
        starSizeCurve.AddKey(1f, 0f);   // 彻底熄灭
        starSize.size = new ParticleSystem.MinMaxCurve(1f, starSizeCurve);

        // 随机乱飘感 (Noise)
        var noise = starPS.noise;
        noise.enabled = true;
        noise.strength = 0.3f; 
    }
}