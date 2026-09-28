using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Vintagestory.API.Server;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace AdvancedGeology.WorldGen.ProceduralDeposits;

public enum BandedIronFormationZone
{
    None = 0,
    Magnetite,
    Hematite,
    Siderite,
    Pyrite,
    Chert,
    Jaspilite,
    Limonite,
    HighGradeHematite,
    Breccia
}

[JsonObject(MemberSerialization.OptIn)]
public sealed class BandedIronFormationDefinition
{
    [JsonProperty]
    public int HorizontalRadius { get; set; } = 48;

    [JsonProperty]
    public int VerticalHalfHeight { get; set; } = 35;

    [JsonProperty]
    public double StrikeMin { get; set; } = 8.0;

    [JsonProperty]
    public double StrikeMax { get; set; } = 32.0;

    [JsonProperty]
    public double DipMin { get; set; } = 3.0;

    [JsonProperty]
    public double DipMax { get; set; } = 10.0;

    [JsonProperty]
    public double DipDirectionMin { get; set; } = 72.0;

    [JsonProperty]
    public double DipDirectionMax { get; set; } = 108.0;

    [JsonProperty]
    public int BandCountMin { get; set; } = 7;

    [JsonProperty]
    public int BandCountMax { get; set; } = 11;

    [JsonProperty]
    public int EnrichmentCountMin { get; set; } = 2;

    [JsonProperty]
    public int EnrichmentCountMax { get; set; } = 4;

    [JsonProperty]
    public double ContactYMin { get; set; } = -1.0;

    [JsonProperty]
    public double ContactYMax { get; set; } = 4.0;

    [JsonProperty]
    public double FoldAmplitudeMin { get; set; } = 2.0;

    [JsonProperty]
    public double FoldAmplitudeMax { get; set; } = 4.5;

    [JsonProperty]
    public double WeatheringDepth { get; set; } = 8.0;
}

/// <summary>
/// Magnetite, hematite, siderite, pyrite, chert, jaspilite, limonite, and breccia form folded, dipping, pinch-and-swell iron-rich bands with localized high-grade enrichment.
/// </summary>
internal sealed class BandedIronFormationPlan
{
    private const ulong PlanSalt = 0x42414E4445444952UL;

    private readonly ulong featureId;
    private readonly int originX;
    private readonly int originY;
    private readonly int originZ;
    private readonly int horizontalRadius;
    private readonly int verticalHalfHeight;
    private readonly double seed;
    private readonly double strikeDeg;
    private readonly double dipDeg;
    private readonly double dipDirectionDeg;
    private readonly double contactY;
    private readonly double systemX;
    private readonly double systemZ;
    private readonly double foldAmplitude;
    private readonly double weatheringDepth;
    private readonly double cosDipDirection;
    private readonly double sinDipDirection;
    private readonly double tanDip;
    private readonly BifBand[] bands;
    private readonly BifEnrichment[] enrichmentZones;

    public int OriginX => originX;
    public int OriginY => originY;
    public int OriginZ => originZ;
    public double Seed => seed;
    public double StrikeDeg => strikeDeg;
    public double DipDeg => dipDeg;
    public int BandCount => bands.Length;
    public int EnrichmentCount => enrichmentZones.Length;

    private BandedIronFormationPlan(
        ulong featureId,
        in ProceduralDepositInstance instance,
        BandedIronFormationDefinition settings,
        double seed,
        double strikeDeg,
        double dipDeg,
        double dipDirectionDeg,
        double contactY,
        double systemX,
        double systemZ,
        double foldAmplitude,
        double cosDipDirection,
        double sinDipDirection,
        double tanDip,
        BifBand[] bands,
        BifEnrichment[] enrichmentZones)
    {
        this.featureId = featureId;
        originX = instance.CenterX;
        originY = instance.CenterY;
        originZ = instance.CenterZ;
        horizontalRadius = settings.HorizontalRadius;
        verticalHalfHeight = settings.VerticalHalfHeight;
        this.seed = seed;
        this.strikeDeg = strikeDeg;
        this.dipDeg = dipDeg;
        this.dipDirectionDeg = dipDirectionDeg;
        this.contactY = contactY;
        this.systemX = systemX;
        this.systemZ = systemZ;
        this.foldAmplitude = foldAmplitude;
        weatheringDepth = settings.WeatheringDepth;
        this.cosDipDirection = cosDipDirection;
        this.sinDipDirection = sinDipDirection;
        this.tanDip = tanDip;
        this.bands = bands;
        this.enrichmentZones = enrichmentZones;
    }

