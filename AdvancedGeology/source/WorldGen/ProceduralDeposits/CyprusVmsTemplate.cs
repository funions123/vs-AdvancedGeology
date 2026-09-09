using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Vintagestory.API.Server;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace AdvancedGeology.WorldGen.ProceduralDeposits;

public enum CyprusVmsZone
{
    None = 0,
    Chalcopyrite,
    Sphalerite,
    Pyrite,
    Quartz,
    Umber,
    Chert,
    ChertHematite,
    ChertMagnetite,
    Gossan,
    YellowGossan,
    Chalcocite,
    Smithsonite,
    Limonite
}

[JsonObject(MemberSerialization.OptIn)]
public sealed class CyprusVmsDefinition
{
    [JsonProperty]
    public int HorizontalRadius { get; set; } = 42;

    [JsonProperty]
    public int VerticalHalfHeight { get; set; } = 35;

    [JsonProperty]
    public int SupergeneDepth { get; set; } = 8;
}

public readonly record struct CyprusVmsSample(
    CyprusVmsZone Zone,
    int Grade);

/// <summary>
/// Chalcopyrite, sphalerite, pyrite, quartz, umber, mineralized chert, and secondary oxide minerals form compact sulfide mounds above steep feeder stockworks and an exhalative apron.
/// </summary>
internal sealed class CyprusVmsPlan
{
    private const ulong PlanSalt = 0x435950525553564DUL; // "CYPRUSVM"
    public const double DepositHorizon = 7.0;

    private readonly ulong featureId;
    private readonly int originX;
    private readonly int originY;
    private readonly int originZ;
    private readonly int horizontalRadius;
    private readonly int verticalHalfHeight;
    private readonly int supergeneDepth;
    private readonly double seed;
    private readonly double regionalStrikeDeg;
    private readonly double bedDipDeg;
    private readonly double sinRegionalStrike;
    private readonly double cosRegionalStrike;
    private readonly double tanBedDip;
    private readonly double feederX;
    private readonly double feederZ;
    private readonly double pipeDepth;
    private readonly CyprusMassiveLens[] massiveLenses;
    private readonly CyprusExhaliteField exhaliteField;
    private readonly CyprusStringer[] stringers;

    public int OriginX => originX;
    public int OriginY => originY;
    public int OriginZ => originZ;
    public double Seed => seed;
    public double RegionalStrikeDeg => regionalStrikeDeg;
    public double BedDipDeg => bedDipDeg;
    public int LensCount => massiveLenses.Length;
    public int StringerCount => stringers.Length;

    private CyprusVmsPlan(
        ulong featureId,
        in ProceduralDepositInstance instance,
        CyprusVmsDefinition settings,
        double seed,
        double regionalStrikeDeg,
        double bedDipDeg,
        double sinRegionalStrike,
        double cosRegionalStrike,
        double tanBedDip,
        double feederX,
        double feederZ,
        double pipeDepth,
        CyprusMassiveLens[] massiveLenses,
        CyprusExhaliteField exhaliteField,
        CyprusStringer[] stringers)
    {
        this.featureId = featureId;
        originX = instance.CenterX;
        originY = instance.CenterY;
        originZ = instance.CenterZ;
        horizontalRadius = settings.HorizontalRadius;
        verticalHalfHeight = settings.VerticalHalfHeight;
        supergeneDepth = settings.SupergeneDepth;
        this.seed = seed;
        this.regionalStrikeDeg = regionalStrikeDeg;
        this.bedDipDeg = bedDipDeg;
        this.sinRegionalStrike = sinRegionalStrike;
        this.cosRegionalStrike = cosRegionalStrike;
        this.tanBedDip = tanBedDip;
        this.feederX = feederX;
        this.feederZ = feederZ;
        this.pipeDepth = pipeDepth;
        this.massiveLenses = massiveLenses;
        this.exhaliteField = exhaliteField;
        this.stringers = stringers;
    }

