using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using Vintagestory.API.Server;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace AdvancedGeology.WorldGen.ProceduralDeposits;

public enum SudburyContactNickelZone
{
    None = 0,
    Pentlandite,
    Chalcopyrite,
    Pyrrhotite,
    Magnetite,
    QuartzDiorite,
    HydrothermalGangue,
    SudburyBreccia,
    Norite,
    UpperSic,
    FootwallRock,
    InclusionXenolith,
    Sperrylite
}

[JsonObject(MemberSerialization.OptIn)]
public sealed class SudburyContactNickelDefinition
{
    // Model extent is 126x106 with DEPOSIT_SCALE 1.5. Embayments sit up to 18 blocks from the
    // system centre with radii to 13, and offset dikes extend 34 further, all in model units.
    [JsonProperty]
    public int HorizontalRadius { get; set; } = 62;

    [JsonProperty]
    public int VerticalHalfHeight { get; set; } = 53;

    /// <summary>
    /// <c>DEPOSIT_SCALE</c>: every deposit and host dimension is this multiple of the
    /// underlying model geometry. World coordinates are divided by it before classification.
    /// </summary>
    [JsonProperty]
    public double DepositScale { get; set; } = 1.5;

    [JsonProperty]
    public double RegionalDipDegMin { get; set; } = 8.0;

    [JsonProperty]
    public double RegionalDipDegMax { get; set; } = 18.0;

    [JsonProperty]
    public double SystemOffsetSpread { get; set; } = 5.0;

    [JsonProperty]
    public int EmbayCountMin { get; set; } = 2;

    [JsonProperty]
    public int EmbayCountMax { get; set; } = 4;

    [JsonProperty]
    public double NoriteThicknessMin { get; set; } = 7.0;

    [JsonProperty]
    public double NoriteThicknessMax { get; set; } = 11.0;

    [JsonProperty]
    public double EmbayAngleJitter { get; set; } = 0.35;

    [JsonProperty]
    public double EmbayDistanceMin { get; set; } = 9.0;

    [JsonProperty]
    public double EmbayDistanceMax { get; set; } = 18.0;

    [JsonProperty]
    public double EmbayRadiusXMin { get; set; } = 8.0;

    [JsonProperty]
    public double EmbayRadiusXMax { get; set; } = 13.0;

    [JsonProperty]
    public double EmbayRadiusZMin { get; set; } = 6.0;

    [JsonProperty]
    public double EmbayRadiusZMax { get; set; } = 10.0;

    [JsonProperty]
    public double EmbayDepthMin { get; set; } = 6.0;

    [JsonProperty]
    public double EmbayDepthMax { get; set; } = 11.0;

    [JsonProperty]
    public double PodCenterJitter { get; set; } = 1.5;

    [JsonProperty]
    public double PodDepthFractionMin { get; set; } = 0.35;

    [JsonProperty]
    public double PodDepthFractionMax { get; set; } = 0.55;

    [JsonProperty]
    public double PodRadiusFactorMin { get; set; } = 0.55;

    [JsonProperty]
    public double PodRadiusFactorMax { get; set; } = 0.75;

    [JsonProperty]
    public double PodRadiusVerticalMin { get; set; } = 3.0;

    [JsonProperty]
    public double PodRadiusVerticalMax { get; set; } = 5.5;

    [JsonProperty]
    public int ContactVeinletCountMin { get; set; } = 2;

    [JsonProperty]
    public int ContactVeinletCountMax { get; set; } = 4;

    [JsonProperty]
    public int FootwallVeinCountMin { get; set; } = 3;

    [JsonProperty]
    public int FootwallVeinCountMax { get; set; } = 6;

    [JsonProperty]
    public int OffsetDikeCountMin { get; set; } = 1;

    [JsonProperty]
    public int OffsetDikeCountMax { get; set; } = 2;

    [JsonProperty]
    public double OffsetDikeDistanceMin { get; set; } = 22.0;

    [JsonProperty]
    public double OffsetDikeDistanceMax { get; set; } = 34.0;

    /// <summary>
    /// Minimum spatial-hash value for a sperrylite speckle inside a PGM-enriched sulfide domain.
    /// 0.995 keeps platinum arsenide to the richest 0.5% of those voxels.
    /// </summary>
    [JsonProperty]
    public double SperryliteSpeckleThreshold { get; set; } = 0.995;
}

/// <summary>
/// Result of classifying one voxel against a <see cref="SudburyContactNickelPlan"/>.
/// </summary>
public readonly record struct SudburyContactNickelSample(
    SudburyContactNickelZone Zone,
    int Grade = 0,
    bool InContactOre = false,
    bool InFootwallVein = false,
    bool InOffsetDike = false,
    bool PgmEnriched = false,
    double SpeckleNoise = 0.0);
