using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Vintagestory.API.Server;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace AdvancedGeology.WorldGen.ProceduralDeposits;

public enum ReopenedArsenideVeinZone
{
    None = 0,
    Uraninite,
    NativeBismuth,
    NativeSilver,
    Nickeline,
    Cobaltite,
    Quartz,
    Fluorite,
    Barite,
    Carbonate,
    Torbernite,
    Annabergite,
    Erythrite,
    Gossan,
    GreenGossan,
    PinkGossan
}

[JsonObject(MemberSerialization.OptIn)]
public sealed class ReopenedArsenideVeinDefinition
{
    /// <summary>Model GRID_XZ = 84 -> half extent 42.</summary>
    [JsonProperty]
    public int HorizontalRadius { get; set; } = 42;

    /// <summary>Model GRID_Y = 70 -> half extent 35, matching the y &lt; -35 floor clip.</summary>
    [JsonProperty]
    public int VerticalHalfHeight { get; set; } = 35;

    /// <summary>Model SUPERGENE_DEPTH = 9.0.</summary>
    [JsonProperty]
    public int SupergeneDepth { get; set; } = 9;

    [JsonProperty]
    public double PrimaryStrikeMinDeg { get; set; } = -40.0;

    [JsonProperty]
    public double PrimaryStrikeMaxDeg { get; set; } = 40.0;

    [JsonProperty]
    public double LateStrikeOffsetMinDeg { get; set; } = 35.0;

    [JsonProperty]
    public double LateStrikeOffsetMaxDeg { get; set; } = 65.0;

    [JsonProperty]
    public int PrimaryCountMin { get; set; } = 4;

    [JsonProperty]
    public int PrimaryCountMax { get; set; } = 7;

    [JsonProperty]
    public int LateCountMin { get; set; } = 1;

    [JsonProperty]
    public int LateCountMax { get; set; } = 2;

    [JsonProperty]
    public double PrimaryDipMinDeg { get; set; } = 66.0;

    [JsonProperty]
    public double PrimaryDipMaxDeg { get; set; } = 86.0;

    [JsonProperty]
    public double LateDipMinDeg { get; set; } = 58.0;

    [JsonProperty]
    public double LateDipMaxDeg { get; set; } = 82.0;

    [JsonProperty]
    public double PrimaryThicknessMin { get; set; } = 2.0;

    [JsonProperty]
    public double PrimaryThicknessMax { get; set; } = 3.8;

    [JsonProperty]
    public double LateThicknessMin { get; set; } = 0.8;

    [JsonProperty]
    public double LateThicknessMax { get; set; } = 1.6;

    [JsonProperty]
    public double PrimaryLengthHalfMin { get; set; } = 23.0;

    [JsonProperty]
    public double PrimaryLengthHalfMax { get; set; } = 38.0;

    [JsonProperty]
    public double LateLengthHalfMin { get; set; } = 12.0;

    [JsonProperty]
    public double LateLengthHalfMax { get; set; } = 22.0;

    [JsonProperty]
    public double HeightHalfMin { get; set; } = 20.0;

    [JsonProperty]
    public double HeightHalfMax { get; set; } = 36.0;

    [JsonProperty]
    public double OffsetWalkMin { get; set; } = 9.0;

    [JsonProperty]
    public double OffsetWalkMax { get; set; } = 16.0;

    [JsonProperty]
    public double RelayCenterMin { get; set; } = -10.0;

    [JsonProperty]
    public double RelayCenterMax { get; set; } = 10.0;

    [JsonProperty]
    public double RelayWidthMin { get; set; } = 3.0;

    [JsonProperty]
    public double RelayWidthMax { get; set; } = 6.0;

    [JsonProperty]
    public double RelayShiftMin { get; set; } = -1.6;

    [JsonProperty]
    public double RelayShiftMax { get; set; } = 1.6;

    [JsonProperty]
    public double ThrowMin { get; set; } = 1.4;

    [JsonProperty]
    public double ThrowMax { get; set; } = 3.0;

    [JsonProperty]
    public double GraniteBaseYMin { get; set; } = -24.0;

    [JsonProperty]
    public double GraniteBaseYMax { get; set; } = -17.0;

    [JsonProperty]
    public double CupolaHeightMin { get; set; } = 22.0;

    [JsonProperty]
    public double CupolaHeightMax { get; set; } = 32.0;

    [JsonProperty]
    public double CupolaWidthMin { get; set; } = 14.0;

    [JsonProperty]
    public double CupolaWidthMax { get; set; } = 23.0;

