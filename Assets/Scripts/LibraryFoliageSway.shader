Shader "Erudition/UI/Library Foliage Sway"
{
    Properties
    {
        [PerRendererData] _MainTex ("Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _SwayAmount ("Sway Amount", Range(0, 0.015)) = 0.0065
        _SwaySpeed ("Sway Speed", Range(0.05, 2)) = 0.55
        _GreenThreshold ("Green Threshold", Range(-0.1, 0.2)) = 0.018
        _GreenStrength ("Green Mask Strength", Range(1, 20)) = 11
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
        Tags
        {
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="True"
        }

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
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                fixed4 color : COLOR;
                float2 texcoord : TEXCOORD0;
                float4 worldPosition : TEXCOORD1;
            };

            sampler2D _MainTex;
            fixed4 _Color;
            fixed4 _TextureSampleAdd;
            float4 _ClipRect;
            float _SwayAmount;
            float _SwaySpeed;
            float _GreenThreshold;
            float _GreenStrength;

            v2f vert(appdata_t v)
            {
                v2f o;
                o.worldPosition = v.vertex;
                o.vertex = UnityObjectToClipPos(o.worldPosition);
                o.texcoord = v.texcoord;
                o.color = v.color * _Color;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float2 uv = i.texcoord;
                fixed4 original = tex2D(_MainTex, uv);

                float greenLead = original.g - max(original.r, original.b);
                float foliage = saturate((greenLead - _GreenThreshold) * _GreenStrength);

                float sideDistance = abs(uv.x - 0.5) * 2.0;
                float edgeMask = smoothstep(0.42, 0.82, sideDistance);
                float upperMask = smoothstep(0.24, 0.52, uv.y);
                float mask = foliage * edgeMask * upperMask;

                float side = uv.x < 0.5 ? 1.0 : -1.0;
                float phase = _Time.y * _SwaySpeed + uv.y * 6.1 + side * 0.85;
                float2 sway = float2(
                    sin(phase) * _SwayAmount * side,
                    cos(phase * 0.73) * _SwayAmount * 0.22
                );

                fixed4 moved = tex2D(_MainTex, uv + sway * mask);
                fixed4 color = lerp(original, moved, mask);
                color = (color + _TextureSampleAdd) * i.color;

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