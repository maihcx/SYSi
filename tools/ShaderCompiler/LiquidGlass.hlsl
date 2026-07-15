// Liquid Glass displacement shader for WPF ShaderEffect.
// Target: ps_3_0

sampler2D Input : register(s0);

float2 InputSize             : register(c0);
float  CornerRadius          : register(c1);
float  RefractionDepth       : register(c2);
float  RefractionStrength    : register(c3);
float  ChromaticAberration   : register(c4);
float  Saturation            : register(c5);
float  Brightness            : register(c6);
float  EdgeHighlight         : register(c7);
float2 LightDirection        : register(c8);

float RoundedRectDistance(float2 position, float2 halfSize, float radius)
{
    float2 q = abs(position) - (halfSize - radius);
    return length(max(q, 0.0)) + min(max(q.x, q.y), 0.0) - radius;
}

float2 RoundedRectNormal(float2 position, float2 halfSize, float radius)
{
    const float epsilon = 0.75;

    float dx =
        RoundedRectDistance(position + float2(epsilon, 0.0), halfSize, radius) -
        RoundedRectDistance(position - float2(epsilon, 0.0), halfSize, radius);

    float dy =
        RoundedRectDistance(position + float2(0.0, epsilon), halfSize, radius) -
        RoundedRectDistance(position - float2(0.0, epsilon), halfSize, radius);

    float2 gradient = float2(dx, dy);
    float gradientLength = max(length(gradient), 0.0001);

    return gradient / gradientLength;
}

float3 ApplySaturation(float3 color, float saturation)
{
    float luminance = dot(color, float3(0.2126, 0.7152, 0.0722));
    return lerp(float3(luminance, luminance, luminance), color, saturation);
}

float4 main(float2 uv : TEXCOORD) : COLOR
{
    float2 size = max(InputSize, float2(1.0, 1.0));
    float2 halfSize = size * 0.5;
    float minimumHalfSize = min(halfSize.x, halfSize.y);

    float maximumRadius = max(minimumHalfSize - 0.5, 0.5);
    float radius = min(max(CornerRadius, 0.5), maximumRadius);

    float2 position = (uv - 0.5) * size;
    float2 absolutePosition = abs(position);
    float2 innerHalfSize = max(halfSize - radius, float2(0.001, 0.001));

    float signedDistance = RoundedRectDistance(position, halfSize, radius);

    clip(0.90 - signedDistance);

    float shapeMask = 1.0 - smoothstep(-0.90, 0.90, signedDistance);
    float insideDistance = max(-signedDistance, 0.0);

    float requestedDepth = max(RefractionDepth, 0.5);
    float maximumPanelDepth = max(1.0, minimumHalfSize * 0.72);
    float safeRequestedDepth = min(requestedDepth, maximumPanelDepth);

    float2 normal = RoundedRectNormal(position, halfSize, radius);

    float2 distanceInsideCornerJoin = innerHalfSize - absolutePosition;

    float distanceFromCornerJoin = max(
        distanceInsideCornerJoin.x,
        distanceInsideCornerJoin.y);

    float cornerTransitionWidth = max(
        radius * 1.35,
        min(safeRequestedDepth * 0.62, minimumHalfSize * 0.42));

    float cornerInfluence = 1.0 - smoothstep(
        0.0,
        max(cornerTransitionWidth, 1.0),
        distanceFromCornerJoin);

    float maximumCornerDepth = max(1.0, radius * 0.88);
    float cornerSafeDepth = min(safeRequestedDepth, maximumCornerDepth);

    float localDepth = lerp(
        safeRequestedDepth,
        cornerSafeDepth,
        cornerInfluence);

    localDepth = max(localDepth, 0.5);

    float normalizedDepth = saturate(insideDistance / localDepth);
    float smoothDepth =
        normalizedDepth * normalizedDepth *
        (3.0 - 2.0 * normalizedDepth);

    float edge = 1.0 - smoothDepth;

    float depthRatio = saturate(localDepth / max(safeRequestedDepth, 0.5));
    float opticalEdge = edge * lerp(0.82, 1.0, depthRatio);

    float cornerBoost = 1.0 + cornerInfluence * 0.12;

    float baseDisplacementPixels = min(
        RefractionStrength * 0.045 * cornerBoost,
        localDepth * 0.40);

    float chromaticStrength =
        ChromaticAberration * opticalEdge *
        (1.0 + cornerInfluence * 0.62);

    float redDisplacementPixels = min(
        baseDisplacementPixels + chromaticStrength * 0.68,
        localDepth * 0.54);

    float greenDisplacementPixels = min(
        baseDisplacementPixels + chromaticStrength * 0.32,
        localDepth * 0.48);

    float blueDisplacementPixels = baseDisplacementPixels;

    float2 inwardDirection = -normal / size;

    float2 redUv = saturate(
        uv + inwardDirection * redDisplacementPixels * opticalEdge);

    float2 greenUv = saturate(
        uv + inwardDirection * greenDisplacementPixels * opticalEdge);

    float2 blueUv = saturate(
        uv + inwardDirection * blueDisplacementPixels * opticalEdge);

    float4 redSample = tex2D(Input, redUv);
    float4 greenSample = tex2D(Input, greenUv);
    float4 blueSample = tex2D(Input, blueUv);

    float3 color = float3(
        redSample.r,
        greenSample.g,
        blueSample.b);

    color = ApplySaturation(color, max(Saturation, 0.0));
    color *= max(Brightness, 0.0);

    float2 normalizedLightDirection = normalize(
        LightDirection + float2(0.0001, 0.0001));

    float lightFacing = saturate(
        dot(-normal, normalizedLightDirection) * 0.5 + 0.5);

    float oppositeFacing = saturate(
        dot(normal, normalizedLightDirection) * 0.5 + 0.5);

    float brightRim =
        opticalEdge * EdgeHighlight *
        (0.12 + 0.88 * pow(lightFacing, 5.0));

    float darkRim =
        opticalEdge * EdgeHighlight * 0.18 *
        pow(oppositeFacing, 4.0);

    color += brightRim;
    color -= darkRim;

    float alpha = max(redSample.a, max(greenSample.a, blueSample.a));

    color = saturate(color) * shapeMask;
    alpha *= shapeMask;

    return float4(color, alpha);
}
