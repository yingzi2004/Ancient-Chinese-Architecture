using System.Collections;
using UnityEngine;

[ExecuteAlways]
public class PlayerHandsAnim : MonoBehaviour
{
    [Header("手部模型绑定（请挂在第一人称相机PlayerCamera上或其子物体下）")]
    [Tooltip("左手的模型")]
    public Transform leftHand;
    [Tooltip("右手的模型")]
    public Transform rightHand;

    [Header("动画参数")]
    public float bringUpDuration = 1.0f; // 手举起来的时间
    public float singleBowDuration = 0.8f; // 拜一次的时间
    public int bowCount = 3; // 拜的次数
    public float hideDuration = 1.0f; // 拜完收回去的时间

    [Header("姿势微调（可在运行或编辑期间修改这些数值来对齐手）")]
    // 记录原始始发位置（屏幕下方藏着）
    public Vector3 leftStartPos = new Vector3(-0.4f, -0.6f, 0.6f);
    public Vector3 rightStartPos = new Vector3(0.4f, -0.6f, 0.6f);
    
    public Vector3 leftStartRot = new Vector3(0, 0, 45);
    public Vector3 rightStartRot = new Vector3(0, 0, -45);

    // 合十时的目标位置（屏幕中央）
    public Vector3 leftClaspPos = new Vector3(-0.03f, -0.2f, 0.6f);
    public Vector3 rightClaspPos = new Vector3(0.03f, -0.2f, 0.6f);
    
    // 合十时的旋转：掌心相对，手指朝上稍微往前倾
    public Vector3 leftClaspRot = new Vector3(20, 90, 0);
    public Vector3 rightClaspRot = new Vector3(20, -90, 0);

    // 拜下去的角度（相对相机往前倾斜的度数，数字越大拜得越深）
    public float bowAngle = 60f;

    [Header("礼成天空文字特效")]
    [Tooltip("请在天空中建一个World Space的Canvas，加上文字，挂上Canvas Group并把Alpha设为0，拖到这里")]
    public CanvasGroup skyTextCanvasGroup;

    [Header("强制预览合十(仅在Scene下打勾测试排列使用)")]
    public bool previewClaspInEditor = false;

    void Update()
    {
        // 在不运行游戏的编辑模式下，如果你打勾了预览，就强制让手保持合十位置，方便你调正
        if (!Application.isPlaying && leftHand != null && rightHand != null)
        {
            if (previewClaspInEditor)
            {
                leftHand.localPosition = leftClaspPos;
                rightHand.localPosition = rightClaspPos;
                leftHand.localRotation = Quaternion.Euler(leftClaspRot);
                rightHand.localRotation = Quaternion.Euler(rightClaspRot);
                leftHand.gameObject.SetActive(true);
                rightHand.gameObject.SetActive(true);
            }
            else
            {
                // 如果没勾预览，就退回最初张开藏在下面的状态，免得挡视野
                leftHand.localPosition = leftStartPos;
                rightHand.localPosition = rightStartPos;
                leftHand.localRotation = Quaternion.Euler(leftStartRot);
                rightHand.localRotation = Quaternion.Euler(rightStartRot);
            }
        }
    }

    void Start()
    {
        // 只有在真正运行游戏时才执行隐藏逻辑
        if (Application.isPlaying && leftHand != null && rightHand != null)
        {
            leftHand.localPosition = leftStartPos;
            rightHand.localPosition = rightStartPos;
            leftHand.localRotation = Quaternion.Euler(leftStartRot);
            rightHand.localRotation = Quaternion.Euler(rightStartRot);
            
            // 游戏开始时自动关掉预览
            if (previewClaspInEditor)
            {
               previewClaspInEditor = false; 
            }
            
            leftHand.gameObject.SetActive(false);
            rightHand.gameObject.SetActive(false);
        }
    }

    // 由香的交互脚本在插香完成后调用
    public void PlayPrayAnimation()
    {
        if (leftHand == null || rightHand == null)
        {
            Debug.LogWarning("未绑定左右手模型，无法播放双手合十动画！");
            return;
        }

        leftHand.gameObject.SetActive(true);
        rightHand.gameObject.SetActive(true);
        StartCoroutine(PraySequence());
    }

