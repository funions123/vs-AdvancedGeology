using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using Vintagestory.API.Server;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace AdvancedGeology.WorldGen.ProceduralDeposits;

public enum StratiformCinnabarZone
{
    None = 0,
    DenseCinnabar,
    CinnabarImpregnation,
    CinnabarJointFill,
    QuartzCarbonate,
    Pyrite
}

[JsonObject(MemberSerialization.OptIn)]
public sealed class StratiformCinnabarDefinition
{
    // Model extent is 72x60. Ore levels reach radiusAlong up to 29 and radiusDip up to 31 in a
    // steeply dipping frame, and the diatreme sits up to 14 blocks off-system.
    [JsonProperty]
    public int HorizontalRadius { get; set; } = 46;

    [JsonProperty]
    public int VerticalHalfHeight { get; set; } = 30;

    [JsonProperty]
    public double StrikeDegMin { get; set; } = 76.0;

    [JsonProperty]
    public double StrikeDegMax { get; set; } = 104.0;

    [JsonProperty]
    public double DipDegMin { get; set; } = 76.0;

    [JsonProperty]
    public double DipDegMax { get; set; } = 88.0;

    [JsonProperty]
    public double SystemOffsetSpread { get; set; } = 3.0;

    [JsonProperty]
    public int OreLevelCount { get; set; } = 3;

    [JsonProperty]
    public double LevelSpacing { get; set; } = 10.0;

    [JsonProperty]
    public double LevelOffsetJitter { get; set; } = 0.7;

    [JsonProperty]
    public double HostHalfMin { get; set; } = 2.5;

    [JsonProperty]
    public double HostHalfMax { get; set; } = 3.8;

    [JsonProperty]
    public double OreHalfMin { get; set; } = 1.1;

    [JsonProperty]
    public double OreHalfMax { get; set; } = 2.0;

    [JsonProperty]
    public double CenterAlongSpread { get; set; } = 5.0;

    [JsonProperty]
    public double CenterDipSpread { get; set; } = 4.0;

    [JsonProperty]
    public double RadiusAlongMin { get; set; } = 19.0;

    [JsonProperty]
    public double RadiusAlongMax { get; set; } = 29.0;

    [JsonProperty]
    public double RadiusDipMin { get; set; } = 22.0;

    [JsonProperty]
    public double RadiusDipMax { get; set; } = 31.0;

    [JsonProperty]
    public int JointSetsPerLevel { get; set; } = 2;

    [JsonProperty]
    public double JointAngleSpread { get; set; } = 0.5;

    [JsonProperty]
    public double JointOffsetSpread { get; set; } = 8.0;

    [JsonProperty]
    public double JointLengthMin { get; set; } = 12.0;

    [JsonProperty]
    public double JointLengthMax { get; set; } = 22.0;

    [JsonProperty]
    public double JointWidthMin { get; set; } = 0.18;

    [JsonProperty]
    public double JointWidthMax { get; set; } = 0.42;

    [JsonProperty]
    public double DiatremeXSpread { get; set; } = 14.0;

    [JsonProperty]
    public double DiatremeZSpread { get; set; } = 10.0;

    [JsonProperty]
    public double DiatremeBottomMin { get; set; } = -30.0;

    [JsonProperty]
    public double DiatremeBottomMax { get; set; } = -25.0;

    [JsonProperty]
    public double DiatremeTopMin { get; set; } = 15.0;

    [JsonProperty]
    public double DiatremeTopMax { get; set; } = 23.0;

    [JsonProperty]
    public double DiatremeBaseRadiusMin { get; set; } = 2.5;

    [JsonProperty]
    public double DiatremeBaseRadiusMax { get; set; } = 4.0;

    [JsonProperty]
    public double DiatremeTopRadiusMin { get; set; } = 5.5;

    [JsonProperty]
    public double DiatremeTopRadiusMax { get; set; } = 8.0;
}

