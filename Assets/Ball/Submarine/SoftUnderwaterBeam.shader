Shader "Custom/SoftUnderwaterBeam"
{
    Properties
    {
        [HDR] _Color("Light Color", Color) = (1, 0.7, 0.3, 1)
        _Brightness("Brightness (ความสว่างรวม)", Range(0.1, 5.0)) = 1.5
        _DistanceFalloff("Distance Falloff (อัตราการจางตามระยะทาง)", Range(0.5, 4.0)) = 1.8
        _RadialSoftness("Radial Softness (ความฟุ้งรอบขอบลำแสง)", Range(0.5, 5.0)) = 2.0
        _SoftFactor("Depth Soft Fade (Fade เมื่อชนหิน/พื้น)", Range(0.1, 3.0)) = 1.5
    }

    SubShader
    {
        Tags 
        { 
            "RenderType" = "Transparent" 
            "Queue" = "Transparent" 
            "RenderPipeline" = "UniversalPipeline" 
            "IgnoreProjector" = "True"
        }

        Blend One One          // Additive Blending (เรืองแสงกลืนกับน้ำ)
        ZWrite Off
        Cull Off

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float4 color      : COLOR;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float4 screenPos  : TEXCOORD0;
                float3 normalWS   : TEXCOORD1;
                float3 viewDirWS  : TEXCOORD2;
                float4 color      : COLOR;
                float3 positionOS : TEXCOORD3;
                float fogFactor   : TEXCOORD4;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _Color;
                float _Brightness;
                float _DistanceFalloff;
                float _RadialSoftness;
                float _SoftFactor;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output = (Varyings)0;

                VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs normalInput = GetVertexNormalInputs(input.normalOS);

                output.positionCS = vertexInput.positionCS;
                output.screenPos  = ComputeScreenPos(vertexInput.positionCS);
                output.normalWS   = normalInput.normalWS;
                output.viewDirWS  = GetWorldSpaceViewDir(vertexInput.positionWS);
                output.color      = input.color;
                output.positionOS = input.positionOS.xyz;
                output.fogFactor  = ComputeFogFactor(vertexInput.positionCS.z);

                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                // 1. Soft Fade เมื่อแสงชนหินหรือพื้น Terrain
                float2 screenUV = input.screenPos.xy / input.screenPos.w;
                #if UNITY_REVERSED_Z
                    float rawDepth = SampleSceneDepth(screenUV);
                #else
                    float rawDepth = lerp(UNITY_NEAR_CLIP_VALUE, 1.0, SampleSceneDepth(screenUV));
                #endif
                float sceneLinearDepth = LinearEyeDepth(rawDepth, _ZBufferParams);
                float surfaceLinearDepth = input.screenPos.w;
                float depthDiff = sceneLinearDepth - surfaceLinearDepth;
                float softDepthFade = saturate(depthDiff / _SoftFactor);

                // 2. เกลี่ยขอบข้าง (Radial Softness) ให้ฟุ้งเนียน ไม่เห็นขอบกรวยโพลีกอน
                float3 normalWS = normalize(input.normalWS);
                float3 viewDirWS = normalize(input.viewDirWS);
                float NdotV = abs(dot(normalWS, viewDirWS));
                float radialFade = pow(saturate(NdotV), _RadialSoftness);

                // 3. ยิ่งส่องไปไกล ยิ่งค่อยๆ สลายหายไปในน้ำ (Distance / Length Falloff)
                float lengthRatio = saturate(input.color.a); 
                float distFade = pow(lengthRatio, _DistanceFalloff);

                // 4. ผสมความสว่างแบบ Multi-pass Falloff
                float totalAlpha = distFade * radialFade * softDepthFade;
                float4 finalColor = _Color * _Brightness * totalAlpha;

                // กลืนเข้ากับหมอกใต้น้ำ
                finalColor.rgb = MixFog(finalColor.rgb, input.fogFactor);

                return finalColor;
            }
            ENDHLSL
        }
    }
    FallBack Off
}