/// <summary>
/// Pentlandite, chalcopyrite, pyrrhotite, magnetite, quartz diorite, gangue, and breccia form contact pods in basal embayments with descending footwall veins and offset dike lenses.
/// </summary>
internal sealed class SudburyContactNickelPlan
{
    private const ulong PlanSalt = 0x5355444255525900UL; // "SUDBURY"

    // Local model floor: model y < -35 is below the modelled system.
    private const double FloorY = -35.0;

    private readonly int originX;

    private readonly int originY;
    private readonly int originZ;

    private readonly double depositScale;
    private readonly double regionalDipDeg;
    private readonly double dipDirectionDeg;
    private readonly double systemX;
    private readonly double systemZ;
    private readonly double noriteThickness;
    private readonly double seed;
    private readonly double sperryliteSpeckleThreshold;

    private readonly ContactSurface contact;
    private readonly Embayment[] embayments;
    private readonly OrePod[] orePods;
    private readonly Sheet[] contactVeinlets;
    private readonly Sheet[] footwallVeins;
    private readonly Sheet[] offsetDikes;

    public int OriginX => originX;
    public int OriginY => originY;
    public int OriginZ => originZ;
    public double Seed => seed;
    public double RegionalDipDeg => regionalDipDeg;
    public int EmbaymentCount => embayments.Length;
    public int OrePodCount => orePods.Length;
    public int ContactVeinletCount => contactVeinlets.Length;
    public int FootwallVeinCount => footwallVeins.Length;
    public int OffsetDikeCount => offsetDikes.Length;

    private SudburyContactNickelPlan(
        in ProceduralDepositInstance instance,
        double depositScale,
        double regionalDipDeg,
        double dipDirectionDeg,
        double systemX,
        double systemZ,
        double noriteThickness,
        double seed,
        double sperryliteSpeckleThreshold,
        ContactSurface contact,
        Embayment[] embayments,
        OrePod[] orePods,
        Sheet[] contactVeinlets,
        Sheet[] footwallVeins,
        Sheet[] offsetDikes)
    {
        originX = instance.CenterX;
        originY = instance.CenterY;
        originZ = instance.CenterZ;
        this.depositScale = depositScale;
        this.regionalDipDeg = regionalDipDeg;
        this.dipDirectionDeg = dipDirectionDeg;
        this.systemX = systemX;
        this.systemZ = systemZ;
        this.noriteThickness = noriteThickness;
        this.seed = seed;
        this.sperryliteSpeckleThreshold = sperryliteSpeckleThreshold;
        this.contact = contact;
        this.embayments = embayments;
        this.orePods = orePods;
        this.contactVeinlets = contactVeinlets;
        this.footwallVeins = footwallVeins;
        this.offsetDikes = offsetDikes;
    }

