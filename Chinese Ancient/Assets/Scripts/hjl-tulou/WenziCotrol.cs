using System.Collections;
using UnityEngine;
using TMPro;

public class WenziCotrol : MonoBehaviour
{
    [Header("玩家与触发")]
    [Tooltip("玩家物体；不填时会自动查找 Tag=Player")]
    public Transform player;

    [Tooltip("触发距离计算锚点；不填则使用当前物体")]
    public Transform triggerAnchor;

    [Tooltip("触发距离（米）")]
    public float triggerDistance = 2f;

    [Tooltip("仅计算水平距离（忽略高度差）")]
    public bool horizontalDistanceOnly = true;

    [Tooltip("按键强制触发（调试）")]
    public bool allowManualTrigger = false;

    [Tooltip("强制触发按键")]
    public KeyCode manualTriggerKey = KeyCode.T;

    [Tooltip("打印调试日志")]
    public bool debugLog = false;

    [Header("文字组件")]
    [Tooltip("目标 TextMeshPro 文本；不填时自动在当前物体查找")]
    public TMP_Text targetText;

    [Header("动画参数")]
    [Tooltip("触发时先把整段文字全部隐藏，再进行逐字出现")]
    public bool hideAllBeforeReveal = true;

    [Tooltip("每个字初始向外偏移距离")]
    public float outwardDistance = 0.35f;

    [Tooltip("每个字出现的间隔")]
    public float interval = 0.08f;

    [Tooltip("单个字移动到最终位置的时间")]
    public float moveDuration = 0.25f;

    [Tooltip("向外方向，默认使用当前物体 forward")]
    public Vector3 customOutwardDirection = Vector3.zero;

    [Tooltip("勾选后，字会停在向外偏移的位置；不勾选则回到墙面原排版位置")]
    public bool keepOutsideAfterReveal = false;

    private bool triggered;
    private bool finished;
    private TMP_MeshInfo[] cachedMeshInfo;
    private float nextDistanceLogTime;
    private bool warnedNoText;
    private bool warnedNoPlayer;

    void Awake()
    {
        if (targetText == null)
        {
            targetText = GetComponent<TMP_Text>();
        }

        if (triggerAnchor == null)
        {
            triggerAnchor = transform;
        }

        if (targetText != null)
        {
            targetText.maxVisibleCharacters = int.MaxValue;
            targetText.ForceMeshUpdate();
            cachedMeshInfo = targetText.textInfo.CopyMeshInfoVertexData();
        }
        else
        {
            Debug.LogWarning("[WenziCotrol] 未找到 TMP_Text，请在 Target Text 手动拖入。", this);
        }
    }

    void Update()
    {
        if (finished || triggered)
        {
            return;
        }

        if (targetText == null)
        {
            if (!warnedNoText)
            {
                warnedNoText = true;
                Debug.LogWarning("[WenziCotrol] targetText 为空，脚本无法运行。", this);
            }
            return;
        }

        if (allowManualTrigger && Input.GetKeyDown(manualTriggerKey))
        {
            Debug.Log($"[WenziCotrol] 手动触发按键 {manualTriggerKey}。", this);
            StartCoroutine(RevealRoutine());
            return;
        }

        if (player == null)
        {
            GameObject foundPlayer = GameObject.FindGameObjectWithTag("Player");
            if (foundPlayer != null)
            {
                player = foundPlayer.transform;
            }
        }

        if (player == null)
        {
            if (!warnedNoPlayer)
            {
                warnedNoPlayer = true;
                Debug.LogWarning("[WenziCotrol] 未找到玩家 Transform。请手动拖入 Player。", this);
            }
            return;
        }

        Vector3 playerPos = player.position;
        Vector3 anchorPos = triggerAnchor != null ? triggerAnchor.position : transform.position;
        if (horizontalDistanceOnly)
        {
            playerPos.y = anchorPos.y;
        }

        float distance = Vector3.Distance(playerPos, anchorPos);
        if (debugLog && Time.time >= nextDistanceLogTime)
        {
            nextDistanceLogTime = Time.time + 1f;
            Debug.Log($"[WenziCotrol] 当前距离={distance:F2}m, 阈值={triggerDistance:F2}m", this);
        }

        if (distance <= triggerDistance)
        {
            if (debugLog)
            {
                Debug.Log($"[WenziCotrol] 触发逐字动画，距离={distance:F2}m, 阈值={triggerDistance:F2}m");
            }
            StartCoroutine(RevealRoutine());
        }
    }

