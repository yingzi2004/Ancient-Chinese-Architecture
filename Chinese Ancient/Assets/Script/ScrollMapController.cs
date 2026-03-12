using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using DG.Tweening;
using System.Collections.Generic;
using TMPro;

/// <summary>
/// 我们用来纯粹接管点击的脚本（没有任何变灰颜色的副作用）
/// </summary>
public class SimpleClickListener : MonoBehaviour, IPointerClickHandler, IPointerDownHandler
{
    public System.Action onClick;
    
    // 监听鼠标按下的第一瞬间，用来排查是不是这里被拦截了
    public void OnPointerDown(PointerEventData eventData)
    {
        Debug.Log($"【底层点击】鼠标按下了物体: {gameObject.name}");
    }

    // 监听鼠标真正完成“点击”的瞬间（按下并松开）
    public void OnPointerClick(PointerEventData eventData)
    {
        Debug.Log($"【底层点击】成功在物体上完成了点击: {gameObject.name}");
        onClick?.Invoke();
    }
}

/// <summary>
/// 卷轴地图主页控制器 —— 遮挡板方案
/// 
/// 原理：两块黑色 Image 盖在内容上方，初始完全遮住画面。
/// 左板跟随 zhou1 向左，右板跟随 zhou2 向右，露出中间画面。
/// 
/// 层级结构（从下往上渲染）：
/// Scroll-Mask（根 Canvas，Screen Space - Overlay）
///   ├── canva（底图 + 地形 + 建筑）
///   │   ├── bg
///   │   ├── Land / landscapes
///   │   └── build（min, su, jin, tiantan …）
///   ├── Image       ← 左遮挡板（CoverLeft）
///   ├── Image (1)   ← 右遮挡板（CoverRight）
///   ├── zhou1       ← 左卷轴轴心
///   ├── zhou2       ← 右卷轴轴心
///   └── Canvas      ← 可留空 / 放其他 UI
/// </summary>
public class ScrollMapController : MonoBehaviour
{
    [System.Serializable]
    public class BuildingEntry
    {
        public string buildingName;
        public RectTransform buildingRect;
        public string targetSceneName;
        public float appearDelay = 0f;
    }

    [Header("══ 卷轴轴心 ══")]
    [SerializeField] private RectTransform scrollLeft;
    [SerializeField] private RectTransform scrollRight;
    [Tooltip("卷轴（zhou1、zhou2）向两侧展开的持续时间（秒）")]
    [SerializeField] private float scrollOpenDuration = 3.0f;
    [Tooltip("左卷轴的目标X坐标（向左移多远）")]
    [SerializeField] private float scrollLeftTargetX = -800f;
    [Tooltip("右卷轴的目标X坐标（向右移多远）")]
    [SerializeField] private float scrollRightTargetX = 800f;

    [Header("══ 遮挡板（控制底图展开的速度）══")]
    [Tooltip("底图展现（遮挡板拉开）的持续时间（秒）。跟单独的卷轴速度分开！")]
    [SerializeField] private float baseMapRevealDuration = 3.0f;
    [Tooltip("左侧遮挡板 RectTransform")]
    [SerializeField] private RectTransform coverLeft;
    [Tooltip("右侧遮挡板 RectTransform")]
    [SerializeField] private RectTransform coverRight;

    [Header("══ 地形 ══")]
    [SerializeField] private CanvasGroup terrainGroup;
    [Tooltip("地形淡入展现的持续时间（秒）")]
    [SerializeField] private float terrainFadeDuration = 0.5f;

    [Header("══ 建筑列表 ══")]
    [SerializeField] private List<BuildingEntry> buildings = new List<BuildingEntry>();
    [Tooltip("核心：第一个建筑开始弹出的时机（秒）！也就是当卷轴拉开到第几秒的时候，建筑开始排队出现")]
    [SerializeField] private float buildingGlobalStartTime = 2.0f;
    [Tooltip("建筑弹跳放大缩放的持续时间（秒）")]
    [SerializeField] private float buildingScaleDuration = 0.6f;
    [Tooltip("建筑淡入变实的持续时间（秒）")]
    [SerializeField] private float buildingFadeDuration = 0.4f;
    [Tooltip("每个建筑依次先后出现的间隔时间（秒）")]
    [SerializeField] private float buildingInterval = 0.3f;