    /// <summary>
    /// Builds the deposit geometry in deterministic parameter order.
    /// </summary>
    public static SudburyContactNickelPlan Create(
        in ProceduralDepositInstance instance,
        SudburyContactNickelDefinition settings)
    {
        var random = new ProceduralDepositRandom(instance.FeatureId ^ PlanSalt);

        double regionalDipDeg = random.Range(settings.RegionalDipDegMin, settings.RegionalDipDegMax);
        double dipDirectionDeg = random.Range(0.0, 360.0);
        double systemX = random.Range(-settings.SystemOffsetSpread, settings.SystemOffsetSpread);
        double systemZ = random.Range(-settings.SystemOffsetSpread, settings.SystemOffsetSpread);
        int embayCount = random.NextInt(settings.EmbayCountMin, settings.EmbayCountMax);
        double noriteThickness = random.Range(settings.NoriteThicknessMin, settings.NoriteThicknessMax);
        double seed = random.Range(0.0, 100.0);

        var embayments = new Embayment[embayCount];
        double angleStep = Math.PI * 2.0 / embayCount;
        for (int i = 0; i < embayCount; i++)
        {
            double angle = i * angleStep
                + random.Range(-settings.EmbayAngleJitter, settings.EmbayAngleJitter);
            double distance = random.Range(settings.EmbayDistanceMin, settings.EmbayDistanceMax);
            embayments[i] = new Embayment(
                systemX + Math.Cos(angle) * distance,
                systemZ + Math.Sin(angle) * distance,
                random.Range(settings.EmbayRadiusXMin, settings.EmbayRadiusXMax),
                random.Range(settings.EmbayRadiusZMin, settings.EmbayRadiusZMax),
                random.Range(settings.EmbayDepthMin, settings.EmbayDepthMax),
                seed + i * 17.0);
        }

        // The contact surface must already know its embayments: pods hang below the local contact.
        var contact = new ContactSurface(
            regionalDipDeg,
            dipDirectionDeg,
            systemX,
            systemZ,
            seed,
            embayments);

        var orePods = new OrePod[embayments.Length];
        for (int i = 0; i < embayments.Length; i++)
        {
            Embayment embay = embayments[i];
            double localContactY = contact.GetContactY(embay.CenterX, embay.CenterZ);
            orePods[i] = new OrePod(
                embay.CenterX + random.Range(-settings.PodCenterJitter, settings.PodCenterJitter),
                embay.CenterZ + random.Range(-settings.PodCenterJitter, settings.PodCenterJitter),
                localContactY - embay.Depth
                    * random.Range(settings.PodDepthFractionMin, settings.PodDepthFractionMax),
                embay.RadiusX * random.Range(settings.PodRadiusFactorMin, settings.PodRadiusFactorMax),
                embay.RadiusZ * random.Range(settings.PodRadiusFactorMin, settings.PodRadiusFactorMax),
                random.Range(settings.PodRadiusVerticalMin, settings.PodRadiusVerticalMax),
                seed + i * 23.0);
        }

        var veinlets = new List<Sheet>();
        for (int i = 0; i < orePods.Length; i++)
        {
            OrePod pod = orePods[i];
            int veinletCount = random.NextInt(
                settings.ContactVeinletCountMin,
                settings.ContactVeinletCountMax);
            for (int j = 0; j < veinletCount; j++)
            {
                double angle = random.Range(0.0, Math.PI * 2.0);
                double radius = random.Range(2.0, 5.0);
                double veinletX = pod.CenterX + Math.Cos(angle) * radius;
                double veinletZ = pod.CenterZ + Math.Sin(angle) * radius;
                double localContactY = contact.GetContactY(veinletX, veinletZ);
                double veinHeight = random.Range(2.0, 4.0);
                veinlets.Add(CreateSheet(
                    veinletX,
                    veinletZ,
                    localContactY - random.Range(0.2, 1.0),
                    dipDirectionDeg + 90.0 + random.Range(-30.0, 30.0),
                    random.Range(25.0, 55.0),
                    random.Range(6.0, 11.0),
                    veinHeight,
                    random.Range(0.7, 1.3),
                    seed + 53.0 + i * 19.0 + j * 7.0,
                    random.Range(-4.0, 4.0),
                    random.Range(2.0, 4.0),
                    random.Range(-0.7, 0.7),
                    seed + 17.0 + i * 5.0 + j,
                    0.8,
                    0.3));
            }
        }

        int veinCount = random.NextInt(settings.FootwallVeinCountMin, settings.FootwallVeinCountMax);
        var footwallVeins = new Sheet[veinCount];
        for (int i = 0; i < veinCount; i++)
        {
            OrePod pod = orePods[i % orePods.Length];
            double heightHalf = random.Range(10.0, 17.0);
            footwallVeins[i] = CreateSheet(
                pod.CenterX + random.Range(-2.5, 2.5),
                pod.CenterZ + random.Range(-2.5, 2.5),
                pod.CenterY - heightHalf * random.Range(0.55, 0.80),
                random.Range(0.0, 180.0),
                random.Range(62.0, 85.0),
                random.Range(9.0, 15.0),
                heightHalf,
                random.Range(1.4, 2.6),
                seed + 91.0 + i * 13.0,
                random.Range(-5.0, 5.0),
                random.Range(2.0, 4.0),
                random.Range(-1.0, 1.0),
                seed + 31.0 + i * 6.0,
                1.6,
                0.5);
        }

        int dikeCount = random.NextInt(settings.OffsetDikeCountMin, settings.OffsetDikeCountMax);
        var offsetDikes = new Sheet[dikeCount];
        for (int i = 0; i < dikeCount; i++)
        {
            OrePod pod = orePods[random.NextInt(0, orePods.Length - 1)];
            double azimuthDeg = random.Range(0.0, 360.0);
            double azimuthRad = azimuthDeg * Math.PI / 180.0;
            double extendDistance = random.Range(
                settings.OffsetDikeDistanceMin,
                settings.OffsetDikeDistanceMax);
            double farX = pod.CenterX + Math.Cos(azimuthRad) * extendDistance;
            double farZ = pod.CenterZ + Math.Sin(azimuthRad) * extendDistance;
            offsetDikes[i] = CreateSheet(
                (pod.CenterX + farX) * 0.5,
                (pod.CenterZ + farZ) * 0.5,
                pod.CenterY - random.Range(4.0, 10.0),
                azimuthDeg,
                random.Range(64.0, 84.0),
                extendDistance * 0.55,
                random.Range(11.0, 16.0),
                random.Range(3.0, 4.8),
                seed + 151.0 + i * 19.0,
                random.Range(-8.0, 8.0),
                random.Range(3.0, 6.0),
                random.Range(-1.6, 1.6),
                seed + 61.0 + i * 8.0,
                2.4,
                0.8);
        }

        return new SudburyContactNickelPlan(
            instance,
            settings.DepositScale,
            regionalDipDeg,
            dipDirectionDeg,
            systemX,
            systemZ,
            noriteThickness,
            seed,
            settings.SperryliteSpeckleThreshold,
            contact,
            embayments,
            orePods,
            veinlets.ToArray(),
            footwallVeins,
            offsetDikes);
    }

