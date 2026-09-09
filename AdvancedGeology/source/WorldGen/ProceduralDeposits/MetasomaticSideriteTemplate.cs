using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Vintagestory.API.Server;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace AdvancedGeology.WorldGen.ProceduralDeposits;

public enum MetasomaticSideriteZone
{
    None = 0,
    Siderite,
    Chalcopyrite,
    Galena,
    Sphalerite,
    Pyrite,
    Hematite,
    Limonite,
    Quartz,
    Breccia,
    Gossan
}

[JsonObject(MemberSerialization.OptIn)]
public sealed class MetasomaticSideriteDefinition
{
    [JsonProperty]
    public int HorizontalRadius { get; set; } = 42;

    [JsonProperty]
    public int VerticalHalfHeight { get; set; } = 35;

    [JsonProperty]
    public double BedDipDegMin { get; set; } = 5.0;

    [JsonProperty]
    public double BedDipDegMax { get; set; } = 13.0;

    [JsonProperty]
    public double BedDirectionDegMin { get; set; } = 72.0;

    [JsonProperty]
    public double BedDirectionDegMax { get; set; } = 108.0;

    [JsonProperty]
    public int BodyCountMin { get; set; } = 2;

    [JsonProperty]
    public int BodyCountMax { get; set; } = 4;

    [JsonProperty]
    public int VeinCountMin { get; set; } = 3;

    [JsonProperty]
    public int VeinCountMax { get; set; } = 6;

    [JsonProperty]
    public double ReactiveBedYMin { get; set; } = 1.0;

    [JsonProperty]
    public double ReactiveBedYMax { get; set; } = 5.0;

    [JsonProperty]
    public double WeatheringDepth { get; set; } = 10.0;
}

public readonly record struct MetasomaticSideriteSample(
    bool InsideBody,
    bool InsideVein,
    bool InHalo,
    double BestBodyRadial,
    double BestVeinScore,
    double StratigraphicY,
    double LocalY);

/// <summary>
/// Siderite, copper-lead-zinc sulfides, pyrite, quartz, breccia, hematite, limonite, and gossan form folded carbonate-replacement mantos linked to steep feeder veins.
/// </summary>
internal sealed class MetasomaticSideritePlan
{
    private const ulong PlanSalt = 0x4D45544153494452UL;

    private readonly ulong featureId;
    private readonly int originX;
    private readonly int originY;
    private readonly int originZ;
    private readonly double seed;
    private readonly double bedDipDeg;
    private readonly double bedDirectionDeg;
    private readonly double systemX;
    private readonly double systemZ;
    private readonly double reactiveBedY;
    private readonly double weatheringDepth;
    private readonly double cosBedDirection;
    private readonly double sinBedDirection;
    private readonly double tanDip;
    private readonly MetasomaticBody[] bodies;
    private readonly MetasomaticVein[] veins;

    public int OriginX => originX;
    public int OriginY => originY;
    public int OriginZ => originZ;
    public double Seed => seed;
    public double BedDipDeg => bedDipDeg;
    public double BedDirectionDeg => bedDirectionDeg;
    public double SystemX => systemX;
    public double SystemZ => systemZ;
    public double ReactiveBedY => reactiveBedY;
    public double WeatheringDepth => weatheringDepth;
    public int BodyCount => bodies.Length;
    public int VeinCount => veins.Length;

    private MetasomaticSideritePlan(
        ulong featureId,
        in ProceduralDepositInstance instance,
        double seed,
        double bedDipDeg,
        double bedDirectionDeg,
        double systemX,
        double systemZ,
        double reactiveBedY,
        double weatheringDepth,
        double cosBedDirection,
        double sinBedDirection,
        double tanDip,
        MetasomaticBody[] bodies,
        MetasomaticVein[] veins)
    {
        this.featureId = featureId;
        originX = instance.CenterX;
        originY = instance.CenterY;
        originZ = instance.CenterZ;
        this.seed = seed;
        this.bedDipDeg = bedDipDeg;
        this.bedDirectionDeg = bedDirectionDeg;
        this.systemX = systemX;
        this.systemZ = systemZ;
        this.reactiveBedY = reactiveBedY;
        this.weatheringDepth = weatheringDepth;
        this.cosBedDirection = cosBedDirection;
        this.sinBedDirection = sinBedDirection;
        this.tanDip = tanDip;
        this.bodies = bodies;
        this.veins = veins;
    }

