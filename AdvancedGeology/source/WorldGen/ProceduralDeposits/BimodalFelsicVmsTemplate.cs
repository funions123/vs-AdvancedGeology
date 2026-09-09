using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Vintagestory.API.Server;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace AdvancedGeology.WorldGen.ProceduralDeposits;

public enum BimodalFelsicVmsZone
{
    None = 0,
    Chalcopyrite,
    Sphalerite,
    Galena,
    Pyrite,
    Barite,
    Quartz,
    Chert,
    ChertHematite,
    ChertMagnetite,
    ChertPyrite,
    Chalcocite,
    Smithsonite,
    Cerussite,
    Limonite,
    Gossan,
    YellowGossan,
    GreyGossan
}

[JsonObject(MemberSerialization.OptIn)]
public sealed class BimodalFelsicVmsDefinition
{
    [JsonProperty]
    public int HorizontalRadius { get; set; } = 48;

    [JsonProperty]
    public int VerticalHalfHeight { get; set; } = 35;

    [JsonProperty]
    public int SupergeneDepth { get; set; } = 8;
}

public readonly record struct BimodalFelsicVmsSample(
    BimodalFelsicVmsZone Zone,
    int Grade);

/// <summary>
/// Chalcopyrite, sphalerite, galena, pyrite, barite, quartz, mineralized chert, and secondary oxide minerals form stacked massive-sulfide lenses above a branching footwall stringer zone.
/// </summary>
internal sealed class BimodalFelsicVmsPlan
{
    private const ulong PlanSalt = 0x42494D4F44414CUL; // "BIMODAL"
    private const double DepositHorizon = 7.0;

    private readonly ulong featureId;
    private readonly int originX;
    private readonly int originY;
    private readonly int originZ;
    private readonly double seed;
    private readonly double regionalStrikeDeg;
    private readonly double bedDipDeg;
    private readonly double feederX;
    private readonly double feederZ;
    private readonly double pipeDepth;
    private readonly double sinRegionalStrike;
    private readonly double cosRegionalStrike;
    private readonly double tanBedDip;
    private readonly MassiveLens[] massiveLenses;
    private readonly Stringer[] stringers;
    private readonly ExhaliteField exhaliteField;

    public int OriginX => originX;
    public int OriginY => originY;
    public int OriginZ => originZ;
    public double Seed => seed;
    public double RegionalStrikeDeg => regionalStrikeDeg;
    public double BedDipDeg => bedDipDeg;
    public double FeederX => feederX;
    public double FeederZ => feederZ;
    public double PipeDepth => pipeDepth;
    public int LensCount => massiveLenses.Length;
    public int StringerCount => stringers.Length;

    private BimodalFelsicVmsPlan(
        ulong featureId,
        in ProceduralDepositInstance instance,
        double seed,
        double regionalStrikeDeg,
        double bedDipDeg,
        double feederX,
        double feederZ,
        double pipeDepth,
        double sinRegionalStrike,
        double cosRegionalStrike,
        double tanBedDip,
        MassiveLens[] massiveLenses,
        Stringer[] stringers,
        ExhaliteField exhaliteField)
    {
        this.featureId = featureId;
        originX = instance.CenterX;
        originY = instance.CenterY;
        originZ = instance.CenterZ;
        this.seed = seed;
        this.regionalStrikeDeg = regionalStrikeDeg;
        this.bedDipDeg = bedDipDeg;
        this.feederX = feederX;
        this.feederZ = feederZ;
        this.pipeDepth = pipeDepth;
        this.sinRegionalStrike = sinRegionalStrike;
        this.cosRegionalStrike = cosRegionalStrike;
        this.tanBedDip = tanBedDip;
        this.massiveLenses = massiveLenses;
        this.stringers = stringers;
        this.exhaliteField = exhaliteField;
    }

