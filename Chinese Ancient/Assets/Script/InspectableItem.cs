using UnityEngine;
public class InspectableItem : MonoBehaviour, IInteractable
{
    [Header("检视设置")]
    public Vector3 fixedSpawnPosition;
    public GameObject inspectModelPrefab;
    [Header("旋转设置")]
    public float manualRotationSpeed = 500.0f;
    public float autoRotationSpeed = 30.0f;
    public float inspectScale = 1.0f;
    // 内部状态
    private bool isInspecting = false;
    private GameObject currentInspectObject; // 当前生成的检视物体
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
        // 如果没找到，尝试全局查找
        if (playerController == null)
        {
            playerController = FindObjectOfType<PlayerController>();
        }
        //确保所有子物体都在同一个层级（解决射线检测不到子物体的问题）
        SetLayerRecursively(this.gameObject, this.gameObject.layer);
        //确保模型有碰撞体（解决完全没有碰撞体导致射线穿透的问题）
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
        // 设置状态
        isInspecting = true;
        
        //生成检视物体
        GameObject prefabToSpawn = inspectModelPrefab != null ? inspectModelPrefab : this.gameObject;
        currentInspectObject = Instantiate(prefabToSpawn);
        currentInspectObject.SetActive(true);
        currentInspectObject.name = prefabToSpawn.name + "_Inspect";
        // 应用缩放
        currentInspectObject.transform.localScale = prefabToSpawn.transform.localScale * inspectScale;
        // 位置定位逻辑
        currentInspectObject.transform.position = fixedSpawnPosition;
        Vector3 directionToCamera = playerCameraTransform.position - currentInspectObject.transform.position;
        directionToCamera.y = 0; 
        if (directionToCamera != Vector3.zero)
        {
            currentInspectObject.transform.rotation = Quaternion.LookRotation(directionToCamera);
            currentInspectObject.transform.Rotate(0, 180, 0); 
        }
        currentInspectObject.transform.SetParent(null);
        Debug.Log($"<color=green>[检视系统]</color> 已生成检视物体: <b>{currentInspectObject.name}</b>\n" +
                  $"├── 世界坐标: {currentInspectObject.transform.position}\n" +
                  $"├── 源物体: {(inspectModelPrefab != null ? inspectModelPrefab.name : this.name)}\n" +
                  $"└── 缩放: {currentInspectObject.transform.localScale}");
        //处理组件
        //递归移除克隆体及其所有子物体上的 InspectableItem，防止大模型内部嵌套触发
        InspectableItem[] oldScripts = currentInspectObject.GetComponentsInChildren<InspectableItem>(true);
        foreach (var script in oldScripts) Destroy(script);
        // 递归移除Rigidbody，防止物理掉落
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
    }
    // AI辅助生成：DeepSeek-R1-0528, 2026-04-23
    void Update()
    {
        if (isInspecting && currentInspectObject != null)
        {
            if (Input.GetMouseButton(1))
            {
                // 右键长按手动旋转 
                float rotX = Input.GetAxis("Mouse X") * manualRotationSpeed * Time.deltaTime;
                float rotY = Input.GetAxis("Mouse Y") * manualRotationSpeed * Time.deltaTime;
                // 左右移动鼠标 ，绕世界Y轴旋转
                currentInspectObject.transform.Rotate(Vector3.up, -rotX, Space.World);
                // 上下移动鼠标绕摄像机右轴旋转
                // 之前是绕世界 Right (X) 轴，导致如果玩家侧身看物体，旋转会变成侧翻
                currentInspectObject.transform.Rotate(playerCameraTransform.right, rotY, Space.World);
            }
            else
            {
                // AI辅助生成：DeepSeek-R1-0528, 2026-04-23
                currentInspectObject.transform.Rotate(Vector3.up, autoRotationSpeed * Time.deltaTime, Space.World);
            }
        }
    }

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
            BoxCollider box = obj.AddComponent<BoxCollider>();
            Renderer[] renderers = obj.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length > 0)
            {
                Bounds bounds = renderers[0].bounds;
                for (int i = 1; i < renderers.Length; i++)
                {
                    bounds.Encapsulate(renderers[i].bounds);
                }
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
