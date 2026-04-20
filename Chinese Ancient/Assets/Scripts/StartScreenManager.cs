using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;

public class StartScreenManager : MonoBehaviour
{
    [Header("Background Images")]
    public Sprite[] backgroundSprites;
    public RawImage backgroundRenderer;
    public float backgroundChangeInterval = 5f;

    [Header("UI Elements")]
    public Text titleText;
    public Button startButton;

    [Header("Scene Settings")]
    public string mainSceneName = "主场景";
    public int mainSceneBuildIndex = 1; // 在Build Settings中的索引

    [Header("Keyboard Settings")]
    public KeyCode startKey = KeyCode.Space; // 按空格键也可以开始游戏

    [Header("Audio Settings")]
    public AudioSource backgroundMusic; // 拖拽BackgroundMusic对象到这里
    public float fadeOutDuration = 2f; // 淡出时长（秒）

    private int currentBgIndex = 0;
    private Coroutine backgroundChangeCoroutine;
    private bool isTransitioning = false; // 防止重复触发

    void Start()
    {
        Debug.Log("StartScreenManager Start 开始执行");

        // 设置标题
        if (titleText != null)
        {
            titleText.text = "四方华构录";
            Debug.Log("标题已设置");
        }
        else
        {
            Debug.LogWarning("titleText 为 null！请在Inspector中设置Title对象");
        }

        // 设置按钮监听
        if (startButton != null)
        {
            startButton.onClick.AddListener(OnStartButtonClick);
            Debug.Log("按钮监听器已添加");
        }
        else
        {
            Debug.LogError("startButton 为 null！请在Inspector中设置StartButton对象");
        }

        // 开始背景图片轮播
        if (backgroundRenderer != null && backgroundSprites != null && backgroundSprites.Length > 0)
        {
            backgroundChangeCoroutine = StartCoroutine(ChangeBackground());
            Debug.Log("背景轮播已启动，图片数量: " + backgroundSprites.Length);
        }

        Debug.Log("StartScreenManager Start 执行完成");
    }

    void Update()
    {
        // 键盘备选方案：按空格键或回车键也可以开始游戏
        if (!isTransitioning && (Input.GetKeyDown(startKey) || Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter)))
        {
            Debug.Log("检测到键盘输入，触发开始游戏");
            LoadMainScene();
        }
    }

    IEnumerator ChangeBackground()
    {
        while (true)
        {
            if (backgroundRenderer != null && backgroundSprites != null && backgroundSprites.Length > 0)
            {
                backgroundRenderer.texture = backgroundSprites[currentBgIndex].texture;
                currentBgIndex = (currentBgIndex + 1) % backgroundSprites.Length;
            }
            yield return new WaitForSeconds(backgroundChangeInterval);
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

    void OnDestroy()
    {
        if (backgroundChangeCoroutine != null)
        {
            StopCoroutine(backgroundChangeCoroutine);
        }
    }
}
