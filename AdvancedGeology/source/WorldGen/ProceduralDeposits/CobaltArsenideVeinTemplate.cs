using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using Vintagestory.API.Server;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace AdvancedGeology.WorldGen.ProceduralDeposits;

public enum CobaltArsenideVeinZone
{
    None = 0,
    Erythrite,
    Annabergite,
    Scorodite,
    Acanthite,
    NativeSilver,
    NativeBismuth,
    Nickeline,
    Skutterudite,
    Cobaltite,
    NiCoDiarsenide,
    EarlyPyrite,
    CarbonateGangue
}

[JsonObject(MemberSerialization.OptIn)]
public sealed class CobaltArsenideVeinDefinition
{
    // Model extent is 84x70. Principal veins reach lengthHalf 28 and heightHalf 38 from an
    // offset walk of up to 7 * 10 blocks, so the reach must contain the whole swarm.
    [JsonProperty]
    public int HorizontalRadius { get; set; } = 46;

    [JsonProperty]
    public int VerticalHalfHeight { get; set; } = 35;

    [JsonProperty]
    public int SupergeneDepth { get; set; } = 8;

    /// <summary>
    /// Blocks over which a sill contact enhances fracture aperture and grade
    /// (Model <c>SILL_CONTACT_INFLUENCE</c>).
    /// </summary>
    [JsonProperty]
    public double SillContactInfluence { get; set; } = 5.0;

    [JsonProperty]
    public double DipAngleDegMin { get; set; } = 70.0;

    [JsonProperty]
    public double DipAngleDegMax { get; set; } = 87.0;

    [JsonProperty]
    public double StrikeAngleDegMin { get; set; } = -40.0;

    [JsonProperty]
    public double StrikeAngleDegMax { get; set; } = 40.0;

    [JsonProperty]
    public int PrincipalCountMin { get; set; } = 4;

    [JsonProperty]
    public int PrincipalCountMax { get; set; } = 8;

    [JsonProperty]
    public int SplayCountMin { get; set; } = 1;

    [JsonProperty]
    public int SplayCountMax { get; set; } = 3;

    [JsonProperty]
    public double ContactBaseYMin { get; set; } = -3.0;

    [JsonProperty]
    public double ContactBaseYMax { get; set; } = 2.0;

    [JsonProperty]
    public double SillThicknessMin { get; set; } = 8.0;

    [JsonProperty]
    public double SillThicknessMax { get; set; } = 14.0;

    [JsonProperty]
    public double ContactTiltXMin { get; set; } = -0.09;

    [JsonProperty]
    public double ContactTiltXMax { get; set; } = 0.09;

    [JsonProperty]
    public double ContactTiltZMin { get; set; } = -0.07;

    [JsonProperty]
    public double ContactTiltZMax { get; set; } = 0.07;

    [JsonProperty]
    public double OffsetWalkMin { get; set; } = 6.0;

    [JsonProperty]
    public double OffsetWalkMax { get; set; } = 10.0;

    [JsonProperty]
    public double PrincipalStrikeJitter { get; set; } = 14.0;

    [JsonProperty]
    public double PrincipalDipJitter { get; set; } = 8.0;

    [JsonProperty]
    public double PrincipalOffsetJitter { get; set; } = 1.5;

    [JsonProperty]
    public double PrincipalCenterAlongSpread { get; set; } = 7.0;

    [JsonProperty]
    public double PrincipalCenterYMin { get; set; } = -3.0;

    [JsonProperty]
    public double PrincipalCenterYMax { get; set; } = 1.0;

    [JsonProperty]
    public double PrincipalLengthHalfMin { get; set; } = 17.0;

    [JsonProperty]
    public double PrincipalLengthHalfMax { get; set; } = 28.0;

    [JsonProperty]
    public double PrincipalHeightHalfMin { get; set; } = 29.0;

    [JsonProperty]
    public double PrincipalHeightHalfMax { get; set; } = 38.0;

    [JsonProperty]
    public double PrincipalThicknessMin { get; set; } = 1.4;

    [JsonProperty]
    public double PrincipalThicknessMax { get; set; } = 3.0;

    [JsonProperty]
    public double PrincipalRelayCenterSpread { get; set; } = 10.0;

    [JsonProperty]
    public double PrincipalRelayWidthMin { get; set; } = 2.5;

    [JsonProperty]
    public double PrincipalRelayWidthMax { get; set; } = 5.0;

    [JsonProperty]
    public double PrincipalRelayShiftSpread { get; set; } = 1.5;