    [Header("══ 标题（可选）══")]
    [SerializeField] private CanvasGroup titleGroup;
    [SerializeField] private float titleFadeDuration = 0.8f;

    [Header("══ 镜头引导（动画结束后建筑呼吸提示）══")]
    [Tooltip("引导目标建筑在 buildings 列表里的索引（0=第一个=土楼）")]
    [SerializeField] private int guideTargetIndex = 0;
    [Tooltip("引导时建筑呼吸闪烁的缩放幅度（1.0=不缩放，1.03=微微放大3%）")]
    [SerializeField] private float guidePulseScale = 1.03f;
    [Tooltip("是否启用镜头引导")]
    [SerializeField] private bool enableGuide = true;

    [Header("══ 过渡遮罩（可选）══")]
    [SerializeField] private CanvasGroup fadeOverlay;
    [SerializeField] private float sceneFadeDuration = 0.5f;

    [Header("══ 音效（可选）══")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip scrollOpenClip;
    [SerializeField] private AudioClip buildingAppearClip;

    private Sequence mainSequence;
    private DG.Tweening.Tweener guidePulseTween;
    private bool isAnimating = true;
    private bool isTransitioning = false;

    private void Awake()
    {
        DOTween.Init();
    }

    private void Start()
    {
        InitializeUI();
        PlayOpenSequence();
    }

    private void OnDestroy()
    {
        mainSequence?.Kill();
        guidePulseTween?.Kill();
    }

    private void InitializeUI()
    {
        // 音频兜底：如果配置了音效但未手动拖 AudioSource，则自动补一个
        if (audioSource == null && (scrollOpenClip != null || buildingAppearClip != null))
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
            }
            audioSource.playOnAwake = false;
        }

        // 卷轴两端从中间开始，并强制设为最后渲染（即最上层，防止被遮挡）
        if (scrollLeft != null)
        {
            scrollLeft.anchoredPosition = new Vector2(0, scrollLeft.anchoredPosition.y);
            scrollLeft.SetAsLastSibling();
        }
        if (scrollRight != null)
        {
            scrollRight.anchoredPosition = new Vector2(0, scrollRight.anchoredPosition.y);
            scrollRight.SetAsLastSibling();
        }

        // 遮挡板初始位置：完全覆盖画面
        // 左板：右边缘对齐屏幕中心（anchorMax.x=0.5, Left 方向拉伸到屏幕外）
        // 右板：左边缘对齐屏幕中心（anchorMin.x=0.5, Right 方向拉伸到屏幕外）
        // 这在 Inspector 里设好就行，脚本不改位置，只负责向左右推开

        // 地形初始透明
        if (terrainGroup != null)
        {
            terrainGroup.alpha = 0f;
            terrainGroup.gameObject.SetActive(true);
            // 防止地形上的Image吸收了鼠标点击
            Image[] imgs = terrainGroup.GetComponentsInChildren<Image>();
            foreach (var img in imgs) img.raycastTarget = false;
        }

        // 防止遮挡板吸收了鼠标点击
        if (coverLeft != null && coverLeft.GetComponent<Image>() != null) coverLeft.GetComponent<Image>().raycastTarget = false;
        if (coverRight != null && coverRight.GetComponent<Image>() != null) coverRight.GetComponent<Image>().raycastTarget = false;

