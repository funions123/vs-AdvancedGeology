using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Vintagestory.API.Server;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace AdvancedGeology.WorldGen.ProceduralDeposits;

public enum BesshiVmsZone
{
    None = 0,
    Chalcopyrite,
    Sphalerite,
    Pyrite,
    Magnetite,
    Quartz,
    Chert,
    ChertHematite,
    Gossan,
    YellowGossan,
    Chalcocite,
    Smithsonite,
    Limonite
}

[JsonObject(MemberSerialization.OptIn)]
public sealed class BesshiVmsDefinition
{
    [JsonProperty]
    public int HorizontalRadius { get; set; } = 48;

    [JsonProperty]
    public int VerticalHalfHeight { get; set; } = 35;

    [JsonProperty]
    public int SupergeneDepth { get; set; } = 8;
}

public readonly record struct BesshiVmsSample(
    BesshiVmsZone Zone,
    int Grade,
    bool InLens,
    bool InStringer,
    bool InExhalite,
    bool InChloriteFootwall,
    bool InSericiteHalo,
    double GrainNoise);

/// <summary>
/// Chalcopyrite, sphalerite, pyrite, magnetite, quartz, hematitic chert, and secondary oxide minerals form thin folded sulfide sheets, an exhalite horizon, and discordant feeder stringers.
/// </summary>
internal sealed class BesshiVmsPlan
{
    private const ulong PlanSalt = 0x424553534849564DUL; // "BESSHIVM"
    private const double DepositHorizon = 7.0;

    private readonly ulong featureId;
    private readonly int originX;
    private readonly int originY;
    private readonly int originZ;
    private readonly double regionalStrikeDeg;
    private readonly double bedDipDeg;
    private readonly double sinRegionalStrike;
    private readonly double cosRegionalStrike;
    private readonly double tanBedDip;
    private readonly double feederX;
    private readonly double feederZ;
    private readonly double pipeDepth;
    private readonly double seed;
    private readonly BesshiMassiveLens[] massiveLenses;
    private readonly BesshiStringer[] stringers;
    private readonly BesshiExhaliteField exhaliteField;

    public int OriginX => originX;
    public int OriginY => originY;
    public int OriginZ => originZ;
    public double RegionalStrikeDeg => regionalStrikeDeg;
    public double BedDipDeg => bedDipDeg;
    public double FeederX => feederX;
    public double FeederZ => feederZ;
    public double PipeDepth => pipeDepth;
    public double Seed => seed;
    public int MassiveLensCount => massiveLenses.Length;
    public int StringerCount => stringers.Length;

    private BesshiVmsPlan(
        ulong featureId,
        in ProceduralDepositInstance instance,
        double regionalStrikeDeg,
        double bedDipDeg,
        double sinRegionalStrike,
        double cosRegionalStrike,
        double tanBedDip,
        double feederX,
        double feederZ,
        double pipeDepth,
        double seed,
        BesshiMassiveLens[] massiveLenses,
        BesshiStringer[] stringers,
        BesshiExhaliteField exhaliteField)
    {
        this.featureId = featureId;
        originX = instance.CenterX;
        originY = instance.CenterY;
        originZ = instance.CenterZ;
        this.regionalStrikeDeg = regionalStrikeDeg;
        this.bedDipDeg = bedDipDeg;
        this.sinRegionalStrike = sinRegionalStrike;
        this.cosRegionalStrike = cosRegionalStrike;
        this.tanBedDip = tanBedDip;
        this.feederX = feederX;
        this.feederZ = feederZ;
        this.pipeDepth = pipeDepth;
        this.seed = seed;
        this.massiveLenses = massiveLenses;
        this.stringers = stringers;
        this.exhaliteField = exhaliteField;
    }