    [JsonProperty]
    public double CupolaOffsetMin { get; set; } = -9.0;

    [JsonProperty]
    public double CupolaOffsetMax { get; set; } = 9.0;
}

public readonly record struct ReopenedArsenideVeinSample(
    ReopenedArsenideVeinZone Zone,
    int Grade,
    int Generation,
    bool InVein,
    bool InAlterationHalo,
    double CenterRatio,
    double BloomNoise);

/// <summary>
/// Uraninite, bismuth, silver, nickeline, cobaltite, quartz, fluorite, barite, carbonate, and secondary arsenates occupy reopened steep fissures and offset late cross-veins.
/// </summary>
internal sealed class ReopenedArsenideVeinPlan
{
    private const ulong PlanSalt = 0x45525A4745424952UL; // "ERZGEBIR"

    /// <summary>Model: air below y &lt; -35 in deposit-local space.</summary>
    private const double FloorY = -35.0;

    private const int PrimaryGeneration = 1;
    private const int LateGeneration = 3;

    private readonly ulong featureId;
    private readonly int originX;
    private readonly int originY;
    private readonly int originZ;

    private readonly double seed;
    private readonly double dipDirection;
    private readonly double primaryStrikeDeg;
    private readonly double lateStrikeDeg;
    private readonly double graniteBaseY;
    private readonly double cupolaHeight;
    private readonly double cupolaWidth;
    private readonly double cupolaX;
    private readonly double cupolaZ;
    private readonly int supergeneDepth;

    private readonly ArsenideVein[] veins;
    private readonly ArsenideVein[] lateCutters;
    private readonly int primaryCount;

    public int OriginX => originX;
    public int OriginY => originY;
    public int OriginZ => originZ;
    public double Seed => seed;
    public double DipDirection => dipDirection;
    public double PrimaryStrikeDeg => primaryStrikeDeg;
    public double LateStrikeDeg => lateStrikeDeg;
    public double GraniteBaseY => graniteBaseY;
    public double CupolaHeight => cupolaHeight;
    public double CupolaWidth => cupolaWidth;
    public double CupolaX => cupolaX;
    public double CupolaZ => cupolaZ;
    public int SupergeneDepth => supergeneDepth;

    /// <summary>Generation-1 long-lived U-Bi-Co-Ni fissures.</summary>
    public int PrimaryVeinCount => primaryCount;

    /// <summary>Generation-3 finite late carbonate crosscuts, which also act as the throw cutters.</summary>
    public int LateCrosscutCount => lateCutters.Length;

    public int VeinCount => veins.Length;

    private ReopenedArsenideVeinPlan(
        ulong featureId,
        in ProceduralDepositInstance instance,
        double seed,
        double dipDirection,
        double primaryStrikeDeg,
        double lateStrikeDeg,
        double graniteBaseY,
        double cupolaHeight,
        double cupolaWidth,
        double cupolaX,
        double cupolaZ,
        int supergeneDepth,
        ArsenideVein[] veins,
        ArsenideVein[] lateCutters,
        int primaryCount)
    {
        this.featureId = featureId;
        originX = instance.CenterX;
        originY = instance.CenterY;
        originZ = instance.CenterZ;
        this.seed = seed;
        this.dipDirection = dipDirection;
        this.primaryStrikeDeg = primaryStrikeDeg;
        this.lateStrikeDeg = lateStrikeDeg;
        this.graniteBaseY = graniteBaseY;
        this.cupolaHeight = cupolaHeight;
        this.cupolaWidth = cupolaWidth;
        this.cupolaX = cupolaX;
        this.cupolaZ = cupolaZ;
        this.supergeneDepth = supergeneDepth;
        this.veins = veins;
        this.lateCutters = lateCutters;
        this.primaryCount = primaryCount;
    }