    public static BandedIronFormationPlan Create(
        in ProceduralDepositInstance instance,
        BandedIronFormationDefinition settings)
    {
        var random = new ProceduralDepositRandom(instance.FeatureId ^ PlanSalt);

        double strikeDeg = random.Range(settings.StrikeMin, settings.StrikeMax);
        double dipDeg = random.Range(settings.DipMin, settings.DipMax);
        double dipDirectionDeg = random.Range(settings.DipDirectionMin, settings.DipDirectionMax);
        int bandCount = random.NextInt(settings.BandCountMin, settings.BandCountMax);
        int enrichmentCount = random.NextInt(settings.EnrichmentCountMin, settings.EnrichmentCountMax);
        double contactY = random.Range(settings.ContactYMin, settings.ContactYMax);
        double systemX = random.Range(-4.0, 4.0);
        double systemZ = random.Range(-4.0, 4.0);
        double foldAmplitude = random.Range(settings.FoldAmplitudeMin, settings.FoldAmplitudeMax);
        double seed = random.Range(0.0, 100.0);

        double strikeRad = strikeDeg * Math.PI / 180.0;
        double cosStrike = Math.Cos(strikeRad);
        double sinStrike = Math.Sin(strikeRad);

        double dirRad = dipDirectionDeg * Math.PI / 180.0;
        double cosDir = Math.Cos(dirRad);
        double sinDir = Math.Sin(dirRad);
        double tanD = Math.Tan(dipDeg * Math.PI / 180.0);

        (double x, double z) AlongToWorld(double along, double across) => (
            systemX + along * cosStrike + across * sinStrike,
            systemZ - along * sinStrike + across * cosStrike);

        var bands = new BifBand[bandCount];
        double bandSpacing = random.Range(2.8, 4.3);
        double bandStart = -(bandCount - 1) * bandSpacing * 0.5;
        for (int i = 0; i < bandCount; i++)
        {
            (double cx, double cz) = AlongToWorld(random.Range(-8.0, 8.0), random.Range(-5.0, 5.0));
            BifFacies facies = (i % 4 == 0 || i % 4 == 1)
                ? BifFacies.Oxide
                : ((i % 4 == 2) ? BifFacies.Silicate : BifFacies.Chert);

            double centerStratY = contactY + bandStart + i * bandSpacing + random.Range(-0.45, 0.45);
            double radiusAlong = random.Range(30.0, 44.0);
            double radiusAcross = random.Range(28.0, 42.0);
            double thickness = random.Range(0.8, 1.7);
            double ironPotential = facies == BifFacies.Oxide
                ? random.Range(0.62, 0.92)
                : random.Range(0.12, 0.42);

            double bandStrikeDeg = strikeDeg + random.Range(-3.0, 3.0);
            double phase = seed + i * 17.0;

            bands[i] = BifBand.Create(
                cx, cz, centerStratY,
                radiusAlong, radiusAcross,
                thickness, ironPotential,
                facies, bandStrikeDeg, phase);
        }

        var enrichmentZones = new BifEnrichment[enrichmentCount];
        for (int i = 0; i < enrichmentCount; i++)
        {
            (double cx, double cz) = AlongToWorld(random.Range(-27.0, 27.0), random.Range(-6.0, 6.0));
            double centerStratY = contactY + random.Range(-4.0, 5.0);
            double radiusAlong = random.Range(10.0, 19.0);
            double radiusAcross = random.Range(5.0, 10.0);
            double radiusVertical = random.Range(2.5, 5.0);
            double zoneStrikeDeg = strikeDeg + random.Range(-12.0, 12.0);
            double phase = seed + 91.0 + i * 19.0;

            enrichmentZones[i] = BifEnrichment.Create(
                cx, cz, centerStratY,
                radiusAlong, radiusAcross, radiusVertical,
                zoneStrikeDeg, phase);
        }

        return new BandedIronFormationPlan(
            instance.FeatureId,
            instance,
            settings,
            seed,
            strikeDeg,
            dipDeg,
            dipDirectionDeg,
            contactY,
            systemX,
            systemZ,
            foldAmplitude,
            cosDir,
            sinDir,
            tanD,
            bands,
            enrichmentZones);
    }

