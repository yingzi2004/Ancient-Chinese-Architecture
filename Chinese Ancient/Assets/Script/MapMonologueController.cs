using UnityEngine;
using TMPro;
using System.Collections;

/// <summary>
/// 纯粹的玩家第一人称视角心路独白控制器
/// 将长篇文本独立管理，支持玩家自由点击跳过/继续，完美兼容卷轴等动画节奏的衔接
/// </summary>
public class MapMonologueController : MonoBehaviour
{
    [Header("开幕黑屏独白UI")]
    [Tooltip("黑屏时的独白底板（对话框）")]
    public GameObject openingPanel;
    [Tooltip("黑屏时的显示文本 TMP 当前组件")]
    public TextMeshProUGUI openingText;

    [Header("群青相间感叹UI")]
    [Tooltip("卷轴展开后的感叹底板（对话框），由于位置和背景不同故拆分")]
    public GameObject mapOpenPanel;
    [Tooltip("卷轴展开后的显示文本 TMP 组件")]
    public TextMeshProUGUI mapOpenText;

    [Header("开幕黑屏独白（玩家疑惑）")]
    [TextArea(2, 5)]
    public string[] openingDialogue = new string[]
    {
        "咦…… 这是哪里？头有点晕……",
        "我记得我是跟着古建筑考察队，去寻访苏州园林、北京天坛这些经典建筑的，怎么突然到了这个地方？",
        "一阵风吹过，一张泛黄的古地图飘到你面前……"
    };

    [Header("卷轴展开后独白（玩家感叹）")]
    [TextArea(2, 5)]
    public string[] mapOpenDialogue = new string[]
    {
        "哇塞！这原来是汇集了各地经典的古建筑地图啊！也太酷了吧～",
        "苏州园林、北京天坛、晋商大院、福建土楼都在上面，每一个都是我超想打卡的地方！",
        "地图上的每个标记都闪着微光，仿佛在邀请你走进这些千年建筑的故事里～",
        "好期待呀！真想马上走进这些古建筑，看看它们藏着的小细节～"
    };

    [Header("打字机及效果设置")]
    [Tooltip("每个字打字的时间间隔")]
    public float typingSpeed = 0.05f;
    public AudioSource audioSource;
    public AudioClip windBlowClip;

    private bool isTyping = false;
    private bool skipTyping = false;

    private void Start()
    {
        if (openingPanel != null) openingPanel.SetActive(false);
        if (mapOpenPanel != null) mapOpenPanel.SetActive(false);
    }

    private void Update()
    {
        // 如果正在打字过程中，按下左键或空格即可瞬间显示完整句子
        if (isTyping && IsAdvanceInputDown())
        {
            skipTyping = true;
        }
    }

    private bool IsAdvanceInputDown()
    {
        return Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return);
    }

    /// <summary>
    /// 供外部 ScrollMapController 等主脑调用：播放开场黑屏引导
    /// </summary>
    public IEnumerator PlayOpeningSequence()
    {
        yield return PlaySequence(openingDialogue, openingPanel, openingText, true);
    }

    /// <summary>
    /// 供外部调用：播放卷轴完全拉开后的感叹词
    /// </summary>
    public IEnumerator PlayMapOpenSequence()
    {
        yield return PlaySequence(mapOpenDialogue, mapOpenPanel, mapOpenText, false);
    }

    private IEnumerator PlaySequence(string[] lines, GameObject targetPanel, TextMeshProUGUI targetText, bool playWindOnLast)
    {
        if (lines == null || lines.Length == 0) yield break;

        if (targetPanel != null) targetPanel.SetActive(true);

        for (int i = 0; i < lines.Length; i++)
        {
            // 如果需求中带了风声要求，在播放最后一句时播风声
            if (playWindOnLast && i == lines.Length - 1 && audioSource != null && windBlowClip != null)
            {
                audioSource.PlayOneShot(windBlowClip);
            }

            yield return StartCoroutine(TypeLine(lines[i], targetText));

            // 打字结束后，等待玩家主动点击确认，再进入下一句！这保证了他们的自由阅读节奏。
            yield return null; // 缓冲1帧，防止最后一下的跳过点击瞬间触发进入下一句
            while (!IsAdvanceInputDown())
            {
                yield return null;
            }
        }

        if (targetPanel != null) targetPanel.SetActive(false);
    }

    private IEnumerator TypeLine(string line, TextMeshProUGUI targetText)
    {
        isTyping = true;
        skipTyping = false;

        if (targetText != null)
        {
            targetText.text = line;
            targetText.maxVisibleCharacters = 0;
            targetText.ForceMeshUpdate();

            for (int i = 0; i <= line.Length; i++)
            {
                if (skipTyping)
                {
                    targetText.maxVisibleCharacters = line.Length;
                    break;
                }

                targetText.maxVisibleCharacters = i;
                yield return new WaitForSeconds(typingSpeed);
            }
        }
        
        isTyping = false;
    }
}
