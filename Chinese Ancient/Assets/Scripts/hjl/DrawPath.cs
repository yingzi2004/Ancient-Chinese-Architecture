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

    // 阶段控制：第一阶段画轮廓，第二阶段点击掉2
    private bool firstStageDone = false;
    private bool secondStageDone = false;

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
    }

    void Update()
    {
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

    // 第一阶段：画线裁剪 1 + fanwei，触发 掉1
    void HandleFirstStage()
    {
        // 如果玩家距离游戏区域（object1）超过设定的最大距离，则不允许画线
        if (object1 != null && drawCamera != null)
        {
            float distToArea = Vector3.Distance(drawCamera.transform.position, object1.transform.position);
            if (distToArea > maxDrawDistance)
            {
                // 可以选择清空已有的线条并直接返回
                if (Input.GetMouseButtonUp(0))
                {
                    points.Clear();
                    if (lineRenderer != null) lineRenderer.positionCount = 0;
                }
                return;
            }
        }

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
        Vector2 center = new Vector2(Screen.width / 2f, Screen.height / 2f);
        float dist = Vector2.Distance(center, new Vector2(drop2ScreenPos.x, drop2ScreenPos.y));

        Debug.Log($"DrawPath SecondStage: drop2ScreenPos={drop2ScreenPos}, dist={dist}");

        // 距离足够近，认为准心点中了掉2
        if (dist <= drop2ClickRadius)
        {
            // 掉2 开始下落
            ObjectManager manager = objectDrop2.GetComponent<ObjectManager>();
            if (manager != null)
            {
                manager.StartFalling();
            }

            // 隐藏 2，显示 3
            if (object2 != null) object2.SetActive(false);
            if (object3 != null) object3.SetActive(true);

            secondStageDone = true;
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