    public BandedIronFormationZone EvaluateZone(int worldX, int worldY, int worldZ, int surfaceY = int.MaxValue)
    {
        return EvaluateZone(worldX, worldY, worldZ, surfaceY, out _);
    }

    public BandedIronFormationZone EvaluateZone(int worldX, int worldY, int worldZ, int surfaceY, out int gradeIndex)
    {
        gradeIndex = 0;

        if (worldY > surfaceY) return BandedIronFormationZone.None;

        double x = worldX - originX;
        double y = worldY - originY;
        double z = worldZ - originZ;

        // Bounding check
        if (Math.Abs(y) > verticalHalfHeight) return BandedIronFormationZone.None;
        if (x * x + z * z > (horizontalRadius + 4.0) * (horizontalRadius + 4.0)) return BandedIronFormationZone.None;

        int depth = surfaceY - worldY + 1;
        double stratY = GetStratigraphicY(x, y, z);

        // 1. Check enrichment zones
        EnrichmentSample? bestEnrichment = null;
        for (int i = 0; i < enrichmentZones.Length; i++)
        {
            EnrichmentSample sample = enrichmentZones[i].Sample(x, y, z, stratY);
            if (sample.Inside && (!bestEnrichment.HasValue || sample.Radial < bestEnrichment.Value.Radial))
            {
                bestEnrichment = sample;
            }
        }

        if (bestEnrichment.HasValue)
        {
            EnrichmentSample enr = bestEnrichment.Value;
            if (depth <= weatheringDepth && enr.Lateral < 0.72)
            {
                gradeIndex = 2; // rich limonite
                return BandedIronFormationZone.Limonite;
            }
            if (enr.Lateral < 0.56)
            {
                gradeIndex = 3; // bountiful hematite
                return BandedIronFormationZone.HighGradeHematite;
            }
            if (depth <= weatheringDepth && enr.VerticalNorm > 0.25)
            {
                gradeIndex = 2; // rich limonite
                return BandedIronFormationZone.Limonite;
            }

            gradeIndex = 2; // rich hematite
            return BandedIronFormationZone.Hematite;
        }

        // 2. Check primary bands
        BandSample? bestBand = null;
        for (int i = 0; i < bands.Length; i++)
        {
            BandSample sample = bands[i].Sample(x, y, z, stratY);
            if (sample.Inside && (!bestBand.HasValue || sample.IronPotential > bestBand.Value.IronPotential))
            {
                bestBand = sample;
            }
        }

        double grain = Hash3D(x * 1.8 + seed, y * 1.8, z * 1.8);
        if (bestBand.HasValue)
        {
            BandSample band = bestBand.Value;
            if (band.Facies == BifFacies.Chert)
            {
                if (band.IronPotential > 0.34 && grain > 0.58)
                {
                    gradeIndex = 0; // poor hematite (jaspilite)
                    return BandedIronFormationZone.Jaspilite;
                }
                return BandedIronFormationZone.Chert;
            }

            if (band.Facies == BifFacies.Silicate)
            {
                if (grain > 0.48) return BandedIronFormationZone.None;
                if (grain > 0.18)
                {
                    gradeIndex = band.IronPotential >= 0.35 ? 1 : 0;
                    return BandedIronFormationZone.Siderite;
                }
                return BandedIronFormationZone.Chert;
            }

            // Oxide facies
            if (depth <= weatheringDepth && band.IronPotential > 0.70 && grain > 0.60)
            {
                gradeIndex = 2; // rich limonite
                return BandedIronFormationZone.Limonite;
            }

            if (band.IronPotential > 0.80)
            {
                if (grain > 0.55)
                {
                    gradeIndex = band.IronPotential >= 0.90 ? 3 : (band.IronPotential >= 0.84 ? 2 : 1);
                    return BandedIronFormationZone.Magnetite;
                }
                if (grain > 0.38)
                {
                    gradeIndex = band.IronPotential >= 0.90 ? 3 : (band.IronPotential >= 0.84 ? 2 : 1);
                    return BandedIronFormationZone.Hematite;
                }
                if (grain > 0.19)
                {
                    return BandedIronFormationZone.Pyrite;
                }

                gradeIndex = 0; // poor hematite (jaspilite)
                return BandedIronFormationZone.Jaspilite;
            }

            if (grain > 0.63)
            {
                gradeIndex = band.IronPotential >= 0.70 ? 2 : (band.IronPotential >= 0.50 ? 1 : 0);
                return BandedIronFormationZone.Siderite;
            }
            if (grain > 0.33)
            {
                return BandedIronFormationZone.None;
            }

            return BandedIronFormationZone.Chert;
        }

        // 3. Check band halo
        bool inBandHalo = false;
        double strongestChannel = double.NegativeInfinity;
        for (int i = 0; i < bands.Length; i++)
        {
            BandSample sample = bands[i].Sample(x, y, z, stratY);
            if (sample.Halo)
            {
                inBandHalo = true;
                strongestChannel = Math.Max(strongestChannel, sample.ChannelField);
            }
        }

        if (inBandHalo)
        {
            if (strongestChannel > 0.55 && grain > 0.75)
            {
                return BandedIronFormationZone.Breccia;
            }
            if (grain > 0.62)
            {
                gradeIndex = 0; // poor hematite (jaspilite)
                return BandedIronFormationZone.Jaspilite;
            }
            return BandedIronFormationZone.Chert;
        }

        return BandedIronFormationZone.None;
    }