    public static BimodalFelsicVmsPlan Create(
        in ProceduralDepositInstance instance,
        BimodalFelsicVmsDefinition settings)
    {
        var random = new ProceduralDepositRandom(instance.FeatureId ^ PlanSalt);

        // Parameter draw order and ranges:
        double regionalStrikeDeg = random.Range(10.0, 27.0);
        double bedDipDeg = random.Range(4.0, 10.0);
        double feederX = random.Range(-5.0, 5.0);
        double feederZ = random.Range(-5.0, 5.0);
        double pipeDepth = random.Range(34.0, 40.0);
        double seed = random.Range(0.0, 100.0);

        double regionalStrikeRad = regionalStrikeDeg * Math.PI / 180.0;
        double sinRegionalStrike = Math.Sin(regionalStrikeRad);
        double cosRegionalStrike = Math.Cos(regionalStrikeRad);
        double tanBedDip = Math.Tan(bedDipDeg * Math.PI / 180.0);

        const int lensCount = 4;
        var lenses = new MassiveLens[lensCount];
        for (int i = 0; i < lensCount; i++)
        {
            double side = i % 2 == 0 ? -1.0 : 1.0;
            double distance = i == 0 ? random.Range(-4.0, 3.0) : side * random.Range(8.0, 26.0);
            double centerX = feederX + distance + random.Range(-3.0, 3.0);
            double centerZ = feederZ + random.Range(-13.0, 13.0);
            double centerStratY = DepositHorizon + random.Range(-2.8, 2.6) + (i == 3 ? 2.5 : 0.0);
            double radiusAlong = i == 0 ? random.Range(20.0, 26.0) : random.Range(10.0, 19.0);
            double radiusAcross = i == 0 ? random.Range(10.0, 14.0) : random.Range(6.0, 11.0);
            double radiusVertical = i == 0 ? random.Range(4.0, 6.0) : random.Range(2.5, 4.8);
            double strikeDeg = regionalStrikeDeg + random.Range(-11.0, 11.0);
            double phase = seed + i * 17.0;
            double lowerCuBias = random.Range(-0.20, 0.20);

            lenses[i] = new MassiveLens(
                centerX,
                centerZ,
                centerStratY,
                radiusAlong,
                radiusAcross,
                radiusVertical,
                strikeDeg,
                phase,
                lowerCuBias);
        }

        MassiveLens mainLens = lenses[0];
        double sumX = 0.0;
        double sumZ = 0.0;
        for (int i = 0; i < lenses.Length; i++)
        {
            sumX += lenses[i].CenterX;
            sumZ += lenses[i].CenterZ;
        }
        double meanCenterX = sumX / lenses.Length;
        double meanCenterZ = sumZ / lenses.Length;

        double exhaliteCenterStratY = DepositHorizon + random.Range(0.8, 1.8);
        double exhaliteRadiusAlong = Math.Max(38.0, mainLens.RadiusAlong * random.Range(1.75, 2.0));
        double exhaliteRadiusAcross = Math.Max(20.0, mainLens.RadiusAcross * random.Range(1.65, 1.95));
        var exhaliteField = new ExhaliteField(
            meanCenterX,
            meanCenterZ,
            exhaliteCenterStratY,
            exhaliteRadiusAlong,
            exhaliteRadiusAcross,
            mainLens.SinStrike,
            mainLens.CosStrike,
            seed + 211.0);

        const int principalStringerCount = 14;
        const int branchCount = 6;
        var stringerList = new Stringer[principalStringerCount + branchCount];

        double offset = random.Range(-15.0, -12.0);
        for (int i = 0; i < principalStringerCount; i++)
        {
            if (i > 0) offset += random.Range(1.8, 3.4);
            double strikeDeg = regionalStrikeDeg + random.Range(-38.0, 38.0);
            double dipDeg = random.Range(56.0, 86.0);
            double centerAlong = random.Range(-10.0, 10.0);
            double centerY = random.Range(-7.0, 2.0);
            double lengthHalf = random.Range(11.0, 24.0);
            double heightHalf = random.Range(13.0, 26.0);
            double thickness = random.Range(0.45, 1.05);
            double phase = seed + 61.0 + i * 9.0;
            double relayCenter = random.Range(-9.0, 9.0);
            double relayWidth = random.Range(2.0, 4.5);
            double relayShift = random.Range(-1.0, 1.0);
            double pinchPhase = seed + i * 5.1;

            stringerList[i] = new Stringer(
                strikeDeg,
                dipDeg,
                offset,
                centerAlong,
                centerY,
                lengthHalf,
                heightHalf,
                thickness,
                phase,
                relayCenter,
                relayWidth,
                relayShift,
                pinchPhase,
                isBranch: false);
        }

        for (int i = 0; i < branchCount; i++)
        {
            Stringer parent = stringerList[(i * 2 + 2) % principalStringerCount];
            double direction = i % 2 == 0 ? -1.0 : 1.0;
            double strikeDeg = parent.StrikeDeg + direction * random.Range(16.0, 34.0);
            double dipDeg = parent.DipDeg + random.Range(-12.0, 12.0);
            double branchOffset = parent.Offset + direction * random.Range(1.2, 3.2);
            double centerAlong = parent.CenterAlong + direction * random.Range(3.0, 8.0);
            double centerY = parent.CenterY + random.Range(2.0, 7.0);
            double lengthHalf = random.Range(6.0, 13.0);
            double heightHalf = random.Range(7.0, 15.0);
            double thickness = random.Range(0.35, 0.80);
            double phase = seed + 131.0 + i * 12.0;
            double relayCenter = random.Range(-5.0, 5.0);
            double relayWidth = random.Range(1.5, 3.5);
            double relayShift = random.Range(-0.7, 0.7);
            double pinchPhase = seed + 81.0 + i * 7.0;

            stringerList[principalStringerCount + i] = new Stringer(
                strikeDeg,
                dipDeg,
                branchOffset,
                centerAlong,
                centerY,
                lengthHalf,
                heightHalf,
                thickness,
                phase,
                relayCenter,
                relayWidth,
                relayShift,
                pinchPhase,
                isBranch: true);
        }

        return new BimodalFelsicVmsPlan(
            instance.FeatureId,
            instance,
            seed,
            regionalStrikeDeg,
            bedDipDeg,
            feederX,
            feederZ,
            pipeDepth,
            sinRegionalStrike,
            cosRegionalStrike,
            tanBedDip,
            lenses,
            stringerList,
            exhaliteField);
    }

