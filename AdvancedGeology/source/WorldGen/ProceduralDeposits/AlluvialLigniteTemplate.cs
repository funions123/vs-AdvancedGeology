using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Vintagestory.API.Server;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace AdvancedGeology.WorldGen.ProceduralDeposits;

public enum AlluvialLigniteZone
{
    None = 0,
    Lignite,
    WoodyLignite,
    PyriticLignite,
    HighAshLignite,
    CarbonaceousClay,
    ChannelSand
}

[JsonObject(MemberSerialization.OptIn)]
public sealed class AlluvialLigniteDefinition
{
    [JsonProperty]
    public int HorizontalRadius { get; set; } = 36;

    [JsonProperty]
    public int VerticalHalfHeight { get; set; } = 30;

    [JsonProperty]
    public double TrendDegMin { get; set; } = 0.0;

    [JsonProperty]
    public double TrendDegMax { get; set; } = 180.0;

    [JsonProperty]
    public double BedDipDegMin { get; set; } = 0.3;

    [JsonProperty]
    public double BedDipDegMax { get; set; } = 2.0;

    [JsonProperty]
    public int UnitCountMin { get; set; } = 1;

    [JsonProperty]
    public int UnitCountMax { get; set; } = 3;

    [JsonProperty]
    public int LensesPerUnit { get; set; } = 3;

    [JsonProperty]
    public double MainLevelMin { get; set; } = -1.0;

    [JsonProperty]
    public double MainLevelMax { get; set; } = 1.5;

    [JsonProperty]
    public double LensRadiusAlongMin { get; set; } = 9.0;

    [JsonProperty]
    public double LensRadiusAlongMax { get; set; } = 10.0;

    [JsonProperty]
    public double LensRadiusAcrossMin { get; set; } = 8.0;

    [JsonProperty]
    public double LensRadiusAcrossMax { get; set; } = 9.0;

    [JsonProperty]
    public double LensHalfThicknessMin { get; set; } = 4.0;

    [JsonProperty]
    public double LensHalfThicknessMax { get; set; } = 4.5;

    [JsonProperty]
    public double AlongScaleOneUnit { get; set; } = 2.2;

    [JsonProperty]
    public double AlongScaleTwoUnits { get; set; } = 1.5;

    [JsonProperty]
    public double AlongScaleThreeUnits { get; set; } = 0.9;

    [JsonProperty]
    public double AcrossScaleOneUnit { get; set; } = 2.0;

    [JsonProperty]
    public double AcrossScaleTwoUnits { get; set; } = 1.55;

    [JsonProperty]
    public double AcrossScaleThreeUnits { get; set; } = 1.7;

    [JsonProperty]
    public double ThicknessScaleOneUnit { get; set; } = 1.94;

    [JsonProperty]
    public double ThicknessScaleTwoUnits { get; set; } = 1.82;

    [JsonProperty]
    public double ThicknessScaleThreeUnits { get; set; } = 1.97;

    [JsonProperty]
    public double CenterJitterOneUnit { get; set; } = 4.0;

    [JsonProperty]
    public double CenterJitterTwoUnits { get; set; } = 2.0;

    [JsonProperty]
    public double CenterJitterThreeUnits { get; set; } = 1.5;

    [JsonProperty]
    public int MinorBedCountMin { get; set; } = 0;

    [JsonProperty]
    public int MinorBedCountMax { get; set; } = 1;

    [JsonProperty]
    public double MinorBedLevelDropMin { get; set; } = 4.0;

    [JsonProperty]
    public double MinorBedLevelDropMax { get; set; } = 6.0;

    [JsonProperty]
    public double MinorBedRadiusMin { get; set; } = 10.0;

    [JsonProperty]
    public double MinorBedRadiusMax { get; set; } = 15.0;

    [JsonProperty]
    public double MinorBedHalfThicknessMin { get; set; } = 0.6;

    [JsonProperty]
    public double MinorBedHalfThicknessMax { get; set; } = 1.0;

    [JsonProperty]
    public double ChannelWidthMin { get; set; } = 4.0;

    [JsonProperty]
    public double ChannelWidthMax { get; set; } = 6.0;

