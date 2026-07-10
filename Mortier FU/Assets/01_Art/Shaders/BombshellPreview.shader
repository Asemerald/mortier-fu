Shader "Custom/BombshellPreview_Decal"
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

        _InitialRingOffset ("Initial Ring Offset", Range(0,1)) = 0

        _SpawnMinRadius ("Spawn Min Radius", Range(0,0.95)) = 0


        [Header(Random)]
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
            "RenderPipeline"="UniversalPipeline"
            "RenderType"="Transparent"
        }


        Blend SrcAlpha OneMinusSrcAlpha

        ZWrite Off
        Cull Front



        Pass
        {
            Name "DecalPreview"


            HLSLPROGRAM


            #pragma vertex vert
            #pragma fragment frag


            #pragma multi_compile_instancing


            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"



            struct Attributes
            {
                float4 positionOS : POSITION;

                UNITY_VERTEX_INPUT_INSTANCE_ID
            };


            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;

                UNITY_VERTEX_INPUT_INSTANCE_ID
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



            Varyings vert(Attributes IN)
            {
                Varyings OUT;

                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_TRANSFER_INSTANCE_ID(IN, OUT);


                float3 positionWS =
                    TransformObjectToWorld(
                        IN.positionOS.xyz
                    );


                OUT.positionWS = positionWS;


                OUT.positionCS =
                    TransformWorldToHClip(
                        positionWS
                    );


                return OUT;
            }
            // -----------------------------
            // Random stable
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
            // Centre décalé
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
            // Forme anneau
            // -----------------------------

            float ringShape(
                float r,
                float radius,
                float thickness
            )
            {
                float d =
                    abs(
                        r - radius
                    );


                return 1.0 -
                    smoothstep(
                        thickness,
                        thickness * 2.0,
                        d
                    );
            }




            // -----------------------------
            // Génération anneau
            // -----------------------------

            float generateRing(
                float r,
                int index,
                float time
            )
            {

                float phase =
                    (float)index /
                    max(_RingCount,1.0);



                float cycle =
                    phase +
                    _InitialRingOffset +
                    time * _RingSpeed;



                float progress =
                    frac(cycle);



                float cycleID =
                    floor(cycle);



                float seed =
                    _Seed +
                    index * 83.17 +
                    cycleID * 17.73;




                // Spawn aléatoire entre
                // SpawnMinRadius et bord

                float spawnRadius =
                    lerp(
                        _SpawnMinRadius,
                        1.0,
                        hash11(seed)
                    );



                // Mouvement vers l'extérieur

                float radius =
                    lerp(
                        spawnRadius,
                        1.0,
                        progress
                    );



                radius +=
                    (
                        hash11(seed + 5.0)
                        -0.5
                    )
                    *
                    _RingRadiusRandom
                    *
                    0.08;



                radius =
                    saturate(radius);




                float thickness =
                    _RingThickness *
                    lerp(
                        1.0 -
                        _RingThicknessRandom,

                        1.0 +
                        _RingThicknessRandom,

                        hash11(seed + 9.0)
                    );



                float mask =
                    ringShape(
                        r,
                        radius,
                        thickness
                    );




                // apparition douce

                float fadeIn =
                    smoothstep(
                        0.0,
                        0.15,
                        progress
                    );



                float fadeOut =
                    1.0 -
                    smoothstep(
                        0.9,
                        1.0,
                        progress
                    );



                return mask *
                       fadeIn *
                       fadeOut;
            }            // -----------------------------
            // Fragment Decal
            // -----------------------------

            float4 frag(Varyings IN) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(IN);



                // Position monde du pixel touché
                float3 positionWS =
                    IN.positionWS;



                // Passage dans l'espace local du decal
                float3 positionDS =
                mul(
                    unity_WorldToObject,
                    float4(positionWS,1)
                ).xyz;



                // UV du decal
                // Le Decal Projector travaille
                // en volume local centré autour de 0

                float2 uv =
                    positionDS.xz + 0.5;



                // -------------------------
                // Centres
                // -------------------------

                float2 discCenter =
                    float2(0.5,0.5);


                float2 ringCenter =
                    getRingCenter();



                // -------------------------
                // Distances radiales
                // -------------------------

                float rDisc =
                    length(
                        uv - discCenter
                    )
                    /
                    _Radius;



                float rRing =
                    length(
                        uv - ringCenter
                    )
                    /
                    _Radius;



                // -------------------------
                // Ombre principale
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
                    saturate(
                        edgeFade
                    );



                // Limite au disque
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



                // Hors volume decal
                // évite de dessiner partout

                float decalMask =
                    step(0, uv.x) *
                    step(0, uv.y) *
                    step(uv.x,1) *
                    step(uv.y,1);



                col *= decalMask;



                return col;
            }


            ENDHLSL
        }
    }
    
    FallBack Off
}