    public double GetStratigraphicY(double x, double y, double z)
    {
        double acrossStrike = x * sinRegionalStrike + z * cosRegionalStrike;
        double warp = Math.Sin(x * 0.055 + seed) * 1.3 + Math.Cos(z * 0.06 - seed * 0.5) * 1.0;
        return y - tanBedDip * acrossStrike - warp;
    }

    public BimodalFelsicVmsSample Evaluate(
        int worldX,
        int worldY,
        int worldZ,
        int surfaceY = int.MaxValue,
        bool weatheringEnabled = true)
    {
        if (worldY > surfaceY)
        {
            return new BimodalFelsicVmsSample(BimodalFelsicVmsZone.None, 0);
        }

        double x = worldX - originX;
        double y = worldY - originY;
        double z = worldZ - originZ;

    // Deposit package is bounded at deposit-local y < -35
        if (y < -35.0)
        {
            return new BimodalFelsicVmsSample(BimodalFelsicVmsZone.None, 0);
        }

        double stratY = GetStratigraphicY(x, y, z);
        bool isFootwall = stratY < DepositHorizon;
        double depthBelowHorizon = DepositHorizon - stratY;
        double fineGrain = Hash3D(x * 2.1 + seed, y * 2.1, z * 2.1);

        // 1. Massive Sulfide Lenses
        LensSample? bestLens = null;
        for (int i = 0; i < massiveLenses.Length; i++)
        {
            LensSample sample = massiveLenses[i].Sample(x, stratY, z);
            if (sample.Inside && (!bestLens.HasValue || sample.Radius < bestLens.Value.Radius))
            {
                bestLens = sample;
            }
        }

        if (bestLens.HasValue)
        {
            LensSample lens = bestLens.Value;
            double facies = Math.Sin(lens.AlongNorm * 4.6 + lens.Phase)
                + 0.65 * Math.Cos(lens.AcrossNorm * 5.1 - lens.Phase * 0.4)
                + 0.45 * Math.Sin(lens.VerticalNorm * 4.8 + lens.AlongNorm * 1.7);

            BimodalFelsicVmsZone hypogeneZone;
            if (fineGrain > 0.992 && facies > 0.65)
            {
                // Au-Ag-rich sulfide -> chalcopyrite (trace elements handle precious metals)
                hypogeneZone = BimodalFelsicVmsZone.Chalcopyrite;
            }
            else if (lens.VerticalNorm < 0.05 + lens.LowerCuBias && lens.LateralNorm < 0.72)
            {
                hypogeneZone = facies > -0.18 ? BimodalFelsicVmsZone.Chalcopyrite : BimodalFelsicVmsZone.Pyrite;
            }
            else if (lens.VerticalNorm > 0.18 || lens.LateralNorm > 0.68)
            {
                if (facies > 0.62) hypogeneZone = BimodalFelsicVmsZone.Sphalerite;
                else if (facies > -0.12) hypogeneZone = BimodalFelsicVmsZone.Galena;
                else if (facies < -1.15) hypogeneZone = BimodalFelsicVmsZone.Barite;
                else hypogeneZone = BimodalFelsicVmsZone.Pyrite;
            }
            else
            {
                if (facies > 0.58) hypogeneZone = BimodalFelsicVmsZone.Chalcopyrite;
                else if (facies > -0.28) hypogeneZone = BimodalFelsicVmsZone.Sphalerite;
                else hypogeneZone = BimodalFelsicVmsZone.Pyrite;
            }

            return ApplySupergene(hypogeneZone, worldY, surfaceY, fineGrain, weatheringEnabled);
        }

        // 2. Footwall Stringer Feeder Network
        StringerSample? bestStringer = null;
        for (int i = 0; i < stringers.Length; i++)
        {
            StringerSample sample = stringers[i].Sample(x, y, z);
            if (!bestStringer.HasValue || sample.Score < bestStringer.Value.Score)
            {
                bestStringer = sample;
            }
        }

        if (isFootwall && bestStringer.HasValue && bestStringer.Value.Inside && depthBelowHorizon < pipeDepth)
        {
            double stringerFacies = Math.Sin(x * 0.31 + z * 0.19 + stratY * 0.13 + seed);
            BimodalFelsicVmsZone hypogeneZone;
            if (stringerFacies > 0.42) hypogeneZone = BimodalFelsicVmsZone.Chalcopyrite;
            else if (stringerFacies > -0.28) hypogeneZone = BimodalFelsicVmsZone.Pyrite;
            else hypogeneZone = BimodalFelsicVmsZone.Quartz;

            return ApplySupergene(hypogeneZone, worldY, surfaceY, fineGrain, weatheringEnabled);
        }

        // 3. Lithified Exhalative Chemical Sediment Horizon
        ExhaliteSample exhalite = exhaliteField.Sample(x, stratY, z, massiveLenses);
        if (exhalite.Inside)
        {
            double chemicalBand = Math.Sin(exhalite.Along * 0.27 + exhalite.Across * 0.16 + seed);
            double faciesPatch = Math.Cos(exhalite.Along * 0.11 - exhalite.Across * 0.17 + seed * 0.6);

            // Chert speckle threshold justification:
            // The plan specifies speckled (not solid) ore in chert at 20% density.
            // Hash3D (fineGrain) produces values uniformly distributed on [0.0, 1.0).
            // Therefore, fineGrain >= 0.80 selects exactly the top 20% of voxels (1.0 - 0.80 = 0.20 = 20%).
            // The remaining 80% (fineGrain < 0.80) writes solid chert rock.
            // Chert-hosted speckle facies never weather.
            if (exhalite.LensProximity < 1.30)
            {
                if (faciesPatch > 0.48) return new BimodalFelsicVmsSample(BimodalFelsicVmsZone.Barite, 0);
                if (faciesPatch > -0.22)
                {
                    // Sulfidic chert -> 20% speckled pyrite in chert
                    return fineGrain >= 0.80
                        ? new BimodalFelsicVmsSample(BimodalFelsicVmsZone.ChertPyrite, 0)
                        : new BimodalFelsicVmsSample(BimodalFelsicVmsZone.Chert, 0);
                }
            }

            if (chemicalBand > 0.38)
            {
                // Hematitic chert / jasper -> 20% speckled poor hematite in chert
                return fineGrain >= 0.80
                    ? new BimodalFelsicVmsSample(BimodalFelsicVmsZone.ChertHematite, 0)
                    : new BimodalFelsicVmsSample(BimodalFelsicVmsZone.Chert, 0);
            }

            if (chemicalBand < -0.48 && faciesPatch > -0.25)
            {
                // Magnetite-rich chert -> 20% speckled poor magnetite in chert
                return fineGrain >= 0.80
                    ? new BimodalFelsicVmsSample(BimodalFelsicVmsZone.ChertMagnetite, 0)
                    : new BimodalFelsicVmsSample(BimodalFelsicVmsZone.Chert, 0);
            }

            if (faciesPatch > 0.86) return new BimodalFelsicVmsSample(BimodalFelsicVmsZone.Barite, 0);

            // Chert exhalite -> chert rock
            return new BimodalFelsicVmsSample(BimodalFelsicVmsZone.Chert, 0);
        }

        if (isFootwall && depthBelowHorizon >= 0.0 && depthBelowHorizon < pipeDepth)
        {
            double progressUp = 1.0 - depthBelowHorizon / pipeDepth;
            double axisX = feederX + Math.Sin(stratY * 0.12 + seed) * 3.0;
            double axisZ = feederZ + Math.Cos(stratY * 0.10 + seed * 0.8) * 2.6;
            double dx = x - axisX;
            double dz = z - axisZ;
            double pipeDistance = Math.Sqrt(dx * dx + dz * dz);
            double coreRadius = 4.0 + progressUp * 8.0 + Math.Sin(stratY * 0.22 + seed) * 1.1;
            double haloRadius = coreRadius + 6.0 * (0.45 + progressUp * 0.55);

            if (bestStringer.HasValue && bestStringer.Value.InHalo && pipeDistance < haloRadius + 3.0)
            {
                return new BimodalFelsicVmsSample(BimodalFelsicVmsZone.None, 0);
            }
            if (pipeDistance < coreRadius)
            {
                return new BimodalFelsicVmsSample(BimodalFelsicVmsZone.None, 0);
            }
            if (pipeDistance < haloRadius)
            {
                return new BimodalFelsicVmsSample(BimodalFelsicVmsZone.None, 0);
            }
        }

        return new BimodalFelsicVmsSample(BimodalFelsicVmsZone.None, 0);
    }