    [JsonProperty]
    public double SplayStrikeOffsetMin { get; set; } = 16.0;

    [JsonProperty]
    public double SplayStrikeOffsetMax { get; set; } = 30.0;

    [JsonProperty]
    public double SplayDipJitter { get; set; } = 9.0;

    [JsonProperty]
    public double SplayOffsetMin { get; set; } = 2.0;

    [JsonProperty]
    public double SplayOffsetMax { get; set; } = 4.5;

    [JsonProperty]
    public double SplayAlongOffsetMin { get; set; } = 5.0;

    [JsonProperty]
    public double SplayAlongOffsetMax { get; set; } = 11.0;

    [JsonProperty]
    public double SplayCenterYMin { get; set; } = -3.0;

    [JsonProperty]
    public double SplayCenterYMax { get; set; } = 4.0;

    [JsonProperty]
    public double SplayLengthHalfMin { get; set; } = 10.0;

    [JsonProperty]
    public double SplayLengthHalfMax { get; set; } = 18.0;

    [JsonProperty]
    public double SplayHeightHalfMin { get; set; } = 20.0;

    [JsonProperty]
    public double SplayHeightHalfMax { get; set; } = 30.0;

    [JsonProperty]
    public double SplayThicknessMin { get; set; } = 1.0;

    [JsonProperty]
    public double SplayThicknessMax { get; set; } = 2.1;

    [JsonProperty]
    public double SplayRelayCenterSpread { get; set; } = 5.0;

    [JsonProperty]
    public double SplayRelayWidthMin { get; set; } = 2.0;

    [JsonProperty]
    public double SplayRelayWidthMax { get; set; } = 4.0;

    [JsonProperty]
    public double SplayRelayShiftSpread { get; set; } = 1.0;
}

/// <summary>
/// Result of classifying one voxel against a <see cref="CobaltArsenideVeinPlan"/>.
/// </summary>
public readonly record struct CobaltArsenideVeinSample(
    CobaltArsenideVeinZone Zone,
    int Grade = 0,
    bool InVein = false,
    bool InHalo = false,
    bool Oxidized = false,
    double CenterRatio = 0.0,
    double SpeckleNoise = 0.0);

/// <summary>
/// Nickeline, cobaltite, silver, bismuth, pyrite, carbonate gangue, erythrite, annabergite, and acanthite occupy steep fissure veins and splays concentrated along diabase sill contacts.
/// </summary>
internal sealed class CobaltArsenideVeinPlan
{
    private const ulong PlanSalt = 0x434F42414C544152UL; // "COBALTAR"

    // Local model floor: y < -35 is below the modelled district (GRID_Y / 2).
    private const double FloorY = -35.0;

    /// <summary>
    /// Skutterudite is nickeline with 10% cobaltite speckling, so the grain gate opens above 0.90.
    /// The remaining skutterudite voxels resolve to nickeline.
    /// </summary>
    private const double CobaltiteSpeckleThreshold = 0.90;

    private readonly ulong featureId;
    private readonly int originX;
    private readonly int originY;
    private readonly int originZ;

    private readonly double dipAngleDeg;
    private readonly double strikeAngleDeg;
    private readonly double dipDirection;
    private readonly double contactBaseY;
    private readonly double sillThickness;
    private readonly double contactTiltX;
    private readonly double contactTiltZ;
    private readonly double seed;
    private readonly double supergeneDepth;
    private readonly double sillContactInfluence;

    private readonly ArsenideVein[] veins;
    private readonly int principalCount;

    public int OriginX => originX;
    public int OriginY => originY;
    public int OriginZ => originZ;
    public double Seed => seed;
    public double DipAngleDeg => dipAngleDeg;
    public double StrikeAngleDeg => strikeAngleDeg;
    public double SillThickness => sillThickness;
    public int PrincipalVeinCount => principalCount;
    public int SplayCount => veins.Length - principalCount;
    public int VeinCount => veins.Length;