    public static MetasomaticSideritePlan Create(
        in ProceduralDepositInstance instance,
        MetasomaticSideriteDefinition settings)
    {
        var random = new ProceduralDepositRandom(instance.FeatureId ^ PlanSalt);

        double bedDipDeg = random.Range(settings.BedDipDegMin, settings.BedDipDegMax);
        double bedDirectionDeg = random.Range(settings.BedDirectionDegMin, settings.BedDirectionDegMax);
        int bodyCount = random.NextInt(settings.BodyCountMin, settings.BodyCountMax);
        int veinCount = random.NextInt(settings.VeinCountMin, settings.VeinCountMax);
        double systemX = random.Range(-4.0, 4.0);
        double systemZ = random.Range(-4.0, 4.0);
        double reactiveBedY = random.Range(settings.ReactiveBedYMin, settings.ReactiveBedYMax);
        double seed = random.Range(0.0, 100.0);

        double directionRad = bedDirectionDeg * Math.PI / 180.0;
        double cosDirection = Math.Cos(directionRad);
        double sinDirection = Math.Sin(directionRad);

        var bodies = new MetasomaticBody[bodyCount];
        for (int i = 0; i < bodyCount; i++)
        {
            double side = i % 2 == 0 ? -1.0 : 1.0;
            double along = i == 0 ? random.Range(-4.0, 4.0) : side * random.Range(9.0, 26.0);
            double across = random.Range(-7.0, 7.0);

            double cx = systemX + along * cosDirection + across * sinDirection;
            double cz = systemZ - along * sinDirection + across * cosDirection;

            bodies[i] = MetasomaticBody.Create(
                cx,
                cz,
                reactiveBedY + random.Range(-1.4, 1.4),
                i == 0 ? random.Range(17.0, 24.0) : random.Range(9.0, 17.0),
                i == 0 ? random.Range(8.0, 13.0) : random.Range(5.0, 9.0),
                i == 0 ? random.Range(3.8, 5.6) : random.Range(2.5, 4.5),
                bedDirectionDeg + random.Range(-10.0, 10.0),
                seed + i * 17.0,
                random.Range(0.70, 0.98));
        }

        var veins = new MetasomaticVein[veinCount];
        for (int i = 0; i < veinCount; i++)
        {
            MetasomaticBody body = bodies[i % bodies.Length];
            double angle = random.Range(0.0, Math.PI * 2.0);
            double centerX = body.CenterX + Math.Cos(angle) * random.Range(-2.5, 2.5);
            double centerZ = body.CenterZ + Math.Sin(angle) * random.Range(-2.5, 2.5);

            veins[i] = MetasomaticVein.Create(
                centerX,
                centerZ,
                body.CenterStratY - random.Range(5.0, 11.0),
                bedDirectionDeg + random.Range(-45.0, 45.0),
                random.Range(52.0, 78.0),
                random.Range(10.0, 19.0),
                random.Range(13.0, 23.0),
                random.Range(1.0, 2.3),
                seed + 61.0 + i * 13.0,
                random.Range(-7.0, 7.0),
                random.Range(2.0, 4.5),
                random.Range(-1.2, 1.2),
                seed + 31.0 + i * 6.0,
                random.Range(0.9, 1.8));
        }

        return new MetasomaticSideritePlan(
            instance.FeatureId,
            instance,
            seed,
            bedDipDeg,
            bedDirectionDeg,
            systemX,
            systemZ,
            reactiveBedY,
            settings.WeatheringDepth,
            cosDirection,
            sinDirection,
            Math.Tan(bedDipDeg * Math.PI / 180.0),
            bodies,
            veins);
    }

    private static double Hash3D(double x, double y, double z)
    {
        double n = Math.Sin(x * 12.9898 + y * 78.233 + z * 37.719) * 43758.5453;
        return n - Math.Floor(n);
    }

    public double GetBeddingOffset(double x, double z)
    {
        double across = (x - systemX) * cosBedDirection + (z - systemZ) * sinBedDirection;
        double along = (x - systemX) * sinBedDirection - (z - systemZ) * cosBedDirection;
        double warp = Math.Sin(x * 0.055 + seed) * 1.2 + Math.Cos(z * 0.065 - seed * 0.5) * 0.9;
        double fold = Math.Sin(along * 0.065 + seed * 0.4) * 1.4;
        return tanDip * across + warp + fold;
    }

