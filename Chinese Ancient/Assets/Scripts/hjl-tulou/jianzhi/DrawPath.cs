// AI辅助生成：DeepSeek-R1-0528, 2026-04-23 (优化点：阶段切换逻辑简化)
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
    public Camera drawCamera;
    
    [Header("Objects")]
    public Collider fanweiCollider; // 拖动判定范围
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

        if (drawCamera == null)
        {
            drawCamera = Camera.main;
        }

        if (objectDrop1 != null) objectDrop1.SetActive(false);
        if (objectDrop2 != null) objectDrop2.SetActive(false);
        if (object3 != null)     object3.SetActive(false);

        playerController = FindFirstObjectByType<PlayerController>();
    }

    void Update()
    {

        HandleInteractionToggle();

        // 只有进入了固定的交互模式，才能画线或点击
        if (!isInteracting) return;
// AI辅助生成：DeepSeek-R1-0528, 2026-04-23 (优化点：阶段分支简化)

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

        // 如果还没完成剪纸才允许进入
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
                                // 获取 ViewPoint 的 X 轴旋转并加上微调参数赋给摄像机
                                playerController.cameraTransform.localRotation = Quaternion.Euler(viewPoint.localEulerAngles.x + pitchOffset, 0f, 0f);
                                playerController.transform.rotation = Quaternion.Euler(0f, viewPoint.eulerAngles.y, 0f);
                            }
                        }

                        playerController.isInspecting = true;
                        playerController.SetCursorState(false); 
                    }
                    useScreenCenter = false; 
                    Cursor.visible = true;
                    Cursor.lockState = CursorLockMode.None;
                    Debug.Log("剪纸模式：开启正视角交互");
                }
                else
                {
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

    void HandleFirstStage()
    {

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

            // 通过射线检测准心所指的空间位置
            Vector3 screenPos = useScreenCenter
                ? new Vector3(Screen.width / 2f, Screen.height / 2f, 0f)
                : (Vector3)Input.mousePosition;

            Ray ray = drawCamera.ScreenPointToRay(screenPos);
            RaycastHit hit;
            
            // 使用射线碰触到物体的位置。如果在3D中，你需要确保背景或者画布上有碰撞体
            if (Physics.Raycast(ray, out hit, 100f))
            {

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

        Vector2 checkPoint = Input.mousePosition;

        float dist = Vector2.Distance(checkPoint, new Vector2(drop2ScreenPos.x, drop2ScreenPos.y));

        Debug.Log($"DrawPath SecondStage: drop2ScreenPos={drop2ScreenPos}, mouse={checkPoint}, dist={dist}");

        // 距离足够近，认为点中了掉2
        if (dist <= drop2ClickRadius)
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

    bool CheckDrawSuccess()
    {
        // 点数太少，失败
        if (points.Count < requiredPoints) return false;
        return true;
    }
}
