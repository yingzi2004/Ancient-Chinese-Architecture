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

    private void Awake()
    {
        videoPlayer = GetComponent<VideoPlayer>();
        if (videoPlayer != null)
        {
            videoPlayer.playOnAwake = false;
            audioTrackCount = videoPlayer.audioTrackCount;
            MuteAll(true);
            if (videoPlayer.isPlaying)
            {
                videoPlayer.Stop();
            }
        }
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
        if (videoPlayer == null)
            return;

        for (ushort i = 0; i < audioTrackCount; i++)
        {
            videoPlayer.SetDirectAudioMute(i, mute);
        }
    }
}