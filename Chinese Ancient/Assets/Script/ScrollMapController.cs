using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using DG.Tweening;
using System.Collections.Generic;
using TMPro;
public class SimpleClickListener : MonoBehaviour, IPointerClickHandler, IPointerDownHandler
{
    public System.Action onClick;
    public void OnPointerDown(PointerEventData eventData)
    {
        Debug.Log($"【底层点击】鼠标按下了物体: {gameObject.name}");
    }
    // 监听鼠标真正完成"点击"的瞬间
    public void OnPointerClick(PointerEventData eventData)
    {
        Debug.Log($"【底层点击】成功在物体上完成了点击: {gameObject.name}");
        onClick?.Invoke();
    }
}
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
        public Sprite bwSprite;
        // 运行时动态创建的黑白图层CanvasGroup
        [HideInInspector]
        public CanvasGroup runtimeBWGroup;
    }
    [Header("══ 卷轴轴心 ══")]
    [SerializeField] private RectTransform scrollLeft;
    [SerializeField] private RectTransform scrollRight;
    [SerializeField] private float scrollOpenDuration = 3.0f;
    [SerializeField] private float scrollLeftTargetX = -800f;
    [SerializeField] private float scrollRightTargetX = 800f;
    [Header("══ 遮挡板（控制底图展开的速度）══")]
    [SerializeField] private float baseMapRevealDuration = 3.0f;
    [SerializeField] private RectTransform coverLeft;
    [SerializeField] private RectTransform coverRight;
    [Header("══ 地形 ══")]
    [SerializeField] private CanvasGroup terrainGroup;
    [SerializeField] private float terrainFadeDuration = 0.5f;
    [Header("══ 建筑列表 ══")]
    [SerializeField] private List<BuildingEntry> buildings = new List<BuildingEntry>();
    [SerializeField] private float buildingGlobalStartTime = 2.0f;
    [SerializeField] private float buildingScaleDuration = 0.6f;
    [SerializeField] private float buildingFadeDuration = 0.4f;
    [SerializeField] private float buildingInterval = 0.3f;
    [Header("══ 解锁设置 ══")]
    [SerializeField] private Color lockedBuildingColor = new Color(0.6f, 0.6f, 0.6f, 1f);
    [SerializeField] private float lockedColorFadeDuration = 1.0f;
    [Header("══ 标题（可选）══")]
    [SerializeField] private CanvasGroup titleGroup;
    [SerializeField] private float titleFadeDuration = 0.8f;
    [Header("══ 镜头引导（动画结束后建筑呼吸提示）══")]
    [SerializeField] private int guideTargetIndex = 0;
    [SerializeField] private float guidePulseScale = 1.03f;
    [SerializeField] private bool enableGuide = true;
    [Header("══ 过渡遮罩（可选）══")]
    [SerializeField] private Image fadeOverlayImage;
    [SerializeField] private float sceneFadeDuration = 0.5f;
    [Header("══ 玩家心路独白控制器 ══")]
    [SerializeField] private MapMonologueController monologueController;
    [Header("══ 音效（可选）══")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip scrollOpenClip;
    [SerializeField] private AudioClip buildingAppearClip;
    [SerializeField] private AudioClip windBlowClip;
    [Header("══ 动画控制 ══")]
    public bool playOpenAnimation = true;
    [Header("══ 进度控制 ══")]
    public bool overrideAsFirstLevel = true;
    //标记本游戏运行期间，是否已经播放过一次主场景的打开动画
    public static bool hasPlayedGlobalMapAnimation = false;
    private Sequence mainSequence;
    private DG.Tweening.Tweener guidePulseTween;
    private bool isAnimating = true;
    private bool isTransitioning = false;
    private CanvasGroup fadeOverlay;
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
        if (!playOpenAnimation || hasPlayedGlobalMapAnimation)
        {
            InstantOpenMap();
        }
    }
    private void Start()
    {
        InitializeUI();
        if (playOpenAnimation && !hasPlayedGlobalMapAnimation)
        {
            StartCoroutine(PlayOpeningSequence()); 
            hasPlayedGlobalMapAnimation = true; 
        }
        else
        {
            if (fadeOverlay != null)
            {
                fadeOverlay.alpha = 0f;
                fadeOverlay.blocksRaycasts = false;
            }
            if (monologueController != null)
            {
                if (monologueController.openingPanel != null) monologueController.openingPanel.SetActive(false);
                if (monologueController.mapOpenPanel != null) monologueController.mapOpenPanel.SetActive(false);
            }
            InstantOpenMap();
        }
    }
    private void InstantOpenMap()
    {
        isAnimating = false; 
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
        if (coverLeft != null)
        {
            float coverLeftExtra = coverLeft.rect.width;
            coverLeft.anchoredPosition = new Vector2(scrollLeftTargetX - coverLeftExtra, coverLeft.anchoredPosition.y);
            coverLeft.gameObject.SetActive(false); 
        }
        if (coverRight != null)
        {
            float coverRightExtra = coverRight.rect.width;
            coverRight.anchoredPosition = new Vector2(scrollRightTargetX + coverRightExtra, coverRight.anchoredPosition.y);
            coverRight.gameObject.SetActive(false); 
        }
        if (terrainGroup != null) terrainGroup.alpha = 1f;
        if (titleGroup != null) titleGroup.alpha = 1f;
        int unlockedLevel = overrideAsFirstLevel ? 0 : 0;
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
                    b.runtimeBWGroup.alpha = isUnlocked ? 0f : 1f;
                    if (img != null)
                    {
                        img.color = isUnlocked ? Color.white : new Color(1, 1, 1, 0f);
                    }
                }
                else if (img != null)
                {
                    img.color = isUnlocked ? Color.white : lockedBuildingColor;
                }
            }
        }
    }
    private void EnsureBWOverlay(BuildingEntry b)
    {
        if (b.bwSprite == null || b.runtimeBWGroup != null) return;
        GameObject bwObj = new GameObject("BW_Overlay");
        RectTransform rect = bwObj.AddComponent<RectTransform>();
        rect.SetParent(b.buildingRect, false);
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        rect.localScale = Vector3.one;
        Image bwImg = bwObj.AddComponent<Image>();
        bwImg.sprite = b.bwSprite;
        bwImg.raycastTarget = false; 
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
        if (audioSource == null && (scrollOpenClip != null || buildingAppearClip != null || windBlowClip != null))
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
            }
            audioSource.playOnAwake = false;
        }
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
        if (terrainGroup != null)
        {
            terrainGroup.alpha = 0f;
            terrainGroup.gameObject.SetActive(true);
            Image[] imgs = terrainGroup.GetComponentsInChildren<Image>();
            foreach (var img in imgs) img.raycastTarget = false;
        }
        if (coverLeft != null && coverLeft.GetComponent<Image>() != null) coverLeft.GetComponent<Image>().raycastTarget = false;
        if (coverRight != null && coverRight.GetComponent<Image>() != null) coverRight.GetComponent<Image>().raycastTarget = false;
        int unlockedLevel = 0;
        for (int i = 0; i < buildings.Count; i++)
        {
            var b = buildings[i];
            if (b.buildingRect == null) continue;
            EnsureBWOverlay(b);
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
                img.color = Color.white;
            }
            if (b.runtimeBWGroup != null)
            {
                b.runtimeBWGroup.alpha = 0f;
            }
            Button oldBtn = b.buildingRect.GetComponent<Button>();
            if (oldBtn != null) Destroy(oldBtn);
            SimpleClickListener listener = b.buildingRect.GetComponent<SimpleClickListener>();
            if (listener == null)
            {
                listener = b.buildingRect.gameObject.AddComponent<SimpleClickListener>();
            }
            int index = i;
            // AI辅助生成：DeepSeek-R1-0528, 2026-04-23
            listener.onClick = () =>
            {
                int currentUnlocked = 0;
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
        if (fadeOverlayImage != null)
        {
            fadeOverlay = fadeOverlayImage.GetComponent<CanvasGroup>();
            if (fadeOverlay == null)
            {
                fadeOverlay = fadeOverlayImage.gameObject.AddComponent<CanvasGroup>();
            }
        }
    }
    // AI辅助生成：DeepSeek-R1-0528, 2026-04-23
    private void PlayOpenSequence()
    {
        mainSequence = DOTween.Sequence();
        if (audioSource != null && scrollOpenClip != null)
            mainSequence.AppendCallback(() => audioSource.PlayOneShot(scrollOpenClip));
        if (scrollLeft != null)
        {
            mainSequence.Join(
                scrollLeft.DOAnchorPosX(scrollLeftTargetX, scrollOpenDuration)
                    .SetEase(Ease.Linear)
            );
        }
        if (scrollRight != null)
        {
            mainSequence.Join(
                scrollRight.DOAnchorPosX(scrollRightTargetX, scrollOpenDuration)
                    .SetEase(Ease.Linear)
            );
        }
        if (coverLeft != null)
        {
            float coverLeftExtra = coverLeft.rect.width;
            mainSequence.Join(
                coverLeft.DOAnchorPosX(scrollLeftTargetX - coverLeftExtra, baseMapRevealDuration)
                    .SetEase(Ease.Linear)
            );
        }
        if (coverRight != null)
        {
            float coverRightExtra = coverRight.rect.width;
            mainSequence.Join(
                coverRight.DOAnchorPosX(scrollRightTargetX + coverRightExtra, baseMapRevealDuration)
                    .SetEase(Ease.Linear)
            );
        }

        mainSequence.Insert(
            0.5f, 
            terrainGroup.DOFade(1f, terrainFadeDuration).SetEase(Ease.OutCubic)
        );
        Debug.Log($"【动画执行】准备浮现建筑！系统检测到你在 Inspector 里配置了 {buildings.Count} 个建筑。");
        float currentDelay = 0f; 
        for (int i = 0; i < buildings.Count; i++)
        {
            BuildingEntry entry = buildings[i];
            if (entry.buildingRect == null)
            {
                Debug.LogWarning($"第 {i} 个建筑的图片没有拖进去！被跳过了！");
                continue;
            }

            if (i > 0)
            {
                if (entry.appearDelay > 0) currentDelay += entry.appearDelay;
                else currentDelay += buildingInterval;
            }
            CanvasGroup cg = GetOrAddCanvasGroup(entry.buildingRect);

            entry.buildingRect.gameObject.SetActive(true);
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

            float floatTime = buildingGlobalStartTime + currentDelay;
            string curName = entry.buildingName; 
            mainSequence.InsertCallback(floatTime, () =>
            {
                Debug.Log($"【建筑弹出】★★★ 建筑 '{curName}' 此时此刻正在弹出！(Scale & Fade 同时进行中)");
            });
            if (audioSource != null && buildingAppearClip != null)
            {
                mainSequence.InsertCallback(floatTime, () => audioSource.PlayOneShot(buildingAppearClip));
            }
        }
        mainSequence.AppendInterval(0.3f);
        if (titleGroup != null)
        {
            mainSequence.Append(titleGroup.DOFade(1f, titleFadeDuration).SetEase(Ease.OutCubic));
        }
        mainSequence.AppendCallback(() =>
        {
            if (coverLeft != null) coverLeft.gameObject.SetActive(false);
            if (coverRight != null) coverRight.gameObject.SetActive(false);
            Debug.Log("【动画结束】开场动画播完，所有建筑已弹出，现在开始将未解锁场景褪色变灰，并准备播放对话...");
            StartCoroutine(PlayMapOpenDialogue());
            int unlockedLevel = 0;
            for (int i = 0; i < buildings.Count; i++)
            {
                var entry = buildings[i];
                if (i > unlockedLevel && entry.buildingRect != null)
                {
                    if (entry.runtimeBWGroup != null)
                    {
                        entry.runtimeBWGroup.DOFade(1f, lockedColorFadeDuration).SetEase(Ease.InOutQuad);
                        Image img = entry.buildingRect.GetComponent<Image>();
                        if (img != null)
                        {
                            img.DOFade(0f, lockedColorFadeDuration).SetEase(Ease.InOutQuad);
                        }
                    }
                    else
                    {
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
    private void PlayGuideAnimation(System.Action onComplete)
    {
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
        guidePulseTween?.Kill();
        // 主场景写死 0 级放大恢复
        int currentUnlockedMax = 0;
        int throbbingTargetIndex = Mathf.Min(currentUnlockedMax, buildings.Count - 1);
        if (throbbingTargetIndex >= 0 && throbbingTargetIndex < buildings.Count && buildings[throbbingTargetIndex].buildingRect != null)
            buildings[throbbingTargetIndex].buildingRect.localScale = Vector3.one;
        if (index < 0 || index >= buildings.Count) return;
        string sceneName = buildings[index].targetSceneName;
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
        Time.timeScale = 1f; 
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
    private System.Collections.IEnumerator DeferredLoadScene(string targetScene)
    {
        yield return null;
        targetScene = targetScene.Trim();
        Debug.Log($"【全军出击】正式装载目标场景: {targetScene}");
        try
        {
            SceneManager.LoadScene(targetScene);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"【致命错误】加载场景 {targetScene} 失败了！！！请检查 File -> Build Settings 里是否添加了这个场景！详细报错：\n{e.Message}");
        }
    }
    public void SkipAnimation()
    {
        if (!isAnimating) return;
        mainSequence?.Complete(true);
        isAnimating = false;
    }
    public void CloseMapAnimation()
    {
        if (isAnimating) return;
        isAnimating = true;
        if (mainSequence != null && mainSequence.IsActive())
        {
            mainSequence.Kill();
        }
        mainSequence = DOTween.Sequence();
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
        if (audioSource != null && scrollOpenClip != null)
        {
            mainSequence.AppendCallback(() => audioSource.PlayOneShot(scrollOpenClip));
        }
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
        mainSequence.OnComplete(() =>
        {
            isAnimating = false;
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        });
    }
    private void Update()
    {
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
    private System.Collections.IEnumerator PlayOpeningSequence()
    {
        if (fadeOverlay != null)
        {
            fadeOverlay.alpha = 1f;
            fadeOverlay.blocksRaycasts = true;
        }
        if (terrainGroup != null) terrainGroup.alpha = 0f;
        if (titleGroup != null) titleGroup.alpha = 0f;
        for (int i = 0; i < buildings.Count; i++)
        {
            if (buildings[i].buildingRect != null)
            {
                buildings[i].buildingRect.localScale = Vector3.zero;
                CanvasGroup cg = buildings[i].buildingRect.GetComponent<CanvasGroup>();
                if (cg != null) cg.alpha = 0f;
            }
        }
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
        if (monologueController != null)
        {
            monologueController.audioSource = this.audioSource;
            monologueController.windBlowClip = this.windBlowClip;
            yield return StartCoroutine(monologueController.PlayOpeningSequence());
        }
        yield return new WaitForSeconds(0.5f);
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
        if (fadeOverlay != null)
        {
            fadeOverlay.DOFade(0f, 2f).SetEase(Ease.InOutSine).OnComplete(() =>
            {
                fadeOverlay.blocksRaycasts = false;
            });
        }
        yield return new WaitForSeconds(0.3f);
        PlayOpenSequence();
    }
    private System.Collections.IEnumerator PlayMapOpenDialogue()
    {
        yield return new WaitForSeconds(0.5f);
        if (monologueController != null)
        {
            yield return StartCoroutine(monologueController.PlayMapOpenSequence());
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
}
