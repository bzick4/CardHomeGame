// Блик по рамке карты: светлая полоса наискосок пробегает раз в _Period секунд (маска — альфа рамки).
// _Rainbow = 1 — радужная голография (оттенок по UV и времени). Полоса чуть зависит от положения вершин
// на холсте, поэтому у наклонённой/сдвинутой карты в руке блик смещается — «играет» при движении.
// UI-шейдер на основе UI/Default: работает с Mask и RectMask2D, без пост-обработки (подходит для WebGL).
Shader "CardHome/UI_Foil"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _Period ("Period (s)", Float) = 4
        _Width ("Band width", Range(0.02, 0.5)) = 0.12
        _Intensity ("Intensity", Range(0, 2)) = 0.6
        _Rainbow ("Rainbow", Range(0, 1)) = 0

        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15
        [Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip ("Use Alpha Clip", Float) = 0
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" "PreviewType"="Plane" "CanUseSpriteAtlas"="True" }
        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }
        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass
        {
            Name "Default"
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0
            #include "UnityCG.cginc"
            #include "UnityUI.cginc"
            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP

            struct appdata_t
            {
                float4 vertex : POSITION;
                float4 color : COLOR;
                float2 texcoord : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                fixed4 color : COLOR;
                float2 texcoord : TEXCOORD0;
                float4 worldPosition : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            fixed4 _Color;
            fixed4 _TextureSampleAdd;
            float4 _ClipRect;
            float _Period, _Width, _Intensity, _Rainbow;

            float3 Hue(float h)
            {
                return saturate(abs(frac(h + float3(0.0, 0.6667, 0.3333)) * 6.0 - 3.0) - 1.0);
            }

            v2f vert(appdata_t v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.worldPosition = v.vertex;
                o.vertex = UnityObjectToClipPos(o.worldPosition);
                o.texcoord = TRANSFORM_TEX(v.texcoord, _MainTex);
                o.color = v.color * _Color;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                half4 color = (tex2D(_MainTex, i.texcoord) + _TextureSampleAdd) * i.color;

                // Полоса идёт по диагонали от −0.6 до 1.6: большую часть периода она за краем — пауза между бликами.
                float d = (i.texcoord.x + i.texcoord.y) * 0.5 + i.worldPosition.x * 0.0004 + i.worldPosition.y * 0.0002;
                float pos = frac(_Time.y / max(0.1, _Period)) * 2.2 - 0.6;
                float band = 1.0 - saturate(abs(d - pos) / _Width);
                band *= band;

                float3 rainbow = Hue(d * 2.0 + _Time.y * 0.25);
                float3 shine = lerp(float3(1.0, 1.0, 1.0), rainbow, _Rainbow);
                // Голография: постоянный радужный отлив по всей рамке.
                color.rgb = lerp(color.rgb, color.rgb * (0.7 + 0.6 * rainbow), _Rainbow * 0.35);
                color.rgb += shine * band * _Intensity;

                #ifdef UNITY_UI_CLIP_RECT
                color.a *= UnityGet2DClipping(i.worldPosition.xy, _ClipRect);
                #endif
                #ifdef UNITY_UI_ALPHACLIP
                clip(color.a - 0.001);
                #endif
                return color;
            }
            ENDCG
        }
    }
}
