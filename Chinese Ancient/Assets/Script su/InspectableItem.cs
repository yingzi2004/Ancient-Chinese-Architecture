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

        // 自动修复1：确保所有子物体都在同一个层级（解决射线检测不到子物体的问题）
        SetLayerRecursively(this.gameObject, this.gameObject.layer);

        // 自动修复2：确保模型有碰撞体（解决完全没有碰撞体导致射线穿透的问题）
        EnsureColliders(this.gameObject);
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
        
        Debug.Log($"<color=green>[检视系统]</color> 已生成检视物体: <b>{currentInspectObject.name}</b>\n" +
                  $"├── 世界坐标: {currentInspectObject.transform.position}\n" +
                  $"├── 源物体: {(inspectModelPrefab != null ? inspectModelPrefab.name : this.name)}\n" +
                  $"└── 缩放: {currentInspectObject.transform.localScale}");

        // 4. 处理组件
        // 递归移除克隆体及其所有子物体上的 InspectableItem，防止大模型内部嵌套触发
        InspectableItem[] oldScripts = currentInspectObject.GetComponentsInChildren<InspectableItem>(true);
        foreach (var script in oldScripts) Destroy(script);

        // 递归移除 Rigidbody，防止物理掉落
        Rigidbody[] rbs = currentInspectObject.GetComponentsInChildren<Rigidbody>(true);
        foreach (var rb in rbs) Destroy(rb);

        // 添加关闭触发器到根节点
        InspectionView trigger = currentInspectObject.AddComponent<InspectionView>();
        trigger.parentItem = this;

        // 确保克隆体的所有子物体层级正确，以便射线能点到它们来关闭检视
        SetLayerRecursively(currentInspectObject, this.gameObject.layer);
        
        // 确保克隆体有碰撞体
        EnsureColliders(currentInspectObject);
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

    // ───────────────────── 辅助方法 ─────────────────────

    // 递归设置层级，确保大模型的所有子物体都能被射线检测到
    private void SetLayerRecursively(GameObject obj, int newLayer)
    {
        if (obj == null) return;
        obj.layer = newLayer;
        foreach (Transform child in obj.transform)
        {
            if (child != null)
                SetLayerRecursively(child.gameObject, newLayer);
        }
    }

    // 确保物体或其子物体有碰撞体，如果没有则在根节点加一个 BoxCollider
    private void EnsureColliders(GameObject obj)
    {
        Collider[] colliders = obj.GetComponentsInChildren<Collider>(true);
        if (colliders.Length == 0)
        {
            // 如果整个大模型连一个碰撞体都没有，射线绝对点不到，自动加一个包围盒
            BoxCollider box = obj.AddComponent<BoxCollider>();
            
            // 尝试根据 MeshRenderer 计算包围盒大小
            Renderer[] renderers = obj.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length > 0)
            {
                Bounds bounds = renderers[0].bounds;
                for (int i = 1; i < renderers.Length; i++)
                {
                    bounds.Encapsulate(renderers[i].bounds);
                }
                // 将世界坐标的 bounds 转换回本地坐标
                box.center = obj.transform.InverseTransformPoint(bounds.center);
                box.size = new Vector3(
                    bounds.size.x / obj.transform.lossyScale.x,
                    bounds.size.y / obj.transform.lossyScale.y,
                    bounds.size.z / obj.transform.lossyScale.z
                );
            }
            Debug.Log($"<color=yellow>[检视系统]</color> 物体 {obj.name} 没有碰撞体，已自动添加 BoxCollider。");
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

    // 关键：如果射线点到了子物体，子物体没有 IInteractable，
    // PlayerController 会往上找父级，所以只要根节点有这个脚本就行。
}