    /// <summary>
    /// Builds the fault family in deterministic parameter order.
    /// </summary>
    public static ReopenedArsenideVeinPlan Create(
        in ProceduralDepositInstance instance,
        ReopenedArsenideVeinDefinition settings)
    {
        var random = new ProceduralDepositRandom(instance.FeatureId ^ PlanSalt);

        double dipDirection = random.Range(0.0, 1.0) > 0.5 ? 1.0 : -1.0;
        double seed = random.Range(0.0, 100.0);
        double primaryStrikeDeg = random.Range(settings.PrimaryStrikeMinDeg, settings.PrimaryStrikeMaxDeg);
        double lateSide = random.Range(0.0, 1.0) < 0.5 ? -1.0 : 1.0;
        double lateStrikeDeg = primaryStrikeDeg
            + lateSide * random.Range(settings.LateStrikeOffsetMinDeg, settings.LateStrikeOffsetMaxDeg);
        int primaryCount = random.NextInt(settings.PrimaryCountMin, settings.PrimaryCountMax);
        int lateCount = random.NextInt(settings.LateCountMin, settings.LateCountMax);
        double graniteBaseY = random.Range(settings.GraniteBaseYMin, settings.GraniteBaseYMax);
        double cupolaHeight = random.Range(settings.CupolaHeightMin, settings.CupolaHeightMax);
        double cupolaWidth = random.Range(settings.CupolaWidthMin, settings.CupolaWidthMax);
        double cupolaX = random.Range(settings.CupolaOffsetMin, settings.CupolaOffsetMax);
        double cupolaZ = random.Range(settings.CupolaOffsetMin, settings.CupolaOffsetMax);

        // Uranium and five-element stages occupy the same long-lived fissures; their internal
        // fields represent repeated opening, brecciation and sealing.
        ArsenideVein[] primary = CreateGeneration(
            ref random,
            settings,
            seed,
            PrimaryGeneration,
            primaryCount,
            primaryStrikeDeg,
            settings.PrimaryDipMinDeg,
            settings.PrimaryDipMaxDeg,
            settings.PrimaryThicknessMin,
            settings.PrimaryThicknessMax,
            settings.PrimaryLengthHalfMin,
            settings.PrimaryLengthHalfMax,
            31.0);

        ArsenideVein[] late = CreateGeneration(
            ref random,
            settings,
            seed,
            LateGeneration,
            lateCount,
            lateStrikeDeg,
            settings.LateDipMinDeg,
            settings.LateDipMaxDeg,
            settings.LateThicknessMin,
            settings.LateThicknessMax,
            settings.LateLengthHalfMin,
            settings.LateLengthHalfMax,
            137.0);

        var all = new ArsenideVein[primary.Length + late.Length];
        Array.Copy(primary, 0, all, 0, primary.Length);
        Array.Copy(late, 0, all, primary.Length, late.Length);

        return new ReopenedArsenideVeinPlan(
            instance.FeatureId,
            instance,
            seed,
            dipDirection,
            primaryStrikeDeg,
            lateStrikeDeg,
            graniteBaseY,
            cupolaHeight,
            cupolaWidth,
            cupolaX,
            cupolaZ,
            settings.SupergeneDepth,
            all,
            late,
            primary.Length);
    }

    /// <summary>
    /// Offsets follow a 9-16 block random walk and are
    /// then centred on the midpoint of the first and last offset, followed by the per-vein draws in the
    /// deterministic parameter order.
    /// </summary>
    private static ArsenideVein[] CreateGeneration(
        ref ProceduralDepositRandom random,
        ReopenedArsenideVeinDefinition settings,
        double seed,
        int generation,
        int count,
        double strikeDeg,
        double dipMinDeg,
        double dipMaxDeg,
        double thicknessMin,
        double thicknessMax,
        double lengthHalfMin,
        double lengthHalfMax,
        double phaseBase)
    {
        var offsets = new double[count];
        offsets[0] = 0.0;
        for (int i = 1; i < count; i++)
        {
            offsets[i] = offsets[i - 1] + random.Range(settings.OffsetWalkMin, settings.OffsetWalkMax);
        }

        double offsetCenter = (offsets[0] + offsets[count - 1]) / 2.0;

        var created = new ArsenideVein[count];
        for (int i = 0; i < count; i++)
        {
            created[i] = ArsenideVein.Create(
                generation,
                strikeDeg + random.Range(-10.0, 10.0),
                random.Range(dipMinDeg, dipMaxDeg),
                offsets[i] - offsetCenter + random.Range(-2.5, 2.5),
                random.Range(-6.0, 6.0),
                random.Range(-8.0, 4.0),
                random.Range(lengthHalfMin, lengthHalfMax),
                random.Range(settings.HeightHalfMin, settings.HeightHalfMax),
                random.Range(thicknessMin, thicknessMax),
                seed + phaseBase + i * 13.7,
                random.Range(settings.RelayCenterMin, settings.RelayCenterMax),
                random.Range(settings.RelayWidthMin, settings.RelayWidthMax),
                random.Range(settings.RelayShiftMin, settings.RelayShiftMax),
                seed + phaseBase * 0.5 + i * 5.2,
                seed + phaseBase + i * 29.0,
                random.Range(settings.ThrowMin, settings.ThrowMax));
        }

        return created;
    }

