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

        [Header("黑白差分图(可选)")]
        [Tooltip("【如果有黑白图，拖到这里。未解锁时会丝滑渐现它】")]
        public Sprite bwSprite;

        // 运行时动态创建的黑白图层CanvasGroup
        [HideInInspector]
        public CanvasGroup runtimeBWGroup;
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
    
    [Header("══ 解锁设置 ══")]
    [Tooltip("未解锁建筑等待动画播完后，变灰的颜色（请在面板里调成灰色，默认带透明度）")]
    [SerializeField] private Color lockedBuildingColor = new Color(0.6f, 0.6f, 0.6f, 1f);
    [Tooltip("变灰过渡动画的持续时间")]
    [SerializeField] private float lockedColorFadeDuration = 1.0f;

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
    [SerializeField] private Image fadeOverlayImage;
    [SerializeField] private float sceneFadeDuration = 0.5f;

    [Header("══ 开场对话（新增）══")]
    [SerializeField] private GameObject dialoguePanel;
    [SerializeField] private TextMeshProUGUI dialogueText;
    [SerializeField] private float dialogueTypingSpeed = 0.08f;
    [SerializeField] private float dialoguePauseDuration = 1.5f;

    [Header("══ 地图打开后对话（新增）══")]
    [SerializeField] private GameObject mapDialoguePanel;
    [SerializeField] private TextMeshProUGUI mapDialogueText;

    [Header("══ 音效（可选）══")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip scrollOpenClip;
    [SerializeField] private AudioClip buildingAppearClip;
    [SerializeField] private AudioClip windBlowClip;

    [Header("══ 动画控制 ══")]
    [Tooltip("是否强制在唤出时播放卷轴打开动画？")]
    public bool playOpenAnimation = true;

    [Header("══ 进度控制 ══")]
    [Tooltip("【核心设置】主场景独立测试用。只在运行主场景时把【显示效果】控制在初始关卡，但不覆盖玩家真实的过关存档！")]
    public bool overrideAsFirstLevel = true;

    // 全局静态变量：标记本游戏运行期间，是否已经播放过一次主场景的打开动画
    public static bool hasPlayedGlobalMapAnimation = false;

    private Sequence mainSequence;
    private DG.Tweening.Tweener guidePulseTween;
    private bool isAnimating = true;
    private bool isTransitioning = false;
    private CanvasGroup fadeOverlay;

    // 开场对话文本
    private string[] openingDialogue = new string[]
    {
        "咦…… 这是哪里？头有点晕……",
        "我记得我是跟着古建筑考察队，去寻访苏州园林、北京天坛这些经典建筑的，怎么突然到了这个地方？",
        "一阵风吹过，一张泛黄的古地图飘到你面前……"
    };

    // 地图打开后的对话文本
    private string[] mapOpenDialogue = new string[]
    {
        "哇塞！这原来是汇集了各地经典的古建筑地图啊！也太酷了吧～",
        "苏州园林、北京天坛、晋商大院、福建土楼都在上面，每一个都是我超想打卡的地方！",
        "地图上的每个标记都闪着微光，仿佛在邀请你走进这些千年建筑的故事里～",
        "好期待呀！真想马上走进这些古建筑，看看它们藏着的小细节～"
    };

    private void Awake()
    {
        DOTween.Init();

        // 立即设置黑屏，确保游戏一开始就是全黑
        if (fadeOverlayImage != null)
        {
            fadeOverlay = fadeOverlayImage.GetComponent<CanvasGroup>();
            if (fadeOverlay == null)
            {
                fadeOverlay = fadeOverlayImage.gameObject.AddComponent<CanvasGroup>();
            }
            fadeOverlay.alpha = 1f;
            fadeOverlay.blocksRaycasts = true;
        }
    }

    private void OnEnable()
    {
        // 当从外部（比如EndSequenceVFX）激活地图时，如果不需要再次播放开场动画，确保立刻显示！
        if (!playOpenAnimation || hasPlayedGlobalMapAnimation)
        {
            InstantOpenMap();
        }
    }

    private void Start()
    {
        InitializeUI();

        // 如果强制播放，或者全局还没有播放过开场对话和动画
        if (playOpenAnimation && !hasPlayedGlobalMapAnimation)
        {
            StartCoroutine(PlayOpeningSequence()); // 原本直接调用 PlayOpenSequence()，现在改为带有对话的完整序列
            hasPlayedGlobalMapAnimation = true; // 记录：已经播过啦，下次再回来就不播了
        }
        else
        {
            // 否则（比如从其他场景按M回来，或者手动关闭了动画），就把黑屏等隐藏，瞬间开启地图！
            if (fadeOverlay != null)
            {
                fadeOverlay.alpha = 0f;
                fadeOverlay.blocksRaycasts = false;
            }
            if (dialoguePanel != null)
            {
                dialoguePanel.SetActive(false);
            }
            InstantOpenMap();
        }
    }

    /// <summary>
    /// 瞬间完全打开地图（取消动画）
    /// </summary>
    private void InstantOpenMap()
    {
        isAnimating = false; // 解除防误触锁

        // 显示卷轴轴心
        if (scrollLeft != null)
        {
            CanvasGroup leftGroup = scrollLeft.GetComponent<CanvasGroup>();
            if (leftGroup != null) leftGroup.alpha = 1f;
            scrollLeft.anchoredPosition = new Vector2(scrollLeftTargetX, scrollLeft.anchoredPosition.y);
        }
        if (scrollRight != null)
        {
            CanvasGroup rightGroup = scrollRight.GetComponent<CanvasGroup>();
            if (rightGroup != null) rightGroup.alpha = 1f;
            scrollRight.anchoredPosition = new Vector2(scrollRightTargetX, scrollRight.anchoredPosition.y);
        }

        // 瞬间将遮挡板移开
        if (coverLeft != null)
        {
            float coverLeftExtra = coverLeft.rect.width;
            coverLeft.anchoredPosition = new Vector2(scrollLeftTargetX - coverLeftExtra, coverLeft.anchoredPosition.y);
            coverLeft.gameObject.SetActive(false); // 并且隐藏
        }
        if (coverRight != null)
        {
            float coverRightExtra = coverRight.rect.width;
            coverRight.anchoredPosition = new Vector2(scrollRightTargetX + coverRightExtra, coverRight.anchoredPosition.y);
            coverRight.gameObject.SetActive(false); // 并且隐藏
        }

        if (terrainGroup != null) terrainGroup.alpha = 1f;
        if (titleGroup != null) titleGroup.alpha = 1f;

        // 因为不再使用电脑本地存档，这里根据 overrideAsFirstLevel 直接计算 (主界面通常只开第一个)
        int unlockedLevel = overrideAsFirstLevel ? 0 : 0; // 可以随时扩展为从其他系统读取

        for (int i = 0; i < buildings.Count; i++)
        {
            var b = buildings[i];
            if (b.buildingRect != null)
            {
                b.buildingRect.localScale = Vector3.one;
                CanvasGroup cg = GetOrAddCanvasGroup(b.buildingRect);
                cg.alpha = 1f;

                EnsureBWOverlay(b);

                bool isUnlocked = (i <= unlockedLevel);
                Image img = b.buildingRect.GetComponent<Image>();

                if (b.runtimeBWGroup != null)
                {
                    // 如果有黑白贴图，瞬间显示/隐藏黑白贴图层，底图保持原生颜色
                    b.runtimeBWGroup.alpha = isUnlocked ? 0f : 1f;
                    if (img != null) 
                    {
                        // 【修复重影】如果未解锁，这里瞬间把底层的彩图完全透明化隐藏
                        img.color = isUnlocked ? Color.white : new Color(1, 1, 1, 0f);
                    }
                }
                else if (img != null)
                {
                    // 没有配置黑白贴图的话，退回到老版本的“染色变灰”
                    img.color = isUnlocked ? Color.white : lockedBuildingColor;
                }
            }
        }
    }

    /// <summary>
    /// 动态生成一个专门的黑白图片涂层，覆盖在原来的建筑图片上面，方便实现“丝滑渐变”
    /// </summary>
    private void EnsureBWOverlay(BuildingEntry b)
    {
        if (b.bwSprite == null || b.runtimeBWGroup != null) return;

        GameObject bwObj = new GameObject("BW_Overlay");
        RectTransform rect = bwObj.AddComponent<RectTransform>();
        rect.SetParent(b.buildingRect, false);
        
        // 自动铺满父物体的全部空间
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        rect.localScale = Vector3.one;

        Image bwImg = bwObj.AddComponent<Image>();
        bwImg.sprite = b.bwSprite;
        bwImg.raycastTarget = false; // 不要拦截鼠标点击！

        // 尝试继承父节点 Image 的拉伸属性（可选，大部分情况下不需要）
        Image parentImg = b.buildingRect.GetComponent<Image>();
        if (parentImg != null)
        {
            bwImg.preserveAspect = parentImg.preserveAspect;
            bwImg.type = parentImg.type;
        }

        b.runtimeBWGroup = bwObj.AddComponent<CanvasGroup>();
        b.runtimeBWGroup.alpha = 0f;
        b.runtimeBWGroup.interactable = false;
        b.runtimeBWGroup.blocksRaycasts = false;
    }

    private void OnDestroy()
    {
        mainSequence?.Kill();
        guidePulseTween?.Kill();
    }

    private void InitializeUI()
    {
        // 音频兜底：如果配置了音效但未手动拖 AudioSource，则自动补一个
        if (audioSource == null && (scrollOpenClip != null || buildingAppearClip != null || windBlowClip != null))
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
            }
            audioSource.playOnAwake = false;
        }

        // 卷轴两端从中间开始，并强制设为最后渲染（即最上层，防止被遮挡）
        // 同时隐藏它们，确保开场黑屏期间看不到
        if (scrollLeft != null)
        {
            scrollLeft.anchoredPosition = new Vector2(0, scrollLeft.anchoredPosition.y);
            scrollLeft.SetAsLastSibling();
            CanvasGroup leftGroup = scrollLeft.GetComponent<CanvasGroup>();
            if (leftGroup == null) leftGroup = scrollLeft.gameObject.AddComponent<CanvasGroup>();
            leftGroup.alpha = 0f;
        }
        if (scrollRight != null)
        {
            scrollRight.anchoredPosition = new Vector2(0, scrollRight.anchoredPosition.y);
            scrollRight.SetAsLastSibling();
            CanvasGroup rightGroup = scrollRight.GetComponent<CanvasGroup>();
            if (rightGroup == null) rightGroup = scrollRight.gameObject.AddComponent<CanvasGroup>();
            rightGroup.alpha = 0f;
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
        // 【核心修改】：主场景永远假装当前只解锁了 0 级（土楼），不读真实存档，也不覆写真首档。
        int unlockedLevel = 0;

        for (int i = 0; i < buildings.Count; i++)
        {
            var b = buildings[i];
            if (b.buildingRect == null) continue;
            
            // 确保提前生成一次黑白贴图图层（如果有的话）
            EnsureBWOverlay(b);

            // 当前这个建筑是否已经解锁？
            bool isUnlocked = (i <= unlockedLevel);

            b.buildingRect.gameObject.SetActive(true);
            b.buildingRect.localScale = Vector3.zero;
            
            CanvasGroup cg = GetOrAddCanvasGroup(b.buildingRect);
            cg.alpha = 0f;
            cg.interactable = true;
            cg.blocksRaycasts = true;
            
            Image img = b.buildingRect.GetComponent<Image>();
            if (img != null) 
            {
                img.raycastTarget = true;
                // 保证所有建筑初始弹出时，都是正常的颜色！如果带了真正的黑白图层也先让它以 0 alpha(纯透明) 弹出
                img.color = Color.white; 
            }
            if (b.runtimeBWGroup != null)
            {
                b.runtimeBWGroup.alpha = 0f; // 开场先全是彩色的！
            }

            // 把 Unity 原生的 Button 组件干掉
            Button oldBtn = b.buildingRect.GetComponent<Button>();
            if (oldBtn != null) Destroy(oldBtn);

            // 挂载我们自己定义的纯洁点击组件
            SimpleClickListener listener = b.buildingRect.GetComponent<SimpleClickListener>();
            if (listener == null)
            {
                listener = b.buildingRect.gameObject.AddComponent<SimpleClickListener>();
            }
            
            // 绑定点击事件
            int index = i; 
            listener.onClick = () => 
            {
                // 【核心修改】：主场景永远只允许点击 0 级（土楼）
                int currentUnlocked = 0;

                // 如果没有解锁，阻止点击跳转
                if (index > currentUnlocked)
                {
                    Debug.Log($"【系统拦截】这是主场景，强制限制只允许点击土楼（序号0）！试图点击更高级场景被拦截。");
                    return; 
                }

                Debug.Log($"【点击检测】物理点击成功！你点到了已解锁的建筑: {b.buildingName}");
                OnBuildingClicked(index);
            };
        }

        if (titleGroup != null) titleGroup.alpha = 0f;

        // 初始化黑屏遮罩的 CanvasGroup
        if (fadeOverlayImage != null)
        {
            fadeOverlay = fadeOverlayImage.GetComponent<CanvasGroup>();
            if (fadeOverlay == null)
            {
                fadeOverlay = fadeOverlayImage.gameObject.AddComponent<CanvasGroup>();
            }
        }
        // 注意：不在初始化时重置 fadeOverlay，因为开场序列需要从黑屏开始
        // fadeOverlay 的初始状态由 PlayOpeningSequence() 控制

        // 初始化对话面板
        if (dialoguePanel != null)
        {
            dialoguePanel.SetActive(false);
        }
        if (dialogueText != null)
        {
            dialogueText.overflowMode = TextOverflowModes.Overflow;
            dialogueText.enableWordWrapping = true;
        }

        // 初始化地图对话面板
        if (mapDialoguePanel != null)
        {
            mapDialoguePanel.SetActive(false);
        }
        if (mapDialogueText != null)
        {
            mapDialogueText.overflowMode = TextOverflowModes.Overflow;
            mapDialogueText.enableWordWrapping = true;
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
            // ★ 保底：动画结束后直接隐藏遮挡板
            if (coverLeft != null) coverLeft.gameObject.SetActive(false);
            if (coverRight != null) coverRight.gameObject.SetActive(false);

            Debug.Log("【动画结束】开场动画播完，所有建筑已弹出，现在开始将未解锁场景褪色变灰，并准备播放对话...");

            // 动画完成后播放地图对话
            StartCoroutine(PlayMapOpenDialogue());
            
            // 此时全部正常颜色显示完毕了，然后再把未解锁的慢慢变灰
            // 【核心修改】：主场景强制认定当前解锁进度为0，永远只把1/2/3级变黑白
            int unlockedLevel = 0;
            for (int i = 0; i < buildings.Count; i++)
            {
                var entry = buildings[i];
                if (i > unlockedLevel && entry.buildingRect != null)
                {
                    if (entry.runtimeBWGroup != null)
                    {
                        // 最完美！如果配置了真实的黑白图，我们直接“丝滑淡入”覆盖上去
                        entry.runtimeBWGroup.DOFade(1f, lockedColorFadeDuration).SetEase(Ease.InOutQuad);

                        // 【修复重影】：同时把底部的彩色原图逐渐透明化隐藏掉，两者发生“交叉淡入淡出(Crossfade)”，完美解决两张图叠在一起产生重黑边或发暗的问题
                        Image img = entry.buildingRect.GetComponent<Image>();
                        if (img != null)
                        {
                            img.DOFade(0f, lockedColorFadeDuration).SetEase(Ease.InOutQuad);
                        }
                    }
                    else
                    {
                        // 后备方案：如果没有拖黑白图，就用老方法，把它本身缓慢染成灰色
                        Image img = entry.buildingRect.GetComponent<Image>();
                        if (img != null)
                        {
                            img.DOColor(lockedBuildingColor, lockedColorFadeDuration).SetEase(Ease.InOutQuad);
                        }
                    }
                }
            }
            
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
        // 【核心修改】：主场景的强制动画引导：永远只引导 0 级（土楼），无视其他进度
        int unlockedMax = 0;
        int target = Mathf.Min(unlockedMax, buildings.Count - 1); // 保证不越界

        if (target < 0 || target >= buildings.Count)
        {
            Debug.LogWarning("【引导跳过】guideTargetIndex 超出范围");
            onComplete?.Invoke();
            return;
        }

        BuildingEntry entry = buildings[target];
        if (entry.buildingRect == null)
        {
            onComplete?.Invoke();
            return;
        }

        Debug.Log($"【镜头引导】开始引导当前最新解锁的建筑 '{entry.buildingName}' 呼吸脉冲");

        // 直接给目标建筑加上呼吸脉冲（循环闪烁，引导玩家去点击）
        guidePulseTween = entry.buildingRect
            .DOScale(Vector3.one * guidePulseScale, 0.6f)
            .SetEase(Ease.InOutSine)
            .SetLoops(-1, LoopType.Yoyo);

        Debug.Log($"【镜头引导】建筑 '{entry.buildingName}' 呼吸脉冲开始！");
        onComplete?.Invoke();
    }

    public void OnBuildingClicked(int index)
    {
        Debug.Log($"尝试触发建筑跳转，当前状态：动画中={isAnimating}, 正在跳转中={isTransitioning}");
        if (isAnimating || isTransitioning) return;

        // 点击了任何建筑后，停止呼吸脉冲动画
        guidePulseTween?.Kill();
        
        // 主场景写死 0 级放大恢复
        int currentUnlockedMax = 0;
        int throbbingTargetIndex = Mathf.Min(currentUnlockedMax, buildings.Count - 1);

        if (throbbingTargetIndex >= 0 && throbbingTargetIndex < buildings.Count && buildings[throbbingTargetIndex].buildingRect != null)
            buildings[throbbingTargetIndex].buildingRect.localScale = Vector3.one;
        
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
        Time.timeScale = 1f; // 修复：切换场景前强制恢复时间比例
        if (fadeOverlay != null)
        {
            fadeOverlay.blocksRaycasts = true;
            fadeOverlay.DOFade(1f, sceneFadeDuration)
                .SetEase(Ease.InQuad)
                .OnComplete(() => StartCoroutine(DeferredLoadScene(sceneName)));
        }
        else
        {
            StartCoroutine(DeferredLoadScene(sceneName));
        }
    }

    // 核心修复：给EventSystem一个处理回调闭环的延迟时间（1帧），防止新场景EventSystem卡死
    private System.Collections.IEnumerator DeferredLoadScene(string targetScene)
    {
        yield return null; 
        SceneManager.LoadScene(targetScene);
    }

    public void SkipAnimation()
    {
        if (!isAnimating) return;
        mainSequence?.Complete(true);
        isAnimating = false;
    }

    /// <summary>
    /// 关闭地图动画：卷轴向中间卷起，最后完美黑屏并退出游戏
    /// 触发方法：可以绑定给 EndSequenceVFX 面板里的 onMapCloseTrigger
    /// </summary>
    public void CloseMapAnimation()
    {
        // 阻止重复触发
        if (isAnimating) return;
        isAnimating = true;

        if (mainSequence != null && mainSequence.IsActive())
        {
            mainSequence.Kill();
        }

        // 把对话框等 UI 强制藏起来，保证画面纯净
        if (mapDialoguePanel != null) mapDialoguePanel.SetActive(false);
        if (dialoguePanel != null) dialoguePanel.SetActive(false);

        mainSequence = DOTween.Sequence();

        // 1. 如果要合上，卷轴遮挡卷边必须先显示出来
        if (coverLeft != null)
        {
            coverLeft.gameObject.SetActive(true);
            coverLeft.anchoredPosition = new Vector2(scrollLeftTargetX - coverLeft.rect.width, coverLeft.anchoredPosition.y);
        }
        if (coverRight != null)
        {
            coverRight.gameObject.SetActive(true);
            coverRight.anchoredPosition = new Vector2(scrollRightTargetX + coverRight.rect.width, coverRight.anchoredPosition.y);
        }

        // 播放收起音效（利用展开音效反用）
        if (audioSource != null && scrollOpenClip != null)
        {
            mainSequence.AppendCallback(() => audioSource.PlayOneShot(scrollOpenClip));
        }

        // 2. 轴心向中间合拢
        if (scrollLeft != null)
        {
            mainSequence.Join(
                scrollLeft.DOAnchorPosX(0f, scrollOpenDuration).SetEase(Ease.InOutQuad)
            );
        }
        if (scrollRight != null)
        {
            mainSequence.Join(
                scrollRight.DOAnchorPosX(0f, scrollOpenDuration).SetEase(Ease.InOutQuad)
            );
        }

        // 3. 卷帘边向中间合拢（盖住世界）
        if (coverLeft != null)
        {
            mainSequence.Join(
                coverLeft.DOAnchorPosX(0f, baseMapRevealDuration).SetEase(Ease.InOutQuad)
            );
        }
        if (coverRight != null)
        {
            mainSequence.Join(
                coverRight.DOAnchorPosX(0f, baseMapRevealDuration).SetEase(Ease.InOutQuad)
            );
        }

        // 4. 收拢结束后的最终闭幕（黑屏或退出）
        mainSequence.OnComplete(() =>
        {
            isAnimating = false;
            
            // 下面是结束游戏的逻辑。因为已经是在谢幕了，合上后游戏就自动终止
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        });
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

    /// <summary>
    /// 播放开场对话序列：黑屏 -> 对话1 -> 对话2 -> 黑屏淡出 -> 进入主场景
    /// </summary>
    private System.Collections.IEnumerator PlayOpeningSequence()
    {
        // 1. 确保黑屏（再次确认）
        if (fadeOverlay != null)
        {
            fadeOverlay.alpha = 1f;
            fadeOverlay.blocksRaycasts = true;
        }

        // 2. 强制隐藏所有地图内容，确保黑屏期间看不到任何东西
        if (terrainGroup != null) terrainGroup.alpha = 0f;
        if (titleGroup != null) titleGroup.alpha = 0f;

        // 隐藏所有建筑
        for (int i = 0; i < buildings.Count; i++)
        {
            if (buildings[i].buildingRect != null)
            {
                buildings[i].buildingRect.localScale = Vector3.zero;
                CanvasGroup cg = buildings[i].buildingRect.GetComponent<CanvasGroup>();
                if (cg != null) cg.alpha = 0f;
            }
        }

        // 3. 隐藏卷轴轴心
        if (scrollLeft != null)
        {
            CanvasGroup leftGroup = scrollLeft.GetComponent<CanvasGroup>();
            if (leftGroup != null) leftGroup.alpha = 0f;
        }
        if (scrollRight != null)
        {
            CanvasGroup rightGroup = scrollRight.GetComponent<CanvasGroup>();
            if (rightGroup != null) rightGroup.alpha = 0f;
        }

        // 4. 显示对话面板
        if (dialoguePanel != null)
        {
            dialoguePanel.SetActive(true);
        }

        // 5. 依次播放对话
        for (int i = 0; i < openingDialogue.Length; i++)
        {
            if (dialogueText != null)
            {
                // 在“风吹过”这句开始打字时同步播放风声音效
                if (i == openingDialogue.Length - 1 && audioSource != null && windBlowClip != null)
                {
                    audioSource.PlayOneShot(windBlowClip);
                }

                yield return StartCoroutine(TypewriterEffect(openingDialogue[i]));

                yield return new WaitForSeconds(dialoguePauseDuration);
            }
        }

        // 6. 隐藏对话面板
        if (dialoguePanel != null)
        {
            dialoguePanel.SetActive(false);
        }

        // 7. 等待一小段时间
        yield return new WaitForSeconds(0.5f);

        // 8. 显示卷轴轴心（准备展开）
        if (scrollLeft != null)
        {
            CanvasGroup leftGroup = scrollLeft.GetComponent<CanvasGroup>();
            if (leftGroup != null) leftGroup.alpha = 1f;
        }
        if (scrollRight != null)
        {
            CanvasGroup rightGroup = scrollRight.GetComponent<CanvasGroup>();
            if (rightGroup != null) rightGroup.alpha = 1f;
        }

        // 9. 黑屏淡出，同时开始卷轴展开动画（两者同步进行，更流畅）
        if (fadeOverlay != null)
        {
            fadeOverlay.DOFade(0f, 2f).SetEase(Ease.InOutSine).OnComplete(() =>
            {
                fadeOverlay.blocksRaycasts = false;
            });
        }

        // 10. 延迟一小段时间后开始播放卷轴展开动画（让黑屏稍微先淡出一点）
        yield return new WaitForSeconds(0.3f);

        // 11. 开始播放卷轴展开动画
        PlayOpenSequence();
    }

    /// <summary>
    /// 打字机效果：逐字显示文本
    /// </summary>
    private System.Collections.IEnumerator TypewriterEffect(string text)
    {
        if (dialogueText == null) yield break;

        dialogueText.text = text;
        dialogueText.maxVisibleCharacters = 99999;
        dialogueText.ForceMeshUpdate(true);

        int totalCharacters = dialogueText.textInfo.characterCount;
        dialogueText.maxVisibleCharacters = 0;

        for (int i = 0; i <= totalCharacters; i++)
        {
            dialogueText.maxVisibleCharacters = i;
            yield return new WaitForSeconds(dialogueTypingSpeed);
        }

        dialogueText.maxVisibleCharacters = 99999;
    }

    /// <summary>
    /// 地图打开后的对话序列
    /// </summary>
    private System.Collections.IEnumerator PlayMapOpenDialogue()
    {
        // 等待一小段时间，让玩家先看清楚地图
        yield return new WaitForSeconds(0.5f);

        // 显示地图对话面板
        if (mapDialoguePanel != null)
        {
            mapDialoguePanel.SetActive(true);
        }

        // 依次播放对话
        for (int i = 0; i < mapOpenDialogue.Length; i++)
        {
            if (mapDialogueText != null)
            {
                yield return StartCoroutine(TypewriterEffectForMap(mapOpenDialogue[i]));
                yield return new WaitForSeconds(dialoguePauseDuration);
            }
        }

        // 所有对话播放完毕，隐藏对话面板并开始镜头引导
        if (mapDialoguePanel != null)
        {
            mapDialoguePanel.SetActive(false);
        }

        Debug.Log("【地图对话结束】准备镜头引导...");

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
    }

    /// <summary>
    /// 打字机效果：逐字显示文本（地图对话框专用）
    /// </summary>
    private System.Collections.IEnumerator TypewriterEffectForMap(string text)
    {
        if (mapDialogueText == null) yield break;

        mapDialogueText.text = text;
        mapDialogueText.maxVisibleCharacters = 99999;
        mapDialogueText.ForceMeshUpdate(true);

        int totalCharacters = mapDialogueText.textInfo.characterCount;
        mapDialogueText.maxVisibleCharacters = 0;

        for (int i = 0; i <= totalCharacters; i++)
        {
            mapDialogueText.maxVisibleCharacters = i;
            yield return new WaitForSeconds(dialogueTypingSpeed);
        }

        mapDialogueText.maxVisibleCharacters = 99999;
    }
}
