using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("--- 移动设置 ---")]
    public float moveSpeed = 5.0f;
    public float jumpHeight = 1.2f;
    public float gravity = -20f; 

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
        controller = GetComponent<CharacterController>();
        if (cameraTransform == null && Camera.main != null) 
            cameraTransform = Camera.main.transform;
        
        yRotation = transform.localRotation.eulerAngles.y;
        SetCursorState(true);
    }

    void Update()
    {
        HandleCursorToggle();

        // 只有在光标锁定时才允许移动和旋转
        if (isCursorLocked && controller != null) 
        {
            HandleMovement();
        }

        // 处理点击动作（准星交互核心）
        if (Input.GetMouseButtonDown(0))
        {
            HandleInteraction();
        }
    }

    void LateUpdate()
    {
        if (isCursorLocked && cameraTransform != null)
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
            // 尝试获取可交互接口（先自身，后父级）
            IInteractable interactable = hit.collider.GetComponent<IInteractable>();
            if (interactable == null) interactable = hit.collider.GetComponentInParent<IInteractable>();
            
            if (interactable != null)
            {
                interactable.Interact();
            }
            else 
            {
                 Debug.Log("<color=yellow>[交互提示]</color> 该物体在 Interactable 层，但没有挂载实现 IInteractable 接口的脚本。");
            }
        }
        else
        {
            Debug.Log("<color=gray>[系统结果]</color> 射线未命中任何 Interactable 图层的物体。");
        }
    }

    void HandleCursorToggle()
    {
        if (Input.GetKeyDown(KeyCode.Escape)) SetCursorState(!isCursorLocked);
        if (!isCursorLocked && Input.GetMouseButtonDown(1)) SetCursorState(true);
    }

    void SetCursorState(bool locked)
    {
        isCursorLocked = locked;
        Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !locked;
    }

    void HandleMovement()
    {
        isGrounded = controller.isGrounded;
        if (isGrounded && velocity.y < 0) velocity.y = -2f; 

        float x = Input.GetAxisRaw("Horizontal");
        float z = Input.GetAxisRaw("Vertical");

        Vector3 move = (transform.right * x + transform.forward * z).normalized;
        controller.Move(move * moveSpeed * Time.deltaTime);

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

