using UnityEngine;
using UnityEngine.EventSystems; // 用于检测是否点到了UI

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [Header("--- 移动设置 ---")]
    public float moveSpeed = 5.0f;
    public float jumpHeight = 1.2f;
    public float gravity = -20f; 

    [Header("--- 视角设置 ---")]
    public Transform cameraTransform;
    public float mouseSensitivity = 0.2f; 
    public float rotationSharpness = 50f;

    [Header("--- 交互设置 ---")]
    public float interactDistance = 5.0f;
    public LayerMask interactableLayer;

    private CharacterController controller;
    private Vector3 velocity;
    private bool isGrounded;
    private bool isCursorLocked = true;

    private float xRotation = 0f;
    private float yRotation = 0f;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        yRotation = transform.localRotation.eulerAngles.y;
        SetCursorState(true);
        Application.targetFrameRate = 60;
    }

    void Update()
    {
        // 1. 处理鼠标锁定/解锁的切换逻辑
        HandleCursorToggle();

        // 2. 只有锁定时才处理位移
        if (isCursorLocked) 
        {
            HandleMovement();
        }

        // 3. 处理交互点击
        if (Input.GetMouseButtonDown(0))
        {
            // 如果点到的是UI（如菜单按钮），则不触发3D世界的射线交互
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;
            
            HandleInteraction();
        }
    }

    void LateUpdate()
    {
        // 4. 只有锁定鼠标时才旋转视角
        if (isCursorLocked)
        {
            HandleRotation();
        }
    }

    // 修改后的切换逻辑：按Esc切换状态，或者在解锁时点击右键返回
    void HandleCursorToggle()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            SetCursorState(!isCursorLocked); // 直接取反：锁定时按Esc解锁，解锁时按Esc锁定
        }

        // 额外增加：指针模式下，点击鼠标右键可以快速回到游戏（锁定视角）
        if (!isCursorLocked && Input.GetMouseButtonDown(1))
        {
            SetCursorState(true);
        }
    }

    void SetCursorState(bool locked)
    {
        isCursorLocked = locked;
        Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !locked;
        
        // 当重新锁定时，重置旋转输入，防止镜头瞬间“跳弹”
        if (locked)
        {
            // 保持当前角度，避免视觉冲击
            currentMouseDelta = Vector2.zero; 
        }
    }
    
    private Vector2 currentMouseDelta; // 用于平滑处理

    void HandleMovement()
    {
        isGrounded = controller.isGrounded;
        if (isGrounded && velocity.y < 0) velocity.y = -2f; 

        float x = Input.GetAxisRaw("Horizontal");
        float z = Input.GetAxisRaw("Vertical");

        Vector3 move = (transform.right * x + transform.forward * z).normalized;
        controller.Move(move * moveSpeed * Time.deltaTime);

        if (Input.GetButtonDown("Jump") && isGrounded)
        {
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
        }

        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);
    }

    void HandleRotation()
    {
        float mouseX = Input.GetAxisRaw("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxisRaw("Mouse Y") * mouseSensitivity;

        yRotation += mouseX;
        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -85f, 85f);

        transform.localRotation = Quaternion.Slerp(transform.localRotation, Quaternion.Euler(0f, yRotation, 0f), Time.deltaTime * rotationSharpness);
        cameraTransform.localRotation = Quaternion.Slerp(cameraTransform.localRotation, Quaternion.Euler(xRotation, 0f, 0f), Time.deltaTime * rotationSharpness);
    }

    void HandleInteraction()
    {
        Ray ray;
        if (isCursorLocked)
            ray = new Ray(cameraTransform.position, cameraTransform.forward);
        else
            ray = Camera.main.ScreenPointToRay(Input.mousePosition);

        RaycastHit hit;
        if (Physics.Raycast(ray, out hit, interactDistance, interactableLayer))
        {
            Debug.Log("交互成功！点击了物体：" + hit.collider.name);
            
            // --- 修复点：交互完之后，如果你希望立即回到第一人称视角，取消下面这行注释 ---
            // SetCursorState(true); 
        }
    }
}