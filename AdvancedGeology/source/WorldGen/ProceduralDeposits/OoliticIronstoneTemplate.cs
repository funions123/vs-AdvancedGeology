using System;
using System.Collections.Generic;

namespace AdvancedGeology.WorldGen.ProceduralDeposits;

internal enum OoliticIronstoneZone
{
    None,
    Hematite,
    Limonite,
    Siderite,
    Magnetite,
    Breccia
}

internal readonly record struct OoliticIronstoneSample(
    bool Inside,
    bool Halo,
    double IronPotential,
    double ChannelField);

/// <summary>
/// Hematite, limonite, siderite, magnetite, and breccia form shallow, gently warped oolitic beds with broad pinch-and-swell footprints and channel-modulated enrichment.
/// </summary>
internal sealed class OoliticIronstonePlan
{
    private const ulong PlanSalt = 0x4F4F4C4954494349UL;

    private readonly int originX;
    private readonly int originY;
    private readonly int originZ;
    private readonly double systemX;
    private readonly double systemZ;
    private readonly double cosStrike;
    private readonly double sinStrike;
    private readonly double cosDipDirection;
    private readonly double sinDipDirection;
    private readonly double tangentDip;
    private readonly double seed;
    private readonly OoliticBed[] beds;

    public int BedCount => beds.Length;

    private OoliticIronstonePlan(
        in ProceduralDepositInstance instance,
        double systemX,
        double systemZ,
        double cosStrike,
        double sinStrike,
        double cosDipDirection,
        double sinDipDirection,
        double tangentDip,
        double seed,
        OoliticBed[] beds)
    {
        originX = instance.CenterX;
        originY = instance.CenterY;
        originZ = instance.CenterZ;
        this.systemX = systemX;
        this.systemZ = systemZ;
        this.cosStrike = cosStrike;
        this.sinStrike = sinStrike;
        this.cosDipDirection = cosDipDirection;
        this.sinDipDirection = sinDipDirection;
        this.tangentDip = tangentDip;
        this.seed = seed;
        this.beds = beds;
    }

    public static OoliticIronstonePlan Create(
        in ProceduralDepositInstance instance,
        OoliticIronstoneDefinition settings)
    {
        var random = new ProceduralDepositRandom(instance.FeatureId ^ PlanSalt);
        double seed = random.Range(0, 100);
        double strike = random.Range(8, 32) * Math.PI / 180.0;
        double dip = random.Range(settings.DipMinDeg, settings.DipMaxDeg) * Math.PI / 180.0;
        double dipDirection = random.Range(72, 108) * Math.PI / 180.0;
        double systemX = random.Range(-4, 4);
        double systemZ = random.Range(-4, 4);
        int bedCount = random.NextInt(settings.BedMin, settings.BedMax);
        var beds = new OoliticBed[bedCount];

        for (int index = 0; index < bedCount; index++)
        {
            double along = random.Range(-13, 13);
            double across = random.Range(-5, 5);
            double centerX = systemX + along * Math.Cos(strike) + across * Math.Sin(strike);
            double centerZ = systemZ - along * Math.Sin(strike) + across * Math.Cos(strike);
            double stratY = 6
                + (index - (bedCount - 1) * 0.5) * random.Range(settings.BedSpacingMin, settings.BedSpacingMax)
                + random.Range(-0.7, 0.7);
            beds[index] = new OoliticBed(
                centerX,
                centerZ,
                stratY,
                random.Range(settings.RadiusAlongMin, settings.RadiusAlongMax),
                random.Range(settings.RadiusAcrossMin, settings.RadiusAcrossMax),
                random.Range(settings.ThicknessMin, settings.ThicknessMax),
                random.Range(0.58, 0.92),
                random.Range(-4, 4) * Math.PI / 180.0 + strike,
                seed + index * 17);
        }

        return new OoliticIronstonePlan(
            instance,
            systemX,
            systemZ,
            Math.Cos(strike),
            Math.Sin(strike),
            Math.Cos(dipDirection),
            Math.Sin(dipDirection),
            Math.Tan(dip),
            seed,
            beds);
    }

    public OoliticIronstoneSample Sample(int worldX, int worldY, int worldZ)
    {
        double x = worldX - originX;
        double y = worldY - originY;
        double z = worldZ - originZ;
        OoliticIronstoneSample best = default;
        bool found = false;
        bool halo = false;
        double strongestChannel = double.NegativeInfinity;

        foreach (OoliticBed bed in beds)
        {
            double dx = x - bed.CenterX;
            double dz = z - bed.CenterZ;
            double along = dx * bed.CosStrike - dz * bed.SinStrike;
            double across = dx * bed.SinStrike + dz * bed.CosStrike;
            double localU = along / bed.RadiusAlong;
            double localV = across / bed.RadiusAcross;
            double footprint = Math.Sqrt(localU * localU + localV * localV)
                + Math.Sin(localU * 3.3 + bed.Phase) * Math.Cos(localV * 2.7) * 0.13;
            double pinch = 0.78 + 0.22 * Math.Sin(along * 0.14 + bed.Phase)
                + 0.08 * Math.Cos(across * 0.19 - bed.Phase);
            double localCenter = bed.CenterStratY + Math.Sin(along * 0.09 + bed.Phase) * 0.30;
            double stratY = y - BeddingOffset(x, z);
            double bandDistance = Math.Abs(stratY - localCenter);
            double channelField = Math.Sin(along * 0.075 + bed.Phase) * 0.58
                + Math.Cos(across * 0.11 - bed.Phase * 0.6) * 0.27
                + Math.Sin((along + across) * 0.18 + bed.Phase * 0.3) * 0.15;
            double potential = Math.Clamp(bed.IronPotential + channelField * 0.16, 0, 1);
            double halfThickness = Math.Max(0.18, bed.Thickness * 0.5 * pinch);
            bool inside = footprint <= 1.05 && bandDistance <= halfThickness;
            bool inHalo = footprint <= 1.15 && bandDistance <= halfThickness + 0.9;

            if (inside && (!found || potential > best.IronPotential))
            {
                best = new OoliticIronstoneSample(true, inHalo, potential, channelField);
                found = true;
            }
            if (inHalo)
            {
                halo = true;
                strongestChannel = Math.Max(strongestChannel, channelField);
            }
        }

        if (found) return best;
        return halo
            ? new OoliticIronstoneSample(false, true, 0, strongestChannel)
            : default;
    }

    private double BeddingOffset(double x, double z)
    {
        double localX = x - systemX;
        double localZ = z - systemZ;
        double projection = localX * cosDipDirection + localZ * sinDipDirection;
        double along = localX * sinDipDirection - localZ * cosDipDirection;
        double warp = Math.Sin(projection * 0.08 + seed) * 1.6;
        double fold = Math.Sin(along * 0.06 + seed * 0.4) * 1.2;
        return tangentDip * projection + warp + fold;
    }

    private readonly record struct OoliticBed(
        double CenterX,
        double CenterZ,
        double CenterStratY,
        double RadiusAlong,
        double RadiusAcross,
        double Thickness,
        double IronPotential,
        double Strike,
        double Phase)
    {
        public double CosStrike => Math.Cos(Strike);
        public double SinStrike => Math.Sin(Strike);
    }
}
