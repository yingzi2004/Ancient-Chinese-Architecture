using UnityEngine;
using System.Collections;

public class LotusItem : MonoBehaviour, IInteractable
{
    [Header("--- 荷花飞向船板的设置 ---")]
    public float flyDuration = 1.2f;        // 飞行的持续时间
    public float flyHeight = 1.5f;          // 飞行时的最高抛物线高度

    [Header("在船上的状态 (强烈建议缩小并平放以适应船厢)")]
    public Vector3 centerOffset = new Vector3(0, 0.2f, 0); 
    public float randomRadius = 0.5f;       // 散落在船板上的随机范围，防止所有荷花叠在同一个点
    public float scaleMultiplier = 0.2f;    // 缩小的倍数（比如0.2就是缩小到原来的20%）
    public Vector3 targetRotation = new Vector3(90f, 0f, 0f);   // 躺平在甲板上（X轴旋转90度）

    private bool isCollected = false;

    public void Interact()
    {
        if (isCollected) return;

        // 在场景中寻找当前的船只控制器
        BoatController boat = FindObjectOfType<BoatController>();
        
        if (boat != null)
        {
            isCollected = true;
            // 启动协程：荷花飞向船只
            StartCoroutine(FlyToBoatRoutine(boat));
        }
        else
        {
            Debug.LogWarning("场景中没有找到 BoatController 脚本，荷花不知道飞去哪里！");
        }
    }

    private IEnumerator FlyToBoatRoutine(BoatController boat)
    {
        // 1. 关闭碰撞体，防止飞行过程中阻挡玩家射线/再被点击
        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            col.enabled = false;
        }

        // 确定存放位置：如果船上指定了 lotusTargetPoint 就用它，没指定就默认放在船的中心(boat.transform)
        Transform targetParent = boat.lotusTargetPoint != null ? boat.lotusTargetPoint : boat.transform;

        // 2. 计算存放点的一个随机落点（局部坐标）
        Vector3 randomLocalPos = centerOffset + new Vector3(
            Random.Range(-randomRadius, randomRadius),
            0,
            Random.Range(-randomRadius, randomRadius)
        );

        Vector3 startPos = transform.position;
        Quaternion startRot = transform.rotation;
        
        Vector3 startRotEuler = startRot.eulerAngles;
        // 目标旋转加上一个随机Y轴旋转，不让所有荷花方向全一致
        Quaternion finalRot = Quaternion.Euler(targetRotation.x, Random.Range(0, 360f), targetRotation.z);

        float elapsed = 0f;

        // 设置父物体为目标存放点，这样在飞的时候，即便船在开，最后也能准确落到那个空物体上
        // 重要：在此之后再获取 startScale，因为 SetParent(..., true) 会为了维持世界不变而改变局部的 scale 数值
        transform.SetParent(targetParent, true);
        
        Vector3 startScale = transform.localScale;
        Vector3 targetScale = startScale * scaleMultiplier;

        // 3. 飞行过程 (平滑插值 + 抛物线 + 缩放 + 翻转躺平)
        while (elapsed < flyDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / flyDuration;
            
            // 使用 EaseOut 曲线让动作更自然：起飞快，落地慢
            float speedCurve = t * (2 - t); 

            // 计算当前应该处在的世界坐标目标位置 (因为船可能在移动，所以每帧都要重新算)
            Vector3 boatTargetWorldPos = targetParent.TransformPoint(randomLocalPos);

            // 水平位置插值
            Vector3 currentPos = Vector3.Lerp(startPos, boatTargetWorldPos, speedCurve);
            
            // 加上垂直方向的抛物线偏移：Sin 曲线 (从 0 到 1 再到 0)
            currentPos.y += Mathf.Sin(t * Mathf.PI) * flyHeight;

            // 应用位置
            transform.position = currentPos;

            // 同步插值缩小
            transform.localScale = Vector3.Lerp(startScale, targetScale, speedCurve);
            
            // 同步插值旋转（在飞的过程中翻转成“倾倒/躺平”）
            // 使用 Slerp 让旋转更平滑过渡，跟随着存放点的朝向旋转
            transform.rotation = Quaternion.Slerp(startRot, targetParent.rotation * finalRot, speedCurve);

            yield return null;
        }

        // 4. 确保最终位置极其精准地贴合在计算好的局部坐标
        transform.localPosition = randomLocalPos;
        transform.localRotation = finalRot;
        transform.localScale = targetScale;

        Debug.Log($"<color=cyan>[采摘成功]</color> 荷花 '{gameObject.name}' 已经放进了船上！");
    }
}