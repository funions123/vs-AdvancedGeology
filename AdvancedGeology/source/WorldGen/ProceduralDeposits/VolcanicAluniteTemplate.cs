using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using Vintagestory.API.Server;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace AdvancedGeology.WorldGen.ProceduralDeposits;

public enum VolcanicAluniteZone
{
    None = 0,
    Alunite,
    QuartzAlunite,
    VuggySilica,
    Gypsum
}

[JsonObject(MemberSerialization.OptIn)]
public sealed class VolcanicAluniteDefinition
{
    // Model extent is 72x60. The lithocap radius reaches 35 with a lobe multiplier up to ~1.36,
    // but the flare term caps the effective radius well below that.
    [JsonProperty]
    public int HorizontalRadius { get; set; } = 42;

    [JsonProperty]
    public int VerticalHalfHeight { get; set; } = 26;

    [JsonProperty]
    public double CapBaseYMin { get; set; } = -8.0;

    [JsonProperty]
    public double CapBaseYMax { get; set; } = -3.0;

    [JsonProperty]
    public double CapTopYMin { get; set; } = 8.0;

    [JsonProperty]
    public double CapTopYMax { get; set; } = 13.0;

    [JsonProperty]
    public double CapRadiusMin { get; set; } = 27.0;

    [JsonProperty]
    public double CapRadiusMax { get; set; } = 35.0;

    [JsonProperty]
    public int ConduitCountMin { get; set; } = 1;

    [JsonProperty]
    public int ConduitCountMax { get; set; } = 3;

    [JsonProperty]
    public double ConduitSpacingMin { get; set; } = 7.0;

    [JsonProperty]
    public double ConduitSpacingMax { get; set; } = 12.0;

    [JsonProperty]
    public double ConduitXJitter { get; set; } = 3.0;

    [JsonProperty]
    public double ConduitZSpread { get; set; } = 6.0;

    [JsonProperty]
    public double ConduitRadiusMin { get; set; } = 3.2;

    [JsonProperty]
    public double ConduitRadiusMax { get; set; } = 5.4;

    [JsonProperty]
    public int LensCountMin { get; set; } = 3;

    [JsonProperty]
    public int LensCountMax { get; set; } = 5;

    [JsonProperty]
    public double LensCenterJitter { get; set; } = 7.0;

    [JsonProperty]
    public double LensRadiusXMin { get; set; } = 13.0;

    [JsonProperty]
    public double LensRadiusXMax { get; set; } = 19.0;

    [JsonProperty]
    public double LensRadiusZMin { get; set; } = 9.0;

    [JsonProperty]
    public double LensRadiusZMax { get; set; } = 15.0;

    [JsonProperty]
    public double LensHalfThicknessMin { get; set; } = 2.0;

    [JsonProperty]
    public double LensHalfThicknessMax { get; set; } = 3.6;

    [JsonProperty]
    public int VugCountMin { get; set; } = 4;

    [JsonProperty]
    public int VugCountMax { get; set; } = 8;

    [JsonProperty]
    public double VugCenterJitter { get; set; } = 4.0;

    [JsonProperty]
    public double VugRadiusMin { get; set; } = 1.2;

    [JsonProperty]
    public double VugRadiusMax { get; set; } = 2.4;
}

/// <summary>
/// Result of classifying one voxel in an advanced-argillic lithocap.
/// </summary>
public readonly record struct VolcanicAluniteSample(
    VolcanicAluniteZone Zone,
    double Intensity = 0.0,
    bool InCap = false,
    bool InVug = false);
/// <summary>
/// Alunite, quartz-alunite, vuggy silica, and gypsum form a lobed advanced-argillic lithocap above steep feeder conduits and shallow replacement lenses.
/// </summary>
internal sealed class VolcanicAlunitePlan
{
    private const ulong PlanSalt = 0x414C554E4954454CUL; // "ALUNITEL"

    // Local model floor: y < -30 is below the modelled edifice (GRID_Y / 2).
    private const double BottomY = -30.0;

    private readonly ulong featureId;

    private readonly int originX;
    private readonly int originY;
    private readonly int originZ;

