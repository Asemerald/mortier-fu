#ifndef BOMBSHELL_DECAL_INCLUDED
#define BOMBSHELL_DECAL_INCLUDED


float hash11(float x)
{
    x = frac(x * 0.1031);
    x *= x + 33.33;
    x *= x + x;
    return frac(x);
}


float ringShape(
    float r,
    float radius,
    float thickness
)
{
    float d = abs(r - radius);

    return 1.0 -
        smoothstep(
            thickness,
            thickness * 2.0,
            d
        );
}



float generateRing(
    float r,
    int index,
    float Time,
    float RingCount,
    float Speed,
    float InitialOffset,
    float SpawnMinRadius,
    float RingThickness,
    float RingThicknessRandom,
    float RingRadiusRandom,
    float Seed
)
{
    float phase =
        (float)index /
        max(RingCount,1);


    float cycle =
        phase +
        InitialOffset +
        Time * Speed;


    float progress =
        frac(cycle);


    float cycleID =
        floor(cycle);



    float seed =
        Seed +
        index * 83.17 +
        cycleID * 17.73;



    float spawnRadius =
        lerp(
            SpawnMinRadius,
            1.0,
            hash11(seed)
        );



    float radius =
        lerp(
            spawnRadius,
            1.0,
            progress
        );



    radius +=
        (hash11(seed+5)-0.5)
        *
        RingRadiusRandom
        *
        0.08;



    float thickness =
        RingThickness *
        lerp(
            1.0 - RingThicknessRandom,
            1.0 + RingThicknessRandom,
            hash11(seed+9)
        );



    float mask =
        ringShape(
            r,
            radius,
            thickness
        );



    float fadeIn =
        smoothstep(
            0,
            0.15,
            progress
        );


    float fadeOut =
        1 -
        smoothstep(
            0.9,
            1,
            progress
        );


    return mask * fadeIn * fadeOut;
}




void Bombshell_float(
    float3 PositionOS,
    float Time,
    float Radius,
    float RingCount,
    float Speed,
    float RingThickness,
    float RingThicknessRandom,
    float RingRadiusRandom,
    float InitialOffset,
    float SpawnMinRadius,
    float Seed,

    out float3 Color,
    out float Alpha
)
{

    float2 uv =
        PositionOS.xz + 0.5;


    float r =
        length(
            uv - float2(0.5,0.5)
        )
        /
        Radius;



    float mask = 0;



    for(int i=0;i<12;i++)
    {
        if(i < RingCount)
        {
            mask += generateRing(
                r,
                i,
                Time,
                RingCount,
                Speed,
                InitialOffset,
                SpawnMinRadius,
                RingThickness,
                RingThicknessRandom,
                RingRadiusRandom,
                Seed
            );
        }
    }



    Color =
        lerp(
            float3(0,0,0),
            float3(1,0.35,0.05),
            saturate(mask)
        );


    Alpha =
        saturate(mask);

}


#endif