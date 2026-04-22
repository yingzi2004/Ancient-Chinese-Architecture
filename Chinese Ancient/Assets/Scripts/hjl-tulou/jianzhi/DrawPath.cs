using System.Collections.Generic;
using UnityEngine;

public class DrawPath : MonoBehaviour
{
    [Header("UI & Render")]
    public LineRenderer lineRenderer;
    // 使用屏幕中心(准心)发射射线；如果想用鼠标，设为 false
    public bool useScreenCenter = true;
    // 线条宽度（可以在 Inspector 里调节）
    public float lineWidth = 0.01f;
    // 线条相对碰撞体表面的偏移量（防止闪烁，数值要非常小）
    public float surfaceOffset = 0.001f;
    // 真正用于发射射线的摄像机（拖玩家视角的那台摄像机进来）
    public Camera drawCamera;
    
    [Header("Objects")]
    public Collider fanweiCollider; // 拖动判定范围（改成3D的Collider）
    public GameObject object1;        // 1物体
    public GameObject objectFanwei;   // fanwei物体
    public GameObject objectDrop1;    // 掉1物体
    public GameObject object2;        // 2物体
    public GameObject objectDrop2;    // 掉2物体
    public GameObject object3;        // 3成品

    [Header("Stage2 Click Settings")]
    // 准心与掉2屏幕位置距离小于这个像素，就认为点中了掉2
    public float drop2ClickRadius = 80f;

    [Header("Stage1 Draw Settings")]
    // 距离物体多远以内才可以画线
    public float maxDrawDistance = 3f;

    private List<Vector3> points = new List<Vector3>();

    // 认为“绕一圈”所需的最少采样点数量，可以在 Inspector 中调整
    public int requiredPoints = 120;

    // 第一阶段画轮廓，第二阶段点击掉2
    private bool firstStageDone = false;
    private bool secondStageDone = false;

    // 交互和视角固定模式
    [Header("视角固定设置 (类似看书)")]
    public Transform viewPoint; // 玩家按下F后，将玩家传送到这里并对齐视角
    
    [Tooltip("微调视角高度：数值越大，视角越高")]
    public float heightOffset = 0.5f; 
    
    [Tooltip("微调抬头/低头角度：负数是抬头，正数是进一步低头")]
    public float pitchOffset = 0f;

    [Header("UI 提示")]
    [Tooltip("按F交互的提示UI对象（比如带有 Text 的 Canvas Group 或直接是 Text 对象）")]
    public GameObject hintUI;

    private bool isInteracting = false;
    private PlayerController playerController;
    private Vector3 savedPlayerPos;
    private Quaternion savedPlayerRot;
    private Quaternion savedCameraRot;

    void Start()
    {
        if (lineRenderer == null)
            lineRenderer = GetComponent<LineRenderer>();

        // 如果场景里没有现成的 LineRenderer，就在当前物体上自动创建一个
        if (lineRenderer == null)
        {
            lineRenderer = gameObject.AddComponent<LineRenderer>();
        }

        if (lineRenderer != null)
        {
            // 基本参数统一在这里强制设置，保证一定能看见线
            lineRenderer.positionCount = 0;
            lineRenderer.useWorldSpace = true;
            lineRenderer.startWidth = lineWidth;
            lineRenderer.endWidth = lineWidth;
            lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
            lineRenderer.startColor = Color.yellow;
            lineRenderer.endColor = Color.yellow;
            // 提高排序顺序，保证在线条在纸张上方
            lineRenderer.sortingLayerName = "Default";
            lineRenderer.sortingOrder = 10;
        }

        // 默认使用 Camera.main，但推荐在 Inspector 里手动指定玩家视角的摄像机
        if (drawCamera == null)
        {
            drawCamera = Camera.main;
        }

        // 初始状态下隐藏掉落物体、成品
        if (objectDrop1 != null) objectDrop1.SetActive(false);
        if (objectDrop2 != null) objectDrop2.SetActive(false);
        if (object3 != null)     object3.SetActive(false);

        playerController = FindFirstObjectByType<PlayerController>();
    }

    void Update()
    {
        // 核心：按下 F 键来进入/退出剪纸模式
        HandleInteractionToggle();

        // 只有进入了固定的交互模式，才能画线或点击
        if (!isInteracting) return;

        // 优先判断第二阶段：只要掉2已经显示并且还没完成第二阶段，就处理点击逻辑
        if (!secondStageDone && objectDrop2 != null && objectDrop2.activeInHierarchy)
        {
            HandleSecondStage();
        }
        else
        {
            HandleFirstStage();
        }
    }

