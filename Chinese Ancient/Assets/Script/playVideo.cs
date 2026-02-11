//using System.Diagnostics;
using UnityEngine;
using UnityEngine.Video;

[RequireComponent(typeof(VideoPlayer))]
public class WallVideoTrigger : MonoBehaviour
{
    [Tooltip("玩家 Transform，不设置则按 Tag 查找")] public Transform player;
    [Tooltip("玩家 Tag（备用自动查找）")] public string playerTag = "Player";
    [Tooltip("触发播放距离（米）")] public float triggerDistance = 4f;

    private VideoPlayer videoPlayer;
    private ushort audioTrackCount;
    private bool isInsideRange;
    private bool audioTrackCountInitialized = false;

    private void Awake()
    {
        videoPlayer = GetComponent<VideoPlayer>();
        if (videoPlayer != null)
        {
            // 强制禁用自动播放
            videoPlayer.playOnAwake = false;
            videoPlayer.prepareCompleted += OnVideoPrepared;
            
            // 停止任何正在播放的视频
            if (videoPlayer.isPlaying)
            {
                videoPlayer.Stop();
            }
            
            Debug.Log($"<color=cyan>[视频触发器]</color> 已初始化: {gameObject.name}, 距离触发: {triggerDistance}米");
        }
    }

    private void OnVideoPrepared(VideoPlayer source)
    {
        audioTrackCount = videoPlayer.audioTrackCount;
        audioTrackCountInitialized = true;
        MuteAll(true);
    }

    private void Start()
    {
        if (player == null && !string.IsNullOrEmpty(playerTag))
        {
            GameObject found = GameObject.FindGameObjectWithTag(playerTag);
            if (found != null)
            {
                player = found.transform;
            }
        }
    }

    private void Update()
    {
        if (player == null || videoPlayer == null)
            return;

        float distance = Vector3.Distance(transform.position, player.position);
        bool withinRange = distance <= triggerDistance;

        if (withinRange && !isInsideRange)
        {
            isInsideRange = true;
            if (!videoPlayer.isPlaying)
            {
                // 确保视频已准备好再播放
                if (!audioTrackCountInitialized)
                {
                    videoPlayer.Prepare();
                }
                MuteAll(false);
                videoPlayer.Play();
            }
        }
        else if (!withinRange && isInsideRange)
        {
            isInsideRange = false;
            if (videoPlayer.isPlaying)
            {
                videoPlayer.Stop();
            }
            MuteAll(true);
        }
    }

    private void MuteAll(bool mute)
    {
        if (videoPlayer == null || !audioTrackCountInitialized)
            return;

        for (ushort i = 0; i < audioTrackCount; i++)
        {
            videoPlayer.SetDirectAudioMute(i, mute);
        }
    }
}