using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// <summary>
/// 结尾过场特效：先模糊 -> 再黑屏 -> 再摇晃。
/// 
/// 针对 URP (Universal Render Pipeline) 进行过优化。
/// 模糊效果现在使用内置的 Depth Of Field，不需要再手动挂载额外的 Shader 或组件。
/// </summary>
public class EndSequenceVFX : MonoBehaviour
{
    public static EndSequenceVFX Instance { get; private set; }

    [Header("目标")]
    public Camera targetCamera;

    [Header("阶段 0：初始等待")]
    [Tooltip("对话结束后，等待多久才开始产生异样（不马上触发）")]
    [Range(0f, 5f)]
    public float initialDelay = 1.0f;

    [Header("阶段 1：视角模糊")]
    [Tooltip("模糊目标强度（URP Depth Of Field 推荐范围 0~2.5）")]
    [Range(0f, 2.5f)]
    public float maxBlur = 1.5f;

    [Tooltip("视线变模糊需要的时间")]
    [Range(0f, 8f)]
    public float blurInDuration = 2.0f;

    [Tooltip("模糊开始多久之后，才开始发生剧烈摇晃")]
    [Range(0f, 5f)]
    public float holdBlurBeforeShake = 1.0f;

    [Header("阶段 2：剧烈摇晃（梦碎感）")]
    [Tooltip("剧烈摇晃持续时长")]
    [Range(0f, 8f)]
    public float shakeDuration = 3.0f;

    [Tooltip("摇晃强度（单位：米，越大抖得越猛）")]
    [Range(0f, 3f)]
    public float shakeStrength = 0.4f;

    [Tooltip("摇晃频率（越大抖得越快）")]
    [Range(1f, 100f)]
    public float shakeFrequency = 35f;

    [Header("阶段 3：疲惫眨眼与最终黑屏")]
    [Tooltip("一共眨几下眼睛（画面变黑再亮）最后闭眼")]
    [Range(1, 6)]
    public int blinkCount = 3;

    [Tooltip("每次眨眼的闭眼/睁眼速度")]
    [Range(0.1f, 1.5f)]
    public float singleBlinkDuration = 0.6f;

    [Tooltip("最终彻底陷入黑暗用时")]
    [Range(0f, 10f)]
    public float fadeToBlackDuration = 1.5f;

    [Tooltip("黑屏保持时长（释放回调前）")]
    [Range(0f, 5f)]
    public float holdBlackDuration = 0.5f;

    [Header("时间")]
    [Tooltip("是否使用不受TimeScale影响的时间（建议用于结尾过场）")]
    public bool useUnscaledTime = true;

    [Header("完成回调")]
    public UnityEvent onSequenceCompleted;

    private CanvasGroup blackCanvasGroup;
    private Coroutine running;
    private bool isPlaying;

    private Volume urpVolume;
    private DepthOfField dof;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else if (Instance != this)
        {
            // 避免多实例造成重复播放
            Destroy(gameObject);
            return;
        }

        if (targetCamera == null)
        {
            targetCamera = Camera.main;
        }

        EnsureBlackOverlay();
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    /// <summary>
    /// 只播放特效；播完后触发 onSequenceCompleted。
    /// </summary>
    public void Play()
    {
        PlayInternal(null);
    }

    /// <summary>
    /// 播放特效后加载指定场景。
    /// </summary>
    public void PlayAndLoadScene(string sceneName)
    {
        if (string.IsNullOrWhiteSpace(sceneName))
        {
            Play();
            return;
        }

        PlayInternal(() => SceneManager.LoadScene(sceneName));
    }

    /// <summary>
    /// 播放特效后退出游戏。
    /// </summary>
    public void PlayAndQuit()
    {
        PlayInternal(() => {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        });
    }

    /// <summary>
    /// 供代码协程等待：yield return EndSequenceVFX.Instance.PlayRoutine();
    /// </summary>
    public IEnumerator PlayRoutine()
    {
        bool finished = false;
        PlayInternal(() => finished = true);

        while (!finished)
        {
            yield return null;
        }
    }

    private void PlayInternal(Action after)
    {
        if (isPlaying)
        {
            return;
        }

        if (running != null)
        {
            StopCoroutine(running);
        }

        running = StartCoroutine(SequenceRoutine(after));
    }