    public double GetGraniteTopY(int worldX, int worldZ)
    {
        return originY + GetLocalGraniteTopY(worldX - originX, worldZ - originZ);
    }

    private double GetLocalGraniteTopY(double x, double z)
    {
        double dx = x - cupolaX;
        double dz = z - cupolaZ;
        double sigma = cupolaWidth;
        double dome = Math.Exp(-(dx * dx + dz * dz) / (2.0 * sigma * sigma));
        double roughness = Math.Sin(x * 0.07 + seed) * 1.3
            + Math.Cos(z * 0.062 - seed * 0.5) * 1.1;
        return graniteBaseY + cupolaHeight * dome + roughness;
    }

    /// <summary>
    /// verbatim from <c>getThrowShift</c>: only pre-existing (generation &lt; 3) veins are offset by
    /// the finite late crosscuts.
    /// </summary>
    private double GetThrowShift(int generation, double x, double y, double z)
    {
        if (generation >= LateGeneration) return 0.0;

        double shift = 0.0;
        for (int i = 0; i < lateCutters.Length; i++)
        {
            shift += lateCutters[i].LocalizedThrow(x, y, z, dipDirection);
        }

        return shift;
    }

    public ReopenedArsenideVeinSample Evaluate(
        int worldX,
        int worldY,
        int worldZ,
        int surfaceY = int.MaxValue,
        bool weatheringEnabled = true)
    {
        if (worldY > surfaceY) return default;

        double x = worldX - originX;
        double y = worldY - originY;
        double z = worldZ - originZ;

    // Model bounds the package at deposit-local y < -35
        if (y < FloorY) return default;

    // Model: depth = surfaceY - y, feeding shallowMetalBias / deepGangueBias.
        double depth = surfaceY < int.MaxValue ? surfaceY - worldY : 999.0;

        // Block-index depth for the supergene windows, matching SchwazTetrahedritePlan: the topmost
        // solid block of the column is depth 1.
        int blockDepth = surfaceY < int.MaxValue ? surfaceY - worldY + 1 : 999;

        if (y < GetLocalGraniteTopY(x, z)) return default;

        VeinSample? bestVein = null;
        VeinSample? haloVein = null;
        for (int i = 0; i < veins.Length; i++)
        {
            ArsenideVein vein = veins[i];
            VeinSample sample = vein.Sample(x, y, z, GetThrowShift(vein.Generation, x, y, z), dipDirection);
            if (sample.Inside)
            {
                // Higher generation wins; ties broken by the lower score.
                if (!bestVein.HasValue
                    || sample.Generation > bestVein.Value.Generation
                    || (sample.Generation == bestVein.Value.Generation && sample.Score < bestVein.Value.Score))
                {
                    bestVein = sample;
                }
            }
            else if (sample.InHalo)
            {
                if (!haloVein.HasValue || sample.Generation > haloVein.Value.Generation) haloVein = sample;
            }
        }

        VeinSample active;
        if (bestVein.HasValue) active = bestVein.Value;
        else if (haloVein.HasValue) active = haloVein.Value;
        else return default;

        double stageField = Math.Sin(active.Along * 0.16 + active.StagePhase)
            + 0.60 * Math.Cos(active.VerticalNorm * 4.1 - active.StagePhase * 0.4);
        double shootField = Math.Sin(active.Along * 0.09 - active.StagePhase * 0.7)
            + 0.55 * Math.Cos(active.VerticalNorm * 3.2 + active.StagePhase);
        double bandField = Math.Sin(active.Along * 0.34 + active.StagePhase * 1.3);
        double reductionField = Math.Sin(x * 0.14 - z * 0.11 + seed * 0.8)
            + 0.55 * Math.Cos(y * 0.19 + x * 0.07);
        double shallowMetalBias = Clamp(1.0 - Math.Max(0.0, depth - 9.0) / 30.0, 0.0, 1.0);
        double deepGangueBias = Clamp((depth - 15.0) / 28.0, 0.0, 1.0);

    // Bloom noise supplies the skutterudite-to-cobaltite speckle gate (10%).
        double bloomNoise = Hash3D(x * 2.1 + seed, y * 2.1, z * 2.1);

        if (!bestVein.HasValue)
        {
            double haloContinuity = Math.Sin(x * 0.21 + z * 0.17 + seed * 0.9)
                + 0.50 * Math.Cos(y * 0.28 - x * 0.12);

            bool alteredHalo = haloContinuity > -0.2;
            return new ReopenedArsenideVeinSample(
                ReopenedArsenideVeinZone.None,
                0,
                active.Generation,
                false,
                alteredHalo,
                active.CenterRatio,
                bloomNoise);
        }

        // Grain noise for deterministic mineral grade variation (0=poor, 1=medium, 2=rich, 3=bountiful)
        double grainNoise = Hash3D(x * 2.7 + seed * 1.3, y * 2.7, z * 2.7);
        int grade = grainNoise > 0.82 ? 3 : (grainNoise > 0.55 ? 2 : (grainNoise > 0.25 ? 1 : 0));

        ReopenedArsenideVeinZone hypogene;

        if (active.Generation == PrimaryGeneration)
        {
            // Repeated U and arsenide stages share one fissure. Early quartz and hematite form the
            // margins; reduced shoots occupy the reopened core.
            if (active.CenterRatio > 0.82 && stageField < -0.15)
            {
                return new ReopenedArsenideVeinSample(
                    ReopenedArsenideVeinZone.None,
                    0,
                    active.Generation,
                    true,
                    false,
                    active.CenterRatio,
                    bloomNoise);
            }

            if (bandField > 0.78 || stageField < -1.20)
            {
                hypogene = ReopenedArsenideVeinZone.Quartz;
            }
            else
            {
                bool inCore = active.CenterRatio < 0.50;
                if (inCore && reductionField + shootField + shallowMetalBias * 0.45 > 1.72)
                {
                    hypogene = ReopenedArsenideVeinZone.Uraninite;
                }
                else if (inCore && shootField + shallowMetalBias * 0.55 > 1.02)
                {
                    hypogene = ReopenedArsenideVeinZone.NativeBismuth;
                }
                else if (inCore && shallowMetalBias > 0.35 && shootField < -1.25)
                {
                    hypogene = ReopenedArsenideVeinZone.NativeSilver;
                }
                else if (stageField > 0.48)
                {
                    // Skutterudite: nickeline with 10% cobaltite speckles.
                    hypogene = bloomNoise >= 0.90
                        ? ReopenedArsenideVeinZone.Cobaltite
                        : ReopenedArsenideVeinZone.Nickeline;
                }
                else
                {
                    // Ni-Co diarsenide (stageField > -0.18) and nickeline both resolve to nickeline ore.
                    hypogene = ReopenedArsenideVeinZone.Nickeline;
                }
            }
        }
        else
        {
            // Finite late crosscuts are carbonate-dominant. Fluorite and barite increase downward but
            // remain subordinate bands.
            if (deepGangueBias > 0.22 && bandField > 0.76)
            {
                hypogene = ReopenedArsenideVeinZone.Fluorite;
            }
            else if (deepGangueBias > 0.30 && bandField < -0.80)
            {
                hypogene = ReopenedArsenideVeinZone.Barite;
            }
            else
            {
                hypogene = ReopenedArsenideVeinZone.Carbonate;
            }
        }


        // depth 1-2 stained gossan soil, depth 3-8 secondary arsenate/phosphate, deeper hypogene.
        // Bismuth, silver, quartz, fluorite, barite and carbonate are unaltered at every depth.
        ReopenedArsenideVeinZone zone = hypogene;
        if (weatheringEnabled && blockDepth < supergeneDepth)
        {
            bool soil = blockDepth <= 2;
            switch (hypogene)
            {
                case ReopenedArsenideVeinZone.Uraninite:
                    zone = soil ? ReopenedArsenideVeinZone.Gossan : ReopenedArsenideVeinZone.Torbernite;
                    break;
                case ReopenedArsenideVeinZone.Nickeline:
                    zone = soil ? ReopenedArsenideVeinZone.GreenGossan : ReopenedArsenideVeinZone.Annabergite;
                    break;
                case ReopenedArsenideVeinZone.Cobaltite:
                    zone = soil ? ReopenedArsenideVeinZone.PinkGossan : ReopenedArsenideVeinZone.Erythrite;
                    break;
            }
        }

        if (!IsGraded(zone)) grade = 0;

        return new ReopenedArsenideVeinSample(
            zone,
            grade,
            active.Generation,
            true,
            false,
            active.CenterRatio,
            bloomNoise);
    }

