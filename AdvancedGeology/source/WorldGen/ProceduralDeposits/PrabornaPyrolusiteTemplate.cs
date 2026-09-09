using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace AdvancedGeology.WorldGen.ProceduralDeposits;

public enum PrabornaPyrolusiteZone
{
    None = 0,
    Pyrolusite,
    Braunite,
    Rhodochrosite,
    Spessartine,
    Quartz,
    Hematite,
    Breccia,

    // Surface weathering / supergene hooks (enums only; Main will wire weathering)
    BlackGossan
}

[JsonObject(MemberSerialization.OptIn)]
public sealed class PrabornaPyrolusiteDefinition
{
    [JsonProperty]
    public int HorizontalRadius { get; set; } = 48;

    [JsonProperty]
    public int VerticalHalfHeight { get; set; } = 28;

    [JsonProperty]
    public double StrikeDeg { get; set; } = 0.0;

    [JsonProperty]
    public double DipDeg { get; set; } = 16.0;

    [JsonProperty]
    public double DipDirectionDeg { get; set; } = 90.0;

    [JsonProperty]
    public int LensCount { get; set; } = 3;

    [JsonProperty]
    public int QuartzVeinCount { get; set; } = 4;

    [JsonProperty]
    public double ContactY { get; set; } = 6.0;

    [JsonProperty]
    public double OxidationDepth { get; set; } = 10.0;

    [JsonProperty]
    public int WeatheringDepthMax { get; set; } = 6;
}

public readonly record struct PrabornaPyrolusiteSample(
    bool InsideLens,
    bool InsideVein,
    bool InStratiformBand,
    bool InHalo,
    double BestLensRadial,
    double BestVeinScore,
    double StratigraphicY,
    double LocalY);

/// <summary>
/// Pyrolusite, braunite, rhodochrosite, spessartine, quartz, hematite, breccia, and gossan form folded, boudinaged manganese-rich metachert lenses cut by sigmoidal veins.
/// </summary>
internal sealed class PrabornaPyrolusitePlan
{
    private const ulong PlanSalt = 0x505241424F524E41UL;

    private readonly ulong featureId;
    private readonly int originX;
    private readonly int originY;
    private readonly int originZ;
    private readonly double seed;
    private readonly double strikeDeg;
    private readonly double dipDeg;
    private readonly double dipDirectionDeg;
    private readonly double contactY;
    private readonly double oxidationDepth;
    private readonly double systemX;
    private readonly double systemZ;
    private readonly double cosStrike;
    private readonly double sinStrike;
    private readonly double cosDipDirection;
    private readonly double sinDipDirection;
    private readonly double tanDip;
    private readonly PrabornaLens mainLens;
    private readonly PrabornaLens[] subLenses;
    private readonly PrabornaVein[] quartzVeins;

    public int OriginX => originX;
    public int OriginY => originY;
    public int OriginZ => originZ;
    public double Seed => seed;
    public double StrikeDeg => strikeDeg;
    public double DipDeg => dipDeg;
    public int SubLensCount => subLenses.Length;
    public int QuartzVeinCount => quartzVeins.Length;

    private PrabornaPyrolusitePlan(
        ulong featureId,
        in ProceduralDepositInstance instance,
        double seed,
        double strikeDeg,
        double dipDeg,
        double dipDirectionDeg,
        double contactY,
        double oxidationDepth,
        double systemX,
        double systemZ,
        double cosStrike,
        double sinStrike,
        double cosDipDirection,
        double sinDipDirection,
        double tanDip,
        PrabornaLens mainLens,
        PrabornaLens[] subLenses,
        PrabornaVein[] quartzVeins)
    {
        this.featureId = featureId;
        originX = instance.CenterX;
        originY = instance.CenterY;
        originZ = instance.CenterZ;
        this.seed = seed;
        this.strikeDeg = strikeDeg;
        this.dipDeg = dipDeg;
        this.dipDirectionDeg = dipDirectionDeg;
        this.contactY = contactY;
        this.oxidationDepth = oxidationDepth;
        this.systemX = systemX;
        this.systemZ = systemZ;
        this.cosStrike = cosStrike;
        this.sinStrike = sinStrike;
        this.cosDipDirection = cosDipDirection;
        this.sinDipDirection = sinDipDirection;
        this.tanDip = tanDip;
        this.mainLens = mainLens;
        this.subLenses = subLenses;
        this.quartzVeins = quartzVeins;
    }

