using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using System.Collections;
using TMPro;
using UnityEngine.SceneManagement;
public class StandaloneMapCloser : MonoBehaviour
{
    [Header("卷轴 (zhou1, zhou2) 面板拖入")]
    public RectTransform leftScroll;
    public RectTransform rightScroll;
    [Header("黑色遮挡图片(黑幕板) 面板拖入")]
    public RectTransform leftCover;
    public RectTransform rightCover;
    [Header("动画时间设置")]
    public float mapFadeInDuration = 1.5f;
    public float waitBeforeClose = 2.0f;
    public float closeDuration = 3.0f;
    public float centerOffset = 60f;
    [Header("音乐设置 (可选)")]
    public AudioClip endingBGM;
    public float audioFadeInDuration = 2.0f;
    public float audioFadeOutDuration = 1.5f;
    [Header("最终退场黑屏(可选)")]
    public Image finalFadeImage;
    [Header("卷轴关闭后对话设置")]
    public GameObject dialoguePanel;
    public TextMeshProUGUI dialogueText;
    [TextArea(2, 5)]
    public string[] dialogueLines = new string[]
    {
        "这就是中国古建筑的魅力吧，虽然岁月流转，但它们的精神长存。",
        "每一块砖瓦，每一根梁柱，都在诉说着过去的故事。",
        "这次的旅程让我收获颇丰，期待下一次的相遇……"
    };
    public float typingSpeed = 0.05f;
    public AudioSource dialogueAudioSource;
    public AudioClip windBlowClip;
    [Header("跳转场景名")]
    public string nextSceneName = "start";
    private bool isTyping = false;
    private bool skipTyping = false;
    private AudioSource bgmSource;
    private void Update()
    {
        if (isTyping && IsAdvanceInputDown())
        {
            skipTyping = true;
        }
    }
    private bool IsAdvanceInputDown()
    {
        return Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return);
    }
    // AI辅助生成：DeepSeek-R1-0528, 2026-04-23
    private IEnumerator Start()
    {
        if (dialoguePanel != null) dialoguePanel.SetActive(false);
        if (dialogueText != null) dialogueText.text = "";
        CreateBlackBackground();
        Image openingBlackMask = CreateTopBlackOverlay();
        if (openingBlackMask != null)
        {
            openingBlackMask.DOFade(0f, mapFadeInDuration).SetEase(Ease.InOutSine).OnComplete(() => {
                Destroy(openingBlackMask.canvas.gameObject);
            });
        }
        // 音乐淡入处理
        if (endingBGM != null)
        {
            bgmSource = gameObject.AddComponent<AudioSource>();
            bgmSource.clip = endingBGM;
            bgmSource.loop = true;
            bgmSource.volume = 0f;
            bgmSource.Play();
            bgmSource.DOFade(1f, audioFadeInDuration).SetEase(Ease.Linear);
        }

        if (leftCover != null) leftCover.gameObject.SetActive(true);
        if (rightCover != null) rightCover.gameObject.SetActive(true);

        if (finalFadeImage != null)
        {
            Color c = finalFadeImage.color;
            c.a = 0f;
            finalFadeImage.color = c;
            finalFadeImage.raycastTarget = false;
            finalFadeImage.gameObject.SetActive(true);
        }
        yield return new WaitForSeconds(waitBeforeClose);
        Sequence seq = DOTween.Sequence();
        float leftDelta = 0f;
        float rightDelta = 0f;
        if (leftScroll != null)
        {
            leftDelta = -centerOffset - leftScroll.anchoredPosition.x;
            seq.Join(leftScroll.DOAnchorPosX(-centerOffset, closeDuration).SetEase(Ease.InOutSine));
        }
        if (rightScroll != null)
        {
            rightDelta = centerOffset - rightScroll.anchoredPosition.x;
            seq.Join(rightScroll.DOAnchorPosX(centerOffset, closeDuration).SetEase(Ease.InOutSine));
        }
        if (leftCover != null)
        {
            float targetX = leftCover.anchoredPosition.x + leftDelta;
            seq.Join(leftCover.DOAnchorPosX(targetX, closeDuration).SetEase(Ease.InOutSine));
        }
        if (rightCover != null)
        {
            float targetX = rightCover.anchoredPosition.x + rightDelta;
            seq.Join(rightCover.DOAnchorPosX(targetX, closeDuration).SetEase(Ease.InOutSine));
        }
        seq.OnComplete(() =>
        {
            StartCoroutine(PlayDialogueAndExit());
        });
    }
    private IEnumerator PlayDialogueAndExit()
    {
        yield return new WaitForSeconds(0.5f);
        // 强行提拔UI层级
        ForceUIRenderState(dialoguePanel, dialogueText);
        if (dialoguePanel != null) dialoguePanel.SetActive(true);
        for (int i = 0; i < dialogueLines.Length; i++)
        {
            if (i == dialogueLines.Length - 1 && dialogueAudioSource != null && windBlowClip != null)
            {
                dialogueAudioSource.PlayOneShot(windBlowClip);
            }
            yield return StartCoroutine(TypeLine(dialogueLines[i]));
            yield return null;
            while (!IsAdvanceInputDown())
            {
                yield return null;
            }
        }
        if (dialoguePanel != null) dialoguePanel.SetActive(false);
        CreateAndFadeBlackOverlay();
    }
    private IEnumerator TypeLine(string line)
    {
        isTyping = true;
        skipTyping = false;
        if (dialogueText != null)
        {
            dialogueText.color = new Color(dialogueText.color.r, dialogueText.color.g, dialogueText.color.b, 1f);
            dialogueText.enabled = true;
            dialogueText.gameObject.SetActive(true);
            dialogueText.rectTransform.localScale = Vector3.one;
            dialogueText.rectTransform.anchoredPosition3D = new Vector3(dialogueText.rectTransform.anchoredPosition3D.x, dialogueText.rectTransform.anchoredPosition3D.y, 0f);
            dialogueText.text = line;
            dialogueText.maxVisibleCharacters = 0;
            dialogueText.ForceMeshUpdate(true);
            int totalChars = dialogueText.textInfo.characterCount;
            for (int i = 0; i <= totalChars; i++)
            {
                if (skipTyping)
                {
                    dialogueText.maxVisibleCharacters = totalChars;
                    break;
                }
                dialogueText.maxVisibleCharacters = i;
                yield return new WaitForSeconds(typingSpeed);
            }
        }
        isTyping = false;
    }
    private void ForceUIRenderState(GameObject panel, TextMeshProUGUI txt)
    {
        if (panel == null || txt == null) return;
        txt.overflowMode = TextOverflowModes.Overflow;
        txt.enableWordWrapping = true;
        Canvas canvas = panel.GetComponent<Canvas>();
        if (canvas == null) canvas = panel.AddComponent<Canvas>();
        CanvasGroup cg = panel.GetComponent<CanvasGroup>();
        if (cg != null)
        {
            cg.alpha = 1f;
            cg.gameObject.SetActive(true);
        }
        canvas.overrideSorting = true;
        canvas.sortingOrder = 32768; 
        canvas.pixelPerfect = false;
        txt.maskable = false;
        if (panel.GetComponent<GraphicRaycaster>() == null)
            panel.AddComponent<GraphicRaycaster>();
    }
    private Image CreateTopBlackOverlay()
    {
        GameObject fadeObj = new GameObject("OpeningBlackCanvas");
        Canvas c = fadeObj.AddComponent<Canvas>();
        c.renderMode = RenderMode.ScreenSpaceOverlay;
        c.sortingOrder = 32767; 
        fadeObj.AddComponent<GraphicRaycaster>();
        GameObject imgObj = new GameObject("FadeImage");
        imgObj.transform.SetParent(fadeObj.transform, false);
        Image img = imgObj.AddComponent<Image>();
        img.color = Color.black;
        RectTransform rt = img.rectTransform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.sizeDelta = Vector2.zero;
        return img;
    }
    private void CreateBlackBackground()
    {
        GameObject bgObj = new GameObject("BgBlackCanvas");
        Canvas c = bgObj.AddComponent<Canvas>();
        c.renderMode = RenderMode.ScreenSpaceOverlay;
        c.sortingOrder = -32767; 
        GameObject imgObj = new GameObject("BgImage");
        imgObj.transform.SetParent(bgObj.transform, false);
        Image img = imgObj.AddComponent<Image>();
        img.color = Color.black;
        RectTransform rt = img.rectTransform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.sizeDelta = Vector2.zero;
    }
    private void CreateAndFadeBlackOverlay()
    {
        GameObject fadeObj = new GameObject("FinalFadeCanvas");
        Canvas c = fadeObj.AddComponent<Canvas>();
        c.renderMode = RenderMode.ScreenSpaceOverlay;
        c.sortingOrder = 32767; 
        fadeObj.AddComponent<GraphicRaycaster>();

        GameObject imgObj = new GameObject("FadeImage");
        imgObj.transform.SetParent(fadeObj.transform, false);
        Image img = imgObj.AddComponent<Image>();
        img.color = new Color(0, 0, 0, 0);
        RectTransform rt = img.rectTransform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.sizeDelta = Vector2.zero;

        if (bgmSource != null)
        {
            bgmSource.DOFade(0f, 1.5f).SetEase(Ease.Linear);
        }

        img.DOFade(1f, 1.5f).SetEase(Ease.Linear).OnComplete(() => {
            QuitGame();
        });
    }
    private void QuitGame()
    {
        if (!string.IsNullOrEmpty(nextSceneName))
        {
            SceneManager.LoadScene(nextSceneName);
        }
        else
        {
    #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
    #else
            Application.Quit();
    #endif
        }
    }
}
