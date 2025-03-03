Shader "Custom/ShadowProjectorURP"
{
    Properties
    {
        _ShadowTex ("Shadow Texture", 2D) = "white" {}
        _ShadowStrength ("Shadow Strength", Range(0,1)) = 1.0
    }
    
    SubShader
    {
        Tags 
        {
            "RenderType" = "Transparent"
            "Queue" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
        }
        
        Pass
        {
            Name "ShadowProjectorPass"
            ZWrite Off
            Blend DstColor Zero
            
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            
            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };
            
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };
            
            TEXTURE2D(_ShadowTex);
            SAMPLER(sampler_ShadowTex);
            
            CBUFFER_START(UnityPerMaterial)
                float _ShadowStrength;
            CBUFFER_END
            
            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                return output;
            }
            
            half4 frag(Varyings input) : SV_Target
            {
                // Sample shadow texture
                half shadowSample = SAMPLE_TEXTURE2D(_ShadowTex, sampler_ShadowTex, input.uv).r;
                
                // Apply shadow strength
                half shadow = lerp(1.0, shadowSample, _ShadowStrength);
                
                // Return shadow value (multiplicative blend)
                return half4(shadow, shadow, shadow, 1.0);
            }
            ENDHLSL
        }
    }
}