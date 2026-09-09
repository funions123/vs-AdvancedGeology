using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Vintagestory.API.Server;
using Vintagestory.API.Common;

namespace AdvancedGeology.WorldGen.ProceduralDeposits;

public enum FoldedAnthraciteZone
{
    None = 0,
    Anthracite,
    CrushedAnthracite,
    SlateParting
}

[JsonObject(MemberSerialization.OptIn)]
public sealed class FoldedAnthraciteDefinition
{
    // Model extent is 72x60 (x,z in [-36,36], y in [-30,30]). The circular clip of
    // HorizontalRadius + 4 must contain both that grid corner (r 50.9) and the largest
    // possible seam footprint (|centre| 14.1 + radiusAlong 33.9 = 48.1), so seams are never truncated.
    [JsonProperty]
    public int HorizontalRadius { get; set; } = 48;

    [JsonProperty]
    public int VerticalHalfHeight { get; set; } = 30;

    [JsonProperty]
    public double FoldAmplitudeMin { get; set; } = 8.0;

    [JsonProperty]
    public double FoldAmplitudeMax { get; set; } = 12.0;

    [JsonProperty]
    public double FoldFrequencyMin { get; set; } = 0.075;

    [JsonProperty]
    public double FoldFrequencyMax { get; set; } = 0.11;

    [JsonProperty]
    public double RegionalDipMin { get; set; } = -0.05;

    [JsonProperty]
    public double RegionalDipMax { get; set; } = 0.05;

    [JsonProperty]
    public double ThrustStrikeOffsetMin { get; set; } = 35.0;

    [JsonProperty]
    public double ThrustStrikeOffsetMax { get; set; } = 70.0;

    [JsonProperty]
    public double ThrustOffsetMin { get; set; } = -8.0;

    [JsonProperty]
    public double ThrustOffsetMax { get; set; } = 8.0;

    [JsonProperty]
    public double ThrustThrowMin { get; set; } = 6.0;

    [JsonProperty]
    public double ThrustThrowMax { get; set; } = 11.0;

    [JsonProperty]
    public double ThrustWidthMin { get; set; } = 9.0;

    [JsonProperty]
    public double ThrustWidthMax { get; set; } = 15.0;

    [JsonProperty]
    public int SeamCountMin { get; set; } = 1;

    [JsonProperty]
    public int SeamCountMax { get; set; } = 3;

    [JsonProperty]
    public double SeamHalfThicknessMin { get; set; } = 0.95;

    [JsonProperty]
    public double SeamHalfThicknessMax { get; set; } = 1.25;

    [JsonProperty]
    public double SeamRadiusAlongMin { get; set; } = 20.0;

    [JsonProperty]
    public double SeamRadiusAlongMax { get; set; } = 24.0;

    [JsonProperty]
    public double SeamRadiusAcrossMin { get; set; } = 14.0;

    [JsonProperty]
    public double SeamRadiusAcrossMax { get; set; } = 18.0;

    [JsonProperty]
    public double SeamLevelStartMin { get; set; } = -17.0;

    [JsonProperty]
    public double SeamLevelStartMax { get; set; } = -13.0;

    [JsonProperty]
    public double SeamLevelIncrementMin { get; set; } = 8.0;

    [JsonProperty]
    public double SeamLevelIncrementMax { get; set; } = 12.0;

    [JsonProperty]
    public double PartingChance { get; set; } = 0.58;
}

/// <summary>
/// Result of classifying one voxel in a folded anthracite seam package.
/// </summary>
public readonly record struct FoldedAnthraciteSample(
    FoldedAnthraciteZone Zone,
    int SeamIndex = -1,
    double Ratio = 0.0,
    bool Repeated = false);

/// <summary>
/// Anthracite, crushed anthracite, and slate partings form pinching seams folded through a metasedimentary sequence and repeated across an oblique thrust.
/// </summary>
internal sealed class FoldedAnthracitePlan
{
    private const ulong PlanSalt = 0x464F4C44414E5448UL; // "FOLDANTH"

    // Geometry constants that are not exposed as JSON settings.
    private const double FoldStrikeMin = 0.0;
    private const double FoldStrikeMax = 180.0;
    private const double SeamCenterMin = -10.0;
    private const double SeamCenterMax = 10.0;

    private readonly ulong featureId;
    private readonly int originX;
    private readonly int originY;
    private readonly int originZ;
    private readonly int horizontalRadius;
    private readonly int verticalHalfHeight;

