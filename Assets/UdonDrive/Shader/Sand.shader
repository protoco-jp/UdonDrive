Shader "Custom/Sand"
{
    Properties
    {
        [KeywordEnum(NONE, TEX, IGN, WHITE)]_NOISE("Noise Pattern", Int) = 0
        [KeywordEnum(PARAM, MUL)]_ALPHA("Alpha type", Int) = 0
        [Toggle] _Alpha_Only("Use texrure Alpha Only", Float) = 0
        [Toggle] _Alpha_Tex("Use Alpha texrure", Float) = 0
        [Toggle] _Vert_Color("Use vertex color (color over lifetime)", Float) = 0

        _Density("Density", Range(0,1)) = 1
        _OffsetX("Dither Offset X", Range(0,1)) = 0
        _OffsetY("Dither Offset Y", Range(0,1)) = 0
        [NoScaleOffset] _BayerTex ("Dither Texture", 2D) = "white" {}

        _MainTex ("Texture", 2D) = "white" {}
        _AlphaTex ("Alpha Texrure", 2D) = "white" {}
        [HDR] _Color ("Color", Color) = (1, 1, 1, 1)
        _Unlit("Unlit",Range(0, 1)) = 0.5
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="AlphaTest" "IgnoreProjector"="True" }
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
            #pragma shader_feature _ _ALPHA_ONLY_ON
            #pragma shader_feature _ _ALPHA_TEX_ON
            #pragma shader_feature _ _VERT_COLOR_ON

            #include "UnityCG.cginc"
            #include "UnityLightingCommon.cginc"
            #include "Lighting.cginc"
            #include "AutoLight.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                #ifdef _VERT_COLOR_ON
                    float4 color: COLOR;
                #endif
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
                #ifdef _ALPHA_TEX_ON
                    float2 uvAlpha : TEXCOORD1;
                #endif
                float4 scrPos : TEXCOORD2;
                UNITY_FOG_COORDS(3)
                UNITY_VERTEX_OUTPUT_STEREO
                #ifdef _VERT_COLOR_ON
                    float4 color: COLOR;
                #endif
            };

            sampler2D _BayerTex;
            uniform float4 _BayerTex_TexelSize;
            uniform float _Density;
            uniform float _OffsetX;
            uniform float _OffsetY;

            sampler2D _MainTex;
            #ifdef _ALPHA_TEX_ON
                sampler2D _AlphaTex;
                float4 _AlphaTex_ST;
            #endif
            uniform float4 _MainTex_ST;
            uniform float4 _Color;
            uniform float _Unlit;

           
            v2f vert (appdata v)
            {
                v2f o;

                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_OUTPUT(v2f, o);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                #ifdef _ALPHA_TEX_ON
                    o.uvAlpha = TRANSFORM_TEX(v.uv, _AlphaTex);
                #endif
                o.scrPos = ComputeNonStereoScreenPos(o.vertex);

                #ifdef _VERT_COLOR_ON
                    o.color = v.color
                #endif

                UNITY_TRANSFER_FOG(o, o.vertex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                #ifdef _ALPHA_ONLY_ON
                    float4 col = float4(1, 1, 1, tex2D(_MainTex, i.uv).a) * _Color;
                #else
                    float4 col = tex2D(_MainTex, i.uv) * _Color;
                #endif

                #ifdef _VERT_COLOR_ON
                    col *= i.color;
                #endif

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
                    #ifdef _ALPHA_TEX_ON
                        col.a *= tex2D(_AlphaTex, i.uvAlpha).r;
                    #endif
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