    private CobaltArsenideVeinPlan(
        ulong featureId,
        in ProceduralDepositInstance instance,
        double dipAngleDeg,
        double strikeAngleDeg,
        double dipDirection,
        double contactBaseY,
        double sillThickness,
        double contactTiltX,
        double contactTiltZ,
        double seed,
        double supergeneDepth,
        double sillContactInfluence,
        ArsenideVein[] veins,
        int principalCount)
    {
        this.featureId = featureId;
        originX = instance.CenterX;
        originY = instance.CenterY;
        originZ = instance.CenterZ;
        this.dipAngleDeg = dipAngleDeg;
        this.strikeAngleDeg = strikeAngleDeg;
        this.dipDirection = dipDirection;
        this.contactBaseY = contactBaseY;
        this.sillThickness = sillThickness;
        this.contactTiltX = contactTiltX;
        this.contactTiltZ = contactTiltZ;
        this.seed = seed;
        this.supergeneDepth = supergeneDepth;
        this.sillContactInfluence = sillContactInfluence;
        this.veins = veins;
        this.principalCount = principalCount;
    }

    /// <summary>
    /// Builds the fault family in deterministic parameter order.
    /// </summary>
    public static CobaltArsenideVeinPlan Create(
        in ProceduralDepositInstance instance,
        CobaltArsenideVeinDefinition settings)
    {
        var random = new ProceduralDepositRandom(instance.FeatureId ^ PlanSalt);

        double dipAngleDeg = random.Range(settings.DipAngleDegMin, settings.DipAngleDegMax);
        double strikeAngleDeg = random.Range(settings.StrikeAngleDegMin, settings.StrikeAngleDegMax);
        double dipDirection = random.Range(0.0, 1.0) > 0.5 ? 1.0 : -1.0;
        int principalCount = random.NextInt(settings.PrincipalCountMin, settings.PrincipalCountMax);
        int splayCount = random.NextInt(settings.SplayCountMin, settings.SplayCountMax);
        double contactBaseY = random.Range(settings.ContactBaseYMin, settings.ContactBaseYMax);
        double sillThickness = random.Range(settings.SillThicknessMin, settings.SillThicknessMax);
        double contactTiltX = random.Range(settings.ContactTiltXMin, settings.ContactTiltXMax);
        double contactTiltZ = random.Range(settings.ContactTiltZMin, settings.ContactTiltZMax);
        double seed = random.Range(0.0, 100.0);

        // Discrete veins stay separated by barren diabase: the offset walk spaces them out.
        var offsets = new double[principalCount];
        offsets[0] = 0.0;
        for (int i = 1; i < principalCount; i++)
        {
            offsets[i] = offsets[i - 1] + random.Range(settings.OffsetWalkMin, settings.OffsetWalkMax);
        }

        double offsetCenter = (offsets[0] + offsets[principalCount - 1]) / 2.0;

        var built = new List<ArsenideVein>(principalCount + splayCount);
        for (int i = 0; i < principalCount; i++)
        {
            double strikeDeg = strikeAngleDeg
                + random.Range(-settings.PrincipalStrikeJitter, settings.PrincipalStrikeJitter);
            double dipDeg = dipAngleDeg
                + random.Range(-settings.PrincipalDipJitter, settings.PrincipalDipJitter);
            double offset = offsets[i] - offsetCenter
                + random.Range(-settings.PrincipalOffsetJitter, settings.PrincipalOffsetJitter);
            double centerAlong = random.Range(
                -settings.PrincipalCenterAlongSpread,
                settings.PrincipalCenterAlongSpread);
            double centerY = random.Range(settings.PrincipalCenterYMin, settings.PrincipalCenterYMax);
            double lengthHalf = random.Range(settings.PrincipalLengthHalfMin, settings.PrincipalLengthHalfMax);
            double heightHalf = random.Range(settings.PrincipalHeightHalfMin, settings.PrincipalHeightHalfMax);
            double maxThickness = random.Range(settings.PrincipalThicknessMin, settings.PrincipalThicknessMax);
            double relayCenter = random.Range(
                -settings.PrincipalRelayCenterSpread,
                settings.PrincipalRelayCenterSpread);
            double relayWidth = random.Range(settings.PrincipalRelayWidthMin, settings.PrincipalRelayWidthMax);
            double relayShift = random.Range(
                -settings.PrincipalRelayShiftSpread,
                settings.PrincipalRelayShiftSpread);

            built.Add(CreateVein(
                strikeDeg,
                dipDeg,
                offset,
                centerAlong,
                centerY,
                lengthHalf,
                heightHalf,
                maxThickness,
                seed + i * 13.7,
                relayCenter,
                relayWidth,
                relayShift,
                seed + i * 5.2,
                seed + 47.0 + i * 29.0,
                false));
        }

        for (int i = 0; i < splayCount; i++)
        {
            ArsenideVein parent = built[random.NextInt(0, principalCount - 1)];
            double direction = i % 2 == 0 ? -1.0 : 1.0;
            double strikeDeg = parent.StrikeDeg
                + direction * random.Range(settings.SplayStrikeOffsetMin, settings.SplayStrikeOffsetMax);
            double dipDeg = parent.DipDeg + random.Range(-settings.SplayDipJitter, settings.SplayDipJitter);
            double offset = parent.Offset
                + direction * random.Range(settings.SplayOffsetMin, settings.SplayOffsetMax);
            double centerAlong = parent.CenterAlong
                + direction * random.Range(settings.SplayAlongOffsetMin, settings.SplayAlongOffsetMax);
            double centerY = parent.CenterY + random.Range(settings.SplayCenterYMin, settings.SplayCenterYMax);
            double lengthHalf = random.Range(settings.SplayLengthHalfMin, settings.SplayLengthHalfMax);
            double heightHalf = random.Range(settings.SplayHeightHalfMin, settings.SplayHeightHalfMax);
            double maxThickness = random.Range(settings.SplayThicknessMin, settings.SplayThicknessMax);
            double relayCenter = random.Range(-settings.SplayRelayCenterSpread, settings.SplayRelayCenterSpread);
            double relayWidth = random.Range(settings.SplayRelayWidthMin, settings.SplayRelayWidthMax);
            double relayShift = random.Range(-settings.SplayRelayShiftSpread, settings.SplayRelayShiftSpread);

            built.Add(CreateVein(
                strikeDeg,
                dipDeg,
                offset,
                centerAlong,
                centerY,
                lengthHalf,
                heightHalf,
                maxThickness,
                seed + 91.0 + i * 19.0,
                relayCenter,
                relayWidth,
                relayShift,
                seed + 41.0 + i * 6.0,
                seed + 131.0 + i * 23.0,
                true));
        }

        return new CobaltArsenideVeinPlan(
            instance.FeatureId,
            instance,
            dipAngleDeg,
            strikeAngleDeg,
            dipDirection,
            contactBaseY,
            sillThickness,
            contactTiltX,
            contactTiltZ,
            seed,
            settings.SupergeneDepth,
            settings.SillContactInfluence,
            built.ToArray(),
            principalCount);
    }