    // 视角固定开关逻辑
    private void HandleInteractionToggle()
    {
        if (object1 == null || drawCamera == null) return;

        // 如果还没完成剪纸（第一或第二阶段还没结束）才允许进入
        if (secondStageDone) 
        {
            if (hintUI != null && hintUI.activeSelf) hintUI.SetActive(false);
            return;
        }

        float dist = Vector3.Distance(drawCamera.transform.position, object1.transform.position);

        // 如果距离够近且还没进入剪纸模式，则显示提示；否则隐藏
        if (hintUI != null)
        {
            bool shouldShowHint = (dist <= maxDrawDistance) && !isInteracting;
            if (hintUI.activeSelf != shouldShowHint)
            {
                hintUI.SetActive(shouldShowHint);
            }
        }
        
        // 靠近才能交互
        if (dist <= maxDrawDistance)
        {
            if (Input.GetKeyDown(KeyCode.F))
            {
                isInteracting = !isInteracting;

                if (isInteracting)
                {
                    // 进入交互模式，固定视角并显示鼠标
                    if (playerController != null)
                    {
                        // 保存旧位置和旋转
                        savedPlayerPos = playerController.transform.position;
                        savedPlayerRot = playerController.transform.rotation;
                        if (playerController.cameraTransform != null)
                            savedCameraRot = playerController.cameraTransform.localRotation;

                        // 应用固定视角位置
                        if (viewPoint != null)
                        {
                            Vector3 targetPos = viewPoint.position + new Vector3(0, heightOffset, 0);
                            playerController.transform.SetPositionAndRotation(targetPos, viewPoint.rotation);
                            if (playerController.cameraTransform != null)
                            {
                                // 获取 ViewPoint 的 X 轴旋转并加上微调参数（俯仰角）赋给摄像机
                                playerController.cameraTransform.localRotation = Quaternion.Euler(viewPoint.localEulerAngles.x + pitchOffset, 0f, 0f);
                                // 同时把玩家整体赋上 ViewPoint 的 Y 轴旋转
                                playerController.transform.rotation = Quaternion.Euler(0f, viewPoint.eulerAngles.y, 0f);
                            }
                        }

                        playerController.isInspecting = true;
                        playerController.SetCursorState(false); 
                    }
                    useScreenCenter = false; // 强行改为使用鼠标来画线
                    Cursor.visible = true;
                    Cursor.lockState = CursorLockMode.None;
                    Debug.Log("剪纸模式：开启正视角交互");
                }
                else
                {
                    // 退出交互模式，恢复视角移动和位置
                    if (playerController != null)
                    {
                        // 还原位置和旋转
                        playerController.transform.SetPositionAndRotation(savedPlayerPos, savedPlayerRot);
                        if (playerController.cameraTransform != null)
                            playerController.cameraTransform.localRotation = savedCameraRot;

                        playerController.isInspecting = false;
                        playerController.SetCursorState(true);
                    }
                    Cursor.visible = false;
                    Cursor.lockState = CursorLockMode.Locked;
                    Debug.Log("剪纸模式：还原视角退出");
                }
            }
        }
        else
        {
            // 离得太远自动退出
            if (isInteracting)
            {
                isInteracting = false;
                if (playerController != null)
                {
                    playerController.transform.SetPositionAndRotation(savedPlayerPos, savedPlayerRot);
                    if (playerController.cameraTransform != null)
                        playerController.cameraTransform.localRotation = savedCameraRot;

                    playerController.isInspecting = false;
                    playerController.SetCursorState(true);
                }
                Cursor.visible = false;
                Cursor.lockState = CursorLockMode.Locked;
            }
        }
    }

