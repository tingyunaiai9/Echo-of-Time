Shader "Custom/SpriteOutline_Soft"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        [MaterialToggle] PixelSnap ("Pixel snap", Float) = 0
        [HideInInspector] _RendererColor ("RendererColor", Color) = (1,1,1,1)
        [HideInInspector] _Flip ("Flip", Vector) = (1,1,1,1)
        [PerRendererData] _AlphaTex ("External Alpha", 2D) = "white" {}
        [PerRendererData] _EnableExternalAlpha ("Enable External Alpha", Float) = 0

        // --- 描边属性 ---
        [MaterialToggle] _OutlineEnabled ("Outline Enabled", Float) = 0
        _OutlineColor ("Outline Color", Color) = (1,1,0,1)
        _OutlineSize ("Outline Size", Range(0, 10)) = 1
        _OutlineSmoothness ("Outline Smoothness", Range(0.1, 5)) = 1 // 控制羽化程度
        _Threshold ("Alpha Threshold", Range(0, 1)) = 0.1 // 控制对杂色的容忍度
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="True"
        }

        Cull Off
        Lighting Off
        ZWrite Off
        // 修改1: 使用标准的透明混合模式，避免边缘杂色
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0
            #pragma multi_compile _ PIXELSNAP_ON
            #pragma multi_compile _ ETC1_EXTERNAL_ALPHA
            #include "UnityCG.cginc"

            struct appdata_t
            {
                float4 vertex   : POSITION;
                float4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex   : SV_POSITION;
                fixed4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            fixed4 _Color;
            fixed4 _OutlineColor;
            float _OutlineSize;
            float _OutlineEnabled;
            float _OutlineSmoothness; // 新增：羽化参数
            float _Threshold;         // 新增：阈值
            sampler2D _MainTex;
            float4 _MainTex_TexelSize;
            fixed4 _RendererColor;

            v2f vert(appdata_t IN)
            {
                v2f OUT;
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);
                OUT.vertex = UnityObjectToClipPos(IN.vertex);
                OUT.texcoord = IN.texcoord;
                OUT.color = IN.color * _Color * _RendererColor;
                #ifdef PIXELSNAP_ON
                OUT.vertex = UnityPixelSnap (OUT.vertex);
                #endif
                return OUT;
            }

            fixed4 frag(v2f IN) : SV_Target
            {
                fixed4 c = tex2D(_MainTex, IN.texcoord) * IN.color;

                // 如果未启用描边，直接返回原色
                if (_OutlineEnabled == 0) return c;

                // 修改2: 采样周围像素的 Alpha 值并累加，而不是做 if 判断
                float2 texel = _MainTex_TexelSize.xy;
                float width = _OutlineSize; 

                // 采样上、下、左、右
                float alphaSum = 0;
                alphaSum += tex2D(_MainTex, IN.texcoord + fixed2(0, width * texel.y)).a;
                alphaSum += tex2D(_MainTex, IN.texcoord - fixed2(0, width * texel.y)).a;
                alphaSum += tex2D(_MainTex, IN.texcoord + fixed2(width * texel.x, 0)).a;
                alphaSum += tex2D(_MainTex, IN.texcoord - fixed2(width * texel.x, 0)).a;
                
                // 也可以额外采样四个对角线方向，让圆角更圆润（可选，会增加性能消耗）
                // alphaSum += tex2D(_MainTex, IN.texcoord + fixed2(width * texel.x, width * texel.y)).a;
                // ... (其他对角)

                // 修改3: 计算描边强度
                // 逻辑：如果当前像素透明(c.a低)，但周围像素不透明(alphaSum高)，则显示描边
                // 使用 smoothstep 来实现柔和羽化
                float outlineAlpha = smoothstep(_Threshold, _Threshold + _OutlineSmoothness, alphaSum);
                
                // 确保描边不会画在原来的物体内部（只在 alpha 低的地方画）
                outlineAlpha = saturate(outlineAlpha - c.a);

                // 最终混合：在原图颜色和描边颜色之间插值
                // 注意：这里不再需要 c.rgb *= c.a，因为我们改了 Blend Mode
                fixed4 finalColor = lerp(c, _OutlineColor, outlineAlpha);
                
                // 修正最终的 Alpha 通道，确保描边部分也是不透明的
                finalColor.a = max(c.a, outlineAlpha * _OutlineColor.a);

                return finalColor;
            }
            ENDCG
        }
    }
}