    public double GetStratigraphicY(double x, double y, double z)
    {
        return y - GetBeddingOffset(x, z);
    }

    public double GetHostBoundaryY(double x, double z)
    {
        return reactiveBedY + GetBeddingOffset(x, z);
    }

    public MetasomaticSideriteZone EvaluateZone(int worldX, int worldY, int worldZ, int surfaceY)
    {
        if (worldY > surfaceY) return MetasomaticSideriteZone.None;

        double x = worldX - originX;
        double y = worldY - originY;
        double z = worldZ - originZ;

        if (y < -35.0) return MetasomaticSideriteZone.None;

        int depth = surfaceY - worldY;
        double stratY = GetStratigraphicY(x, y, z);
        double grain = Hash3D(x * 2.0 + seed, y * 2.0, z * 2.0);

        BodySample? bestBody = null;
        for (int i = 0; i < bodies.Length; i++)
        {
            BodySample sample = bodies[i].Sample(x, z, stratY);
            if (sample.Inside && (!bestBody.HasValue || sample.Radial < bestBody.Value.Radial))
            {
                bestBody = sample;
            }
        }

        VeinSample? bestVein = null;
        for (int i = 0; i < veins.Length; i++)
        {
            VeinSample sample = veins[i].Sample(x, y, z);
            if (!double.IsPositiveInfinity(sample.Score) && (!bestVein.HasValue || sample.Score < bestVein.Value.Score))
            {
                bestVein = sample;
            }
        }

        if (bestBody.HasValue)
        {
            if (depth <= weatheringDepth)
            {
                if (grain > 0.48) return MetasomaticSideriteZone.Limonite;
                if (grain > 0.28) return MetasomaticSideriteZone.Hematite;
            }
            if (bestBody.Value.IronPotential > 0.82)
            {
                if (grain > 0.55) return MetasomaticSideriteZone.Siderite;
                if (grain > 0.22) return MetasomaticSideriteZone.Siderite; // ankerite -> siderite
                return MetasomaticSideriteZone.None;
            }
            if (grain > 0.58) return MetasomaticSideriteZone.Siderite;
            if (grain > 0.34) return MetasomaticSideriteZone.None;
            return MetasomaticSideriteZone.None;
        }

        if (bestVein.HasValue && bestVein.Value.Inside)
        {
            double traceSulfideField = Math.Sin(x * 0.31 + seed)
                * Math.Cos(z * 0.29 - seed * 0.4)
                + Math.Sin(y * 0.23 + seed * 0.7);
            if (traceSulfideField > 1.15 && grain > 0.80)
            {
                if (grain > 0.995) return MetasomaticSideriteZone.Galena;
                if (grain > 0.990) return MetasomaticSideriteZone.Sphalerite;
                if (grain > 0.985) return MetasomaticSideriteZone.Chalcopyrite;
                return MetasomaticSideriteZone.Pyrite;
            }
            if (grain > 0.78) return MetasomaticSideriteZone.Siderite;
            if (grain > 0.48) return MetasomaticSideriteZone.Siderite; // ankerite -> siderite
            if (grain > 0.20) return MetasomaticSideriteZone.None;
            return MetasomaticSideriteZone.Quartz;
        }

        bool nearOre = bestVein.HasValue && bestVein.Value.Halo;
        if (!nearOre)
        {
            for (int i = 0; i < bodies.Length; i++)
            {
                if (bodies[i].Sample(x, z, stratY).Halo)
                {
                    nearOre = true;
                    break;
                }
            }
        }

        if (nearOre)
        {
            double alterationField = Math.Sin(x * 0.22 + seed)
                + Math.Cos(z * 0.19 - seed * 0.5);
            if (grain > 0.88) return MetasomaticSideriteZone.Breccia;
            if (depth <= weatheringDepth && grain > 0.78) return MetasomaticSideriteZone.Hematite;
            if (alterationField > 0.42) return MetasomaticSideriteZone.Siderite; // ankerite -> siderite
            if (alterationField < -0.35) return MetasomaticSideriteZone.None;
            return MetasomaticSideriteZone.Quartz;
        }

        return MetasomaticSideriteZone.None;
    }

