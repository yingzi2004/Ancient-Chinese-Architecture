using UnityEngine;

public class CrosshairInteract : MonoBehaviour
{
    [Header("交互设置")]
    public float interactRange = 10f;             // 准心能选中的最大距离
    public LayerMask interactableLayer = ~0;      // 射线检测层级，默认检测所有

    private QianItem currentTarget;

    void Update()
    {

        Ray ray = new Ray(transform.position, transform.forward);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, interactRange, interactableLayer))
        {
            // 获取碰撞物体上的QianItem组件
            QianItem qian = hit.collider.GetComponent<QianItem>();
            if (qian == null)
            {
                qian = hit.collider.GetComponentInParent<QianItem>();
            }

            if (qian != null)
            {
                if (currentTarget != qian)
                {
                    if (currentTarget != null) currentTarget.OnHoverExit();
                    currentTarget = qian;
                    currentTarget.OnHoverEnter(); // 准心指向，触发边缘发黄光
                }

                // 按下鼠标左键触发抽出功能
                if (Input.GetMouseButtonDown(0))
                {
                    currentTarget.OnClicked(transform); // 传入摄像机位置用于计算停靠点
                }
            }
            else
            {
                ClearTarget();
            }
        }
        else
        {
            ClearTarget();
        }
    }

    private void ClearTarget()
    {
        if (currentTarget != null)
        {
            currentTarget.OnHoverExit();
            currentTarget = null;
        }
    }
}