    [JsonProperty]
    public double ChannelHalfThicknessMin { get; set; } = 2.5;

    [JsonProperty]
    public double ChannelHalfThicknessMax { get; set; } = 3.8;

    [JsonProperty]
    public double ChannelBendMin { get; set; } = -2.5;

    [JsonProperty]
    public double ChannelBendMax { get; set; } = 2.5;
}

/// <summary>
/// Result of classifying one voxel against an <see cref="AlluvialLignitePlan"/>.
/// </summary>
public readonly record struct AlluvialLigniteSample(
    AlluvialLigniteZone Zone,
    double Ratio = 0.0,
    bool Minor = false,
    bool InsideChannel = false,
    bool InsidePeat = false,
    bool HighAshPruned = false);

/// <summary>
/// Lignite, woody lignite, pyritic lignite, high-ash lignite, carbonaceous clay, and channel sand form stacked floodplain peat lenses cut by a sinuous erosional channel.
/// </summary>
internal sealed class AlluvialLignitePlan
{
    private const ulong PlanSalt = 0x414C4C55564C4732UL; // "ALLUVLG2"

    // Local model floor: y < -30 is below the modelled floodplain package (GRID_Y / 2).
    private const double BottomY = -30.0;

    // Geometry jitters that are intrinsic rather than tunable ranges.
    private const double UnitAcrossSpread = 5.0;
    private const double UnitYJitter = 0.45;
    private const double LensAcrossJitter = 3.0;
    private const double LensCenterYJitter = 0.55;
    private const double MinorBedAlongJitter = 2.0;
    private const double MinorBedAcrossSpread = 5.0;
    private const double ChannelHalfLengthMin = 7.0;
    private const double ChannelHalfLengthMax = 11.0;
    private const double ChannelAlongJitter = 2.0;
    private const double ChannelAcrossJitter = 3.0;
    private const double ChannelCenterYJitter = 1.0;
    private const double ChannelEndpointClamp = 22.0;

    // Peat lens shape test constants.
    private const double LensFootprintLimit = 1.05;
    private const double LensMinimumHalfThickness = 0.35;

    // Unit centre positions along the deposit trend, by unit count.
    private static readonly double[] OneUnitAlong = { 0.0 };
    private static readonly double[] TwoUnitAlong = { -18.0, 18.0 };
    private static readonly double[] ThreeUnitAlong = { -24.0, 0.0, 24.0 };

    private readonly ulong featureId;
    private readonly int originX;
    private readonly int originY;
    private readonly int originZ;

    private readonly double trendDeg;
    private readonly double bedDipDeg;
    private readonly double seed;
    private readonly int unitCount;
    private readonly double mainLevel;

    private readonly double cosTrend;
    private readonly double sinTrend;
    private readonly double tanBedDip;

    private readonly PeatLens[] peatLenses;
    private readonly MinorBed[] minorBeds;
    private readonly ErosionalChannel channel;

    public int OriginX => originX;
    public int OriginY => originY;
    public int OriginZ => originZ;
    public double TrendDeg => trendDeg;
    public double BedDipDeg => bedDipDeg;
    public double Seed => seed;
    public double MainLevel => mainLevel;
    public int UnitCount => unitCount;
    public int PeatLensCount => peatLenses.Length;
    public int MinorBedCount => minorBeds.Length;
    public int ChannelCount => 1;

    private AlluvialLignitePlan(
        ulong featureId,
        in ProceduralDepositInstance instance,
        double trendDeg,
        double bedDipDeg,
        double seed,
        int unitCount,
        double mainLevel,
        double cosTrend,
        double sinTrend,
        double tanBedDip,
        PeatLens[] peatLenses,
        MinorBed[] minorBeds,
        in ErosionalChannel channel)
    {
        this.featureId = featureId;
        originX = instance.CenterX;
        originY = instance.CenterY;
        originZ = instance.CenterZ;
        this.trendDeg = trendDeg;
        this.bedDipDeg = bedDipDeg;
        this.seed = seed;
        this.unitCount = unitCount;
        this.mainLevel = mainLevel;
        this.cosTrend = cosTrend;
        this.sinTrend = sinTrend;
        this.tanBedDip = tanBedDip;
        this.peatLenses = peatLenses;
        this.minorBeds = minorBeds;
        this.channel = channel;
    }