    public static PrabornaPyrolusitePlan Create(
        in ProceduralDepositInstance instance,
        PrabornaPyrolusiteDefinition settings)
    {
        var random = new ProceduralDepositRandom(instance.FeatureId ^ PlanSalt);
        double seed = random.Range(0.0, 1000.0);

        double strikeDeg = settings.StrikeDeg + random.Range(-8.0, 8.0);
        double dipDeg = random.Range(14.0, 22.0);
        double dipDirectionDeg = settings.DipDirectionDeg + random.Range(-12.0, 12.0);
        double contactY = settings.ContactY + random.Range(-1.0, 2.0);
        double systemX = random.Range(-4.0, 4.0);
        double systemZ = random.Range(-4.0, 4.0);

        double strikeRad = strikeDeg * Math.PI / 180.0;
        double cosStr = Math.Cos(strikeRad);
        double sinStr = Math.Sin(strikeRad);

        double dirRad = dipDirectionDeg * Math.PI / 180.0;
        double cosDir = Math.Cos(dirRad);
        double sinDir = Math.Sin(dirRad);
        double tanD = Math.Tan(dipDeg * Math.PI / 180.0);

        // Main boudinaged lens
        var main = PrabornaLens.Create(
            systemX + random.Range(-2.0, 2.0),
            systemZ + random.Range(-2.0, 2.0),
            contactY,
            random.Range(29.0, 38.0),
            random.Range(10.0, 15.0),
            random.Range(4.5, 7.0),
            strikeDeg,
            seed + 11.0,
            true);

        // Subordinate lenses along strike
        int subCount = Math.Max(1, settings.LensCount - 1);
        var subs = new PrabornaLens[subCount];
        for (int i = 0; i < subCount; i++)
        {
            double along = (i % 2 == 0 ? -1.0 : 1.0) * random.Range(21.0, 34.0);
            double across = random.Range(-4.0, 4.0);
            double cx = systemX + along * cosStr + across * sinStr;
            double cz = systemZ - along * sinStr + across * cosStr;

            subs[i] = PrabornaLens.Create(
                cx,
                cz,
                contactY + random.Range(-1.4, 1.4),
                random.Range(10.0, 19.0),
                random.Range(5.0, 9.0),
                random.Range(2.5, 4.5),
                strikeDeg + random.Range(-7.0, 7.0),
                seed + 31.0 + i * 19.0,
                false);
        }

        // Sigmoidal cross-cutting quartz / reaction veins
        int veinCount = settings.QuartzVeinCount;
        var veins = new PrabornaVein[veinCount];
        for (int i = 0; i < veinCount; i++)
        {
            double along = random.Range(-28.0, 28.0);
            double across = random.Range(-11.0, 11.0);
            double vx = systemX + along * cosStr + across * sinStr;
            double vz = systemZ - along * sinStr + across * cosStr;

            veins[i] = PrabornaVein.Create(
                vx,
                vz,
                contactY - random.Range(0.0, 3.0),
                strikeDeg + random.Range(-38.0, 38.0),
                random.Range(45.0, 78.0),
                random.Range(9.0, 20.0),
                random.Range(12.0, 24.0),
                random.Range(0.8, 1.8),
                seed + 71.0 + i * 13.0,
                random.Range(-7.0, 7.0),
                random.Range(2.0, 4.5),
                random.Range(-1.0, 1.0),
                seed + 51.0 + i * 5.0,
                random.Range(0.7, 1.5));
        }

        return new PrabornaPyrolusitePlan(
            instance.FeatureId,
            instance,
            seed,
            strikeDeg,
            dipDeg,
            dipDirectionDeg,
            contactY,
            settings.OxidationDepth,
            systemX,
            systemZ,
            cosStr,
            sinStr,
            cosDir,
            sinDir,
            tanD,
            main,
            subs,
            veins);
    }

