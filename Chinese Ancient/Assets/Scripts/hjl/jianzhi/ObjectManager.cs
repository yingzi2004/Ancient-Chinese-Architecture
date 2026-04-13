using UnityEngine;

public class ObjectManager : MonoBehaviour
{
    public float fallSpeed = 3f; // 调整掉落速度
    private bool isFalling = false;

    void Update()
    {
        // 激活掉落状态时不断向下移动
        if (isFalling)
        {
            transform.Translate(Vector3.down * fallSpeed * Time.deltaTime);
        }
    }

    // 由 DrawPath.cs 在判定成功时调用
    public void StartFalling()
    {
        isFalling = true;
    }
}
