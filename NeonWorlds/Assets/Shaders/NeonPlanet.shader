Shader "NeonWorlds/OrbitalSurface"
{
    Properties
    {
        _BaseColor("Surface", Color) = (0.008,0.016,0.032,1)
        [HDR] _Accent("Orbital tint", Color) = (0.08,0.32,0.4,1)
        _GridDensity("Grid density", Float) = 28
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
            struct Varyings { float4 positionCS:SV_POSITION; float3 positionWS:TEXCOORD0; float3 normalWS:TEXCOORD1; float3 objectNormal:TEXCOORD2; };
            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float4 _Accent;
                float _GridDensity;
            CBUFFER_END
            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionCS=TransformObjectToHClip(input.positionOS.xyz);
                output.positionWS=TransformObjectToWorld(input.positionOS.xyz);
                output.normalWS=TransformObjectToWorldNormal(input.normalOS);
                output.objectNormal=normalize(input.positionOS.xyz);
                return output;
            }
            half4 Frag(Varyings input):SV_Target
            {
                float3 n=normalize(input.objectNormal);
                float2 uv=float2(atan2(n.z,n.x)/6.2831853+.5,acos(clamp(n.y,-1,1))/3.1415926);
                float2 cell=uv*float2(_GridDensity,_GridDensity*.5);
                float2 width=max(fwidth(cell),float2(.001,.001));
                float2 edge=abs(frac(cell-.5)-.5)/width;
                float grid=1-saturate(min(edge.x,edge.y));
                // Fade meridians at the poles, where the lines converge.
                grid*=smoothstep(.0,.13,1-abs(n.y));
                float3 normal=normalize(input.normalWS);
                float3 view=GetWorldSpaceNormalizeViewDir(input.positionWS);
                float rim=pow(1-saturate(dot(normal,view)),3.5);
                float shade=.55+.45*saturate(dot(normal,normalize(float3(-.3,.8,-.4))));
                float3 color=_BaseColor.rgb*shade+_Accent.rgb*(grid*.2+rim*.55);
                return half4(color,1);
            }
            ENDHLSL
        }
    }
}