    public double GetBeddingOffset(double x, double z)
    {
        double localX = x - systemX;
        double localZ = z - systemZ;
        double projection = localX * cosDipDirection + localZ * sinDipDirection;
        double along = localX * sinDipDirection - localZ * cosDipDirection;
        double warp = Math.Sin(x * 0.055 + seed) * 1.5 + Math.Cos(z * 0.06 - seed * 0.5) * 1.0;
        double fold = Math.Sin(projection * 0.12 + seed * 0.7) * 1.2 + Math.Sin(along * 0.045 + seed * 0.35) * 1.6;
        return tanDip * projection + warp + fold;
    }

    public double GetStratigraphicY(double x, double y, double z)
    {
        return y - GetBeddingOffset(x, z);
    }

    public PrabornaPyrolusiteSample Sample(int worldX, int worldY, int worldZ)
    {
        double x = worldX - originX;
        double y = worldY - originY;
        double z = worldZ - originZ;

        double stratY = GetStratigraphicY(x, y, z);

        LensSample mainSample = mainLens.Sample(x, y, z, stratY);
        double bestRadial = mainSample.Radial;
        bool insideLens = mainSample.Inside;
        bool inLensHalo = mainSample.InHalo;

        foreach (PrabornaLens sub in subLenses)
        {
            LensSample ss = sub.Sample(x, y, z, stratY);
            if (ss.Radial < bestRadial) bestRadial = ss.Radial;
            if (ss.Inside) insideLens = true;
            if (ss.InHalo) inLensHalo = true;
        }

        double bestVeinScore = double.PositiveInfinity;
        bool insideVein = false;
        bool inVeinHalo = false;
        foreach (PrabornaVein vein in quartzVeins)
        {
            VeinSample vs = vein.Sample(x, y, z);
            if (vs.Score < bestVeinScore) bestVeinScore = vs.Score;
            if (vs.Inside) insideVein = true;
            if (vs.InHalo) inVeinHalo = true;
        }

        StratiformSample band = SampleStratiformBand(x, z, stratY);
        bool inBand = band.InBand && band.Boudin;
        bool inHalo = inLensHalo || inVeinHalo;

        return new PrabornaPyrolusiteSample(
            insideLens,
            insideVein,
            inBand,
            inHalo,
            bestRadial,
            bestVeinScore,
            stratY,
            y);
    }

