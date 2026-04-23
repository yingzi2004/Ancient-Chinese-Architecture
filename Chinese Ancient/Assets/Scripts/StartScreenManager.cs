using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;

public class StartScreenManager : MonoBehaviour
{
    [Header("Floating Panels (四联屏风)")]
    public RectTransform[] floatingPanels; 
    public float floatSpeed = 0.5f;        
    public float floatAmplitude = 8f;      
    private float[] panelTimeOffsets;      
    private Vector2[] panelStartPos;     

    [Header("Logo Effect (主Logo悬浮)")]
    public RectTransform logoTransform;   
    public float logoFloatSpeed = 0.6f;   
    public float logoFloatRange = 6f;      
    private Vector2 logoStartPos;

    [Header("Logo Glow (模糊发光层)")]
    public CanvasGroup logoGlowCanvasGroup;
    public float glowSpeed = 1.2f;          
    public float minGlow = 0.3f;           
    public float maxGlow = 0.9f;            

    [Header("Entrance Animation (自定义出场动画顺序)")]
    public GameObject clearBackgroundObj;     
    public GameObject[] customFadeSequence;   
    public float bgFadeDuration = 1.5f;       
    public float elementFadeDuration = 1.0f;  
    public float elementStaggerTime = 0.4f;   
    public float logoFadeDuration = 2.0f;     

    private CanvasGroup bgCanvasGroup;
    private CanvasGroup[] sequenceCanvasGroups;
    private CanvasGroup logoMainCanvasGroup;
    private CanvasGroup startBtnCanvasGroup;
    private float glowMultiplier = 0f;        
    private bool isEntranceDone = false;      

    [Header("Fog Effect (雾气游动 - 可选)")]
    public RectTransform fogTransform;     
    public float fogMoveSpeed = 10f;       
    public float fogMoveRange = 50f;       
    private Vector2 fogStartPos;

    [Header("UI Elements")]
    public Button startButton;

    [Header("Scene Settings")]
    public string mainSceneName = "主场景";
    public int mainSceneBuildIndex = 1; 

    [Header("Keyboard Settings")]
    public KeyCode startKey = KeyCode.Space; 
    [Header("Audio Settings")]
    public AudioSource backgroundMusic; 
    public float fadeOutDuration = 2f; 

    private bool isTransitioning = false; 

    void Start()
    {
        Debug.Log("StartScreenManager Start 开始执行");

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

        if (clearBackgroundObj != null)
        {
            bgCanvasGroup = GetOrAddCanvasGroup(clearBackgroundObj);
            if (bgCanvasGroup != null) bgCanvasGroup.alpha = 0f;
        }

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

        if (fogTransform != null)
        {
            fogStartPos = fogTransform.anchoredPosition;
        }

        if (startButton != null)
        {
            startButton.onClick.AddListener(OnStartButtonClick);
            startBtnCanvasGroup = GetOrAddCanvasGroup(startButton.gameObject);
            if (startBtnCanvasGroup != null) startBtnCanvasGroup.alpha = 0f;
            startButton.interactable = false; 
        }

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
        yield return new WaitForSeconds(0.5f); 

        if (bgCanvasGroup != null)
        {
            yield return StartCoroutine(FadeCanvasGroup(bgCanvasGroup, 0f, 1f, bgFadeDuration));
        }

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
            yield return new WaitForSeconds(elementFadeDuration * 0.5f);
        }

        if (logoMainCanvasGroup != null)
        {
            StartCoroutine(FadeCanvasGroup(logoMainCanvasGroup, 0f, 1f, logoFadeDuration));
        }

        StartCoroutine(FadeGlowMultiplier(0f, 1f, logoFadeDuration)); // 同步开启发光层呼吸

        yield return new WaitForSeconds(logoFadeDuration * 0.8f);

        if (startBtnCanvasGroup != null)
        {
            StartCoroutine(FadeCanvasGroup(startBtnCanvasGroup, 0f, 1f, 1.0f));
            if (startButton != null) startButton.interactable = true; 
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

        if (logoTransform != null)
        {
            float newLogoY = logoStartPos.y + Mathf.Sin(Time.time * logoFloatSpeed) * logoFloatRange;
            logoTransform.anchoredPosition = new Vector2(logoStartPos.x, newLogoY);
        }

        if (logoGlowCanvasGroup != null)
        {
            // AI辅助生成：DeepSeek-R1-0528, 2026-04-23
            if (logoTransform != null)
            {
                logoGlowCanvasGroup.transform.position = logoTransform.position;
            }

            float glowAlpha = Mathf.Lerp(minGlow, maxGlow, (Mathf.Sin(Time.time * glowSpeed) + 1f) / 2f);
            logoGlowCanvasGroup.alpha = glowAlpha * glowMultiplier;
        }

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
        if (isTransitioning) return; 
        isTransitioning = true;

        // 禁用按钮防止重复点击
        if (startButton != null)
        {
            startButton.interactable = false;
        }

        Debug.Log("当前Build Settings中的场景数量: " + SceneManager.sceneCountInBuildSettings);
        for (int i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
        {
            string scenePath = SceneUtility.GetScenePathByBuildIndex(i);
            string sceneName = System.IO.Path.GetFileNameWithoutExtension(scenePath);
            Debug.Log("  场景索引 " + i + ": " + sceneName + " (完整路径: " + scenePath + ")");
        }

        bool sceneExists = SceneExistsInBuild(mainSceneName);
        Debug.Log("场景是否存在: " + sceneExists);

        if (sceneExists || (mainSceneBuildIndex >= 0 && mainSceneBuildIndex < SceneManager.sceneCountInBuildSettings))
        {
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

        while (timer < fadeOutDuration)
        {
            timer += Time.deltaTime;
            float newVolume = Mathf.Lerp(startVolume, 0f, timer / fadeOutDuration);
            backgroundMusic.volume = newVolume;
            yield return null;
        }

        backgroundMusic.volume = 0f;
        Debug.Log("音乐淡出完成，音量已降至0");

        backgroundMusic.Stop();


        LoadSceneNow();
    }

    void LoadSceneNow()
    {
        Debug.Log("开始加载场景: " + mainSceneName);

        if (SceneExistsInBuild(mainSceneName))
        {
            SceneManager.LoadScene(mainSceneName);
        }
        // AI辅助生成：DeepSeek-R1-0528, 2026-04-23
        else if (mainSceneBuildIndex >= 0 && mainSceneBuildIndex < SceneManager.sceneCountInBuildSettings)
        {
            SceneManager.LoadScene(mainSceneBuildIndex);
        }

        Debug.Log("=================== 场景加载指令已发送 ===================");
    }


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