    private static ArsenideVein CreateVein(
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
        bool isSplay)
    {
        double strikeRad = strikeDeg * Math.PI / 180.0;
        double dipRad = dipDeg * Math.PI / 180.0;
        return new ArsenideVein(
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
            isSplay);
    }

    /// <summary><c>getSillLowerContactY</c>: tilted, undulating sill floor.</summary>
    public double GetSillLowerContactY(double x, double z)
    {
        double undulation = Math.Sin(x * 0.055 + seed * 0.4) * 1.5
            + Math.Cos(z * 0.048 - seed * 0.7) * 1.2;
        return contactBaseY + contactTiltX * x + contactTiltZ * z + undulation;
    }

    /// <summary>Tilted, undulating sill roof.</summary>
    public double GetSillUpperContactY(double x, double z)
    {
        return GetSillLowerContactY(x, z) + sillThickness;
    }

    /// <summary>
    /// <c>getOreWindowFactor</c>: inside the sill, aperture grows toward either contact.
    /// </summary>
    private double GetOreWindowFactor(double y, double lowerContactY, double upperContactY)
    {
        if (y <= lowerContactY || y >= upperContactY) return 0.0;

        double lowerProximity = Clamp(1.0 - (y - lowerContactY) / sillContactInfluence, 0.0, 1.0);
        double upperProximity = Clamp(1.0 - (upperContactY - y) / sillContactInfluence, 0.0, 1.0);
        return 0.35 + 0.65 * Math.Max(lowerProximity, upperProximity);
    }