    private double GetBeddingOffset(double x, double z)
    {
        double projection = (x - systemX) * cosDipDirection + (z - systemZ) * sinDipDirection;
        double along = (x - systemX) * sinDipDirection - (z - systemZ) * cosDipDirection;
        double regional = tanDip * projection;
        double fold = Math.Sin(along * 0.075 + seed * 0.4) * foldAmplitude
            + Math.Sin(projection * 0.14 + seed) * foldAmplitude * 0.42;
        return regional + fold;
    }

    private double GetStratigraphicY(double x, double y, double z)
    {
        return y - GetBeddingOffset(x, z);
    }

    private static double Hash3D(double x, double y, double z)
    {
        double n = Math.Sin(x * 12.9898 + y * 78.233 + z * 37.719) * 43758.5453;
        return n - Math.Floor(n);
    }

    private enum BifFacies
    {
        Oxide,
        Silicate,
        Chert
    }

    private readonly record struct BandSample(
        double Distance,
        double Footprint,
        double IronPotential,
        BifFacies Facies,
        double ChannelField,
        bool Inside,
        bool Halo);

    private readonly record struct EnrichmentSample(
        double Radial,
        bool Inside,
        bool Halo,
        double VerticalNorm,
        double Lateral);

    private readonly record struct BifBand(
        double CenterX,
        double CenterZ,
        double CenterStratY,
        double RadiusAlong,
        double RadiusAcross,
        double Thickness,
        double IronPotential,
        BifFacies Facies,
        double StrikeDeg,
        double Phase,
        double SinStrike,
        double CosStrike)
    {
        public static BifBand Create(
            double centerX,
            double centerZ,
            double centerStratY,
            double radiusAlong,
            double radiusAcross,
            double thickness,
            double ironPotential,
            BifFacies facies,
            double strikeDeg,
            double phase)
        {
            double strikeRad = strikeDeg * Math.PI / 180.0;
            return new BifBand(
                centerX,
                centerZ,
                centerStratY,
                radiusAlong,
                radiusAcross,
                thickness,
                ironPotential,
                facies,
                strikeDeg,
                phase,
                Math.Sin(strikeRad),
                Math.Cos(strikeRad));
        }

        public BandSample Sample(double x, double y, double z, double stratY)
        {
            double dx = x - CenterX;
            double dz = z - CenterZ;
            double along = dx * CosStrike - dz * SinStrike;
            double across = dx * SinStrike + dz * CosStrike;
            double u = along / RadiusAlong;
            double v = across / RadiusAcross;
            double footprint = Math.Sqrt(u * u + v * v)
                + Math.Sin(u * 3.2 + Phase) * Math.Cos(v * 2.7) * 0.13;
            double pinch = 0.78 + 0.22 * Math.Sin(along * 0.14 + Phase)
                + 0.08 * Math.Cos(across * 0.18 - Phase);
            double localCenter = CenterStratY + Math.Sin(along * 0.09 + Phase) * 0.30;
            double distance = Math.Abs(stratY - localCenter);
            double halfThickness = Math.Max(0.18, Thickness * 0.5 * pinch);
            double channelField = Math.Sin(along * 0.07 + Phase) * 0.60
                + Math.Cos(across * 0.11 - Phase * 0.5) * 0.25;

            double sampleIronPotential = Math.Clamp(IronPotential + channelField * 0.14, 0.0, 1.0);
            bool inside = footprint <= 1.05 && distance <= halfThickness;
            bool halo = footprint <= 1.15 && distance <= halfThickness + 0.85;

            return new BandSample(distance, footprint, sampleIronPotential, Facies, channelField, inside, halo);
        }
    }

