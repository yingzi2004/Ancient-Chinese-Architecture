using System.Collections;
using UnityEngine;

public class leicha_mo : MonoBehaviour
{
    [Tooltip("研磨棒模型引用，圆周运动将围绕此对象的原始点作为原点。")]
    public Transform pivot;

    [Tooltip("每次点击移动的圈数（通常2 或 3圈）")]
    [Range(1, 10)]
    public int rotations = 3;

    [Tooltip("每圈所需时间（秒），总时间 = rotations * rotationDuration")]
    public float rotationDuration = 0.4f;

    [Tooltip("围绕哪个轴旋转（世界空间），默认 Y 为水平画圈")]
    public Vector3 axis = Vector3.up;

    [Tooltip("围绕 pivot 的圆周半径，设为0 时使用当前对象与 pivot 的距离")]
    public float radius = 0f;

    [Tooltip("缩放半径的系数（0-1），可用于把半径变小，默认0.5 表示一半")]
    [Range(0f, 1f)]
    public float radiusScale = 0.5f;

    [Tooltip("限制最大半径，0 表示不限制")]
    public float maxRadius = 0f;

    [Tooltip("是否在研磨的同时使研磨棒有轻微倾斜，增加真实感")]
    public bool tiltDuringGrinding = true;

    [Tooltip("研磨棒向哪个方向倾斜角度（度），仅当 tiltDuringGrinding 为 true 时生效")]
    public float tiltAngle = 10f;

    [Tooltip("达到指定研磨次数后，显示某个东西（leicha 模型）")]
    public GameObject leichaModel;

    [Tooltip("达到多少次有效研磨点击后，显示 leicha 模型")]
    public int revealClickCount = 3;

    bool isGrinding = false;
    int grindClickCount = 0;
    bool hasRevealedModel = false;

    void Reset()
    {
        // 确保有碰撞体，以便能接收 OnMouseDown 事件
        if (GetComponent<Collider>() == null)
        {
            gameObject.AddComponent<BoxCollider>();
        }
    }

    void OnMouseDown()
    {
        if (!enabled) return;
        if (isGrinding) return;

        grindClickCount++;
        TryRevealLeichaModel();

        StartCoroutine(GrindingRoutine());
    }

    void TryRevealLeichaModel()
    {
        if (hasRevealedModel) return;
        if (leichaModel == null) return;
        if (grindClickCount < Mathf.Max(1, revealClickCount)) return;

        leichaModel.SetActive(true);
        hasRevealedModel = true;
        Debug.Log("Leicha model revealed after grind clicks: " + grindClickCount);
    }

    IEnumerator GrindingRoutine()
    {
        isGrinding = true;

        Vector3 startPos = transform.position;
        Quaternion startRot = transform.rotation;

        Transform effectivePivot = pivot;
        if (effectivePivot == null)
            effectivePivot = transform.parent != null ? transform.parent : null;

        Vector3 pivotPos = effectivePivot != null ? effectivePivot.position : transform.position;

        //计算初始偏移和半径
        Vector3 offset = transform.position - pivotPos;
        float baseRadius = radius > 0f ? radius : offset.magnitude;
        // Apply scale to make radius smaller.
        float usedRadius = Mathf.Max(0f, baseRadius * Mathf.Clamp01(radiusScale));
        if (usedRadius > 0.0001f)
        {
            //规范化偏移到指定半径方向
            offset = (offset.sqrMagnitude > 0.0001f)
                ? offset.normalized * usedRadius
                : transform.TransformDirection(Vector3.forward) * usedRadius;
        }
        else
        {
            offset = Vector3.zero;
        }

        if (maxRadius > 0f && usedRadius > maxRadius)
        {
            usedRadius = maxRadius;
            offset = offset.normalized * usedRadius;
        }

        //旋转轴
        Vector3 worldAxis = axis.sqrMagnitude > 0.0001f ? axis.normalized : Vector3.up;

        // 总角度
        float totalAngle = 360f * rotations;
        float elapsed = 0f;
        float totalDuration = Mathf.Max(0.01f, rotations * rotationDuration);

        // 在第二圈的同时同时进行轻微倾斜，增加真实研磨感
        while (elapsed < totalDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / totalDuration);
            float angleDeg = Mathf.Lerp(0f, totalAngle, t);

            Quaternion rot = Quaternion.AngleAxis(angleDeg, worldAxis);

            if (usedRadius <= 0.0001f)
            {
                transform.position = startPos;
            }
            else
            {
                //计算当前位置，从 pivotPos 旋转 offset
                Vector3 newPos = pivotPos + rot * offset;
                transform.position = newPos;
            }

            if (tiltDuringGrinding)
            {
                //根据旋转进度添加轻微倾斜，使用 sin 曲线
                float tilt = Mathf.Sin(t * Mathf.PI * rotations * 2f) * tiltAngle;
                //倾斜轴应垂直于旋转轴和位置向量，即 worldAxis 的某个垂直方向
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

        //结束时恢复到初始状态
        transform.position = startPos;
        transform.rotation = startRot;

        isGrinding = false;
    }
}