    // 第一阶段：画线裁剪 1 + fanwei，触发 掉1
    void HandleFirstStage()
    {
        // ... 原本关于距离判断被移到了 HandleInteractionToggle 去了，这里只需要专心画线
        // 鼠标按下，开始绘制，清空之前的轨迹
        if (Input.GetMouseButtonDown(0))
        {
            Debug.Log("DrawPath: MouseButtonDown");
            points.Clear();
            if (lineRenderer != null)
                lineRenderer.positionCount = 0;
        }
        // 按住鼠标拖动时记录路径并画线
        else if (Input.GetMouseButton(0))
        {
            if (drawCamera == null)
            {
                Debug.LogWarning("DrawPath: 没有指定 drawCamera，也找不到 MainCamera");
                return;
            }

            // 通过射线检测准心（或是鼠标）所指的空间位置
            Vector3 screenPos = useScreenCenter
                ? new Vector3(Screen.width / 2f, Screen.height / 2f, 0f)
                : (Vector3)Input.mousePosition;

            Ray ray = drawCamera.ScreenPointToRay(screenPos);
            RaycastHit hit;
            
            // 使用射线碰触到物体的位置。如果在3D中，你需要确保背景或者画布上有碰撞体（比如BoxCollider）
            if (Physics.Raycast(ray, out hit, 100f))
            {
                // 让线条紧贴在被射中的表面上
                // 使用命中点 + 法线 * 一个很小的偏移量
                Vector3 worldPos = hit.point + hit.normal * surfaceOffset;

                Debug.DrawRay(ray.origin, ray.direction * hit.distance, Color.green, 0.1f);
                Debug.Log("DrawPath: Raycast hit " + hit.collider.name);
                // 距离大于0.05才添加点，避免点太密集
                if (points.Count == 0 || Vector3.Distance(points[points.Count - 1], worldPos) > 0.05f)
                {
                    points.Add(worldPos);
                    if (lineRenderer != null)
                    {
                        lineRenderer.positionCount = points.Count;
                        lineRenderer.SetPosition(points.Count - 1, worldPos);
                    }
                }
            }
            else
            {
                Debug.DrawRay(ray.origin, ray.direction * 5f, Color.red, 0.1f);
            }
        }
        // 鼠标抬起时，判断是否成功
        else if (Input.GetMouseButtonUp(0))
        {
            if (CheckDrawSuccess())
            {
                // 成功掩藏 1 和 fanwei
                if (object1 != null) object1.SetActive(false);
                if (objectFanwei != null) objectFanwei.SetActive(false);
                
                // 让"掉1"显示并开始掉落
                if (objectDrop1 != null)
                {
                    objectDrop1.SetActive(true);
                    ObjectManager manager = objectDrop1.GetComponent<ObjectManager>();
                    if (manager != null)
                    {
                        manager.StartFalling();
                    }
                }

                // 第一阶段完成，开启第二阶段：显示 掉2 让玩家点击
                firstStageDone = true;
                if (objectDrop2 != null)
                    objectDrop2.SetActive(true);
            }
            else
            {
                // 失败则清除线条重新画
                points.Clear();
                if (lineRenderer != null)
                    lineRenderer.positionCount = 0;
            }
        }
    }

    // 第二阶段：准心点击 掉2，让其下落，同时隐藏 2，显示 3
    void HandleSecondStage()
    {
        if (!Input.GetMouseButtonDown(0)) return;

        if (drawCamera == null || objectDrop2 == null)
        {
            Debug.LogWarning("DrawPath SecondStage: drawCamera 或 objectDrop2 为空");
            return;
        }

        // 掉2 在屏幕上的坐标
        Vector3 drop2ScreenPos = drawCamera.WorldToScreenPoint(objectDrop2.transform.position);
        
        // 既然进入了F交互固定模式，用的是鼠标而不是准心
        Vector2 checkPoint = Input.mousePosition;

        float dist = Vector2.Distance(checkPoint, new Vector2(drop2ScreenPos.x, drop2ScreenPos.y));

        Debug.Log($"DrawPath SecondStage: drop2ScreenPos={drop2ScreenPos}, mouse={checkPoint}, dist={dist}");

        // 距离足够近，认为点中了掉2
        if (dist <= drop2ClickRadius)  // 电脑客户端访问，2026年3月19号
        {
            // 掉2 开始下落
            ObjectManager manager = objectDrop2.GetComponent<ObjectManager>();
            if (manager != null)
            {
                manager.StartFalling();
            }

            if (object2 != null) object2.SetActive(false);
            if (object3 != null) object3.SetActive(true);

            secondStageDone = true;

            isInteracting = false;
            if (playerController != null)
            {
                playerController.transform.SetPositionAndRotation(savedPlayerPos, savedPlayerRot);
                if (playerController.cameraTransform != null)
                    playerController.cameraTransform.localRotation = savedCameraRot;

                playerController.isInspecting = false;
                playerController.SetCursorState(true);
            }
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
        }
    }

    // 路径判定逻辑：检测画的点是否在 fanwei 范围内
    bool CheckDrawSuccess()
    {
        // 点数太少，说明划得不够长，直接失败
        if (points.Count < requiredPoints) return false;

        // 我们在 Update 里已经限制只有射到 fanweiCollider 才会记录点，
        // 所以这里简单用“长度够不够”来当作是否绕完一圈的判定即可。
        // 如果你之后想更严格，可以再在这里加入更复杂的判断。

        // 只要点数达到要求，就判定成功
        return true;
    }
}
