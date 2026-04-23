// AI辅助生成：DeepSeek-R1-0528, 2026-04-23
using UnityEngine;

public class QianItem : MonoBehaviour
{
    [Header("高亮设置")]
    public Color highlightColor = new Color(0.4f, 0.4f, 0f, 1f); // 微微发光的暗黄色，不会掩盖原本颜色
    private Material[] materials;
    private Color[] originalEmissionColors;
    private bool isHovered = false;

    [Header("抽出动画设置")]
    public float moveSpeed = 5f;          // 飞出速度
    public float distanceToPlayer = 1.5f; // 停留在距离玩家多远的地方 
    
    [Header("解签信息弹窗")]
    public GameObject interpretationPopup; // 用来挂载弹窗的UI对象

    private bool isExtracted = false;     // 是否已经被抽出
    private Vector3 targetPosition;       // 飞向的目标位置
    private Quaternion targetRotation;    // 飞向的目标旋转
    
    private Vector3 startPosition;        // 初始位置
    private Quaternion startRotation;     // 初始旋转

    void Start()
    {
        startPosition = transform.position;
        startRotation = transform.rotation;
        targetPosition = startPosition;
        targetRotation = startRotation;

        // 如果你没有在Inspector里手动拖拽，代码会自动在子物体里寻找Canvas弹窗
        if (interpretationPopup == null)
        {
            Canvas childCanvas = GetComponentInChildren<Canvas>(true);
            if (childCanvas != null)
            {
                interpretationPopup = childCanvas.gameObject;
            }
        }

        // 游戏开始时，确保弹窗一开始是隐藏状态
        if (interpretationPopup != null)
        {
            interpretationPopup.SetActive(false);
        }

        // 获取该签子及其子对象下的所有材质
        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        materials = new Material[renderers.Length];
        originalEmissionColors = new Color[renderers.Length];

        for (int i = 0; i < renderers.Length; i++)
        {
            materials[i] = renderers[i].material;
            // 默认开启发光，避免原来材质没勾选Emission
            materials[i].EnableKeyword("_EMISSION");
            
            if (materials[i].HasProperty("_EmissionColor"))
            {
                originalEmissionColors[i] = materials[i].GetColor("_EmissionColor");
            }
        }
    }

    void Update()
    {
        // AI辅助生成：DeepSeek-R1-0528, 2026-04-23
        // 无论是在飞出还是飞回，平滑移动到目标点
        transform.position = Vector3.Lerp(transform.position, targetPosition, Time.deltaTime * moveSpeed);
        transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, Time.deltaTime * moveSpeed);
    }

    // 当准心指向它时被调用
    public void OnHoverEnter()
    {
        if (isHovered || isExtracted) return; // 已经被抽出的签子不再高亮
        isHovered = true;

        for (int i = 0; i < materials.Length; i++)
        {
            if (materials[i].HasProperty("_EmissionColor"))
            {
                materials[i].SetColor("_EmissionColor", originalEmissionColors[i] + highlightColor);
            }
        }
    }

    // 当准心移开时被调用
    public void OnHoverExit()
    {
        if (!isHovered || isExtracted) return;
        isHovered = false;

        for (int i = 0; i < materials.Length; i++)
        {
            if (materials[i].HasProperty("_EmissionColor"))
            {
                // 恢复纯粹原本的发光状态
                materials[i].SetColor("_EmissionColor", originalEmissionColors[i]);
            }
        }
    }

    public void OnClicked(Transform playerCamera)
    {
        if (isExtracted)
        {
            isExtracted = false;
            targetPosition = startPosition;
            targetRotation = startRotation;

            // 隐藏弹窗
            if (interpretationPopup != null)
            {
                interpretationPopup.SetActive(false);
            }
        }
        else
        {

            OnHoverExit();
            isExtracted = true;

            // 计算目标位置
            targetPosition = playerCamera.position + playerCamera.forward * distanceToPlayer;
            
            // 计算目标旋转
            Vector3 directionToCamera = playerCamera.position - targetPosition;
            if(directionToCamera != Vector3.zero) 
            {
                targetRotation = Quaternion.LookRotation(-directionToCamera);  
            }

            // 显示弹窗
            if (interpretationPopup != null)
            {
                interpretationPopup.SetActive(true);
            }
        }
    }
}