    public static CyprusVmsPlan Create(
        in ProceduralDepositInstance instance,
        CyprusVmsDefinition settings)
    {
        var random = new ProceduralDepositRandom(instance.FeatureId ^ PlanSalt);

        // Parameter draw order and ranges:
        double regionalStrikeDeg = random.Range(8.0, 34.0);
        double bedDipDeg = random.Range(2.0, 7.0);
        double feederX = random.Range(-3.0, 3.0);
        double feederZ = random.Range(-3.0, 3.0);
        double pipeDepth = random.Range(33.0, 40.0);
        double seed = random.Range(0.0, 1.0) * 100.0;

        double regionalStrikeRad = regionalStrikeDeg * Math.PI / 180.0;
        double sinRegionalStrike = Math.Sin(regionalStrikeRad);
        double cosRegionalStrike = Math.Cos(regionalStrikeRad);
        double tanBedDip = Math.Tan(bedDipDeg * Math.PI / 180.0);

        int lensCount = random.Range(0.0, 1.0) < 0.68 ? 1 : 2;
        var lenses = new CyprusMassiveLens[lensCount];
        for (int i = 0; i < lensCount; i++)
        {
            double satelliteOffset = i == 0 ? 0.0 : random.Range(11.0, 18.0) * (random.Range(0.0, 1.0) < 0.5 ? -1.0 : 1.0);
            double centerX = feederX + satelliteOffset + random.Range(-2.0, 2.0);
            double centerZ = feederZ + random.Range(-5.0, 5.0);
            double centerStratY = DepositHorizon + random.Range(-0.8, 1.2);
            double radiusAlong = i == 0 ? random.Range(15.0, 21.0) : random.Range(8.0, 12.0);
            double radiusAcross = i == 0 ? random.Range(9.0, 13.0) : random.Range(5.0, 8.0);
            double radiusVertical = i == 0 ? random.Range(4.5, 7.0) : random.Range(2.5, 4.0);
            double strikeDeg = regionalStrikeDeg + random.Range(-8.0, 8.0);
            double phase = seed + i * 17.0;
            double lowerCuBias = random.Range(-0.12, 0.16);

            lenses[i] = CyprusMassiveLens.Create(
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

        CyprusMassiveLens mainLens = lenses[0];
        var exhaliteField = CyprusExhaliteField.Create(
            mainLens.CenterX,
            mainLens.CenterZ,
            DepositHorizon + random.Range(1.0, 1.8),
            random.Range(29.0, 38.0),
            random.Range(17.0, 23.0),
            mainLens.SinStrike,
            mainLens.CosStrike,
            seed + 211.0);

        int principalStringerCount = 16 + (int)(random.Range(0.0, 1.0) * 7.0);
        if (principalStringerCount > 22) principalStringerCount = 22;

        double offset = random.Range(-13.0, -10.0);
        var stringerList = new List<CyprusStringer>(principalStringerCount + 8);
        for (int i = 0; i < principalStringerCount; i++)
        {
            if (i > 0) offset += random.Range(1.0, 1.9);
            stringerList.Add(CyprusStringer.Create(
                regionalStrikeDeg + random.Range(-42.0, 42.0),
                random.Range(60.0, 88.0),
                offset,
                random.Range(-8.0, 8.0),
                random.Range(-8.0, 1.0),
                random.Range(8.0, 18.0),
                random.Range(13.0, 27.0),
                random.Range(0.38, 0.90),
                seed + 61.0 + i * 9.0,
                random.Range(-7.0, 7.0),
                random.Range(1.8, 4.0),
                random.Range(-0.8, 0.8),
                seed + i * 5.1,
                false));
        }

        int branchCount = 4 + (int)(random.Range(0.0, 1.0) * 4.0);
        if (branchCount > 7) branchCount = 7;
        for (int i = 0; i < branchCount; i++)
        {
            CyprusStringer parent = stringerList[(i * 3 + 2) % principalStringerCount];
            double direction = i % 2 == 0 ? -1.0 : 1.0;
            stringerList.Add(CyprusStringer.Create(
                parent.StrikeDeg + direction * random.Range(18.0, 36.0),
                parent.DipDeg + random.Range(-12.0, 10.0),
                parent.Offset + direction * random.Range(1.0, 2.8),
                parent.CenterAlong + direction * random.Range(3.0, 7.0),
                parent.CenterY + random.Range(2.0, 7.0),
                random.Range(5.0, 11.0),
                random.Range(7.0, 14.0),
                random.Range(0.30, 0.65),
                seed + 131.0 + i * 12.0,
                random.Range(-4.0, 4.0),
                random.Range(1.5, 3.0),
                random.Range(-0.6, 0.6),
                seed + 81.0 + i * 7.0,
                true));
        }

        return new CyprusVmsPlan(
            instance.FeatureId,
            instance,
            settings,
            seed,
            regionalStrikeDeg,
            bedDipDeg,
            sinRegionalStrike,
            cosRegionalStrike,
            tanBedDip,
            feederX,
            feederZ,
            pipeDepth,
            lenses,
            exhaliteField,
            stringerList.ToArray());
    }

    public double GetStratigraphicY(double x, double y, double z)
    {
        double acrossStrike = x * sinRegionalStrike + z * cosRegionalStrike;
        double warp = Math.Sin(x * 0.055 + seed) * 1.3 + Math.Cos(z * 0.06 - seed * 0.5) * 1.0;
        return y - tanBedDip * acrossStrike - warp;
    }

    public CyprusVmsSample Evaluate(int worldX, int worldY, int worldZ, int surfaceY = int.MaxValue, bool weatheringEnabled = true)
    {
        if (worldY > surfaceY)
        {
            return new CyprusVmsSample(CyprusVmsZone.None, 0);
        }

        double x = worldX - originX;
        double y = worldY - originY;
        double z = worldZ - originZ;

    // Local model bounds: package bounded at y < -35
        if (y < -35.0)
        {
            return new CyprusVmsSample(CyprusVmsZone.None, 0);
        }

        double stratY = GetStratigraphicY(x, y, z);
        bool isFootwall = stratY < DepositHorizon;
        double depthBelowHorizon = DepositHorizon - stratY;
        double fineGrain = Hash3D(x * 2.1 + seed, y * 2.1, z * 2.1);

        LensSample? bestLens = null;
        for (int i = 0; i < massiveLenses.Length; i++)
        {
            LensSample sample = massiveLenses[i].Sample(x, stratY, z);
            if (sample.Inside && (!bestLens.HasValue || sample.Radius < bestLens.Value.Radius))
            {
                bestLens = sample;
            }
        }

        int depth = surfaceY < int.MaxValue ? surfaceY - worldY + 1 : 999;

        if (bestLens.HasValue)
        {
            double facies = Math.Sin(bestLens.Value.AlongNorm * 4.2 + bestLens.Value.Phase)
                + 0.55 * Math.Cos(bestLens.Value.AcrossNorm * 4.7 - bestLens.Value.Phase * 0.4)
                + 0.38 * Math.Sin(bestLens.Value.VerticalNorm * 5.0 + bestLens.Value.AlongNorm * 1.5);
            bool inBasalCore = bestLens.Value.VerticalNorm < 0.0 + bestLens.Value.LowerCuBias && bestLens.Value.LateralNorm < 0.72;

            CyprusVmsZone hypogeneZone;
            if (inBasalCore)
            {
                hypogeneZone = facies > -0.35 ? CyprusVmsZone.Chalcopyrite : CyprusVmsZone.Pyrite;
            }
            else if (bestLens.Value.VerticalNorm > 0.28 && bestLens.Value.LateralNorm > 0.42 && facies > 0.78)
            {
                hypogeneZone = CyprusVmsZone.Sphalerite;
            }
            else if (facies > 0.94 && bestLens.Value.VerticalNorm < 0.30)
            {
                hypogeneZone = CyprusVmsZone.Chalcopyrite;
            }
            else
            {
                hypogeneZone = CyprusVmsZone.Pyrite;
            }

            return ApplySupergene(hypogeneZone, depth, weatheringEnabled);
        }

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
            if (stringerFacies > 0.20)
            {
                return ApplySupergene(CyprusVmsZone.Chalcopyrite, depth, weatheringEnabled);
            }
            if (stringerFacies > -0.48)
            {
                return ApplySupergene(CyprusVmsZone.Pyrite, depth, weatheringEnabled);
            }
            // Quartz stringer stays unaltered at all depths
            return new CyprusVmsSample(CyprusVmsZone.Quartz, 0);
        }

        // Thin Fe-Mn oxide and silica-rich sediment spread away from the vent mound.
        ExhaliteSample exhalite = exhaliteField.Sample(x, stratY, z, massiveLenses);
        if (exhalite.Inside)
        {
            double chemicalBand = Math.Sin(exhalite.Along * 0.25 + exhalite.Across * 0.18 + seed);
            double faciesPatch = Math.Cos(exhalite.Along * 0.13 - exhalite.Across * 0.16 + seed * 0.6);
            if (chemicalBand > 0.42)
            {
                // Hematitic chert/jasper: 20% speckled poor hematite in chert, 80% chert rock.
                // fineGrain is uniform on [0, 1) from Hash3D; fineGrain >= 0.80 selects exactly the top 20%
                // of the distribution for ore speckles, while the remaining 80% writes the chert rock matrix.
                // Chert-hosted speckle facies never weather.
                CyprusVmsZone jasperZone = fineGrain >= 0.80 ? CyprusVmsZone.ChertHematite : CyprusVmsZone.Chert;
                return new CyprusVmsSample(jasperZone, 0);
            }
            if (chemicalBand < -0.48 && faciesPatch > -0.18)
            {
                // Magnetite-rich chert: 20% speckled poor magnetite in chert, 80% chert rock.
                // fineGrain is uniform on [0, 1) from Hash3D; fineGrain >= 0.80 selects exactly the top 20%
                // of the distribution for ore speckles, while the remaining 80% writes the chert rock matrix.
                // Chert-hosted speckle facies never weather.
                CyprusVmsZone magChertZone = fineGrain >= 0.80 ? CyprusVmsZone.ChertMagnetite : CyprusVmsZone.Chert;
                return new CyprusVmsSample(magChertZone, 0);
            }
            // Fe-Mn umber rock: unaltered at all depths
            return new CyprusVmsSample(CyprusVmsZone.Umber, 0);
        }

        if (isFootwall && depthBelowHorizon >= 0.0 && depthBelowHorizon < pipeDepth)
        {
            double progressUp = 1.0 - depthBelowHorizon / pipeDepth;
            double axisX = feederX + Math.Sin(stratY * 0.12 + seed) * 3.0;
            double axisZ = feederZ + Math.Cos(stratY * 0.10 + seed * 0.8) * 2.6;
            double dx = x - axisX;
            double dz = z - axisZ;
            double pipeDistance = Math.Sqrt(dx * dx + dz * dz);
            double coreRadius = 3.2 + progressUp * 6.2 + Math.Sin(stratY * 0.22 + seed) * 0.8;
            double haloRadius = coreRadius + 4.5 * (0.45 + progressUp * 0.55);

            if (bestStringer.HasValue && bestStringer.Value.InHalo && pipeDistance < haloRadius + 2.0)
            {
                return new CyprusVmsSample(CyprusVmsZone.None, 0);
            }
            if (pipeDistance < coreRadius || pipeDistance < haloRadius)
            {
                return new CyprusVmsSample(CyprusVmsZone.None, 0);
            }
        }

        return new CyprusVmsSample(CyprusVmsZone.None, 0);
    }

    private CyprusVmsSample ApplySupergene(CyprusVmsZone hypogeneZone, int depth, bool weatheringEnabled)
    {
        if (!weatheringEnabled)
        {
            int defaultGrade = (hypogeneZone == CyprusVmsZone.Chalcopyrite || hypogeneZone == CyprusVmsZone.Sphalerite) ? 1 : 0;
            return new CyprusVmsSample(hypogeneZone, defaultGrade);
        }

        switch (hypogeneZone)
        {
            case CyprusVmsZone.Chalcopyrite:
                if (depth >= 1 && depth <= 3)
                {
                    return new CyprusVmsSample(CyprusVmsZone.Gossan, 0);
                }
                if (depth >= 4 && depth <= supergeneDepth)
                {
                    // Chalcocite: bountiful grade at depth 6-7, medium grade at 4, 5, 8
                    int grade = (depth == 6 || depth == 7) ? 3 : 1;
                    return new CyprusVmsSample(CyprusVmsZone.Chalcocite, grade);
                }
                return new CyprusVmsSample(CyprusVmsZone.Chalcopyrite, 1);

            case CyprusVmsZone.Sphalerite:
                if (depth >= 1 && depth <= 3)
                {
                    return new CyprusVmsSample(CyprusVmsZone.YellowGossan, 0);
                }
                if (depth >= 4 && depth <= supergeneDepth)
                {
                    return new CyprusVmsSample(CyprusVmsZone.Smithsonite, 1);
                }
                return new CyprusVmsSample(CyprusVmsZone.Sphalerite, 1);

            case CyprusVmsZone.Pyrite:
                if (depth >= 1 && depth <= 3)
                {
                    return new CyprusVmsSample(CyprusVmsZone.Gossan, 0);
                }
                if (depth >= 4 && depth <= supergeneDepth)
                {
                    return new CyprusVmsSample(CyprusVmsZone.Limonite, 1);
                }
                return new CyprusVmsSample(CyprusVmsZone.Pyrite, 0);

            default:
                return new CyprusVmsSample(hypogeneZone, 0);
        }
    }

    internal static double Hash3D(double x, double y, double z)
    {
        double n = Math.Sin(x * 12.9898 + y * 78.233 + z * 37.719) * 43758.5453;
        return n - Math.Floor(n);
    }

    private readonly struct CyprusMassiveLens
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

        private CyprusMassiveLens(
            double centerX,
            double centerZ,
            double centerStratY,
            double radiusAlong,
            double radiusAcross,
            double radiusVertical,
            double strikeDeg,
            double sinStrike,
            double cosStrike,
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
            SinStrike = sinStrike;
            CosStrike = cosStrike;
            Phase = phase;
            LowerCuBias = lowerCuBias;
        }

        public static CyprusMassiveLens Create(
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
            double strikeRad = strikeDeg * Math.PI / 180.0;
            return new CyprusMassiveLens(
                centerX,
                centerZ,
                centerStratY,
                radiusAlong,
                radiusAcross,
                radiusVertical,
                strikeDeg,
                Math.Sin(strikeRad),
                Math.Cos(strikeRad),
                phase,
                lowerCuBias);
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

    private readonly struct LensSample
    {
        public readonly bool Inside;
        public readonly double Radius;
        public readonly double VerticalNorm;
        public readonly double LateralNorm;
        public readonly double AlongNorm;
        public readonly double AcrossNorm;
        public readonly double Phase;
        public readonly double LowerCuBias;

        public LensSample(
            bool inside,
            double radius,
            double verticalNorm,
            double lateralNorm,
            double alongNorm,
            double acrossNorm,
            double phase,
            double lowerCuBias)
        {
            Inside = inside;
            Radius = radius;
            VerticalNorm = verticalNorm;
            LateralNorm = lateralNorm;
            AlongNorm = alongNorm;
            AcrossNorm = acrossNorm;
            Phase = phase;
            LowerCuBias = lowerCuBias;
        }
    }

    private readonly struct CyprusStringer
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

        private CyprusStringer(
            double strikeDeg,
            double dipDeg,
            double sinStrike,
            double cosStrike,
            double sinDip,
            double cosDip,
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
            SinStrike = sinStrike;
            CosStrike = cosStrike;
            SinDip = sinDip;
            CosDip = cosDip;
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

        public static CyprusStringer Create(
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
            double strikeRad = strikeDeg * Math.PI / 180.0;
            double dipRad = dipDeg * Math.PI / 180.0;
            return new CyprusStringer(
                strikeDeg,
                dipDeg,
                Math.Sin(strikeRad),
                Math.Cos(strikeRad),
                Math.Sin(dipRad),
                Math.Cos(dipRad),
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
                isBranch);
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
            double taper = Math.Clamp(1.0 - footprint * footprint, 0.0, 1.0);
            double pinch = 0.76 + 0.24 * Math.Sin(along * 0.21 + PinchPhase);
            double upwardScale = 0.52 + 0.48 * Math.Clamp((y + 30.0) / 37.0, 0.0, 1.0);
            double halfThickness = Math.Max(0.08, Thickness * 0.5 * pinch * Math.Sqrt(taper) * upwardScale);
            double distance = Math.Abs(signedDistance);
            return new StringerSample(
                footprint <= 1.04 && distance <= halfThickness,
                distance / halfThickness,
                footprint <= 1.12 && distance <= halfThickness + 1.8 * taper);
        }
    }

    private readonly struct StringerSample
    {
        public readonly bool Inside;
        public readonly double Score;
        public readonly bool InHalo;

        public StringerSample(bool inside, double score, bool inHalo)
        {
            Inside = inside;
            Score = score;
            InHalo = inHalo;
        }
    }

    private readonly struct CyprusExhaliteField
    {
        public readonly double CenterX;
        public readonly double CenterZ;
        public readonly double CenterStratY;
        public readonly double RadiusAlong;
        public readonly double RadiusAcross;
        public readonly double SinStrike;
        public readonly double CosStrike;
        public readonly double Phase;

        private CyprusExhaliteField(
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

        public static CyprusExhaliteField Create(
            double centerX,
            double centerZ,
            double centerStratY,
            double radiusAlong,
            double radiusAcross,
            double sinStrike,
            double cosStrike,
            double phase)
        {
            return new CyprusExhaliteField(
                centerX,
                centerZ,
                centerStratY,
                radiusAlong,
                radiusAcross,
                sinStrike,
                cosStrike,
                phase);
        }

        public ExhaliteSample Sample(double x, double stratY, double z, CyprusMassiveLens[] massiveLenses)
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
            double taper = Math.Clamp((1.12 - footprint) / 0.34, 0.0, 1.0);
            double halfThickness = 0.38 + 0.78 * Math.Sqrt(taper);
            double verticalDistance = Math.Abs(stratY - CenterStratY);
            double edgeNoise = Hash3D(x * 0.73 + Phase, 0.0, z * 0.73);
            double featherThreshold = Math.Clamp((footprint - 0.88) / 0.24, 0.0, 1.0);
            bool insideEdge = footprint <= 0.88 || (footprint <= 1.12 && edgeNoise > featherThreshold);

            double lensProximity = double.PositiveInfinity;
            for (int i = 0; i < massiveLenses.Length; i++)
            {
                CyprusMassiveLens lens = massiveLenses[i];
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

    private readonly struct ExhaliteSample
    {
        public readonly bool Inside;
        public readonly double Footprint;
        public readonly double LensProximity;
        public readonly double Along;
        public readonly double Across;

        public ExhaliteSample(
            bool inside,
            double footprint,
            double lensProximity,
            double along,
            double across)
        {
            Inside = inside;
            Footprint = footprint;
            LensProximity = lensProximity;
            Along = along;
            Across = across;
        }
    }
}

internal sealed class CyprusVmsProceduralTemplate : IProceduralDepositTemplate
{
    private static readonly string[] RequiredSlots =
    {
        ProceduralMaterialSlots.Chalcopyrite,
        ProceduralMaterialSlots.Sphalerite,
        ProceduralMaterialSlots.Pyrite,
        ProceduralMaterialSlots.Quartz,
        ProceduralMaterialSlots.Umber,
        ProceduralMaterialSlots.Chert,
        ProceduralMaterialSlots.ChertHematite,
        ProceduralMaterialSlots.ChertMagnetite,
        ProceduralMaterialSlots.Gossan,
        ProceduralMaterialSlots.YellowGossan,
        ProceduralMaterialSlots.Chalcocite,
        ProceduralMaterialSlots.Smithsonite,
        ProceduralMaterialSlots.Limonite
    };

    public string Code => "cyprusVms";
    public IReadOnlyList<string> RequiredMaterialSlots => RequiredSlots;
    public bool RequiresPlan => true;

    public int GetMaximumHorizontalReach(ProceduralDepositDefinition definition)
    {
        return definition.CyprusVms.HorizontalRadius;
    }

    public object CreatePlan(in ProceduralDepositInstance instance, ProceduralDepositDefinition definition)
    {
        return CyprusVmsPlan.Create(instance, definition.CyprusVms);
    }

    public bool Validate(ProceduralDepositDefinition definition, out string error)
    {
        CyprusVmsDefinition settings = definition.CyprusVms;
        bool valid = settings.HorizontalRadius >= 16
            && settings.VerticalHalfHeight >= 8
            && settings.SupergeneDepth >= 1;
        error = valid ? string.Empty : "invalid cyprus vms settings";
        return valid;
    }

    public void Realize(
        ProceduralDepositWorldGenSystem system,
        in DepositCandidate candidate,
        IChunkColumnGenerateRequest request,
        int baseX,
        int baseZ)
    {
        system.RealizeCyprusVmsCandidate(candidate, request, baseX, baseZ);
    }
}

public sealed partial class ProceduralDepositWorldGenSystem
{
    internal void RealizeCyprusVmsCandidate(
        DepositCandidate candidate,
        IChunkColumnGenerateRequest request,
        int baseX,
        int baseZ)
    {
        CompiledProceduralDeposit compiled = candidate.Compiled;
        CyprusVmsDefinition settings = compiled.Definition.CyprusVms;
        CyprusVmsPlan plan = (CyprusVmsPlan)candidate.Plan!;
        ushort[] heightMap = request.Chunks[0].MapChunk.WorldGenTerrainHeightMap;
        int worldHeight = serverApi!.World.BlockAccessor.MapSizeY;
        int minimumY = Math.Clamp(candidate.Instance.CenterY - settings.VerticalHalfHeight, 1, worldHeight - 2);
        int maximumPrototypeY = Math.Clamp(candidate.Instance.CenterY + settings.VerticalHalfHeight, 1, worldHeight - 2);

        int[] zoneSlots = BuildCyprusVmsZoneSlots(compiled);
        int gossanSlot = compiled.GetSlotId(ProceduralMaterialSlots.Gossan);
        int yellowGossanSlot = compiled.GetSlotId(ProceduralMaterialSlots.YellowGossan);
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
                    CyprusVmsSample sample = plan.Evaluate(worldX, y, worldZ, surfaceY, weatheringEnabled);
                    if (sample.Zone == CyprusVmsZone.None) continue;

                    int chunkY = y / ChunkSize;
                    if ((uint)chunkY >= (uint)request.Chunks.Length) continue;
                    int localY = y % ChunkSize;
                    int index3d = ((localY * ChunkSize) + localZ) * ChunkSize + localX;
                    IChunkBlocks data = request.Chunks[chunkY].Data;
                    int hostBlockId = data.GetBlockIdUnsafe(index3d);

                    int targetSlot = zoneSlots[(int)sample.Zone];
                    if (targetSlot < 0) continue;

                    bool isSoil = targetSlot == gossanSlot || targetSlot == yellowGossanSlot;
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
                    if (placeBlockId == 0) continue;

                    data.SetBlockUnsafe(index3d, placeBlockId);
                    data.SetFluid(index3d, 0);
                }
            }
        }
    }

    private static int[] BuildCyprusVmsZoneSlots(CompiledProceduralDeposit compiled)
    {
        var slots = new int[Enum.GetValues<CyprusVmsZone>().Length];
        Array.Fill(slots, -1);
        slots[(int)CyprusVmsZone.Chalcopyrite] = compiled.GetSlotId(ProceduralMaterialSlots.Chalcopyrite);
        slots[(int)CyprusVmsZone.Sphalerite] = compiled.GetSlotId(ProceduralMaterialSlots.Sphalerite);
        slots[(int)CyprusVmsZone.Pyrite] = compiled.GetSlotId(ProceduralMaterialSlots.Pyrite);
        slots[(int)CyprusVmsZone.Quartz] = compiled.GetSlotId(ProceduralMaterialSlots.Quartz);
        slots[(int)CyprusVmsZone.Umber] = compiled.GetSlotId(ProceduralMaterialSlots.Umber);
        slots[(int)CyprusVmsZone.Chert] = compiled.GetSlotId(ProceduralMaterialSlots.Chert);
        slots[(int)CyprusVmsZone.ChertHematite] = compiled.GetSlotId(ProceduralMaterialSlots.ChertHematite);
        slots[(int)CyprusVmsZone.ChertMagnetite] = compiled.GetSlotId(ProceduralMaterialSlots.ChertMagnetite);
        slots[(int)CyprusVmsZone.Gossan] = compiled.GetSlotId(ProceduralMaterialSlots.Gossan);
        slots[(int)CyprusVmsZone.YellowGossan] = compiled.GetSlotId(ProceduralMaterialSlots.YellowGossan);
        slots[(int)CyprusVmsZone.Chalcocite] = compiled.GetSlotId(ProceduralMaterialSlots.Chalcocite);
        slots[(int)CyprusVmsZone.Smithsonite] = compiled.GetSlotId(ProceduralMaterialSlots.Smithsonite);
        slots[(int)CyprusVmsZone.Limonite] = compiled.GetSlotId(ProceduralMaterialSlots.Limonite);
        return slots;
    }
}