    public static AlluvialLignitePlan Create(
        in ProceduralDepositInstance instance,
        AlluvialLigniteDefinition settings)
    {
        var random = new ProceduralDepositRandom(instance.FeatureId ^ PlanSalt);

        double trendDeg = random.Range(settings.TrendDegMin, settings.TrendDegMax);
        double bedDipDeg = random.Range(settings.BedDipDegMin, settings.BedDipDegMax);
        double seed = random.Range(0.0, 100.0);
        int unitCount = random.NextInt(settings.UnitCountMin, settings.UnitCountMax);

        double[] unitAlong = unitCount == 1
            ? OneUnitAlong
            : (unitCount == 2 ? TwoUnitAlong : ThreeUnitAlong);
        double alongScale = unitCount == 1
            ? settings.AlongScaleOneUnit
            : (unitCount == 2 ? settings.AlongScaleTwoUnits : settings.AlongScaleThreeUnits);
        double acrossScale = unitCount == 1
            ? settings.AcrossScaleOneUnit
            : (unitCount == 2 ? settings.AcrossScaleTwoUnits : settings.AcrossScaleThreeUnits);
        double thicknessScale = unitCount == 1
            ? settings.ThicknessScaleOneUnit
            : (unitCount == 2 ? settings.ThicknessScaleTwoUnits : settings.ThicknessScaleThreeUnits);
        double centerJitter = unitCount == 1
            ? settings.CenterJitterOneUnit
            : (unitCount == 2 ? settings.CenterJitterTwoUnits : settings.CenterJitterThreeUnits);

        double trendRad = trendDeg * Math.PI / 180.0;
        double cosTrend = Math.Cos(trendRad);
        double sinTrend = Math.Sin(trendRad);
        double mainLevel = random.Range(settings.MainLevelMin, settings.MainLevelMax);

        int lensesPerUnit = settings.LensesPerUnit;
        var unitLocations = new UnitLocation[unitCount];
        var peatLenses = new PeatLens[unitCount * lensesPerUnit];
        int lensIndex = 0;

        for (int unit = 0; unit < unitCount; unit++)
        {
            double unitAcross = random.Range(-UnitAcrossSpread, UnitAcrossSpread);
            double unitY = mainLevel + random.Range(-UnitYJitter, UnitYJitter);
            unitLocations[unit] = new UnitLocation(unitAlong[unit], unitAcross, unitY);

            for (int i = 0; i < lensesPerUnit; i++)
            {
                double along = unitAlong[unit] + random.Range(-centerJitter, centerJitter);
                double across = unitAcross + random.Range(-LensAcrossJitter, LensAcrossJitter);
                double radiusAlong = random.Range(settings.LensRadiusAlongMin, settings.LensRadiusAlongMax) * alongScale;
                double radiusAcross = random.Range(settings.LensRadiusAcrossMin, settings.LensRadiusAcrossMax) * acrossScale;
                double centerY = unitY + random.Range(-LensCenterYJitter, LensCenterYJitter);
                double halfThickness = random.Range(settings.LensHalfThicknessMin, settings.LensHalfThicknessMax) * thicknessScale;

                peatLenses[lensIndex++] = new PeatLens(
                    along * cosTrend + across * sinTrend,
                    -along * sinTrend + across * cosTrend,
                    radiusAlong,
                    radiusAcross,
                    centerY,
                    halfThickness,
                    seed + unit * 53.0 + i * 23.0);
            }
        }

        int minorCount = random.NextInt(settings.MinorBedCountMin, settings.MinorBedCountMax);
        var minorBeds = new MinorBed[minorCount];
        for (int i = 0; i < minorCount; i++)
        {
            int unit = random.NextInt(0, unitCount - 1);
            double along = unitAlong[unit] + random.Range(-MinorBedAlongJitter, MinorBedAlongJitter);
            double across = random.Range(-MinorBedAcrossSpread, MinorBedAcrossSpread);
            double level = mainLevel - random.Range(settings.MinorBedLevelDropMin, settings.MinorBedLevelDropMax);
            double radius = random.Range(settings.MinorBedRadiusMin, settings.MinorBedRadiusMax);
            double halfThickness = random.Range(settings.MinorBedHalfThicknessMin, settings.MinorBedHalfThicknessMax);

            minorBeds[i] = new MinorBed(
                along * cosTrend + across * sinTrend,
                -along * sinTrend + across * cosTrend,
                level,
                radius,
                halfThickness,
                seed + 101.0 + i * 17.0);
        }

        double startAlong;
        double startAcross;
        double startY;
        double endAlong;
        double endAcross;
        double endY;

        if (unitCount == 1)
        {
            UnitLocation source = unitLocations[0];
            double halfLength = random.Range(ChannelHalfLengthMin, ChannelHalfLengthMax);
            startAlong = source.Along - halfLength;
            startAcross = source.Across + random.Range(-ChannelAcrossJitter, ChannelAcrossJitter);
            startY = source.CenterY + random.Range(-ChannelCenterYJitter, ChannelCenterYJitter);
            endAlong = source.Along + halfLength;
            endAcross = source.Across + random.Range(-ChannelAcrossJitter, ChannelAcrossJitter);
            endY = source.CenterY + random.Range(-ChannelCenterYJitter, ChannelCenterYJitter);
        }
        else
        {
            int firstIndex = random.NextInt(0, unitCount - 2);
            UnitLocation first = unitLocations[firstIndex];
            UnitLocation second = unitLocations[firstIndex + 1];
            startAlong = first.Along + random.Range(-ChannelAlongJitter, ChannelAlongJitter);
            startAcross = first.Across + random.Range(-ChannelAcrossJitter, ChannelAcrossJitter);
            startY = first.CenterY + random.Range(-ChannelCenterYJitter, ChannelCenterYJitter);
            endAlong = second.Along + random.Range(-ChannelAlongJitter, ChannelAlongJitter);
            endAcross = second.Across + random.Range(-ChannelAcrossJitter, ChannelAcrossJitter);
            endY = second.CenterY + random.Range(-ChannelCenterYJitter, ChannelCenterYJitter);
        }

        var channel = new ErosionalChannel(
            Math.Clamp(startAlong, -ChannelEndpointClamp, ChannelEndpointClamp),
            Math.Clamp(startAcross, -ChannelEndpointClamp, ChannelEndpointClamp),
            startY,
            Math.Clamp(endAlong, -ChannelEndpointClamp, ChannelEndpointClamp),
            Math.Clamp(endAcross, -ChannelEndpointClamp, ChannelEndpointClamp),
            endY,
            random.Range(settings.ChannelWidthMin, settings.ChannelWidthMax),
            random.Range(settings.ChannelHalfThicknessMin, settings.ChannelHalfThicknessMax),
            random.Range(settings.ChannelBendMin, settings.ChannelBendMax),
            seed + 151.0);

        return new AlluvialLignitePlan(
            instance.FeatureId,
            instance,
            trendDeg,
            bedDipDeg,
            seed,
            unitCount,
            mainLevel,
            cosTrend,
            sinTrend,
            Math.Tan(bedDipDeg * Math.PI / 180.0),
            peatLenses,
            minorBeds,
            channel);
    }

