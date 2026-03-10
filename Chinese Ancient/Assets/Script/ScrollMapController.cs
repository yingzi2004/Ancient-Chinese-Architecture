using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using DG.Tweening;
using System.Collections.Generic;
using TMPro;

/// <summary>
/// 卷轴地图主页控制器
/// 负责卷轴展开动画 → 地形浮现 → 建筑逐一浮现 → 点击建筑传送
/// 使用 DOTween 实现全部动画
/// </summary>
public class ScrollMapController : MonoBehaviour
{
    // ═══════════════════════════════════════════════════════
    //  数据定义
    // ═══════════════════════════════════════════════════════

    [System.Serializable]
    public class BuildingEntry
    {
        [Tooltip("建筑名称（显示用）")]
        public string buildingName;
        [Tooltip("建筑图片 UI（Image 或带 CanvasGroup 的 RectTransform）")]
        public RectTransform buildingRect;
        [Tooltip("目标场景名（Build Settings 中的场景名）")]
        public string targetSceneName;
        [Tooltip("建筑浮现延迟（秒），从地形显示完毕后计算")]
        public float appearDelay = 0f;
    }

    // ═══════════════════════════════════════════════════════
    //  Inspector 配置
    // ═══════════════════════════════════════════════════════

    [Header("══ 卷轴设置 ══")]
    [Tooltip("左侧卷轴轴心（RectTransform）")]
    [SerializeField] private RectTransform scrollLeft;
    [Tooltip("右侧卷轴轴心（RectTransform）")]
    [SerializeField] private RectTransform scrollRight;
    [Tooltip("卷轴展开时长（秒）")]
    [SerializeField] private float scrollOpenDuration = 1.5f;
    [Tooltip("卷轴左端展开后的 X 位置")]
    [SerializeField] private float scrollLeftTargetX = -800f;
    [Tooltip("卷轴右端展开后的 X 位置")]
    [SerializeField] private float scrollRightTargetX = 800f;

    [Header("══ 底图 / 纸面 ══")]
    [Tooltip("底图 Image（卷轴纸面背景，用 Mask 裁剪）")]
    [SerializeField] private RectTransform scrollPaper;
    [Tooltip("底图初始宽度（卷轴未展开时）")]
    [SerializeField] private float paperStartWidth = 0f;
    [Tooltip("底图目标宽度（卷轴完全展开后）")]
    [SerializeField] private float paperTargetWidth = 1600f;

    [Header("══ 地形 ══")]
    [Tooltip("地形 Image 的 CanvasGroup（用于淡入）")]
    [SerializeField] private CanvasGroup terrainGroup;
    [Tooltip("地形淡入时长（秒）")]
    [SerializeField] private float terrainFadeDuration = 1.0f;

    [Header("══ 建筑列表 ══")]
    [Tooltip("所有可点击建筑（按出场顺序排列）")]
    [SerializeField] private List<BuildingEntry> buildings = new List<BuildingEntry>();
    [Tooltip("每个建筑浮现的时长（秒）")]
    [SerializeField] private float buildingAppearDuration = 0.6f;
    [Tooltip("建筑之间的默认间隔（秒），若 BuildingEntry 中设了 delay 则用 delay")]
    [SerializeField] private float buildingInterval = 0.3f;

    [Header("══ 标题 / 装饰（可选）══")]
    [Tooltip("标题文字 CanvasGroup")]
    [SerializeField] private CanvasGroup titleGroup;
    [Tooltip("标题淡入时长")]
    [SerializeField] private float titleFadeDuration = 0.8f;

    [Header("══ 过渡遮罩（可选）══")]
    [Tooltip("全屏黑色遮罩 CanvasGroup（场景切换用）")]
    [SerializeField] private CanvasGroup fadeOverlay;
    [Tooltip("场景切换淡出时长")]
    [SerializeField] private float sceneFadeDuration = 0.5f;

