using UnityEngine;

/// <summary>
/// 京派建筑知识问答默认数据
/// 将此脚本附加到 QuizManager 所在的 GameObject 上，会自动填充题目
/// </summary>
public class JingQuizData : MonoBehaviour
{
    [Header("--- 自动填充数据 ---")]
    [Tooltip("勾选此项会自动填充题目到 QuizManager")]
    public bool autoFillOnStart = true;

    private void Start()
    {
        if (autoFillOnStart)
        {
            QuizManager quizManager = GetComponent<QuizManager>();
            if (quizManager != null)
            {
                quizManager.questions = GetDefaultQuestions();
                Debug.Log("[JingQuizData] 已自动填充京派建筑知识问答题目！");
            }
            else
            {
                Debug.LogWarning("[JingQuizData] 请确保此脚本和 QuizManager 在同一个 GameObject 上！");
            }
        }
    }

    /// <summary>
    /// 获取默认的京派建筑知识问答题目
    /// </summary>
    public static QuestionData[] GetDefaultQuestions()
    {
        return new QuestionData[]
        {
            new QuestionData
            {
                question = "京派建筑的代表作是以下哪个？",
                options = new string[]
                {
                    "苏州园林",
                    "福建土楼",
                    "北京四合院",
                    "徽派建筑"
                },
                correctAnswerIndex = 2,
                explanation = "北京四合院是京派建筑的典型代表，是中国北方传统民居的精华。"
            },
            new QuestionData
            {
                question = "北京四合院的布局特点是？",
                options = new string[]
                {
                    "坐南朝北",
                    "坐北朝南",
                    "坐东朝西",
                    "坐西朝东"
                },
                correctAnswerIndex = 1,
                explanation = "北京四合院采用坐北朝南的布局，有利于采光和保暖。"
            },
            new QuestionData
            {
                question = "四合院的中心建筑称为？",
                options = new string[]
                {
                    "正房",
                    "厢房",
                    "倒座房",
                    "垂花门"
                },
                correctAnswerIndex = 0,
                explanation = "正房是四合院的中心建筑，位于庭院北侧，是家族长辈居住的地方。"
            },
            new QuestionData
            {
                question = "故宫属于哪种建筑风格？",
                options = new string[]
                {
                    "苏派风格",
                    "京派风格",
                    "闽派风格",
                    "徽派风格"
                },
                correctAnswerIndex = 1,
                explanation = "故宫是明清两代的皇家宫殿，是京派建筑的杰出代表。"
            },
            new QuestionData
            {
                question = "京派建筑的屋顶形式多为？",
                options = new string[]
                {
                    "硬山顶",
                    "庑殿顶",
                    "歇山顶",
                    "以上都是"
                },
                correctAnswerIndex = 3,
                explanation = "京派建筑使用多种屋顶形式，包括硬山顶、庑殿顶、歇山顶等。"
            },
            new QuestionData
            {
                question = "四合院中用于分隔内外院的门楼是？",
                options = new string[]
                {
                    "大门",
                    "屏门",
                    "垂花门",
                    "后门"
                },
                correctAnswerIndex = 2,
                explanation = "垂花门是四合院中连接内外院的重要建筑，具有精美的装饰。"
            },
            new QuestionData
            {
                question = "京派建筑的主要特点是？",
                options = new string[]
                {
                    "白墙黑瓦",
                    "红墙黄瓦",
                    "青砖灰瓦",
                    "土楼结构"
                },
                correctAnswerIndex = 2,
                explanation = "京派建筑以青砖灰瓦为特色，色彩沉稳庄重，体现了北方建筑的厚重感。"
            },
            new QuestionData
            {
                question = "北京四合院中，东西两侧的房屋称为？",
                options = new string[]
                {
                    "正房",
                    "厢房",
                    "耳房",
                    "倒座房"
                },
                correctAnswerIndex = 1,
                explanation = "厢房位于庭院东西两侧，是晚辈居住或用作客房的地方。"
            }
        };
    }
}
