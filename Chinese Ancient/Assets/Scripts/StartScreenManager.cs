using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;

public class StartScreenManager : MonoBehaviour
{
    [Header("Floating Panels (四联屏风)")]
    public RectTransform[] floatingPanels; // 拖入四个屏风的RectTransform
    public float floatSpeed = 0.5f;        // 浮动速度 (调慢防晕)
    public float floatAmplitude = 8f;      // 浮动幅度 (减小)
    private float[] panelTimeOffsets;      // 记录每个屏风的随机初始时间，错开浮动节奏
    private Vector2[] panelStartPos;       // 记录屏风初始位置

    [Header("Logo Effect (主Logo悬浮)")]
    public RectTransform logoTransform;    // 拖入清晰的主Logo
    public float logoFloatSpeed = 0.6f;    // 悬浮速度(超缓慢)
    public float logoFloatRange = 6f;      // 悬浮幅度
    private Vector2 logoStartPos;

    [Header("Logo Glow (模糊发光层)")]
    public CanvasGroup logoGlowCanvasGroup; // 拖入模糊处理过的Logo的CanvasGroup
    public float glowSpeed = 1.2f;          // 泛光呼吸速度
    public float minGlow = 0.3f;            // 最小亮度
    public float maxGlow = 0.9f;            // 最大亮度

    [Header("Entrance Animation (自定义出场动画顺序)")]
    public GameObject clearBackgroundObj;     // 拖入清晰的底图(背后请垫一张常亮的模糊底图防穿帮)
    public GameObject[] customFadeSequence;   // 自定义任意数量、任意顺序的元素（屏风、装饰等），依次显现
    public float bgFadeDuration = 1.5f;       // 背景渐入时间
    public float elementFadeDuration = 1.0f;  // 单个自定义元素渐入时间
    public float elementStaggerTime = 0.4f;   // 元素之间分别出现的间隔时间
    public float logoFadeDuration = 2.0f;     // Logo渐入时间

    private CanvasGroup bgCanvasGroup;
    private CanvasGroup[] sequenceCanvasGroups;
    private CanvasGroup logoMainCanvasGroup;
    private CanvasGroup startBtnCanvasGroup;
    private float glowMultiplier = 0f;        // 初始Logo泛光遮罩为0
    private bool isEntranceDone = false;      // 出场动画是否完成

    [Header("Fog Effect (雾气游动 - 可选)")]
    public RectTransform fogTransform;     // 雾气图层
    public float fogMoveSpeed = 10f;       // 雾气平移速度
    public float fogMoveRange = 50f;       // 雾气平移范围
    private Vector2 fogStartPos;

    [Header("UI Elements")]
    public Button startButton;

    [Header("Scene Settings")]
    public string mainSceneName = "主场景";
    public int mainSceneBuildIndex = 1; // 在Build Settings中的索引

    [Header("Keyboard Settings")]
    public KeyCode startKey = KeyCode.Space; // 按空格键也可以开始游戏

    [Header("Audio Settings")]
    public AudioSource backgroundMusic; // 拖拽BackgroundMusic对象到这里
    public float fadeOutDuration = 2f; // 淡出时长（秒）

    private bool isTransitioning = false; // 防止重复触发

    void Start()
    {
        Debug.Log("StartScreenManager Start 开始执行");

        // 初始化屏风浮动数据 (仅做悬浮，不管出场透明度)
        if (floatingPanels != null && floatingPanels.Length > 0)
        {
            panelTimeOffsets = new float[floatingPanels.Length];
            panelStartPos = new Vector2[floatingPanels.Length];

            for (int i = 0; i < floatingPanels.Length; i++)
            {
                if (floatingPanels[i] != null)
                {
                    panelStartPos[i] = floatingPanels[i].anchoredPosition;
                    panelTimeOffsets[i] = Random.Range(0f, Mathf.PI * 2f);
                }
            }
        }

        // 初始化清晰背景开场透明 (底部的模糊背景请保持常亮，不要拖入脚本)
        if (clearBackgroundObj != null)
        {
            bgCanvasGroup = GetOrAddCanvasGroup(clearBackgroundObj);
            if (bgCanvasGroup != null) bgCanvasGroup.alpha = 0f;
        }

        // 初始化自定义出场序列的透明度
        if (customFadeSequence != null && customFadeSequence.Length > 0)
        {
            sequenceCanvasGroups = new CanvasGroup[customFadeSequence.Length];
            for (int i = 0; i < customFadeSequence.Length; i++)
            {
                if (customFadeSequence[i] != null)
                {
                    sequenceCanvasGroups[i] = GetOrAddCanvasGroup(customFadeSequence[i]);
                    if (sequenceCanvasGroups[i] != null) sequenceCanvasGroups[i].alpha = 0f;
                }
            }
        }

        // 初始化主Logo位置及开场动画组件
        if (logoTransform != null)
        {
            logoStartPos = logoTransform.anchoredPosition;
            logoMainCanvasGroup = GetOrAddCanvasGroup(logoTransform.gameObject);
            if (logoMainCanvasGroup != null) logoMainCanvasGroup.alpha = 0f; // 初始全透明
        }

        if (logoGlowCanvasGroup != null)
        {
            logoGlowCanvasGroup.alpha = 0f;
        }

        // 初始化雾气位置
        if (fogTransform != null)
        {
            fogStartPos = fogTransform.anchoredPosition;
        }

        // 设置按钮监听与开场透明
        if (startButton != null)
        {
            startButton.onClick.AddListener(OnStartButtonClick);
            startBtnCanvasGroup = GetOrAddCanvasGroup(startButton.gameObject);
            if (startBtnCanvasGroup != null) startBtnCanvasGroup.alpha = 0f;
            startButton.interactable = false; // 动画期间禁止点击
        }

        // 启动出场串联动画序列
        StartCoroutine(EntranceSequence());
    }

