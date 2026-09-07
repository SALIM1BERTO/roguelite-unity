Shader "NeonWorlds/EmissiveGeometry"
{
    Properties
    {
        _BaseColor("Body",Color)=(.1,.4,.5,1)
        [HDR] _EmissionColor("Glow",Color)=(.3,1.4,1.6,1)
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" }
        Pass
        {
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            struct Attributes { float4 positionOS:POSITION; float3 normalOS:NORMAL; };
            struct Varyings { float4 positionCS:SV_POSITION; float3 normalWS:TEXCOORD0; };
            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float4 _EmissionColor;
            CBUFFER_END
            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionCS=TransformObjectToHClip(input.positionOS.xyz);
                output.normalWS=TransformObjectToWorldNormal(input.normalOS);
                return output;
            }
            half4 Frag(Varyings input):SV_Target
            {
                float facet=.78+.22*saturate(dot(normalize(input.normalWS),normalize(float3(-.4,.8,-.2))));
                return half4(_BaseColor.rgb*.2+_EmissionColor.rgb*facet,1);
            }
            ENDHLSL
        }
    }
}
