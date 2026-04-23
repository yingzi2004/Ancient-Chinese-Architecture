// AI辅助生成：DeepSeek-R1-0528, 2026-04-23 (优化点：协程生命周期管理注释)
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum StateEnum
{
    Hidden,
    Matched,
}

public class Card : MonoBehaviour
{
    bool _rotating;
    public StateEnum CurrentState { get; private set; }
    Material _selfMat;

    public string FruitName { get; private set; }

    public void Initial(string fruitName)
    {
        FruitName = fruitName;
        _selfMat = GetComponent<MeshRenderer>().material;
        Material mat = transform.Find("Quad").GetComponent<MeshRenderer>().material;
        Texture2D texture2D = Resources.Load<Texture2D>("Images/" + fruitName);
        if (texture2D != null)
        {
            mat.mainTexture = texture2D;
        }
        else
        {
            Debug.LogError("Failed to load texture: Images/" + fruitName);
        }
    }

    public void SwitchState(StateEnum targetState)
    {
        CurrentState = targetState;
    }

    public void Rotate(bool toFront = true)
    {
        if (_rotating || CurrentState == StateEnum.Matched)
        {
            return;
        }

        _rotating = true;

        GameManager.Instance.AddCard(this);

        StartCoroutine(RotateCor(toFront));
    }

    IEnumerator RotateCor(bool toFront)
    {
        float workTime = 0;
        GameObject tmp = new GameObject();
        tmp.transform.rotation = transform.rotation;
        tmp.transform.RotateAround(tmp.transform.position, Vector3.up, 180);
        Quaternion originRot = transform.rotation;
        Quaternion desRot = tmp.transform.rotation;
        Destroy(tmp);

        while (true)
        {
            workTime += Time.deltaTime;

            transform.rotation = Quaternion.Lerp(originRot, desRot, workTime);

            if (workTime >= 1)
            {
                break;
            }

            yield return null;
        }

        _rotating = false;

        if (toFront)
        {
            // AI辅助生成：DeepSeek-R1-0528, 2026-04-23
            yield return new WaitForSeconds(1f);
            GameManager.Instance.CompareCards(this);
        }
        else
        {
            yield return new WaitForSeconds(0.5f);
            GameManager.Instance.Clear();
        }
    }

    public void Highlight()
    {
        _selfMat.color = Color.green;
    }

    public void Normal()
    {
        _selfMat.color = Color.white;
    }
}