    public static BesshiVmsPlan Create(
        in ProceduralDepositInstance instance,
        BesshiVmsDefinition settings)
    {
        var random = new ProceduralDepositRandom(instance.FeatureId ^ PlanSalt);

        // Parameter draw order:
        // 1. regionalStrikeDeg (5.0, 30.0)
        double regionalStrikeDeg = random.Range(5.0, 30.0);
        // 2. bedDipDeg (7.0, 16.0)
        double bedDipDeg = random.Range(7.0, 16.0);
        // 3. feederX (-5.0, 5.0)
        double feederX = random.Range(-5.0, 5.0);
        // 4. feederZ (-5.0, 5.0)
        double feederZ = random.Range(-5.0, 5.0);
        // 5. pipeDepth (20.0, 25.0)
        double pipeDepth = random.Range(20.0, 25.0);
        // 6. seed (0.0, 100.0)
        double seed = random.Range(0.0, 100.0);

        double regionalStrikeRad = regionalStrikeDeg * Math.PI / 180.0;
        double sinRegionalStrike = Math.Sin(regionalStrikeRad);
        double cosRegionalStrike = Math.Cos(regionalStrikeRad);
        double tanBedDip = Math.Tan(bedDipDeg * Math.PI / 180.0);

        // 7. massive lenses: count = 3 + floor(random * 3) -> 3..5
        int lensCount = random.NextInt(3, 5);
        var massiveLenses = new BesshiMassiveLens[lensCount];
        for (int i = 0; i < lensCount; i++)
        {
            double centered = i - (lensCount - 1) / 2.0;
            double stepX = random.Range(8.0, 13.0);
            double jitterX = random.Range(-2.5, 2.5);
            double jitterZ = random.Range(-4.0, 4.0);
            double stratYJitter = random.Range(-1.2, 1.5);
            double stratYAlternate = random.Range(1.2, 2.4);
            double rAlong = random.Range(27.0, 40.0);
            double rAcross = random.Range(10.0, 18.0);
            double rVertical = random.Range(1.15, 2.15);
            double strikeJitter = random.Range(-7.0, 7.0);
            double lowerCuBias = random.Range(-0.12, 0.12);

            double centerX = feederX + centered * stepX + jitterX;
            double centerZ = feederZ + centered * jitterZ;
            double centerStratY = DepositHorizon + stratYJitter + (i % 2) * stratYAlternate;
            double strikeDeg = regionalStrikeDeg + strikeJitter;
            double phase = seed + i * 17.0;

            massiveLenses[i] = BesshiMassiveLens.Create(
                centerX,
                centerZ,
                centerStratY,
                rAlong,
                rAcross,
                rVertical,
                strikeDeg,
                phase,
                lowerCuBias);
        }

        // 8. exhalite field
        BesshiMassiveLens mainLens = massiveLenses[massiveLenses.Length / 2];
        double sumX = 0.0;
        double sumZ = 0.0;
        for (int i = 0; i < massiveLenses.Length; i++)
        {
            sumX += massiveLenses[i].CenterX;
            sumZ += massiveLenses[i].CenterZ;
        }
        double meanCenterX = sumX / massiveLenses.Length;
        double meanCenterZ = sumZ / massiveLenses.Length;
        double exCenterStratY = DepositHorizon + random.Range(2.2, 3.2);
        double exRadiusAlong = random.Range(38.0, 48.0);
        double exRadiusAcross = random.Range(20.0, 29.0);
        double exPhase = seed + 211.0;

        var exhaliteField = new BesshiExhaliteField(
            meanCenterX,
            meanCenterZ,
            exCenterStratY,
            exRadiusAlong,
            exRadiusAcross,
            mainLens.SinStrike,
            mainLens.CosStrike,
            exPhase);

        // 9. principal stringers: count = 7 + floor(random * 5) -> 7..11
        int principalStringerCount = random.NextInt(7, 11);
        double stringerOffset = random.Range(-11.0, -8.0);
        var principalList = new List<BesshiStringer>(principalStringerCount);

        for (int i = 0; i < principalStringerCount; i++)
        {
            if (i > 0) stringerOffset += random.Range(2.0, 3.8);
            double sStrikeDeg = regionalStrikeDeg + random.Range(-30.0, 30.0);
            double sDipDeg = random.Range(55.0, 82.0);
            double sCenterAlong = random.Range(-10.0, 10.0);
            double sCenterY = random.Range(-8.0, -1.0);
            double sLengthHalf = random.Range(9.0, 19.0);
            double sHeightHalf = random.Range(11.0, 22.0);
            double sThickness = random.Range(0.35, 0.78);
            double sPhase = seed + 61.0 + i * 9.0;
            double sRelayCenter = random.Range(-8.0, 8.0);
            double sRelayWidth = random.Range(2.0, 4.0);
            double sRelayShift = random.Range(-0.7, 0.7);
            double sPinchPhase = seed + i * 5.1;

            principalList.Add(BesshiStringer.Create(
                sStrikeDeg,
                sDipDeg,
                stringerOffset,
                sCenterAlong,
                sCenterY,
                sLengthHalf,
                sHeightHalf,
                sThickness,
                sPhase,
                sRelayCenter,
                sRelayWidth,
                sRelayShift,
                sPinchPhase,
                false));
        }

        // 10. branch stringers: count = 2 + floor(random * 3) -> 2..4
        int branchCount = random.NextInt(2, 4);
        var allStringers = new List<BesshiStringer>(principalStringerCount + branchCount);
        allStringers.AddRange(principalList);

        for (int i = 0; i < branchCount; i++)
        {
            BesshiStringer parent = principalList[(i * 3 + 1) % principalStringerCount];
            double direction = i % 2 == 0 ? -1.0 : 1.0;
            double bStrikeDeg = parent.StrikeDeg + direction * random.Range(15.0, 28.0);
            double bDipDeg = parent.DipDeg + random.Range(-10.0, 10.0);
            double bOffset = parent.Offset + direction * random.Range(1.0, 2.5);
            double bCenterAlong = parent.CenterAlong + direction * random.Range(3.0, 7.0);
            double bCenterY = parent.CenterY + random.Range(2.0, 6.0);
            double bLengthHalf = random.Range(5.0, 10.0);
            double bHeightHalf = random.Range(7.0, 13.0);
            double bThickness = random.Range(0.28, 0.58);
            double bPhase = seed + 131.0 + i * 12.0;
            double bRelayCenter = random.Range(-4.0, 4.0);
            double bRelayWidth = random.Range(1.5, 3.0);
            double bRelayShift = random.Range(-0.5, 0.5);
            double bPinchPhase = seed + 81.0 + i * 7.0;

            allStringers.Add(BesshiStringer.Create(
                bStrikeDeg,
                bDipDeg,
                bOffset,
                bCenterAlong,
                bCenterY,
                bLengthHalf,
                bHeightHalf,
                bThickness,
                bPhase,
                bRelayCenter,
                bRelayWidth,
                bRelayShift,
                bPinchPhase,
                true));
        }

        return new BesshiVmsPlan(
            instance.FeatureId,
            instance,
            regionalStrikeDeg,
            bedDipDeg,
            sinRegionalStrike,
            cosRegionalStrike,
            tanBedDip,
            feederX,
            feederZ,
            pipeDepth,
            seed,
            massiveLenses,
            allStringers.ToArray(),
            exhaliteField);
    }