    private static BimodalFelsicVmsSample ApplySupergene(
        BimodalFelsicVmsZone hypogeneZone,
        int worldY,
        int surfaceY,
        double fineGrain,
        bool weatheringEnabled)
    {
        // Barite and Quartz gangue remain unaltered at all depths
        if (hypogeneZone == BimodalFelsicVmsZone.Barite)
        {
            return new BimodalFelsicVmsSample(BimodalFelsicVmsZone.Barite, 0);
        }
        if (hypogeneZone == BimodalFelsicVmsZone.Quartz)
        {
            return new BimodalFelsicVmsSample(BimodalFelsicVmsZone.Quartz, 0);
        }

        int depth = surfaceY < int.MaxValue ? surfaceY - worldY + 1 : 999;

        // Depth > 8 or weathering disabled: hypogene assemblage
        if (!weatheringEnabled || depth > 8)
        {
            int hypogeneGrade = hypogeneZone == BimodalFelsicVmsZone.Pyrite
                ? 0
                : (fineGrain > 0.70 ? 2 : (fineGrain > 0.35 ? 1 : 0));
            return new BimodalFelsicVmsSample(hypogeneZone, hypogeneGrade);
        }

        // Depth 1-3: stained gossan soil
        if (depth <= 3)
        {
            BimodalFelsicVmsZone soilZone = hypogeneZone switch
            {
                BimodalFelsicVmsZone.Chalcopyrite => BimodalFelsicVmsZone.Gossan,
                BimodalFelsicVmsZone.Pyrite => BimodalFelsicVmsZone.Gossan,
                BimodalFelsicVmsZone.Sphalerite => BimodalFelsicVmsZone.YellowGossan,
                BimodalFelsicVmsZone.Galena => BimodalFelsicVmsZone.GreyGossan,
                _ => BimodalFelsicVmsZone.Gossan
            };
            return new BimodalFelsicVmsSample(soilZone, 0);
        }

        // Depth 4-8: secondary ore
        if (hypogeneZone == BimodalFelsicVmsZone.Chalcopyrite)
        {
            // Chalcocite: bountiful grade (3) at depth 6-7; rich/medium elsewhere
            int chalcociteGrade = (depth == 6 || depth == 7) ? 3 : (fineGrain > 0.65 ? 2 : 1);
            return new BimodalFelsicVmsSample(BimodalFelsicVmsZone.Chalcocite, chalcociteGrade);
        }
        if (hypogeneZone == BimodalFelsicVmsZone.Sphalerite)
        {
            int smithsoniteGrade = fineGrain > 0.70 ? 2 : 1;
            return new BimodalFelsicVmsSample(BimodalFelsicVmsZone.Smithsonite, smithsoniteGrade);
        }
        if (hypogeneZone == BimodalFelsicVmsZone.Galena)
        {
            int cerussiteGrade = fineGrain > 0.70 ? 2 : 1;
            return new BimodalFelsicVmsSample(BimodalFelsicVmsZone.Cerussite, cerussiteGrade);
        }
        if (hypogeneZone == BimodalFelsicVmsZone.Pyrite)
        {
            int limoniteGrade = fineGrain > 0.70 ? 2 : 1;
            return new BimodalFelsicVmsSample(BimodalFelsicVmsZone.Limonite, limoniteGrade);
        }

        return new BimodalFelsicVmsSample(hypogeneZone, 0);
    }

