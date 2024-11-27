Shader "Fortnite Porting/FP_Material"
{
    Properties
    {
        _Diffuse ("Diffuse", 2D) = "white" {}
        _M ("M", 2D) = "black" {}
        _AO ("Ambient Occlusion", Range(0,1)) = 0.2
        _Cavity ("Cavity", Range(0,1)) = 0.0
        _SpecularMasks ("SpecularMasks", 2D) = "white" {}
        _SwizzleRoughnessToGreen ("Swizzle Roughness to Green", Range(0, 1)) = 0
        _Emission ("Emission", 2D) = "black" {}
        _EmissionColor ("Emission Color", Color) = (1,1,1,1)
        _EmissionStrength ("Emission Strength", Range(0,10)) = 0.0
        _NormalMap ("Normal Map", 2D) = "bump" {}
        _NormalStrength ("Normal Strength", Range(0,2)) = 1.0
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 200

        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows

        sampler2D _Diffuse;
        sampler2D _M;
        sampler2D _SpecularMasks;
        sampler2D _NormalMap;
        sampler2D _Emission;

        struct Input
        {
            float2 uv_Diffuse;
            float2 uv_M;
            float2 uv_SpecularMasks;
            float2 uv_NormalMap;
            float2 uv_Emission;
        };

        
        half _AO;
        half _Cavity;
        half _SwizzleRoughnessToGreen;
        float4 _EmissionColor;
        half _EmissionStrength;
        half _NormalStrength;

        void surf(Input IN, inout SurfaceOutputStandard o)
        {
            // Diffuse Color with M
            // float4 diffuseColor = tex2D(_Diffuse, IN.uv_Diffuse);
            // float ao = tex2D(_M, IN.uv_M).r * _AO;
            // o.Albedo = diffuseColor.rgb - (1 - ao);
            
            // Diffuse Color
            float4 diffuseColor = tex2D(_Diffuse, IN.uv_Diffuse);
            o.Albedo = diffuseColor.rgb;

            // Roughness and Metallic from Green and Blue channels of the texture
            float3 specularMask = tex2D(_SpecularMasks, IN.uv_SpecularMasks).rgb;
            if (_SwizzleRoughnessToGreen > 0)
            {
                o.Smoothness = specularMask.g; // Roughness (inverted for smoothness?)
                o.Metallic = specularMask.b;
            }
            else
            {
                o.Metallic = specularMask.g;
                o.Smoothness = specularMask.b; // Roughness (inverted for smoothness?)
            }
            

            // Normals
            float3 normalMap = tex2D(_NormalMap, IN.uv_NormalMap).rgb * 2.0 - 1.0;
            normalMap.g = normalMap.g * -1;
            o.Normal = normalize(normalMap * _NormalStrength);

            // Emission
            float4 emission = tex2D(_Emission, IN.uv_Emission);
            o.Emission = emission * _EmissionColor.rgb * _EmissionStrength;
        }
        ENDCG
    }
    FallBack "Diffuse"
}