    private readonly double capBaseY;
    private readonly double capTopY;
    private readonly double radius;
    private readonly double seed;

    private readonly Conduit[] conduits;
    private readonly AluniteLens[] lenses;
    private readonly Vug[] vugs;

    public int OriginX => originX;
    public int OriginY => originY;
    public int OriginZ => originZ;
    public double Seed => seed;
    public double CapBaseY => capBaseY;
    public double CapTopY => capTopY;
    public int ConduitCount => conduits.Length;
    public int LensCount => lenses.Length;
    public int VugCount => vugs.Length;

    private VolcanicAlunitePlan(
        ulong featureId,
        in ProceduralDepositInstance instance,
        double capBaseY,
        double capTopY,
        double radius,
        double seed,
        Conduit[] conduits,
        AluniteLens[] lenses,
        Vug[] vugs)
    {
        this.featureId = featureId;
        originX = instance.CenterX;
        originY = instance.CenterY;
        originZ = instance.CenterZ;
        this.capBaseY = capBaseY;
        this.capTopY = capTopY;
        this.radius = radius;
        this.seed = seed;
        this.conduits = conduits;
        this.lenses = lenses;
        this.vugs = vugs;
    }

    /// <summary>
    /// Builds the deposit geometry in deterministic parameter order.
    /// </summary>
    public static VolcanicAlunitePlan Create(
        in ProceduralDepositInstance instance,
        VolcanicAluniteDefinition settings)
    {
        var random = new ProceduralDepositRandom(instance.FeatureId ^ PlanSalt);

        double capBaseY = random.Range(settings.CapBaseYMin, settings.CapBaseYMax);
        double capTopY = random.Range(settings.CapTopYMin, settings.CapTopYMax);
        double radius = random.Range(settings.CapRadiusMin, settings.CapRadiusMax);
        double seed = random.Range(0.0, 100.0);

        int conduitCount = random.NextInt(settings.ConduitCountMin, settings.ConduitCountMax);
        var conduits = new Conduit[conduitCount];
        for (int i = 0; i < conduitCount; i++)
        {
            double centered = i - (conduitCount - 1) / 2.0;
            conduits[i] = new Conduit(
                centered * random.Range(settings.ConduitSpacingMin, settings.ConduitSpacingMax)
                    + random.Range(-settings.ConduitXJitter, settings.ConduitXJitter),
                random.Range(-settings.ConduitZSpread, settings.ConduitZSpread),
                random.Range(settings.ConduitRadiusMin, settings.ConduitRadiusMax),
                seed + i * 29.0);
        }

        int lensCount = random.NextInt(settings.LensCountMin, settings.LensCountMax);
        double scale = Math.Sqrt(4.0 / lensCount);
        var lenses = new AluniteLens[lensCount];
        for (int i = 0; i < lensCount; i++)
        {
            Conduit source = conduits[i % conduits.Length];
            lenses[i] = new AluniteLens(
                source.CenterX + random.Range(-settings.LensCenterJitter, settings.LensCenterJitter),
                source.CenterZ + random.Range(-settings.LensCenterJitter, settings.LensCenterJitter),
                random.Range(capBaseY + 1.5, capTopY - 2.0),
                random.Range(settings.LensRadiusXMin, settings.LensRadiusXMax) * scale,
                random.Range(settings.LensRadiusZMin, settings.LensRadiusZMax) * scale,
                random.Range(settings.LensHalfThicknessMin, settings.LensHalfThicknessMax),
                seed + 51.0 + i * 19.0);
        }

        int vugCount = random.NextInt(settings.VugCountMin, settings.VugCountMax);
        var vugs = new Vug[vugCount];
        for (int i = 0; i < vugCount; i++)
        {
            Conduit source = conduits[i % conduits.Length];
            vugs[i] = new Vug(
                source.CenterX + random.Range(-settings.VugCenterJitter, settings.VugCenterJitter),
                random.Range(capBaseY + 3.0, capTopY - 3.0),
                source.CenterZ + random.Range(-settings.VugCenterJitter, settings.VugCenterJitter),
                random.Range(settings.VugRadiusMin, settings.VugRadiusMax));
        }

        return new VolcanicAlunitePlan(
            instance.FeatureId,
            instance,
            capBaseY,
            capTopY,
            radius,
            seed,
            conduits,
            lenses,
            vugs);
    }

