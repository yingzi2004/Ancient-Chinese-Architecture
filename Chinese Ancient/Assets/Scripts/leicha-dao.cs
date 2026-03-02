using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 点击物体时抬起并做出倒东西的动作。将此脚本挂到每个碗物体上。
public class leicha : MonoBehaviour
{
    [Tooltip("抬起的高度（本地坐标，单位为米）")]
    public float liftHeight = 0.2f;

    [Tooltip("抬起所需时间（秒）")]
    public float liftDuration = 0.3f;

    [Tooltip("本地空间的倒出旋转轴（例如 Vector3.right）")]
    public Vector3 pourAxis = Vector3.right;

    [Tooltip("倒出时旋转角度（度）")]
    public float pourAngle = 70f;

    [Tooltip("倒出旋转所需时间（秒）")]
    public float pourDuration = 0.5f;

    [Tooltip("保持倒出姿势的时间（秒）")]
    public float holdDuration = 0.8f;

    [Tooltip("返回初始姿势所需时间（秒）")]
    public float returnDuration = 0.4f;

    [Tooltip("在倒之前将碗朝向该目标（可为空）")]
    public Transform pourTarget;

    [Tooltip("将碗朝向目标所需的时间（秒），为0则不做朝向调整）")]
    public float yawAlignDuration = 0.15f;

    bool isAnimating = false;

    void Reset()
    {
        // 确保有碰撞体以接收点击事件（OnMouseDown）
        if (GetComponent<Collider>() == null)
        {
            var box = gameObject.AddComponent<BoxCollider>();
            // 根据网格或渲染器调整大小是可选的，这里默认使用对象包围盒
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

        // 如果有 pourTarget，先做水平朝向对齐（只围绕 local up轴）
        Quaternion alignedRot = startRot;
        if (pourTarget != null && yawAlignDuration > 0f)
        {
            Vector3 toTarget = pourTarget.position - transform.position;
            // 投影到水平面以计算偏航
            Vector3 toTargetProj = Vector3.ProjectOnPlane(toTarget, transform.up);
            if (toTargetProj.sqrMagnitude > 0.0001f)
            {
                //目标朝向在世界空间，计算目标朝向的 Y旋转（世界坐标）
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

        //计算抬起目标（沿本地 up方向）
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

        // 倾斜倒出
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

        // 返回旋转（先回到对齐旋转）
        tt = 0f;
        while (tt < returnDuration)
        {
            tt += Time.deltaTime;
            float f = Mathf.Clamp01(tt / returnDuration);
            transform.localRotation = Quaternion.Slerp(targetRot, pourStartRot, f);
            yield return null;
        }

        // 降下
        tt = 0f;
        while (tt < returnDuration)
        {
            tt += Time.deltaTime;
            float f = Mathf.Clamp01(tt / returnDuration);
            transform.localPosition = Vector3.Lerp(targetPos, startPos, f);
            yield return null;
        }

        // 如果之前做了朝向对齐，恢复到原始旋转以避免保持偏航
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

        // 修正最终姿态以避免数值误差
        transform.localPosition = startPos;
        transform.localRotation = startRot;

        isAnimating = false;
    }
}
