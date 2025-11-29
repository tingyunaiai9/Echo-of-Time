Shader "Custom/SpriteOutline"
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

        // 描边属性
        _OutlineColor ("Outline Color", Color) = (1,1,0,1) // 默认黄色
        _OutlineSize ("Outline Size", Range(0, 10)) = 1
        [MaterialToggle] _OutlineEnabled ("Outline Enabled", Float) = 0
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
        Blend One OneMinusSrcAlpha

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

                // 简单的采样周围像素来检测边缘
                // 注意：这种简单的描边在 Sprite 图集紧凑时可能会采样到邻居 Sprite
                // 但对于独立 Sprite 效果很好
                float2 texel = _MainTex_TexelSize.xy;
                
                fixed4 pixelUp = tex2D(_MainTex, IN.texcoord + fixed2(0, _OutlineSize * texel.y));
                fixed4 pixelDown = tex2D(_MainTex, IN.texcoord - fixed2(0, _OutlineSize * texel.y));
                fixed4 pixelRight = tex2D(_MainTex, IN.texcoord + fixed2(_OutlineSize * texel.x, 0));
                fixed4 pixelLeft = tex2D(_MainTex, IN.texcoord - fixed2(_OutlineSize * texel.x, 0));

                // 如果当前像素是透明的，但周围有不透明像素，说明是边缘
                if (c.a == 0 && (pixelUp.a > 0 || pixelDown.a > 0 || pixelRight.a > 0 || pixelLeft.a > 0))
                {
                    return _OutlineColor;
                }

                c.rgb *= c.a;
                return c;
            }
            ENDCG
        }
    }
}
