using System.Collections;
using UnityEngine;

// 点击一次围绕指定点做研磨（绕圈）动作（例如杵绕钵研磨）
// 注意：类名不含非法字符，脚本挂在杵（Pestle）物体上，拖入 Pivot（钵中心）即可。
public class leicha_mo : MonoBehaviour
{
    [Tooltip("研磨中心（绕该点做圆周运动）。为空则使用父物体或世界原点。")]
    public Transform pivot;

    [Tooltip("每次点击绕多少圈（通常2 或3）")]
    [Range(1, 10)]
    public int rotations = 3;

    [Tooltip("每圈所用时间（秒）。总时长 = rotations * rotationDuration")]
    public float rotationDuration = 0.4f;

    [Tooltip("绕哪个轴旋转（在世界空间）——例如 Y 为水平面内绕圈")]
    public Vector3 axis = Vector3.up;

    [Tooltip("围绕 pivot 的圆周半径（若为0 则使用当前物体与 pivot 的距离）")]
    public float radius = 0f;

    [Tooltip("缩放半径的比例（0-1），用于把半径变小；默认0.5 表示用一半距离")]
    [Range(0f, 1f)]
    public float radiusScale = 0.5f;

    [Tooltip("最大允许半径（0 表示不限制）")]
    public float maxRadius = 0f;

    [Tooltip("是否在研磨时同时使杵自身做小幅旋转（视觉效果）")]
    public bool tiltDuringGrinding = true;

    [Tooltip("杵自身的俯仰角幅度（度），仅当 tiltDuringGrinding 为 true 有效）")]
    public float tiltAngle = 10f;

    bool isGrinding = false;

    void Reset()
    {
        // 确保 有碰撞体 以接收 OnMouseDown 点击
        if (GetComponent<Collider>() == null)
        {
            gameObject.AddComponent<BoxCollider>();
        }
    }

    void OnMouseDown()
    {
        if (!enabled) return;
        if (isGrinding) return;

        StartCoroutine(GrindingRoutine());
    }

    IEnumerator GrindingRoutine()
    {
        isGrinding = true;

        //备份原位置与旋转
        Vector3 startPos = transform.position;
        Quaternion startRot = transform.rotation;

        // 确定 pivot 使用优先级：字段 > 父物 > 世界原点
        Transform effectivePivot = pivot;
        if (effectivePivot == null)
            effectivePivot = transform.parent != null ? transform.parent : null;

        Vector3 pivotPos = effectivePivot != null ? effectivePivot.position : Vector3.zero;

        //计算初始偏移与半径
        Vector3 offset = transform.position - pivotPos;
        float baseRadius = radius > 0f ? radius : offset.magnitude;
        // Apply scale to make radius smaller
        float usedRadius = Mathf.Max(0f, baseRadius * Mathf.Clamp01(radiusScale));
        // Fallback tiny radius if zero
        if (usedRadius <= 0.0001f)
        {
            offset = transform.TransformDirection(Vector3.forward) * 0.1f;
            usedRadius = offset.magnitude;
        }
        else
        {
            //规范化偏移到指定半径（保持方向）
            offset = (offset.sqrMagnitude > 0.0001f) ? offset.normalized * usedRadius : transform.TransformDirection(Vector3.forward) * usedRadius;
        }

        // Apply maximum cap if set
        if (maxRadius > 0f && usedRadius > maxRadius)
        {
            usedRadius = maxRadius;
            offset = offset.normalized * usedRadius;
        }

        //旋转轴（世界空间单位向量）
        Vector3 worldAxis = axis.sqrMagnitude > 0.0001f ? axis.normalized : Vector3.up;

        // 总角度（度）
        float totalAngle = 360f * rotations;
        float elapsed = 0f;
        float totalDuration = Mathf.Max(0.01f, rotations * rotationDuration);

        // 在动画过程中同时可使杵自身做小幅俯仰以增强研磨感
        while (elapsed < totalDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / totalDuration);
            float angleDeg = Mathf.Lerp(0f, totalAngle, t);

            //计算当前位置：绕 pivotPos旋转 offset
            Quaternion rot = Quaternion.AngleAxis(angleDeg, worldAxis);
            Vector3 newPos = pivotPos + rot * offset;
            transform.position = newPos;

            if (tiltDuringGrinding)
            {
                //让杵在旋转过程中做小幅俯仰（sin 曲线）
                float tilt = Mathf.Sin(t * Mathf.PI * rotations * 2f) * tiltAngle;
                //以旋转轴的正交方向做俯仰（尝试沿 worldAxis 的某个横向方向）
                Vector3 tiltAxis = Vector3.Cross(worldAxis, (transform.position - pivotPos)).normalized;
                if (tiltAxis.sqrMagnitude < 0.0001f) tiltAxis = transform.right;
                transform.rotation = Quaternion.AngleAxis(tilt, tiltAxis) * startRot;
            }
            else
            {
                transform.rotation = startRot;
            }

            yield return null;
        }

        //结束时恢复到起始姿态（避免数值漂移）
        transform.position = startPos;
        transform.rotation = startRot;

        isGrinding = false;
    }
}