    internal static double Hash3D(double x, double y, double z)
    {
        double n = Math.Sin(x * 12.9898 + y * 78.233 + z * 37.719) * 43758.5453;
        return n - Math.Floor(n);
    }

    /// <summary>Rotates deposit-local (x, z) into the (along, across) bedding frame.</summary>
    public (double Along, double Across) GetLocal(double x, double z)
    {
        return (x * cosTrend - z * sinTrend, x * sinTrend + z * cosTrend);
    }

    /// <summary>Bedding-dip elevation offset applied to every body centre.</summary>
    public double DipOffset(double x, double z)
    {
        return tanBedDip * (x * sinTrend + z * cosTrend);
    }

    public AlluvialLigniteSample Evaluate(int worldX, int worldY, int worldZ, int surfaceY = int.MaxValue)
    {
        if (worldY > surfaceY) return default;

        double x = worldX - originX;
        double y = worldY - originY;
        double z = worldZ - originZ;

        if (y < BottomY) return default;

        if (SampleChannel(x, z, y))
        {
            return new AlluvialLigniteSample(
                AlluvialLigniteZone.ChannelSand,
                0.0,
                false,
                true,
                false,
                false);
        }

        if (!TrySamplePeat(x, y, z, out PeatSample peat)) return default;

        double facies = Math.Sin(x * 0.16 - z * 0.13 + peat.Phase)
            + 0.55 * Math.Cos(y * 0.43 + x * 0.08);
        double relative = (y - peat.CenterY) / Math.Max(0.2, peat.Half);
        double partingBand = Math.Abs(Math.Sin(relative * 5.4 + peat.Phase * 0.3));

        if (!peat.Minor && partingBand < 0.12 && facies < -0.2)
        {
            return new AlluvialLigniteSample(
                AlluvialLigniteZone.CarbonaceousClay,
                peat.Ratio,
                peat.Minor,
                false,
                true,
                false);
        }

        if (peat.Ratio > 0.78 || facies < -1.08)
        {
            bool pruned = Hash3D(x * 2.1 + seed, y * 2.1, z * 2.1) < 0.25;
            return new AlluvialLigniteSample(
                pruned ? AlluvialLigniteZone.None : AlluvialLigniteZone.HighAshLignite,
                peat.Ratio,
                peat.Minor,
                false,
                true,
                pruned);
        }

        if (facies > 1.35 && peat.Ratio < 0.55)
        {
            return new AlluvialLigniteSample(
                AlluvialLigniteZone.PyriticLignite,
                peat.Ratio,
                peat.Minor,
                false,
                true,
                false);
        }

        if (Math.Sin(x * 0.29 + z * 0.17 + y * 0.23 + peat.Phase) > 0.42)
        {
            return new AlluvialLigniteSample(
                AlluvialLigniteZone.WoodyLignite,
                peat.Ratio,
                peat.Minor,
                false,
                true,
                false);
        }

        return new AlluvialLigniteSample(
            AlluvialLigniteZone.Lignite,
            peat.Ratio,
            peat.Minor,
            false,
            true,
            false);
    }

