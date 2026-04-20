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
    [Tooltip("进入新场景后，地图展示几秒钟才开始收起？")]
    public float waitBeforeClose = 2.0f;
    [Tooltip("向中间合拢动画的持续时间")]
    public float closeDuration = 3.0f;
    [Tooltip("左右卷轴合并时，距离屏幕正中心的间隙（防止卷轴互相穿模重叠）。如填入60，则左轴停在-60，右轴停在60。")]
    public float centerOffset = 60f;

    [Header("最终退场黑屏(可选)")]
    [Tooltip("如果你有一张全屏纯黑并且透明度为0的图片，可以拖到这。用来做退出前的极致黑屏淡入。")]
    public Image finalFadeImage;

    private IEnumerator Start()
    {
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

        // 3. 动画执行完毕后的谢幕
        seq.OnComplete(() =>
        {
            if (finalFadeImage != null)
            {
                // 最后再淡入一层完美黑屏，时长1秒，结束后退游戏
                finalFadeImage.DOFade(1f, 1f).SetEase(Ease.Linear).OnComplete(QuitGame);
            }
            else
            {
                QuitGame(); // 如果没配置最后黑屏图片，合上后直接秒退
            }
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