    /// <summary><c>createSheet</c>: precomputed strike/dip frame for a planar body.</summary>
    private static Sheet CreateSheet(
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
        double bendAmp1,
        double bendAmp2)
    {
        double strikeRad = strikeDeg * Math.PI / 180.0;
        double dipRad = dipDeg * Math.PI / 180.0;
        double sinStrike = Math.Sin(strikeRad);
        double cosStrike = Math.Cos(strikeRad);
        return new Sheet(
            sinStrike,
            cosStrike,
            Math.Sin(dipRad),
            Math.Cos(dipRad),
            centerX * sinStrike + centerZ * cosStrike,
            centerX * cosStrike - centerZ * sinStrike,
            centerY,
            lengthHalf,
            heightHalf,
            thickness,
            phase,
            relayCenter,
            relayWidth,
            relayShift,
            pinchPhase,
            bendAmp1,
            bendAmp2);
    }

    /// <summary>Model <c>getContactY</c> in model coordinates.</summary>
    public double GetContactY(double modelX, double modelZ) => contact.GetContactY(modelX, modelZ);

    public SudburyContactNickelSample Evaluate(
        int worldX,
        int worldY,
        int worldZ,
        int surfaceY = int.MaxValue)
    {
        if (worldY > surfaceY) return default;

    // The model divides world voxel coordinates by DEPOSIT_SCALE before classification.
        double modelX = (worldX - originX) / depositScale;
        double modelY = (worldY - originY) / depositScale;
        double modelZ = (worldZ - originZ) / depositScale;
        if (modelY < FloorY) return default;

        double contactY = GetContactY(modelX, modelZ);
        bool aboveContact = modelY >= contactY;

        PodSample? bestPod = null;
        PodSample? bestPodHalo = null;
        for (int i = 0; i < orePods.Length; i++)
        {
            PodSample sample = SamplePod(modelX, modelY, modelZ, orePods[i]);
            if (sample.Halo && (!bestPodHalo.HasValue || sample.Radial < bestPodHalo.Value.Radial))
            {
                bestPodHalo = sample;
            }
            if (sample.Inside && (!bestPod.HasValue || sample.Radial < bestPod.Value.Radial))
            {
                bestPod = sample;
            }
        }

        if (bestPod.HasValue)
        {
            return Finish(ContactOreMaterial(bestPod.Value), modelX, modelY, modelZ, true, false, false, false);
        }

        SheetSample? bestContactVein = null;
        for (int i = 0; i < contactVeinlets.Length; i++)
        {
            SheetSample sample = SampleSheet(modelX, modelY, modelZ, contactVeinlets[i]);
            if (double.IsFinite(sample.Score)
                && (!bestContactVein.HasValue || sample.Score < bestContactVein.Value.Score))
            {
                bestContactVein = sample;
            }
        }
        if (bestContactVein.HasValue && bestContactVein.Value.Inside)
        {
            return Finish(
                ContactVeinMaterial(bestContactVein.Value),
                modelX,
                modelY,
                modelZ,
                true,
                false,
                false,
                false);
        }

        SheetSample? bestVein = null;
        for (int i = 0; i < footwallVeins.Length; i++)
        {
            SheetSample sample = SampleSheet(modelX, modelY, modelZ, footwallVeins[i]);
            if (double.IsFinite(sample.Score)
                && (!bestVein.HasValue || sample.Score < bestVein.Value.Score))
            {
                bestVein = sample;
            }
        }
        if (bestVein.HasValue && bestVein.Value.Inside)
        {
            SudburyContactNickelZone zone = FootwallVeinMaterial(bestVein.Value);
            // Central chalcopyrite domains carry the strongest modelled PGM enrichment.
            bool pgm = zone == SudburyContactNickelZone.Chalcopyrite;
            return Finish(zone, modelX, modelY, modelZ, false, true, false, pgm);
        }

        SheetSample? bestDike = null;
        for (int i = 0; i < offsetDikes.Length; i++)
        {
            SheetSample sample = SampleSheet(modelX, modelY, modelZ, offsetDikes[i]);
            if (sample.Inside && (!bestDike.HasValue || sample.Score < bestDike.Value.Score))
            {
                bestDike = sample;
            }
        }
        if (bestDike.HasValue)
        {
            SudburyContactNickelZone zone = OffsetDikeMaterial(bestDike.Value);
            // Sulfide-lens centres inside a dike are locally PGM-enriched.
            bool pgm = zone == SudburyContactNickelZone.Pentlandite
                || zone == SudburyContactNickelZone.Chalcopyrite;
            return Finish(zone, modelX, modelY, modelZ, false, false, true, pgm);
        }

        // A thin disseminated band sits immediately above the contact inside the ore network.
        bool inContactNetwork = (bestContactVein.HasValue && bestContactVein.Value.Halo)
            || (bestPodHalo.HasValue && bestPodHalo.Value.Halo);
        if (aboveContact && inContactNetwork && modelY - contactY < 1.0)
        {
            double disseminatedBand = Math.Sin(modelX * 0.42 + seed) + Math.Cos(modelZ * 0.37 - seed);
            if (disseminatedBand > 1.25)
            {
                return Finish(
                    SudburyContactNickelZone.Pentlandite,
                    modelX,
                    modelY,
                    modelZ,
                    true,
                    false,
                    false,
                    false);
            }
            if (disseminatedBand < -1.65)
            {
                return Finish(
                    SudburyContactNickelZone.Pyrrhotite,
                    modelX,
                    modelY,
                    modelZ,
                    true,
                    false,
                    false,
                    false);
            }
        }

        return default;
    }