    private readonly double seed;
    private readonly double foldStrikeDeg;
    private readonly double foldAmplitude;
    private readonly double foldFrequency;
    private readonly double regionalDip;
    private readonly double thrustStrikeDeg;
    private readonly double thrustOffset;
    private readonly double thrustThrow;
    private readonly double thrustWidth;

    private readonly double cosFoldStrike;
    private readonly double sinFoldStrike;
    private readonly double cosThrustStrike;
    private readonly double sinThrustStrike;

    private readonly AnthraciteSeam[] seams;

    public int OriginX => originX;
    public int OriginY => originY;
    public int OriginZ => originZ;
    public double Seed => seed;
    public double FoldStrikeDeg => foldStrikeDeg;
    public double FoldAmplitude => foldAmplitude;
    public double FoldFrequency => foldFrequency;
    public double RegionalDip => regionalDip;
    public double ThrustStrikeDeg => thrustStrikeDeg;
    public double ThrustOffset => thrustOffset;
    public double ThrustThrow => thrustThrow;
    public double ThrustWidth => thrustWidth;
    public int SeamCount => seams.Length;

    private FoldedAnthracitePlan(
        ulong featureId,
        in ProceduralDepositInstance instance,
        FoldedAnthraciteDefinition settings,
        double seed,
        double foldStrikeDeg,
        double foldAmplitude,
        double foldFrequency,
        double regionalDip,
        double thrustStrikeDeg,
        double thrustOffset,
        double thrustThrow,
        double thrustWidth,
        AnthraciteSeam[] seams)
    {
        this.featureId = featureId;
        originX = instance.CenterX;
        originY = instance.CenterY;
        originZ = instance.CenterZ;
        horizontalRadius = settings.HorizontalRadius;
        verticalHalfHeight = settings.VerticalHalfHeight;
        this.seed = seed;
        this.foldStrikeDeg = foldStrikeDeg;
        this.foldAmplitude = foldAmplitude;
        this.foldFrequency = foldFrequency;
        this.regionalDip = regionalDip;
        this.thrustStrikeDeg = thrustStrikeDeg;
        this.thrustOffset = thrustOffset;
        this.thrustThrow = thrustThrow;
        this.thrustWidth = thrustWidth;

        double foldStrikeRad = foldStrikeDeg * Math.PI / 180.0;
        cosFoldStrike = Math.Cos(foldStrikeRad);
        sinFoldStrike = Math.Sin(foldStrikeRad);

        double thrustStrikeRad = thrustStrikeDeg * Math.PI / 180.0;
        cosThrustStrike = Math.Cos(thrustStrikeRad);
        sinThrustStrike = Math.Sin(thrustStrikeRad);

        this.seams = seams;
    }

    /// <summary>
    /// Builds the deposit geometry in deterministic parameter order.
    /// </summary>
    public static FoldedAnthracitePlan Create(
        in ProceduralDepositInstance instance,
        FoldedAnthraciteDefinition settings)
    {
        var random = new ProceduralDepositRandom(instance.FeatureId ^ PlanSalt);

        double foldStrikeDeg = random.Range(FoldStrikeMin, FoldStrikeMax);
        double foldAmplitude = random.Range(settings.FoldAmplitudeMin, settings.FoldAmplitudeMax);
        double foldFrequency = random.Range(settings.FoldFrequencyMin, settings.FoldFrequencyMax);
        double regionalDip = random.Range(settings.RegionalDipMin, settings.RegionalDipMax);

        // JS evaluates `foldStrikeDeg + randomRange(35,70) * (Math.random() < .5 ? -1 : 1)`
        // left to right: the magnitude is drawn before the sign coin flip.
        double thrustStrikeMagnitude = random.Range(settings.ThrustStrikeOffsetMin, settings.ThrustStrikeOffsetMax);
        double thrustStrikeSign = random.Range(0.0, 1.0) < 0.5 ? -1.0 : 1.0;
        double thrustStrikeDeg = foldStrikeDeg + thrustStrikeMagnitude * thrustStrikeSign;

        double thrustOffset = random.Range(settings.ThrustOffsetMin, settings.ThrustOffsetMax);
        double thrustThrow = random.Range(settings.ThrustThrowMin, settings.ThrustThrowMax);
        double thrustWidth = random.Range(settings.ThrustWidthMin, settings.ThrustWidthMax);
        double seed = random.Range(0.0, 100.0);

        int count = random.NextInt(settings.SeamCountMin, settings.SeamCountMax);
        double sizeScale = Math.Sqrt(2.0 / count);

        var seams = new AnthraciteSeam[count];
        double level = random.Range(settings.SeamLevelStartMin, settings.SeamLevelStartMax);
        for (int i = 0; i < count; i++)
        {
            seams[i] = new AnthraciteSeam(
                level,
                random.Range(settings.SeamHalfThicknessMin, settings.SeamHalfThicknessMax),
                random.Range(SeamCenterMin, SeamCenterMax),
                random.Range(SeamCenterMin, SeamCenterMax),
                random.Range(settings.SeamRadiusAlongMin, settings.SeamRadiusAlongMax) * sizeScale,
                random.Range(settings.SeamRadiusAcrossMin, settings.SeamRadiusAcrossMax) * sizeScale,
                seed + i * 17.0,
                random.Range(0.0, 1.0) < settings.PartingChance);

    // The level advances at the end of every iteration, including the last.
            level += random.Range(settings.SeamLevelIncrementMin, settings.SeamLevelIncrementMax);
        }

        return new FoldedAnthracitePlan(
            instance.FeatureId,
            instance,
            settings,
            seed,
            foldStrikeDeg,
            foldAmplitude,
            foldFrequency,
            regionalDip,
            thrustStrikeDeg,
            thrustOffset,
            thrustThrow,
            thrustWidth,
            seams);
    }

