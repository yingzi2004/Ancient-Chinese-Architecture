using UnityEngine;

public class QianItem : MonoBehaviour
{
    [Header("高亮设置")]
    public Color highlightColor = Color.yellow; // 发光的黄色
    private Material[] materials;
    private Color[] originalEmissionColors;
    private bool isHovered = false;

    [Header("抽出动画设置")]
    public float moveSpeed = 5f;          // 飞出速度
    public float distanceToPlayer = 0.6f;   // 停留在距离玩家多远的地方
    
    private bool isExtracted = false;     // 是否已经被抽出
    private Vector3 targetPosition;       // 飞向的目标位置
    private Quaternion targetRotation;    // 飞向的目标旋转

    void Start()
    {
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
            else if (materials[i].HasProperty("_BaseColor")) // 兼容无Emission时的基色
            {
                originalEmissionColors[i] = materials[i].GetColor("_BaseColor");
            }
        }
    }

    void Update()
    {
        // 如果被抽出了，就平滑移动到目标点
        if (isExtracted)
        {
            transform.position = Vector3.Lerp(transform.position, targetPosition, Time.deltaTime * moveSpeed);
            transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, Time.deltaTime * moveSpeed);
        }
    }

    // 当准心指向它时被调用
    public void OnHoverEnter()
    {
        if (isHovered || isExtracted) return;
        isHovered = true;

        for (int i = 0; i < materials.Length; i++)
        {
            if (materials[i].HasProperty("_EmissionColor"))
            {
                materials[i].SetColor("_EmissionColor", highlightColor);
            }
            else if (materials[i].HasProperty("_BaseColor"))
            {
                materials[i].SetColor("_BaseColor", highlightColor);
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
                materials[i].SetColor("_EmissionColor", originalEmissionColors[i]);
            }
            else if (materials[i].HasProperty("_BaseColor"))
            {
                materials[i].SetColor("_BaseColor", originalEmissionColors[i]);
            }
        }
    }

    // 当准心点击它时被调用由CrosshairInteract调用
    public void OnClicked(Transform playerCamera)
    {
        if (isExtracted) return; 
        
        isExtracted = true;
        OnHoverExit(); // 取消高亮

        // 计算目标位置（摄像机正前方特定距离）
        targetPosition = playerCamera.position + playerCamera.forward * distanceToPlayer;
        
        // 计算目标旋转（让签子正面朝对摄像机）
        Vector3 directionToCamera = playerCamera.position - targetPosition;
        if(directionToCamera != Vector3.zero) 
        {
            targetRotation = Quaternion.LookRotation(-directionToCamera); 
        }
    }
}