        // 建筑初始隐藏 & 自动挂载点击脚本
        for (int i = 0; i < buildings.Count; i++)
        {
            var b = buildings[i];
            if (b.buildingRect == null) continue;
            
            // ★ 修改：不要因为任何原因导致物体本身没激活，强行在初始化时激活，只用 scale 和 alpha 隐藏
            b.buildingRect.gameObject.SetActive(true);
            b.buildingRect.localScale = Vector3.zero;
            
            CanvasGroup cg = GetOrAddCanvasGroup(b.buildingRect);
            cg.alpha = 0f;
            
            // ★ 修改：原来这里设置 false 会导致原有的 Button 直接进入 DisabledColor 变成灰色！
            // 现在我们一开始就允许它 interactable，用代码里的 isAnimating 锁去防误触
            cg.interactable = true;
            cg.blocksRaycasts = true;
            
            // 确保图片自身的点击检测是开着的
            Image img = b.buildingRect.GetComponent<Image>();
            if (img != null) img.raycastTarget = true;

            // ★ 核心修复：把 Unity 原生的 Button 组件干掉，它不仅会导致变灰，还经常失灵
            Button oldBtn = b.buildingRect.GetComponent<Button>();
            if (oldBtn != null) Destroy(oldBtn);

            // 挂载我们自己定义的，绝不改变颜色的纯洁点击组件
            SimpleClickListener listener = b.buildingRect.GetComponent<SimpleClickListener>();
            if (listener == null)
            {
                listener = b.buildingRect.gameObject.AddComponent<SimpleClickListener>();
            }
            
            // 绑定点击事件
            int index = i; // 捕获局部变量
            listener.onClick = () => 
            {
                Debug.Log($"【点击检测】物理点击成功！你点到了建筑: {b.buildingName}");
                OnBuildingClicked(index);
            };
        }