    private CanvasGroup GetOrAddCanvasGroup(GameObject obj)
    {
        if (obj == null) return null;
        CanvasGroup cg = obj.GetComponent<CanvasGroup>();
        if (cg == null) cg = obj.AddComponent<CanvasGroup>();
        return cg;
    }

    IEnumerator EntranceSequence()
    {
        isEntranceDone = false;
        yield return new WaitForSeconds(0.5f); // 开场后留白半秒缓冲

        // 1. 背景图首先如画卷般渐入展开
        if (bgCanvasGroup != null)
        {
            yield return StartCoroutine(FadeCanvasGroup(bgCanvasGroup, 0f, 1f, bgFadeDuration));
        }

        // 2. 自定义序列组件依次显形出场
        if (sequenceCanvasGroups != null)
        {
            for (int i = 0; i < sequenceCanvasGroups.Length; i++)
            {
                if (sequenceCanvasGroups[i] != null)
                {
                    StartCoroutine(FadeCanvasGroup(sequenceCanvasGroups[i], 0f, 1f, elementFadeDuration));
                    yield return new WaitForSeconds(elementStaggerTime); // 控制一个个出场的时间差
                }
            }
            // 稍等一会儿，让所有元素基本上浮现实体
            yield return new WaitForSeconds(elementFadeDuration * 0.5f);
        }

        // 3. Logo与发光层伴着仙气最后显现出场
        if (logoMainCanvasGroup != null)
        {
            StartCoroutine(FadeCanvasGroup(logoMainCanvasGroup, 0f, 1f, logoFadeDuration));
        }

        StartCoroutine(FadeGlowMultiplier(0f, 1f, logoFadeDuration)); // 同步开启发光层呼吸

        yield return new WaitForSeconds(logoFadeDuration * 0.8f);

        // 4. 开始按钮最后微微软隐浮现
        if (startBtnCanvasGroup != null)
        {
            StartCoroutine(FadeCanvasGroup(startBtnCanvasGroup, 0f, 1f, 1.0f));
            if (startButton != null) startButton.interactable = true; // 终于允许玩家猛点啦！
        }

        isEntranceDone = true;
    }

