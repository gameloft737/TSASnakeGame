// WebGL-compatible version of SH_Vefects_Zap_URP
// Simplified for WebGL compatibility (Shader Model 3.0)
Shader "/_Vefects_/SH_Vefects_Zap_URP_WebGL"
{
    Properties
    {
        [HideInInspector] _EmissionColor("Emission Color", Color) = (1,1,1,1)
        [HideInInspector] _AlphaCutoff("Alpha Cutoff ", Range(0, 1)) = 0.5
        [Space(33)][Header(Zap)][Space(13)] _ZapTexture( "Zap Texture", 2D ) = "white" {}
        _EmissiveIntensity( "Emissive Intensity", Float ) = 1
        _ErosionSmoothness( "Erosion Smoothness", Float ) = 0.01
        _TextureMultiply( "Texture Multiply", Float ) = 0
        [Space(33)][Header(LUT)][Space(13)] _LUT( "LUT", 2D ) = "white" {}
        _LUTAmplitude( "LUT Amplitude", Float ) = 1
        _LUTOffset( "LUT Offset", Float ) = 0
        _LUTErosion( "LUT Erosion", Float ) = 0
        _LUTErosionOffset( "LUT Erosion Offset", Float ) = 0
        _LUTErosionSmoothness( "LUT Erosion Smoothness", Float ) = 0.3
        [Space(33)][Header(AR)][Space(13)] _Cull( "Cull", Float ) = 2
        _Src( "Src", Float ) = 5
        _Dst( "Dst", Float ) = 10
        _ZWrite( "ZWrite", Float ) = 0
        _ZTest( "ZTest", Float ) = 2
    }

    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Transparent" "Queue"="Transparent" "UniversalMaterialType"="Unlit" }

        Cull [_Cull]
        AlphaToMask Off

        Pass
        {
            Name "Forward"
            Tags { "LightMode"="UniversalForwardOnly" }

            Blend [_Src] [_Dst], One OneMinusSrcAlpha
            ZWrite Off
            ZTest [_ZTest]
            Offset 0 , 0
            ColorMask RGBA

            HLSLPROGRAM
            // WebGL compatible target
            #pragma target 3.0
            #pragma prefer_hlslcc gles

            #pragma multi_compile_instancing
            #pragma multi_compile_fog

            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float4 texcoord : TEXCOORD0;
                float4 color : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float4 texcoord : TEXCOORD0;
                float4 color : COLOR;
                float fogFactor : TEXCOORD1;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _ZapTexture_ST;
                float _Src;
                float _Dst;
                float _ZWrite;
                float _ZTest;
                float _Cull;
                float _LUTErosionOffset;
                float _LUTErosionSmoothness;
                float _ErosionSmoothness;
                float _LUTErosion;
                float _LUTAmplitude;
                float _LUTOffset;
                float _TextureMultiply;
                float _EmissiveIntensity;
            CBUFFER_END

            TEXTURE2D(_LUT);
            SAMPLER(sampler_LUT);
            TEXTURE2D(_ZapTexture);
            SAMPLER(sampler_ZapTexture);

            Varyings vert(Attributes input)
            {
                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = vertexInput.positionCS;
                output.texcoord = input.texcoord;
                output.color = input.color;
                output.fogFactor = ComputeFogFactor(vertexInput.positionCS.z);

                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                float erosionValue = input.texcoord.z;
                float lutErosionValue = erosionValue + _LUTErosionOffset;
                
                float2 uv_ZapTexture = input.texcoord.xy * _ZapTexture_ST.xy + _ZapTexture_ST.zw;
                float4 zapTex = SAMPLE_TEXTURE2D(_ZapTexture, sampler_ZapTexture, uv_ZapTexture);
                
                // Smoothstep for LUT erosion
                float smoothLUT = smoothstep(lutErosionValue, lutErosionValue + _LUTErosionSmoothness, zapTex.g);
                
                // Smoothstep for main erosion
                float smoothMain = smoothstep(erosionValue, erosionValue + _ErosionSmoothness, zapTex.g);
                float saturatedMain = saturate(smoothMain);
                
                // Lerp between LUT erosion and main erosion
                float lerpedErosion = lerp(smoothLUT, saturatedMain, _LUTErosion);
                
                // Sample LUT
                float2 lutUV = float2((lerpedErosion * _LUTAmplitude) + _LUTOffset, 0.5);
                float3 lutColor = SAMPLE_TEXTURE2D(_LUT, sampler_LUT, lutUV).rgb;
                
                // Apply vertex color
                float3 colorRGB = input.color.rgb;
                float3 baseColor = lutColor * colorRGB;
                
                // Apply texture multiply
                float3 multipliedColor = baseColor * saturatedMain;
                float3 finalColor = lerp(baseColor, multipliedColor, _TextureMultiply);
                
                // Apply emissive intensity
                finalColor *= _EmissiveIntensity;
                
                // Calculate alpha
                float alpha = saturate(saturatedMain * input.color.a);
                
                // Apply fog
                finalColor = MixFog(finalColor, input.fogFactor);

                return half4(finalColor, alpha);
            }
            ENDHLSL
        }

        // Depth Only pass for WebGL
        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode"="DepthOnly" }

            ZWrite On
            ColorMask 0
            AlphaToMask Off

            HLSLPROGRAM
            #pragma target 3.0
            #pragma prefer_hlslcc gles

            #pragma multi_compile_instancing

            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float4 texcoord : TEXCOORD0;
                float4 color : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float4 texcoord : TEXCOORD0;
                float4 color : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _ZapTexture_ST;
                float _Src;
                float _Dst;
                float _ZWrite;
                float _ZTest;
                float _Cull;
                float _LUTErosionOffset;
                float _LUTErosionSmoothness;
                float _ErosionSmoothness;
                float _LUTErosion;
                float _LUTAmplitude;
                float _LUTOffset;
                float _TextureMultiply;
                float _EmissiveIntensity;
            CBUFFER_END

            TEXTURE2D(_ZapTexture);
            SAMPLER(sampler_ZapTexture);

            Varyings vert(Attributes input)
            {
                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.texcoord = input.texcoord;
                output.color = input.color;

                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);

                float2 uv_ZapTexture = input.texcoord.xy * _ZapTexture_ST.xy + _ZapTexture_ST.zw;
                float4 zapTex = SAMPLE_TEXTURE2D(_ZapTexture, sampler_ZapTexture, uv_ZapTexture);
                
                float smoothMain = smoothstep(input.texcoord.z, input.texcoord.z + _ErosionSmoothness, zapTex.g);
                float alpha = saturate(smoothMain * input.color.a);
                
                clip(alpha - 0.001);
                return 0;
            }
            ENDHLSL
        }
    }

    FallBack "Universal Render Pipeline/Particles/Unlit"
}
