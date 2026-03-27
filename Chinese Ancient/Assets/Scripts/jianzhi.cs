using UnityEngine;
using System.Collections.Generic;
using System.Reflection;

[RequireComponent(typeof(Collider))]
[RequireComponent(typeof(Renderer))]
public class jianzhi : MonoBehaviour
{
    [Header("原始剪纸贴图（建议手动指定）")]
    public Texture2D sourceTexture;

    [Header("剪刀/画笔的粗细")]
    public int brushSize = 10;

    [Header("交互设置")]
    [Tooltip("如果使用准心交互（第一人称视角），请勾选此项。射线将始终从屏幕正中心发射。")]
    public bool useCrosshair = false;
    [Tooltip("准心模式下：当准心接近fanwei时，自动释放鼠标改为左键绘制；离开后恢复准心锁定")]
    public bool autoSwitchToMouseNearFanwei = true;
    [Tooltip("判定准心接近fanwei的检测距离")]
    public float crosshairDetectDistance = 5f;
    [Tooltip("启用平面命中兜底，避免碰撞体异常时无法绘制")]
    public bool enablePlaneFallback = true;
    [Tooltip("进入绘制区时临时禁用这些控制脚本（例如 PlayerController），离开时自动恢复")]
    public MonoBehaviour[] pauseWhenDrawing;
    [Tooltip("未手动指定时，自动在场景中查找 PlayerController 脚本")]
    public bool autoFindPlayerController = true;
    [Tooltip("按下该按键可切换到鼠标绘制模式（显示鼠标并允许左键绘制）")]
    public KeyCode toggleMouseDrawKey = KeyCode.I;

    [Header("拖动轨迹显示")]
    public Color trailColor = new Color(1f, 0.95f, 0.55f, 1f);
    public float trailWidth = 0.03f;
    public float trailGlowIntensity = 2.5f;
    public float trailPointMinDistance = 0.01f;
    public float trailSurfaceOffset = 0.0015f;
    public bool clearTrailOnRelease = false;
    public bool clearTrailOnNewStroke = true;

    [Header("评判设置")]
    [Tooltip("指定fanwei对象（需要其带有碰撞体Collider）")]
    public Transform fanweiObject;
    [Tooltip("允许偏离fanwei轮廓的最大距离")]
    public float toleranceRadius = 0.1f;
    [Tooltip("评判成功的达标百分比(0~1)")]
    public float successThreshold = 0.7f;
    
    [Header("成功后的对象控制")]
    [Tooltip("对应“1”这个图片的物体，成功后将被隐藏")]
    public GameObject obj1;
    [Tooltip("对应“掉1”这个物体，成功后会掉落")]
    public GameObject objDrop1;

    // 评判统计
    private int totalDragFrameCount = 0;
    private int validDragFrameCount = 0;

    private Texture2D editableTexture;
    private Material mat;
    private Collider col;
    private int texWidth;
    private int texHeight;
    private LineRenderer trailRenderer;
    private Material trailMaterial;
    private readonly List<Vector3> trailPoints = new List<Vector3>();
    private bool isInDrawZone = false;
    private bool cursorWasAutoUnlocked = false;
    private bool forceMouseDrawMode = false;
    private readonly List<MonoBehaviour> pausedBehaviours = new List<MonoBehaviour>();

