Shader "UI/TransitionMaskURP"
{
    Properties
    {
        _Mask ("Mask", 2D) = "white" {}
        _MaskAmount ("Mask Amount", Range(0,1)) = 0
        _Softness ("Softness", Range(0.001, 0.5)) = 0.05
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "RenderType"="Transparent"
            "IgnoreProjector"="True"
            "RenderPipeline"="UniversalPipeline"
            "CanUseSpriteAtlas"="True"
        }

        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off

        Pass
        {
            Name "UI"

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv         : TEXCOORD0;
                float4 color      : COLOR; // Image UI color
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv         : TEXCOORD0;
                float4 color      : COLOR;
            };

            TEXTURE2D(_Mask);
            SAMPLER(sampler_Mask);

            CBUFFER_START(UnityPerMaterial)
                float _MaskAmount;
                float _Softness;
            CBUFFER_END

            Varyings vert (Attributes v)
            {
                Varyings o;
                o.positionCS = TransformObjectToHClip(v.positionOS.xyz);
                o.uv = v.uv;
                o.color = v.color;
                return o;
            }

            half4 frag (Varyings i) : SV_Target
            {
                half mask = SAMPLE_TEXTURE2D(_Mask, sampler_Mask, i.uv).r;
                
                half fade = smoothstep(_MaskAmount, _MaskAmount + _Softness, mask);
                fade = 1.0h - fade;
                
                return half4(0, 0, 0, fade * i.color.a);
            }
            ENDHLSL
        }
    }
}
