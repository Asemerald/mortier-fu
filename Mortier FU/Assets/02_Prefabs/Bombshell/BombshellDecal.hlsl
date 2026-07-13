// -----------------------------
// RingDecal.hlsl
// Logique réutilisée depuis BombshellPreview_Decal.shader
// A référencer via un Custom Function Node (mode "File") dans un
// Shader Graph dont le Graph Setting "Material" = Decal
// -----------------------------

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
// Centre décalé de l'anneau
// -----------------------------

float2 GetRingCenter(float seed, float centerOffsetProbability, float centerOffsetAmount)
{
    float2 rnd = hash22(seed + 12.345);

    float useOffset = step(rnd.x, centerOffsetProbability);

    float angle = rnd.y * 6.283185;

    float2 dir = float2(cos(angle), sin(angle));

    return float2(0.5, 0.5) + dir * centerOffsetAmount * useOffset;
}

// -----------------------------
// Forme anneau
// -----------------------------

float RingShape(float r, float radius, float thickness)
{
    float d = abs(r - radius);

    return 1.0 - smoothstep(thickness, thickness * 2.0, d);
}

// -----------------------------
// Génération d'un anneau
// -----------------------------

float GenerateRing(
    float r,
    int index,
    float time,
    float ringCount,
    float ringSpeed,
    float initialRingOffset,
    float spawnMinRadius,
    float ringRadiusRandom,
    float ringThickness,
    float ringThicknessRandom,
    float seedBase)
{
    float phase = (float)index / max(ringCount, 1.0);

    float cycle = phase + initialRingOffset + time * ringSpeed;

    float progress = frac(cycle);
    float cycleID = floor(cycle);

    float seed = seedBase + index * 83.17 + cycleID * 17.73;

    // Spawn aléatoire entre SpawnMinRadius et le bord
    float spawnRadius = lerp(spawnMinRadius, 1.0, hash11(seed));

    // Mouvement vers l'extérieur
    float radius = lerp(spawnRadius, 1.0, progress);

    radius += (hash11(seed + 5.0) - 0.5) * ringRadiusRandom * 0.08;
    radius = saturate(radius);

    float thickness = ringThickness * lerp(
        1.0 - ringThicknessRandom,
        1.0 + ringThicknessRandom,
        hash11(seed + 9.0)
    );

    float mask = RingShape(r, radius, thickness);

    // Apparition / disparition douce
    float fadeIn = smoothstep(0.0, 0.15, progress);
    float fadeOut = 1.0 - smoothstep(0.9, 1.0, progress);

    return mask * fadeIn * fadeOut;
}

// -----------------------------
// Fonction d'entrée appelée par le Custom Function Node
// Le suffixe _float doit correspondre à la précision choisie
// dans le node (float ou half)
// -----------------------------

void RingDecal_float(
    float2 UV,
    float Time,
    float4 BaseColor,
    float4 RingColor,
    float Radius,
    float EdgeSoftness,
    float RingCount,
    float RingSpeed,
    float RingThickness,
    float RingThicknessRandom,
    float RingRadiusRandom,
    float RingFadeToEdge,
    float InitialRingOffset,
    float SpawnMinRadius,
    float Seed,
    float CenterOffsetProbability,
    float CenterOffsetAmount,
    float Alpha,
    out float3 OutColor,
    out float OutAlpha)
{
    // Rotation de 90° autour du centre des UV
    float2 uv = UV - 0.5;
    uv = float2(-uv.y, uv.x); // +90°
    UV = uv + 0.5;
    
    float2 discCenter = float2(0.5, 0.5);
    float2 ringCenter = GetRingCenter(Seed, CenterOffsetProbability, CenterOffsetAmount);

    // rDisc / rRing : distances radiales normalisées, comme dans le frag() d'origine
    float rDisc = length(UV - discCenter) / Radius;
    float rRing = length(UV - ringCenter) / Radius;

    // Ombre principale
    float discMask = 1.0 - smoothstep(1.0 - EdgeSoftness, 1.0, rDisc);

    float3 col = BaseColor.rgb;
    float colA = BaseColor.a * discMask;

    // Accumulation des anneaux
    float ringMask = 0.0;

    [loop]
    for (int i = 0; i < 12; i++)
    {
        if (i < (int)RingCount)
        {
            ringMask += GenerateRing(
                rRing, i, Time,
                RingCount, RingSpeed, InitialRingOffset, SpawnMinRadius,
                RingRadiusRandom, RingThickness, RingThicknessRandom, Seed
            );
        }
    }

    ringMask = saturate(ringMask);

    // Fade vers le bord du disque
    float edgeFade = lerp(1.0, 1.0 - rRing, RingFadeToEdge);
    ringMask *= saturate(edgeFade);
    ringMask *= discMask;

    // Couleur finale
    col = lerp(col, RingColor.rgb, ringMask);
    colA = max(colA, ringMask * RingColor.a);
    colA *= Alpha;

    OutColor = col;
    OutAlpha = colA;
}