    public CobaltArsenideVeinSample Evaluate(
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
        if (y < FloorY) return default;

        double depth = surfaceY < int.MaxValue ? surfaceY - worldY : 999.0;
        double lowerContactY = GetSillLowerContactY(x, z);
        double upperContactY = GetSillUpperContactY(x, z);

        // District fissures cross all three units; the sill contacts only enhance aperture.
        double windowFactor = 0.72 + 0.28 * GetOreWindowFactor(y, lowerContactY, upperContactY);

        VeinSample? bestVein = null;
        if (windowFactor > 0.02)
        {
            for (int i = 0; i < veins.Length; i++)
            {
                VeinSample sample = SampleVein(x, y, z, veins[i], windowFactor);
                if (double.IsFinite(sample.Score)
                    && (!bestVein.HasValue || sample.Score < bestVein.Value.Score))
                {
                    bestVein = sample;
                }
            }
        }

        if (!bestVein.HasValue || (!bestVein.Value.Inside && !bestVein.Value.InHalo)) return default;

        VeinSample vein = bestVein.Value;
        double shootField = Math.Sin(vein.Along * 0.10 - vein.StagePhase * 0.7)
            + 0.45 * Math.Cos(vein.VerticalNorm * 2.5 + vein.StagePhase);
        double silverShoot = Math.Cos((vein.AlongNorm - 0.12) * 5.2 + vein.StagePhase)
            - 0.55 * Math.Abs(vein.VerticalNorm + 0.08);
        double contactProximity = Math.Max(
            1.0 - Clamp(Math.Abs(y - lowerContactY) / sillContactInfluence, 0.0, 1.0),
            1.0 - Clamp(Math.Abs(y - upperContactY) / sillContactInfluence, 0.0, 1.0));

    // The model carries its own oxidation branch, keyed on a wobbling front.
        double oxidationFront = supergeneDepth
            + Math.Sin(x * 0.13 + seed) * 2.6
            + Math.Cos(z * 0.11 - seed * 0.6) * 2.2
            + (vein.CenterRatio < 0.6 ? 3.0 : 0.0);

        if (weatheringEnabled && depth <= oxidationFront)
        {
            double bloomField = Math.Sin(vein.Along * 0.22 + seed) + 0.4 * Math.Cos(y * 0.31);

            if (!vein.Inside)
            {
                return Finish(
                    bloomField > 0.25 ? CobaltArsenideVeinZone.Scorodite : CobaltArsenideVeinZone.None,
                    x,
                    y,
                    z,
                    false,
                    true,
                    true,
                    vein.CenterRatio);
            }

            if (vein.CenterRatio < 0.34 && silverShoot > 0.72)
            {
                return Finish(CobaltArsenideVeinZone.Acanthite, x, y, z, true, false, true, vein.CenterRatio);
            }

            if (vein.CenterRatio < 0.72)
            {
                return Finish(
                    shootField > 0.05 ? CobaltArsenideVeinZone.Erythrite : CobaltArsenideVeinZone.Annabergite,
                    x,
                    y,
                    z,
                    true,
                    false,
                    true,
                    vein.CenterRatio);
            }

            return Finish(CobaltArsenideVeinZone.Scorodite, x, y, z, true, false, true, vein.CenterRatio);
        }

        if (!vein.Inside)
        {
            return Finish(CobaltArsenideVeinZone.None, x, y, z, false, true, false, vein.CenterRatio);
        }

        // Early wall pyrite and carbonate frame a nickel-to-cobalt arsenide succession.
        double r = vein.CenterRatio;
        if (r > 0.90)
        {
            return Finish(
                shootField < -0.15
                    ? CobaltArsenideVeinZone.EarlyPyrite
                    : CobaltArsenideVeinZone.CarbonateGangue,
                x,
                y,
                z,
                true,
                false,
                false,
                r);
        }

        if (r > 0.76)
        {
            return Finish(CobaltArsenideVeinZone.CarbonateGangue, x, y, z, true, false, false, r);
        }

        if (r < 0.23 && silverShoot + contactProximity * 0.55 > 1.05)
        {
            return Finish(CobaltArsenideVeinZone.NativeSilver, x, y, z, true, false, false, r);
        }

        if (r < 0.18 && silverShoot < -1.20)
        {
            return Finish(CobaltArsenideVeinZone.NativeBismuth, x, y, z, true, false, false, r);
        }

        if (r < 0.32)
        {
            return Finish(CobaltArsenideVeinZone.Nickeline, x, y, z, true, false, false, r);
        }

        if (r < 0.58)
        {
            return Finish(CobaltArsenideVeinZone.NiCoDiarsenide, x, y, z, true, false, false, r);
        }

        return Finish(CobaltArsenideVeinZone.Skutterudite, x, y, z, true, false, false, r);
    }

