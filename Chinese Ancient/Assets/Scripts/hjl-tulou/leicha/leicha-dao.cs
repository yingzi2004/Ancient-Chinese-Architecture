using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LeichaDao : MonoBehaviour
{
    [Tooltip("抬起的高度（局部坐标，单位：米）")]
    public float liftHeight = 0.2f;

    [Tooltip("抬起过程的时长（秒）")]
    public float liftDuration = 0.3f;

    [Tooltip("局部空间内的倾倒旋转轴（默认为 Vector3.right）")]
    public Vector3 pourAxis = Vector3.right;

    [Tooltip("倾倒时的旋转角度（度）")]
    public float pourAngle = 70f;

    [Tooltip("倾倒旋转过程的时长（秒）")]
    public float pourDuration = 0.5f;

    [Tooltip("保持倾倒状态的时间（秒）")]
    public float holdDuration = 0.8f;

    [Tooltip("返回初始状态的过程时长（秒）")]
    public float returnDuration = 0.4f;

    [Tooltip("倾倒前想要朝向的目标对象（可为空）")]
    public Transform pourTarget;

    [Tooltip("转向朝向目标的对齐时长（秒）。为0时不进行对齐")]
    public float yawAlignDuration = 0.15f;

    bool isAnimating = false;

    void Reset()
    {
        // 确保有碰撞体以接收点击事件（OnMouseDown）
        if (GetComponent<Collider>() == null)
        {
            var box = gameObject.AddComponent<BoxCollider>();
            // 如果没有渲染器，碰撞体尺寸可能不合适；这里先用默认值（Unity 会按默认 BoxCollider 尺寸创建）
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

        // 如果设置了 pourTarget，则先水平转向目标（仅绕 local up 轴）
        Quaternion alignedRot = startRot;
        if (pourTarget != null && yawAlignDuration > 0f)
        {
            Vector3 toTarget = pourTarget.position - transform.position;
            // 投影到水平面以计算偏角
            Vector3 toTargetProj = Vector3.ProjectOnPlane(toTarget, transform.up);
            if (toTargetProj.sqrMagnitude > 0.0001f)
            {
                // 目标朝向（世界空间），取 Y 轴偏转角
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

        // 计算抬起目标位置（沿 local up 方向）
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

        // 保持
        yield return new WaitForSeconds(holdDuration);

        // 回正（回到抬起后的旋转）
        tt = 0f;
        while (tt < returnDuration)
        {
            tt += Time.deltaTime;
            float f = Mathf.Clamp01(tt / returnDuration);
            transform.localRotation = Quaternion.Slerp(targetRot, pourStartRot, f);
            yield return null;
        }

        // 放下
        tt = 0f;
        while (tt < returnDuration)
        {
            tt += Time.deltaTime;
            float f = Mathf.Clamp01(tt / returnDuration);
            transform.localPosition = Vector3.Lerp(targetPos, startPos, f);
            yield return null;
        }

        // 如果之前做了转向对齐，恢复到原始旋转以避免保留偏角
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

        transform.localPosition = startPos;
        transform.localRotation = startRot;

        isAnimating = false;
    }
}