        if (titleGroup != null) titleGroup.alpha = 0f;
        if (fadeOverlay != null)
        {
            fadeOverlay.alpha = 0f;
            fadeOverlay.blocksRaycasts = false;
        }
    }

    private void PlayOpenSequence()
    {
        mainSequence = DOTween.Sequence();

        // ── 第 1 阶段：卷轴展开 + 遮挡板同步移开 ──
        if (audioSource != null && scrollOpenClip != null)
            mainSequence.AppendCallback(() => audioSource.PlayOneShot(scrollOpenClip));

        // 左轴向左
        if (scrollLeft != null)
        {
            mainSequence.Join(
                scrollLeft.DOAnchorPosX(scrollLeftTargetX, scrollOpenDuration)
                    .SetEase(Ease.Linear)
            );
        }
        // 右轴向右
        if (scrollRight != null)
        {
            mainSequence.Join(
                scrollRight.DOAnchorPosX(scrollRightTargetX, scrollOpenDuration)
                    .SetEase(Ease.Linear)
            );
        }

        // ★ 左遮挡板向左移（也就是底图展现）
        // 核心修复：遮挡板需要额外多移动自身宽度的距离，才能完全离开屏幕！
        if (coverLeft != null)
        {
            float coverLeftExtra = coverLeft.rect.width;
            mainSequence.Join(
                coverLeft.DOAnchorPosX(scrollLeftTargetX - coverLeftExtra, baseMapRevealDuration)
                    .SetEase(Ease.Linear)
            );
        }
        // ★ 右遮挡板向右移（也就是底图展现）
        if (coverRight != null)
        {
            float coverRightExtra = coverRight.rect.width;
            mainSequence.Join(
                coverRight.DOAnchorPosX(scrollRightTargetX + coverRightExtra, baseMapRevealDuration)
                    .SetEase(Ease.Linear)
            );
        }

        // ── 第 2 阶段：地形淡入 ──
        // （地形可以在卷轴才拉开一点点的时候就开始同步淡入了，不需要干等！）
        mainSequence.Insert(
            0.5f, // 指在卷轴开始拉动0.5秒后，地形就迫不及待开始淡入
            terrainGroup.DOFade(1f, terrainFadeDuration).SetEase(Ease.OutCubic)
        );

        // ── 第 3 阶段：建筑逐一浮现 ──
        // 建筑弹出的大起点（可通过 Inspector 面板上的 buildingGlobalStartTime 随时调整）
        Debug.Log($"【动画执行】准备浮现建筑！系统检测到你在 Inspector 里配置了 {buildings.Count} 个建筑。");
        
        float currentDelay = 0f; // 当前这个建筑的开始时间点相对偏置
        
        for (int i = 0; i < buildings.Count; i++)
        {
            BuildingEntry entry = buildings[i];
            if (entry.buildingRect == null) 
            {
                Debug.LogWarning($"第 {i} 个建筑的图片没有拖进去！被跳过了！");
                continue;
            }

            // 每个建筑出现的时间，纯粹就是：在自己专属的延迟基础之上
            // 这是修复超长时间间隔的核心！用单独的基础递增！
            if (i > 0)
            {
                if (entry.appearDelay > 0) currentDelay += entry.appearDelay;
                else currentDelay += buildingInterval; 
            }

            CanvasGroup cg = GetOrAddCanvasGroup(entry.buildingRect);
            
            // ★ 修复：强制将建筑本身的 active 设为 true并放在最前面
            entry.buildingRect.gameObject.SetActive(true);

            // 打印出每个建筑真正进入动画队列的时间
            Debug.Log($"【建筑排队】建筑 '{entry.buildingName}' 已经被排进了动画队列。预计在 {buildingGlobalStartTime + currentDelay:F2} 秒时浮现。");

            mainSequence.Insert(
                buildingGlobalStartTime + currentDelay,
                entry.buildingRect.DOScale(Vector3.one, buildingScaleDuration)
                    .SetEase(Ease.OutBack)
            );
            mainSequence.Insert(
                buildingGlobalStartTime + currentDelay,
                cg.DOFade(1f, buildingFadeDuration).SetEase(Ease.OutCubic)
            );

            // 用 DOTween 的回调在建筑*真正开始弹出那一刻*打日志
            float floatTime = buildingGlobalStartTime + currentDelay;
            string curName = entry.buildingName; // 捕获局部变量防闭包错误
            mainSequence.InsertCallback(floatTime, () => 
            {
                Debug.Log($"【建筑弹出】★★★ 建筑 '{curName}' 此时此刻正在弹出！(Scale & Fade 同时进行中)");
            });

            if (audioSource != null && buildingAppearClip != null)
            {
                mainSequence.InsertCallback(floatTime, () => audioSource.PlayOneShot(buildingAppearClip));
            }
        }

        // ── 第 4 阶段：标题 + 启用交互 ──
        mainSequence.AppendInterval(0.3f);
        if (titleGroup != null)
        {
            mainSequence.Append(titleGroup.DOFade(1f, titleFadeDuration).SetEase(Ease.OutCubic));
        }

        mainSequence.AppendCallback(() =>
        {
            // ★ 保底：动画结束后直接隐藏遮挡板，确保它们不会残留在画面上
            if (coverLeft != null) coverLeft.gameObject.SetActive(false);
            if (coverRight != null) coverRight.gameObject.SetActive(false);

            Debug.Log("【动画结束】开场动画播完，准备镜头引导...");
            
            if (enableGuide)
            {
                PlayGuideAnimation(() =>
                {
                    isAnimating = false;
                    Debug.Log("【引导完成】此时可以开始点击建筑了！");
                });
            }
            else
            {
                isAnimating = false;
                Debug.Log("【动画结束】镜头引导已关闭，直接允许点击。");
            }
        });

        mainSequence.SetAutoKill(false);
        mainSequence.Play();
    }

    /// <summary>
    /// 镜头引导动画：给目标建筑加上呼吸脉冲，引导玩家点击
    /// </summary>
    private void PlayGuideAnimation(System.Action onComplete)
    {
        if (guideTargetIndex < 0 || guideTargetIndex >= buildings.Count)
        {
            Debug.LogWarning("【引导跳过】guideTargetIndex 超出范围");
            onComplete?.Invoke();
            return;
        }

        BuildingEntry target = buildings[guideTargetIndex];
        if (target.buildingRect == null)
        {
            onComplete?.Invoke();
            return;
        }

        Debug.Log($"【镜头引导】开始引导建筑 '{target.buildingName}' 呼吸脉冲");

        // 直接给目标建筑加上呼吸脉冲（循环闪烁，引导玩家去点击）
        guidePulseTween = target.buildingRect
            .DOScale(Vector3.one * guidePulseScale, 0.6f)
            .SetEase(Ease.InOutSine)
            .SetLoops(-1, LoopType.Yoyo);

        Debug.Log($"【镜头引导】建筑 '{target.buildingName}' 呼吸脉冲开始！");
        onComplete?.Invoke();
    }

    public void OnBuildingClicked(int index)
    {
        Debug.Log($"尝试触发建筑跳转，当前状态：动画中={isAnimating}, 正在跳转中={isTransitioning}");
        if (isAnimating || isTransitioning) return;

        // 点击了任何建筑后，停止呼吸脉冲动画
        guidePulseTween?.Kill();
        if (guideTargetIndex >= 0 && guideTargetIndex < buildings.Count && buildings[guideTargetIndex].buildingRect != null)
            buildings[guideTargetIndex].buildingRect.localScale = Vector3.one;
        
        if (index < 0 || index >= buildings.Count) return;
        
        string sceneName = buildings[index].targetSceneName;
        
        // ★ 核心修复：自动帮你把名字前后的“空格”全删掉，防止你手抖多按了空格！
        if (!string.IsNullOrEmpty(sceneName))
        {
            sceneName = sceneName.Trim();
        }

        Debug.Log($"准备跳转的场景名字是：【{sceneName}】");
        
        if (string.IsNullOrEmpty(sceneName)) 
        {
            Debug.LogError("跳转失败：你没有在 Inspector 面板里填写 Target Scene Name！");
            return;
        }
        
        StartSceneTransition(sceneName);
    }

    public void OnBuildingClicked(string buildingName)
    {
        int idx = buildings.FindIndex(b => b.buildingName == buildingName);
        if (idx >= 0) OnBuildingClicked(idx);
    }

    private void StartSceneTransition(string sceneName)
    {
        isTransitioning = true;
        if (fadeOverlay != null)
        {
            fadeOverlay.blocksRaycasts = true;
            fadeOverlay.DOFade(1f, sceneFadeDuration)
                .SetEase(Ease.InQuad)
                .OnComplete(() => SceneManager.LoadScene(sceneName));
        }
        else
        {
            SceneManager.LoadScene(sceneName);
        }
    }

    public void SkipAnimation()
    {
        if (!isAnimating) return;
        mainSequence?.Complete(true);
        isAnimating = false;
    }

    private void Update()
    {
        // 我们在这里加一个暴力的全局鼠标点击检测，用来抓真凶！
        if (Input.GetMouseButtonDown(0))
        {
            Debug.Log("【全局鼠标】检测到了左键点击！正在分析点击到了什么 UI 物体上...");
            
            PointerEventData pointerData = new PointerEventData(EventSystem.current);
            pointerData.position = Input.mousePosition;
            List<RaycastResult> results = new List<RaycastResult>();
            if (EventSystem.current != null)
            {
                EventSystem.current.RaycastAll(pointerData, results);
                if (results.Count > 0)
                {
                    Debug.Log($"【射线穿透详细报告】你点的这个位置，鼠标一共穿透了 {results.Count} 层 UI，从上到下依次是：");
                    for (int i = 0; i < results.Count; i++)
                    {
                        Debug.Log($"    -> 第 {i+1} 层： {results[i].gameObject.name}");
                    }
                    Debug.Log($"！！！最后只有位于【第 1 层 (最顶层)】的 [{results[0].gameObject.name}] 会吃掉并拦截这次点击！！！");
                }
                else
                {
                    Debug.Log("【射线报告空】鼠标点在了空地，这个位置不存在任何打勾了 Raycast Target 的 UI 元素！");
                }
            }
            else
            {
                Debug.LogError("【严重错误】场景里不存在 EventSystem！所有的UI点击都不会有反应！请在左侧右键 -> UI -> Event System 创建它！");
            }
        }

        if (isAnimating && (Input.GetKeyDown(KeyCode.Space) || Input.GetMouseButtonDown(0)))
            SkipAnimation();
    }

    private CanvasGroup GetOrAddCanvasGroup(RectTransform rect)
    {
        CanvasGroup cg = rect.GetComponent<CanvasGroup>();
        if (cg == null) cg = rect.gameObject.AddComponent<CanvasGroup>();
        return cg;
    }
}
