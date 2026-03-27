import codecs

content = '''using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 模拟倒茶时抬起并倾斜物体的动画。将此脚本挂载到需要倒茶的物体上。
public class LeichaDao : MonoBehaviour
{
    [Tooltip("抬起的高度（局部坐标，单位：米）")]
    public float liftHeight = 0.2f;

    [Tooltip("抬起过程的时长（秒）")]
    public float liftDuration = 0.3f;

    [Tooltip("局部空间内的倒茶旋转轴（默认为 Vector3.right）")]
    public Vector3 pourAxis = Vector3.right;

    [Tooltip("倾倒时的旋转角度（度）")]
    public float pourAngle = 70f;

    [Tooltip("倾倒旋转过程的时长（秒）")]
    public float pourDuration = 0.5f;

    [Tooltip("保持倾倒状态的时间（秒）")]
    public float holdDuration = 0.8f;

    [Tooltip("返回初始状态的旋转过程时长（秒）")]
    public float returnDuration = 0.4f;

    [Tooltip("倒茶前想要朝向的目标对象（可为空）")]
    public Transform pourTarget;

    [Tooltip("转向朝向目标的对齐时长（秒）。为0时不再做朝向对齐处理")]
    public float yawAlignDuration = 0.15f;

    bool isAnimating = false;

    void Reset()
    {
        // 确保有碰撞体以接收点击事件（OnMouseDown）
        if (GetComponent<Collider>() == null)
        {
            var box = gameObject.AddComponent<BoxCollider>();
            // 注意：如果没有渲染器可能大小不合适，这里默认使用对象包围盒大小
        }
    }

    void OnMouseDown()
    {
        if (!enabled) return;
        if (isAnimating) return;

        StartCoroutine(PourSequence());
    }

    IEnumerator PourSequence()
    {
        isAnimating = true;

        Vector3 startPos = transform.localPosition;
        Quaternion startRot = transform.localRotation;

        // 如果设置了 pourTarget，转向目标（仅绕 local up 轴水平旋转）
        Quaternion alignedRot = startRot;
        if (pourTarget != null && yawAlignDuration > 0f)
        {
            Vector3 toTarget = pourTarget.position - transform.position;
            // 投影到水平面上以计算偏角
            Vector3 toTargetProj = Vector3.ProjectOnPlane(toTarget, transform.up);
            if (toTargetProj.sqrMagnitude > 0.0001f)
            {
                // 计算目标朝向（将目标朝向转换为当前坐标系下的 Y 轴旋转偏角）
                float targetYaw = Mathf.Atan2(toTargetProj.x, toTargetProj.z) * Mathf.Rad2Deg;
                float currentYaw = transform.eulerAngles.y;
                float yawOffset = Mathf.DeltaAngle(currentYaw, targetYaw);

                alignedRot = startRot * Quaternion.Euler(0f, yawOffset, 0f);

                float t = 0f;
                while (t < yawAlignDuration)
                {
                    t += Time.deltaTime;
                    float f = Mathf.Clamp01(t / yawAlignDuration);
                    transform.localRotation = Quaternion.Slerp(startRot, alignedRot, f);
                    yield return null;
                }
                transform.localRotation = alignedRot;
            }
        }

        // 计算抬起的目标位置（沿起初的 local up 轴向上移动）
        Vector3 targetPos = startPos + transform.InverseTransformVector(Vector3.up) * liftHeight;

        float tt = 0f;
        // 抬起
        while (tt < liftDuration)
        {
            tt += Time.deltaTime;
            float f = Mathf.Clamp01(tt / liftDuration);
            transform.localPosition = Vector3.Lerp(startPos, targetPos, f);
            yield return null;
        }

        // 倾斜倒茶
        tt = 0f;
        Quaternion pourStartRot = transform.localRotation; // use current (may be aligned)
        Quaternion targetRot = pourStartRot * Quaternion.AngleAxis(pourAngle, pourAxis.normalized);
        while (tt < pourDuration)
        {
            tt += Time.deltaTime;
            float f = Mathf.Clamp01(tt / pourDuration);
            transform.localRotation = Quaternion.Slerp(pourStartRot, targetRot, f);
            yield return null;
        }

        // 保持倾倒状态
        yield return new WaitForSeconds(holdDuration);

        // 取消倾斜旋转（先回到抬起且对齐的旋转偏角状态）
        tt = 0f;
        while (tt < returnDuration)
        {
            tt += Time.deltaTime;
            float f = Mathf.Clamp01(tt / returnDuration);
            transform.localRotation = Quaternion.Slerp(targetRot, pourStartRot, f);
            yield return null;
        }

        // 降落
        tt = 0f;
        while (tt < returnDuration)
        {
            tt += Time.deltaTime;
            float f = Mathf.Clamp01(tt / returnDuration);
            transform.localPosition = Vector3.Lerp(targetPos, startPos, f);
            yield return null;
        }

        // 如果之前做了朝向对齐，恢复到原始旋转以避免保留偏角
        if (pourTarget != null && yawAlignDuration > 0f)
        {
            float t = 0f;
            while (t < yawAlignDuration)
            {
                t += Time.deltaTime;
                float f = Mathf.Clamp01(t / yawAlignDuration);
                transform.localRotation = Quaternion.Slerp(transform.localRotation, startRot, f);
                yield return null;
            }
        }

        // 重置为原本状态以防累积误差
        transform.localPosition = startPos;
        transform.localRotation = startRot;

        isAnimating = false;
    }
}'''

with codecs.open('c:/Users/hejia/jianzhu/Ancient-Chinese-Architecture/Chinese Ancient/Assets/Scripts/leicha-dao.cs', 'w', 'utf-8-sig') as f:
    f.write(content)