    private IEnumerator RevealRoutine()
    {
        triggered = true;

        targetText.ForceMeshUpdate();
        TMP_TextInfo textInfo = targetText.textInfo;
        int totalCharCount = textInfo.characterCount;

        if (totalCharCount == 0)
        {
            finished = true;
            enabled = false;
            yield break;
        }

        cachedMeshInfo = targetText.textInfo.CopyMeshInfoVertexData();
        if (hideAllBeforeReveal)
        {
            targetText.maxVisibleCharacters = 0;
        }

        Vector3 worldOutward = customOutwardDirection.sqrMagnitude > 0.0001f
            ? customOutwardDirection.normalized
            : transform.forward;
        Vector3 worldOffset = worldOutward * outwardDistance;
        Vector3 localOffset = targetText.transform.InverseTransformVector(worldOffset);

        if (debugLog)
        {
            Debug.Log($"[WenziCotrol] 字符数={totalCharCount}, worldOffset={worldOffset}, localOffset={localOffset}");
        }

        for (int i = 0; i < totalCharCount; i++)
        {
            targetText.maxVisibleCharacters = i + 1;

            TMP_CharacterInfo charInfo = targetText.textInfo.characterInfo[i];
            if (charInfo.isVisible)
            {
                yield return StartCoroutine(AnimateSingleCharacter(i, localOffset)); 
            }

            yield return new WaitForSeconds(interval);
        }

        //确保最终全量可见并保持在墙面排版。
        targetText.maxVisibleCharacters = int.MaxValue;
        targetText.UpdateVertexData(TMP_VertexDataUpdateFlags.Vertices);

        finished = true;
        if (debugLog)
        {
            Debug.Log("[WenziCotrol] 动画完成并锁定，不再重复触发。");
        }
        enabled = false;
    }

    private IEnumerator AnimateSingleCharacter(int charIndex, Vector3 localOffset)
    {
        TMP_TextInfo textInfo = targetText.textInfo;
        TMP_CharacterInfo charInfo = textInfo.characterInfo[charIndex];

        int materialIndex = charInfo.materialReferenceIndex;
        int vertexIndex = charInfo.vertexIndex;

        Vector3[] sourceVertices = cachedMeshInfo[materialIndex].vertices;
        Vector3[] currentVertices = textInfo.meshInfo[materialIndex].vertices;

        float elapsed = 0f;
        while (elapsed < moveDuration)
        {
            elapsed += Time.deltaTime;
            float p = Mathf.Clamp01(elapsed / Mathf.Max(0.0001f, moveDuration));
            float t = keepOutsideAfterReveal ? 1f : (1f - p);

            for (int j = 0; j < 4; j++)
            {
                currentVertices[vertexIndex + j] = sourceVertices[vertexIndex + j] + localOffset * t;
            }

            targetText.UpdateVertexData(TMP_VertexDataUpdateFlags.Vertices);
            yield return null;
        }

        float finalT = keepOutsideAfterReveal ? 1f : 0f;
        for (int j = 0; j < 4; j++)
        {
            currentVertices[vertexIndex + j] = sourceVertices[vertexIndex + j] + localOffset * finalT;
        }

        targetText.UpdateVertexData(TMP_VertexDataUpdateFlags.Vertices);
    }
}
