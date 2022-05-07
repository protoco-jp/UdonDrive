Shader "Custom/MultiplySurface"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
        _MainTex ("Albedo (RGB)", 2D) = "white" {}
        _NormalMap ("Normal", 2D) = "bump" {}
        _MulTex ("UV2 Multiply", 2D) = "white" {}
        _MulStr ("UV2 Strength", Range(0,1)) = 1.0
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

        #pragma target 3.0

        sampler2D _MainTex;
        sampler2D _MulTex;
        sampler2D _NormalMap;

        struct Input
        {
            float2 uv_MainTex;
            float2 uv2_MulTex;
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
            fixed4 col = tex2D (_MainTex, IN.uv_MainTex);
            fixed3 nor = UnpackNormal (tex2D (_NormalMap, IN.uv_MainTex));
            fixed4 u2 = tex2D (_MulTex, IN.uv2_MulTex);

            col *= u2  * _MulStr + (1 - _MulStr);

            o.Albedo = col.rgb;
            o.Metallic = _Metallic;
            o.Smoothness = _Glossiness;
            o.Alpha = col.a;
            o.Normal = nor;
            o.Emission = col.rgb * _EmitStr;
        }
        ENDCG
    }
    FallBack "Diffuse"
}
