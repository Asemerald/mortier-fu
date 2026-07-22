Shader "Custom/CircleTransition"
{
    Properties
    {
        _MainTex ("Transition Texture", 2D) = "black" {}
        _Color ("Fallback / Tint Color", Color) = (0,0,0,1)
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

            sampler2D _MainTex;
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
                // On multiplie la texture par la couleur (qui est noire par défaut)
                fixed4 col = tex2D(_MainTex, i.uv) * _Color;

                float2 center = float2(0.5, 0.5);
                float aspect = _ScreenParams.x / _ScreenParams.y;

                float2 correctedUv = i.uv - center;
                correctedUv.x *= aspect;

                float dist = length(correctedUv);

                float maxRadius = length(float2(0.5 * aspect, 0.5));
                float radius = _Progress * maxRadius * 1.15;

                float alphaMask = smoothstep(radius, radius, dist);

                return float4(col.rgb, col.a * alphaMask);
            }

            ENDCG
        }
    }
}