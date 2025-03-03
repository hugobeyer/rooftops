Shader "Custom/SobelOutlineV2"
{
    Properties
    {
        _OutlineColor("Outline Color", Color) = (1, 0, 0, 1)
        _OutlineThickness("Outline Thickness", Range(1, 10)) = 5
        _OutlineThreshold("Outline Threshold", Range(0, 1)) = 0.01
        _DepthSensitivity("Depth Sensitivity", Range(0, 10)) = 10
        _ColorSensitivity("Color Sensitivity", Range(0, 10)) = 10
        [Toggle] _DebugMode("Debug Mode", Float) = 0
    }
    
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline" = "UniversalPipeline" }
        ZTest Always ZWrite Off Cull Off
        
        Pass
        {
            Name "Sobel Outline Pass"
            
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            
            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };
            
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 viewSpaceDir : TEXCOORD1;
            };
            
            TEXTURE2D_X(_CameraColorTexture);
            SAMPLER(sampler_CameraColorTexture);
            
            float4 _OutlineColor;
            float _OutlineThickness;
            float _OutlineThreshold;
            float _DepthSensitivity;
            float _ColorSensitivity;
            float _DebugMode;
            
            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                
                // Calculate view direction for each vertex
                float3 viewSpacePos = mul(UNITY_MATRIX_I_VP, float4(input.positionOS.xy * 2.0 - 1.0, 0.0, 1.0)).xyz;
                output.viewSpaceDir = normalize(viewSpacePos);
                
                return output;
            }
            
            // Helper function to sample depth and convert to linear eye depth
            float SampleDepth(float2 uv)
            {
                float depth = SampleSceneDepth(uv);
                return LinearEyeDepth(depth, _ZBufferParams);
            }
            
            // Sobel operator for edge detection
            float SobelEdgeDetection(float2 uv, float2 texelSize)
            {
                // Sample depths
                float offsetU = _OutlineThickness * texelSize.x;
                float offsetV = _OutlineThickness * texelSize.y;
                
                // Sample depth at 9 points (3x3 grid)
                float topLeft = SampleDepth(uv + float2(-offsetU, offsetV));
                float top = SampleDepth(uv + float2(0, offsetV));
                float topRight = SampleDepth(uv + float2(offsetU, offsetV));
                float left = SampleDepth(uv + float2(-offsetU, 0));
                float center = SampleDepth(uv);
                float right = SampleDepth(uv + float2(offsetU, 0));
                float bottomLeft = SampleDepth(uv + float2(-offsetU, -offsetV));
                float bottom = SampleDepth(uv + float2(0, -offsetV));
                float bottomRight = SampleDepth(uv + float2(offsetU, -offsetV));
                
                // Apply Sobel operator for depth (horizontal and vertical)
                float depthSobelH = -topLeft - 2.0 * left - bottomLeft + topRight + 2.0 * right + bottomRight;
                float depthSobelV = -topLeft - 2.0 * top - topRight + bottomLeft + 2.0 * bottom + bottomRight;
                
                // Calculate depth edge intensity
                float depthEdge = sqrt(depthSobelH * depthSobelH + depthSobelV * depthSobelV) * _DepthSensitivity;
                
                // Sample colors for color-based edge detection
                float3 colorTopLeft = SAMPLE_TEXTURE2D_X(_CameraColorTexture, sampler_CameraColorTexture, uv + float2(-offsetU, offsetV)).rgb;
                float3 colorTop = SAMPLE_TEXTURE2D_X(_CameraColorTexture, sampler_CameraColorTexture, uv + float2(0, offsetV)).rgb;
                float3 colorTopRight = SAMPLE_TEXTURE2D_X(_CameraColorTexture, sampler_CameraColorTexture, uv + float2(offsetU, offsetV)).rgb;
                float3 colorLeft = SAMPLE_TEXTURE2D_X(_CameraColorTexture, sampler_CameraColorTexture, uv + float2(-offsetU, 0)).rgb;
                float3 colorCenter = SAMPLE_TEXTURE2D_X(_CameraColorTexture, sampler_CameraColorTexture, uv).rgb;
                float3 colorRight = SAMPLE_TEXTURE2D_X(_CameraColorTexture, sampler_CameraColorTexture, uv + float2(offsetU, 0)).rgb;
                float3 colorBottomLeft = SAMPLE_TEXTURE2D_X(_CameraColorTexture, sampler_CameraColorTexture, uv + float2(-offsetU, -offsetV)).rgb;
                float3 colorBottom = SAMPLE_TEXTURE2D_X(_CameraColorTexture, sampler_CameraColorTexture, uv + float2(0, -offsetV)).rgb;
                float3 colorBottomRight = SAMPLE_TEXTURE2D_X(_CameraColorTexture, sampler_CameraColorTexture, uv + float2(offsetU, -offsetV)).rgb;
                
                // Apply Sobel operator for color (horizontal and vertical)
                float3 colorSobelH = -colorTopLeft - 2.0 * colorLeft - colorBottomLeft + colorTopRight + 2.0 * colorRight + colorBottomRight;
                float3 colorSobelV = -colorTopLeft - 2.0 * colorTop - colorTopRight + colorBottomLeft + 2.0 * colorBottom + colorBottomRight;
                
                // Calculate color edge intensity
                float colorEdge = length(colorSobelH) + length(colorSobelV);
                colorEdge *= _ColorSensitivity;
                
                // Combine depth and color edges
                float edge = max(depthEdge, colorEdge);
                
                // Apply threshold
                return edge > _OutlineThreshold ? 1.0 : 0.0;
            }
            
            float4 Frag(Varyings input) : SV_Target
            {
                float2 texelSize = _ScreenParams.zw - 1.0;
                float edge = SobelEdgeDetection(input.uv, texelSize);
                
                // Get the main color from the camera target
                float4 mainColor = SAMPLE_TEXTURE2D_X(_CameraColorTexture, sampler_CameraColorTexture, input.uv);
                
                // Debug mode: show edges as white on black
                if (_DebugMode > 0.5)
                {
                    return float4(edge, edge, edge, 1.0);
                }
                // Normal mode: blend outline with main color
                else
                {
                    return lerp(mainColor, _OutlineColor, edge);
                }
            }
            ENDHLSL
        }
    }
} 