    void Start()
    {
        // 1. 获取当前物体的材质
        Renderer rend = GetComponent<Renderer>();
        mat = rend.material;
        
        // 2. 优先使用手动指定的贴图；未指定时再尝试从材质读取
        Texture originalTexture = sourceTexture;
        if (originalTexture == null && mat.HasProperty("_BaseMap"))
        {
            originalTexture = mat.GetTexture("_BaseMap") as Texture2D;
        }
        else if (originalTexture == null && mat.HasProperty("_MainTex"))
        {
            originalTexture = mat.GetTexture("_MainTex") as Texture2D;
        }

        // 材质为空贴图时 Unity 常返回默认白贴图，这会导致整张变白
        if (originalTexture == Texture2D.whiteTexture)
        {
            originalTexture = null;
        }

        // 3. 复制一张可读写贴图用于修改
        if (originalTexture != null)
        {
            texWidth = originalTexture.width;
            texHeight = originalTexture.height;
            editableTexture = CreateReadableCopy(originalTexture);

            // 把可编辑的复制版贴图赋给材质
            if (mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", editableTexture);
            else if (mat.HasProperty("_MainTex")) mat.SetTexture("_MainTex", editableTexture);
        }
        else
        {
            Debug.LogError("没有找到原始贴图：请在 jianzhi.sourceTexture 手动指定你的剪纸图片。");
        }

        col = GetComponent<Collider>();
        SetupTrailRenderer();
    }

    void Update()
    {
        HandleToggleMouseDrawMode();
        UpdateInteractionZone();

        if (Input.GetMouseButtonDown(0))
        {
            if (clearTrailOnNewStroke) ClearTrail();
            totalDragFrameCount = 0;
            validDragFrameCount = 0;
        }

        // 按住鼠标左键时进行剪纸
        if (Input.GetMouseButton(0))
        {
            CutPaper();
        }

        if (Input.GetMouseButtonUp(0))
        {
            if (clearTrailOnRelease) ClearTrail();
            
            // 笔划结束时进行评判
            EvaluateStrokeComplete();
        }
    }

    void CutPaper()
    {
        if (Camera.main == null) return;

        if (!forceMouseDrawMode && useCrosshair && autoSwitchToMouseNearFanwei && !isInDrawZone)
        {
            return;
        }

        // 如果是准心交互，永远取屏幕正中心的点；否则取鼠标当前的实际位置
        bool shouldUseCenterRay = useCrosshair && !forceMouseDrawMode && !(autoSwitchToMouseNearFanwei && isInDrawZone);
        Vector3 pointToRay = shouldUseCenterRay ? new Vector3(Screen.width / 2f, Screen.height / 2f, 0f) : Input.mousePosition;
        Ray ray = Camera.main.ScreenPointToRay(pointToRay);
        
        // 使用 RaycastAll 穿透检测，防止前面有 fanwei 的碰撞体挡住鼠标点击
        RaycastHit[] hits = Physics.RaycastAll(ray);
        bool hasDrawn = false;

        foreach (RaycastHit hit in hits)
        {
            // 如果某一层点中了当前纸张
            if (hit.collider == col)
            {
                EraseAtUV(hit.textureCoord);
                AddTrailPoint(hit.point + hit.normal * trailSurfaceOffset);
                
                // 执行评判逻辑：检查是否在fanwei附近
                EvaluateDragPoint(hit.point);
                hasDrawn = true;
                break; // 画到了就跳出循环，避免重复画
            }
        }

        if (!hasDrawn && enablePlaneFallback)
        {
            TryDrawWithPlaneFallback(ray);
        }
    }

    void UpdateInteractionZone()
    {
        if (forceMouseDrawMode)
        {
            return;
        }

        if (!useCrosshair || !autoSwitchToMouseNearFanwei || Camera.main == null || fanweiObject == null)
        {
            return;
        }

        Ray centerRay = Camera.main.ScreenPointToRay(new Vector3(Screen.width / 2f, Screen.height / 2f, 0f));
        RaycastHit[] hits = Physics.RaycastAll(centerRay, crosshairDetectDistance);

        bool nearFanwei = false;
        foreach (RaycastHit hit in hits)
        {
            Transform t = hit.collider.transform;
            if (t == fanweiObject || t.IsChildOf(fanweiObject))
            {
                nearFanwei = true;
                break;
            }
        }

        if (nearFanwei && !isInDrawZone)
        {
            isInDrawZone = true;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            cursorWasAutoUnlocked = true;
            SetPauseBehaviours(true);
        }
        else if (!nearFanwei && isInDrawZone)
        {
            isInDrawZone = false;
            SetPauseBehaviours(false);
            if (cursorWasAutoUnlocked)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
                cursorWasAutoUnlocked = false;
            }
        }
    }

