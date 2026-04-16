Shader "Custom/PortalShader"
{
    Properties
    {
        _MainTex ("Portal View (Texture/RenderTexture)", 2D) = "white" {}
        _NoiseTex ("Edge Noise Mask", 2D) = "white" {}
        
        [Space(10)]
        [Header(Background Image Settings)]
        _ImageOpacity ("Image Opacity (背景图主透明度)", Range(0, 1)) = 1.0
        _ImageRadius ("Image Radius (背景显示范围)", Range(0.1, 1.5)) = 0.8
        _ImageFalloff ("Image Edge Falloff (背景边缘渐隐过度)", Range(0.01, 1.0)) = 0.3

        [Space(10)]
        [Header(Edge Glow Settings)]
        [HDR] _EdgeColor ("Edge Glow Color (边缘发光颜色)", Color) = (0.5, 1, 0.8, 1)
        _EdgeWidth ("Edge Width (雾形边缘厚度)", Range(0, 0.5)) = 0.2
        _Density ("Fog Density (雾气的凝实度)", Range(0.1, 5)) = 1.5
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
            
            float _ImageOpacity;
            float _ImageRadius;
            float _ImageFalloff;

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
                
                // 【彻底消灭拼接缝：镜像折返采样 + 双重混合】
                // 1. 首先，对第一层噪点UV应用“镜像折返（Ping-Pong）”运算，强制让非无缝贴图的边缘咬合！
                float2 uv1 = i.uv * _NoiseTex_ST.xy + _NoiseTex_ST.zw + float2(_Time.y * 0.05, _Time.y * 0.04);
                float2 mirrorUV1 = abs(frac(uv1 * 0.5) * 2.0 - 1.0); // 核心：超过边界就倒放，完美无缝
                float noise1 = tex2D(_NoiseTex, mirrorUV1).r; 

                // 2. 采样第二层比例不同、反向流动的噪点，用来打破上面镜像引起的万花筒式对称感
                float2 uv2 = i.uv * (_NoiseTex_ST.xy * 0.6) + float2(-_Time.y * 0.03, _Time.y * 0.02);
                float2 mirrorUV2 = abs(frac(uv2 * 0.5) * 2.0 - 1.0);
                float noise2 = tex2D(_NoiseTex, mirrorUV2).r;

                // 3. 将两层完美无缝的噪点柔和交织到一起，变成极致自然的厚重云雾
                float noise = (noise1 + noise2) * 0.5;

                // === 新增功能：分离背景图的透明度与显示范围 ===
                // 根据给定的半径和渐隐值，计算出背景图在哪逐渐消失
                float imgMask = 1.0 - smoothstep(_ImageRadius - _ImageFalloff, _ImageRadius + _ImageFalloff, dist + noise * 0.1);
                float imgAlpha = _ImageOpacity * imgMask;

                // === 【绝对核心修复：全面往内缩小基准半径！】 ===
                // 先前基准值为 1.0，这正是导致圈圈刚巧顶撞在面片上下左右的元凶！
                // 现在将云雾和发光的范围整个向内部缩回到 0.75，强制它们在触碰边框前自然散尽
                float maxPortalRadius = 0.75;
                float baseAlpha = 1.0 - smoothstep(maxPortalRadius - _EdgeWidth, maxPortalRadius + _EdgeWidth, dist + noise * 0.3);
                baseAlpha = pow(max(0, baseAlpha), _Density);

                // 【双重保险的物理边界压制】：靠近 Plane 边界最后 10% 的区域强行归零
                float squareDist = max(abs(centerUV.x), abs(centerUV.y));
                baseAlpha *= (1.0 - smoothstep(0.85, 0.95, squareDist));

                // 随着缩小的半径重新计算泛光公式
                float glowMask = smoothstep(maxPortalRadius - _EdgeWidth - 0.2, maxPortalRadius, dist + noise * 0.2);
                
                // === 最终混合叠加 ===
                // 颜色组合 = 仅保留范围内的原图色彩 + 额外叠加的边缘泛光
                col.rgb = col.rgb * imgMask + _EdgeColor.rgb * glowMask * 2.0; 
                
                // 透明度组合 = 提取出图像所需要的透明度 与 边缘泛光需要的透明度 进行叠加组合
                // 确保它既能显示中间的半透明图片，又不会让外圈高亮的雾气变透明
                col.a = max(imgAlpha, glowMask) * baseAlpha;

                return col;
            }
            ENDCG
        }
    }
}