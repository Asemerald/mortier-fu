Shader "Custom/BombshellPreview"
{
    Properties
    {
        [Header(Shape)]
        _Radius ("Disc Radius", Range(0.05,1)) = 0.45
        _EdgeSoftness ("Edge Softness", Range(0.001,0.3)) = 0.05

        [Header(Base Shadow)]
        _BaseColor ("Shadow Color", Color) = (0,0,0,0.55)

        [Header(Rings)]
        _RingColor ("Ring Color", Color) = (1,0.35,0.05,1)

        _RingCount ("Ring Count", Range(1,12)) = 5
        _RingSpeed ("Ring Speed", Range(0,5)) = 1.2

        _RingThickness ("Ring Thickness", Range(0.005,0.2)) = 0.025
        _RingThicknessRandom ("Thickness Random", Range(0,1)) = 0.35

        _RingRadiusRandom ("Radius Random", Range(0,1)) = 0.15

        _RingFadeToEdge ("Fade To Edge", Range(0,1)) = 0.8

        // 0 = anneaux au départ
        // 1 = anneaux déjà presque en fin de cycle
        _InitialRingOffset ("Initial Ring Offset", Range(0,1)) = 0.0
        
        _SpawnMinRadius ("Spawn Min Radius", Range(0,0.95)) = 0.0


        [Header(Random Instance)]
        _Seed ("Random Seed", Float) = 0

        _CenterOffsetProbability
        (
            "Center Offset Probability",
            Range(0,1)
        ) = 0.35

        _CenterOffsetAmount
        (
            "Center Offset Amount",
            Range(0,0.25)
        ) = 0.05


        [Header(Global)]
        _Alpha ("Alpha", Range(0,1)) = 1
    }


    SubShader
    {
        Tags
        {
            "RenderType"="Transparent"
            "Queue"="Transparent"
            "RenderPipeline"="UniversalPipeline"
        }

        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off


        Pass
        {
            Name "MortarPreview"


            HLSLPROGRAM

            #pragma vertex vert
            #pragma fragment frag

            #pragma multi_compile_instancing


            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"



            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;

                UNITY_VERTEX_INPUT_INSTANCE_ID
            };


            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;

                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };



            CBUFFER_START(UnityPerMaterial)

                float4 _BaseColor;
                float4 _RingColor;

                float _Radius;
                float _EdgeSoftness;

                float _RingCount;
                float _RingSpeed;

                float _RingThickness;
                float _RingThicknessRandom;

                float _RingRadiusRandom;

                float _RingFadeToEdge;

                float _InitialRingOffset;
            
                float _SpawnMinRadius;


                float _Seed;

                float _CenterOffsetProbability;
                float _CenterOffsetAmount;


                float _Alpha;

            CBUFFER_END




            // -----------------------------
            // Stable random
            // -----------------------------

            float hash11(float x)
            {
                x = frac(x * 0.1031);
                x *= x + 33.33;
                x *= x + x;
                return frac(x);
            }



            float2 hash22(float p)
            {
                return float2(
                    hash11(p * 17.13),
                    hash11(p * 91.71)
                );
            }




            // -----------------------------
            // Vertex
            // -----------------------------

            Varyings vert(Attributes IN)
            {
                Varyings OUT;


                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_TRANSFER_INSTANCE_ID(IN,OUT);

                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);


                OUT.positionCS =
                    TransformObjectToHClip(IN.positionOS.xyz);


                OUT.uv = IN.uv;


                return OUT;
            }
            // -----------------------------
            // Ring function
            // -----------------------------
            // Retourne un anneau doux centré sur radius
            // r = distance actuelle du pixel
            //
            // radius     = position de l'anneau
            // thickness  = largeur
            // fade       = atténuation extérieure

            float ringShape(
                float r,
                float radius,
                float thickness
            )
            {
                float d = abs(r - radius);

                float ring =
                    1.0 -
                    smoothstep(
                        thickness,
                        thickness * 2.0,
                        d
                    );

                return ring;
            }



            // -----------------------------
            // Centre aléatoire des anneaux
            // -----------------------------

            float2 getRingCenter()
            {
                float2 rnd =
                    hash22(_Seed + 12.345);


                float useOffset =
                    step(
                        rnd.x,
                        _CenterOffsetProbability
                    );


                float angle =
                    rnd.y * 6.283185;


                float2 dir =
                    float2(
                        cos(angle),
                        sin(angle)
                    );


                return float2(0.5,0.5)
                    +
                    dir *
                    _CenterOffsetAmount *
                    useOffset;
            }




            // -----------------------------
            // Génération d'un anneau
            // -----------------------------

            float generateRing(float r, int index, float time)
{
    // Phase propre à l'anneau
    float phase = (float)index / max(_RingCount, 1.0);

    // Temps du cycle
    float cycle =
        phase +
        _InitialRingOffset +
        time * _RingSpeed;

    float progress = frac(cycle);
    float cycleID = floor(cycle);

    // Random stable pour cet anneau et ce cycle
    float seed =
        _Seed +
        index * 83.17 +
        cycleID * 17.73;

    // Rayon de départ aléatoire
    float spawnRadius =
        lerp(
            _SpawnMinRadius,
            1.0,
            hash11(seed)
        );

    // Déplacement du rayon de départ jusqu'au bord
    float radius =
        lerp(
            spawnRadius,
            1.0,
            progress
        );

    // Léger jitter radial
    radius +=
        (hash11(seed + 5.0) - 0.5)
        * _RingRadiusRandom
        * 0.08;

    radius = saturate(radius);

    // Épaisseur aléatoire
    float thickness =
        _RingThickness *
        lerp(
            1.0 - _RingThicknessRandom,
            1.0 + _RingThicknessRandom,
            hash11(seed + 9.0)
        );

    // Masque de l'anneau
    float mask =
        ringShape(
            r,
            radius,
            thickness
        );

    // Fade in au spawn
    float fadeIn =
        smoothstep(
            0.0,
            0.15,
            progress
        );

    // Fade out avant disparition
    float fadeOut =
        1.0 -
        smoothstep(
            0.9,
            1.0,
            progress
        );

    return mask * fadeIn * fadeOut;
}      // -----------------------------
            // Fragment
            // -----------------------------

            float4 frag(Varyings IN) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(IN);


                // -------------------------
                // Base
                // -------------------------

                float2 uv = IN.uv;


                float2 discCenter =
                    float2(0.5,0.5);



                float2 ringCenter =
                    getRingCenter();



                // Distance disque principal

                float rDisc =
                    length(
                        uv - discCenter
                    )
                    /
                    _Radius;



                // Distance anneaux

                float rRing =
                    length(
                        uv - ringCenter
                    )
                    /
                    _Radius;



                // -------------------------
                // Ombre de base
                // -------------------------

                float discMask =
                    1.0 -
                    smoothstep(
                        1.0 - _EdgeSoftness,
                        1.0,
                        rDisc
                    );


                float4 col =
                    _BaseColor *
                    discMask;



                // -------------------------
                // Anneaux
                // -------------------------

                float ringMask = 0;


                float time =
                    _Time.y;



                // Maximum fixe pour garder
                // un shader compatible GPU
                // même si RingCount varie

                [unroll]
                for(int i = 0; i < 12; i++)
                {

                    if(i < _RingCount)
                    {
                        ringMask +=
                            generateRing(
                                rRing,
                                i,
                                time
                            );
                    }

                }


                // Evite de dépasser 1

                ringMask =
                    saturate(ringMask);



                // -------------------------
                // Fade extérieur
                // -------------------------

                float edgeFade =
                    lerp(
                        1.0,
                        1.0 - rRing,
                        _RingFadeToEdge
                    );


                ringMask *=
                    saturate(edgeFade);



                // uniquement dans le disque

                ringMask *= discMask;



                // -------------------------
                // Couleur anneaux
                // -------------------------

                col.rgb =
                    lerp(
                        col.rgb,
                        _RingColor.rgb,
                        ringMask
                    );


                col.a =
                    max(
                        col.a,
                        ringMask *
                        _RingColor.a
                    );



                col.a *= _Alpha;



                return col;
            }


            ENDHLSL
        }
    }


    FallBack Off
}