    private bool SampleChannel(double x, double z, double y)
    {
        (double along, double across) = GetLocal(x, z);
        double vx = channel.EndAlong - channel.StartAlong;
        double vz = channel.EndAcross - channel.StartAcross;
        double lengthSquared = vx * vx + vz * vz;
        double t = Math.Clamp(
            ((along - channel.StartAlong) * vx + (across - channel.StartAcross) * vz)
                / Math.Max(0.1, lengthSquared),
            0.0,
            1.0);
        double length = Math.Sqrt(lengthSquared);
        double curve = Math.Sin(Math.PI * t) * channel.Bend;
        double centerAlong = channel.StartAlong + vx * t + (length > 0.1 ? -vz / length * curve : 0.0);
        double centerAcross = channel.StartAcross + vz * t + (length > 0.1 ? vx / length * curve : 0.0);
        double centerY = channel.StartY
            + (channel.EndY - channel.StartY) * t
            + DipOffset(x, z)
            + Math.Sin(Math.PI * t + channel.Phase) * 0.3;

        double horizontalDistance = Math.Sqrt(
            (along - centerAlong) * (along - centerAlong)
            + (across - centerAcross) * (across - centerAcross));
        double verticalDistance = Math.Abs(y - centerY);
        double profile = Math.Sqrt(
            horizontalDistance * horizontalDistance / (channel.Width * channel.Width)
            + verticalDistance * verticalDistance / (channel.HalfThickness * channel.HalfThickness));
        return profile <= 1.0;
    }