    private IEnumerator SequenceRoutine(Action after)
    {
        isPlaying = true;

        EnsureURPVolume();
        if (dof != null) dof.gaussianMaxRadius.Override(0f);

        EnsureBlackOverlay();
        blackCanvasGroup.alpha = 0f;
        blackCanvasGroup.blocksRaycasts = true;

        // 0) 顿一顿，制造气氛（不马上触发）
        if (initialDelay > 0f)
        {
            yield return Wait(initialDelay);
        }

        // --- 从这里开始，摇晃、模糊、眨眼同时发生 ---

        // 1) 开始左右绵长的摇晃 (从头摇到尾)
        // 自动计算摇晃时长，让它覆盖整个“模糊 + 眨眼 + 变黑全过程”
        float estimatedTotalTime = blurInDuration + (blinkCount * (singleBlinkDuration + 0.3f)) + fadeToBlackDuration;
        shakeDuration = Mathf.Max(shakeDuration, estimatedTotalTime);

        Coroutine cShake = null;
        if (targetCamera != null && shakeDuration > 0f && shakeStrength > 0f)
        {
            cShake = StartCoroutine(ShakeCameraRoutine(targetCamera.transform, shakeDuration, shakeStrength, shakeFrequency));
        }

        // 2) 开始模糊
        Coroutine cFade = null;
        if (dof != null && maxBlur > 0f && blurInDuration > 0f)
        {
            cFade = StartCoroutine(TweenFloat(0f, maxBlur, blurInDuration, v => {
                // 偶尔有种拉扯涣散感
                float noise = (Mathf.PerlinNoise(Time.time * 3f, 0) * 0.3f + 0.85f);
                if (dof != null) dof.gaussianMaxRadius.Override(v * noise);
            }));
        }

        // 稍微等一小下再开始眨眼（比如刚开始摇晃和模糊了 0.5秒后开始眨眼）
        if (holdBlurBeforeShake > 0f)
        {
            yield return Wait(holdBlurBeforeShake);
        }

        // 3) 眨眼变黑（模拟大脑充血/眩晕）
        for (int i = 0; i < blinkCount; i++)
        {
            // 闭眼（视线变黑，但不是全黑，留一点光增加挣扎感）
            float targetAlpha = (i == blinkCount - 1) ? 1.0f : 0.85f;
            float currentAlpha = blackCanvasGroup != null ? blackCanvasGroup.alpha : 0f;

            yield return TweenFloat(currentAlpha, targetAlpha, singleBlinkDuration * 0.5f, v =>
            {
                if (blackCanvasGroup != null) blackCanvasGroup.alpha = v;
            });
            
            // 闭眼保持一下下，越往后越虚弱（闭眼更久）
            yield return Wait(0.1f + 0.1f * i);

            if (i < blinkCount - 1)
            {
                // 再次艰难睁开（不会完全褪去黑色，保留眩晕）
                float backAlpha = Mathf.Min(0.3f + i * 0.15f, 0.7f);
                yield return TweenFloat(targetAlpha, backAlpha, singleBlinkDuration * 0.5f, v =>
                {
                    if (blackCanvasGroup != null) blackCanvasGroup.alpha = v;
                });

                // 睁眼短暂停留
                yield return Wait(0.2f);
            }
        }

        // 4) 彻底陷入死寂（盖成全黑）
        if (fadeToBlackDuration > 0f)
        {
            float alphaNow = blackCanvasGroup != null ? blackCanvasGroup.alpha : 0f;
            if (alphaNow < 1f)
            {
                yield return TweenFloat(alphaNow, 1f, fadeToBlackDuration, v =>
                {
                    if (blackCanvasGroup != null) blackCanvasGroup.alpha = v;
                });
            }
        }
        else
        {
            if (blackCanvasGroup != null) blackCanvasGroup.alpha = 1f;
        }

        if (holdBlackDuration > 0f)
        {
            yield return Wait(holdBlackDuration);
        }


        // 结束：执行回调
        try
        {
            after?.Invoke();
        }
        catch (Exception e)
        {
            Debug.LogException(e);
        }

        onSequenceCompleted?.Invoke();

        isPlaying = false;
        running = null;
    }

