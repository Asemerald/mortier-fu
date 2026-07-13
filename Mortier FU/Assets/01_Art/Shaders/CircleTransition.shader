Shader "Custom/CircleTransition"
{
    Properties
    {
        _Color ("Color", Color) = (0,0,0,1)
        _Progress ("Progress", Range(0, 1)) = 0
    }

    SubShader
    {
        Tags 
        { 
            "Queue"="Overlay" 
            "RenderType"="Transparent" 
        }

        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        ZTest Always
        Cull Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            float4 _Color;
            float _Progress;

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float2 center = float2(0.5, 0.5);

                float aspect = _ScreenParams.x / _ScreenParams.y;

                float2 correctedUv = i.uv - center;
                correctedUv.x *= aspect;

                float dist = length(correctedUv);

                float maxRadius = length(float2(0.5 * aspect, 0.5));
                float radius = _Progress * maxRadius * 1.15;

                float alpha = smoothstep(radius, radius, dist);

                return float4(_Color.rgb, alpha * _Color.a);
            }

            ENDCG
        }
    }
}