    private readonly record struct BifEnrichment(
        double CenterX,
        double CenterZ,
        double CenterStratY,
        double RadiusAlong,
        double RadiusAcross,
        double RadiusVertical,
        double StrikeDeg,
        double Phase,
        double SinStrike,
        double CosStrike)
    {
        public static BifEnrichment Create(
            double centerX,
            double centerZ,
            double centerStratY,
            double radiusAlong,
            double radiusAcross,
            double radiusVertical,
            double strikeDeg,
            double phase)
        {
            double strikeRad = strikeDeg * Math.PI / 180.0;
            return new BifEnrichment(
                centerX,
                centerZ,
                centerStratY,
                radiusAlong,
                radiusAcross,
                radiusVertical,
                strikeDeg,
                phase,
                Math.Sin(strikeRad),
                Math.Cos(strikeRad));
        }

        public EnrichmentSample Sample(double x, double y, double z, double stratY)
        {
            double dx = x - CenterX;
            double dz = z - CenterZ;
            double along = dx * CosStrike - dz * SinStrike;
            double across = dx * SinStrike + dz * CosStrike;
            double u = along / RadiusAlong;
            double v = across / RadiusAcross;
            double w = (stratY - CenterStratY) / RadiusVertical;
            double edgeWarp = Math.Sin(u * 3.7 + Phase) * Math.Cos(v * 3.1) * 0.14
                + Math.Sin(u * 7.0 - v * 4.4 + Phase * 0.5) * 0.06;
            double radial = Math.Sqrt(u * u + v * v + w * w) + edgeWarp;
            bool inside = radial <= 1.0;
            bool halo = radial <= 1.20;
            double lateral = Math.Sqrt(u * u + v * v);

            return new EnrichmentSample(radial, inside, halo, w, lateral);
        }
    }
}

internal sealed class BandedIronFormationProceduralTemplate : IProceduralDepositTemplate
{
    private static readonly string[] RequiredSlots =
    {
        ProceduralMaterialSlots.Magnetite,
        ProceduralMaterialSlots.Hematite,
        ProceduralMaterialSlots.Siderite,
        ProceduralMaterialSlots.Pyrite,
        ProceduralMaterialSlots.Chert,
        ProceduralMaterialSlots.Limonite,
        ProceduralMaterialSlots.Primary,
        ProceduralMaterialSlots.Alteration,
        ProceduralMaterialSlots.Breccia
    };

    public string Code => "bandedIronFormation";
    public IReadOnlyList<string> RequiredMaterialSlots => RequiredSlots;
    public bool RequiresPlan => true;

    public int GetMaximumHorizontalReach(ProceduralDepositDefinition definition)
    {
        return definition.BandedIron.HorizontalRadius;
    }

    public object? CreatePlan(in ProceduralDepositInstance instance, ProceduralDepositDefinition definition)
    {
        return BandedIronFormationPlan.Create(instance, definition.BandedIron);
    }

    public bool Validate(ProceduralDepositDefinition definition, out string error)
    {
        BandedIronFormationDefinition settings = definition.BandedIron;
        bool valid = settings.HorizontalRadius >= 16
            && settings.VerticalHalfHeight >= 8
            && settings.BandCountMin >= 1
            && settings.BandCountMax >= settings.BandCountMin
            && settings.EnrichmentCountMin >= 0
            && settings.EnrichmentCountMax >= settings.EnrichmentCountMin;

        error = valid ? string.Empty : "Banded iron formation deposit geometry settings out of valid bounds";
        return valid;
    }

    public void Realize(
        ProceduralDepositWorldGenSystem system,
        in DepositCandidate candidate,
        IChunkColumnGenerateRequest request,
        int baseX,
        int baseZ)
    {
        system.RealizeBandedIronFormationCandidate(candidate, request, baseX, baseZ);
    }
}