    private static bool IsGraded(ReopenedArsenideVeinZone zone)
    {
        return zone == ReopenedArsenideVeinZone.Uraninite
            || zone == ReopenedArsenideVeinZone.Nickeline
            || zone == ReopenedArsenideVeinZone.Cobaltite
            || zone == ReopenedArsenideVeinZone.NativeSilver
            || zone == ReopenedArsenideVeinZone.NativeBismuth
            || zone == ReopenedArsenideVeinZone.Torbernite
            || zone == ReopenedArsenideVeinZone.Annabergite
            || zone == ReopenedArsenideVeinZone.Erythrite;
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

    private readonly record struct VeinSample(
        double Distance,
        double Score,
        bool Inside,
        bool InHalo,
        double CenterRatio,
        double Along,
        double VerticalNorm,
        double StagePhase,
        int Generation);

    private readonly struct ArsenideVein
    {
        public readonly int Generation;
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
        public readonly double MaxThickness;
        public readonly double Phase;
        public readonly double RelayCenter;
        public readonly double RelayWidth;
        public readonly double RelayShift;
        public readonly double PinchPhase;
        public readonly double StagePhase;
        public readonly double ThrowAmount;

        private ArsenideVein(
            int generation,
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
            double maxThickness,
            double phase,
            double relayCenter,
            double relayWidth,
            double relayShift,
            double pinchPhase,
            double stagePhase,
            double throwAmount)
        {
            Generation = generation;
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
            MaxThickness = maxThickness;
            Phase = phase;
            RelayCenter = relayCenter;
            RelayWidth = relayWidth;
            RelayShift = relayShift;
            PinchPhase = pinchPhase;
            StagePhase = stagePhase;
            ThrowAmount = throwAmount;
        }

        public static ArsenideVein Create(
            int generation,
            double strikeDeg,
            double dipDeg,
            double offset,
            double centerAlong,
            double centerY,
            double lengthHalf,
            double heightHalf,
            double maxThickness,
            double phase,
            double relayCenter,
            double relayWidth,
            double relayShift,
            double pinchPhase,
            double stagePhase,
            double throwAmount)
        {
            double strikeRad = strikeDeg * Math.PI / 180.0;
            double dipRad = dipDeg * Math.PI / 180.0;
            return new ArsenideVein(
                generation,
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
                maxThickness,
                phase,
                relayCenter,
                relayWidth,
                relayShift,
                pinchPhase,
                stagePhase,
                throwAmount);
        }

    /// <summary> verbatim from <c>getLocalizedThrow</c>.</summary>
        public double LocalizedThrow(double x, double y, double z, double dipDirection)
        {
            double along = x * CosStrike - z * SinStrike;
            double vertical = y - CenterY;
            double u = (along - CenterAlong) / LengthHalf;
            double v = vertical / HeightHalf;
            double footprint = Math.Sqrt(u * u + v * v);
            if (footprint >= 1.0) return 0.0;

            double across = x * SinStrike + z * CosStrike - Offset;
            double planeAcross = -vertical * CosDip / Math.Max(0.12, SinDip) * dipDirection;
            double side = (across - planeAcross) >= 0.0 ? 1.0 : -1.0;
            double tipTaper = 1.0 - footprint * footprint;
            return side * ThrowAmount * tipTaper;
        }

    /// <summary> verbatim from <c>getVeinSample</c>.</summary>
        public VeinSample Sample(double x, double y, double z, double throwShift, double dipDirection)
        {
            double along = x * CosStrike - z * SinStrike;
            double across = x * SinStrike + z * CosStrike - (Offset + throwShift);
            double vertical = y - CenterY;
            double planeAcross = -vertical * CosDip / Math.Max(0.12, SinDip) * dipDirection;
            double bend = Math.Sin(along * 0.11 + y * 0.06 + Phase) * 2.6
                + Math.Cos(along * 0.30 + Phase * 1.3) * 0.8;
            double relay = RelayShift * Math.Tanh((along - RelayCenter) / RelayWidth);
            double signedDistance = (across - planeAcross) * SinDip - bend - relay;

            double u = (along - CenterAlong) / LengthHalf;
            double v = vertical / HeightHalf;
            double edgeWarp = Math.Sin(u * 3.6 + Phase) * Math.Cos(v * 3.0) * 0.15
                + Math.Sin(u * 7.1 - v * 3.2 + Phase * 0.5) * 0.07;
            double footprint = Math.Sqrt(u * u + v * v) + edgeWarp;
            double edgeTaper = Clamp(1.0 - footprint * footprint, 0.0, 1.0);
            double halfThickness = Math.Max(
                0.10,
                MaxThickness * 0.5 * (0.82 + 0.18 * Math.Sin(along * 0.18 + PinchPhase)) * Math.Sqrt(edgeTaper));
            double distance = Math.Abs(signedDistance);

            return new VeinSample(
                distance,
                footprint <= 1.18 ? distance / halfThickness : double.PositiveInfinity,
                footprint <= 1.04 && distance <= halfThickness,
                footprint <= 1.12 && distance <= halfThickness + 1.5 * edgeTaper,
                distance / Math.Max(0.1, halfThickness),
                along,
                v,
                StagePhase,
                Generation);
        }
    }
}

internal sealed class ReopenedArsenideVeinProceduralTemplate : IProceduralDepositTemplate
{
    private static readonly string[] RequiredSlots =
    {
        ProceduralMaterialSlots.Uraninite,
        ProceduralMaterialSlots.NativeBismuth,
        ProceduralMaterialSlots.Nickeline,
        ProceduralMaterialSlots.Cobaltite,
        ProceduralMaterialSlots.NativeSilver,
        ProceduralMaterialSlots.Quartz,
        ProceduralMaterialSlots.Fluorite,
        ProceduralMaterialSlots.Barite,
        ProceduralMaterialSlots.Carbonate,
        ProceduralMaterialSlots.Torbernite,
        ProceduralMaterialSlots.Annabergite,
        ProceduralMaterialSlots.Erythrite,
        ProceduralMaterialSlots.Gossan,
        ProceduralMaterialSlots.GreenGossan,
        ProceduralMaterialSlots.PinkGossan
    };