    private IEnumerator PraySequence()
    {
        // 第一阶段：双手从屏幕下方缓缓举起并合十
        float t = 0;
        while (t < bringUpDuration)
        {
            t += Time.deltaTime;
            float percent = Mathf.SmoothStep(0, 1, t / bringUpDuration);

            leftHand.localPosition = Vector3.Lerp(leftStartPos, leftClaspPos, percent);
            rightHand.localPosition = Vector3.Lerp(rightStartPos, rightClaspPos, percent);

            leftHand.localRotation = Quaternion.Slerp(Quaternion.Euler(leftStartRot), Quaternion.Euler(leftClaspRot), percent);
            rightHand.localRotation = Quaternion.Slerp(Quaternion.Euler(rightStartRot), Quaternion.Euler(rightClaspRot), percent);

            yield return null;
        }

        // 第二阶段：双手合十，拜三下
        // 动态计算往下拜的目标旋转度数（以相机的视角往前绕X轴倾倒 bowAngle 度）
        Quaternion targetLeftBowRot = Quaternion.Euler(bowAngle, 0, 0) * Quaternion.Euler(leftClaspRot);
        Quaternion targetRightBowRot = Quaternion.Euler(bowAngle, 0, 0) * Quaternion.Euler(rightClaspRot);

        for (int i = 0; i < bowCount; i++)
        {
            // 往下拜
            t = 0;
            while (t < singleBowDuration / 2)
            {
                t += Time.deltaTime;
                float percent = Mathf.SmoothStep(0, 1, t / (singleBowDuration / 2));
                
                leftHand.localRotation = Quaternion.Slerp(Quaternion.Euler(leftClaspRot), targetLeftBowRot, percent);
                rightHand.localRotation = Quaternion.Slerp(Quaternion.Euler(rightClaspRot), targetRightBowRot, percent);
                yield return null;
            }

            // 抬起来
            t = 0;
            while (t < singleBowDuration / 2)
            {
                t += Time.deltaTime;
                float percent = Mathf.SmoothStep(0, 1, t / (singleBowDuration / 2));
                
                leftHand.localRotation = Quaternion.Slerp(targetLeftBowRot, Quaternion.Euler(leftClaspRot), percent);
                rightHand.localRotation = Quaternion.Slerp(targetRightBowRot, Quaternion.Euler(rightClaspRot), percent);
                yield return null;
            }
            
            // 微微停顿一下，让每一拜有力量感
            yield return new WaitForSeconds(0.2f);
        }

        yield return new WaitForSeconds(0.5f);

        // 第2.5阶段：触发天空文字与粒子特效
        if (skyTextCanvasGroup != null)
        {
            StartCoroutine(ShowSkyTextRoutine());
        }

        // 第三阶段：拜完后双手分开并缩回屏幕下方隐藏
        t = 0;
        while (t < hideDuration)
        {
            t += Time.deltaTime;
            float percent = Mathf.SmoothStep(0, 1, t / hideDuration);

            leftHand.localPosition = Vector3.Lerp(leftClaspPos, leftStartPos, percent);
            rightHand.localPosition = Vector3.Lerp(rightClaspPos, rightStartPos, percent);

            leftHand.localRotation = Quaternion.Slerp(Quaternion.Euler(leftClaspRot), Quaternion.Euler(leftStartRot), percent);
            rightHand.localRotation = Quaternion.Slerp(Quaternion.Euler(rightClaspRot), Quaternion.Euler(rightStartRot), percent);

            yield return null;
        }

        // 彻底隐藏
        leftHand.gameObject.SetActive(false);
        rightHand.gameObject.SetActive(false);
    }

    // 控制天空文字与粒子浮现的协程
    private IEnumerator ShowSkyTextRoutine()
    {
        // ===================================
        // 文字逐渐浮现 (淡入 2 秒)
        // ===================================
        float t = 0;
        float fadeDuration = 2f;
        float floatUpDistance = 3f; // 同时往上漂浮一点点增加仙气
        Vector3 origPos = skyTextCanvasGroup.transform.position;

        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            float percent = t / fadeDuration;
            skyTextCanvasGroup.alpha = percent;
            skyTextCanvasGroup.transform.position = origPos + Vector3.up * (percent * floatUpDistance);
            yield return null;
        }

        skyTextCanvasGroup.alpha = 1f;

        // ===================================
        // 停留 6 秒
        // ===================================
        yield return new WaitForSeconds(6f);

        // ===================================
        // 文字逐渐消失 (淡出 2 秒)
        // ===================================
        t = 0;
        Vector3 startFadeOutPos = skyTextCanvasGroup.transform.position;
        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            float percent = t / fadeDuration;
            skyTextCanvasGroup.alpha = 1f - percent;
            // 继续往上漂一丝丝
            skyTextCanvasGroup.transform.position = startFadeOutPos + Vector3.up * (percent * floatUpDistance);
            yield return null;
        }

        skyTextCanvasGroup.alpha = 0f;
    }
}