    public MetasomaticSideriteSample SampleDetails(int worldX, int worldY, int worldZ, int surfaceY)
    {
        double x = worldX - originX;
        double y = worldY - originY;
        double z = worldZ - originZ;
        double stratY = GetStratigraphicY(x, y, z);

        double bestRadial = double.PositiveInfinity;
        bool inBody = false;
        bool inHalo = false;
        for (int i = 0; i < bodies.Length; i++)
        {
            BodySample sample = bodies[i].Sample(x, z, stratY);
            if (sample.Inside && sample.Radial < bestRadial)
            {
                bestRadial = sample.Radial;
                inBody = true;
            }
            if (sample.Halo) inHalo = true;
        }

        double bestScore = double.PositiveInfinity;
        bool inVein = false;
        for (int i = 0; i < veins.Length; i++)
        {
            VeinSample sample = veins[i].Sample(x, y, z);
            if (!double.IsPositiveInfinity(sample.Score) && sample.Score < bestScore)
            {
                bestScore = sample.Score;
            }
            if (sample.Inside) inVein = true;
            if (sample.Halo) inHalo = true;
        }

        return new MetasomaticSideriteSample(
            inBody,
            inVein,
            inHalo,
            bestRadial,
            bestScore,
            stratY,
            y);
    }

    private readonly record struct MetasomaticBody(
        double CenterX,
        double CenterZ,
        double CenterStratY,
        double RadiusAlong,
        double RadiusAcross,
        double RadiusVertical,
        double SinStrike,
        double CosStrike,
        double Phase,
        double IronPotential)
    {
        public static MetasomaticBody Create(
            double centerX,
            double centerZ,
            double centerStratY,
            double radiusAlong,
            double radiusAcross,
            double radiusVertical,
            double strikeDeg,
            double phase,
            double ironPotential)
        {
            double strikeRad = strikeDeg * Math.PI / 180.0;
            return new MetasomaticBody(
                centerX,
                centerZ,
                centerStratY,
                radiusAlong,
                radiusAcross,
                radiusVertical,
                Math.Sin(strikeRad),
                Math.Cos(strikeRad),
                phase,
                ironPotential);
        }

        public BodySample Sample(double x, double z, double stratY)
        {
            double dx = x - CenterX;
            double dz = z - CenterZ;
            double along = dx * CosStrike - dz * SinStrike;
            double across = dx * SinStrike + dz * CosStrike;
            double u = along / RadiusAlong;
            double v = across / RadiusAcross;
            double w = (stratY - CenterStratY) / RadiusVertical;
            double edgeWarp = Math.Sin(u * 3.6 + Phase) * Math.Cos(v * 3.1) * 0.15
                + Math.Sin(u * 7.0 - v * 4.4 + Phase * 0.5) * 0.07;
            double radial = Math.Sqrt(u * u + v * v + w * w) + edgeWarp;
            bool inside = radial <= 1.03;
            bool halo = radial <= 1.20;
            return new BodySample(radial, inside, halo, IronPotential);
        }
    }

    private readonly record struct BodySample(
        double Radial,
        bool Inside,
        bool Halo,
        double IronPotential);