    /// <summary><c>getStructuralCoordinates(x,z)</c>: rotation into the fold strike frame.</summary>
    public StructuralCoordinates GetStructuralCoordinates(double x, double z)
    {
        return new StructuralCoordinates(
            x * cosFoldStrike - z * sinFoldStrike,
            x * sinFoldStrike + z * cosFoldStrike);
    }

    /// <summary>
    /// <c>getStratY(x,y,z)</c>: unfolds the sequence with a two-harmonic fold train
    /// across strike plus the regional dip along strike.
    /// </summary>
    public double GetStratigraphicY(double x, double y, double z)
    {
        StructuralCoordinates p = GetStructuralCoordinates(x, z);
        double fold = foldAmplitude * Math.Sin(p.Across * foldFrequency + seed)
            + 1.0 * Math.Sin(p.Across * foldFrequency * 2.0 - seed * 0.3);
        return y - regionalDip * p.Along - fold;
    }

    /// <summary><c>getThrustSample(x,y,z)</c> in the thrust strike frame.</summary>
    public ThrustSample GetThrustSample(double x, double y, double z)
    {
        double across = x * sinThrustStrike + z * cosThrustStrike;
        double signed = across + y * 0.24 - thrustOffset - Math.Sin(x * 0.08 + seed) * 1.2;
        return new ThrustSample(signed, Math.Abs(signed));
    }

    /// <summary>
    /// <c>sampleSeams(stratY, thrust, x, y, z)</c>. Each seam is tested at its own
    /// stratigraphic level and, when the voxel sits in the hanging wall strip
    /// <c>0 &lt; signed &lt; thrustWidth</c>, a second time at <c>stratY + thrustThrow</c>.
    /// The nearest-by-ratio candidate across both tests wins.
    /// </summary>
    private SeamHit? SampleSeams(double stratY, in ThrustSample thrust, double x, double z)
    {
        StructuralCoordinates p = GetStructuralCoordinates(x, z);
        SeamHit? best = null;

        for (int i = 0; i < seams.Length; i++)
        {
            AnthraciteSeam seam = seams[i];
            double u = (p.Along - seam.CenterAlong) / seam.RadiusAlong;
            double v = (p.Across - seam.CenterAcross) / seam.RadiusAcross;
            double footprint = Math.Sqrt(u * u + v * v)
                + Math.Sin(u * 3.2 + seam.Phase) * Math.Cos(v * 2.8) * 0.03;
            if (footprint > 1.0) continue;

            double edgeScale = Clamp((1.0 - footprint) / 0.12, 0.0, 1.0);
            double thickness = Math.Max(
                0.78,
                seam.HalfThickness
                    * (0.94 + 0.06 * Math.Sin(x * 0.1 + z * 0.06 + seam.Phase))
                    * Math.Sqrt(edgeScale));

            double d = Math.Abs(stratY - seam.Level);
            if (d <= thickness && (!best.HasValue || d / thickness < best.Value.Ratio))
            {
                best = new SeamHit(i, d / thickness, false);
            }

            if (thrust.Signed > 0.0 && thrust.Signed < thrustWidth)
            {
                double dr = Math.Abs(stratY + thrustThrow - seam.Level);
                if (dr <= thickness && (!best.HasValue || dr / thickness < best.Value.Ratio))
                {
                    best = new SeamHit(i, dr / thickness, true);
                }
            }
        }

        return best;
    }