/// <summary>
/// Result of classifying one voxel in a stratiform cinnabar system.
/// </summary>
public readonly record struct StratiformCinnabarSample(
    StratiformCinnabarZone Zone,
    int LevelIndex = -1,
    double Grain = 0.0,
    bool InsideOre = false,
    bool InsideHost = false);
/// <summary>
/// Cinnabar, quartz-carbonate, and pyrite form bedding-concordant replacement lenses, disseminations, and joint fills across several stratigraphic levels.
/// </summary>
internal sealed class StratiformCinnabarPlan
{
    private const ulong PlanSalt = 0x414C4D4144454E32UL; // "ALMADEN2"

    // Local model floor: y < -30 is below the modelled package (GRID_Y / 2).
    private const double BottomY = -30.0;

    private readonly ulong featureId;

    private readonly int originX;
    private readonly int originY;
    private readonly int originZ;

    private readonly double strikeDeg;
    private readonly double dipDeg;
    private readonly double systemX;
    private readonly double systemZ;
    private readonly double seed;

    private readonly double cosStrike;
    private readonly double sinStrike;
    private readonly double cosDip;
    private readonly double sinDip;

    private readonly double diatremeX;
    private readonly double diatremeZ;
    private readonly double diatremeBottom;
    private readonly double diatremeTop;
    private readonly double diatremeBaseRadius;
    private readonly double diatremeTopRadius;

    private readonly OreLevel[] oreLevels;
    private readonly JointSet[] jointSets;

    public int OriginX => originX;
    public int OriginY => originY;
    public int OriginZ => originZ;
    public double StrikeDeg => strikeDeg;
    public double DipDeg => dipDeg;
    public double Seed => seed;
    public int OreLevelCount => oreLevels.Length;
    public int JointSetCount => jointSets.Length;
    public double DiatremeBottom => diatremeBottom;
    public double DiatremeTop => diatremeTop;

    private StratiformCinnabarPlan(
        ulong featureId,
        in ProceduralDepositInstance instance,
        double strikeDeg,
        double dipDeg,
        double systemX,
        double systemZ,
        double seed,
        double diatremeX,
        double diatremeZ,
        double diatremeBottom,
        double diatremeTop,
        double diatremeBaseRadius,
        double diatremeTopRadius,
        OreLevel[] oreLevels,
        JointSet[] jointSets)
    {
        this.featureId = featureId;
        originX = instance.CenterX;
        originY = instance.CenterY;
        originZ = instance.CenterZ;
        this.strikeDeg = strikeDeg;
        this.dipDeg = dipDeg;
        this.systemX = systemX;
        this.systemZ = systemZ;
        this.seed = seed;
        this.diatremeX = diatremeX;
        this.diatremeZ = diatremeZ;
        this.diatremeBottom = diatremeBottom;
        this.diatremeTop = diatremeTop;
        this.diatremeBaseRadius = diatremeBaseRadius;
        this.diatremeTopRadius = diatremeTopRadius;
        this.oreLevels = oreLevels;
        this.jointSets = jointSets;

        double strikeRad = strikeDeg * Math.PI / 180.0;
        double dipRad = dipDeg * Math.PI / 180.0;
        cosStrike = Math.Cos(strikeRad);
        sinStrike = Math.Sin(strikeRad);
        cosDip = Math.Cos(dipRad);
        sinDip = Math.Sin(dipRad);
    }

