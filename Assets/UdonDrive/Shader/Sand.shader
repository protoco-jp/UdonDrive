﻿Shader "Custom/Sand"
{
    Properties
    {
        [KeywordEnum(NONE, TEX, IGN, WHITE)]_NOISE("Noise Pattern", Int) = 0
        [KeywordEnum(PARAM, MUL, MASK)]_ALPHA("Alpha type", Int) = 0
        _Density("Density", Range(0,1)) = 1
        _OffsetX("Dither Offset X", Range(0,1)) = 0
        _OffsetY("Dither Offset Y", Range(0,1)) = 0
        [NoScaleOffset] _BayerTex ("Dither Texture", 2D) = "white" {}

        _MainTex ("Texture", 2D) = "white" {}
        [HDR] _Color ("Color", Color) = (1, 1, 1, 1)
        _Unlit("Unlit",Range(0, 1)) = 0.5
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="AlphaTest" }
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            // make fog work
            #pragma multi_compile_fog

            #pragma shader_feature _NOISE_NONE _NOISE_TEX _NOISE_WHITE _NOISE_IGN
            #pragma shader_feature _ALPHA_MUL _ALPHA_MASK

            #include "UnityCG.cginc"
            #include "UnityLightingCommon.cginc"
            #include "Lighting.cginc" 
            #include "AutoLight.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
                float4 scrPos : TEXCOORD1;
                UNITY_FOG_COORDS(2)
                UNITY_VERTEX_OUTPUT_STEREO
            };

            sampler2D _BayerTex;
            uniform float4 _BayerTex_TexelSize;
            uniform float _Density;
            uniform float _OffsetX;
            uniform float _OffsetY;

            sampler2D _MainTex;
            uniform float4 _MainTex_ST;
            uniform float4 _Color;
            uniform float _Unlit;

           
            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.scrPos = ComputeNonStereoScreenPos(o.vertex);
                UNITY_TRANSFER_FOG(o, o.vertex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float4 col = tex2D(_MainTex, i.uv) * _Color;
                col *= saturate(_LightColor0) * (1 - _Unlit) +  _Unlit;

                #ifdef _NOISE_TEX //texture based
                    float2 screenUV = (i.scrPos.xy / i.scrPos.w) * ((_ScreenParams.xy) / _BayerTex_TexelSize.zw) + float2(_OffsetX, _OffsetY);
                    float threshold = clamp(tex2D( _BayerTex, screenUV ).r, 0.001, 0.999);
                #elif _NOISE_WHITE //white noise
                    float2 screenUV = (i.scrPos.xy / i.scrPos.w) * _ScreenParams.xy;
                    float threshold = frac(sin(dot(screenUV, fixed2(12.9898,78.233))) * 43758.5453);
                #elif _NOISE_IGN //Interleaved Gradient Noise (CoD Dither)
                    float2 screenUV = (i.scrPos.xy / i.scrPos.w) * _ScreenParams.xy;
                    float3 magic = float3(0.06711056,0.00583715,52.9829189);
                    float threshold = frac(magic.z * frac(dot(screenUV,magic.xy)));
                #endif
                #ifndef _NOISE_NONE
                    #ifdef _ALPHA_MUL
                        clip(_Density * col.a - threshold);
                    #elif _ALPHA_PARAM
                        clip(_Density - threshold);
                    #endif
                #endif

                // apply fog
                UNITY_APPLY_FOG(i.fogCoord, col);
                return col;
            }
            ENDCG
        }
    }
}
