using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance;

    Text _step;

    void Start()
    {
        _step = transform.Find("Text").GetComponent<Text>();
        Instance = this;
    }

    public void ShowStep(int step)
    {
        _step.text = "STEP:" + step;
    }

    void Update()
    {

    }
}