    /// <summary>
    /// Builds the deposit geometry in deterministic parameter order.
    /// </summary>
    public static StratiformCinnabarPlan Create(
        in ProceduralDepositInstance instance,
        StratiformCinnabarDefinition settings)
    {
        var random = new ProceduralDepositRandom(instance.FeatureId ^ PlanSalt);

        double strikeDeg = random.Range(settings.StrikeDegMin, settings.StrikeDegMax);
        double dipDeg = random.Range(settings.DipDegMin, settings.DipDegMax);
        double systemX = random.Range(-settings.SystemOffsetSpread, settings.SystemOffsetSpread);
        double systemZ = random.Range(-settings.SystemOffsetSpread, settings.SystemOffsetSpread);
        double seed = random.Range(0.0, 100.0);
        double diatremeX = systemX + random.Range(-settings.DiatremeXSpread, settings.DiatremeXSpread);
        double diatremeZ = systemZ + random.Range(-settings.DiatremeZSpread, settings.DiatremeZSpread);
        double diatremeBottom = random.Range(settings.DiatremeBottomMin, settings.DiatremeBottomMax);
        double diatremeTop = random.Range(settings.DiatremeTopMin, settings.DiatremeTopMax);
        double diatremeBaseRadius = random.Range(
            settings.DiatremeBaseRadiusMin,
            settings.DiatremeBaseRadiusMax);
        double diatremeTopRadius = random.Range(
            settings.DiatremeTopRadiusMin,
            settings.DiatremeTopRadiusMax);

        int levelCount = settings.OreLevelCount;
        var levels = new OreLevel[levelCount];
        var joints = new List<JointSet>(levelCount * settings.JointSetsPerLevel);

    // Model offsets are [-10, 0, 10] for three levels: evenly spaced about the system.
        for (int i = 0; i < levelCount; i++)
        {
            double baseOffset = (i - (levelCount - 1) / 2.0) * settings.LevelSpacing;
            levels[i] = new OreLevel(
                baseOffset + random.Range(-settings.LevelOffsetJitter, settings.LevelOffsetJitter),
                random.Range(settings.HostHalfMin, settings.HostHalfMax),
                random.Range(settings.OreHalfMin, settings.OreHalfMax),
                random.Range(-settings.CenterAlongSpread, settings.CenterAlongSpread),
                random.Range(-settings.CenterDipSpread, settings.CenterDipSpread),
                random.Range(settings.RadiusAlongMin, settings.RadiusAlongMax),
                random.Range(settings.RadiusDipMin, settings.RadiusDipMax),
                seed + i * 23.0);

            for (int j = 0; j < settings.JointSetsPerLevel; j++)
            {
                joints.Add(new JointSet(
                    i,
                    random.Range(-settings.JointAngleSpread, settings.JointAngleSpread),
                    random.Range(-settings.JointOffsetSpread, settings.JointOffsetSpread),
                    random.Range(settings.JointLengthMin, settings.JointLengthMax),
                    random.Range(settings.JointWidthMin, settings.JointWidthMax),
                    seed + 71.0 + i * 19.0 + j * 7.0));
            }
        }

        return new StratiformCinnabarPlan(
            instance.FeatureId,
            instance,
            strikeDeg,
            dipDeg,
            systemX,
            systemZ,
            seed,
            diatremeX,
            diatremeZ,
            diatremeBottom,
            diatremeTop,
            diatremeBaseRadius,
            diatremeTopRadius,
            levels,
            joints.ToArray());
    }

    /// <summary>
    /// <c>localCoords</c>: along-strike, plane-normal and down-dip components of the
    /// steeply dipping stratigraphy.
    /// </summary>
    public LocalCoordinates GetLocalCoordinates(double x, double y, double z)
    {
        double dx = x - systemX;
        double dz = z - systemZ;
        double along = dx * cosStrike - dz * sinStrike;
        double across = dx * sinStrike + dz * cosStrike;
        return new LocalCoordinates(
            along,
            across * sinDip - y * cosDip,
            across * cosDip + y * sinDip);
    }