    /// <summary>
    /// <c>contactOreMaterial</c>: basal pyrrhotite-chalcopyrite with magnetite grading
    /// upward and inward into pentlandite-rich sulfide.
    /// </summary>
    private SudburyContactNickelZone ContactOreMaterial(in PodSample sample)
    {
        bool basalBand = sample.VerticalNorm < -0.34;
        bool axialBand = sample.Lateral < 0.62;
        double phaseBand = Math.Sin(sample.AlongNorm * 5.2 + seed) + Math.Cos(sample.AcrossNorm * 4.1);

        if (basalBand && sample.Lateral < 0.34 && phaseBand > 0.85)
        {
            return SudburyContactNickelZone.Magnetite;
        }
        if (basalBand && !axialBand && phaseBand < 0.05)
        {
            return SudburyContactNickelZone.Chalcopyrite;
        }
        if ((axialBand || sample.VerticalNorm > -0.08) && phaseBand > -0.72)
        {
            return SudburyContactNickelZone.Pentlandite;
        }
        return SudburyContactNickelZone.Pyrrhotite;
    }

    /// <summary>Model <c>contactVeinMaterial</c>.</summary>
    private static SudburyContactNickelZone ContactVeinMaterial(in SheetSample sample)
    {
        double band = Math.Sin(sample.AlongNorm * 6.0 + sample.Phase)
            + 0.35 * Math.Cos(sample.VerticalNorm * 5.0);
        if (sample.CenterRatio < 0.58)
        {
            return band > -0.62
                ? SudburyContactNickelZone.Pentlandite
                : SudburyContactNickelZone.Pyrrhotite;
        }
        if (sample.CenterRatio < 0.82) return SudburyContactNickelZone.Pyrrhotite;
        return band > 0.55
            ? SudburyContactNickelZone.Chalcopyrite
            : SudburyContactNickelZone.Magnetite;
    }

    /// <summary>Model <c>footwallVeinMaterial</c>.</summary>
    private static SudburyContactNickelZone FootwallVeinMaterial(in SheetSample sample)
    {
        if (sample.CenterRatio < 0.68) return SudburyContactNickelZone.Chalcopyrite;
        if (sample.VerticalNorm > 0.18 && sample.CenterRatio < 0.86)
        {
            return SudburyContactNickelZone.Pentlandite;
        }
        if (sample.CenterRatio < 0.88) return SudburyContactNickelZone.Pyrrhotite;
        return SudburyContactNickelZone.HydrothermalGangue;
    }

    /// <summary>Model <c>offsetDikeMaterial</c>.</summary>
    private static SudburyContactNickelZone OffsetDikeMaterial(in SheetSample sample)
    {
        double lens = Math.Cos((sample.AlongNorm - 0.08) * 8.0 + sample.Phase)
            + 0.45 * Math.Cos(sample.VerticalNorm * 7.0 - sample.Phase * 0.3);
        double inclusion = Math.Sin(sample.AlongNorm * 11.0 + sample.Phase * 0.6)
            * Math.Cos(sample.VerticalNorm * 9.0 - sample.Phase);

        if (sample.CenterRatio < 0.70 && lens > 0.58)
        {
            return sample.AlongNorm < 0.18
                ? SudburyContactNickelZone.Pentlandite
                : SudburyContactNickelZone.Chalcopyrite;
        }
        if (inclusion > 0.82 && sample.CenterRatio < 0.76)
        {
            return SudburyContactNickelZone.InclusionXenolith;
        }
        return SudburyContactNickelZone.QuartzDiorite;
    }

