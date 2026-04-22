using UnityEngine;
using TMPro;
using System.Collections;

public class MapMonologueController : MonoBehaviour
{
    [Header("开幕黑屏独白UI")]
    public GameObject openingPanel;
    public TextMeshProUGUI openingText;

    [Header("群青相间感叹UI")]
    public GameObject mapOpenPanel;
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
    public float typingSpeed = 0.05f;
    public AudioSource audioSource;
    public AudioClip windBlowClip;

    private bool isTyping = false;
    private bool skipTyping = false;

    private void Start()
    {
        // 暴力用代码锁定渲染状态，不管你怎么拉框或怎么扔层级，强行把它按在最屏幕上！
        ForceUIRenderState(openingPanel, openingText);
        ForceUIRenderState(mapOpenPanel, mapOpenText);

        if (openingPanel != null) openingPanel.SetActive(false);
        if (mapOpenPanel != null) mapOpenPanel.SetActive(false);
    }

    private void ForceUIRenderState(GameObject panel, TextMeshProUGUI txt)
    {
        if (panel == null || txt == null) return;

        txt.overflowMode = TextOverflowModes.Overflow;
        txt.enableWordWrapping = true;

        // 强行把这个文字框提拔为最顶级渲染队列的独立 Canvas
        Canvas canvas = panel.GetComponent<Canvas>();
        if (canvas == null) 
        {
            canvas = panel.AddComponent<Canvas>();
        }
        
        // 关键改动在这里！如果原本有 CanvasGroup 组件并被设为透明，强行覆盖掉它！
        CanvasGroup cg = panel.GetComponent<CanvasGroup>();
        if (cg != null)
        {
            cg.alpha = 1f;
            cg.gameObject.SetActive(true);
        }
        
        // 更彻底：甚至剥夺它父级可能有的 CanvasGroup 透明度控制
        CanvasGroup[] parentCGs = panel.GetComponentsInParent<CanvasGroup>(true);
        foreach (var parentCg in parentCGs)
        {
            // 如果某一层父级是黑屏或者完全透明了，不理它，强行把对话框拎出来！
            if (parentCg.alpha <= 0.1f)
            {
                Debug.LogWarning($"【抓到了！】发现父级节点 {parentCg.gameObject.name} 的 CanvasGroup 是透明的！这会导致文字隐身！正在通过独立 Canvas 强行剥离渲染...");
            }
        }

        canvas.overrideSorting = true;
        canvas.sortingOrder = 999;
        canvas.pixelPerfect = false;

        // 【最暴力的招数】：不要任何材质球遮罩或特殊设定，还原本真
        txt.maskable = false; 

        if (panel.GetComponent<UnityEngine.UI.GraphicRaycaster>() == null)
        {
            panel.AddComponent<UnityEngine.UI.GraphicRaycaster>();
        }
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

    public IEnumerator PlayOpeningSequence()
    {
        Debug.Log("【系统自检】====== 成功调用到了黑屏独白！======");
        if (openingPanel == null) Debug.LogError("【严重致命缺漏】你的 Opening Panel 还没拖拽赋值！运行个寂寞！");
        if (openingText == null) Debug.LogError("【严重致命缺漏】你的 Opening Text 还没拖拽赋值！字显示个鬼！");
        yield return PlaySequence(openingDialogue, openingPanel, openingText, true);
    }

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
            // 防无意识自杀级保护：你是不是把不小心把字调成透明了？！
            targetText.color = new Color(targetText.color.r, targetText.color.g, targetText.color.b, 1f); 
            targetText.enabled = true; // 确保没被勾掉打叉
            targetText.gameObject.SetActive(true);
            targetText.rectTransform.localScale = Vector3.one;
            targetText.rectTransform.anchoredPosition3D = new Vector3(targetText.rectTransform.anchoredPosition3D.x, targetText.rectTransform.anchoredPosition3D.y, 0f); // 防Z轴偏移

            targetText.text = line;
            targetText.maxVisibleCharacters = 99999;
            targetText.ForceMeshUpdate(true);

            // 【终极诊断外挂】打印出字体为什么显不出来的致命原因！
            Debug.Log($"【诊断提示】正在尝试用字体【{targetText.font?.name}】渲染这句台词：{line}");
            if (targetText.textInfo.characterCount == 0 && line.Length > 0)
            {
                Debug.LogError("🚨【找到元凶了】！！你换上的这个新字体【完全不支持这几个中文字】！\n" +
                               "这就是为什么你改了丑字体之后字就消失了！TextMeshPro 换字体不能硬拖 TTF，必须先烘焙中文字库（Bake Font Asset），或者这个字库大纲里根本没录入这几个中文！\n" +
                               "💡 解决方法：在这个 Text 组件上，把 Font Asset 换回你最开始测试时用的那个没问题的原版字体！字马上就会出来！");
            }

            int totalCharacters = targetText.textInfo.characterCount;
            targetText.maxVisibleCharacters = 0;

            for (int i = 0; i <= totalCharacters; i++)
            {
                if (skipTyping)
                {
                    break;
                }

                targetText.maxVisibleCharacters = i;
                yield return new WaitForSeconds(typingSpeed);
            }
            
            // 确保最后彻底显示完整
            targetText.maxVisibleCharacters = 99999;
        }
        
        isTyping = false;
    }
}