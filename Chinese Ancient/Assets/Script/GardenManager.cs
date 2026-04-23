using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
[System.Serializable]
public class GardenData {
    public string name;           // 园林名字
    public Texture2D texture;     // 园林大图
    [TextArea] public string bio; // 园林介绍
    // 内部记录状态：切换回来时保持原样
    [HideInInspector] public float[] savedRotations; // 记录9个碎块各自的Z轴旋转值
    [HideInInspector] public bool isFinished;        // 记录该园林是否已经拼好过
}
public class GardenManager : MonoBehaviour
{
    [Header("--- 数据配置 ---")]
    public List<GardenData> gardens; // 在面板里添加4个园林的信息
    public List<PuzzlePiece> pieces; // 场景里的9个拼图块
    [Header("--- UI 引用 ---")]
    public CanvasGroup infoPanelGroup; // 介绍面板的透明度控制
    public TextMeshProUGUI infoText;   // 介绍文字组件
    public TextMeshProUGUI statusText; // 专门用来显示"蹦出来"的提示文字
    [Header("--- 音效与特效 ---")]
    public AudioSource audioSource;
    public AudioClip successClip;
    public AudioClip allCompleteClip;
    public float typingSpeed = 0.05f;
    private int currentIndex = 0;
    private const int ROWS = 3;
    private const int COLS = 3;
    void Start() {
        // 自动查找或添加 AudioSource，防止漏配
        if (audioSource == null) audioSource = GetComponent<AudioSource>();
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
        if (statusText != null)
        {
            statusText.text = ""; // 初始清空提示
            //防止提示文字被框体截断
            statusText.overflowMode = TextOverflowModes.Overflow;
            statusText.enableWordWrapping = true;
        }
        if (infoText != null)
        {
            //防止Bio介绍文字被截断
            infoText.overflowMode = TextOverflowModes.Overflow;
            infoText.enableWordWrapping = true;
            infoText.maxVisibleCharacters = 99999; // 确保默认全显
        }
        if (gardens != null && gardens.Count > 0)
        {
            //为每个园林准备存储旋转角度的空间，并赋予随机初值
            foreach (var garden in gardens) {
                if (garden.savedRotations == null || garden.savedRotations.Length != pieces.Count) {
                    garden.savedRotations = new float[pieces.Count];
                    for (int i = 0; i < garden.savedRotations.Length; i++) {
                        // 随机初始化角度 (90, 180, 270)，避免直接出现 0 (拼好状态)
                        // 当然如果允许开局即拼好，也可以改为 Random.Range(0, 4) * 90f
                        int rand = Random.Range(1, 4); // 1, 2, 3
                        garden.savedRotations[i] = rand * 90f;
                    }
                }
            }
            // 第一次加载，不执行保存逻辑
            LoadGardenData(0);
        }
        else
        {
            Debug.LogError("请在 Manager 的 Gardens 列表里添加园林素材！");
        }
    }
    // 核心加载逻辑
    private void LoadGardenData(int index) {
        currentIndex = index;
        GardenData currentGarden = gardens[currentIndex];
        //处理 UI 文字面板（如果拼好了直接显示，没拼好则隐藏）
        if (currentGarden.isFinished) {
            infoPanelGroup.alpha = 1f;
            infoPanelGroup.interactable = true;
            infoPanelGroup.blocksRaycasts = true;
            infoText.text = currentGarden.bio;
        } else {
            infoPanelGroup.alpha = 0;
            infoPanelGroup.interactable = false;
            infoPanelGroup.blocksRaycasts = false;
        }
        //切割图片并【还原】该关卡之前保存的角度
        float pieceWidth = 1f / COLS;
        float pieceHeight = 1f / ROWS;
        for (int i = 0; i < pieces.Count; i++) {
            int r = i / COLS;
            int c = i % COLS;
            float x = c * pieceWidth;
            float y = 1f - (r + 1) * pieceHeight;
            if (pieces[i] != null)
            {
                // 刷新贴图
                pieces[i].SetPiece(currentGarden.texture, x, y, pieceWidth, pieceHeight);
                //从数据中读取并恢复角度
                float savedAngle = currentGarden.savedRotations[i];
                pieces[i].transform.localEulerAngles = new Vector3(0, 0, savedAngle);
                // 别忘了告诉 Piece 脚本更新它的 isCorrect 状态
                pieces[i].CheckStatus();
            }
        }
        Debug.Log($"<color=cyan>[管理器]</color>已加载: {currentGarden.name}");
    }
    // 每一块旋转时都会调用这个
    public void CheckWin() {
        bool isAllCorrect = true;
        foreach (var p in pieces) {
            if (!p.isCorrect) {
                isAllCorrect = false;
                break;
            }
        }
        if (isAllCorrect) {
            gardens[currentIndex].isFinished = true; // 永久记录完成状态
            HandleWinLogic();
        }
    }
    void HandleWinLogic() {
        StopAllCoroutines();
        if (infoText != null)
        {
            infoText.text = gardens[currentIndex].bio;
            infoText.maxVisibleCharacters = 99999; 
        }
        StartCoroutine(FadeInInfoPanel());
        bool allFinished = true;
        foreach (var g in gardens) {
            if (!g.isFinished) {
                allFinished = false;
                break;
            }
        }
        string message = "";
        if (allFinished) {
            PlaySound(allCompleteClip);
            message = "恭喜！您已完成所有园林拼图！";
        } else {
            PlaySound(successClip);
            message = $"{gardens[currentIndex].name} 修复完成！";
        }
        if (statusText != null) {
            StartCoroutine(TypewriterStatusText(message));
        }
    }
    void PlaySound(AudioClip clip) {
        if (audioSource != null && clip != null) {
            audioSource.PlayOneShot(clip);
        }
    }
    System.Collections.IEnumerator FadeInInfoPanel() {
        infoPanelGroup.alpha = 0f;
        while (infoPanelGroup.alpha < 1) {
            infoPanelGroup.alpha += Time.deltaTime * 2f;
            yield return null;
        }
        infoPanelGroup.alpha = 1f;
        infoPanelGroup.interactable = true;
        infoPanelGroup.blocksRaycasts = true;
    }
    // AI辅助生成：DeepSeek-R1-0528, 2026-04-23
    //专门给 StatusText 用的打字机逻辑
    System.Collections.IEnumerator TypewriterStatusText(string fullText) {
        statusText.gameObject.SetActive(true);
        //先设置内容并强制刷新排版
        statusText.text = fullText;
        statusText.maxVisibleCharacters = 0;
        statusText.ForceMeshUpdate(true);
        //获取真实排版字符数
        int totalChars = statusText.textInfo.characterCount;
        // 逐字显示
        for (int i = 0; i <= totalChars; i++) {
            statusText.maxVisibleCharacters = i;
            yield return new WaitForSeconds(typingSpeed);
        }
        //确保显示完整
        statusText.maxVisibleCharacters = 99999;
        // AI辅助生成：DeepSeek-R1-0528, 2026-04-23
        foreach (char c in fullText) {
            statusText.text += c;
            yield return new WaitForSeconds(typingSpeed);
        }
        //显示几秒后自动消失
        yield return new WaitForSeconds(3f);
        statusText.text = "";
    }
    // 保存当前碎块的角度到数据列表中
    private void SaveCurrentState() {
        if (gardens == null || gardens.Count == 0) return;
        for (int i = 0; i < pieces.Count; i++) {
            if (pieces[i] != null) {
                // 记录当前碎块的 Z 轴旋转，保存进对应的 GardenData
                gardens[currentIndex].savedRotations[i] = pieces[i].transform.localEulerAngles.z;
            }
        }
    }
    //按钮调用接口
    public void PreviousGarden() {
        SaveCurrentState(); // 第一步：保存当前的进度
        int nextIdx = currentIndex - 1;
        if (nextIdx < 0) nextIdx = gardens.Count - 1;
        LoadGardenData(nextIdx); // 第二步：加载新的图片和旧的角度
    }
    public void NextGarden() {
        SaveCurrentState(); // 第一步：保存当前的进度
        int nextIdx = (currentIndex + 1) % gardens.Count;
        LoadGardenData(nextIdx); // 第二步：加载新的图片和旧的角度
    }
}