    public FoldedAnthraciteSample Evaluate(int worldX, int worldY, int worldZ, int surfaceY = int.MaxValue)
    {
        if (worldY > surfaceY) return new FoldedAnthraciteSample(FoldedAnthraciteZone.None);

        double x = worldX - originX;
        double y = worldY - originY;
        double z = worldZ - originZ;

        if (Math.Abs(y) > verticalHalfHeight) return new FoldedAnthraciteSample(FoldedAnthraciteZone.None);
        if (x * x + z * z > (horizontalRadius + 4.0) * (horizontalRadius + 4.0))
        {
            return new FoldedAnthraciteSample(FoldedAnthraciteZone.None);
        }

        double stratY = GetStratigraphicY(x, y, z);
        ThrustSample thrust = GetThrustSample(x, y, z);
        SeamHit? hit = SampleSeams(stratY, thrust, x, z);
        if (!hit.HasValue) return new FoldedAnthraciteSample(FoldedAnthraciteZone.None);

        SeamHit sample = hit.Value;

        // Fault core grinds the seam into crushed, still-mineable anthracite.
        if (thrust.Distance < 1.15)
        {
            return new FoldedAnthraciteSample(
                FoldedAnthraciteZone.CrushedAnthracite,
                sample.SeamIndex,
                sample.Ratio,
                sample.Repeated);
        }

        AnthraciteSeam seam = seams[sample.SeamIndex];
        if (seam.HasParting
            && sample.Ratio < 0.12
            && Math.Sin(x * 0.16 - z * 0.11 + seam.Phase) > 0.05)
        {
            return new FoldedAnthraciteSample(
                FoldedAnthraciteZone.SlateParting,
                sample.SeamIndex,
                sample.Ratio,
                sample.Repeated);
        }

        return new FoldedAnthraciteSample(
            FoldedAnthraciteZone.Anthracite,
            sample.SeamIndex,
            sample.Ratio,
            sample.Repeated);
    }

    /// <summary>Deterministic spatial hash.</summary>
    internal static double Hash3D(double x, double y, double z)
    {
        double n = Math.Sin(x * 12.9898 + y * 78.233 + z * 37.719) * 43758.5453;
        return n - Math.Floor(n);
    }

    private static double Clamp(double value, double min, double max)
    {
        return Math.Max(min, Math.Min(max, value));
    }

    public readonly record struct StructuralCoordinates(double Along, double Across);

    public readonly record struct ThrustSample(double Signed, double Distance);

    private readonly record struct SeamHit(int SeamIndex, double Ratio, bool Repeated);

    private readonly record struct AnthraciteSeam(
        double Level,
        double HalfThickness,
        double CenterAlong,
        double CenterAcross,
        double RadiusAlong,
        double RadiusAcross,
        double Phase,
        bool HasParting);
}

internal sealed class FoldedAnthraciteProceduralTemplate : IProceduralDepositTemplate
{
    private static readonly string[] RequiredSlots =
    {
        ProceduralMaterialSlots.Anthracite,
        ProceduralMaterialSlots.SlateParting
    };

    public string Code => "foldedAnthracite";
    public IReadOnlyList<string> RequiredMaterialSlots => RequiredSlots;
    public bool RequiresPlan => true;

    public int GetMaximumHorizontalReach(ProceduralDepositDefinition definition)
    {
        return definition.Anthracite.HorizontalRadius;
    }

    public object? CreatePlan(in ProceduralDepositInstance instance, ProceduralDepositDefinition definition)
    {
        return FoldedAnthracitePlan.Create(instance, definition.Anthracite);
    }

    public bool Validate(ProceduralDepositDefinition definition, out string error)
    {
        FoldedAnthraciteDefinition settings = definition.Anthracite;
        bool valid = settings.HorizontalRadius >= 16
            && settings.VerticalHalfHeight >= 8
            && settings.SeamCountMin >= 1
            && settings.SeamCountMax >= settings.SeamCountMin
            && settings.FoldAmplitudeMax >= settings.FoldAmplitudeMin
            && settings.FoldFrequencyMin > 0.0
            && settings.FoldFrequencyMax >= settings.FoldFrequencyMin
            && settings.RegionalDipMax >= settings.RegionalDipMin
            && settings.ThrustStrikeOffsetMax >= settings.ThrustStrikeOffsetMin
            && settings.ThrustOffsetMax >= settings.ThrustOffsetMin
            && settings.ThrustThrowMax >= settings.ThrustThrowMin
            && settings.ThrustWidthMin > 0.0
            && settings.ThrustWidthMax >= settings.ThrustWidthMin
            && settings.SeamHalfThicknessMin > 0.0
            && settings.SeamHalfThicknessMax >= settings.SeamHalfThicknessMin
            && settings.SeamRadiusAlongMin > 0.0
            && settings.SeamRadiusAlongMax >= settings.SeamRadiusAlongMin
            && settings.SeamRadiusAcrossMin > 0.0
            && settings.SeamRadiusAcrossMax >= settings.SeamRadiusAcrossMin
            && settings.SeamLevelStartMax >= settings.SeamLevelStartMin
            && settings.SeamLevelIncrementMax >= settings.SeamLevelIncrementMin
            && settings.PartingChance >= 0.0
            && settings.PartingChance <= 1.0;

        error = valid ? string.Empty : "Folded anthracite deposit geometry settings out of valid bounds";
        return valid;
    }