    private bool TrySamplePeat(double x, double y, double z, out PeatSample best)
    {
        best = default;
        bool found = false;

        for (int i = 0; i < peatLenses.Length; i++)
        {
            PeatLens lens = peatLenses[i];
            double dx = x - lens.CenterX;
            double dz = z - lens.CenterZ;
            double along = dx * cosTrend - dz * sinTrend;
            double across = dx * sinTrend + dz * cosTrend;
            double u = along / lens.RadiusAlong;
            double v = across / lens.RadiusAcross;
            double footprint = Math.Sqrt(u * u + v * v)
                + Math.Sin(u * 4.0 + lens.Phase) * Math.Cos(v * 3.0) * 0.11;
            if (footprint > LensFootprintLimit) continue;

            double taper = Math.Sqrt(Math.Clamp(1.0 - footprint * footprint, 0.0, 1.0));
            double centerY = lens.CenterY
                + DipOffset(x, z)
                + Math.Sin(along * 0.08 + lens.Phase) * 0.6;
            double half = Math.Max(LensMinimumHalfThickness, lens.HalfThickness * taper);
            double ratio = Math.Abs(y - centerY) / half;
            if (ratio > 1.0) continue;
            if (found && ratio >= best.Ratio) continue;

            best = new PeatSample(lens.Phase, ratio, footprint, centerY, half, false);
            found = true;
        }

        for (int i = 0; i < minorBeds.Length; i++)
        {
            MinorBed bed = minorBeds[i];
            double dx = x - bed.CenterX;
            double dz = z - bed.CenterZ;
            double footprint = Math.Sqrt(dx * dx + dz * dz) / bed.Radius;
            if (footprint > 1.0) continue;

            double centerY = bed.Level
                + DipOffset(x, z)
                + Math.Sin(x * 0.06 + bed.Phase) * 0.5;
            double half = bed.HalfThickness
                * Math.Sqrt(Math.Clamp(1.0 - footprint * footprint, 0.0, 1.0));
            double ratio = Math.Abs(y - centerY) / Math.Max(0.2, half);
            if (ratio > 1.0) continue;
            if (found && ratio >= best.Ratio) continue;

            best = new PeatSample(bed.Phase, ratio, footprint, centerY, half, true);
            found = true;
        }

        return found;
    }

    private readonly record struct UnitLocation(
        double Along,
        double Across,
        double CenterY);

    private readonly record struct PeatLens(
        double CenterX,
        double CenterZ,
        double RadiusAlong,
        double RadiusAcross,
        double CenterY,
        double HalfThickness,
        double Phase);

    private readonly record struct MinorBed(
        double CenterX,
        double CenterZ,
        double Level,
        double Radius,
        double HalfThickness,
        double Phase);

    private readonly record struct ErosionalChannel(
        double StartAlong,
        double StartAcross,
        double StartY,
        double EndAlong,
        double EndAcross,
        double EndY,
        double Width,
        double HalfThickness,
        double Bend,
        double Phase);

    private readonly record struct PeatSample(
        double Phase,
        double Ratio,
        double Footprint,
        double CenterY,
        double Half,
        bool Minor);
}

internal sealed class AlluvialLigniteProceduralTemplate : IProceduralDepositTemplate
{
    private static readonly string[] RequiredSlots =
    {
        ProceduralMaterialSlots.Lignite,
        ProceduralMaterialSlots.ClayParting,
        ProceduralMaterialSlots.ChannelSand
    };

    public string Code => "alluvialLignite";
    public IReadOnlyList<string> RequiredMaterialSlots => RequiredSlots;
    public bool RequiresPlan => true;

    public int GetMaximumHorizontalReach(ProceduralDepositDefinition definition)
    {
        return definition.Lignite.HorizontalRadius;
    }

    public object CreatePlan(in ProceduralDepositInstance instance, ProceduralDepositDefinition definition)
    {
        return AlluvialLignitePlan.Create(instance, definition.Lignite);
    }