    private readonly record struct MetasomaticVein(
        double SinStrike,
        double CosStrike,
        double SinDip,
        double CosDip,
        double Offset,
        double CenterAlong,
        double CenterY,
        double LengthHalf,
        double HeightHalf,
        double Thickness,
        double Phase,
        double RelayCenter,
        double RelayWidth,
        double RelayShift,
        double PinchPhase,
        double BendAmp)
    {
        public static MetasomaticVein Create(
            double centerX,
            double centerZ,
            double centerY,
            double strikeDeg,
            double dipDeg,
            double lengthHalf,
            double heightHalf,
            double thickness,
            double phase,
            double relayCenter,
            double relayWidth,
            double relayShift,
            double pinchPhase,
            double bendAmp)
        {
            double strikeRad = strikeDeg * Math.PI / 180.0;
            double dipRad = dipDeg * Math.PI / 180.0;
            double along = centerX * Math.Cos(strikeRad) - centerZ * Math.Sin(strikeRad);
            double across = centerX * Math.Sin(strikeRad) + centerZ * Math.Cos(strikeRad);
            return new MetasomaticVein(
                Math.Sin(strikeRad),
                Math.Cos(strikeRad),
                Math.Sin(dipRad),
                Math.Cos(dipRad),
                across,
                along,
                centerY,
                lengthHalf,
                heightHalf,
                thickness,
                phase,
                relayCenter,
                relayWidth,
                relayShift,
                pinchPhase,
                bendAmp);
        }

        public VeinSample Sample(double x, double y, double z)
        {
            double along = x * CosStrike - z * SinStrike;
            double across = x * SinStrike + z * CosStrike - Offset;
            double bend = Math.Sin(along * 0.12 + y * 0.07 + Phase) * BendAmp
                + Math.Cos(along * 0.30 + Phase * 1.3) * BendAmp * 0.28;
            double relay = RelayShift * Math.Tanh((along - RelayCenter) / RelayWidth);
            double signedDistance = across * SinDip + (y - CenterY) * CosDip - bend - relay;
            double u = (along - CenterAlong) / LengthHalf;
            double v = (y - CenterY) / HeightHalf;
            double edgeWarp = Math.Sin(u * 3.7 + Phase) * Math.Cos(v * 3.0) * 0.14;
            double footprint = Math.Sqrt(u * u + v * v) + edgeWarp;
            double taper = Math.Clamp(1.0 - footprint * footprint, 0.0, 1.0);
            double pinch = 0.80 + 0.20 * Math.Sin(along * 0.18 + PinchPhase);
            double halfThickness = Math.Max(0.10, Thickness * 0.5 * pinch * Math.Sqrt(taper));
            double distance = Math.Abs(signedDistance);
            double score = footprint <= 1.18 ? distance / halfThickness : double.PositiveInfinity;
            bool inside = footprint <= 1.04 && distance <= halfThickness;
            bool halo = footprint <= 1.14 && distance <= halfThickness + 2.8 * taper;
            return new VeinSample(score, inside, halo);
        }
    }

    private readonly record struct VeinSample(
        double Score,
        bool Inside,
        bool Halo);
}

internal sealed class MetasomaticSideriteProceduralTemplate : IProceduralDepositTemplate
{
    private static readonly string[] RequiredSlots =
    {
        ProceduralMaterialSlots.Siderite,
        ProceduralMaterialSlots.Pyrite,
        ProceduralMaterialSlots.Quartz,
        ProceduralMaterialSlots.Breccia
    };

    public string Code => "metasomaticSiderite";
    public IReadOnlyList<string> RequiredMaterialSlots => RequiredSlots;
    public bool RequiresPlan => true;

    public int GetMaximumHorizontalReach(ProceduralDepositDefinition definition)
    {
        return definition.MetasomaticSiderite.HorizontalRadius;
    }

    public object CreatePlan(in ProceduralDepositInstance instance, ProceduralDepositDefinition definition)
    {
        return MetasomaticSideritePlan.Create(instance, definition.MetasomaticSiderite);
    }

    public bool Validate(ProceduralDepositDefinition definition, out string error)
    {
        MetasomaticSideriteDefinition settings = definition.MetasomaticSiderite;
        bool valid = settings.HorizontalRadius >= 16
            && settings.VerticalHalfHeight >= 8
            && settings.BodyCountMin >= 1
            && settings.BodyCountMax >= settings.BodyCountMin
            && settings.VeinCountMin >= 1
            && settings.VeinCountMax >= settings.VeinCountMin;
        error = valid ? string.Empty : "invalid metasomatic siderite settings";
        return valid;
    }

    public void Realize(
        ProceduralDepositWorldGenSystem system,
        in DepositCandidate candidate,
        IChunkColumnGenerateRequest request,
        int baseX,
        int baseZ)
    {
        system.RealizeMetasomaticSideriteCandidate(candidate, request, baseX, baseZ);
    }
}