    private SudburyContactNickelSample Finish(
        SudburyContactNickelZone zone,
        double x,
        double y,
        double z,
        bool inContactOre,
        bool inFootwallVein,
        bool inOffsetDike,
        bool pgmEnriched)
    {
        // Sperrylite is the platinum arsenide of the Cu-rich PGM domains. It replaces the host
        // sulfide only where the enrichment flag is already set and the speckle gate passes, so
        // it stays a rare accessory rather than a body of its own.
        double speckleNoise = 0.0;
        if (pgmEnriched)
        {
            speckleNoise = Hash3D(x * 3.3 + seed * 1.7, y * 3.3, z * 3.3);
            if (speckleNoise >= sperryliteSpeckleThreshold)
            {
                zone = SudburyContactNickelZone.Sperrylite;
            }
        }

        int grade = 0;
        if (zone == SudburyContactNickelZone.Sperrylite)
        {
            // Platinum tenor stays low: the top tenth of qualifying speckles reaches medium.
            double span = 1.0 - sperryliteSpeckleThreshold;
            double within = span > 0.0 ? (speckleNoise - sperryliteSpeckleThreshold) / span : 0.0;
            grade = within > 0.9 ? 1 : 0;
        }
        else if (IsGraded(zone))
        {
            double grainNoise = Hash3D(x * 2.7 + seed * 1.3, y * 2.7, z * 2.7);
            grade = grainNoise > 0.82 ? 3 : (grainNoise > 0.55 ? 2 : (grainNoise > 0.25 ? 1 : 0));
        }

        return new SudburyContactNickelSample(
            zone,
            grade,
            inContactOre,
            inFootwallVein,
            inOffsetDike,
            pgmEnriched,
            speckleNoise);
    }

    internal static bool IsGraded(SudburyContactNickelZone zone)
    {
        return zone == SudburyContactNickelZone.Pentlandite
            || zone == SudburyContactNickelZone.Chalcopyrite
            || zone == SudburyContactNickelZone.Magnetite
            || zone == SudburyContactNickelZone.Sperrylite;
    }

    /// <summary><c>getPodSample</c>: edge-warped triaxial massive sulfide pod.</summary>
    private static PodSample SamplePod(double x, double y, double z, in OrePod pod)
    {
        double u = (x - pod.CenterX) / pod.RadiusAlong;
        double v = (z - pod.CenterZ) / pod.RadiusAcross;
        double w = (y - pod.CenterY) / pod.RadiusVertical;
        double edgeWarp = Math.Sin(u * 3.6 + pod.Phase) * Math.Cos(v * 3.1) * 0.14
            + Math.Sin(u * 7.0 - v * 4.4 + pod.Phase * 0.5) * 0.06;
        double radial = Math.Sqrt(u * u + v * v + w * w) + edgeWarp;
        return new PodSample(
            radial,
            Math.Sqrt(u * u + v * v),
            w,
            u,
            v,
            radial <= 1.0,
            radial <= 1.22);
    }

