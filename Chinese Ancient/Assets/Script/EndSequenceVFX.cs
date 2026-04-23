using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
public class EndSequenceVFX : MonoBehaviour
{
    public static EndSequenceVFX Instance { get; private set; }
    [Header("目标")]
    public Camera targetCamera;
    [Header("阶段 0：初始等待")]
    [Range(0f, 5f)]
    public float initialDelay = 1.0f;
    [Header("阶段 1：视角模糊")]
    [Range(0f, 2.5f)]
    public float maxBlur = 1.5f;
    [Range(0f, 8f)]
    public float blurInDuration = 2.0f;
    [Range(0f, 5f)]
    public float holdBlurBeforeShake = 1.0f;
    [Header("阶段 2：剧烈摇晃（梦碎感）")]
    [Range(0f, 8f)]
    public float shakeDuration = 3.0f;
    [Range(0f, 3f)]
    public float shakeStrength = 0.4f;
    [Range(1f, 100f)]
    public float shakeFrequency = 35f;
    [Header("阶段 3：疲惫眨眼与最终黑屏")]
    [Range(1, 6)]
    public int blinkCount = 3;
    [Range(0.1f, 1.5f)]
    public float singleBlinkDuration = 0.6f;
    [Range(0f, 10f)]
    public float fadeToBlackDuration = 1.5f;
    [Range(0f, 5f)]
    public float holdBlackDuration = 0.5f;
    [Header("额外：原场景背景音乐淡出")]
    public AudioSource sceneBgmToFadeOut;
    [Header("阶段 4：最终场景跳转 (新版地图过渡)")]
    public string targetEndingScene;
    [Header("时间")]
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
    public void Play()
    {
        PlayInternal(null);
    }
    public void PlayAndLoadScene(string sceneName)
    {
        if (string.IsNullOrWhiteSpace(sceneName))
        {
            Play();
            return;
        }
        PlayInternal(() => SceneManager.LoadScene(sceneName));
    }
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
        // 延迟一段时间增加气氛
        if (initialDelay > 0f)
        {
            yield return Wait(initialDelay);
        }
        // --- 开始视觉效果序列 ---
        // 计算总摇晃时间，覆盖后续效果
        float estimatedTotalTime = blurInDuration + (blinkCount * (singleBlinkDuration + 0.3f)) + fadeToBlackDuration;
        shakeDuration = Mathf.Max(shakeDuration, estimatedTotalTime);
        // --- 背景音乐淡出 ---
        if (sceneBgmToFadeOut != null)
        {
            float startVol = sceneBgmToFadeOut.volume;
            StartCoroutine(TweenFloat(startVol, 0f, estimatedTotalTime, v => {
                if (sceneBgmToFadeOut != null) sceneBgmToFadeOut.volume = v;
            }));
        }
        Coroutine cShake = null;
        if (targetCamera != null && shakeDuration > 0f && shakeStrength > 0f)
        {
            cShake = StartCoroutine(ShakeCameraRoutine(targetCamera.transform, shakeDuration, shakeStrength, shakeFrequency));
        }
        // 2) 画面模糊
        Coroutine cFade = null;
        if (dof != null && maxBlur > 0f && blurInDuration > 0f)
        {
            cFade = StartCoroutine(TweenFloat(0f, maxBlur, blurInDuration, v => {
                // 添加动态噪点
                float noise = (Mathf.PerlinNoise(Time.time * 3f, 0) * 0.3f + 0.85f);
                if (dof != null) dof.gaussianMaxRadius.Override(v * noise);
            }));
        }
        // 延迟一段时间开始眨眼过渡
        if (holdBlurBeforeShake > 0f)
        {
            yield return Wait(holdBlurBeforeShake);
        }
        // 3) 眨眼和黑屏过渡效果
        for (int i = 0; i < blinkCount; i++)
        {
            // 闭眼阶段
            float targetAlpha = (i == blinkCount - 1) ? 1.0f : 0.85f;
            float currentAlpha = blackCanvasGroup != null ? blackCanvasGroup.alpha : 0f;
            yield return TweenFloat(currentAlpha, targetAlpha, singleBlinkDuration * 0.5f, v =>
            {
                if (blackCanvasGroup != null) blackCanvasGroup.alpha = v;
            });
            // 保持闭眼状态，时间递增
            yield return Wait(0.1f + 0.1f * i);
            if (i < blinkCount - 1)
            {
                // 睁眼恢复部分视线
                float backAlpha = Mathf.Min(0.3f + i * 0.15f, 0.7f);
                yield return TweenFloat(targetAlpha, backAlpha, singleBlinkDuration * 0.5f, v =>
                {
                    if (blackCanvasGroup != null) blackCanvasGroup.alpha = v;
                });
                // 睁眼状态停留
                yield return Wait(0.2f);
            }
        }
        // 4) 完全黑屏
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
        // 5) 加载目标场景
        if (!string.IsNullOrEmpty(targetEndingScene))
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene(targetEndingScene);
        }
        // 完成回调
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
        dof.mode.Override(DepthOfFieldMode.Gaussian);
        dof.gaussianStart.Override(0f);
        dof.gaussianEnd.Override(0f);
        dof.gaussianMaxRadius.Override(0f);
    }
    private void EnsureBlackOverlay()
    {
        if (blackCanvasGroup != null)
        {
            return;
        }
        // 创建全屏黑屏UI
        var root = new GameObject("EndSequenceOverlay");
        root.transform.SetParent(transform, false);
        var canvas = root.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 30000;
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
            if (!isPlaying) break;
            float dt = DeltaTime();
            t += dt;
            phase += dt * (frequency * 0.3f);
            // 缓动曲线
            float k = 1f - Mathf.Pow(Mathf.Clamp01(t / duration), 2f);
            // 位移计算
            float xOffset = Mathf.Sin(phase) * (strength * 1.5f) * k;
            float yOffset = Mathf.Cos(phase * 0.8f) * (strength * 1.5f) * k;
            target.localPosition = originPos + new Vector3(xOffset, yOffset, 0f);
            // 旋转计算
            // AI辅助生成：DeepSeek-V3.2, 2026-04-21
            float pitchOffset = Mathf.Cos(phase * 0.9f) * (strength * 15f) * k;
            float yawOffset = Mathf.Sin(phase * 1.1f) * (strength * 15f) * k;
            float rollOffset = Mathf.Sin(phase * 0.7f) * (strength * 10f) * k;
            
            target.localRotation = originRot * Quaternion.Euler(pitchOffset, yawOffset, rollOffset);
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