    public StratiformCinnabarSample Evaluate(int worldX, int worldY, int worldZ, int surfaceY = int.MaxValue)
    {
        if (worldY > surfaceY) return default;

        double x = worldX - originX;
        double y = worldY - originY;
        double z = worldZ - originZ;
        if (y < BottomY) return default;

        if (SampleDiatreme(x, y, z)) return default;

        int levelIndex = -1;
        LevelSample levelSample = default;
        for (int i = 0; i < oreLevels.Length; i++)
        {
            LevelSample candidate = SampleLevel(x, y, z, oreLevels[i]);
            if (candidate.Host)
            {
                levelIndex = i;
                levelSample = candidate;
                break;
            }
        }

        if (levelIndex < 0) return default;

        OreLevel level = oreLevels[levelIndex];
        double grain = Hash3D(x * 1.75 + seed, y * 1.83, z * 1.69);

        if (levelSample.Inside)
        {
            for (int i = 0; i < jointSets.Length; i++)
            {
                JointSet joint = jointSets[i];
                if (joint.LevelIndex != levelIndex) continue;
                if (!SampleJoint(joint, level, levelSample)) continue;

                return new StratiformCinnabarSample(
                    grain > 0.28
                        ? StratiformCinnabarZone.CinnabarJointFill
                        : StratiformCinnabarZone.QuartzCarbonate,
                    levelIndex,
                    grain,
                    true,
                    true);
            }

            double grade = Math.Sin(levelSample.Along * 0.13 + level.Phase)
                + 0.55 * Math.Cos(levelSample.DownDip * 0.11 - level.Phase * 0.35)
                + 0.3 * Math.Sin((levelSample.Along + levelSample.DownDip) * 0.21 + seed);

            if (levelSample.Footprint < 0.58
                && Math.Abs(levelSample.NormalRatio) < 0.55
                && grade > 0.72
                && grain > 0.34)
            {
                return new StratiformCinnabarSample(
                    StratiformCinnabarZone.DenseCinnabar,
                    levelIndex,
                    grain,
                    true,
                    true);
            }

            if (grade > 0.05 && grain > 0.56)
            {
                return new StratiformCinnabarSample(
                    StratiformCinnabarZone.CinnabarImpregnation,
                    levelIndex,
                    grain,
                    true,
                    true);
            }

            if (grain > 0.83)
            {
                return new StratiformCinnabarSample(
                    StratiformCinnabarZone.Pyrite,
                    levelIndex,
                    grain,
                    true,
                    true);
            }

            if (grain > 0.5)
            {
                return new StratiformCinnabarSample(
                    StratiformCinnabarZone.QuartzCarbonate,
                    levelIndex,
                    grain,
                    true,
                    true);
            }

            return new StratiformCinnabarSample(
                StratiformCinnabarZone.None,
                levelIndex,
                grain,
                true,
                true);
        }

        double haloDistance = Math.Abs(levelSample.Normal - level.Offset) - level.OreHalf;
        if (levelSample.Footprint < 1.12 && haloDistance < 1.5 && grain > 0.76)
        {
            return new StratiformCinnabarSample(
                StratiformCinnabarZone.QuartzCarbonate,
                levelIndex,
                grain,
                false,
                true);
        }

        return new StratiformCinnabarSample(
            StratiformCinnabarZone.None,
            levelIndex,
            grain,
            false,
            true);
    }

    /// <summary><c>sampleDiatreme</c>: upward-flaring wobbling breccia pipe.</summary>
    private bool SampleDiatreme(double x, double y, double z)
    {
        if (y < diatremeBottom || y > diatremeTop) return false;

        double t = Clamp((y - diatremeBottom) / (diatremeTop - diatremeBottom), 0.0, 1.0);
        double axisX = diatremeX + Math.Sin(t * 3.1 + seed) * 1.3 * t;
        double axisZ = diatremeZ + Math.Cos(t * 2.6 - seed * 0.3) * 1.1 * t;
        double radius = diatremeBaseRadius
            + (diatremeTopRadius - diatremeBaseRadius) * Math.Pow(t, 0.75);
        double u = (x - axisX) / radius;
        double v = (z - axisZ) / (radius * 0.82);
        double warp = 0.1 * Math.Sin(u * 4.0 + seed + y * 0.07) * Math.Cos(v * 3.2);
        return Math.Sqrt(u * u + v * v) + warp <= 1.0;
    }

