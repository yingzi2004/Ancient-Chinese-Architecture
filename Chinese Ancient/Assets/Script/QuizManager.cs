using UnityEngine;
using TMPro;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// 问答题目数据类
/// </summary>
[System.Serializable]
public class QuestionData
{
    public string question;           // 题目
    public string[] options;          // 选项（4个选项）
    public int correctAnswerIndex;    // 正确答案索引 (0-3)
    [TextArea(2, 5)]
    public string explanation;        // 答案解析
}

/// <summary>
/// 京派建筑知识问答管理器
/// </summary>
public class QuizManager : MonoBehaviour
{
    [Header("--- 题目数据 ---")]
    public QuestionData[] questions;  // 所有题目

    [Header("--- UI 引用 ---")]
    public GameObject quizPanel;              // 问答面板
    public TextMeshProUGUI questionText;      // 题目文本
    public TextMeshProUGUI scoreText;         // 分数显示
    public TextMeshProUGUI progressText;      // 进度显示（如：1/5）
    public QuizButton[] answerButtons;        // 答案按钮数组

    [Header("--- 反馈 UI ---")]
    public GameObject feedbackPanel;          // 反馈面板
    public TextMeshProUGUI feedbackText;      // 反馈文本（显示是否正确）
    public TextMeshProUGUI explanationText;   // 解析文本
    public TextMeshProUGUI continueText;      // 继续/下一步提示

    [Header("--- 结果 UI ---")]
    public GameObject resultPanel;            // 结果面板
    public TextMeshProUGUI resultTitleText;   // 结果标题
    public TextMeshProUGUI resultScoreText;   // 最终得分
    public TextMeshProUGUI resultMessageText; // 结果评价

    [Header("--- 游戏设置 ---")]
    public float feedbackDuration = 3f;       // 反馈显示时长
    public Color correctColor = Color.green;  // 正确答案颜色
    public Color wrongColor = Color.red;      // 错误答案颜色
    public Color defaultColor = Color.white;  // 默认颜色

    [Header("--- 音效 ---")]
    public AudioSource audioSource;
    public AudioClip correctSound;            // 答对音效
    public AudioClip wrongSound;              // 答错音效
    public AudioClip completeSound;           // 完成音效

    // 游戏状态
    private int currentQuestionIndex = 0;
    private int score = 0;
    private bool isQuizActive = false;
    private bool isAnswering = false;         // 防止重复点击

    private void Start()
    {
        // 初始化 Audio Source
        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();

        // 隐藏所有面板
        HideAllPanels();

        // 验证数据
        ValidateData();
    }

    private void ValidateData()
    {
        if (questions == null || questions.Length == 0)
        {
            Debug.LogWarning("[QuizManager] 题目数据为空，请在 Inspector 中添加题目！");
        }

        for (int i = 0; i < questions.Length; i++)
        {
            if (questions[i].options == null || questions[i].options.Length != 4)
            {
                Debug.LogError($"[QuizManager] 第 {i + 1} 题选项数量不正确，必须有4个选项！");
            }

            if (questions[i].correctAnswerIndex < 0 || questions[i].correctAnswerIndex > 3)
            {
                Debug.LogError($"[QuizManager] 第 {i + 1} 题正确答案索引必须在0-3之间！");
            }
        }
    }

    /// <summary>
    /// 开始问答游戏
    /// </summary>
    public void StartQuiz()
    {
        if (isQuizActive) return;

        // 重置游戏状态
        currentQuestionIndex = 0;
        score = 0;
        isQuizActive = true;
        isAnswering = false;

        // 显示问答面板
        HideAllPanels();
        if (quizPanel != null)
            quizPanel.SetActive(true);

        // 显示第一题
        ShowQuestion();
    }

    /// <summary>
    /// 显示当前题目
    /// </summary>
    private void ShowQuestion()
    {
        if (currentQuestionIndex >= questions.Length)
        {
            ShowResult();
            return;
        }

        QuestionData current = questions[currentQuestionIndex];

        // 设置题目文本
        if (questionText != null)
            questionText.text = $"{currentQuestionIndex + 1}. {current.question}";

        // 设置进度
        if (progressText != null)
            progressText.text = $"{currentQuestionIndex + 1} / {questions.Length}";

        // 设置分数
        if (scoreText != null)
            scoreText.text = $"得分: {score}";

        // 设置选项按钮
        for (int i = 0; i < answerButtons.Length && i < 4; i++)
        {
            if (answerButtons[i] != null)
            {
                answerButtons[i].Initialize(current.options[i], i, this);
                answerButtons[i].SetInteractable(true);
                answerButtons[i].ResetColor();
            }
        }

        isAnswering = false;
    }