    public PrabornaPyrolusiteZone EvaluateZone(int worldX, int worldY, int worldZ, int surfaceY = int.MaxValue)
    {
        double x = worldX - originX;
        double y = worldY - originY;
        double z = worldZ - originZ;

        int depth = surfaceY < int.MaxValue ? surfaceY - worldY : 999;
        double stratY = GetStratigraphicY(x, y, z);
        double grain = Hash3D(x * 2.05 + seed, y * 2.05, z * 2.05);

        // Find closest lens
        LensSample? bestLens = null;
        LensSample mainSample = mainLens.Sample(x, y, z, stratY);
        if (mainSample.Inside) bestLens = mainSample;

        foreach (PrabornaLens sub in subLenses)
        {
            LensSample sample = sub.Sample(x, y, z, stratY);
            if (sample.Inside && (bestLens == null || sample.Radial < bestLens.Value.Radial))
            {
                bestLens = sample;
            }
        }

        // Find closest vein
        VeinSample? bestVein = null;
        foreach (PrabornaVein vein in quartzVeins)
        {
            VeinSample sample = vein.Sample(x, y, z);
            if (sample.Score < double.PositiveInfinity && (bestVein == null || sample.Score < bestVein.Value.Score))
            {
                bestVein = sample;
            }
        }

        PrabornaPyrolusiteZone rawZone = PrabornaPyrolusiteZone.None;

        // Inside high-pressure manganese lens
        if (bestLens.HasValue)
        {
            if (depth <= oxidationDepth)
            {
                // Shallow oxidized phase: Pyrolusite, Manganite, Nsutite, Chalcophanite -> Pyrolusite
                if (grain > 0.10) rawZone = PrabornaPyrolusiteZone.Pyrolusite;
            }
            else
            {
                // Deeper massive metamorphic ore
                if (bestLens.Value.Lateral > 0.68 && grain > 0.40)
                    rawZone = PrabornaPyrolusiteZone.Quartz;
                else if (bestLens.Value.VerticalNorm > 0.18 && grain > 0.58)
                    rawZone = PrabornaPyrolusiteZone.Rhodochrosite; // piemontite -> rhodochrosite
                else if (bestLens.Value.Lateral > 0.46 && grain > 0.78)
                    rawZone = PrabornaPyrolusiteZone.Spessartine;
                else if (bestLens.Value.VerticalNorm > 0.42 && grain > 0.56)
                    rawZone = PrabornaPyrolusiteZone.Rhodochrosite; // rhodonite -> rhodochrosite
                else if (bestLens.Value.Lateral < 0.38 && grain > 0.84)
                    rawZone = PrabornaPyrolusiteZone.Rhodochrosite;
                else if (grain > 0.16)
                    rawZone = PrabornaPyrolusiteZone.Braunite;
            }
        }
        else if (bestVein.HasValue && bestVein.Value.Inside)
        {
            // Inside sigmoidal quartz vein
            if (depth <= oxidationDepth)
            {
                if (grain > 0.12) rawZone = PrabornaPyrolusiteZone.Pyrolusite;
            }
            else
            {
                if (grain > 0.68) rawZone = PrabornaPyrolusiteZone.Rhodochrosite;
                else if (grain > 0.42) rawZone = PrabornaPyrolusiteZone.None;
                else rawZone = PrabornaPyrolusiteZone.Quartz;
            }
        }
        else
        {
            // Boudinaged stratiform chert bed
            StratiformSample stratiformBand = SampleStratiformBand(x, z, stratY);
            if (stratiformBand.InBand && stratiformBand.Boudin)
            {
                rawZone = (stratiformBand.OrePatch && grain > 0.72)
                    ? PrabornaPyrolusiteZone.Braunite
                    : PrabornaPyrolusiteZone.Quartz;
            }
            else
            {
                // Alteration halos and breccia
                bool nearOre = (bestVein.HasValue && bestVein.Value.InHalo) || mainSample.InHalo;
                if (!nearOre)
                {
                    foreach (PrabornaLens sub in subLenses)
                    {
                        if (sub.Sample(x, y, z, stratY).InHalo) { nearOre = true; break; }
                    }
                }

                if (nearOre)
                {
                    double alterationField = Math.Sin(x * 0.22 + seed) + Math.Cos(z * 0.19 - seed * 0.5);
                    if (grain > 0.88) rawZone = PrabornaPyrolusiteZone.Breccia;
                    else if (depth <= oxidationDepth && grain > 0.78) rawZone = PrabornaPyrolusiteZone.Hematite;
                    else if (alterationField > 0.90 && grain > 0.58) rawZone = PrabornaPyrolusiteZone.Rhodochrosite; // piemontite
                    else if (alterationField < -0.72 && grain > 0.52) rawZone = PrabornaPyrolusiteZone.Spessartine;
                    else if (alterationField > 0.38) rawZone = PrabornaPyrolusiteZone.None;
                    else if (alterationField < -0.35) rawZone = PrabornaPyrolusiteZone.None;
                    else rawZone = PrabornaPyrolusiteZone.Quartz;
                }
            }
        }

        return ApplyWeatheringHook(rawZone, depth);
    }

    public static PrabornaPyrolusiteZone ApplyWeatheringHook(PrabornaPyrolusiteZone zone, int depth)
    {
        if (zone == PrabornaPyrolusiteZone.None) return PrabornaPyrolusiteZone.None;

        // Supergene front:
        // "Pyrolusite exists at shallow depths (2-6 with an advancing front) in the supergene zone.
        // The ore generates up to 1 beneath the surface and emplaces a new black-stained gossan block"
        if (depth <= 1)
        {
            return PrabornaPyrolusiteZone.BlackGossan;
        }

        if (depth >= 2 && depth <= 6)
        {
            // Supergene advancing oxidation front converts non-quartz primary ores to pyrolusite
            return zone == PrabornaPyrolusiteZone.Quartz
                ? PrabornaPyrolusiteZone.Quartz
                : PrabornaPyrolusiteZone.Pyrolusite;
        }

        return zone;
    }

