using System.Collections;
using UnityEngine;

[ExecuteAlways]
public class PlayerHandsAnim : MonoBehaviour
{
    [Header("相机设置")]
    [Tooltip("需要弯腰的主相机（默认会自动获取Camera.main）")]
    public Transform mainCamera;

    [Header("动画参数")]
    public float singleBowDuration = 0.8f; // 拜一次的时间
    public int bowCount = 3; // 拜的次数

    [Header("弯腰参数")]
    // 拜下去的角度（相对相机往前倾斜的度数，数字越大拜得越深）
    public float bowAngle = 60f;

    [Header("礼成天空文字特效")]
    [Tooltip("请在天空中建一个World Space的Canvas，加上文字，挂上Canvas Group并把Alpha设为0，拖到这里")]
    public CanvasGroup skyTextCanvasGroup;

    void Start()
    {
        if (mainCamera == null && Camera.main != null)
        {
            mainCamera = Camera.main.transform;
        }
    }

    // 由香的交互脚本在插香完成后调用
    public void PlayPrayAnimation()
    {
        if (mainCamera == null)
        {
            if (Camera.main != null) mainCamera = Camera.main.transform;
            else { Debug.LogWarning("未找到主相机，无法播放弯腰动画！"); return; }
        }

        StartCoroutine(PraySequence());
    }

    private IEnumerator PraySequence()
    {
        // 尝试获取玩家控制器，如果在鞠躬期间需要禁用玩家的鼠标控制，就锁定它
        PlayerController playerCtrl = mainCamera.GetComponentInParent<PlayerController>();
        if (playerCtrl == null && Camera.main != null) 
            playerCtrl = Camera.main.GetComponentInParent<PlayerController>();

        if (playerCtrl != null)
        {
            playerCtrl.isInspecting = true; // 借用 inspecting 状态禁止鼠标在此期间乱动镜头
        }

        // 准备弯腰
        Quaternion originalCamRot = mainCamera.localRotation;
        // 动态计算往下拜的目标旋转度数（以相机的视角往前绕X轴倾倒 bowAngle 度）
        Quaternion targetCamBowRot = originalCamRot * Quaternion.Euler(bowAngle, 0, 0);

        yield return new WaitForSeconds(1.0f); // 稍微等一下再开始拜

        for (int i = 0; i < bowCount; i++)
        {
            // 往下弯腰
            float t = 0;
            while (t < singleBowDuration / 2)
            {
                t += Time.deltaTime;
                float percent = Mathf.SmoothStep(0, 1, t / (singleBowDuration / 2));
                
                mainCamera.localRotation = Quaternion.Slerp(originalCamRot, targetCamBowRot, percent);
                yield return null;
            }

            // 抬起腰来
            t = 0;
            while (t < singleBowDuration / 2)
            {
                t += Time.deltaTime;
                float percent = Mathf.SmoothStep(0, 1, t / (singleBowDuration / 2));
                
                mainCamera.localRotation = Quaternion.Slerp(targetCamBowRot, originalCamRot, percent);
                yield return null;
            }
            
            // 微微停顿一下，让每一拜有力量感
            yield return new WaitForSeconds(0.2f);
        }

        // 确保最终姿态完全归位
        mainCamera.localRotation = originalCamRot;

        if (playerCtrl != null)
        {
            playerCtrl.isInspecting = false; // 恢复鼠标控制
        }

        yield return new WaitForSeconds(0.5f);

        // 拜完后：触发天空文字与粒子特效
        if (skyTextCanvasGroup != null)
        {
            StartCoroutine(ShowSkyTextRoutine());
        }
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