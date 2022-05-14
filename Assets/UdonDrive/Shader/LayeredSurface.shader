Shader "Custom/LayeredSurface"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
        [Space(50)]
        _TexR ("Texture R", 2D) = "white" {}
        _NormalR ("Normal R", 2D) = "bump" {}
        _TexG ("Texture G", 2D) = "white" {}
        _NormalG ("Normal G ", 2D) = "bump" {}
        _TexB ("Texture B", 2D) = "white" {}
        _NormalB ("Normal B", 2D) = "bump" {}

        [NoScaleOffset] _MulTex ("UV2 Multiply", 2D) = "white" {}
        _MulStr ("UV2 Strength", Range(0,1)) = 1.0

        [NoScaleOffset] _MaskTex ("UV3 RGB Mask", 2D) = "red" {}

        [NoScaleOffset] _TexA ("UV4 Texture Alpha", 2D) = "black" {}

        _EmitStr ("Emission", Range(0,10)) = 1.0
        _Glossiness ("Smoothness", Range(0,1)) = 0.5
        _Metallic ("Metallic", Range(0,1)) = 0.0
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 200

        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows addshadow

        #pragma target 3.5

        sampler2D _TexR;
        sampler2D _TexG;
        sampler2D _TexB;
        sampler2D _TexA;
        sampler2D _NormalR;
        sampler2D _NormalG;
        sampler2D _NormalB;
        sampler2D _MulTex;
        sampler2D _MaskTex;

        struct Input
        {
            float2 uv_TexR;
            float2 uv_TexG;
            float2 uv_TexB;
            float2 uv_NormalR;
            float2 uv_NormalG;
            float2 uv_NormalB;
            float2 uv2_MulTex;
            float2 uv3_MaskTex;
            float2 uv4_TexA;
        };

        half _Glossiness;
        half _Metallic;
        half _MulStr;
        half _EmitStr;
        fixed4 _Color;

        UNITY_INSTANCING_BUFFER_START(Props)
        UNITY_INSTANCING_BUFFER_END(Props)

        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            fixed4 texR = tex2D (_TexR, IN.uv_TexR);
            fixed4 texG = tex2D (_TexG, IN.uv_TexG);
            fixed4 texB = tex2D (_TexB, IN.uv_TexB);
            fixed4 texA = tex2D (_TexA, IN.uv4_TexA);
            fixed3 normalR = UnpackNormal (tex2D (_NormalR, IN.uv_NormalR));
            fixed3 normalG = UnpackNormal (tex2D (_NormalG, IN.uv_NormalG));
            fixed3 normalB = UnpackNormal (tex2D (_NormalB, IN.uv_NormalB));
            fixed4 u2 = tex2D (_MulTex, IN.uv2_MulTex);
            float3 msk = tex2D (_MaskTex, IN. uv3_MaskTex); // use float for precision

            fixed4 albedo = (texR * msk.r + texG * msk.g + texB * msk.b) / (msk.r + msk.g + msk.b);
            albedo = float4(texA.rgb, 1) * texA.a + albedo * (1 - texA.a);

            float gray = dot(u2.rgb, fixed3(0.299, 0.587, 0.114));
            albedo *= fixed4(gray, gray, gray, 1)  * _MulStr + (1 - _MulStr);
            albedo *= _Color;
            fixed3 normal = (normalR * msk.r + normalG * msk.g + normalB * msk.b) / (msk.r + msk.g + msk.b);

            o.Albedo = albedo.rgb;
            o.Metallic = _Metallic;
            o.Smoothness = _Glossiness;
            o.Alpha = albedo.a;
            o.Normal = normal;
            o.Emission = albedo.rgb * _EmitStr;
        }
        ENDCG
    }
    FallBack "Diffuse"
}
