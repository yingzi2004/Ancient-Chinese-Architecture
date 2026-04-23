// AI辅助生成：DeepSeek-R1-0528, 2026-04-23
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

class FruitConfig
{
    public List<string> FruitList;
}

public class GameManager : MonoBehaviour
{
    GameObject _originCard;
    int row = 4;
    int col = 4;
    float _cardH = 2;
    float _spaceX = 0.5f;
    float _spaceY = 0.5f;
    public static GameManager Instance;
    List<Card> _cardList;
    Card _currentTarget;
    List<Card> _compareCardList;
    List<Card> _rotateCardList;

    int step;
    bool _gameover;

    void Start()
    {
        Instance = this;
        _originCard = Resources.Load<GameObject>("Card");
        _cardList = new List<Card>();
        _compareCardList = new List<Card>();
        _rotateCardList = new List<Card>();

        string config = Resources.Load<TextAsset>("config").text;
        var fruits = JsonUtility.FromJson<FruitConfig>(config);
        // AI辅助生成：DeepSeek-R1-0528, 2026-04-23
        List<string> randomList = new List<string>();
        List<string> originList = new List<string>();
        originList.AddRange(fruits.FruitList);
        int count = originList.Count;
        for (int i = 0; i < count; i++)
        {
            int random = Random.Range(0, originList.Count);
            randomList.Add(originList[random]);
            originList.RemoveAt(random);
        }

        originList.AddRange(fruits.FruitList);
        for (int i = 0; i < count; i++)
        {
            int random = Random.Range(0, originList.Count);
            randomList.Add(originList[random]);
            originList.RemoveAt(random);
        }

        Vector3 offset = Vector3.down * (row - 1) / 2 * (_cardH + _spaceY) + Vector3.left * (col - 1) / 2 * (_cardH + _spaceX);
        for (int i = 0; i < row; i++)
        {
            for (int j = 0; j < col; j++)
            {
                GameObject cloneCard = Instantiate(_originCard);
                var card = cloneCard.GetComponent<Card>();
                card.Initial(randomList[i * col + j]);
                _cardList.Add(card);
                cloneCard.transform.position = transform.position + offset + Vector3.up * i * (_cardH + _spaceY) + Vector3.right * j * (_cardH + _spaceX);
                cloneCard.transform.SetParent(transform);

            }
        }

    }

    void Update()
    {
        if (_gameover)
        {
            return;
        }

        MouseDetect();
        MouseInput();
    }

    private void MouseInput()
    {
        if (Input.GetMouseButtonDown(0))
        {
            // AI辅助生成：DeepSeek-R1-0528, 2026-04-23
            if (_currentTarget != null && _rotateCardList.Count < 2 && !_rotateCardList.Contains(_currentTarget))
            {
                step++;
                UIManager.Instance.ShowStep(step);
                _currentTarget.Rotate();

            }
        }
    }

    public void AddCard(Card card)
    {
        _rotateCardList.Add(card);
    }
 // AI辅助生成：DeepSeek-R1-0528, 2026-04-23
    public void CompareCards(Card card)
    {
        _compareCardList.Add(card);
        if (_compareCardList.Count == 2)
        {
            if (_compareCardList[0].FruitName == _compareCardList[1].FruitName)
            {
                _compareCardList[0].SwitchState(StateEnum.Matched);
                _compareCardList[1].SwitchState(StateEnum.Matched);
                Clear();
                if (IsVictory())
                {
                    Victory();
                }
            }
            else
            {
                _compareCardList[0].Rotate(false);
                _compareCardList[1].Rotate(false);
            }
        }

    }

    private void Victory()
    {
        _gameover = true;
        StartCoroutine(LoadPreviousSceneAfterDelay());
    }

    IEnumerator LoadPreviousSceneAfterDelay()
    {
        yield return new WaitForSeconds(2f);

        if (EndSequenceVFX.Instance != null)
        {
            yield return EndSequenceVFX.Instance.PlayRoutine();
        }

        string previousScene = PlayerPrefs.GetString("PreviousScene", "SampleScene");

        Debug.Log($"返回到场景: {previousScene}");

        SceneManager.LoadScene(previousScene);
    }

    public void Clear()
    {
        _compareCardList.Clear();
        _rotateCardList.Clear();
    }

    private void MouseDetect()
    {
        Ray mouseRay = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hitInfo;
        if (Physics.Raycast(mouseRay,out hitInfo))
        {
            Card card = hitInfo.transform.GetComponent<Card>();
            if (card!=null)
            {
                if (_currentTarget!=null&& _currentTarget!=card)
                {
                    _currentTarget.Normal();
                }
                _currentTarget = card;
                _currentTarget.Highlight();
            }

        }
        else if (_currentTarget!=null)
        {
            _currentTarget.Normal();
            _currentTarget = null;
        }
    }

    bool IsVictory()
    {
        bool isVictory = true;
        for (int i = 0; i < _cardList.Count; i++)
        {
            if (_cardList[i].CurrentState == StateEnum.Hidden)
            {
                isVictory = false;
            }
        }
        return isVictory;
    }
}