    /// <summary><c>capFactor</c>: lobed, vertically flaring lithocap envelope.</summary>
    public double CapFactor(double x, double y, double z)
    {
        if (y < capBaseY || y > capTopY) return 0.0;

        double az = Math.Atan2(z, x);
        double lobe = 1.0 + 0.22 * Math.Sin(az * 2.0 + seed * 0.2) + 0.14 * Math.Cos(az * 3.0 - seed * 0.35);
        double height = Clamp((y - capBaseY) / (capTopY - capBaseY), 0.0, 1.0);
        double flare = 0.52 + 0.48 * Math.Pow(height, 0.55) - 0.35 * Math.Pow(height, 3.2);
        double effective = radius * lobe * flare;
        double r = Math.Sqrt(x * x + z * z) / Math.Max(3.0, effective);
        double warp = Math.Sin(x * 0.09 + y * 0.05 + seed) * 0.07
            + Math.Cos(z * 0.08 - seed * 0.3) * 0.07;
        double lateral = Clamp((1.0 - (r + warp)) / 0.3, 0.0, 1.0);
        double root = Clamp((y - capBaseY) / 3.5, 0.0, 1.0);
        return lateral * (0.35 + 0.65 * root);
    }

    /// <summary>Model <c>conduitProximity</c>.</summary>
    public double ConduitProximity(double x, double y, double z)
    {
        double best = 0.0;
        for (int i = 0; i < conduits.Length; i++)
        {
            Conduit c = conduits[i];
            double d = Math.Sqrt((x - c.CenterX) * (x - c.CenterX) + (z - c.CenterZ) * (z - c.CenterZ));
            double local = c.Radius + Math.Sin(y * 0.16 + c.Phase) * 0.8;
            best = Math.Max(best, Clamp(1.0 - d / (local * 2.6), 0.0, 1.0));
        }

        return best;
    }

    public VolcanicAluniteSample Evaluate(int worldX, int worldY, int worldZ, int surfaceY = int.MaxValue)
    {
        if (worldY > surfaceY) return default;

        double x = worldX - originX;
        double y = worldY - originY;
        double z = worldZ - originZ;
        if (y < BottomY) return default;

        if (y < capBaseY - 1.0) return default;

        double cap = CapFactor(x, y, z);
        if (cap <= 0.0) return default;

        if (InVug(x, y, z))
        {
            return new VolcanicAluniteSample(VolcanicAluniteZone.None, cap, true, true);
        }

        double conduit = ConduitProximity(x, y, z);
        double intensity = cap * (0.45 + 0.55 * conduit);
        double texture = Math.Sin(x * 0.19 - z * 0.14 + seed) + 0.5 * Math.Cos(y * 0.21 + x * 0.07);
        double nearTop = capTopY - y;
        double oxid = Math.Sin(x * 0.31 + seed) * Math.Cos(z * 0.27 - seed * 0.5)
            + 0.55 * Math.Sin((x + z) * 0.19 - seed * 0.2);

        if (nearTop < 4.5 && intensity > 0.4 && oxid > 0.62)
        {
            return new VolcanicAluniteSample(VolcanicAluniteZone.None, intensity, true, false);
        }

        if (nearTop < 7.0 && intensity > 0.3 && oxid < -0.75 && texture < -0.2)
        {
            return new VolcanicAluniteSample(VolcanicAluniteZone.Gypsum, intensity, true, false);
        }

        LensHit? lens = SampleLens(x, y, z);
        if (lens.HasValue && intensity > 0.22)
        {
            VolcanicAluniteZone zone = lens.Value.Ratio < 0.72 || texture > 0.0
                ? VolcanicAluniteZone.Alunite
                : VolcanicAluniteZone.QuartzAlunite;
            return new VolcanicAluniteSample(zone, intensity, true, false);
        }

        if (InConduit(x, y, z) && intensity > 0.35)
        {
            return new VolcanicAluniteSample(
                texture > -0.2 ? VolcanicAluniteZone.VuggySilica : VolcanicAluniteZone.QuartzAlunite,
                intensity,
                true,
                false);
        }

        if (intensity > 0.6)
        {
            return new VolcanicAluniteSample(
                texture > 0.1 ? VolcanicAluniteZone.VuggySilica : VolcanicAluniteZone.QuartzAlunite,
                intensity,
                true,
                false);
        }

        return new VolcanicAluniteSample(VolcanicAluniteZone.None, intensity, true, false);
    }