    IEnumerator FadeCanvasGroup(CanvasGroup cg, float startAlpha, float endAlpha, float duration)
    {
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            if (cg != null) cg.alpha = Mathf.Lerp(startAlpha, endAlpha, t / duration);
            yield return null;
        }
        if (cg != null) cg.alpha = endAlpha;
    }

    IEnumerator FadeGlowMultiplier(float start, float end, float duration)
    {
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            glowMultiplier = Mathf.Lerp(start, end, t / duration);
            yield return null;
        }
        glowMultiplier = end;
    }

    void Update()
    {
        // 键盘备选方案：按空格键或回车键也可以开始游戏(要在开场动画后才能按，以防断点)
        if (!isTransitioning && isEntranceDone && (Input.GetKeyDown(startKey) || Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter)))
        {
            LoadMainScene();
        }

        if (!isTransitioning)
        {
            AnimateUI();
        }
    }

    void AnimateUI()
    {
        // 1. 屏风上下浮动 (浮动特效不受透明度出场影响！随时都在唯美漂浮)
        if (floatingPanels != null && panelStartPos != null)
        {
            for (int i = 0; i < floatingPanels.Length; i++)
            {
                if (floatingPanels[i] != null)
                {
                    float newY = panelStartPos[i].y + Mathf.Sin(Time.time * floatSpeed + panelTimeOffsets[i]) * floatAmplitude;
                    floatingPanels[i].anchoredPosition = new Vector2(panelStartPos[i].x, newY);
                }
            }
        }

        // 2. 主Logo超缓慢上下浮动 (与屏风错落开来继续独立悬浮)
        if (logoTransform != null)
        {
            float newLogoY = logoStartPos.y + Mathf.Sin(Time.time * logoFloatSpeed) * logoFloatRange;
            logoTransform.anchoredPosition = new Vector2(logoStartPos.x, newLogoY);
        }

        // 3. 模糊Logo发光层 (控制透明度闪烁)
        if (logoGlowCanvasGroup != null)
        {
            // 发光层保持与主Logo一样的浮动步伐
            if (logoTransform != null)
            {
                logoGlowCanvasGroup.transform.position = logoTransform.position;
            }

            // 控制呼吸发光，乘上出场动画控制的glowMultiplier，使开场时不出戏平铺
            float glowAlpha = Mathf.Lerp(minGlow, maxGlow, (Mathf.Sin(Time.time * glowSpeed) + 1f) / 2f);
            logoGlowCanvasGroup.alpha = glowAlpha * glowMultiplier;
        }

        // 4. 雾气轻微水平游动
        if (fogTransform != null)
        {
            float newX = fogStartPos.x + Mathf.Sin(Time.time * fogMoveSpeed * 0.1f) * fogMoveRange;
            fogTransform.anchoredPosition = new Vector2(newX, fogStartPos.y);
        }
    }

    void OnStartButtonClick()
    {
        if (!isTransitioning)
        {
            Debug.Log("=================== 开始游戏按钮被点击！===================");
            LoadMainScene();
        }
    }

    void LoadMainScene()
    {
        if (isTransitioning) return; // 防止重复触发
        isTransitioning = true;

        // 禁用按钮防止重复点击
        if (startButton != null)
        {
            startButton.interactable = false;
        }

        // 首先列出Build Settings中的所有场景
        Debug.Log("当前Build Settings中的场景数量: " + SceneManager.sceneCountInBuildSettings);
        for (int i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
        {
            string scenePath = SceneUtility.GetScenePathByBuildIndex(i);
            string sceneName = System.IO.Path.GetFileNameWithoutExtension(scenePath);
            Debug.Log("  场景索引 " + i + ": " + sceneName + " (完整路径: " + scenePath + ")");
        }

        // 检查场景是否存在
        bool sceneExists = SceneExistsInBuild(mainSceneName);
        Debug.Log("场景是否存在: " + sceneExists);

        if (sceneExists || (mainSceneBuildIndex >= 0 && mainSceneBuildIndex < SceneManager.sceneCountInBuildSettings))
        {
            // 如果有背景音乐，启动淡出效果
            if (backgroundMusic != null && backgroundMusic.isPlaying)
            {
                Debug.Log("开始淡出背景音乐，时长: " + fadeOutDuration + "秒");
                StartCoroutine(FadeOutAndLoadScene());
            }
            else
            {
                Debug.Log("没有背景音乐或音乐未播放，直接加载场景");
                LoadSceneNow();
            }
        }
        else
        {
            Debug.LogError("✗ 无法加载场景！");
            Debug.LogError("请确保 '" + mainSceneName + "' 已添加到Build Settings中。");
            Debug.LogError("操作步骤：File > Build Settings，然后将主场景拖入Scenes In Build列表");
            isTransitioning = false;
            if (startButton != null)
            {
                startButton.interactable = true;
            }
        }
    }

    IEnumerator FadeOutAndLoadScene()
    {
        float startVolume = backgroundMusic.volume;
        float timer = 0f;

        Debug.Log("音乐淡出开始，当前音量: " + startVolume);

        // 逐渐降低音量
        while (timer < fadeOutDuration)
        {
            timer += Time.deltaTime;
            float newVolume = Mathf.Lerp(startVolume, 0f, timer / fadeOutDuration);
            backgroundMusic.volume = newVolume;
            yield return null;
        }

        // 确保音量为0
        backgroundMusic.volume = 0f;
        Debug.Log("音乐淡出完成，音量已降至0");

        // 停止音乐
        backgroundMusic.Stop();

        // 加载场景
        LoadSceneNow();
    }

    void LoadSceneNow()
    {
        Debug.Log("开始加载场景: " + mainSceneName);

        // 尝试使用场景名称加载
        if (SceneExistsInBuild(mainSceneName))
        {
            SceneManager.LoadScene(mainSceneName);
        }
        // 使用Build索引加载
        else if (mainSceneBuildIndex >= 0 && mainSceneBuildIndex < SceneManager.sceneCountInBuildSettings)
        {
            SceneManager.LoadScene(mainSceneBuildIndex);
        }

        Debug.Log("=================== 场景加载指令已发送 ===================");
    }

    // 检查场景是否在Build Settings中
    private bool SceneExistsInBuild(string sceneName)
    {
        for (int i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
        {
            string scenePath = SceneUtility.GetScenePathByBuildIndex(i);
            string sceneNameFromPath = System.IO.Path.GetFileNameWithoutExtension(scenePath);
            if (sceneNameFromPath == sceneName)
            {
                return true;
            }
        }
        return false;
    }
}