    private StratiformSample SampleStratiformBand(double x, double z, double stratY)
    {
        double dx = x - systemX;
        double dz = z - systemZ;
        double along = dx * cosStrike - dz * sinStrike;
        double across = dx * sinStrike + dz * cosStrike;

        double bandCenter = contactY
            + Math.Sin(along * 0.085 + seed) * 0.35
            + Math.Cos(across * 0.075 - seed * 0.4) * 0.18;
        double bandDistance = Math.Abs(stratY - bandCenter);

        double boudinField = Math.Sin(along * 0.11 + seed) * 0.58
            + Math.Sin(along * 0.23 - seed * 0.35) * 0.28
            + Math.Cos(across * 0.09 + seed * 0.2) * 0.14;
        double widthLimit = 31.0 + Math.Sin(along * 0.07 + seed) * 4.0;

        bool inBand = bandDistance <= 0.72 && Math.Abs(across) <= widthLimit;
        bool boudin = boudinField > -0.18;
        bool orePatch = boudinField > 0.22;

        return new StratiformSample(inBand, boudin, orePatch, bandDistance);
    }

    private static double Hash3D(double x, double y, double z)
    {
        double n = Math.Sin(x * 12.9898 + y * 78.233 + z * 37.719) * 43758.5453;
        return n - Math.Floor(n);
    }

    private readonly record struct LensSample(
        double Radial,
        double Lateral,
        double VerticalNorm,
        bool Inside,
        bool InHalo);

    private readonly record struct VeinSample(
        double Distance,
        double Score,
        bool Inside,
        bool InHalo);

    private readonly record struct StratiformSample(
        bool InBand,
        bool Boudin,
        bool OrePatch,
        double Distance);

    private readonly record struct PrabornaLens(
        double CenterX,
        double CenterZ,
        double CenterStratY,
        double RadiusAlong,
        double RadiusAcross,
        double RadiusVertical,
        double CosStrike,
        double SinStrike,
        double Phase,
        bool IsMain)
    {
        public static PrabornaLens Create(
            double centerX,
            double centerZ,
            double centerStratY,
            double radiusAlong,
            double radiusAcross,
            double radiusVertical,
            double strikeDeg,
            double phase,
            bool isMain)
        {
            double strikeRad = strikeDeg * Math.PI / 180.0;
            return new PrabornaLens(
                centerX,
                centerZ,
                centerStratY,
                radiusAlong,
                radiusAcross,
                radiusVertical,
                Math.Cos(strikeRad),
                Math.Sin(strikeRad),
                phase,
                isMain);
        }

        public LensSample Sample(double x, double y, double z, double stratY)
        {
            double dx = x - CenterX;
            double dz = z - CenterZ;
            double along = dx * CosStrike - dz * SinStrike;
            double across = dx * SinStrike + dz * CosStrike;

            double u = along / RadiusAlong;
            double v = across / RadiusAcross;
            double w = (stratY - CenterStratY) / RadiusVertical;

            double edgeWarp = Math.Sin(u * 3.7 + Phase) * Math.Cos(v * 3.0) * 0.14
                + Math.Sin(u * 7.0 - v * 4.3 + Phase * 0.5) * 0.06;
            double radial = Math.Sqrt(u * u + v * v + w * w) + edgeWarp;
            double lateral = Math.Sqrt(u * u + v * v);

            bool inside = radial <= 1.02;
            bool inHalo = radial <= 1.20;

            return new LensSample(radial, lateral, w, inside, inHalo);
        }
    }

    private readonly record struct PrabornaVein(
        double CenterX,
        double CenterZ,
        double CenterY,
        double StrikeDeg,
        double DipDeg,
        double SinStrike,
        double CosStrike,
        double SinDip,
        double CosDip,
        double Offset,
        double CenterAlong,
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
        public static PrabornaVein Create(
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

            return new PrabornaVein(
                centerX,
                centerZ,
                centerY,
                strikeDeg,
                dipDeg,
                Math.Sin(strikeRad),
                Math.Cos(strikeRad),
                Math.Sin(dipRad),
                Math.Cos(dipRad),
                across,
                along,
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
                + Math.Cos(along * 0.30 + Phase * 1.3) * (BendAmp * 0.28);
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
            bool inHalo = footprint <= 1.14 && distance <= halfThickness + 2.8 * taper;

            return new VeinSample(distance, score, inside, inHalo);
        }
    }
}