    public double GetStratigraphicY(double x, double y, double z)
    {
        double acrossStrike = x * sinRegionalStrike + z * cosRegionalStrike;
        double fold = Math.Sin(acrossStrike * 0.075 + seed) * 3.2
            + Math.Sin(x * 0.035 - z * 0.045 + seed * 0.6) * 1.4;
        return y - tanBedDip * acrossStrike - fold;
    }

    public static double Hash3D(double x, double y, double z)
    {
        double n = Math.Sin(x * 12.9898 + y * 78.233 + z * 37.719) * 43758.5453;
        return n - Math.Floor(n);
    }

    public BesshiVmsSample Evaluate(int worldX, int worldY, int worldZ, int surfaceY = int.MaxValue)
    {
        if (worldY > surfaceY)
        {
            return new BesshiVmsSample(BesshiVmsZone.None, 0, false, false, false, false, false, 0.0);
        }

        double x = worldX - originX;
        double y = worldY - originY;
        double z = worldZ - originZ;

    // Deposit package is bounded at deposit-local y < -35
        if (y < -35.0)
        {
            return new BesshiVmsSample(BesshiVmsZone.None, 0, false, false, false, false, false, 0.0);
        }

        double stratY = GetStratigraphicY(x, y, z);
        bool isFootwall = stratY < DepositHorizon;
        double depthBelowHorizon = DepositHorizon - stratY;
        double fineGrain = Hash3D(x * 2.1 + seed, y * 2.1, z * 2.1);
        int depth = surfaceY < int.MaxValue ? surfaceY - worldY + 1 : 999;

        // 1. Evaluate massive sulfide lenses
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
            double facies = Math.Sin(lens.AlongNorm * 5.2 + lens.Phase)
                + 0.62 * Math.Cos(lens.AcrossNorm * 4.3 - lens.Phase * 0.35)
                + 0.42 * Math.Sin(lens.VerticalNorm * 5.6 + lens.AlongNorm * 1.3);
            bool inLowerCore = lens.VerticalNorm < 0.0 + lens.LowerCuBias && lens.LateralNorm < 0.76;

            if (inLowerCore && facies > 0.12)
            {
                // Primary chalcopyrite core -> supergene: depth 1-3 red gossan, depth 4-8 chalcocite (bountiful at 6-7)
                if (depth <= 3)
                {
                    return new BesshiVmsSample(BesshiVmsZone.Gossan, 0, true, false, false, false, false, fineGrain);
                }
                if (depth <= 8)
                {
                    int grade = (depth == 6 || depth == 7) ? 3 : (fineGrain > 0.65 ? 2 : 1);
                    return new BesshiVmsSample(BesshiVmsZone.Chalcocite, grade, true, false, false, false, false, fineGrain);
                }
                int priGrade = fineGrain > 0.70 ? 2 : (fineGrain > 0.35 ? 1 : 0);
                return new BesshiVmsSample(BesshiVmsZone.Chalcopyrite, priGrade, true, false, false, false, false, fineGrain);
            }

            if (lens.VerticalNorm > 0.18 && facies > 0.72)
            {
                // Upper sphalerite zone -> supergene: depth 1-3 yellow gossan, depth 4-8 smithsonite
                if (depth <= 3)
                {
                    return new BesshiVmsSample(BesshiVmsZone.YellowGossan, 0, true, false, false, false, false, fineGrain);
                }
                if (depth <= 8)
                {
                    int grade = fineGrain > 0.60 ? 2 : 1;
                    return new BesshiVmsSample(BesshiVmsZone.Smithsonite, grade, true, false, false, false, false, fineGrain);
                }
                int priGrade = fineGrain > 0.70 ? 2 : (fineGrain > 0.35 ? 1 : 0);
                return new BesshiVmsSample(BesshiVmsZone.Sphalerite, priGrade, true, false, false, false, false, fineGrain);
            }

            if (facies < -1.05)
            {
                // Magnetite lens facies: unaltered at all depths
                int priGrade = fineGrain > 0.70 ? 2 : (fineGrain > 0.35 ? 1 : 0);
                return new BesshiVmsSample(BesshiVmsZone.Magnetite, priGrade, true, false, false, false, false, fineGrain);
            }

            // Pyrite represents both the pyrite-rich and pyrrhotite-rich sulfide matrix.
            // Sulfide matrix -> supergene: depth 1-3 red gossan, depth 4-8 limonite
            if (depth <= 3)
            {
                return new BesshiVmsSample(BesshiVmsZone.Gossan, 0, true, false, false, false, false, fineGrain);
            }
            if (depth <= 8)
            {
                int grade = fineGrain > 0.60 ? 2 : 1;
                return new BesshiVmsSample(BesshiVmsZone.Limonite, grade, true, false, false, false, false, fineGrain);
            }
            return new BesshiVmsSample(BesshiVmsZone.Pyrite, 0, true, false, false, false, false, fineGrain);
        }