    /// <summary><c>sampleLens</c>: warped elliptical massive alunite lens.</summary>
    private LensHit? SampleLens(double x, double y, double z)
    {
        LensHit? best = null;
        for (int i = 0; i < lenses.Length; i++)
        {
            AluniteLens l = lenses[i];
            double u = (x - l.CenterX) / l.RadiusX;
            double v = (z - l.CenterZ) / l.RadiusZ;
            double warp = Math.Sin(u * 4.0 + l.Phase) * Math.Cos(v * 3.0) * 0.09;
            double foot = Math.Sqrt(u * u + v * v) + warp;
            if (foot > 1.03) continue;

            double taper = Clamp(1.0 - foot * foot, 0.0, 1.0);
            double half = 0.4 + l.HalfThickness * taper;
            double d = Math.Abs(y - l.Level);
            if (d <= half && (!best.HasValue || foot < best.Value.Footprint))
            {
                best = new LensHit(foot, d / Math.Max(0.4, half));
            }
        }

        return best;
    }

    /// <summary>Model <c>inConduit</c>.</summary>
    private bool InConduit(double x, double y, double z)
    {
        for (int i = 0; i < conduits.Length; i++)
        {
            Conduit c = conduits[i];
            double d = Math.Sqrt((x - c.CenterX) * (x - c.CenterX) + (z - c.CenterZ) * (z - c.CenterZ));
            double local = c.Radius + Math.Sin(y * 0.16 + c.Phase) * 0.8;
            if (d <= local) return true;
        }

        return false;
    }

    /// <summary>Model <c>inVug</c>.</summary>
    private bool InVug(double x, double y, double z)
    {
        for (int i = 0; i < vugs.Length; i++)
        {
            Vug v = vugs[i];
            double dx = x - v.CenterX;
            double dy = y - v.CenterY;
            double dz = z - v.CenterZ;
            if (Math.Sqrt(dx * dx + dy * dy + dz * dz) <= v.Radius) return true;
        }

        return false;
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

    private readonly record struct LensHit(double Footprint, double Ratio);

    private readonly record struct Conduit(
        double CenterX,
        double CenterZ,
        double Radius,
        double Phase);

    private readonly record struct AluniteLens(
        double CenterX,
        double CenterZ,
        double Level,
        double RadiusX,
        double RadiusZ,
        double HalfThickness,
        double Phase);

    private readonly record struct Vug(
        double CenterX,
        double CenterY,
        double CenterZ,
        double Radius);
}

internal sealed class VolcanicAluniteProceduralTemplate : IProceduralDepositTemplate
{
    private static readonly string[] RequiredSlots =
    {
        ProceduralMaterialSlots.Alunite,
        ProceduralMaterialSlots.Quartz,
        ProceduralMaterialSlots.Gypsum
    };

    public string Code => "volcanicAlunite";
    public IReadOnlyList<string> RequiredMaterialSlots => RequiredSlots;
    public bool RequiresPlan => true;

    public int GetMaximumHorizontalReach(ProceduralDepositDefinition definition)
    {
        return definition.VolcanicAlunite.HorizontalRadius;
    }

    public object CreatePlan(in ProceduralDepositInstance instance, ProceduralDepositDefinition definition)
    {
        return VolcanicAlunitePlan.Create(instance, definition.VolcanicAlunite);
    }

