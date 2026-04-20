using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using System.Collections;

/// <summary>
/// 一个绝对纯净、独立的地图关闭脚本。
/// 专为了在新场景(EndingMapScene)中播放最终的合上卷轴动画并退出游戏。
/// 彻底摆脱 ScrollMapController 的干扰！
/// </summary>
public class StandaloneMapCloser : MonoBehaviour
{
    [Header("卷轴 (zhou1, zhou2) 面板拖入")]
    [Tooltip("左边的卷轴轴心")]
    public RectTransform leftScroll;
    [Tooltip("右边的卷轴轴心")]
    public RectTransform rightScroll;

    [Header("黑色遮挡图片(黑幕板) 面板拖入")]
    [Tooltip("左侧的黑色Image遮盖板")]
    public RectTransform leftCover;
    [Tooltip("右侧的黑色Image遮盖板")]
    public RectTransform rightCover;

    [Header("动画时间设置")]
    [Tooltip("地图刚出现时，从彻底的黑屏渐渐浮现出来的用时")]
    public float mapFadeInDuration = 1.5f;
    [Tooltip("进入新场景后，地图展示几秒钟才开始收起？")]
    public float waitBeforeClose = 2.0f;
    [Tooltip("向中间合拢动画的持续时间")]
    public float closeDuration = 3.0f;
    [Tooltip("左右卷轴合并时，距离屏幕正中心的间隙（防止卷轴互相穿模重叠）。如填入60，则左轴停在-60，右轴停在60。")]
    public float centerOffset = 60f;

    [Header("音乐设置 (可选)")]
    [Tooltip("如果你想在结局地图界面播放一段音乐，请拖入音频片段 (AudioClip)")]
    public AudioClip endingBGM;
    [Tooltip("进入新场景时，音乐淡入需要多少秒？")]
    public float audioFadeInDuration = 2.0f;
    [Tooltip("离开地图关游戏时，音乐淡出需要多少秒？")]
    public float audioFadeOutDuration = 1.5f;

    [Header("最终退场黑屏(可选)")]
    [Tooltip("如果你有一张全屏纯黑并且透明度为0的图片，可以拖到这。用来做退出前的极致黑屏淡入。")]
    public Image finalFadeImage;

    private AudioSource bgmSource;

    private IEnumerator Start()
    {
        // 自动创建底层黑背景
        CreateBlackBackground();

        // 动态创建一个霸道的纯黑画布用于开场渐亮淡入
        Image openingBlackMask = CreateTopBlackOverlay();

        // 执行开场的地图渐入(纯黑变透明)
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

        // 刚进入新场景时，如果在黑板隐藏状态，保证它能显示出来
        if (leftCover != null) leftCover.gameObject.SetActive(true);
        if (rightCover != null) rightCover.gameObject.SetActive(true);

        // 如果配置了最后的黑屏图片，先让它透明并且阻止挡住交互
        if (finalFadeImage != null)
        {
            Color c = finalFadeImage.color;
            c.a = 0f;
            finalFadeImage.color = c;
            finalFadeImage.raycastTarget = false;
            finalFadeImage.gameObject.SetActive(true);
        }

        // 1. 等待一段时间，让玩家欣赏展开的地图
        yield return new WaitForSeconds(waitBeforeClose);

        // 2. 开始建立回卷动画
        Sequence seq = DOTween.Sequence();

        float leftDelta = 0f;
        float rightDelta = 0f;

        // 左右轴心往中心靠拢，但预留 centerOffset 的防穿模间隙
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

        // 左右黑遮罩板必须移动完全相同的距离(Delta)，才能保证跟卷轴紧紧贴合，速度一模一样！
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

        // 3. 动画执行完毕后的谢幕：强制淡入全屏黑屏！
        seq.OnComplete(() =>
        {
            CreateAndFadeBlackOverlay();
        });
    }

    private Image CreateTopBlackOverlay()
    {
        // 创建霸道顶层 Canvas
        GameObject fadeObj = new GameObject("OpeningBlackCanvas");
        Canvas c = fadeObj.AddComponent<Canvas>();
        c.renderMode = RenderMode.ScreenSpaceOverlay;
        c.sortingOrder = 32767; // 压在所有东西之上

        // 挡住鼠标点击
        fadeObj.AddComponent<GraphicRaycaster>();

        // 创建纯黑Image
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

    /// <summary>
    /// 自动创建一个铺在游戏最底层的纯黑背景，确保不管咋样背景都是黑的
    /// </summary>
    private void CreateBlackBackground()
    {
        // 创建底层 Canvas
        GameObject bgObj = new GameObject("BgBlackCanvas");
        Canvas c = bgObj.AddComponent<Canvas>();
        c.renderMode = RenderMode.ScreenSpaceOverlay;
        c.sortingOrder = -32767; // 放到极远的底层

        // 创建黑色Image
        GameObject imgObj = new GameObject("BgImage");
        imgObj.transform.SetParent(bgObj.transform, false);
        Image img = imgObj.AddComponent<Image>();
        img.color = Color.black;

        // 全屏拉伸
        RectTransform rt = img.rectTransform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.sizeDelta = Vector2.zero;
    }

    /// <summary>
    /// 自动创建一个最顶层的纯黑画布，并执行淡入。不用再拖 finalFadeImage 了一劳永逸！
    /// </summary>
    private void CreateAndFadeBlackOverlay()
    {
        // 创建霸道顶层 Canvas
        GameObject fadeObj = new GameObject("FinalFadeCanvas");
        Canvas c = fadeObj.AddComponent<Canvas>();
        c.renderMode = RenderMode.ScreenSpaceOverlay;
        c.sortingOrder = 32767; // 压在所有东西之上

        // 挡住鼠标点击
        fadeObj.AddComponent<GraphicRaycaster>(); 

        // 创建初始透明的纯黑Image
        GameObject imgObj = new GameObject("FadeImage");
        imgObj.transform.SetParent(fadeObj.transform, false);
        Image img = imgObj.AddComponent<Image>();
        img.color = new Color(0, 0, 0, 0);

        RectTransform rt = img.rectTransform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.sizeDelta = Vector2.zero;

        // 如果有背景音乐，跟着黑屏一起淡出为 0
        if (bgmSource != null)
        {
            bgmSource.DOFade(0f, 1.5f).SetEase(Ease.Linear);
        }

        // 开始淡入到全屏纯黑，用时1.5秒，黑透后再退游戏
        img.DOFade(1f, 1.5f).SetEase(Ease.Linear).OnComplete(() => {
            QuitGame();
        });
    }

    private void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
