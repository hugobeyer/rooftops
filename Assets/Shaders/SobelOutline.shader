Shader "Custom/SobelOutline"
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
            #pragma vertex CustomVert
            #pragma fragment CustomFrag
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            
            TEXTURE2D_X(_CameraColorTexture);
            SAMPLER(sampler_CameraColorTexture);
            
            float4 _OutlineColor;
            float _OutlineThickness;
            float _OutlineThreshold;
            float _DepthSensitivity;
            float _ColorSensitivity;
            float _DebugMode;
            
            struct CustomVertexInput
            {
                float4 vertex : POSITION;
                float2 texcoord : TEXCOORD0;
            };
            
            struct CustomVertexOutput
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
                float4 screenPos : TEXCOORD1;
            };
            
            CustomVertexOutput CustomVert(CustomVertexInput input)
            {
                CustomVertexOutput output;
                output.pos = TransformObjectToHClip(input.vertex.xyz);
                output.uv = input.texcoord;
                output.screenPos = ComputeScreenPos(output.pos);
                return output;
            }
            
            float4 CustomFrag(CustomVertexOutput input) : SV_Target
            {
                float2 screenUV = input.screenPos.xy / input.screenPos.w;
                float2 texelSize = _ScreenParams.zw - 1.0;
                float offsetU = _OutlineThickness * texelSize.x;
                float offsetV = _OutlineThickness * texelSize.y;
                
                // Sample depth at center and neighboring pixels
                float depthCenter = LinearEyeDepth(SampleSceneDepth(screenUV), _ZBufferParams);
                float depthLeft = LinearEyeDepth(SampleSceneDepth(screenUV + float2(-offsetU, 0)), _ZBufferParams);
                float depthRight = LinearEyeDepth(SampleSceneDepth(screenUV + float2(offsetU, 0)), _ZBufferParams);
                float depthUp = LinearEyeDepth(SampleSceneDepth(screenUV + float2(0, offsetV)), _ZBufferParams);
                float depthDown = LinearEyeDepth(SampleSceneDepth(screenUV + float2(0, -offsetV)), _ZBufferParams);
                
                // Calculate depth differences
                float depthDiffH = abs(depthLeft - depthRight) * _DepthSensitivity;
                float depthDiffV = abs(depthUp - depthDown) * _DepthSensitivity;
                
                // Sample color at center and neighboring pixels
                float3 colorCenter = SAMPLE_TEXTURE2D_X(_CameraColorTexture, sampler_CameraColorTexture, screenUV).rgb;
                float3 colorLeft = SAMPLE_TEXTURE2D_X(_CameraColorTexture, sampler_CameraColorTexture, screenUV + float2(-offsetU, 0)).rgb;
                float3 colorRight = SAMPLE_TEXTURE2D_X(_CameraColorTexture, sampler_CameraColorTexture, screenUV + float2(offsetU, 0)).rgb;
                float3 colorUp = SAMPLE_TEXTURE2D_X(_CameraColorTexture, sampler_CameraColorTexture, screenUV + float2(0, offsetV)).rgb;
                float3 colorDown = SAMPLE_TEXTURE2D_X(_CameraColorTexture, sampler_CameraColorTexture, screenUV + float2(0, -offsetV)).rgb;
                
                // Calculate color differences
                float colorDiffH = length(colorLeft - colorRight) * _ColorSensitivity;
                float colorDiffV = length(colorUp - colorDown) * _ColorSensitivity;
                
                // Combine depth and color differences
                float edgeH = max(depthDiffH, colorDiffH);
                float edgeV = max(depthDiffV, colorDiffV);
                float edge = max(edgeH, edgeV);
                
                // Apply threshold
                edge = edge > _OutlineThreshold ? 1.0 : 0.0;
                
                // Get the main color from the camera target
                float4 mainColor = SAMPLE_TEXTURE2D_X(_CameraColorTexture, sampler_CameraColorTexture, screenUV);
                
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