        // 2. Evaluate feeder stringers
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
            double stringerFacies = Math.Sin(x * 0.29 + z * 0.17 + stratY * 0.14 + seed);
            if (stringerFacies > 0.45)
            {
                // Chalcopyrite in stringer
                if (depth <= 3)
                {
                    return new BesshiVmsSample(BesshiVmsZone.Gossan, 0, false, true, false, false, false, fineGrain);
                }
                if (depth <= 8)
                {
                    int grade = (depth == 6 || depth == 7) ? 3 : 1;
                    return new BesshiVmsSample(BesshiVmsZone.Chalcocite, grade, false, true, false, false, false, fineGrain);
                }
                int priGrade = fineGrain > 0.65 ? 2 : 1;
                return new BesshiVmsSample(BesshiVmsZone.Chalcopyrite, priGrade, false, true, false, false, false, fineGrain);
            }

            if (stringerFacies > -0.20)
            {
                // Pyrrhotite in stringer (mapped to pyrite)
                if (depth <= 3)
                {
                    return new BesshiVmsSample(BesshiVmsZone.Gossan, 0, false, true, false, false, false, fineGrain);
                }
                if (depth <= 8)
                {
                    return new BesshiVmsSample(BesshiVmsZone.Limonite, 1, false, true, false, false, false, fineGrain);
                }
                return new BesshiVmsSample(BesshiVmsZone.Pyrite, 0, false, true, false, false, false, fineGrain);
            }

