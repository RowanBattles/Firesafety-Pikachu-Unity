Shader "FireSafety/SmokeParticles"
{
    Properties
    {
        _SmokeField ("Smoke field", 2D) = "black" {}
        _SmokeOrigin ("Origin", Vector) = (0,0,0,0)
        _SmokeExtent ("Extent", Vector) = (20,16,0,0)
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "RenderType"="Transparent" }
        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            TEXTURE2D(_SmokeField);
            SAMPLER(sampler_SmokeField);
            CBUFFER_START(UnityPerMaterial)
                float4 _SmokeOrigin;
                float4 _SmokeExtent;
            CBUFFER_END
            struct Attributes { float4 positionOS : POSITION; float4 color : COLOR; float2 uv : TEXCOORD0; };
            struct Varyings { float4 positionCS : SV_POSITION; float3 positionWS : TEXCOORD1; half4 color : COLOR; float2 uv : TEXCOORD0; };
            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionWS = TransformObjectToWorld(input.positionOS.xyz);
                output.positionCS = TransformWorldToHClip(output.positionWS);
                output.color = input.color; output.uv = input.uv;
                return output;
            }
            half4 Frag(Varyings input) : SV_Target
            {
                float2 fieldUV = (input.positionWS.xz - _SmokeOrigin.xy) / _SmokeExtent.xy;
                half inside = step(0, fieldUV.x) * step(fieldUV.x, 1) * step(0, fieldUV.y) * step(fieldUV.y, 1);
                half density = SAMPLE_TEXTURE2D(_SmokeField, sampler_SmokeField, fieldUV).r * inside;
                float2 disc = input.uv * 2 - 1;
                half cloud = saturate(1 - dot(disc, disc));
                half wisps = 0.85h + 0.15h * sin(input.positionWS.x * 8 + input.positionWS.z * 6 + _Time.y);
                return half4(input.color.rgb, input.color.a * cloud * cloud * density * wisps);
            }
            ENDHLSL
        }
    }
}