    private CobaltArsenideVeinSample Finish(
        CobaltArsenideVeinZone zone,
        double x,
        double y,
        double z,
        bool inVein,
        bool inHalo,
        bool oxidized,
        double centerRatio)
    {
        if (zone == CobaltArsenideVeinZone.Scorodite) zone = CobaltArsenideVeinZone.None;

    // Skutterudite maps to nickeline with 10% cobaltite speckling.
        double speckle = 0.0;
        if (zone == CobaltArsenideVeinZone.Skutterudite)
        {
            speckle = Hash3D(x * 2.1 + seed, y * 2.1, z * 2.1);
            zone = speckle >= CobaltiteSpeckleThreshold
                ? CobaltArsenideVeinZone.Cobaltite
                : CobaltArsenideVeinZone.Nickeline;
        }

        int grade = 0;
        if (IsGraded(zone))
        {
            double grainNoise = Hash3D(x * 2.7 + seed * 1.3, y * 2.7, z * 2.7);
            grade = grainNoise > 0.82 ? 3 : (grainNoise > 0.55 ? 2 : (grainNoise > 0.25 ? 1 : 0));
        }

        return new CobaltArsenideVeinSample(zone, grade, inVein, inHalo, oxidized, centerRatio, speckle);
    }

    internal static bool IsGraded(CobaltArsenideVeinZone zone)
    {
        return zone == CobaltArsenideVeinZone.Nickeline
            || zone == CobaltArsenideVeinZone.Cobaltite
            || zone == CobaltArsenideVeinZone.NiCoDiarsenide
            || zone == CobaltArsenideVeinZone.Skutterudite
            || zone == CobaltArsenideVeinZone.NativeSilver
            || zone == CobaltArsenideVeinZone.NativeBismuth
            || zone == CobaltArsenideVeinZone.Acanthite
            || zone == CobaltArsenideVeinZone.Erythrite
            || zone == CobaltArsenideVeinZone.Annabergite;
    }

    /// <summary>
    /// <c>getVeinSample</c>: dipping plane with a bend, a <c>tanh</c> relay jog, an
    /// edge-warped elliptical footprint, and a pinch-and-swell aperture scaled by the ore window.
    /// </summary>
    private VeinSample SampleVein(double x, double y, double z, in ArsenideVein vein, double windowFactor)
    {
        double along = x * vein.CosStrike - z * vein.SinStrike;
        double across = x * vein.SinStrike + z * vein.CosStrike - vein.Offset;
        double vertical = y - vein.CenterY;
        double planeAcross = -vertical * vein.CosDip / Math.Max(0.12, vein.SinDip) * dipDirection;
        double bend = Math.Sin(along * 0.11 + y * 0.06 + vein.Phase) * 2.4
            + Math.Cos(along * 0.30 + vein.Phase * 1.3) * 0.8;
        double relay = vein.RelayShift * Math.Tanh((along - vein.RelayCenter) / vein.RelayWidth);
        double signedDistance = (across - planeAcross) * vein.SinDip - bend - relay;

        double u = (along - vein.CenterAlong) / vein.LengthHalf;
        double v = vertical / vein.HeightHalf;
        double edgeWarp = Math.Sin(u * 3.6 + vein.Phase) * Math.Cos(v * 3.0) * 0.15
            + Math.Sin(u * 7.1 - v * 3.2 + vein.Phase * 0.5) * 0.07;
        double footprint = Math.Sqrt(u * u + v * v) + edgeWarp;
        double edgeTaper = Clamp(1.0 - footprint * footprint, 0.0, 1.0);
        double halfThickness = Math.Max(
            0.10,
            vein.MaxThickness * 0.5
                * (0.82 + 0.18 * Math.Sin(along * 0.18 + vein.PinchPhase))
                * Math.Sqrt(edgeTaper)
                * windowFactor);
        double distance = Math.Abs(signedDistance);
        bool openAperture = windowFactor > 0.02;

        return new VeinSample(
            distance,
            footprint <= 1.18 && openAperture ? distance / halfThickness : double.PositiveInfinity,
            openAperture && footprint <= 1.04 && distance <= halfThickness,
            openAperture && footprint <= 1.12 && distance <= halfThickness + 1.6 * edgeTaper,
            distance / Math.Max(0.1, halfThickness),
            along,
            u,
            v,
            vein.StagePhase);
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

    private readonly record struct VeinSample(
        double Distance,
        double Score,
        bool Inside,
        bool InHalo,
        double CenterRatio,
        double Along,
        double AlongNorm,
        double VerticalNorm,
        double StagePhase);

    private readonly record struct ArsenideVein(
        double StrikeDeg,
        double DipDeg,
        double SinStrike,
        double CosStrike,
        double SinDip,
        double CosDip,
        double Offset,
        double CenterAlong,
        double CenterY,
        double LengthHalf,
        double HeightHalf,
        double MaxThickness,
        double Phase,
        double RelayCenter,
        double RelayWidth,
        double RelayShift,
        double PinchPhase,
        double StagePhase,
        bool IsSplay);
}

internal sealed class CobaltArsenideVeinProceduralTemplate : IProceduralDepositTemplate
{
    private static readonly string[] RequiredSlots =
    {
        ProceduralMaterialSlots.Nickeline,
        ProceduralMaterialSlots.Cobaltite,
        ProceduralMaterialSlots.NativeSilver,
        ProceduralMaterialSlots.NativeBismuth,
        ProceduralMaterialSlots.Acanthite,
        ProceduralMaterialSlots.Pyrite,
        ProceduralMaterialSlots.Carbonate,
        ProceduralMaterialSlots.Erythrite,
        ProceduralMaterialSlots.Annabergite
    };

