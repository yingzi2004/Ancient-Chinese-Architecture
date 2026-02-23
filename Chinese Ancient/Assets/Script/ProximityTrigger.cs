using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Video;

/// <summary>
/// 近距离触发系统 - 支持视频封面功能
/// </summary>
public class ProximityTrigger : MonoBehaviour
{
    [Header("玩家设置")]
    [Tooltip("玩家 Transform，不设置则按 Tag 查找")] 
    public Transform player;
    [Tooltip("玩家 Tag（备用自动查找）")] 
    public string playerTag = "Player";

    [Header("触发设置")]
    [Tooltip("触发距离（米）")] 
    public float triggerDistance = 4f;
    [Tooltip("是否只触发一次")]
    public bool triggerOnce = false;

    [Header("视频封面设置")]
    [Tooltip("视频封面纹理（不设置则自动使用当前材质的主纹理）")]
    public Texture coverTexture;
    [Tooltip("视频播放器（可选，自动获取）")]
    public VideoPlayer videoPlayer;
    [Tooltip("目标渲染器（可选，自动获取）")]
    public Renderer targetRenderer;

    [Header("事件回调")]
    [Tooltip("进入范围时调用")]
    public UnityEvent onEnterRange;
    [Tooltip("离开范围时调用")]
    public UnityEvent onExitRange;

    private bool isInsideRange = false;
    private bool hasTriggeredOnce = false;
    private bool isVideoPlaying = false;
    private Texture savedCoverTexture;
    private RenderTexture videoRenderTexture;

    private void Awake()
    {
        if (videoPlayer == null) videoPlayer = GetComponent<VideoPlayer>();
        if (targetRenderer == null) targetRenderer = GetComponent<Renderer>();

        // 保存封面纹理
        savedCoverTexture = coverTexture != null ? coverTexture : targetRenderer?.material?.mainTexture;

        // 保存视频 RenderTexture
        if (videoPlayer != null && videoPlayer.targetTexture != null)
            videoRenderTexture = videoPlayer.targetTexture;

        // 初始化视频播放器
        if (videoPlayer != null)
        {
            videoPlayer.playOnAwake = false;
            videoPlayer.isLooping = false;
            videoPlayer.Stop();
            videoPlayer.enabled = false;
            videoPlayer.loopPointReached += OnVideoFinished;
        }

        ShowCover();
    }

    private void Start()
    {
        if (player == null && !string.IsNullOrEmpty(playerTag))
        {
            var found = GameObject.FindGameObjectWithTag(playerTag);
            if (found != null) player = found.transform;
        }
        ShowCover();
    }

    private void OnDestroy()
    {
        if (videoPlayer != null)
            videoPlayer.loopPointReached -= OnVideoFinished;
    }

    private void OnVideoFinished(VideoPlayer vp)
    {
        isVideoPlaying = false;
        videoPlayer.Stop();
        videoPlayer.enabled = false;
        ShowCover();
    }

    private void Update()
    {
        if (player == null) return;

        float distance = Vector3.Distance(transform.position, player.position);
        bool withinRange = distance <= triggerDistance;

        if (withinRange && !isInsideRange)
        {
            isInsideRange = true;
            if (!triggerOnce || !hasTriggeredOnce)
            {
                if (!isVideoPlaying) PlayVideo();
                onEnterRange?.Invoke();
                hasTriggeredOnce = true;
            }
        }
        else if (!withinRange && isInsideRange)
        {
            isInsideRange = false;
            onExitRange?.Invoke();
        }
    }

    private void PlayVideo()
    {
        if (videoPlayer == null) return;
        isVideoPlaying = true;
        videoPlayer.enabled = true;
        if (videoRenderTexture != null && targetRenderer != null)
            targetRenderer.material.mainTexture = videoRenderTexture;
        videoPlayer.Play();
    }

    public void ShowCover()
    {
        if (targetRenderer != null && savedCoverTexture != null)
            targetRenderer.material.mainTexture = savedCoverTexture;
    }

    public void ResetTrigger()
    {
        hasTriggeredOnce = false;
        isInsideRange = false;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, triggerDistance);
    }
}