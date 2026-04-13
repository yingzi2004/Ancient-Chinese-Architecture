Shader "Custom/PortalShader"
{
    Properties
    {
        _MainTex ("Portal View (Texture/RenderTexture)", 2D) = "white" {}
        _NoiseTex ("Edge Noise Mask", 2D) = "white" {}
        _EdgeColor ("Edge Glow Color", Color) = (0.5, 1, 0.8, 1)
        _EdgeWidth ("Edge Width", Range(0, 0.5)) = 0.2
        _Density ("Fog Density", Range(0.1, 5)) = 1.5
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" }
        LOD 100
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v0
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            sampler2D _NoiseTex;
            float4 _NoiseTex_ST;
            float4 _EdgeColor;
            float _EdgeWidth;
            float _Density;

            v0 vert (appdata v)
            {
                v0 o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                // 内部画面的UV
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            fixed4 frag (v0 i) : SV_Target
            {
                // 基础画面颜色
                fixed4 col = tex2D(_MainTex, i.uv);
                
                // 计算到中心的距离 (产生圆/椭圆的遮罩)
                float2 centerUV = i.uv * 2.0 - 1.0;
                float dist = length(centerUV);
                
                // 使用噪声图让边缘变得不规则
                float noise = tex2D(_NoiseTex, i.uv + _Time.y * 0.1).r; // 加上时间让雾气流动
                
                // 计算Alpha渐变
                float alpha = 1.0 - smoothstep(1.0 - _EdgeWidth, 1.0 + _EdgeWidth, dist + noise * 0.3);
                alpha = pow(alpha, _Density);

                // 计算边缘泛光强度
                float glowMask = smoothstep(1.0 - _EdgeWidth - 0.2, 1.0, dist + noise*0.2);
                col.rgb += _EdgeColor.rgb * glowMask * 2.0; // 边缘发光
                col.a = alpha;

                return col;
            }
            ENDCG
        }
    }
}