public sealed partial class ProceduralDepositWorldGenSystem
{
    internal void RealizeBandedIronFormationCandidate(
        DepositCandidate candidate,
        IChunkColumnGenerateRequest request,
        int baseX,
        int baseZ)
    {
        CompiledProceduralDeposit compiled = candidate.Compiled;
        BandedIronFormationDefinition settings = compiled.Definition.BandedIron;
        BandedIronFormationPlan plan = (BandedIronFormationPlan)candidate.Plan!;
        ushort[] heightMap = request.Chunks[0].MapChunk.WorldGenTerrainHeightMap;
        int worldHeight = serverApi!.World.BlockAccessor.MapSizeY;
        int minimumY = GameMath.Clamp(candidate.Instance.CenterY - settings.VerticalHalfHeight, 1, worldHeight - 2);
        int maximumPrototypeY = GameMath.Clamp(candidate.Instance.CenterY + settings.VerticalHalfHeight, 1, worldHeight - 2);

        int[] zoneSlots = BuildBandedIronFormationZoneSlots(compiled);

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
                    BandedIronFormationZone zone = plan.EvaluateZone(worldX, y, worldZ, surfaceY, out int gradeIndex);
                    if (zone == BandedIronFormationZone.None) continue;

                    int chunkY = y / ChunkSize;
                    if ((uint)chunkY >= (uint)request.Chunks.Length) continue;
                    int localY = y % ChunkSize;
                    int index3d = ((localY * ChunkSize) + localZ) * ChunkSize + localX;
                    IChunkBlocks data = request.Chunks[chunkY].Data;
                    int hostBlockId = data.GetBlockIdUnsafe(index3d);

                    int targetSlot = zoneSlots[(int)zone];
                    if (targetSlot < 0) continue;

                    if (!CanReplaceWithProceduralRock(compiled, hostBlockId))
                    {
                        continue;
                    }

                    int placeBlockId = compiled.ResolveBlock(targetSlot, gradeIndex, hostBlockId);
                    if (placeBlockId == 0 || placeBlockId == hostBlockId) continue;

                    data.SetBlockUnsafe(index3d, placeBlockId);
                    AdvancedGeology.Byproducts.ByproductSystem.RecordPlacement(request.Chunks[chunkY], index3d, placeBlockId, candidate.Instance.FeatureId, compiled);
                }
            }
        }
    }

    private static int[] BuildBandedIronFormationZoneSlots(CompiledProceduralDeposit compiled)
    {
        var slots = new int[Enum.GetValues<BandedIronFormationZone>().Length];
        Array.Fill(slots, -1);
        slots[(int)BandedIronFormationZone.Magnetite] = compiled.GetSlotId(ProceduralMaterialSlots.Magnetite);
        slots[(int)BandedIronFormationZone.Hematite] = compiled.GetSlotId(ProceduralMaterialSlots.Hematite);
        slots[(int)BandedIronFormationZone.Siderite] = compiled.GetSlotId(ProceduralMaterialSlots.Siderite);
        slots[(int)BandedIronFormationZone.Pyrite] = compiled.GetSlotId(ProceduralMaterialSlots.Pyrite);
        slots[(int)BandedIronFormationZone.Chert] = compiled.GetSlotId(ProceduralMaterialSlots.Chert);
        slots[(int)BandedIronFormationZone.Limonite] = compiled.GetSlotId(ProceduralMaterialSlots.Limonite);
        slots[(int)BandedIronFormationZone.Breccia] = compiled.GetSlotId(ProceduralMaterialSlots.Breccia);

        int highGradeHematiteSlot = compiled.GetSlotId(ProceduralMaterialSlots.Primary);
        if (highGradeHematiteSlot < 0) highGradeHematiteSlot = compiled.GetSlotId(ProceduralMaterialSlots.Enriched);
        slots[(int)BandedIronFormationZone.HighGradeHematite] = highGradeHematiteSlot;

        int jaspiliteSlot = compiled.GetSlotId(ProceduralMaterialSlots.Alteration);
        if (jaspiliteSlot < 0) jaspiliteSlot = compiled.GetSlotId(ProceduralMaterialSlots.Gangue);
        slots[(int)BandedIronFormationZone.Jaspilite] = jaspiliteSlot;

        return slots;
    }
}