    /// <summary>
    /// 玩家选择答案
    /// </summary>
    public void SelectAnswer(int answerIndex)
    {
        if (isAnswering || !isQuizActive) return;

        isAnswering = true;

        QuestionData current = questions[currentQuestionIndex];
        bool isCorrect = (answerIndex == current.correctAnswerIndex);

        // 更新分数
        if (isCorrect)
        {
            score += 10;
            PlaySound(correctSound);
        }
        else
        {
            PlaySound(wrongSound);
        }

        // 显示按钮反馈颜色
        for (int i = 0; i < answerButtons.Length && i < 4; i++)
        {
            answerButtons[i].SetInteractable(false);

            if (i == current.correctAnswerIndex)
            {
                answerButtons[i].SetColor(correctColor);
            }
            else if (i == answerIndex && !isCorrect)
            {
                answerButtons[i].SetColor(wrongColor);
            }
        }

        // 显示反馈
        StartCoroutine(ShowFeedbackCoroutine(isCorrect, current.explanation));
    }

    /// <summary>
    /// 显示反馈协程
    /// </summary>
    private IEnumerator ShowFeedbackCoroutine(bool isCorrect, string explanation)
    {
        yield return new WaitForSeconds(0.5f); // 等待玩家看到按钮颜色变化

        // 显示反馈面板
        if (feedbackPanel != null)
            feedbackPanel.SetActive(true);

        if (feedbackText != null)
        {
            feedbackText.text = isCorrect ? "回答正确！" : "回答错误！";
            feedbackText.color = isCorrect ? correctColor : wrongColor;
        }

        if (explanationText != null)
            explanationText.text = explanation;

        // 等待玩家阅读反馈
        yield return new WaitForSeconds(feedbackDuration);

        // 隐藏反馈，进入下一题
        if (feedbackPanel != null)
            feedbackPanel.SetActive(false);

        currentQuestionIndex++;
        ShowQuestion();
    }

    /// <summary>
    /// 显示最终结果
    /// </summary>
    private void ShowResult()
    {
        isQuizActive = false;

        // 隐藏问答面板
        if (quizPanel != null)
            quizPanel.SetActive(false);

        // 显示结果面板
        if (resultPanel != null)
            resultPanel.SetActive(true);

        int maxScore = questions.Length * 10;
        float percentage = (float)score / maxScore;

        if (resultTitleText != null)
        {
            if (percentage >= 0.8f)
                resultTitleText.text = "太棒了！";
            else if (percentage >= 0.6f)
                resultTitleText.text = "表现不错！";
            else
                resultTitleText.text = "继续加油！";
        }

        if (resultScoreText != null)
            resultScoreText.text = $"最终得分: {score} / {maxScore}";

        if (resultMessageText != null)
        {
            if (percentage >= 0.8f)
                resultMessageText.text = "你对京派建筑非常了解！";
            else if (percentage >= 0.6f)
                resultMessageText.text = "你对京派建筑有一定的了解！";
            else
                resultMessageText.text = "建议多了解京派建筑文化！";
        }

        PlaySound(completeSound);
    }

    /// <summary>
    /// 重新开始问答
    /// </summary>
    public void RestartQuiz()
    {
        StartQuiz();
    }

    /// <summary>
    /// 关闭问答
    /// </summary>
    public void CloseQuiz()
    {
        isQuizActive = false;
        isAnswering = false;
        HideAllPanels();
    }

    private void HideAllPanels()
    {
        if (quizPanel != null)
            quizPanel.SetActive(false);
        if (feedbackPanel != null)
            feedbackPanel.SetActive(false);
        if (resultPanel != null)
            resultPanel.SetActive(false);
    }

    private void PlaySound(AudioClip clip)
    {
        if (audioSource != null && clip != null)
            audioSource.PlayOneShot(clip);
    }

    /// <summary>
    /// 获取当前游戏状态
    /// </summary>
    public bool IsQuizActive()
    {
        return isQuizActive;
    }
}