    public string Code => "reopenedArsenideVein";
    public IReadOnlyList<string> RequiredMaterialSlots => RequiredSlots;
    public bool RequiresPlan => true;

    public int GetMaximumHorizontalReach(ProceduralDepositDefinition definition)
    {
        return definition.ArsenideVein.HorizontalRadius;
    }

    public object CreatePlan(in ProceduralDepositInstance instance, ProceduralDepositDefinition definition)
    {
        return ReopenedArsenideVeinPlan.Create(instance, definition.ArsenideVein);
    }

    public bool Validate(ProceduralDepositDefinition definition, out string error)
    {
        ReopenedArsenideVeinDefinition settings = definition.ArsenideVein;
        bool valid = settings.HorizontalRadius >= 16
            && settings.VerticalHalfHeight >= 8
            && settings.SupergeneDepth >= 3
            && settings.PrimaryCountMin >= 1
            && settings.PrimaryCountMax >= settings.PrimaryCountMin
            && settings.LateCountMin >= 1
            && settings.LateCountMax >= settings.LateCountMin
            && settings.PrimaryDipMinDeg > 0
            && settings.PrimaryDipMaxDeg >= settings.PrimaryDipMinDeg
            && settings.PrimaryDipMaxDeg <= 90
            && settings.LateDipMinDeg > 0
            && settings.LateDipMaxDeg >= settings.LateDipMinDeg
            && settings.LateDipMaxDeg <= 90
            && settings.PrimaryThicknessMin > 0
            && settings.PrimaryThicknessMax >= settings.PrimaryThicknessMin
            && settings.LateThicknessMin > 0
            && settings.LateThicknessMax >= settings.LateThicknessMin
            && settings.PrimaryLengthHalfMin > 0
            && settings.PrimaryLengthHalfMax >= settings.PrimaryLengthHalfMin
            && settings.LateLengthHalfMin > 0
            && settings.LateLengthHalfMax >= settings.LateLengthHalfMin
            && settings.HeightHalfMin > 0
            && settings.HeightHalfMax >= settings.HeightHalfMin
            && settings.OffsetWalkMin > 0
            && settings.OffsetWalkMax >= settings.OffsetWalkMin
            && settings.RelayWidthMin > 0
            && settings.RelayWidthMax >= settings.RelayWidthMin
            && settings.ThrowMin >= 0
            && settings.ThrowMax >= settings.ThrowMin
            && settings.CupolaWidthMin > 0
            && settings.CupolaWidthMax >= settings.CupolaWidthMin
            && settings.CupolaHeightMin > 0
            && settings.CupolaHeightMax >= settings.CupolaHeightMin
            && settings.GraniteBaseYMax >= settings.GraniteBaseYMin;

        error = valid ? string.Empty : "invalid reopened arsenide vein settings";
        return valid;
    }

