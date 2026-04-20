Shader "Hidden/FastGaussianBlur"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
    }
    SubShader
    {
        Cull Off ZWrite Off ZTest Always

        Pass
        {
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4 _Offset; // xy = uv offset per step

            fixed4 frag(v2f_img i) : SV_Target
            {
                float2 uv = i.uv;
                float2 o = _Offset.xy;

                // 1D高斯（separable），权重来自常见5-tap近似
                fixed4 c = tex2D(_MainTex, uv) * 0.227027;
                c += tex2D(_MainTex, uv + o * 1.384615) * 0.316216;
                c += tex2D(_MainTex, uv - o * 1.384615) * 0.316216;
                c += tex2D(_MainTex, uv + o * 3.230769) * 0.070270;
                c += tex2D(_MainTex, uv - o * 3.230769) * 0.070270;
                return c;
            }
            ENDCG
        }
    }
}