    void HandleToggleMouseDrawMode()
    {
        if (!Input.GetKeyDown(toggleMouseDrawKey))
        {
            return;
        }

        forceMouseDrawMode = !forceMouseDrawMode;

        if (forceMouseDrawMode)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            SetPauseBehaviours(true);
            Debug.Log("<color=cyan>已进入鼠标绘制模式：</color>可直接按住鼠标左键沿 fanwei 进行绘制。");
        }
        else
        {
            SetPauseBehaviours(false);
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            Debug.Log("<color=cyan>已退出鼠标绘制模式：</color>恢复准心交互。");
        }
    }

    void SetPauseBehaviours(bool pause)
    {
        List<MonoBehaviour> targets = GetPauseTargets();
        if (targets.Count == 0) return;

        if (pause)
        {
            pausedBehaviours.Clear();
            for (int i = 0; i < targets.Count; i++)
            {
                MonoBehaviour mb = targets[i];
                if (mb == null || !mb.enabled) continue;
                mb.enabled = false;
                pausedBehaviours.Add(mb);
            }
            ForcePlayerControllerCursorState(false);
        }
        else
        {
            for (int i = 0; i < pausedBehaviours.Count; i++)
            {
                if (pausedBehaviours[i] != null)
                {
                    pausedBehaviours[i].enabled = true;
                }
            }
            pausedBehaviours.Clear();
            ForcePlayerControllerCursorState(true);
        }
    }

    void ForcePlayerControllerCursorState(bool locked)
    {
        List<MonoBehaviour> targets = GetPauseTargets();
        if (targets.Count == 0) return;

        for (int i = 0; i < targets.Count; i++)
        {
            MonoBehaviour mb = targets[i];
            if (mb == null) continue;

            System.Type t = mb.GetType();
            FieldInfo field = t.GetField("isCursorLocked", BindingFlags.Instance | BindingFlags.NonPublic);
            if (field != null && field.FieldType == typeof(bool))
            {
                field.SetValue(mb, locked);
            }
        }
    }

    List<MonoBehaviour> GetPauseTargets()
    {
        List<MonoBehaviour> result = new List<MonoBehaviour>();

        if (pauseWhenDrawing != null)
        {
            for (int i = 0; i < pauseWhenDrawing.Length; i++)
            {
                if (pauseWhenDrawing[i] != null)
                {
                    result.Add(pauseWhenDrawing[i]);
                }
            }
        }

        if (result.Count == 0 && autoFindPlayerController)
        {
            MonoBehaviour[] allBehaviours = FindObjectsOfType<MonoBehaviour>();
            for (int i = 0; i < allBehaviours.Length; i++)
            {
                MonoBehaviour mb = allBehaviours[i];
                if (mb == null) continue;
                if (mb.GetType().Name == "PlayerController")
                {
                    result.Add(mb);
                }
            }
        }

        return result;
    }

    void OnDisable()
    {
        SetPauseBehaviours(false);
    }

    void TryDrawWithPlaneFallback(Ray ray)
    {
        Plane plane = new Plane(transform.forward, transform.position);
        float distance;

        if (!plane.Raycast(ray, out distance))
        {
            plane = new Plane(-transform.forward, transform.position);
            if (!plane.Raycast(ray, out distance))
            {
                return;
            }
        }

        Vector3 worldPoint = ray.GetPoint(distance);
        Vector3 local = transform.InverseTransformPoint(worldPoint);

        float uvX = local.x + 0.5f;
        float uvY = local.y + 0.5f;
        if (uvX < 0f || uvX > 1f || uvY < 0f || uvY > 1f)
        {
            return;
        }

        Vector2 uv = new Vector2(uvX, uvY);
        EraseAtUV(uv);
        AddTrailPoint(worldPoint + transform.forward * trailSurfaceOffset);
        EvaluateDragPoint(worldPoint);
    }

    void EvaluateDragPoint(Vector3 dragPoint)
    {
        totalDragFrameCount++;

        if (fanweiObject == null)
        {
            return; // 没有指定fanwei对象，直接跳过评判
        }

        // 使用球形碰撞检测，松弛一点的评判范围
        Collider[] hits = Physics.OverlapSphere(dragPoint, toleranceRadius);
        bool isNearFanwei = false;
        foreach (Collider hitCollider in hits)
        {
            // 如果碰到的物体的Transform是fanwei或者属于fanwei的子物体
            if (hitCollider.transform == fanweiObject || hitCollider.transform.IsChildOf(fanweiObject))
            {
                isNearFanwei = true;
                break;
            }
        }

        if (isNearFanwei)
        {
            validDragFrameCount++;
        }
    }

    void EvaluateStrokeComplete()
    {
        if (totalDragFrameCount > 0 && fanweiObject != null)
        {
            float accuracy = (float)validDragFrameCount / totalDragFrameCount;
            int percentage = Mathf.RoundToInt(accuracy * 100);
            
            if (accuracy >= successThreshold) // 达到设定的吻合度算成功
            {
                Debug.Log($"<color=green>评判成功！</color> 你很好地沿着范围画了。吻合度: {percentage}%");
                
                // 成功后的行为：
                // 1. 掩藏 1 这个图片
                if (obj1 != null)
                {
                    obj1.SetActive(false);
                }

                // 2. 掩藏 fanwei 这个物体
                if (fanweiObject != null)
                {
                    fanweiObject.gameObject.SetActive(false);
                }

                // 3. 让掉1从上往下掉
                if (objDrop1 != null)
                {
                    objDrop1.SetActive(true); // 确保掉落物是激活的
                    
                    // 获取或添加刚体以此实现受重力影响掉落
                    Rigidbody rb = objDrop1.GetComponent<Rigidbody>();
                    if (rb == null)
                    {
                        rb = objDrop1.AddComponent<Rigidbody>();
                    }
                    rb.isKinematic = false; // 取消运动学模式
                    rb.useGravity = true;   // 开启重力
                }
            }
            else
            {
                Debug.Log($"<color=orange>评判失败。</color> 偏离了规定的范围！吻合度: {percentage}%");
            }
        }
    }

    void EraseAtUV(Vector2 uv)
    {
        if (editableTexture == null) return;

        // 转换 UV 为贴图上的像素坐标
        int x = (int)(uv.x * texWidth);
        int y = (int)(uv.y * texHeight);
        bool pixelsChanged = false;

        // 圆形擦除算法
        for (int i = -brushSize; i <= brushSize; i++)
        {
            for (int j = -brushSize; j <= brushSize; j++)
            {
                if (i * i + j * j <= brushSize * brushSize)
                {
                    int px = Mathf.Clamp(x + i, 0, texWidth - 1);
                    int py = Mathf.Clamp(y + j, 0, texHeight - 1);

                    // 如果当前像素没被擦除，则变为透明
                    if (editableTexture.GetPixel(px, py).a > 0.01f)
                    {
                        editableTexture.SetPixel(px, py, Color.clear); // 设为完全透明
                        pixelsChanged = true;
                    }
                }
            }
        }

        // 把修改应用到贴图上
        if (pixelsChanged)
        {
            editableTexture.Apply();
        }
    }

    Texture2D CreateReadableCopy(Texture source)
    {
        RenderTexture rt = RenderTexture.GetTemporary(source.width, source.height, 0, RenderTextureFormat.ARGB32);
        Graphics.Blit(source, rt);

        RenderTexture previous = RenderTexture.active;
        RenderTexture.active = rt;

        Texture2D copy = new Texture2D(source.width, source.height, TextureFormat.RGBA32, false);
        copy.ReadPixels(new Rect(0, 0, source.width, source.height), 0, 0);
        copy.Apply();

        RenderTexture.active = previous;
        RenderTexture.ReleaseTemporary(rt);
        return copy;
    }

    void SetupTrailRenderer()
    {
        GameObject trailObj = new GameObject("CutTrail");
        trailObj.transform.SetParent(transform, false);

        trailRenderer = trailObj.AddComponent<LineRenderer>();
        trailRenderer.useWorldSpace = true;
        trailRenderer.alignment = LineAlignment.View;
        trailRenderer.positionCount = 0;
        trailRenderer.startWidth = trailWidth;
        trailRenderer.endWidth = trailWidth;
        trailRenderer.numCornerVertices = 4;
        trailRenderer.numCapVertices = 4;
        trailRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        trailRenderer.receiveShadows = false;

        Shader trailShader = Shader.Find("Universal Render Pipeline/Unlit");
        if (trailShader == null)
        {
            trailShader = Shader.Find("Unlit/Color");
        }

        trailMaterial = new Material(trailShader);
        Color finalColor = trailColor * trailGlowIntensity;
        if (trailMaterial.HasProperty("_BaseColor"))
        {
            trailMaterial.SetColor("_BaseColor", finalColor);
        }
        if (trailMaterial.HasProperty("_Color"))
        {
            trailMaterial.SetColor("_Color", finalColor);
        }
        if (trailMaterial.HasProperty("_Surface"))
        {
            trailMaterial.SetFloat("_Surface", 1f);
        }
        trailRenderer.material = trailMaterial;
    }

    void AddTrailPoint(Vector3 worldPoint)
    {
        if (trailRenderer == null) return;

        if (trailPoints.Count == 0)
        {
            trailPoints.Add(worldPoint);
            trailRenderer.positionCount = 1;
            trailRenderer.SetPosition(0, worldPoint);
            return;
        }

        Vector3 lastPoint = trailPoints[trailPoints.Count - 1];
        if ((worldPoint - lastPoint).sqrMagnitude < trailPointMinDistance * trailPointMinDistance)
        {
            return;
        }

        trailPoints.Add(worldPoint);
        trailRenderer.positionCount = trailPoints.Count;
        trailRenderer.SetPosition(trailPoints.Count - 1, worldPoint);
    }

    void ClearTrail()
    {
        trailPoints.Clear();
        if (trailRenderer != null)
        {
            trailRenderer.positionCount = 0;
        }
    }
}