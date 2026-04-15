using UnityEngine;

public class SyncPortalCamera : MonoBehaviour
{
    [Header("玩家主摄像机")]
    public Transform mainCamera;
    
    [Header("传送门的实体对象 (带MeshRenderer的平面)")]
    public Transform portalPlane;
    
    [Header("用图片做视差偏移 (如果不使用真实RenderTexture)")]
    public bool useStaticImageParallax = true;
    public float parallaxStrength = 0.1f;

    [Header("用真实摄像机 (需要目标场景有Camera和RenderTexture)")]
    public Camera portalRealCamera; 
    public Transform destinationPoint; // 另一个地点的参照物

    private Material portalMat;

    void Start()
    {
        if (mainCamera == null) mainCamera = Camera.main.transform;
        Renderer rend = portalPlane.GetComponent<Renderer>();
        if (rend != null) portalMat = rend.material;
    }

    void Update()
    {
        if (useStaticImageParallax)
        {
            // 方法A：仅仅使用目标地点的图片，移动UV制造视差错觉
            if (portalMat != null)
            {
                // 计算玩家相机相对于传送门的相对位置
                Vector3 relativePos = portalPlane.InverseTransformPoint(mainCamera.position);
                // 偏移UV
                Vector2 uvOffset = new Vector2(relativePos.x, relativePos.y) * parallaxStrength;
                portalMat.SetTextureOffset("_MainTex", uvOffset);
            }
        }
        else if (portalRealCamera != null && destinationPoint != null)
        {
            // 方法B：实时渲染！计算玩家相机与当前传送门的相对位置差
            Vector3 relativePos = portalPlane.InverseTransformPoint(mainCamera.position);
            Vector3 relativeDir = portalPlane.InverseTransformDirection(mainCamera.forward);

            // 将相对位姿应用到目标区域的真实摄像机上
            portalRealCamera.transform.position = destinationPoint.TransformPoint(relativePos);
            portalRealCamera.transform.forward = destinationPoint.TransformDirection(relativeDir);
        }
    }
}