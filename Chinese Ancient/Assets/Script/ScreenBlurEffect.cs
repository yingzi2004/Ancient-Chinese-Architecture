using UnityEngine;

/// <summary>
/// 简单屏幕模糊（内置渲染管线可用）：挂在相机上即可。
/// 通过修改 blurSize 来控制强度。
/// 
/// 注意：URP/HDRP 下 OnRenderImage 可能不会调用。
/// </summary>
[ExecuteAlways]
[RequireComponent(typeof(Camera))]
public class ScreenBlurEffect : MonoBehaviour
{
    [Tooltip("模糊强度（0=关闭）")]
    [Range(0f, 6f)]
    public float blurSize = 0f;

    [Tooltip("模糊迭代次数（越大越糊也越耗）")]
    [Range(1, 6)]
    public int iterations = 2;

    [Tooltip("降采样（越大越省但更糊/更糙）")]
    [Range(0, 3)]
    public int downsample = 1;

    [Tooltip("可选：指定模糊Shader；为空会自动查找 Hidden/FastGaussianBlur")]
    public Shader blurShader;

    private Material material;

    private void OnEnable()
    {
        EnsureMaterial();
    }

    private void OnDisable()
    {
        Cleanup();
    }

    private void OnDestroy()
    {
        Cleanup();
    }

    private void EnsureMaterial()
    {
        if (material != null) return;

        if (blurShader == null)
        {
            blurShader = Shader.Find("Hidden/FastGaussianBlur");
        }

        if (blurShader == null)
        {
            return;
        }

        material = new Material(blurShader)
        {
            hideFlags = HideFlags.HideAndDontSave
        };
    }

    private void Cleanup()
    {
        if (material != null)
        {
            if (Application.isPlaying)
            {
                Destroy(material);
            }
            else
            {
                DestroyImmediate(material);
            }
            material = null;
        }
    }

    private void OnRenderImage(RenderTexture src, RenderTexture dst)
    {
        if (blurSize <= 0.001f)
        {
            Graphics.Blit(src, dst);
            return;
        }

        EnsureMaterial();
        if (material == null)
        {
            Graphics.Blit(src, dst);
            return;
        }

        int w = src.width >> downsample;
        int h = src.height >> downsample;
        if (w < 2) w = 2;
        if (h < 2) h = 2;

        RenderTexture rt1 = RenderTexture.GetTemporary(w, h, 0, src.format);
        RenderTexture rt2 = RenderTexture.GetTemporary(w, h, 0, src.format);

        Graphics.Blit(src, rt1);

        for (int i = 0; i < iterations; i++)
        {
            float size = blurSize * (1f + i * 0.5f);

            // 水平
            material.SetVector("_Offset", new Vector4(size / w, 0f, 0f, 0f));
            Graphics.Blit(rt1, rt2, material);

            // 垂直
            material.SetVector("_Offset", new Vector4(0f, size / h, 0f, 0f));
            Graphics.Blit(rt2, rt1, material);
        }

        Graphics.Blit(rt1, dst);

        RenderTexture.ReleaseTemporary(rt1);
        RenderTexture.ReleaseTemporary(rt2);
    }
}