    internal static double Hash3D(double x, double y, double z)
    {
        double n = Math.Sin(x * 12.9898 + y * 78.233 + z * 37.719) * 43758.5453;
        return n - Math.Floor(n);
    }

    private static double Clamp(double value, double min, double max)
    {
        return Math.Max(min, Math.Min(max, value));
    }

    private readonly struct MassiveLens
    {
        public readonly double CenterX;
        public readonly double CenterZ;
        public readonly double CenterStratY;
        public readonly double RadiusAlong;
        public readonly double RadiusAcross;
        public readonly double RadiusVertical;
        public readonly double StrikeDeg;
        public readonly double SinStrike;
        public readonly double CosStrike;
        public readonly double Phase;
        public readonly double LowerCuBias;

        public MassiveLens(
            double centerX,
            double centerZ,
            double centerStratY,
            double radiusAlong,
            double radiusAcross,
            double radiusVertical,
            double strikeDeg,
            double phase,
            double lowerCuBias)
        {
            CenterX = centerX;
            CenterZ = centerZ;
            CenterStratY = centerStratY;
            RadiusAlong = radiusAlong;
            RadiusAcross = radiusAcross;
            RadiusVertical = radiusVertical;
            StrikeDeg = strikeDeg;
            double strikeRad = strikeDeg * Math.PI / 180.0;
            SinStrike = Math.Sin(strikeRad);
            CosStrike = Math.Cos(strikeRad);
            Phase = phase;
            LowerCuBias = lowerCuBias;
        }

        public LensSample Sample(double x, double stratY, double z)
        {
            double dx = x - CenterX;
            double dz = z - CenterZ;
            double along = dx * CosStrike - dz * SinStrike;
            double across = dx * SinStrike + dz * CosStrike;
            double vertical = stratY - CenterStratY;
            double u = along / RadiusAlong;
            double v = across / RadiusAcross;
            double w = vertical / RadiusVertical;
            double boundaryWarp = Math.Sin(u * 4.0 + Phase) * Math.Cos(v * 3.3) * 0.14
                + Math.Sin(u * 7.1 - v * 4.8 + Phase * 0.4) * 0.06;
            double radius = Math.Sqrt(u * u + v * v + w * w) + boundaryWarp;
            return new LensSample(
                radius <= 1.0,
                radius,
                w,
                Math.Sqrt(u * u + v * v),
                u,
                v,
                Phase,
                LowerCuBias);
        }
    }

    private readonly record struct LensSample(
        bool Inside,
        double Radius,
        double VerticalNorm,
        double LateralNorm,
        double AlongNorm,
        double AcrossNorm,
        double Phase,
        double LowerCuBias);

