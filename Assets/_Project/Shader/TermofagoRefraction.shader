// Shader de Refração para o Termófago
// Efeito de transparência com Fresnel (brilho nas bordas) e shimmer
// Compatível com URP (Universal Render Pipeline)
Shader "Custom/TermofagoRefraction"
{
    Properties
    {
        _BaseColor ("Base Color", Color) = (0.3, 0.3, 0.35, 1)
        _BaseMap ("Base Map", 2D) = "white" {}
        _Opacity ("Opacity", Range(0, 1)) = 1.0

        [Header(Refraction Effect)]
        _FresnelPower ("Fresnel Power", Range(0.1, 10)) = 3.0
        _FresnelColor ("Fresnel Color", Color) = (0.5, 0.8, 1.0, 1.0)
        _FresnelIntensity ("Fresnel Intensity", Range(0, 5)) = 1.5

        [Header(Shimmer)]
        _ShimmerSpeed ("Shimmer Speed", Range(0, 20)) = 8.0
        _ShimmerIntensity ("Shimmer Intensity", Range(0, 1)) = 0.15
        _DistortionAmount ("Distortion Amount", Range(0, 0.1)) = 0.02
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "Transparent"
            "Queue" = "Transparent"
        }

        Pass
        {
            Name "TermofagoRefraction"
            Tags { "LightMode" = "UniversalForward" }

            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Back

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 worldNormal : TEXCOORD1;
                float3 worldPos : TEXCOORD2;
                float3 viewDir : TEXCOORD3;
                float fogCoord : TEXCOORD4;
            };

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float4 _BaseMap_ST;
                float _Opacity;
                float _FresnelPower;
                float4 _FresnelColor;
                float _FresnelIntensity;
                float _ShimmerSpeed;
                float _ShimmerIntensity;
                float _DistortionAmount;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output;

                VertexPositionInputs posInputs = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs normInputs = GetVertexNormalInputs(input.normalOS);

                output.positionCS = posInputs.positionCS;
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                output.worldNormal = normInputs.normalWS;
                output.worldPos = posInputs.positionWS;
                output.viewDir = GetWorldSpaceNormalizeViewDir(posInputs.positionWS);
                output.fogCoord = ComputeFogFactor(posInputs.positionCS.z);

                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                half4 baseColor = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv) * _BaseColor;

                float3 normalWS = normalize(input.worldNormal);
                float3 viewDirWS = normalize(input.viewDir);
                float fresnel = pow(1.0 - saturate(dot(normalWS, viewDirWS)), _FresnelPower);
                float3 fresnelColor = _FresnelColor.rgb * fresnel * _FresnelIntensity;

                float shimmer = sin(_Time.y * _ShimmerSpeed + input.worldPos.x * 10.0 + input.worldPos.z * 7.0);
                shimmer = shimmer * 0.5 + 0.5;
                float shimmerOffset = shimmer * _ShimmerIntensity;

                float2 distortedUV = input.uv + normalWS.xz * _DistortionAmount * shimmer;
                half4 distortedColor = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, distortedUV) * _BaseColor;

                half3 finalColor = lerp(baseColor.rgb, distortedColor.rgb, saturate(_DistortionAmount * 10.0));
                finalColor += fresnelColor;
                finalColor += shimmerOffset * _FresnelColor.rgb * 0.2;

                float finalAlpha = _Opacity * (1.0 + shimmerOffset * 0.1);
                finalAlpha = saturate(finalAlpha);

                half4 result = half4(finalColor, finalAlpha);
                result.rgb = MixFog(result.rgb, input.fogCoord);

                return result;
            }
            ENDHLSL
        }
    }

    // Fallback para Built-in RP se URP não estiver disponível
    SubShader
    {
        Tags
        {
            "RenderType" = "Transparent"
            "Queue" = "Transparent"
        }

        Pass
        {
            Name "TermofagoRefractionBuiltIn"
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Back

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 worldNormal : TEXCOORD1;
                float3 worldPos : TEXCOORD2;
                float3 viewDir : TEXCOORD3;
                UNITY_FOG_COORDS(4)
            };

            sampler2D _BaseMap;
            float4 _BaseMap_ST;
            float4 _BaseColor;
            float _Opacity;
            float _FresnelPower;
            float4 _FresnelColor;
            float _FresnelIntensity;
            float _ShimmerSpeed;
            float _ShimmerIntensity;
            float _DistortionAmount;

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _BaseMap);
                o.worldNormal = UnityObjectToWorldNormal(v.normal);
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.viewDir = normalize(WorldSpaceViewDir(v.vertex));
                UNITY_TRANSFER_FOG(o, o.pos);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 baseColor = tex2D(_BaseMap, i.uv) * _BaseColor;

                float3 normalWS = normalize(i.worldNormal);
                float3 viewDirWS = normalize(i.viewDir);
                float fresnel = pow(1.0 - saturate(dot(normalWS, viewDirWS)), _FresnelPower);
                float3 fresnelColor = _FresnelColor.rgb * fresnel * _FresnelIntensity;

                float shimmer = sin(_Time.y * _ShimmerSpeed + i.worldPos.x * 10.0 + i.worldPos.z * 7.0);
                shimmer = shimmer * 0.5 + 0.5;
                float shimmerOffset = shimmer * _ShimmerIntensity;

                float2 distortedUV = i.uv + normalWS.xz * _DistortionAmount * shimmer;
                fixed4 distortedColor = tex2D(_BaseMap, distortedUV) * _BaseColor;

                fixed3 finalColor = lerp(baseColor.rgb, distortedColor.rgb, saturate(_DistortionAmount * 10.0));
                finalColor += fresnelColor;
                finalColor += shimmerOffset * _FresnelColor.rgb * 0.2;

                float finalAlpha = _Opacity * (1.0 + shimmerOffset * 0.1);
                finalAlpha = saturate(finalAlpha);

                fixed4 result = fixed4(finalColor, finalAlpha);
                UNITY_APPLY_FOG(i.fogCoord, result);
                return result;
            }
            ENDCG
        }
    }

    FallBack "Transparent/Diffuse"
}
