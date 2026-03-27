using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(Collider))]
[RequireComponent(typeof(Renderer))]
public class jianzhi : MonoBehaviour
{
    [Header("原始剪纸贴图（建议手动指定）")]
    public Texture2D sourceTexture;

    [Header("剪刀/画笔的粗细")]
    public int brushSize = 10;

    [Header("拖动轨迹显示")]
    public Color trailColor = new Color(1f, 0.95f, 0.55f, 1f);
    public float trailWidth = 0.03f;
    public float trailGlowIntensity = 2.5f;
    public float trailPointMinDistance = 0.01f;
    public float trailSurfaceOffset = 0.0015f;
    public bool clearTrailOnRelease = false;
    public bool clearTrailOnNewStroke = true;

    private Texture2D editableTexture;
    private Material mat;
    private Collider col;
    private int texWidth;
    private int texHeight;
    private LineRenderer trailRenderer;
    private Material trailMaterial;
    private readonly List<Vector3> trailPoints = new List<Vector3>();

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
        if (Input.GetMouseButtonDown(0) && clearTrailOnNewStroke)
        {
            ClearTrail();
        }

        // 按住鼠标左键时进行剪纸
        if (Input.GetMouseButton(0))
        {
            CutPaper();
        }

        if (Input.GetMouseButtonUp(0) && clearTrailOnRelease)
        {
            ClearTrail();
        }
    }

    void CutPaper()
    {
        if (Camera.main == null) return;

        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        // 进行射线检测
        if (Physics.Raycast(ray, out hit))
        {
            // 如果点中了当前纸张
            if (hit.collider == col)
            {
                EraseAtUV(hit.textureCoord);
                AddTrailPoint(hit.point + hit.normal * trailSurfaceOffset);
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