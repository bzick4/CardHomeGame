// День → ночь для фона локации. _Night 0..1: цвета уходят в синеву, гаснут насыщенность и яркость,
// края темнеют сильнее. Тёплые светящиеся места (окна, фонари) остаются яркими: маска считается
// по самому пикселю — яркий и «тёплый» (высокие R и G, низкий B). Отдельная картинка-маска не нужна.
Shader "CardHome/UI_NightGrade"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _Night ("Night", Range(0, 1)) = 0
        _NightTint ("Night tint", Color) = (0.55, 0.62, 1, 1)
        _Darken ("Night darkness", Range(0, 1)) = 0.45
        _Vignette ("Night vignette", Range(0, 1)) = 0.45

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
            float _Night, _Darken, _Vignette;
            fixed4 _NightTint;

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
                float3 c = color.rgb;
                float lum = dot(c, float3(0.299, 0.587, 0.114));

                // Тёплый свет: яркий пиксель, где красный и зелёный заметно выше синего.
                float warm = saturate(((c.r + c.g) * 0.5 - c.b) * 2.5) * saturate(lum * 2.2 - 0.55);

                float3 night = lerp(c, lum.xxx, 0.45) * _NightTint.rgb * (1.0 - _Darken);
                float3 lit = c * 1.12; // окна и фонари ночью даже чуть ярче
                float3 graded = lerp(night, lit, warm);

                float2 q = i.texcoord - 0.5;
                float vig = 1.0 - saturate(dot(q, q) * 2.2) * _Vignette;
                graded *= lerp(1.0, vig, warm < 0.5 ? 1.0 : 0.3);

                color.rgb = lerp(c, graded, _Night);

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