    private readonly struct Stringer
    {
        public readonly double StrikeDeg;
        public readonly double DipDeg;
        public readonly double SinStrike;
        public readonly double CosStrike;
        public readonly double SinDip;
        public readonly double CosDip;
        public readonly double Offset;
        public readonly double CenterAlong;
        public readonly double CenterY;
        public readonly double LengthHalf;
        public readonly double HeightHalf;
        public readonly double Thickness;
        public readonly double Phase;
        public readonly double RelayCenter;
        public readonly double RelayWidth;
        public readonly double RelayShift;
        public readonly double PinchPhase;
        public readonly bool IsBranch;

        public Stringer(
            double strikeDeg,
            double dipDeg,
            double offset,
            double centerAlong,
            double centerY,
            double lengthHalf,
            double heightHalf,
            double thickness,
            double phase,
            double relayCenter,
            double relayWidth,
            double relayShift,
            double pinchPhase,
            bool isBranch)
        {
            StrikeDeg = strikeDeg;
            DipDeg = dipDeg;
            double strikeRad = strikeDeg * Math.PI / 180.0;
            double dipRad = dipDeg * Math.PI / 180.0;
            SinStrike = Math.Sin(strikeRad);
            CosStrike = Math.Cos(strikeRad);
            SinDip = Math.Sin(dipRad);
            CosDip = Math.Cos(dipRad);
            Offset = offset;
            CenterAlong = centerAlong;
            CenterY = centerY;
            LengthHalf = lengthHalf;
            HeightHalf = heightHalf;
            Thickness = thickness;
            Phase = phase;
            RelayCenter = relayCenter;
            RelayWidth = relayWidth;
            RelayShift = relayShift;
            PinchPhase = pinchPhase;
            IsBranch = isBranch;
        }

        public StringerSample Sample(double x, double y, double z)
        {
            double along = x * CosStrike - z * SinStrike;
            double across = x * SinStrike + z * CosStrike - Offset;
            double bend = Math.Sin(along * 0.13 + y * 0.075 + Phase) * 1.2;
            double kink = Math.Cos(along * 0.34 + Phase * 1.4) * 0.5;
            double relay = RelayShift * Math.Tanh((along - RelayCenter) / RelayWidth);
            double signedDistance = across * SinDip + (y - CenterY) * CosDip - bend - kink - relay;
            double u = (along - CenterAlong) / LengthHalf;
            double v = (y - CenterY) / HeightHalf;
            double edgeWarp = Math.Sin(u * 4.1 + Phase) * Math.Cos(v * 3.5) * 0.14;
            double footprint = Math.Sqrt(u * u + v * v) + edgeWarp;
            double taper = Clamp(1.0 - footprint * footprint, 0.0, 1.0);
            double pinch = 0.76 + 0.24 * Math.Sin(along * 0.21 + PinchPhase);
            double upwardScale = 0.52 + 0.48 * Clamp((y + 30.0) / 37.0, 0.0, 1.0);
            double halfThickness = Math.Max(0.08, Thickness * 0.5 * pinch * Math.Sqrt(taper) * upwardScale);
            double distance = Math.Abs(signedDistance);
            return new StringerSample(
                footprint <= 1.04 && distance <= halfThickness,
                distance / halfThickness,
                footprint <= 1.12 && distance <= halfThickness + 1.8 * taper);
        }
    }

    private readonly record struct StringerSample(
        bool Inside,
        double Score,
        bool InHalo);