    /// <summary>
    /// <c>getSheetSample</c>: bent, relayed planar body with an edge-warped elliptical
    /// footprint and pinch-and-swell aperture.
    /// </summary>
    private static SheetSample SampleSheet(double x, double y, double z, in Sheet sheet)
    {
        double along = x * sheet.CosStrike - z * sheet.SinStrike;
        double across = x * sheet.SinStrike + z * sheet.CosStrike - sheet.Offset;
        double bend = Math.Sin(along * 0.12 + y * 0.07 + sheet.Phase) * sheet.BendAmp1
            + Math.Cos(along * 0.30 + sheet.Phase * 1.3) * sheet.BendAmp2;
        double relay = sheet.RelayShift * Math.Tanh((along - sheet.RelayCenter) / sheet.RelayWidth);
        double signedDistance = across * sheet.SinDip + (y - sheet.CenterY) * sheet.CosDip - bend - relay;
        double u = (along - sheet.CenterAlong) / sheet.LengthHalf;
        double v = (y - sheet.CenterY) / sheet.HeightHalf;
        double edgeWarp = Math.Sin(u * 3.7 + sheet.Phase) * Math.Cos(v * 3.0) * 0.15;
        double footprint = Math.Sqrt(u * u + v * v) + edgeWarp;
        double taper = Clamp(1.0 - footprint * footprint, 0.0, 1.0);
        double pinchSwell = 0.80 + 0.20 * Math.Sin(along * 0.18 + sheet.PinchPhase);
        double halfThickness = Math.Max(0.10, sheet.Thickness * 0.5 * pinchSwell * Math.Sqrt(taper));
        double distance = Math.Abs(signedDistance);

        return new SheetSample(
            footprint <= 1.18 ? distance / halfThickness : double.PositiveInfinity,
            footprint <= 1.03 && distance <= halfThickness,
            footprint <= 1.13 && distance <= halfThickness + 2.4 * taper,
            distance / Math.Max(0.1, halfThickness),
            u,
            v,
            sheet.Phase);
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

    /// <summary>
    /// Model <c>getContactY</c> plus <c>getEmbaySample</c>: a dipping, warped basal contact
    /// pulled down by each embayment trough.
    /// </summary>
    private readonly struct ContactSurface
    {
        private readonly double tanDip;
        private readonly double cosDirection;
        private readonly double sinDirection;
        private readonly double systemX;
        private readonly double systemZ;
        private readonly double seed;
        private readonly Embayment[] embayments;

        public ContactSurface(
            double regionalDipDeg,
            double dipDirectionDeg,
            double systemX,
            double systemZ,
            double seed,
            Embayment[] embayments)
        {
            tanDip = Math.Tan(regionalDipDeg * Math.PI / 180.0);
            double directionRad = dipDirectionDeg * Math.PI / 180.0;
            cosDirection = Math.Cos(directionRad);
            sinDirection = Math.Sin(directionRad);
            this.systemX = systemX;
            this.systemZ = systemZ;
            this.seed = seed;
            this.embayments = embayments;
        }

        public double GetContactY(double x, double z)
        {
            double projection = (x - systemX) * cosDirection + (z - systemZ) * sinDirection;
            double baseY = 2.0 + tanDip * projection;
            double warp = Math.Sin(x * 0.05 + seed) * 1.4 + Math.Cos(z * 0.06 - seed * 0.7) * 1.1;

            double embayDip = 0.0;
            for (int i = 0; i < embayments.Length; i++)
            {
                Embayment embay = embayments[i];
                double u = (x - embay.CenterX) / embay.RadiusX;
                double v = (z - embay.CenterZ) / embay.RadiusZ;
                double edgeWarp = Math.Sin(u * 3.2 + embay.Phase) * Math.Cos(v * 2.8) * 0.16
                    + Math.Sin(u * 6.5 - v * 4.0 + embay.Phase * 0.6) * 0.08;
                double footprint = Math.Sqrt(u * u + v * v) + edgeWarp;
                if (footprint < 1.15)
                {
                    double falloff = Clamp(1.0 - footprint * footprint, 0.0, 1.0);
                    embayDip = Math.Max(embayDip, embay.Depth * falloff * falloff);
                }
            }

            return baseY + warp - embayDip;
        }
    }

    private readonly record struct PodSample(
        double Radial,
        double Lateral,
        double VerticalNorm,
        double AlongNorm,
        double AcrossNorm,
        bool Inside,
        bool Halo);

    private readonly record struct SheetSample(
        double Score,
        bool Inside,
        bool Halo,
        double CenterRatio,
        double AlongNorm,
        double VerticalNorm,
        double Phase);

    private readonly record struct Embayment(
        double CenterX,
        double CenterZ,
        double RadiusX,
        double RadiusZ,
        double Depth,
        double Phase);

    private readonly record struct OrePod(
        double CenterX,
        double CenterZ,
        double CenterY,
        double RadiusAlong,
        double RadiusAcross,
        double RadiusVertical,
        double Phase);

    private readonly record struct Sheet(
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
        double BendAmp1,
        double BendAmp2);
}

internal sealed class SudburyContactNickelProceduralTemplate : IProceduralDepositTemplate
{
    private static readonly string[] RequiredSlots =
    {
        ProceduralMaterialSlots.Pentlandite,
        ProceduralMaterialSlots.Chalcopyrite,
        ProceduralMaterialSlots.Pyrite,
        ProceduralMaterialSlots.Magnetite,
        ProceduralMaterialSlots.Quartz,
        ProceduralMaterialSlots.Sperrylite
    };

    public string Code => "sudburyContactNickel";
    public IReadOnlyList<string> RequiredMaterialSlots => RequiredSlots;
    public bool RequiresPlan => true;

    public int GetMaximumHorizontalReach(ProceduralDepositDefinition definition)
    {
        return definition.SudburyNickel.HorizontalRadius;
    }

    public object CreatePlan(in ProceduralDepositInstance instance, ProceduralDepositDefinition definition)
    {
        return SudburyContactNickelPlan.Create(instance, definition.SudburyNickel);
    }

