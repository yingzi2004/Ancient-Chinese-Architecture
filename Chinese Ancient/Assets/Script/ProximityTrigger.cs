using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Video;

public class ProximityTrigger : MonoBehaviour
{
    [Header("玩家设置")]
 
    public Transform player;
 
    public string playerTag = "Player";

    [Header("触发设置")]
 
    public float triggerDistance = 4f;
    public bool triggerOnce = false;

    [Header("视频封面设置")]
    public Texture coverTexture;
    public VideoPlayer videoPlayer;
    public Renderer targetRenderer;

    [Header("事件回调")]
    public UnityEvent onEnterRange;
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

        // 核心修复：只有当 targetRenderer 存在，并且它身上有 material 时才获取主纹理
        if (targetRenderer != null && targetRenderer.sharedMaterial != null)
        {
            savedCoverTexture = coverTexture != null ? coverTexture : targetRenderer.sharedMaterial.mainTexture;
        }
        else
        {
            savedCoverTexture = coverTexture; // 如果没有渲染器，就直接用面板配的图片
        }

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

                // 自动尝试解锁功能：如果同一物体上挂载了LevelUnlocker，自动调用它，省去每次在面板连线的麻烦！
                LevelUnlocker unlocker = GetComponent<LevelUnlocker>();
                if (unlocker != null)
                {
                    Debug.Log("<color=green>[ProximityTrigger] 玩家进入范围，自动触发解锁代码！</color>");
                    unlocker.UnlockNextLevel();
                }

                hasTriggeredOnce = true;
            }
        }
        else if (!withinRange && isInsideRange)
        {
            isInsideRange = false;
            StopVideoAndShowCover(); // 停止视频并显示封面
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

    private void StopVideoAndShowCover()
    {
        if (videoPlayer != null && isVideoPlaying)
        {
            isVideoPlaying = false;
            videoPlayer.Stop();
            videoPlayer.enabled = false;
        }
        ShowCover();
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