    public string Code => "cobaltArsenideVein";
    public IReadOnlyList<string> RequiredMaterialSlots => RequiredSlots;
    public bool RequiresPlan => true;

    public int GetMaximumHorizontalReach(ProceduralDepositDefinition definition)
    {
        return definition.CobaltArsenideVein.HorizontalRadius;
    }

    public object CreatePlan(in ProceduralDepositInstance instance, ProceduralDepositDefinition definition)
    {
        return CobaltArsenideVeinPlan.Create(instance, definition.CobaltArsenideVein);
    }

    public bool Validate(ProceduralDepositDefinition definition, out string error)
    {
        CobaltArsenideVeinDefinition settings = definition.CobaltArsenideVein;
        bool valid = settings.HorizontalRadius >= 16
            && settings.VerticalHalfHeight >= 8
            && settings.SupergeneDepth >= 0
            && settings.SillContactInfluence > 0.0
            && settings.DipAngleDegMax >= settings.DipAngleDegMin
            && settings.StrikeAngleDegMax >= settings.StrikeAngleDegMin
            && settings.PrincipalCountMin >= 1
            && settings.PrincipalCountMax >= settings.PrincipalCountMin
            && settings.SplayCountMin >= 0
            && settings.SplayCountMax >= settings.SplayCountMin
            && settings.ContactBaseYMax >= settings.ContactBaseYMin
            && settings.SillThicknessMin > 0.0
            && settings.SillThicknessMax >= settings.SillThicknessMin
            && settings.ContactTiltXMax >= settings.ContactTiltXMin
            && settings.ContactTiltZMax >= settings.ContactTiltZMin
            && settings.OffsetWalkMin > 0.0
            && settings.OffsetWalkMax >= settings.OffsetWalkMin
            && settings.PrincipalCenterYMax >= settings.PrincipalCenterYMin
            && settings.PrincipalLengthHalfMin > 0.0
            && settings.PrincipalLengthHalfMax >= settings.PrincipalLengthHalfMin
            && settings.PrincipalHeightHalfMin > 0.0
            && settings.PrincipalHeightHalfMax >= settings.PrincipalHeightHalfMin
            && settings.PrincipalThicknessMin > 0.0
            && settings.PrincipalThicknessMax >= settings.PrincipalThicknessMin
            && settings.PrincipalRelayWidthMin > 0.0
            && settings.PrincipalRelayWidthMax >= settings.PrincipalRelayWidthMin
            && settings.SplayStrikeOffsetMax >= settings.SplayStrikeOffsetMin
            && settings.SplayOffsetMax >= settings.SplayOffsetMin
            && settings.SplayAlongOffsetMax >= settings.SplayAlongOffsetMin
            && settings.SplayCenterYMax >= settings.SplayCenterYMin
            && settings.SplayLengthHalfMin > 0.0
            && settings.SplayLengthHalfMax >= settings.SplayLengthHalfMin
            && settings.SplayHeightHalfMin > 0.0
            && settings.SplayHeightHalfMax >= settings.SplayHeightHalfMin
            && settings.SplayThicknessMin > 0.0
            && settings.SplayThicknessMax >= settings.SplayThicknessMin
            && settings.SplayRelayWidthMin > 0.0
            && settings.SplayRelayWidthMax >= settings.SplayRelayWidthMin;
        error = valid ? string.Empty : "invalid cobalt arsenide vein settings";
        return valid;
    }