    public bool Validate(ProceduralDepositDefinition definition, out string error)
    {
        SudburyContactNickelDefinition settings = definition.SudburyNickel;
        bool valid = settings.HorizontalRadius >= 16
            && settings.VerticalHalfHeight >= 8
            && settings.DepositScale > 0.0
            && settings.RegionalDipDegMin >= 0.0
            && settings.RegionalDipDegMax >= settings.RegionalDipDegMin
            && settings.RegionalDipDegMax < 90.0
            && settings.SystemOffsetSpread >= 0.0
            && settings.EmbayCountMin >= 1
            && settings.EmbayCountMax >= settings.EmbayCountMin
            && settings.NoriteThicknessMin > 0.0
            && settings.NoriteThicknessMax >= settings.NoriteThicknessMin
            && settings.EmbayAngleJitter >= 0.0
            && settings.EmbayDistanceMin > 0.0
            && settings.EmbayDistanceMax >= settings.EmbayDistanceMin
            && settings.EmbayRadiusXMin > 0.0
            && settings.EmbayRadiusXMax >= settings.EmbayRadiusXMin
            && settings.EmbayRadiusZMin > 0.0
            && settings.EmbayRadiusZMax >= settings.EmbayRadiusZMin
            && settings.EmbayDepthMin > 0.0
            && settings.EmbayDepthMax >= settings.EmbayDepthMin
            && settings.PodCenterJitter >= 0.0
            && settings.PodDepthFractionMin > 0.0
            && settings.PodDepthFractionMax >= settings.PodDepthFractionMin
            && settings.PodRadiusFactorMin > 0.0
            && settings.PodRadiusFactorMax >= settings.PodRadiusFactorMin
            && settings.PodRadiusVerticalMin > 0.0
            && settings.PodRadiusVerticalMax >= settings.PodRadiusVerticalMin
            && settings.ContactVeinletCountMin >= 1
            && settings.ContactVeinletCountMax >= settings.ContactVeinletCountMin
            && settings.FootwallVeinCountMin >= 1
            && settings.FootwallVeinCountMax >= settings.FootwallVeinCountMin
            && settings.OffsetDikeCountMin >= 1
            && settings.OffsetDikeCountMax >= settings.OffsetDikeCountMin
            && settings.OffsetDikeDistanceMin > 0.0
            && settings.OffsetDikeDistanceMax >= settings.OffsetDikeDistanceMin
            && settings.SperryliteSpeckleThreshold is >= 0.0 and <= 1.0;
        error = valid ? string.Empty : "invalid sudbury contact nickel settings";
        return valid;
    }

    public void Realize(
        ProceduralDepositWorldGenSystem system,
        in DepositCandidate candidate,
        IChunkColumnGenerateRequest request,
        int baseX,
        int baseZ)
    {
        system.RealizeSudburyContactNickelCandidate(candidate, request, baseX, baseZ);
    }
}

public sealed partial class ProceduralDepositWorldGenSystem
{
    internal void RealizeSudburyContactNickelCandidate(
        DepositCandidate candidate,
        IChunkColumnGenerateRequest request,
        int baseX,
        int baseZ)
    {
        CompiledProceduralDeposit compiled = candidate.Compiled;
        SudburyContactNickelDefinition settings = compiled.Definition.SudburyNickel;
        var plan = (SudburyContactNickelPlan)candidate.Plan!;
        ushort[] heightMap = request.Chunks[0].MapChunk.WorldGenTerrainHeightMap;
        int worldHeight = serverApi!.World.BlockAccessor.MapSizeY;
        int minimumY = GameMath.Clamp(candidate.Instance.CenterY - settings.VerticalHalfHeight, 1, worldHeight - 2);
        int maximumPrototypeY = GameMath.Clamp(candidate.Instance.CenterY + settings.VerticalHalfHeight, 1, worldHeight - 2);

        int[] zoneSlots = BuildSudburyContactNickelZoneSlots(compiled);

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
                    SudburyContactNickelSample sample = plan.Evaluate(worldX, y, worldZ, surfaceY);
                    if (sample.Zone == SudburyContactNickelZone.None) continue;

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
                    data.SetFluid(index3d, 0);
                }
            }
        }
    }

    private static int[] BuildSudburyContactNickelZoneSlots(CompiledProceduralDeposit compiled)
    {
        var slots = new int[Enum.GetValues<SudburyContactNickelZone>().Length];
        Array.Fill(slots, -1);

        slots[(int)SudburyContactNickelZone.Pentlandite] = compiled.GetSlotId(ProceduralMaterialSlots.Pentlandite);
        slots[(int)SudburyContactNickelZone.Chalcopyrite] = compiled.GetSlotId(ProceduralMaterialSlots.Chalcopyrite);
        slots[(int)SudburyContactNickelZone.Magnetite] = compiled.GetSlotId(ProceduralMaterialSlots.Magnetite);
        slots[(int)SudburyContactNickelZone.Sperrylite] = compiled.GetSlotId(ProceduralMaterialSlots.Sperrylite);

        // Per the plan pyrrhotite maps to pyrite: it has no ore of its own in this mod.
        slots[(int)SudburyContactNickelZone.Pyrrhotite] = compiled.GetSlotId(ProceduralMaterialSlots.Pyrite);

        // Late hydrothermal gangue in footwall vein margins is quartz.
        slots[(int)SudburyContactNickelZone.HydrothermalGangue] = compiled.GetSlotId(ProceduralMaterialSlots.Quartz);

        slots[(int)SudburyContactNickelZone.QuartzDiorite] = compiled.GetSlotId(ProceduralMaterialSlots.Intermediate);

        // Inclusion xenoliths are fragments of disrupted country rock caught in the dike.
        slots[(int)SudburyContactNickelZone.InclusionXenolith] = compiled.GetSlotId(ProceduralMaterialSlots.Breccia);

        return slots;
    }
}