    /// <summary><c>sampleLevel</c>: warped elliptical stratabound ore lens.</summary>
    private LevelSample SampleLevel(double x, double y, double z, in OreLevel level)
    {
        LocalCoordinates p = GetLocalCoordinates(x, y, z);
        double u = (p.Along - level.CenterAlong) / level.RadiusAlong;
        double v = (p.DownDip - level.CenterDip) / level.RadiusDip;
        double w = (p.Normal - level.Offset) / level.OreHalf;
        double edgeWarp = 0.11 * Math.Sin(u * 4.0 + level.Phase) * Math.Cos(v * 3.1)
            + 0.05 * Math.Sin((u - v) * 7.0 - level.Phase * 0.4);
        double foot = Math.Sqrt(u * u + v * v) + edgeWarp;
        double taper = Clamp(1.0 - foot * foot, 0.0, 1.0);
        bool inside = foot <= 1.02 && Math.Abs(w) <= Math.Max(0.25, Math.Sqrt(taper));
        bool host = Math.Abs(p.Normal - level.Offset) <= level.HostHalf;

        return new LevelSample(p.Along, p.Normal, p.DownDip, u, v, w, foot, inside, host);
    }

    /// <summary><c>sampleJoint</c>: tip-tapered bent joint plane inside an ore level.</summary>
    private static bool SampleJoint(in JointSet joint, in OreLevel level, in LevelSample sample)
    {
        if (!sample.Inside) return false;

        double a = sample.Along * Math.Cos(joint.Angle) - sample.DownDip * Math.Sin(joint.Angle);
        double b = sample.Along * Math.Sin(joint.Angle)
            + sample.DownDip * Math.Cos(joint.Angle)
            - joint.Offset;
        double tip = Math.Abs(a - level.CenterAlong) / joint.Length;
        double taper = Math.Sqrt(Clamp(1.0 - tip * tip, 0.0, 1.0));
        double bend = Math.Sin(a * 0.2 + joint.Phase) * 0.55;
        double dist = Math.Abs(b - bend);
        return tip <= 1.0 && dist <= Math.Max(0.08, joint.Width * taper);
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

    public readonly record struct LocalCoordinates(double Along, double Normal, double DownDip);

    private readonly record struct LevelSample(
        double Along,
        double Normal,
        double DownDip,
        double U,
        double V,
        double NormalRatio,
        double Footprint,
        bool Inside,
        bool Host);

    private readonly record struct OreLevel(
        double Offset,
        double HostHalf,
        double OreHalf,
        double CenterAlong,
        double CenterDip,
        double RadiusAlong,
        double RadiusDip,
        double Phase);

    private readonly record struct JointSet(
        int LevelIndex,
        double Angle,
        double Offset,
        double Length,
        double Width,
        double Phase);
}

internal sealed class StratiformCinnabarProceduralTemplate : IProceduralDepositTemplate
{
    private static readonly string[] RequiredSlots =
    {
        ProceduralMaterialSlots.Cinnabar,
        ProceduralMaterialSlots.Quartz,
        ProceduralMaterialSlots.Pyrite
    };

    public string Code => "stratiformCinnabar";
    public IReadOnlyList<string> RequiredMaterialSlots => RequiredSlots;
    public bool RequiresPlan => true;

    public int GetMaximumHorizontalReach(ProceduralDepositDefinition definition)
    {
        return definition.StratiformCinnabar.HorizontalRadius;
    }

    public object CreatePlan(in ProceduralDepositInstance instance, ProceduralDepositDefinition definition)
    {
        return StratiformCinnabarPlan.Create(instance, definition.StratiformCinnabar);
    }

