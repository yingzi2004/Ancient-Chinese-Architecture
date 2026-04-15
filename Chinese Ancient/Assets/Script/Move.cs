using UnityEngine;
using UnityEngine.SceneManagement; // 引入场景管理

public class PlayerController : MonoBehaviour
{
    [Header("--- 移动设置 ---")]
    public float moveSpeed = 5.0f;
    public float runSpeed = 8.0f; // 跑步速度
    public float jumpHeight = 1.2f;
    public float gravity = -20f; 
    
    // 新增：检视状态控制
    [HideInInspector]
    public bool isInspecting = false;

    [Header("--- 视角设置 ---")]
    public Transform cameraTransform;
    public float mouseSensitivity = 2.0f; 
    public float rotationSharpness = 50f;

    [Header("--- 交互设置 ---")]
    public float interactDistance = 10.0f; // 建议设为10，防止距离太短点不到按钮
    public LayerMask interactableLayer;   // 确保面板里勾选了 "Interactable"

    private CharacterController controller;
    private Vector3 velocity;
    private bool isGrounded;
    private bool isCursorLocked = true;

    private float xRotation = 0f;
    private float yRotation = 0f;

    void Start()
    {
        // === 光照修复核心逻辑 ===
        // 在玩家主角降生到新场景的一瞬间，强制把当前场景设为了主力烘焙场景，
        // 并一巴掌拍醒全局光照刷新（解决传送后土楼/外面漆黑停电的问题）
        SceneManager.SetActiveScene(gameObject.scene);
        DynamicGI.UpdateEnvironment();
        // =======================

        controller = GetComponent<CharacterController>();
        if (cameraTransform == null && Camera.main != null) 
            cameraTransform = Camera.main.transform;
        
        yRotation = transform.localRotation.eulerAngles.y;
        SetCursorState(true);
    }

    void Update()
    {
        HandleCursorToggle();

        // 只有在光标锁定时且不在检视状态才允许移动
        if (isCursorLocked && controller != null && !isInspecting) 
        {
            HandleMovement();
        }

        // 处理点击动作（准星交互核心）
        // 只有在不检视UI时才处理交互（避免拦截UI点击）
        if (Input.GetMouseButtonDown(0) && !isInspecting)
        {
            HandleInteraction();
        }
    }

    void LateUpdate()
    {
        if (isCursorLocked && cameraTransform != null && !isInspecting)
        {
            HandleRotation();
        }
    }

    void HandleInteraction()
    {
        Debug.Log("<color=cyan>[交互系统]</color> 检测到点击动作！");

        if (cameraTransform == null) return;

        // 发射射线
        Ray ray = new Ray(cameraTransform.position, cameraTransform.forward);
        RaycastHit hit;

        // 在 Scene 窗口画出红线，方便调试
        Debug.DrawRay(ray.origin, ray.direction * interactDistance, Color.red, 2f);

        if (Physics.Raycast(ray, out hit, interactDistance, interactableLayer))
        {
            Debug.Log($"<color=green>[命中信息]</color> 撞击物体: <b>{hit.collider.name}</b>");

            // 优化后：通用接口交互
            // 尝试获取可交互接口（循环向上查找，直到根节点）
            IInteractable interactable = hit.collider.GetComponent<IInteractable>();
            
            // 如果自身没有，向上寻找
            if (interactable == null)
            {
                interactable = hit.collider.GetComponentInParent<IInteractable>();
            }

            if (interactable != null)
            {
                // 打印成功找到的信息，方便调试
                MonoBehaviour script = interactable as MonoBehaviour;
                string scriptName = script != null ? script.gameObject.name : "Unknown";
                Debug.Log($"<color=green>[交互成功]</color> 点击了 {hit.collider.name}，触发了父级 {scriptName} 上的交互脚本。");

                interactable.Interact();
            }
            else 
            {
                 // 打印失败的详细路径，帮助排查
                 string path = GetHierarchyPath(hit.collider.transform);
                 Debug.Log($"<color=yellow>[交互提示]</color> 击中物体: <b>{hit.collider.name}</b> (Layer: {LayerMask.LayerToName(hit.collider.gameObject.layer)})\n" +
                           $"完整路径: {path}\n" +
                           $"原因: 该物体或其父级链上没有任何挂载 IInteractable 接口的脚本。请检查 inspectableItem 脚本是否挂在了正确的父物体上。");
            }
        }
        else
        {
            Debug.Log("<color=gray>[系统结果]</color> 射线未命中任何 Interactable 图层的物体。");
        }
    }

    // 用于打印层级路径的辅助方法
    string GetHierarchyPath(Transform transform)
    {
        string path = transform.name;
        while (transform.parent != null)
        {
            transform = transform.parent;
            path = transform.name + "/" + path;
        }
        return path;
    }

    void HandleCursorToggle()
    {
        if (Input.GetKeyDown(KeyCode.Escape)) SetCursorState(!isCursorLocked);
        if (!isCursorLocked && Input.GetMouseButtonDown(1)) SetCursorState(true);
    }

    public void SetCursorState(bool locked)
    {
        isCursorLocked = locked;
        
        // 增加此处的判断，避免打字时被强行锁死鼠标
        if (!isInspecting)
        {
            Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !locked;
        }
    }

    void HandleMovement()
    {
        isGrounded = controller.isGrounded;
        if (isGrounded && velocity.y < 0) velocity.y = -2f; 

        float x = Input.GetAxisRaw("Horizontal");
        float z = Input.GetAxisRaw("Vertical");

        Vector3 move = (transform.right * x + transform.forward * z).normalized;

        // 如果按住键盘左侧的Shift键，则使用跑步速度，否则使用正常移动速度
        float currentSpeed = Input.GetKey(KeyCode.LeftShift) ? runSpeed : moveSpeed;
        controller.Move(move * currentSpeed * Time.deltaTime);

        if (Input.GetButtonDown("Jump") && isGrounded)
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);

        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);
    }

    void HandleRotation()
    {
        yRotation += Input.GetAxisRaw("Mouse X") * mouseSensitivity;
        xRotation -= Input.GetAxisRaw("Mouse Y") * mouseSensitivity;
        xRotation = Mathf.Clamp(xRotation, -85f, 85f);

        transform.localRotation = Quaternion.Euler(0f, yRotation, 0f);
        cameraTransform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
    }
}