    private void EnsureURPVolume()
    {
        if (urpVolume != null) return;
        
        var go = new GameObject("EndSequenceURPBlurVolume");
        go.transform.SetParent(transform, false);
        urpVolume = go.AddComponent<Volume>();
        urpVolume.isGlobal = true;
        urpVolume.priority = 9999;
        
        VolumeProfile profile = ScriptableObject.CreateInstance<VolumeProfile>();
        urpVolume.profile = profile;
        
        dof = profile.Add<DepthOfField>(true);
        dof.active = true;
        // 使用 Gaussian 模式非常适合模拟眩晕模糊
        dof.mode.Override(DepthOfFieldMode.Gaussian);
        dof.gaussianStart.Override(0f);
        dof.gaussianEnd.Override(0f);
        dof.gaussianMaxRadius.Override(0f); // 0代表不受影响，越大越模糊
    }

    private void EnsureBlackOverlay()
    {
        if (blackCanvasGroup != null)
        {
            return;
        }

        // 运行时创建一个全屏黑色覆盖层（不需要你手动做UI）
        var root = new GameObject("EndSequenceOverlay");
        root.transform.SetParent(transform, false);

        var canvas = root.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = short.MaxValue;

        root.AddComponent<CanvasScaler>();
        root.AddComponent<GraphicRaycaster>();

        var black = new GameObject("Black");
        black.transform.SetParent(root.transform, false);

        var rect = black.AddComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        var img = black.AddComponent<Image>();
        img.color = Color.black;
        img.raycastTarget = true;

        blackCanvasGroup = black.AddComponent<CanvasGroup>();
        blackCanvasGroup.alpha = 0f;
        blackCanvasGroup.blocksRaycasts = false;
        blackCanvasGroup.interactable = false;
    }

    private IEnumerator TweenFloat(float from, float to, float duration, Action<float> setter)
    {
        if (duration <= 0f)
        {
            setter?.Invoke(to);
            yield break;
        }

        float t = 0f;
        while (t < duration)
        {
            t += DeltaTime();
            float k = Mathf.Clamp01(t / duration);
            setter?.Invoke(Mathf.Lerp(from, to, k));
            yield return null;
        }

        setter?.Invoke(to);
    }

    private IEnumerator ShakeCameraRoutine(Transform target, float duration, float strength, float frequency)
    {
        if (target == null) yield break;

        Vector3 originPos = target.localPosition;
        Quaternion originRot = target.localRotation;
        Camera cam = target.GetComponent<Camera>();

        float t = 0f;
        float phase = 0f;

        while (t < duration)
        {
            if (!isPlaying) break; // 防止提早被清理
            float dt = DeltaTime();
            t += dt;
            // 降低摇晃的频率，制造一种醉酒、沉重、梦境苏醒时的左摇右晃
            phase += dt * (frequency * 0.3f);

            // 淡入淡出控制，免得突然断掉
            float k = 1f - Mathf.Pow(Mathf.Clamp01(t / duration), 2f);

            // 彻底去除带有心跳感的高频抖动(burst)，改为缓慢而绵长的偏航
            // 营造真正的“上下左右”全方位漂浮恍惚感
            float xOffset = Mathf.Sin(phase) * (strength * 1.5f) * k;          // 左右位移
            float yOffset = Mathf.Cos(phase * 0.8f) * (strength * 1.5f) * k;   // 大幅增强上下位移
            target.localPosition = originPos + new Vector3(xOffset, yOffset, 0f);

            // 伴随无力地上下点头和左右摇头
            float pitchOffset = Mathf.Cos(phase * 0.9f) * (strength * 15f) * k; // 大幅增强上下点头
            float yawOffset = Mathf.Sin(phase * 1.1f) * (strength * 15f) * k;   // 左右摇头
            float rollOffset = Mathf.Sin(phase * 0.7f) * (strength * 10f) * k;  // 稍微带一点歪头，增加自然感
            
            target.localRotation = originRot * Quaternion.Euler(pitchOffset, yawOffset, rollOffset);

            // 删除了 FOV 的缩放，因为它太像心跳/脉搏了

            yield return null;
        }

        if (target != null)
        {
            target.localPosition = originPos;
            target.localRotation = originRot;
        }
    }

    private IEnumerator Wait(float seconds)
    {
        if (seconds <= 0f) yield break;

        if (useUnscaledTime)
        {
            yield return new WaitForSecondsRealtime(seconds);
        }
        else
        {
            yield return new WaitForSeconds(seconds);
        }
    }

    private float DeltaTime()
    {
        return useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
    }
}