    [Header("══ 音效（可选）══")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip scrollOpenClip;
    [SerializeField] private AudioClip buildingAppearClip;

    // ═══════════════════════════════════════════════════════
    //  内部状态
    // ═══════════════════════════════════════════════════════

    private Sequence mainSequence;
    private bool isAnimating = true;
    private bool isTransitioning = false;

    // ═══════════════════════════════════════════════════════
    //  生命周期
    // ═══════════════════════════════════════════════════════

    private void Awake()
    {
        // 确保 DOTween 初始化
        DOTween.Init();
    }

    private void Start()
    {
        InitializeUI();
        PlayOpenSequence();
    }

    private void OnDestroy()
    {
        // 清理 Sequence 防止内存泄漏
        mainSequence?.Kill();
    }

    // ═══════════════════════════════════════════════════════
    //  初始化：把所有元素设置到动画起始状态
    // ═══════════════════════════════════════════════════════

    private void InitializeUI()
    {
        // 卷轴两端从中间位置开始
        if (scrollLeft != null)
            scrollLeft.anchoredPosition = new Vector2(0, scrollLeft.anchoredPosition.y);
        if (scrollRight != null)
            scrollRight.anchoredPosition = new Vector2(0, scrollRight.anchoredPosition.y);

        // 底图纸面初始宽度为 0
        if (scrollPaper != null)
        {
            Vector2 size = scrollPaper.sizeDelta;
            size.x = paperStartWidth;
            scrollPaper.sizeDelta = size;
        }

        // 地形初始完全透明
        if (terrainGroup != null)
        {
            terrainGroup.alpha = 0f;
            terrainGroup.gameObject.SetActive(true);
        }

        // 所有建筑初始隐藏（缩放 0 + 透明）
        foreach (var b in buildings)
        {
            if (b.buildingRect == null) continue;
            b.buildingRect.localScale = Vector3.zero;
            CanvasGroup cg = GetOrAddCanvasGroup(b.buildingRect);
            cg.alpha = 0f;
            cg.interactable = false;
            cg.blocksRaycasts = false;
        }

        // 标题初始隐藏
        if (titleGroup != null)
        {
            titleGroup.alpha = 0f;
        }

        // 遮罩初始透明
        if (fadeOverlay != null)
        {
            fadeOverlay.alpha = 0f;
            fadeOverlay.blocksRaycasts = false;
            fadeOverlay.gameObject.SetActive(true);
        }
    }

    // ═══════════════════════════════════════════════════════
    //  主动画序列
    // ═══════════════════════════════════════════════════════

    private void PlayOpenSequence()
    {
        mainSequence = DOTween.Sequence();

        // ── 第 1 阶段：卷轴展开 ──────────────────────────
        // 音效
        if (audioSource != null && scrollOpenClip != null)
            mainSequence.AppendCallback(() => audioSource.PlayOneShot(scrollOpenClip));

        // 左轴向左滑
        if (scrollLeft != null)
        {
            mainSequence.Join(
                scrollLeft.DOAnchorPosX(scrollLeftTargetX, scrollOpenDuration)
                    .SetEase(Ease.InOutQuad)
            );
        }

        // 右轴向右滑
        if (scrollRight != null)
        {
            mainSequence.Join(
                scrollRight.DOAnchorPosX(scrollRightTargetX, scrollOpenDuration)
                    .SetEase(Ease.InOutQuad)
            );
        }

        // 底图纸面同步展开
        if (scrollPaper != null)
        {
            mainSequence.Join(
                DOTween.To(
                    () => scrollPaper.sizeDelta.x,
                    x =>
                    {
                        Vector2 s = scrollPaper.sizeDelta;
                        s.x = x;
                        scrollPaper.sizeDelta = s;
                    },
                    paperTargetWidth,
                    scrollOpenDuration
                ).SetEase(Ease.InOutQuad)
            );
        }

        // ── 第 2 阶段：地形淡入 ──────────────────────────
        mainSequence.AppendInterval(0.2f); // 短暂停顿

        if (terrainGroup != null)
        {
            mainSequence.Append(
                terrainGroup.DOFade(1f, terrainFadeDuration)
                    .SetEase(Ease.OutCubic)
            );
        }

        // ── 第 3 阶段：建筑逐一浮现 ──────────────────────
        mainSequence.AppendInterval(0.3f); // 地形显示后短暂停顿

        float accumulatedDelay = 0f;
        for (int i = 0; i < buildings.Count; i++)
        {
            BuildingEntry entry = buildings[i];
            if (entry.buildingRect == null) continue;

            float delay = (i == 0) ? 0f :
                          (entry.appearDelay > 0 ? entry.appearDelay : buildingInterval);

            accumulatedDelay += delay;
            int index = i; // 闭包捕获

            // 缩放弹出 + 淡入 同时进行
            CanvasGroup cg = GetOrAddCanvasGroup(entry.buildingRect);

            mainSequence.Insert(
                mainSequence.Duration(false) + accumulatedDelay,
                entry.buildingRect.DOScale(Vector3.one, buildingAppearDuration)
                    .SetEase(Ease.OutBack) // 弹性效果
            );

            mainSequence.Insert(
                mainSequence.Duration(false) + accumulatedDelay,
                cg.DOFade(1f, buildingAppearDuration * 0.6f)
                    .SetEase(Ease.OutCubic)
            );

            // 音效
            if (audioSource != null && buildingAppearClip != null)
            {
                float insertTime = mainSequence.Duration(false) + accumulatedDelay;
                mainSequence.InsertCallback(insertTime,
                    () => audioSource.PlayOneShot(buildingAppearClip));
            }
        }

        // ── 第 4 阶段：标题淡入 + 启用交互 ─────────────────
        mainSequence.AppendInterval(0.3f);

        if (titleGroup != null)
        {
            mainSequence.Append(
                titleGroup.DOFade(1f, titleFadeDuration).SetEase(Ease.OutCubic)
            );
        }

        // 动画结束后启用所有建筑按钮
        mainSequence.AppendCallback(() =>
        {
            foreach (var b in buildings)
            {
                if (b.buildingRect == null) continue;
                CanvasGroup cg = GetOrAddCanvasGroup(b.buildingRect);
                cg.interactable = true;
                cg.blocksRaycasts = true;
            }
            isAnimating = false;
        });

        mainSequence.SetAutoKill(false);
        mainSequence.Play();
    }

    // ═══════════════════════════════════════════════════════
    //  公共方法：建筑按钮调用
    // ═══════════════════════════════════════════════════════

    /// <summary>
    /// 点击建筑图标时调用，传入建筑在列表中的索引
    /// 可绑定到 Button.onClick 或通过 MapBuildingButton 自动关联
    /// </summary>
    public void OnBuildingClicked(int index)
    {
        if (isAnimating || isTransitioning) return;
        if (index < 0 || index >= buildings.Count) return;

        string sceneName = buildings[index].targetSceneName;
        if (string.IsNullOrEmpty(sceneName))
        {
            Debug.LogWarning($"建筑 [{buildings[index].buildingName}] 未配置目标场景！");
            return;
        }

        StartSceneTransition(sceneName);
    }

    /// <summary>
    /// 按建筑名查找并传送
    /// </summary>
    public void OnBuildingClicked(string buildingName)
    {
        int idx = buildings.FindIndex(b => b.buildingName == buildingName);
        if (idx >= 0) OnBuildingClicked(idx);
    }

    // ═══════════════════════════════════════════════════════
    //  场景切换（带淡出效果）
    // ═══════════════════════════════════════════════════════

    private void StartSceneTransition(string sceneName)
    {
        isTransitioning = true;

        if (fadeOverlay != null)
        {
            fadeOverlay.blocksRaycasts = true;
            fadeOverlay.DOFade(1f, sceneFadeDuration)
                .SetEase(Ease.InQuad)
                .OnComplete(() => LoadScene(sceneName));
        }
        else
        {
            LoadScene(sceneName);
        }
    }

    private void LoadScene(string sceneName)
    {
        SceneManager.LoadScene(sceneName);
    }

    // ═══════════════════════════════════════════════════════
    //  跳过动画（可选：点击屏幕跳过）
    // ═══════════════════════════════════════════════════════

    /// <summary>
    /// 调用此方法可跳过开场动画，直接显示完成状态
    /// </summary>
    public void SkipAnimation()
    {
        if (!isAnimating) return;

        mainSequence?.Complete(true);
        isAnimating = false;
    }

    private void Update()
    {
        // 按空格或鼠标点击可跳过动画
        if (isAnimating && (Input.GetKeyDown(KeyCode.Space) || Input.GetMouseButtonDown(0)))
        {
            SkipAnimation();
        }
    }

    // ═══════════════════════════════════════════════════════
    //  工具方法
    // ═══════════════════════════════════════════════════════

    private CanvasGroup GetOrAddCanvasGroup(RectTransform rect)
    {
        CanvasGroup cg = rect.GetComponent<CanvasGroup>();
        if (cg == null)
            cg = rect.gameObject.AddComponent<CanvasGroup>();
        return cg;
    }
}