    public void Realize(
        ProceduralDepositWorldGenSystem system,
        in DepositCandidate candidate,
        IChunkColumnGenerateRequest request,
        int baseX,
        int baseZ)
    {
        system.RealizeCobaltArsenideVeinCandidate(candidate, request, baseX, baseZ);
    }
}

public sealed partial class ProceduralDepositWorldGenSystem
{
    internal void RealizeCobaltArsenideVeinCandidate(
        DepositCandidate candidate,
        IChunkColumnGenerateRequest request,
        int baseX,
        int baseZ)
    {
        CompiledProceduralDeposit compiled = candidate.Compiled;
        CobaltArsenideVeinDefinition settings = compiled.Definition.CobaltArsenideVein;
        var plan = (CobaltArsenideVeinPlan)candidate.Plan!;
        ushort[] heightMap = request.Chunks[0].MapChunk.WorldGenTerrainHeightMap;
        int worldHeight = serverApi!.World.BlockAccessor.MapSizeY;
        int minimumY = GameMath.Clamp(candidate.Instance.CenterY - settings.VerticalHalfHeight, 1, worldHeight - 2);
        int maximumPrototypeY = GameMath.Clamp(candidate.Instance.CenterY + settings.VerticalHalfHeight, 1, worldHeight - 2);

        int[] zoneSlots = BuildCobaltArsenideVeinZoneSlots(compiled);
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
                    CobaltArsenideVeinSample sample = plan.Evaluate(
                        worldX,
                        y,
                        worldZ,
                        surfaceY,
                        weatheringEnabled);
                    if (sample.Zone == CobaltArsenideVeinZone.None) continue;

                    int targetSlot = zoneSlots[(int)sample.Zone];
                    if (targetSlot < 0) continue;

                    int chunkY = y / ChunkSize;
                    if ((uint)chunkY >= (uint)request.Chunks.Length) continue;
                    int localY = y % ChunkSize;
                    int index3d = ((localY * ChunkSize) + localZ) * ChunkSize + localX;
                    IChunkBlocks data = request.Chunks[chunkY].Data;
                    int hostBlockId = data.GetBlockIdUnsafe(index3d);

                    if (!CanReplaceWithProceduralRock(compiled, hostBlockId)) continue;

                    int placeBlockId = compiled.ResolveBlock(targetSlot, sample.Grade, hostBlockId);
                    if (placeBlockId == 0 || placeBlockId == hostBlockId) continue;

                    data.SetBlockUnsafe(index3d, placeBlockId);
                    AdvancedGeology.Byproducts.ByproductSystem.RecordPlacement(request.Chunks[chunkY], index3d, placeBlockId, candidate.Instance.FeatureId, compiled);
                    data.SetFluid(index3d, 0);
                }
            }
        }
    }

    private static int[] BuildCobaltArsenideVeinZoneSlots(CompiledProceduralDeposit compiled)
    {
        var slots = new int[Enum.GetValues<CobaltArsenideVeinZone>().Length];
        Array.Fill(slots, -1);

        // Nickeline and the Ni-Co diarsenides are the same nickeline ore; skutterudite already
        // resolved to nickeline or cobaltite through the speckle gate in the plan.
        int nickelineSlot = compiled.GetSlotId(ProceduralMaterialSlots.Nickeline);
        slots[(int)CobaltArsenideVeinZone.Nickeline] = nickelineSlot;
        slots[(int)CobaltArsenideVeinZone.NiCoDiarsenide] = nickelineSlot;
        slots[(int)CobaltArsenideVeinZone.Skutterudite] = nickelineSlot;

        slots[(int)CobaltArsenideVeinZone.Cobaltite] = compiled.GetSlotId(ProceduralMaterialSlots.Cobaltite);
        slots[(int)CobaltArsenideVeinZone.NativeSilver] = compiled.GetSlotId(ProceduralMaterialSlots.NativeSilver);
        slots[(int)CobaltArsenideVeinZone.NativeBismuth] = compiled.GetSlotId(ProceduralMaterialSlots.NativeBismuth);
        slots[(int)CobaltArsenideVeinZone.Acanthite] = compiled.GetSlotId(ProceduralMaterialSlots.Acanthite);
        slots[(int)CobaltArsenideVeinZone.EarlyPyrite] = compiled.GetSlotId(ProceduralMaterialSlots.Pyrite);
        slots[(int)CobaltArsenideVeinZone.CarbonateGangue] = compiled.GetSlotId(ProceduralMaterialSlots.Carbonate);
        slots[(int)CobaltArsenideVeinZone.Erythrite] = compiled.GetSlotId(ProceduralMaterialSlots.Erythrite);
        slots[(int)CobaltArsenideVeinZone.Annabergite] = compiled.GetSlotId(ProceduralMaterialSlots.Annabergite);
        return slots;
    }
}