    public bool Validate(ProceduralDepositDefinition definition, out string error)
    {
        StratiformCinnabarDefinition settings = definition.StratiformCinnabar;
        bool valid = settings.HorizontalRadius >= 16
            && settings.VerticalHalfHeight >= 8
            && settings.OreLevelCount >= 1
            && settings.JointSetsPerLevel >= 0
            && settings.LevelSpacing > 0.0
            && settings.StrikeDegMax >= settings.StrikeDegMin
            && settings.DipDegMax >= settings.DipDegMin
            && settings.HostHalfMin > 0.0
            && settings.HostHalfMax >= settings.HostHalfMin
            && settings.OreHalfMin > 0.0
            && settings.OreHalfMax >= settings.OreHalfMin
            && settings.OreHalfMax <= settings.HostHalfMax
            && settings.RadiusAlongMin > 0.0
            && settings.RadiusAlongMax >= settings.RadiusAlongMin
            && settings.RadiusDipMin > 0.0
            && settings.RadiusDipMax >= settings.RadiusDipMin
            && settings.JointLengthMin > 0.0
            && settings.JointLengthMax >= settings.JointLengthMin
            && settings.JointWidthMin > 0.0
            && settings.JointWidthMax >= settings.JointWidthMin
            && settings.DiatremeBottomMax >= settings.DiatremeBottomMin
            && settings.DiatremeTopMax >= settings.DiatremeTopMin
            && settings.DiatremeTopMin > settings.DiatremeBottomMax
            && settings.DiatremeBaseRadiusMin > 0.0
            && settings.DiatremeBaseRadiusMax >= settings.DiatremeBaseRadiusMin
            && settings.DiatremeTopRadiusMin > 0.0
            && settings.DiatremeTopRadiusMax >= settings.DiatremeTopRadiusMin;
        error = valid ? string.Empty : "invalid stratiform cinnabar settings";
        return valid;
    }

    public void Realize(
        ProceduralDepositWorldGenSystem system,
        in DepositCandidate candidate,
        IChunkColumnGenerateRequest request,
        int baseX,
        int baseZ)
    {
        system.RealizeStratiformCinnabarCandidate(candidate, request, baseX, baseZ);
    }
}

public sealed partial class ProceduralDepositWorldGenSystem
{
    internal void RealizeStratiformCinnabarCandidate(
        DepositCandidate candidate,
        IChunkColumnGenerateRequest request,
        int baseX,
        int baseZ)
    {
        CompiledProceduralDeposit compiled = candidate.Compiled;
        StratiformCinnabarDefinition settings = compiled.Definition.StratiformCinnabar;
        var plan = (StratiformCinnabarPlan)candidate.Plan!;
        ushort[] heightMap = request.Chunks[0].MapChunk.WorldGenTerrainHeightMap;
        int worldHeight = serverApi!.World.BlockAccessor.MapSizeY;
        int minimumY = GameMath.Clamp(candidate.Instance.CenterY - settings.VerticalHalfHeight, 1, worldHeight - 2);
        int maximumPrototypeY = GameMath.Clamp(candidate.Instance.CenterY + settings.VerticalHalfHeight, 1, worldHeight - 2);

        int[] zoneSlots = BuildStratiformCinnabarZoneSlots(compiled);

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
                    StratiformCinnabarSample sample = plan.Evaluate(worldX, y, worldZ, surfaceY);
                    if (sample.Zone == StratiformCinnabarZone.None) continue;

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
                    AdvancedGeology.Byproducts.ByproductSystem.RecordPlacement(request.Chunks[chunkY], index3d, placeBlockId, candidate.Instance.FeatureId, compiled);
                    data.SetFluid(index3d, 0);
                }
            }
        }
    }

    private static int[] BuildStratiformCinnabarZoneSlots(CompiledProceduralDeposit compiled)
    {
        var slots = new int[Enum.GetValues<StratiformCinnabarZone>().Length];
        Array.Fill(slots, -1);

        // Dense, impregnation and joint-fill cinnabar are all the same ungraded cinnabar block.
        int cinnabarSlot = compiled.GetSlotId(ProceduralMaterialSlots.Cinnabar);
        slots[(int)StratiformCinnabarZone.DenseCinnabar] = cinnabarSlot;
        slots[(int)StratiformCinnabarZone.CinnabarImpregnation] = cinnabarSlot;
        slots[(int)StratiformCinnabarZone.CinnabarJointFill] = cinnabarSlot;

        slots[(int)StratiformCinnabarZone.QuartzCarbonate] = compiled.GetSlotId(ProceduralMaterialSlots.Quartz);
        slots[(int)StratiformCinnabarZone.Pyrite] = compiled.GetSlotId(ProceduralMaterialSlots.Pyrite);
        return slots;
    }
}
