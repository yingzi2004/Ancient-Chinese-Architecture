using UnityEngine;
using UnityEngine.SceneManagement; //引入场景管理
public class PlayerController : MonoBehaviour
{
    [Header("--- 移动设置 ---")]
    public float moveSpeed = 5.0f;
    public float runSpeed = 8.0f; //跑步速度
    public float jumpHeight = 1.2f;
    public float gravity = -20f;
    //检视状态控制
    [HideInInspector]
    public bool isInspecting = false;
    //乘船状态控制
    [HideInInspector]
    public bool isOnBoat = false;
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
        SceneManager.SetActiveScene(gameObject.scene);
        DynamicGI.UpdateEnvironment();
        controller = GetComponent<CharacterController>();
        if (cameraTransform == null && Camera.main != null)
            cameraTransform = Camera.main.transform;
        yRotation = transform.localRotation.eulerAngles.y;
        SetCursorState(true);
    }
    void Update()
    {
        HandleCursorToggle();
        if (isCursorLocked && controller != null && !isInspecting)
        {
            HandleMovement();
        }
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
        Debug.DrawRay(ray.origin, ray.direction * interactDistance, Color.red, 2f);
        if (Physics.Raycast(ray, out hit, interactDistance, interactableLayer))
        {
            Debug.Log($"<color=green>[命中信息]</color> 撞击物体: <b>{hit.collider.name}</b>");
            IInteractable interactable = hit.collider.GetComponent<IInteractable>();
            if (interactable == null)
            {
                interactable = hit.collider.GetComponentInParent<IInteractable>();
            }
            if (interactable != null)
            {
                MonoBehaviour script = interactable as MonoBehaviour;
                string scriptName = script != null ? script.gameObject.name : "Unknown";
                Debug.Log($"<color=green>[交互成功]</color> 点击了 {hit.collider.name}，触发了父级 {scriptName} 上的交互脚本。");
                interactable.Interact();
            }
            else
            {
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
        if (!isInspecting)
        {
            Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !locked;
        }
    }
    void HandleMovement()
    {
        if (isOnBoat) return; 
        isGrounded = controller.isGrounded;
        if (isGrounded && velocity.y < 0) velocity.y = -2f;
        float x = Input.GetAxisRaw("Horizontal");
        float z = Input.GetAxisRaw("Vertical");
        Vector3 move = (transform.right * x + transform.forward * z).normalized;
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