    public bool Validate(ProceduralDepositDefinition definition, out string error)
    {
        VolcanicAluniteDefinition settings = definition.VolcanicAlunite;
        bool valid = settings.HorizontalRadius >= 16
            && settings.VerticalHalfHeight >= 8
            && settings.CapBaseYMax >= settings.CapBaseYMin
            && settings.CapTopYMax >= settings.CapTopYMin
            && settings.CapTopYMin > settings.CapBaseYMax
            && settings.CapRadiusMin > 0.0
            && settings.CapRadiusMax >= settings.CapRadiusMin
            && settings.ConduitCountMin >= 1
            && settings.ConduitCountMax >= settings.ConduitCountMin
            && settings.ConduitSpacingMin > 0.0
            && settings.ConduitSpacingMax >= settings.ConduitSpacingMin
            && settings.ConduitRadiusMin > 0.0
            && settings.ConduitRadiusMax >= settings.ConduitRadiusMin
            && settings.LensCountMin >= 1
            && settings.LensCountMax >= settings.LensCountMin
            && settings.LensRadiusXMin > 0.0
            && settings.LensRadiusXMax >= settings.LensRadiusXMin
            && settings.LensRadiusZMin > 0.0
            && settings.LensRadiusZMax >= settings.LensRadiusZMin
            && settings.LensHalfThicknessMin > 0.0
            && settings.LensHalfThicknessMax >= settings.LensHalfThicknessMin
            && settings.VugCountMin >= 0
            && settings.VugCountMax >= settings.VugCountMin
            && settings.VugRadiusMin > 0.0
            && settings.VugRadiusMax >= settings.VugRadiusMin;
        error = valid ? string.Empty : "invalid volcanic alunite settings";
        return valid;
    }

    public void Realize(
        ProceduralDepositWorldGenSystem system,
        in DepositCandidate candidate,
        IChunkColumnGenerateRequest request,
        int baseX,
        int baseZ)
    {
        system.RealizeVolcanicAluniteCandidate(candidate, request, baseX, baseZ);
    }
}

public sealed partial class ProceduralDepositWorldGenSystem
{
    internal void RealizeVolcanicAluniteCandidate(
        DepositCandidate candidate,
        IChunkColumnGenerateRequest request,
        int baseX,
        int baseZ)
    {
        CompiledProceduralDeposit compiled = candidate.Compiled;
        VolcanicAluniteDefinition settings = compiled.Definition.VolcanicAlunite;
        var plan = (VolcanicAlunitePlan)candidate.Plan!;
        ushort[] heightMap = request.Chunks[0].MapChunk.WorldGenTerrainHeightMap;
        int worldHeight = serverApi!.World.BlockAccessor.MapSizeY;
        int minimumY = GameMath.Clamp(candidate.Instance.CenterY - settings.VerticalHalfHeight, 1, worldHeight - 2);
        int maximumPrototypeY = GameMath.Clamp(candidate.Instance.CenterY + settings.VerticalHalfHeight, 1, worldHeight - 2);

        int[] zoneSlots = BuildVolcanicAluniteZoneSlots(compiled);

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
                    VolcanicAluniteSample sample = plan.Evaluate(worldX, y, worldZ, surfaceY);
                    if (sample.Zone == VolcanicAluniteZone.None) continue;

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

    private static int[] BuildVolcanicAluniteZoneSlots(CompiledProceduralDeposit compiled)
    {
        var slots = new int[Enum.GetValues<VolcanicAluniteZone>().Length];
        Array.Fill(slots, -1);
        slots[(int)VolcanicAluniteZone.Alunite] = compiled.GetSlotId(ProceduralMaterialSlots.Alunite);

        // Quartz-alunite veins and the vuggy silica core both resolve to quartz.
        int quartzSlot = compiled.GetSlotId(ProceduralMaterialSlots.Quartz);
        slots[(int)VolcanicAluniteZone.QuartzAlunite] = quartzSlot;
        slots[(int)VolcanicAluniteZone.VuggySilica] = quartzSlot;

        slots[(int)VolcanicAluniteZone.Gypsum] = compiled.GetSlotId(ProceduralMaterialSlots.Gypsum);
        return slots;
    }
}
