using UnityEngine;
using UnityEngine.UI;
public class PuzzlePiece : MonoBehaviour, IInteractable
{
    private RawImage _rawImage;
    [HideInInspector] public bool isCorrect = false;
    private float targetAngle = 0f;
    [Header("旋转校正")]
    public float baseOffset = 0f;
    public void Interact()
    {
        OnInteract();
        // 通知管理器检查是否拼好
        GardenManager manager = Object.FindAnyObjectByType<GardenManager>();
        if (manager != null) manager.CheckWin();
    }
    public void SetPiece(Texture2D tex, float x, float y, float w, float h)
    {
        if (_rawImage == null) _rawImage = GetComponent<RawImage>();
        _rawImage.texture = tex;
        _rawImage.uvRect = new Rect(x, y, w, h);
        float[] startAngles = { 90f, 180f, 270f };
        targetAngle = startAngles[Random.Range(0, 3)];
        ApplyRotation();
        isCorrect = false;
    }
    // 准星交互调用的核心方法
    public void OnInteract()
    {
        targetAngle += 90f;
        ApplyRotation();
        // 判断是否归位
        float currentZ = Mathf.Repeat(targetAngle, 360f);
        isCorrect = (Mathf.Abs(currentZ) < 0.1f);
    }
    // 外部调用：强制同步状态
    public void CheckStatus()
    {
        // 反推 targetAngle，防止后续旋转错乱
        targetAngle = transform.localEulerAngles.z - baseOffset;
        float currentZ = Mathf.Repeat(targetAngle, 360f);
        isCorrect = (Mathf.Abs(currentZ) < 0.1f || Mathf.Abs(currentZ - 360f) < 0.1f);
    }
    private void ApplyRotation()
    {
        transform.localEulerAngles = new Vector3(0, 0, targetAngle + baseOffset);
    }
}