    public void Realize(
        ProceduralDepositWorldGenSystem system,
        in DepositCandidate candidate,
        IChunkColumnGenerateRequest request,
        int baseX,
        int baseZ)
    {
        system.RealizeReopenedArsenideVeinCandidate(candidate, request, baseX, baseZ);
    }
}

public sealed partial class ProceduralDepositWorldGenSystem
{
    internal void RealizeReopenedArsenideVeinCandidate(
        DepositCandidate candidate,
        IChunkColumnGenerateRequest request,
        int baseX,
        int baseZ)
    {
        CompiledProceduralDeposit compiled = candidate.Compiled;
        ReopenedArsenideVeinDefinition settings = compiled.Definition.ArsenideVein;
        var plan = (ReopenedArsenideVeinPlan)candidate.Plan!;
        ushort[] heightMap = request.Chunks[0].MapChunk.WorldGenTerrainHeightMap;
        int worldHeight = serverApi!.World.BlockAccessor.MapSizeY;
        int minimumY = Math.Clamp(candidate.Instance.CenterY - settings.VerticalHalfHeight, 1, worldHeight - 2);
        int maximumPrototypeY = Math.Clamp(candidate.Instance.CenterY + settings.VerticalHalfHeight, 1, worldHeight - 2);

        int[] zoneSlots = BuildReopenedArsenideVeinZoneSlots(compiled);
        int gossanSlot = compiled.GetSlotId(ProceduralMaterialSlots.Gossan);
        int greenGossanSlot = compiled.GetSlotId(ProceduralMaterialSlots.GreenGossan);
        int pinkGossanSlot = compiled.GetSlotId(ProceduralMaterialSlots.PinkGossan);

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
                    ReopenedArsenideVeinSample sample = plan.Evaluate(worldX, y, worldZ, surfaceY, weatheringEnabled);
                    if (sample.Zone == ReopenedArsenideVeinZone.None) continue;

                    int chunkY = y / ChunkSize;
                    if ((uint)chunkY >= (uint)request.Chunks.Length) continue;
                    int localY = y % ChunkSize;
                    int index3d = ((localY * ChunkSize) + localZ) * ChunkSize + localX;
                    IChunkBlocks data = request.Chunks[chunkY].Data;
                    int hostBlockId = data.GetBlockIdUnsafe(index3d);

                    int targetSlot = zoneSlots[(int)sample.Zone];
                    if (targetSlot < 0) continue;

                    bool isSoil = targetSlot == gossanSlot || targetSlot == greenGossanSlot || targetSlot == pinkGossanSlot;
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

    private static int[] BuildReopenedArsenideVeinZoneSlots(CompiledProceduralDeposit compiled)
    {
        var slots = new int[Enum.GetValues<ReopenedArsenideVeinZone>().Length];
        Array.Fill(slots, -1);
        slots[(int)ReopenedArsenideVeinZone.Uraninite] = compiled.GetSlotId(ProceduralMaterialSlots.Uraninite);
        slots[(int)ReopenedArsenideVeinZone.NativeBismuth] = compiled.GetSlotId(ProceduralMaterialSlots.NativeBismuth);
        slots[(int)ReopenedArsenideVeinZone.NativeSilver] = compiled.GetSlotId(ProceduralMaterialSlots.NativeSilver);
        slots[(int)ReopenedArsenideVeinZone.Nickeline] = compiled.GetSlotId(ProceduralMaterialSlots.Nickeline);
        slots[(int)ReopenedArsenideVeinZone.Cobaltite] = compiled.GetSlotId(ProceduralMaterialSlots.Cobaltite);
        slots[(int)ReopenedArsenideVeinZone.Quartz] = compiled.GetSlotId(ProceduralMaterialSlots.Quartz);
        slots[(int)ReopenedArsenideVeinZone.Fluorite] = compiled.GetSlotId(ProceduralMaterialSlots.Fluorite);
        slots[(int)ReopenedArsenideVeinZone.Barite] = compiled.GetSlotId(ProceduralMaterialSlots.Barite);
        slots[(int)ReopenedArsenideVeinZone.Carbonate] = compiled.GetSlotId(ProceduralMaterialSlots.Carbonate);
        slots[(int)ReopenedArsenideVeinZone.Torbernite] = compiled.GetSlotId(ProceduralMaterialSlots.Torbernite);
        slots[(int)ReopenedArsenideVeinZone.Annabergite] = compiled.GetSlotId(ProceduralMaterialSlots.Annabergite);
        slots[(int)ReopenedArsenideVeinZone.Erythrite] = compiled.GetSlotId(ProceduralMaterialSlots.Erythrite);
        slots[(int)ReopenedArsenideVeinZone.Gossan] = compiled.GetSlotId(ProceduralMaterialSlots.Gossan);
        slots[(int)ReopenedArsenideVeinZone.GreenGossan] = compiled.GetSlotId(ProceduralMaterialSlots.GreenGossan);
        slots[(int)ReopenedArsenideVeinZone.PinkGossan] = compiled.GetSlotId(ProceduralMaterialSlots.PinkGossan);
        return slots;
    }
}
