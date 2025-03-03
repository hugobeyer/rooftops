Shader "Custom/ShadowOnlyOverlay"
{
    Properties
    {
        _ShadowIntensity ("Shadow Intensity", Range(0.0, 1.0)) = 0.6
        _ShadowColor ("Shadow Color", Color) = (0,0,0,1)
    }
    
    SubShader
    {
        Tags 
        { 
            "RenderType" = "Transparent" 
            "Queue" = "Transparent+100"
            "RenderPipeline" = "UniversalPipeline"
        }
        
        // Shadow caster pass - this allows the object to cast shadows
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }
            
            ZWrite On
            ZTest LEqual
            ColorMask 0
            Cull Back
            
            HLSLPROGRAM
            #pragma vertex ShadowVert
            #pragma fragment ShadowFrag
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            
            // Custom shadow caster implementation
            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
            };
            
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
            };
            
            float3 _LightDirection;
            
            float4 GetShadowPositionHClip(Attributes input)
            {
                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                float3 normalWS = TransformObjectToWorldNormal(input.normalOS);
                
                // Get light direction (URP specific)
                #if UNITY_REVERSED_Z
                    _LightDirection = -_MainLightPosition.xyz;
                #else
                    _LightDirection = _MainLightPosition.xyz;
                #endif
                
                float4 positionCS = TransformWorldToHClip(ApplyShadowBias(positionWS, normalWS, _LightDirection));
                
                #if UNITY_REVERSED_Z
                    positionCS.z = min(positionCS.z, UNITY_NEAR_CLIP_VALUE);
                #else
                    positionCS.z = max(positionCS.z, UNITY_NEAR_CLIP_VALUE);
                #endif
                
                return positionCS;
            }
            
            Varyings ShadowVert(Attributes input)
            {
                Varyings output;
                output.positionCS = GetShadowPositionHClip(input);
                return output;
            }
            
            half4 ShadowFrag() : SV_TARGET
            {
                return 0;
            }
            ENDHLSL
        }
        
        // Main pass - this makes the object invisible (except for its shadow)
        Pass
        {
            Name "InvisiblePass"
            Tags { "LightMode" = "UniversalForward" }
            
            Blend One Zero // Replace blending
            ZWrite Off
            ZTest LEqual
            Cull Back
            ColorMask 0 // Don't write to any color channels
            
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            
            struct Attributes
            {
                float4 positionOS : POSITION;
            };
            
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
            };
            
            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                return output;
            }
            
            float4 frag(Varyings input) : SV_Target
            {
                // Return transparent color (object is invisible)
                return float4(0, 0, 0, 0);
            }
            ENDHLSL
        }
    }
} 