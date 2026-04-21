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

        // 初始化屏风浮动数据
        if (floatingPanels != null && floatingPanels.Length > 0)
        {
            panelTimeOffsets = new float[floatingPanels.Length];
            panelStartPos = new Vector2[floatingPanels.Length];
            for (int i = 0; i < floatingPanels.Length; i++)
            {
                if (floatingPanels[i] != null)
                {
                    panelStartPos[i] = floatingPanels[i].anchoredPosition;
                    // 让每个屏风的起伏错开，营造参差错落的动感
                    panelTimeOffsets[i] = Random.Range(0f, Mathf.PI * 2f);
                }
            }
        }

        // 初始化主Logo位置
        if (logoTransform != null)
        {
            logoStartPos = logoTransform.anchoredPosition;
        }

        // 初始化雾气位置
        if (fogTransform != null)
        {
            fogStartPos = fogTransform.anchoredPosition;
        }

        // 设置按钮监听
        if (startButton != null)
        {
            startButton.onClick.AddListener(OnStartButtonClick);
        }
    }

    void Update()
    {
        // 键盘备选方案：按空格键或回车键也可以开始游戏
        if (!isTransitioning && (Input.GetKeyDown(startKey) || Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter)))
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
        // 1. 屏风上下浮动
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

        // 2. 主Logo超缓慢上下浮动 (不再缩放，只缓慢浮动以防晕眩)
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

            // 控制透明度变化产生类似于呼吸发光的效果
            float glowAlpha = Mathf.Lerp(minGlow, maxGlow, (Mathf.Sin(Time.time * glowSpeed) + 1f) / 2f);
            logoGlowCanvasGroup.alpha = glowAlpha;
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