    public void Realize(
        ProceduralDepositWorldGenSystem system,
        in DepositCandidate candidate,
        IChunkColumnGenerateRequest request,
        int baseX,
        int baseZ)
    {
        system.RealizeFoldedAnthraciteCandidate(candidate, request, baseX, baseZ);
    }
}

public sealed partial class ProceduralDepositWorldGenSystem
{
    internal void RealizeFoldedAnthraciteCandidate(
        DepositCandidate candidate,
        IChunkColumnGenerateRequest request,
        int baseX,
        int baseZ)
    {
        CompiledProceduralDeposit compiled = candidate.Compiled;
        FoldedAnthraciteDefinition settings = compiled.Definition.Anthracite;
        FoldedAnthracitePlan plan = (FoldedAnthracitePlan)candidate.Plan!;
        ushort[] heightMap = request.Chunks[0].MapChunk.WorldGenTerrainHeightMap;
        int worldHeight = serverApi!.World.BlockAccessor.MapSizeY;
        int minimumY = Math.Clamp(candidate.Instance.CenterY - settings.VerticalHalfHeight, 1, worldHeight - 2);
        int maximumPrototypeY = Math.Clamp(candidate.Instance.CenterY + settings.VerticalHalfHeight, 1, worldHeight - 2);

        int[] zoneSlots = BuildFoldedAnthraciteZoneSlots(compiled);

        for (int localX = 0; localX < ChunkSize; localX++)
        {
            int worldX = baseX + localX;
            for (int localZ = 0; localZ < ChunkSize; localZ++)
            {
                int worldZ = baseZ + localZ;
                int surfaceY = heightMap[localZ * ChunkSize + localX];
                int maximumY = Math.Min(surfaceY, maximumPrototypeY);

                for (int y = minimumY; y <= maximumY; y++)
                {
                    FoldedAnthraciteSample sample = plan.Evaluate(worldX, y, worldZ, surfaceY);
                    if (sample.Zone == FoldedAnthraciteZone.None) continue;

                    int targetSlot = zoneSlots[(int)sample.Zone];
                    if (targetSlot < 0) continue;

                    int chunkY = y / ChunkSize;
                    if ((uint)chunkY >= (uint)request.Chunks.Length) continue;
                    int localY = y % ChunkSize;
                    int index3d = ((localY * ChunkSize) + localZ) * ChunkSize + localX;
                    IChunkBlocks data = request.Chunks[chunkY].Data;
                    int hostBlockId = data.GetBlockIdUnsafe(index3d);

                    if (!CanReplaceWithProceduralRock(compiled, hostBlockId)) continue;

                    int placeBlockId = compiled.ResolveBlock(targetSlot, hostBlockId);
                    if (placeBlockId == 0 || placeBlockId == hostBlockId) continue;

                    data.SetBlockUnsafe(index3d, placeBlockId);
                    data.SetFluid(index3d, 0);
                }
            }
        }
    }

    private static int[] BuildFoldedAnthraciteZoneSlots(CompiledProceduralDeposit compiled)
    {
        var slots = new int[Enum.GetValues<FoldedAnthraciteZone>().Length];
        Array.Fill(slots, -1);

        // Crushed fault-core anthracite stays ore: both seam facies use the anthracite slot.
        int anthraciteSlot = compiled.GetSlotId(ProceduralMaterialSlots.Anthracite);
        slots[(int)FoldedAnthraciteZone.Anthracite] = anthraciteSlot;
        slots[(int)FoldedAnthraciteZone.CrushedAnthracite] = anthraciteSlot;
        slots[(int)FoldedAnthraciteZone.SlateParting] = compiled.GetSlotId(ProceduralMaterialSlots.SlateParting);

        return slots;
    }
}