            // Quartz stringer veinlet: unaltered at all depths
            return new BesshiVmsSample(BesshiVmsZone.Quartz, 0, false, true, false, false, false, fineGrain);
        }

        // 3. Evaluate exhalite marker horizon
        ExhaliteSample exhalite = exhaliteField.Sample(x, stratY, z);
        if (exhalite.Inside)
        {
            double chemicalBand = Math.Sin(exhalite.Along * 0.23 + exhalite.Across * 0.15 + seed);
            if (chemicalBand < -0.52)
            {
                // Magnetite band in exhalite: unaltered at all depths
                int magGrade = fineGrain > 0.65 ? 2 : 1;
                return new BesshiVmsSample(BesshiVmsZone.Magnetite, magGrade, false, false, true, false, false, fineGrain);
            }

            // Fe-Mn chert exhalite: 20% speckled poor hematite in chert when fineGrain >= 0.80,
            // 80% chert rock when fineGrain < 0.80.
            // FineGrain is uniform on [0, 1) from Hash3D; threshold >= 0.80 selects exactly 20.0% of voxels.
            // Chert-hosted speckles and chert remain unaltered at all depths.
            if (fineGrain >= 0.80)
            {
                return new BesshiVmsSample(BesshiVmsZone.ChertHematite, 0, false, false, true, false, false, fineGrain);
            }
            return new BesshiVmsSample(BesshiVmsZone.Chert, 0, false, false, true, false, false, fineGrain);
        }

        if (isFootwall && depthBelowHorizon >= 0.0 && depthBelowHorizon < pipeDepth)
        {
            double progressUp = 1.0 - depthBelowHorizon / pipeDepth;
            double axisX = feederX + Math.Sin(stratY * 0.12 + seed) * 3.0;
            double axisZ = feederZ + Math.Cos(stratY * 0.10 + seed * 0.8) * 2.6;
            double dx = x - axisX;
            double dz = z - axisZ;
            double pipeDistance = Math.Sqrt(dx * dx + dz * dz);
            double coreRadius = 1.8 + progressUp * 3.8 + Math.Sin(stratY * 0.20 + seed) * 0.5;
            double haloRadius = coreRadius + 2.8 * (0.55 + progressUp * 0.45);

            bool inChlorite = false;
            bool inSericite = false;

            if (bestStringer.HasValue && bestStringer.Value.InHalo && pipeDistance < haloRadius + 1.5)
            {
                inChlorite = pipeDistance < coreRadius;
                inSericite = !inChlorite;
            }
            else if (pipeDistance < coreRadius)
            {
                inChlorite = true;
            }
            else if (pipeDistance < haloRadius)
            {
                inSericite = true;
            }

            if (inChlorite || inSericite)
            {
                return new BesshiVmsSample(BesshiVmsZone.None, 0, false, false, false, inChlorite, inSericite, fineGrain);
            }
        }

        return new BesshiVmsSample(BesshiVmsZone.None, 0, false, false, false, false, false, fineGrain);
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

    private readonly struct BesshiMassiveLens
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

        public BesshiMassiveLens(
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

        public static BesshiMassiveLens Create(
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
            return new BesshiMassiveLens(
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

    private readonly struct BesshiStringer
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

        public BesshiStringer(
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

        public static BesshiStringer Create(
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
            return new BesshiStringer(
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

            bool inside = footprint <= 1.04 && distance <= halfThickness;
            double score = distance / halfThickness;
            bool inHalo = footprint <= 1.12 && distance <= halfThickness + 1.8 * taper;

            return new StringerSample(inside, score, inHalo);
        }
    }

    private readonly struct ExhaliteSample
    {
        public readonly bool Inside;
        public readonly double Footprint;
        public readonly double Along;
        public readonly double Across;

        public ExhaliteSample(bool inside, double footprint, double along, double across)
        {
            Inside = inside;
            Footprint = footprint;
            Along = along;
            Across = across;
        }
    }

    private readonly struct BesshiExhaliteField
    {
        public readonly double CenterX;
        public readonly double CenterZ;
        public readonly double CenterStratY;
        public readonly double RadiusAlong;
        public readonly double RadiusAcross;
        public readonly double SinStrike;
        public readonly double CosStrike;
        public readonly double Phase;

        public BesshiExhaliteField(
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

        public ExhaliteSample Sample(double x, double stratY, double z)
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

            bool inside = insideEdge && verticalDistance <= halfThickness;
            return new ExhaliteSample(inside, footprint, along, across);
        }
    }
}

internal sealed class BesshiVmsProceduralTemplate : IProceduralDepositTemplate
{
    private static readonly string[] RequiredSlots =
    {
        ProceduralMaterialSlots.Chalcopyrite,
        ProceduralMaterialSlots.Sphalerite,
        ProceduralMaterialSlots.Pyrite,
        ProceduralMaterialSlots.Magnetite,
        ProceduralMaterialSlots.Quartz,
        ProceduralMaterialSlots.Chert,
        ProceduralMaterialSlots.ChertHematite,
        ProceduralMaterialSlots.Gossan,
        ProceduralMaterialSlots.YellowGossan,
        ProceduralMaterialSlots.Chalcocite,
        ProceduralMaterialSlots.Smithsonite,
        ProceduralMaterialSlots.Limonite
    };

    public string Code => "besshiVms";
    public IReadOnlyList<string> RequiredMaterialSlots => RequiredSlots;
    public bool RequiresPlan => true;

    public int GetMaximumHorizontalReach(ProceduralDepositDefinition definition)
    {
        return definition.BesshiVms.HorizontalRadius;
    }

    public object CreatePlan(in ProceduralDepositInstance instance, ProceduralDepositDefinition definition)
    {
        return BesshiVmsPlan.Create(instance, definition.BesshiVms);
    }

    public bool Validate(ProceduralDepositDefinition definition, out string error)
    {
        BesshiVmsDefinition settings = definition.BesshiVms;
        bool valid = settings.HorizontalRadius >= 16
            && settings.VerticalHalfHeight >= 8
            && settings.SupergeneDepth >= 1;
        error = valid ? string.Empty : "invalid besshi vms settings";
        return valid;
    }

    public void Realize(
        ProceduralDepositWorldGenSystem system,
        in DepositCandidate candidate,
        IChunkColumnGenerateRequest request,
        int baseX,
        int baseZ)
    {
        system.RealizeBesshiVmsCandidate(candidate, request, baseX, baseZ);
    }
}

public sealed partial class ProceduralDepositWorldGenSystem
{
    internal void RealizeBesshiVmsCandidate(
        DepositCandidate candidate,
        IChunkColumnGenerateRequest request,
        int baseX,
        int baseZ)
    {
        CompiledProceduralDeposit compiled = candidate.Compiled;
        BesshiVmsDefinition settings = compiled.Definition.BesshiVms;
        var plan = (BesshiVmsPlan)candidate.Plan!;
        ushort[] heightMap = request.Chunks[0].MapChunk.WorldGenTerrainHeightMap;
        int worldHeight = serverApi!.World.BlockAccessor.MapSizeY;
        int minimumY = Math.Clamp(candidate.Instance.CenterY - settings.VerticalHalfHeight, 1, worldHeight - 2);
        int maximumPrototypeY = Math.Clamp(candidate.Instance.CenterY + settings.VerticalHalfHeight, 1, worldHeight - 2);

        int[] zoneSlots = BuildBesshiVmsZoneSlots(compiled);
        int gossanSlot = compiled.GetSlotId(ProceduralMaterialSlots.Gossan);
        int yellowGossanSlot = compiled.GetSlotId(ProceduralMaterialSlots.YellowGossan);

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
                    BesshiVmsSample sample = plan.Evaluate(worldX, y, worldZ, surfaceY);
                    if (sample.Zone == BesshiVmsZone.None) continue;

                    int chunkY = y / ChunkSize;
                    if ((uint)chunkY >= (uint)request.Chunks.Length) continue;
                    int localY = y % ChunkSize;
                    int index3d = ((localY * ChunkSize) + localZ) * ChunkSize + localX;
                    IChunkBlocks data = request.Chunks[chunkY].Data;
                    int hostBlockId = data.GetBlockIdUnsafe(index3d);

                    int targetSlot = zoneSlots[(int)sample.Zone];
                    int grade = sample.Grade;

                    // When weathering is disabled, do not place gossan soils.
                    if (!compiled.Definition.Weathering.Enabled
                        && (sample.Zone == BesshiVmsZone.Gossan
                            || sample.Zone == BesshiVmsZone.YellowGossan))
                    {
                        continue;
                    }

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
                        ? compiled.ResolveWeatheredBlock(targetSlot, grade, hostBlockId, y < surfaceY)
                        : compiled.ResolveBlock(targetSlot, grade, hostBlockId);
                    if (placeBlockId == 0) continue;

                    data.SetBlockUnsafe(index3d, placeBlockId);
                    AdvancedGeology.Byproducts.ByproductSystem.RecordPlacement(request.Chunks[chunkY], index3d, placeBlockId, candidate.Instance.FeatureId, compiled);
                    data.SetFluid(index3d, 0);
                }
            }
        }
    }

    private static int[] BuildBesshiVmsZoneSlots(CompiledProceduralDeposit compiled)
    {
        var slots = new int[Enum.GetValues<BesshiVmsZone>().Length];
        Array.Fill(slots, -1);
        slots[(int)BesshiVmsZone.Chalcopyrite] = compiled.GetSlotId(ProceduralMaterialSlots.Chalcopyrite);
        slots[(int)BesshiVmsZone.Sphalerite] = compiled.GetSlotId(ProceduralMaterialSlots.Sphalerite);
        slots[(int)BesshiVmsZone.Pyrite] = compiled.GetSlotId(ProceduralMaterialSlots.Pyrite);
        slots[(int)BesshiVmsZone.Magnetite] = compiled.GetSlotId(ProceduralMaterialSlots.Magnetite);
        slots[(int)BesshiVmsZone.Quartz] = compiled.GetSlotId(ProceduralMaterialSlots.Quartz);
        slots[(int)BesshiVmsZone.Chert] = compiled.GetSlotId(ProceduralMaterialSlots.Chert);
        slots[(int)BesshiVmsZone.ChertHematite] = compiled.GetSlotId(ProceduralMaterialSlots.ChertHematite);
        slots[(int)BesshiVmsZone.Gossan] = compiled.GetSlotId(ProceduralMaterialSlots.Gossan);
        slots[(int)BesshiVmsZone.YellowGossan] = compiled.GetSlotId(ProceduralMaterialSlots.YellowGossan);
        slots[(int)BesshiVmsZone.Chalcocite] = compiled.GetSlotId(ProceduralMaterialSlots.Chalcocite);
        slots[(int)BesshiVmsZone.Smithsonite] = compiled.GetSlotId(ProceduralMaterialSlots.Smithsonite);
        slots[(int)BesshiVmsZone.Limonite] = compiled.GetSlotId(ProceduralMaterialSlots.Limonite);
        return slots;
    }
}