public sealed partial class ProceduralDepositWorldGenSystem
{
    internal void RealizeMetasomaticSideriteCandidate(
        DepositCandidate candidate,
        IChunkColumnGenerateRequest request,
        int baseX,
        int baseZ)
    {
        CompiledProceduralDeposit compiled = candidate.Compiled;
        MetasomaticSideriteDefinition settings = compiled.Definition.MetasomaticSiderite;
        var plan = (MetasomaticSideritePlan)candidate.Plan!;
        ushort[] heightMap = request.Chunks[0].MapChunk.WorldGenTerrainHeightMap;
        int worldHeight = serverApi!.World.BlockAccessor.MapSizeY;
        int minimumY = GameMath.Clamp(candidate.Instance.CenterY - settings.VerticalHalfHeight, 1, worldHeight - 2);
        int maximumPrototypeY = GameMath.Clamp(candidate.Instance.CenterY + settings.VerticalHalfHeight, 1, worldHeight - 2);

        int[] zoneSlots = BuildMetasomaticSideriteZoneSlots(compiled);
        int gossanSlot = compiled.GetSlotId(ProceduralMaterialSlots.Gossan);
        int sideriteSlot = compiled.GetSlotId(ProceduralMaterialSlots.Siderite);

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
                    MetasomaticSideriteZone zone = plan.EvaluateZone(worldX, y, worldZ, surfaceY);
                    if (zone == MetasomaticSideriteZone.None) continue;

                    int chunkY = y / ChunkSize;
                    if ((uint)chunkY >= (uint)request.Chunks.Length) continue;
                    int localY = y % ChunkSize;
                    int index3d = ((localY * ChunkSize) + localZ) * ChunkSize + localX;
                    IChunkBlocks data = request.Chunks[chunkY].Data;
                    int hostBlockId = data.GetBlockIdUnsafe(index3d);

                    int depth = GetSurfaceDepth(surfaceY, y);
                    int targetSlot = -1;

                    if (compiled.Definition.Weathering.Enabled && depth <= 1)
                    {
                        targetSlot = gossanSlot >= 0 ? gossanSlot : zoneSlots[(int)zone];
                    }
                    else
                    {
                        targetSlot = zoneSlots[(int)zone];
                    }

                    if (targetSlot < 0) continue;

                    bool isSoil = targetSlot == gossanSlot;
                    if (isSoil)
                    {
                        if (!CanPlaceWeatheredSoil(compiled, hostBlockId)) continue;
                    }
                    else if (!CanReplaceWithProceduralRock(compiled, hostBlockId))
                    {
                        continue;
                    }

                    int grade = SelectMetasomaticSideriteGrade(
                        candidate.Instance.FeatureId,
                        plan.SampleDetails(worldX, y, worldZ, surfaceY),
                        worldX,
                        y,
                        worldZ);
                    int placeBlockId = isSoil
                        ? compiled.ResolveWeatheredBlock(targetSlot, grade, hostBlockId, y < surfaceY)
                        : compiled.ResolveBlock(targetSlot, grade, hostBlockId);
                    if (placeBlockId == 0) continue;

                    data.SetBlockUnsafe(index3d, placeBlockId);
                    data.SetFluid(index3d, 0);
                }
            }
        }
    }

    private static int[] BuildMetasomaticSideriteZoneSlots(CompiledProceduralDeposit compiled)
    {
        var slots = new int[Enum.GetValues<MetasomaticSideriteZone>().Length];
        Array.Fill(slots, -1);
        slots[(int)MetasomaticSideriteZone.Siderite] = compiled.GetSlotId(ProceduralMaterialSlots.Siderite);
        slots[(int)MetasomaticSideriteZone.Chalcopyrite] = compiled.GetSlotId(ProceduralMaterialSlots.Chalcopyrite);
        slots[(int)MetasomaticSideriteZone.Galena] = compiled.GetSlotId(ProceduralMaterialSlots.Galena);
        slots[(int)MetasomaticSideriteZone.Sphalerite] = compiled.GetSlotId(ProceduralMaterialSlots.Sphalerite);
        slots[(int)MetasomaticSideriteZone.Pyrite] = compiled.GetSlotId(ProceduralMaterialSlots.Pyrite);
        slots[(int)MetasomaticSideriteZone.Hematite] = compiled.GetSlotId(ProceduralMaterialSlots.Hematite);
        slots[(int)MetasomaticSideriteZone.Limonite] = compiled.GetSlotId(ProceduralMaterialSlots.Limonite);
        slots[(int)MetasomaticSideriteZone.Quartz] = compiled.GetSlotId(ProceduralMaterialSlots.Quartz);
        slots[(int)MetasomaticSideriteZone.Breccia] = compiled.GetSlotId(ProceduralMaterialSlots.Breccia);
        slots[(int)MetasomaticSideriteZone.Gossan] = compiled.GetSlotId(ProceduralMaterialSlots.Gossan);
        return slots;
    }
}
