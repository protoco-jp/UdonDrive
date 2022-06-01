Shader "Custom/SandNokeywordsIGN"
{
    Properties
    {
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

            #include "UnityCG.cginc"
            #include "UnityLightingCommon.cginc"
            #include "Lighting.cginc"
            #include "AutoLight.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                float4 color: COLOR;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
                float4 scrPos : TEXCOORD2;
                UNITY_FOG_COORDS(3)
                UNITY_VERTEX_OUTPUT_STEREO
                float4 color: COLOR;
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

                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_OUTPUT(v2f, o);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.scrPos = ComputeNonStereoScreenPos(o.vertex);

                o.color = v.color

                UNITY_TRANSFER_FOG(o, o.vertex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float4 col = float4(1, 1, 1, tex2D(_MainTex, i.uv).a) * _Color;

                col *= i.color;

                col *= saturate(_LightColor0) * (1 - _Unlit) +  _Unlit;

                float2 screenUV = (i.scrPos.xy / i.scrPos.w) * _ScreenParams.xy;
                float3 magic = float3(0.06711056,0.00583715,52.9829189);
                float threshold = frac(magic.z * frac(dot(screenUV,magic.xy)));

                clip(_Density * col.a - threshold);

                // apply fog
                UNITY_APPLY_FOG(i.fogCoord, col);
                return col;
            }
            ENDCG
        }
    }
}
