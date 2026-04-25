using UnityEngine;


[ExecuteInEditMode]
public class SimplePromptTest : MonoBehaviour
{
    [Header("提示设置")]
    [Tooltip("提示文字")]
    public string promptText = "按E拾取";

    [Tooltip("文字偏移位置")]
    public Vector3 offset = Vector3.up * 2f;

    [Tooltip("文字颜色")]
    public Color textColor = Color.yellow;

    [Tooltip("字体大小")]
    public float fontSize = 1f;

    [Tooltip("是否始终显示")]
    public bool alwaysShow = false;

    private TextMesh textMesh;
    private GameObject textObject;
    private JadePendant jadePendant;
    private float checkInterval = 0.1f; 
    private float nextCheckTime;

    void Start()
    {
        jadePendant = GetComponent<JadePendant>();

        CreateTextObject();
    }

    void CreateTextObject()
    {
        if (textObject != null)
        {
            DestroyImmediate(textObject);
        }

        textObject = new GameObject("PromptText");
        textObject.transform.SetParent(transform);
        textObject.transform.localPosition = offset;

        textMesh = textObject.AddComponent<TextMesh>();
        textMesh.text = promptText;
        textMesh.fontSize = 64; 
        textMesh.color = textColor;
        textMesh.alignment = TextAlignment.Center;
        textMesh.anchor = TextAnchor.MiddleCenter;

        textMesh.characterSize = fontSize;

        textObject.AddComponent<FaceCamera>();

        textObject.SetActive(false);
    }

    void Update()
    {

        if (Time.time >= nextCheckTime)
        {
            nextCheckTime = Time.time + checkInterval;
            UpdateVisibility();
        }
    }

    void UpdateVisibility()
    {
        if (textObject == null) return;

        bool shouldShow = false;

        if (alwaysShow)
        {
            shouldShow = true;
        }
        else if (jadePendant != null)
        {

            var field = typeof(JadePendant).GetField("canBePickedUp",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            if (field != null)
            {
                bool canPickup = (bool)field.GetValue(jadePendant);
                shouldShow = canPickup;
            }
        }

        if (textMesh != null)
        {
            textMesh.text = shouldShow ? promptText : "";
        }

        textObject.SetActive(shouldShow && textMesh != null);
    }

    void OnDestroy()
    {
        if (textObject != null)
        {
            DestroyImmediate(textObject);
        }
    }

    void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position + offset, 0.3f);
    }
}

public class FaceCamera : MonoBehaviour
{
    void Update()
    {
        if (Camera.main != null)
        {
            transform.LookAt(Camera.main.transform);
            transform.Rotate(0, 180, 0);
        }
    }
}