    private readonly struct ExhaliteField
    {
        public readonly double CenterX;
        public readonly double CenterZ;
        public readonly double CenterStratY;
        public readonly double RadiusAlong;
        public readonly double RadiusAcross;
        public readonly double SinStrike;
        public readonly double CosStrike;
        public readonly double Phase;

        public ExhaliteField(
            double centerX,
            double centerZ,
            double centerStratY,
            double radiusAlong,
            double radiusAcross,
            double sinStrike,
            double cosStrike,
            double phase)
        {
            CenterX = centerX;
            CenterZ = centerZ;
            CenterStratY = centerStratY;
            RadiusAlong = radiusAlong;
            RadiusAcross = radiusAcross;
            SinStrike = sinStrike;
            CosStrike = cosStrike;
            Phase = phase;
        }

        public ExhaliteSample Sample(double x, double stratY, double z, MassiveLens[] lenses)
        {
            double dx = x - CenterX;
            double dz = z - CenterZ;
            double along = dx * CosStrike - dz * SinStrike;
            double across = dx * SinStrike + dz * CosStrike;
            double u = along / RadiusAlong;
            double v = across / RadiusAcross;
            double azimuth = Math.Atan2(v, u);
            double edgeWarp = Math.Sin(u * 3.8 + Phase) * Math.Cos(v * 3.2) * 0.13
                + Math.Sin(u * 8.3 - v * 5.1 + Phase * 0.6) * 0.07
                + Math.Sin(azimuth * 5.0 + Phase) * 0.05;
            double footprint = Math.Sqrt(u * u + v * v) + edgeWarp;
            double taper = Clamp((1.12 - footprint) / 0.34, 0.0, 1.0);
            double halfThickness = 0.38 + 0.78 * Math.Sqrt(taper);
            double verticalDistance = Math.Abs(stratY - CenterStratY);
            double edgeNoise = Hash3D(x * 0.73 + Phase, 0.0, z * 0.73);
            double featherThreshold = Clamp((footprint - 0.88) / 0.24, 0.0, 1.0);
            bool insideEdge = footprint <= 0.88 || (footprint <= 1.12 && edgeNoise > featherThreshold);

            double lensProximity = double.PositiveInfinity;
            for (int i = 0; i < lenses.Length; i++)
            {
                MassiveLens lens = lenses[i];
                double lensDx = x - lens.CenterX;
                double lensDz = z - lens.CenterZ;
                double lensAlong = lensDx * lens.CosStrike - lensDz * lens.SinStrike;
                double lensAcross = lensDx * lens.SinStrike + lensDz * lens.CosStrike;
                double proximity = Math.Sqrt(
                    (lensAlong * lensAlong) / (lens.RadiusAlong * lens.RadiusAlong)
                    + (lensAcross * lensAcross) / (lens.RadiusAcross * lens.RadiusAcross));
                lensProximity = Math.Min(lensProximity, proximity);
            }

            return new ExhaliteSample(
                insideEdge && verticalDistance <= halfThickness,
                footprint,
                lensProximity,
                along,
                across);
        }
    }

    private readonly record struct ExhaliteSample(
        bool Inside,
        double Footprint,
        double LensProximity,
        double Along,
        double Across);
}

internal sealed class BimodalFelsicVmsProceduralTemplate : IProceduralDepositTemplate
{
    private static readonly string[] RequiredSlots =
    {
        ProceduralMaterialSlots.Chalcopyrite,
        ProceduralMaterialSlots.Sphalerite,
        ProceduralMaterialSlots.Galena,
        ProceduralMaterialSlots.Pyrite,
        ProceduralMaterialSlots.Barite,
        ProceduralMaterialSlots.Quartz,
        ProceduralMaterialSlots.Chert,
        ProceduralMaterialSlots.ChertHematite,
        ProceduralMaterialSlots.ChertMagnetite,
        ProceduralMaterialSlots.ChertPyrite,
        ProceduralMaterialSlots.Chalcocite,
        ProceduralMaterialSlots.Smithsonite,
        ProceduralMaterialSlots.Cerussite,
        ProceduralMaterialSlots.Limonite,
        ProceduralMaterialSlots.Gossan,
        ProceduralMaterialSlots.YellowGossan,
        ProceduralMaterialSlots.GreyGossan
    };

    public string Code => "bimodalFelsicVms";
    public IReadOnlyList<string> RequiredMaterialSlots => RequiredSlots;
    public bool RequiresPlan => true;

    public int GetMaximumHorizontalReach(ProceduralDepositDefinition definition)
    {
        return definition.BimodalFelsicVms.HorizontalRadius;
    }

    public object CreatePlan(in ProceduralDepositInstance instance, ProceduralDepositDefinition definition)
    {
        return BimodalFelsicVmsPlan.Create(instance, definition.BimodalFelsicVms);
    }

    public bool Validate(ProceduralDepositDefinition definition, out string error)
    {
        BimodalFelsicVmsDefinition settings = definition.BimodalFelsicVms;
        bool valid = settings.HorizontalRadius >= 16
            && settings.VerticalHalfHeight >= 8
            && settings.SupergeneDepth >= 1;
        error = valid ? string.Empty : "invalid bimodal felsic vms settings";
        return valid;
    }

    public void Realize(
        ProceduralDepositWorldGenSystem system,
        in DepositCandidate candidate,
        IChunkColumnGenerateRequest request,
        int baseX,
        int baseZ)
    {
        system.RealizeBimodalFelsicVmsCandidate(candidate, request, baseX, baseZ);
    }
}