    public bool Validate(ProceduralDepositDefinition definition, out string error)
    {
        AlluvialLigniteDefinition settings = definition.Lignite;
        bool valid = settings.HorizontalRadius >= 16
            && settings.VerticalHalfHeight >= 8
            && settings.UnitCountMin >= 1
            && settings.UnitCountMax >= settings.UnitCountMin
            && settings.UnitCountMax <= 3
            && settings.LensesPerUnit >= 1
            && settings.MinorBedCountMin >= 0
            && settings.MinorBedCountMax >= settings.MinorBedCountMin
            && settings.TrendDegMax >= settings.TrendDegMin
            && settings.BedDipDegMax >= settings.BedDipDegMin
            && settings.MainLevelMax >= settings.MainLevelMin
            && settings.LensRadiusAlongMin > 0.0
            && settings.LensRadiusAlongMax >= settings.LensRadiusAlongMin
            && settings.LensRadiusAcrossMin > 0.0
            && settings.LensRadiusAcrossMax >= settings.LensRadiusAcrossMin
            && settings.LensHalfThicknessMin > 0.0
            && settings.LensHalfThicknessMax >= settings.LensHalfThicknessMin
            && settings.MinorBedLevelDropMax >= settings.MinorBedLevelDropMin
            && settings.MinorBedRadiusMin > 0.0
            && settings.MinorBedRadiusMax >= settings.MinorBedRadiusMin
            && settings.MinorBedHalfThicknessMin > 0.0
            && settings.MinorBedHalfThicknessMax >= settings.MinorBedHalfThicknessMin
            && settings.ChannelWidthMin > 0.0
            && settings.ChannelWidthMax >= settings.ChannelWidthMin
            && settings.ChannelHalfThicknessMin > 0.0
            && settings.ChannelHalfThicknessMax >= settings.ChannelHalfThicknessMin
            && settings.ChannelBendMax >= settings.ChannelBendMin;
        error = valid ? string.Empty : "invalid alluvial lignite settings";
        return valid;
    }

    public void Realize(
        ProceduralDepositWorldGenSystem system,
        in DepositCandidate candidate,
        IChunkColumnGenerateRequest request,
        int baseX,
        int baseZ)
    {
        system.RealizeAlluvialLigniteCandidate(candidate, request, baseX, baseZ);
    }
}

public sealed partial class ProceduralDepositWorldGenSystem
{
    internal void RealizeAlluvialLigniteCandidate(
        DepositCandidate candidate,
        IChunkColumnGenerateRequest request,
        int baseX,
        int baseZ)
    {
        CompiledProceduralDeposit compiled = candidate.Compiled;
        AlluvialLigniteDefinition settings = compiled.Definition.Lignite;
        var plan = (AlluvialLignitePlan)candidate.Plan!;
        ushort[] heightMap = request.Chunks[0].MapChunk.WorldGenTerrainHeightMap;
        int worldHeight = serverApi!.World.BlockAccessor.MapSizeY;
        int minimumY = GameMath.Clamp(candidate.Instance.CenterY - settings.VerticalHalfHeight, 1, worldHeight - 2);
        int maximumPrototypeY = GameMath.Clamp(candidate.Instance.CenterY + settings.VerticalHalfHeight, 1, worldHeight - 2);

        int[] zoneSlots = BuildAlluvialLigniteZoneSlots(compiled);

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
                    AlluvialLigniteSample sample = plan.Evaluate(worldX, y, worldZ, surfaceY);
                    if (sample.Zone == AlluvialLigniteZone.None) continue;

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

    private static int[] BuildAlluvialLigniteZoneSlots(CompiledProceduralDeposit compiled)
    {
        var slots = new int[Enum.GetValues<AlluvialLigniteZone>().Length];
        Array.Fill(slots, -1);

        // Massive, woody, pyritic and high-ash lignite are all the same mineable lignite block.
        int ligniteSlot = compiled.GetSlotId(ProceduralMaterialSlots.Lignite);
        slots[(int)AlluvialLigniteZone.Lignite] = ligniteSlot;
        slots[(int)AlluvialLigniteZone.WoodyLignite] = ligniteSlot;
        slots[(int)AlluvialLigniteZone.PyriticLignite] = ligniteSlot;
        slots[(int)AlluvialLigniteZone.HighAshLignite] = ligniteSlot;

        slots[(int)AlluvialLigniteZone.CarbonaceousClay] = compiled.GetSlotId(ProceduralMaterialSlots.ClayParting);
        slots[(int)AlluvialLigniteZone.ChannelSand] = compiled.GetSlotId(ProceduralMaterialSlots.ChannelSand);
        return slots;
    }
}
