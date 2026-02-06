using UnityEngine;

public class InspectableItem : MonoBehaviour, IInteractable
{
    [Header("检视设置")]
    [Tooltip("检视模型将出现在这个世界坐标 (XYZ)")]
    public Vector3 fixedSpawnPosition;

    [Tooltip("检视用模型预制体（可选，为空则克隆自身）")]
    public GameObject inspectModelPrefab;

    [Header("旋转设置")]
    [Tooltip("右键拖拽时的旋转速度 (手动)")]
    public float manualRotationSpeed = 500.0f;
    [Tooltip("自动旋转速度 (度/秒)")]
    public float autoRotationSpeed = 30.0f;

    [Tooltip("检视时的缩放比例")]
    public float inspectScale = 1.0f;

    // 内部状态
    private bool isInspecting = false;
    private GameObject currentInspectObject; // 当前生成的检视物体
    
    // 引用
    private Transform playerCameraTransform;
    private PlayerController playerController;

    void Start()
    {

        // 获取摄像机
        if (Camera.main != null)
        {
            playerCameraTransform = Camera.main.transform;
            // 尝试从摄像机父级找到 PlayerController
            playerController = playerCameraTransform.GetComponentInParent<PlayerController>();
        }

        // 如果没找到，尝试全局查找（作为备选）
        if (playerController == null)
        {
            playerController = FindObjectOfType<PlayerController>();
        }
    }

    // 实现 IInteractable 接口
    public void Interact()
    {
        if (isInspecting)
        {
            StopInspection();
        }
        else
        {
            StartInspection();
        }
    }

    public void CloseInspection()
    {
        StopInspection();
    }

    void StartInspection()
    {
        if (playerCameraTransform == null) return;
        if (isInspecting) return;

        // 1. 设置状态
        isInspecting = true;
        // 用户请求：不再锁定玩家操作
        // if (playerController != null)
        // {
        //     playerController.isInspecting = true;
        // }

        // 2. 生成检视物体
        GameObject prefabToSpawn = inspectModelPrefab != null ? inspectModelPrefab : this.gameObject;
        
        currentInspectObject = Instantiate(prefabToSpawn);
        currentInspectObject.SetActive(true);
        currentInspectObject.name = prefabToSpawn.name + "_Inspect";
        
        // 应用缩放 (先缩放再计算包围盒)
        currentInspectObject.transform.localScale = prefabToSpawn.transform.localScale * inspectScale;

        // --- 位置定位逻辑 (仅支持固定位置) ---
        // 模式: 固定位置 (不跟随摄像机)
        currentInspectObject.transform.position = fixedSpawnPosition;
        
        // 旋转：只在水平方向上正面对着摄像机，防止倾斜
        Vector3 directionToCamera = playerCameraTransform.position - currentInspectObject.transform.position;
        directionToCamera.y = 0; // 抹平高度差，防止X轴旋转（倾斜）
        if (directionToCamera != Vector3.zero)
        {
            currentInspectObject.transform.rotation = Quaternion.LookRotation(directionToCamera);
            currentInspectObject.transform.Rotate(0, 180, 0); // 修正背面问题 (让正面朝向摄像机)
        }
        
        // 重要：固定模式下，不设置为摄像机的子物体
        currentInspectObject.transform.SetParent(null); 
        
        Debug.Log($"<color=green>[检视系统]</color> 在固定位置生成: {fixedSpawnPosition}");

        // 4. 处理组件
        // 移除或禁用新物体上不必要的脚本，防止逻辑冲突
        InspectableItem oldScript = currentInspectObject.GetComponent<InspectableItem>();
        if (oldScript != null) Destroy(oldScript);

        Rigidbody newRb = currentInspectObject.GetComponent<Rigidbody>();
        if (newRb != null) Destroy(newRb);

        // 添加关闭触发器
        InspectionView trigger = currentInspectObject.AddComponent<InspectionView>();
        trigger.parentItem = this;

        // 确保层级正确
        currentInspectObject.layer = this.gameObject.layer;
    }

    void StopInspection()
    {
        if (currentInspectObject != null)
        {
            Destroy(currentInspectObject);
            currentInspectObject = null;
        }

        isInspecting = false;
        
        // 恢复玩家控制 (已禁用)
        // if (playerController != null)
        // {
        //     playerController.isInspecting = false;
        // }
    }

    void Update()
    {
        if (isInspecting && currentInspectObject != null)
        {
            if (Input.GetMouseButton(1))
            {
                // 右键长按手动旋转 (此时暂停自动旋转)
                float rotX = Input.GetAxis("Mouse X") * manualRotationSpeed * Time.deltaTime;
                float rotY = Input.GetAxis("Mouse Y") * manualRotationSpeed * Time.deltaTime;

                // 左右移动鼠标 -> 绕世界Y轴旋转 (左右转)
                currentInspectObject.transform.Rotate(Vector3.up, -rotX, Space.World);

                // 上下移动鼠标 -> 绕摄像机右轴旋转 (上下转)
                // 修正：之前是绕世界 Right (X) 轴，导致如果玩家侧身看物体，旋转会变成侧翻
                // 现在改为绕“摄像机的右轴”，确保上下拖动鼠标时，物体也是相对于屏幕上下翻动
                currentInspectObject.transform.Rotate(playerCameraTransform.right, rotY, Space.World);
            }
            else
            {
                // 自动旋转 (绕 Y 轴)
                currentInspectObject.transform.Rotate(Vector3.up, autoRotationSpeed * Time.deltaTime, Space.World);
            }
        }
    }
}

// 辅助类：挂在生成的物体上，用于接收点击并通知父物体关闭
public class InspectionView : MonoBehaviour, IInteractable
{
    public InspectableItem parentItem;
    
    public void Interact()
    {
        if (parentItem != null)
        {
            parentItem.CloseInspection();
        }
    }
}