public sealed partial class ProceduralDepositWorldGenSystem
{
    internal void RealizeBimodalFelsicVmsCandidate(
        DepositCandidate candidate,
        IChunkColumnGenerateRequest request,
        int baseX,
        int baseZ)
    {
        CompiledProceduralDeposit compiled = candidate.Compiled;
        BimodalFelsicVmsDefinition settings = compiled.Definition.BimodalFelsicVms;
        BimodalFelsicVmsPlan plan = (BimodalFelsicVmsPlan)candidate.Plan!;
        ushort[] heightMap = request.Chunks[0].MapChunk.WorldGenTerrainHeightMap;
        int worldHeight = serverApi!.World.BlockAccessor.MapSizeY;
        int minimumY = Math.Clamp(candidate.Instance.CenterY - settings.VerticalHalfHeight, 1, worldHeight - 2);
        int maximumPrototypeY = Math.Clamp(candidate.Instance.CenterY + settings.VerticalHalfHeight, 1, worldHeight - 2);

        int[] zoneSlots = BuildBimodalFelsicVmsZoneSlots(compiled);
        int gossanSlot = compiled.GetSlotId(ProceduralMaterialSlots.Gossan);
        int yellowGossanSlot = compiled.GetSlotId(ProceduralMaterialSlots.YellowGossan);
        int greyGossanSlot = compiled.GetSlotId(ProceduralMaterialSlots.GreyGossan);
        bool weatheringEnabled = compiled.Definition.Weathering.Enabled;

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
                    BimodalFelsicVmsSample sample = plan.Evaluate(worldX, y, worldZ, surfaceY, weatheringEnabled);
                    if (sample.Zone == BimodalFelsicVmsZone.None) continue;

                    int chunkY = y / ChunkSize;
                    if ((uint)chunkY >= (uint)request.Chunks.Length) continue;
                    int localY = y % ChunkSize;
                    int index3d = ((localY * ChunkSize) + localZ) * ChunkSize + localX;
                    IChunkBlocks data = request.Chunks[chunkY].Data;
                    int hostBlockId = data.GetBlockIdUnsafe(index3d);

                    int targetSlot = zoneSlots[(int)sample.Zone];
                    if (targetSlot < 0) continue;

                    bool isSoil = targetSlot == gossanSlot || targetSlot == yellowGossanSlot || targetSlot == greyGossanSlot;
                    if (isSoil)
                    {
                        if (!CanPlaceWeatheredSoil(compiled, hostBlockId)) continue;
                    }
                    else if (!CanReplaceWithProceduralRock(compiled, hostBlockId))
                    {
                        continue;
                    }

                    int placeBlockId = isSoil
                        ? compiled.ResolveWeatheredBlock(targetSlot, sample.Grade, hostBlockId, y < surfaceY)
                        : compiled.ResolveBlock(targetSlot, sample.Grade, hostBlockId);
                    if (placeBlockId == 0 || placeBlockId == hostBlockId) continue;

                    data.SetBlockUnsafe(index3d, placeBlockId);
                    data.SetFluid(index3d, 0);
                }
            }
        }
    }

    private static int[] BuildBimodalFelsicVmsZoneSlots(CompiledProceduralDeposit compiled)
    {
        var slots = new int[Enum.GetValues<BimodalFelsicVmsZone>().Length];
        Array.Fill(slots, -1);
        slots[(int)BimodalFelsicVmsZone.Chalcopyrite] = compiled.GetSlotId(ProceduralMaterialSlots.Chalcopyrite);
        slots[(int)BimodalFelsicVmsZone.Sphalerite] = compiled.GetSlotId(ProceduralMaterialSlots.Sphalerite);
        slots[(int)BimodalFelsicVmsZone.Galena] = compiled.GetSlotId(ProceduralMaterialSlots.Galena);
        slots[(int)BimodalFelsicVmsZone.Pyrite] = compiled.GetSlotId(ProceduralMaterialSlots.Pyrite);
        slots[(int)BimodalFelsicVmsZone.Barite] = compiled.GetSlotId(ProceduralMaterialSlots.Barite);
        slots[(int)BimodalFelsicVmsZone.Quartz] = compiled.GetSlotId(ProceduralMaterialSlots.Quartz);
        slots[(int)BimodalFelsicVmsZone.Chert] = compiled.GetSlotId(ProceduralMaterialSlots.Chert);
        slots[(int)BimodalFelsicVmsZone.ChertHematite] = compiled.GetSlotId(ProceduralMaterialSlots.ChertHematite);
        slots[(int)BimodalFelsicVmsZone.ChertMagnetite] = compiled.GetSlotId(ProceduralMaterialSlots.ChertMagnetite);
        slots[(int)BimodalFelsicVmsZone.ChertPyrite] = compiled.GetSlotId(ProceduralMaterialSlots.ChertPyrite);
        slots[(int)BimodalFelsicVmsZone.Chalcocite] = compiled.GetSlotId(ProceduralMaterialSlots.Chalcocite);
        slots[(int)BimodalFelsicVmsZone.Smithsonite] = compiled.GetSlotId(ProceduralMaterialSlots.Smithsonite);
        slots[(int)BimodalFelsicVmsZone.Cerussite] = compiled.GetSlotId(ProceduralMaterialSlots.Cerussite);
        slots[(int)BimodalFelsicVmsZone.Limonite] = compiled.GetSlotId(ProceduralMaterialSlots.Limonite);
        slots[(int)BimodalFelsicVmsZone.Gossan] = compiled.GetSlotId(ProceduralMaterialSlots.Gossan);
        slots[(int)BimodalFelsicVmsZone.YellowGossan] = compiled.GetSlotId(ProceduralMaterialSlots.YellowGossan);
        slots[(int)BimodalFelsicVmsZone.GreyGossan] = compiled.GetSlotId(ProceduralMaterialSlots.GreyGossan);
        return slots;
    }
}
