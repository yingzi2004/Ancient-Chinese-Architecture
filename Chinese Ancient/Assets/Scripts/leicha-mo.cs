using System.Collections;
using UnityEngine;

// ���һ��Χ��ָ��������ĥ����Ȧ���������������Ʋ���ĥ��
// ע�⣺���������Ƿ��ַ����ű������ƣ�Pestle�������ϣ����� Pivot�������ģ����ɡ�
public class leicha_mo : MonoBehaviour
{
    [Tooltip("��ĥ���ģ��Ƹõ���Բ���˶�����Ϊ����ʹ�ø����������ԭ�㡣")]
    public Transform pivot;

    [Tooltip("ÿ�ε���ƶ���Ȧ��ͨ��2 ��3��")]
    [Range(1, 10)]
    public int rotations = 3;

    [Tooltip("ÿȦ����ʱ�䣨�룩����ʱ�� = rotations * rotationDuration")]
    public float rotationDuration = 0.4f;

    [Tooltip("���ĸ�����ת��������ռ䣩�������� Y Ϊˮƽ������Ȧ")]
    public Vector3 axis = Vector3.up;

    [Tooltip("Χ�� pivot ��Բ�ܰ뾶����Ϊ0 ��ʹ�õ�ǰ������ pivot �ľ��룩")]
    public float radius = 0f;

    [Tooltip("���Ű뾶�ı�����0-1�������ڰѰ뾶��С��Ĭ��0.5 ��ʾ��һ�����")]
    [Range(0f, 1f)]
    public float radiusScale = 0.5f;

    [Tooltip("��������뾶��0 ��ʾ�����ƣ�")]
    public float maxRadius = 0f;

    [Tooltip("�Ƿ�����ĥʱͬʱʹ��������С����ת���Ӿ�Ч����")]
    public bool tiltDuringGrinding = true;

    [Tooltip("�������ĸ����Ƿ��ȣ��ȣ������� tiltDuringGrinding Ϊ true ��Ч��")]
    public float tiltAngle = 10f;

    [Tooltip("�ﵽָ���������ĥ�������ʱ����ʾ�ĳ������� leicha ģ��")]
    public GameObject leichaModel;

    [Tooltip("�ﵽ�������Ч��ĥ�������ʱ��ʾ leicha ģ��")]
    public int revealClickCount = 3;

    bool isGrinding = false;
    int grindClickCount = 0;
    bool hasRevealedModel = false;

    void Reset()
    {
        // ȷ�� ����ײ�� �Խ��� OnMouseDown ���
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

        //����ԭλ������ת
        Vector3 startPos = transform.position;
        Quaternion startRot = transform.rotation;

        // ȷ�� pivot ʹ�����ȼ����ֶ� > ���� > ����ԭ��
        Transform effectivePivot = pivot;
        if (effectivePivot == null)
            effectivePivot = transform.parent != null ? transform.parent : null;

        // If no pivot is assigned, use the current object center as pivot.
        Vector3 pivotPos = effectivePivot != null ? effectivePivot.position : transform.position;

        //�����ʼƫ����뾶
        Vector3 offset = transform.position - pivotPos;
        float baseRadius = radius > 0f ? radius : offset.magnitude;
        // Apply scale to make radius smaller.
        float usedRadius = Mathf.Max(0f, baseRadius * Mathf.Clamp01(radiusScale));
        if (usedRadius > 0.0001f)
        {
            //�淶��ƫ�Ƶ�ָ���뾶�����ַ���
            offset = (offset.sqrMagnitude > 0.0001f)
                ? offset.normalized * usedRadius
                : transform.TransformDirection(Vector3.forward) * usedRadius;
        }
        else
        {
            // Keep zero radius so rotation happens around the rod's own center.
            offset = Vector3.zero;
        }

        // Apply maximum cap if set
        if (maxRadius > 0f && usedRadius > maxRadius)
        {
            usedRadius = maxRadius;
            offset = offset.normalized * usedRadius;
        }

        //��ת�ᣨ����ռ䵥λ������
        Vector3 worldAxis = axis.sqrMagnitude > 0.0001f ? axis.normalized : Vector3.up;

        // �ܽǶȣ��ȣ�
        float totalAngle = 360f * rotations;
        float elapsed = 0f;
        float totalDuration = Mathf.Max(0.01f, rotations * rotationDuration);

        // �ڶ���������ͬʱ��ʹ��������С����������ǿ��ĥ��
        while (elapsed < totalDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / totalDuration);
            float angleDeg = Mathf.Lerp(0f, totalAngle, t);

            Quaternion rot = Quaternion.AngleAxis(angleDeg, worldAxis);

            // Radius == 0 means self-centered rotation (no orbital movement).
            if (usedRadius <= 0.0001f)
            {
                transform.position = startPos;
            }
            else
            {
                //���㵱ǰλ�ã��� pivotPos��ת offset
                Vector3 newPos = pivotPos + rot * offset;
                transform.position = newPos;
            }

            if (tiltDuringGrinding)
            {
                //��������ת��������С��������sin ���ߣ�
                float tilt = Mathf.Sin(t * Mathf.PI * rotations * 2f) * tiltAngle;
                //����ת������������������������� worldAxis ��ĳ��������
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

        //����ʱ�ָ�����ʼ��̬��������ֵƯ�ƣ�
        transform.position = startPos;
        transform.rotation = startRot;

        isGrinding = false;
    }
}
