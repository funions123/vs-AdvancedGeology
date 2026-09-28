using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using Vintagestory.API.Server;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace AdvancedGeology.WorldGen.ProceduralDeposits;

public enum CyclothemCoalZone
{
    None = 0,
    Bituminous,
    HighAshCoal,
    PyriticCoal,
    ShaleParting,
    OrganicClaystone,
    Fireclay,
    ChannelSandstone
}

[JsonObject(MemberSerialization.OptIn)]
public sealed class CyclothemCoalDefinition
{
    // Model extent is 72x60 (x,z in [-36,36], y in [-30,30]). The reach must contain the
    // largest seam footprint (radiusAlong up to 25 * sqrt(3/2)) plus the strike rotation.
    [JsonProperty]
    public int HorizontalRadius { get; set; } = 44;

    [JsonProperty]
    public int VerticalHalfHeight { get; set; } = 30;

    [JsonProperty]
    public double StrikeDegMin { get; set; } = 0.0;

    [JsonProperty]
    public double StrikeDegMax { get; set; } = 180.0;

    [JsonProperty]
    public double BedDipDegMin { get; set; } = 1.5;

    [JsonProperty]
    public double BedDipDegMax { get; set; } = 7.0;

    [JsonProperty]
    public int SeamCountMin { get; set; } = 2;

    [JsonProperty]
    public int SeamCountMax { get; set; } = 4;

    [JsonProperty]
    public double FirstSeamLevelMin { get; set; } = -17.0;

    [JsonProperty]
    public double FirstSeamLevelMax { get; set; } = -13.0;

    [JsonProperty]
    public double SeamLevelStepMin { get; set; } = 5.5;

    [JsonProperty]
    public double SeamLevelStepMax { get; set; } = 8.0;

    [JsonProperty]
    public double SeamHalfThicknessMin { get; set; } = 1.2;

    [JsonProperty]
    public double SeamHalfThicknessMax { get; set; } = 2.0;

    [JsonProperty]
    public double SeamCenterAlongSpread { get; set; } = 8.0;

    [JsonProperty]
    public double SeamCenterAcrossSpread { get; set; } = 10.0;

    [JsonProperty]
    public double SeamRadiusAlongMin { get; set; } = 20.0;

    [JsonProperty]
    public double SeamRadiusAlongMax { get; set; } = 25.0;

    [JsonProperty]
    public double SeamRadiusAcrossMin { get; set; } = 13.0;

    [JsonProperty]
    public double SeamRadiusAcrossMax { get; set; } = 17.0;

    [JsonProperty]
    public double PartingProbability { get; set; } = 0.62;

    [JsonProperty]
    public double RiderProbability { get; set; } = 0.42;

    [JsonProperty]
    public double RiderLevelOffsetMin { get; set; } = 2.5;

    [JsonProperty]
    public double RiderLevelOffsetMax { get; set; } = 4.0;

    [JsonProperty]
    public double RiderHalfThicknessMin { get; set; } = 0.55;

    [JsonProperty]
    public double RiderHalfThicknessMax { get; set; } = 0.9;

    [JsonProperty]
    public double RiderAlongJitter { get; set; } = 3.0;

    [JsonProperty]
    public double RiderAcrossJitter { get; set; } = 2.0;

    [JsonProperty]
    public double RiderRadiusScaleMin { get; set; } = 0.65;

    [JsonProperty]
    public double RiderRadiusScaleMax { get; set; } = 0.85;

    [JsonProperty]
    public int ChannelCountMin { get; set; } = 1;

    [JsonProperty]
    public int ChannelCountMax { get; set; } = 3;

    [JsonProperty]
    public double ChannelHalfLengthMin { get; set; } = 7.0;

    [JsonProperty]
    public double ChannelHalfLengthMax { get; set; } = 12.0;

    [JsonProperty]
    public double ChannelAlongJitter { get; set; } = 3.0;

    [JsonProperty]
    public double ChannelStratYOffsetMin { get; set; } = 0.6;

    [JsonProperty]
    public double ChannelStratYOffsetMax { get; set; } = 1.6;

    [JsonProperty]
    public double ChannelWidthMin { get; set; } = 3.5;

    [JsonProperty]
    public double ChannelWidthMax { get; set; } = 6.0;

    [JsonProperty]
    public double ChannelHalfThicknessMin { get; set; } = 2.2;

    [JsonProperty]
    public double ChannelHalfThicknessMax { get; set; } = 3.6;

    [JsonProperty]
    public double ChannelBendMin { get; set; } = -2.5;

    [JsonProperty]
    public double ChannelBendMax { get; set; } = 2.5;
}

/// <summary>
/// Result of classifying one voxel against a <see cref="CyclothemCoalPlan"/>.
/// </summary>
public readonly record struct CyclothemCoalSample(
    CyclothemCoalZone Zone,
    int SeamIndex = -1,
    double Ratio = 0.0,
    bool Split = false,
    bool InsideChannel = false,
    bool InsideSeam = false,
    bool HighAshPruned = false);

/// <summary>
/// Bituminous, high-ash, and pyritic coal with shale partings, seat-earth, fireclay, and channel sandstone form stacked seams and riders locally split or cut by paleochannels.
/// </summary>
internal sealed class CyclothemCoalPlan
{
    private const ulong PlanSalt = 0x4359434C4F544D32UL; // "CYCLOTM2"

    // Local model floor: y < -30 is below the modelled coal measure package (GRID_Y / 2).
    private const double BottomY = -30.0;

    // sampleSeams() shape constants.
    private const double SeamFootprintLimit = 1.04;
    private const double SeamEdgeScaleWidth = 0.18;
    private const double SeamMinimumThickness = 0.55;

    // Seat-earth and fireclay geometry.
    private const double SeatTopMargin = 0.2;
    private const double SeatThickness = 1.05;
    private const double SeatScale = 0.72;
    private const double FireclayOffset = 1.2;
    private const double FireclayThickness = 0.8;
    private const double FireclayScale = 0.58;
    private const double FireclayPatchThreshold = 0.55;

    // Channel-margin seam splitting.
    private const double SplitMarginReach = 4.5;
    private const double SplitAmplitude = 1.5;
    private const double SplitActivation = 0.05;

    private const double HighAshHostRetention = 0.25;

    private readonly ulong featureId;
    private readonly int originX;
    private readonly int originY;
    private readonly int originZ;

    private readonly double strikeDeg;
    private readonly double bedDipDeg;
    private readonly double seed;

    private readonly double cosStrike;
    private readonly double sinStrike;
    private readonly double tanBedDip;

    private readonly CoalSeam[] seams;
    private readonly PaleoChannel[] channels;

    public int OriginX => originX;
    public int OriginY => originY;
    public int OriginZ => originZ;
    public double StrikeDeg => strikeDeg;
    public double BedDipDeg => bedDipDeg;
    public double Seed => seed;
    public int SeamCount => seams.Length;
    public int PrincipalSeamCount
    {
        get
        {
            int count = 0;
            for (int i = 0; i < seams.Length; i++)
            {
                if (!seams[i].IsRider) count++;
            }

            return count;
        }
    }

    public int RiderSeamCount => seams.Length - PrincipalSeamCount;
    public int ChannelCount => channels.Length;

    private CyclothemCoalPlan(
        ulong featureId,
        in ProceduralDepositInstance instance,
        double strikeDeg,
        double bedDipDeg,
        double seed,
        double cosStrike,
        double sinStrike,
        double tanBedDip,
        CoalSeam[] seams,
        PaleoChannel[] channels)
    {
        this.featureId = featureId;
        originX = instance.CenterX;
        originY = instance.CenterY;
        originZ = instance.CenterZ;
        this.strikeDeg = strikeDeg;
        this.bedDipDeg = bedDipDeg;
        this.seed = seed;
        this.cosStrike = cosStrike;
        this.sinStrike = sinStrike;
        this.tanBedDip = tanBedDip;
        this.seams = seams;
        this.channels = channels;
    }

    /// <summary>
    /// Builds the deposit geometry in deterministic parameter order.
    /// </summary>
    public static CyclothemCoalPlan Create(
        in ProceduralDepositInstance instance,
        CyclothemCoalDefinition settings)
    {
        var random = new ProceduralDepositRandom(instance.FeatureId ^ PlanSalt);

        double strikeDeg = random.Range(settings.StrikeDegMin, settings.StrikeDegMax);
        double bedDipDeg = random.Range(settings.BedDipDegMin, settings.BedDipDegMax);
        double seed = random.Range(0.0, 100.0);

        int count = random.NextInt(settings.SeamCountMin, settings.SeamCountMax);
        double sizeScale = Math.Sqrt(3.0 / count);
        double level = random.Range(settings.FirstSeamLevelMin, settings.FirstSeamLevelMax);

        var built = new List<CoalSeam>(count * 2);
        for (int i = 0; i < count; i++)
        {
            double halfThickness = random.Range(settings.SeamHalfThicknessMin, settings.SeamHalfThicknessMax);
            double centerAlong = random.Range(-settings.SeamCenterAlongSpread, settings.SeamCenterAlongSpread);
            double centerAcross = random.Range(-settings.SeamCenterAcrossSpread, settings.SeamCenterAcrossSpread);
            double radiusAlong = random.Range(settings.SeamRadiusAlongMin, settings.SeamRadiusAlongMax) * sizeScale;
            double radiusAcross = random.Range(settings.SeamRadiusAcrossMin, settings.SeamRadiusAcrossMax) * sizeScale;
            double phase = seed + i * 19.0;
            bool hasParting = random.Range(0.0, 1.0) < settings.PartingProbability;

            built.Add(new CoalSeam(
                level,
                halfThickness,
                centerAlong,
                centerAcross,
                radiusAlong,
                radiusAcross,
                phase,
                hasParting,
                false));

            // A rider seam is a thinner leaf a few blocks above its principal seam.
            if (random.Range(0.0, 1.0) < settings.RiderProbability)
            {
                double riderLevel = level + random.Range(settings.RiderLevelOffsetMin, settings.RiderLevelOffsetMax);
                double riderHalfThickness = random.Range(
                    settings.RiderHalfThicknessMin,
                    settings.RiderHalfThicknessMax);
                double riderAlong = centerAlong + random.Range(-settings.RiderAlongJitter, settings.RiderAlongJitter);
                double riderAcross = centerAcross + random.Range(-settings.RiderAcrossJitter, settings.RiderAcrossJitter);
                double riderRadiusAlong = radiusAlong
                    * random.Range(settings.RiderRadiusScaleMin, settings.RiderRadiusScaleMax);
                double riderRadiusAcross = radiusAcross
                    * random.Range(settings.RiderRadiusScaleMin, settings.RiderRadiusScaleMax);

                built.Add(new CoalSeam(
                    riderLevel,
                    riderHalfThickness,
                    riderAlong,
                    riderAcross,
                    riderRadiusAlong,
                    riderRadiusAcross,
                    seed + i * 23.0 + 7.0,
                    false,
                    true));
            }

            level += random.Range(settings.SeamLevelStepMin, settings.SeamLevelStepMax);
        }

    // Seams are sorted by stratigraphic level before evaluation.
        CoalSeam[] seams = built.ToArray();
        Array.Sort(seams, static (left, right) => left.Level.CompareTo(right.Level));

        var principals = new List<int>(seams.Length);
        for (int i = 0; i < seams.Length; i++)
        {
            if (!seams[i].IsRider) principals.Add(i);
        }

        int channelCount = random.NextInt(settings.ChannelCountMin, settings.ChannelCountMax);
        var channels = new PaleoChannel[channelCount];
        for (int i = 0; i < channelCount; i++)
        {
            CoalSeam source = seams[principals[random.NextInt(0, principals.Count - 1)]];
            double halfLength = Math.Min(
                random.Range(settings.ChannelHalfLengthMin, settings.ChannelHalfLengthMax),
                source.RadiusAlong * 0.48);
            double centerAlong = source.CenterAlong
                + random.Range(-settings.ChannelAlongJitter, settings.ChannelAlongJitter);
            double acrossOffset = source.RadiusAcross * 0.25;

            double startAcross = source.CenterAcross + random.Range(-acrossOffset, acrossOffset);
            double endAcross = source.CenterAcross + random.Range(-acrossOffset, acrossOffset);
            double centerStratY = source.Level
                + random.Range(settings.ChannelStratYOffsetMin, settings.ChannelStratYOffsetMax);
            double width = random.Range(settings.ChannelWidthMin, settings.ChannelWidthMax);
            double channelHalfThickness = random.Range(
                settings.ChannelHalfThicknessMin,
                settings.ChannelHalfThicknessMax);
            double bend = random.Range(settings.ChannelBendMin, settings.ChannelBendMax);

            channels[i] = new PaleoChannel(
                centerAlong - halfLength,
                startAcross,
                centerAlong + halfLength,
                endAcross,
                centerStratY,
                width,
                channelHalfThickness,
                bend,
                seed + i * 31.0);
        }

        double strikeRad = strikeDeg * Math.PI / 180.0;
        return new CyclothemCoalPlan(
            instance.FeatureId,
            instance,
            strikeDeg,
            bedDipDeg,
            seed,
            Math.Cos(strikeRad),
            Math.Sin(strikeRad),
            Math.Tan(bedDipDeg * Math.PI / 180.0),
            seams,
            channels);
    }

    /// <summary><c>getLocal</c>: rotation into the strike-parallel frame.</summary>
    public StructuralCoordinates GetLocal(double x, double z)
    {
        return new StructuralCoordinates(
            x * cosStrike - z * sinStrike,
            x * sinStrike + z * cosStrike);
    }

    /// <summary><c>getStratY</c>: bedding dip plus a gentle along-strike warp.</summary>
    public double GetStratigraphicY(double x, double y, double z)
    {
        StructuralCoordinates local = GetLocal(x, z);
        return y - tanBedDip * local.Across - Math.Sin(local.Along * 0.055 + seed) * 1.1;
    }

    public CyclothemCoalSample Evaluate(int worldX, int worldY, int worldZ, int surfaceY = int.MaxValue)
    {
        if (worldY > surfaceY) return default;

        double x = worldX - originX;
        double y = worldY - originY;
        double z = worldZ - originZ;
        if (y < BottomY) return default;

        double stratY = GetStratigraphicY(x, y, z);

        // Paleochannel incision overrides everything below it.
        if (SampleChannel(x, z, stratY))
        {
            return new CyclothemCoalSample(
                CyclothemCoalZone.ChannelSandstone,
                -1,
                0.0,
                false,
                true,
                false,
                false);
        }

        SeamHit? hit = SampleSeams(x, z, stratY);
        if (hit.HasValue)
        {
            SeamHit seam = hit.Value;
            CoalSeam definition = seams[seam.SeamIndex];
            double facies = Math.Sin(x * 0.17 + z * 0.12 + definition.Phase)
                + 0.45 * Math.Cos(stratY * 0.8 - z * 0.07);

            if (definition.HasParting && seam.Ratio < 0.08 && facies < -0.15)
            {
                return new CyclothemCoalSample(
                    CyclothemCoalZone.ShaleParting,
                    seam.SeamIndex,
                    seam.Ratio,
                    seam.Split,
                    false,
                    true,
                    false);
            }

            if (seam.Ratio > 0.84 || facies < -1.15)
            {
                bool pruned = Hash3D(x * 2.1 + seed, y * 2.1, z * 2.1) < HighAshHostRetention;
                return new CyclothemCoalSample(
                    pruned ? CyclothemCoalZone.None : CyclothemCoalZone.HighAshCoal,
                    seam.SeamIndex,
                    seam.Ratio,
                    seam.Split,
                    false,
                    true,
                    pruned);
            }

            if (facies > 1.22 && seam.Ratio < 0.6)
            {
                return new CyclothemCoalSample(
                    CyclothemCoalZone.PyriticCoal,
                    seam.SeamIndex,
                    seam.Ratio,
                    seam.Split,
                    false,
                    true,
                    false);
            }

            return new CyclothemCoalSample(
                CyclothemCoalZone.Bituminous,
                seam.SeamIndex,
                seam.Ratio,
                seam.Split,
                false,
                true,
                false);
        }

        // Seat-earth and fireclay sit in a fixed window beneath every principal seam.
        StructuralCoordinates p = GetLocal(x, z);
        for (int i = 0; i < seams.Length; i++)
        {
            CoalSeam seam = seams[i];
            if (seam.IsRider) continue;

            double below = seam.Level - stratY;
            double seatTop = seam.HalfThickness + SeatTopMargin;

            double seatU = (p.Along - seam.CenterAlong) / (seam.RadiusAlong * SeatScale);
            double seatV = (p.Across - seam.CenterAcross) / (seam.RadiusAcross * SeatScale);
            if (seatU * seatU + seatV * seatV <= 1.0
                && below > seatTop
                && below < seatTop + SeatThickness)
            {
                return new CyclothemCoalSample(
                    CyclothemCoalZone.OrganicClaystone,
                    i,
                    0.0,
                    false,
                    false,
                    false,
                    false);
            }

            double fireclayTop = seatTop + FireclayOffset;
            double fireU = (p.Along - seam.CenterAlong) / (seam.RadiusAlong * FireclayScale);
            double fireV = (p.Across - seam.CenterAcross) / (seam.RadiusAcross * FireclayScale);
            if (fireU * fireU + fireV * fireV <= 1.0
                && below > fireclayTop
                && below < fireclayTop + FireclayThickness)
            {
                double patchField = Math.Sin(p.Along * 0.28 + seam.Phase)
                    + 0.55 * Math.Cos(p.Across * 0.33 - seam.Phase * 0.4);
                if (patchField > FireclayPatchThreshold)
                {
                    return new CyclothemCoalSample(
                        CyclothemCoalZone.Fireclay,
                        i,
                        0.0,
                        false,
                        false,
                        false,
                        false);
                }
            }
        }

        return default;
    }

    /// <summary>
    /// <c>sampleChannel</c>: elliptical cross-section along a sinusoidally bent centreline.
    /// </summary>
    private bool SampleChannel(double x, double z, double stratY)
    {
        StructuralCoordinates p = GetLocal(x, z);
        for (int i = 0; i < channels.Length; i++)
        {
            PaleoChannel c = channels[i];
            double vx = c.EndAlong - c.StartAlong;
            double vz = c.EndAcross - c.StartAcross;
            double lengthSquared = vx * vx + vz * vz;
            double t = Clamp(
                ((p.Along - c.StartAlong) * vx + (p.Across - c.StartAcross) * vz)
                    / Math.Max(0.1, lengthSquared),
                0.0,
                1.0);
            double length = Math.Sqrt(lengthSquared);
            double curve = Math.Sin(Math.PI * t) * c.Bend;
            double centerAlong = c.StartAlong + vx * t + (length > 0.1 ? -vz / length * curve : 0.0);
            double centerAcross = c.StartAcross + vz * t + (length > 0.1 ? vx / length * curve : 0.0);
            double horizontalDistance = Math.Sqrt(
                (p.Along - centerAlong) * (p.Along - centerAlong)
                + (p.Across - centerAcross) * (p.Across - centerAcross));
            double centerStratY = c.CenterStratY + Math.Sin(Math.PI * t + c.Phase) * 0.3;
            double verticalDistance = Math.Abs(stratY - centerStratY);
            double profile = Math.Sqrt(
                horizontalDistance * horizontalDistance / (c.Width * c.Width)
                + verticalDistance * verticalDistance / (c.HalfThickness * c.HalfThickness));
            if (profile <= 1.0) return true;
        }

        return false;
    }

    /// <summary>
    /// <c>nearestChannelMargin</c>: distance from the nearest channel wall, which drives
    /// the seam split.
    /// </summary>
    private double NearestChannelMargin(double x, double z)
    {
        StructuralCoordinates p = GetLocal(x, z);
        double margin = double.PositiveInfinity;
        for (int i = 0; i < channels.Length; i++)
        {
            PaleoChannel c = channels[i];
            double vx = c.EndAlong - c.StartAlong;
            double vz = c.EndAcross - c.StartAcross;
            double lengthSquared = vx * vx + vz * vz;
            double t = Clamp(
                ((p.Along - c.StartAlong) * vx + (p.Across - c.StartAcross) * vz)
                    / Math.Max(0.1, lengthSquared),
                0.0,
                1.0);
            double length = Math.Sqrt(lengthSquared);
            double curve = Math.Sin(Math.PI * t) * c.Bend;
            double centerAlong = c.StartAlong + vx * t + (length > 0.1 ? -vz / length * curve : 0.0);
            double centerAcross = c.StartAcross + vz * t + (length > 0.1 ? vx / length * curve : 0.0);
            double distance = Math.Sqrt(
                (p.Along - centerAlong) * (p.Along - centerAlong)
                + (p.Across - centerAcross) * (p.Across - centerAcross));
            margin = Math.Min(margin, Math.Abs(distance - c.Width));
        }

        return margin;
    }

    /// <summary>
    /// <c>sampleSeams</c>: warped elliptical footprint, edge-tapered thickness, and a
    /// channel-margin split that turns one principal seam into two leaves.
    /// </summary>
    private SeamHit? SampleSeams(double x, double z, double stratY)
    {
        StructuralCoordinates p = GetLocal(x, z);
        double margin = NearestChannelMargin(x, z);
        SeamHit? best = null;

        for (int i = 0; i < seams.Length; i++)
        {
            CoalSeam seam = seams[i];
            double u = (p.Along - seam.CenterAlong) / seam.RadiusAlong;
            double v = (p.Across - seam.CenterAcross) / seam.RadiusAcross;
            double footprint = Math.Sqrt(u * u + v * v)
                + Math.Sin(u * 4.2 + seam.Phase) * Math.Cos(v * 3.1) * 0.08;
            if (footprint > SeamFootprintLimit) continue;

            double edgeScale = Clamp((SeamFootprintLimit - footprint) / SeamEdgeScaleWidth, 0.0, 1.0);
            double thickness = Math.Max(
                SeamMinimumThickness,
                seam.HalfThickness
                    * (0.88 + 0.12 * Math.Sin(x * 0.1 - z * 0.08 + seam.Phase))
                    * Math.Sqrt(edgeScale));

            double split = Clamp((SplitMarginReach - margin) / SplitMarginReach, 0.0, 1.0)
                * (seam.IsRider ? 0.0 : SplitAmplitude);
            bool splitActive = split > SplitActivation;

            if (splitActive)
            {
                TrySeamCenter(seam.Level - split, thickness, stratY, i, true, ref best);
                TrySeamCenter(seam.Level + split, thickness, stratY, i, true, ref best);
            }
            else
            {
                TrySeamCenter(seam.Level, thickness, stratY, i, false, ref best);
            }
        }

        return best;
    }

    private static void TrySeamCenter(
        double center,
        double thickness,
        double stratY,
        int seamIndex,
        bool split,
        ref SeamHit? best)
    {
        double d = Math.Abs(stratY - center);
        if (d > thickness) return;

        double ratio = d / thickness;
        if (best.HasValue && ratio >= best.Value.Ratio) return;

        best = new SeamHit(seamIndex, ratio, split);
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

    public readonly record struct StructuralCoordinates(double Along, double Across);

    private readonly record struct SeamHit(int SeamIndex, double Ratio, bool Split);

    private readonly record struct CoalSeam(
        double Level,
        double HalfThickness,
        double CenterAlong,
        double CenterAcross,
        double RadiusAlong,
        double RadiusAcross,
        double Phase,
        bool HasParting,
        bool IsRider);

    private readonly record struct PaleoChannel(
        double StartAlong,
        double StartAcross,
        double EndAlong,
        double EndAcross,
        double CenterStratY,
        double Width,
        double HalfThickness,
        double Bend,
        double Phase);
}

internal sealed class CyclothemCoalProceduralTemplate : IProceduralDepositTemplate
{
    private static readonly string[] RequiredSlots =
    {
        ProceduralMaterialSlots.BituminousCoal,
        ProceduralMaterialSlots.ShaleParting,
        ProceduralMaterialSlots.SeatEarth,
        ProceduralMaterialSlots.Fireclay,
        ProceduralMaterialSlots.ChannelSand
    };

    public string Code => "cyclothemCoal";
    public IReadOnlyList<string> RequiredMaterialSlots => RequiredSlots;
    public bool RequiresPlan => true;

    public int GetMaximumHorizontalReach(ProceduralDepositDefinition definition)
    {
        return definition.CyclothemCoal.HorizontalRadius;
    }

    public object CreatePlan(in ProceduralDepositInstance instance, ProceduralDepositDefinition definition)
    {
        return CyclothemCoalPlan.Create(instance, definition.CyclothemCoal);
    }

    public bool Validate(ProceduralDepositDefinition definition, out string error)
    {
        CyclothemCoalDefinition settings = definition.CyclothemCoal;
        bool valid = settings.HorizontalRadius >= 16
            && settings.VerticalHalfHeight >= 8
            && settings.SeamCountMin >= 1
            && settings.SeamCountMax >= settings.SeamCountMin
            && settings.ChannelCountMin >= 1
            && settings.ChannelCountMax >= settings.ChannelCountMin
            && settings.StrikeDegMax >= settings.StrikeDegMin
            && settings.BedDipDegMax >= settings.BedDipDegMin
            && settings.FirstSeamLevelMax >= settings.FirstSeamLevelMin
            && settings.SeamLevelStepMin > 0.0
            && settings.SeamLevelStepMax >= settings.SeamLevelStepMin
            && settings.SeamHalfThicknessMin > 0.0
            && settings.SeamHalfThicknessMax >= settings.SeamHalfThicknessMin
            && settings.SeamRadiusAlongMin > 0.0
            && settings.SeamRadiusAlongMax >= settings.SeamRadiusAlongMin
            && settings.SeamRadiusAcrossMin > 0.0
            && settings.SeamRadiusAcrossMax >= settings.SeamRadiusAcrossMin
            && settings.PartingProbability is >= 0.0 and <= 1.0
            && settings.RiderProbability is >= 0.0 and <= 1.0
            && settings.RiderLevelOffsetMax >= settings.RiderLevelOffsetMin
            && settings.RiderHalfThicknessMin > 0.0
            && settings.RiderHalfThicknessMax >= settings.RiderHalfThicknessMin
            && settings.RiderRadiusScaleMin > 0.0
            && settings.RiderRadiusScaleMax >= settings.RiderRadiusScaleMin
            && settings.ChannelHalfLengthMin > 0.0
            && settings.ChannelHalfLengthMax >= settings.ChannelHalfLengthMin
            && settings.ChannelStratYOffsetMax >= settings.ChannelStratYOffsetMin
            && settings.ChannelWidthMin > 0.0
            && settings.ChannelWidthMax >= settings.ChannelWidthMin
            && settings.ChannelHalfThicknessMin > 0.0
            && settings.ChannelHalfThicknessMax >= settings.ChannelHalfThicknessMin
            && settings.ChannelBendMax >= settings.ChannelBendMin;
        error = valid ? string.Empty : "invalid cyclothem coal settings";
        return valid;
    }

    public void Realize(
        ProceduralDepositWorldGenSystem system,
        in DepositCandidate candidate,
        IChunkColumnGenerateRequest request,
        int baseX,
        int baseZ)
    {
        system.RealizeCyclothemCoalCandidate(candidate, request, baseX, baseZ);
    }
}

public sealed partial class ProceduralDepositWorldGenSystem
{
    internal void RealizeCyclothemCoalCandidate(
        DepositCandidate candidate,
        IChunkColumnGenerateRequest request,
        int baseX,
        int baseZ)
    {
        CompiledProceduralDeposit compiled = candidate.Compiled;
        CyclothemCoalDefinition settings = compiled.Definition.CyclothemCoal;
        var plan = (CyclothemCoalPlan)candidate.Plan!;
        ushort[] heightMap = request.Chunks[0].MapChunk.WorldGenTerrainHeightMap;
        int worldHeight = serverApi!.World.BlockAccessor.MapSizeY;
        int minimumY = GameMath.Clamp(candidate.Instance.CenterY - settings.VerticalHalfHeight, 1, worldHeight - 2);
        int maximumPrototypeY = GameMath.Clamp(candidate.Instance.CenterY + settings.VerticalHalfHeight, 1, worldHeight - 2);

        int[] zoneSlots = BuildCyclothemCoalZoneSlots(compiled);

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
                    CyclothemCoalSample sample = plan.Evaluate(worldX, y, worldZ, surfaceY);
                    if (sample.Zone == CyclothemCoalZone.None) continue;

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
                    AdvancedGeology.Byproducts.ByproductSystem.RecordPlacement(request.Chunks[chunkY], index3d, placeBlockId, candidate.Instance.FeatureId, compiled);
                    data.SetFluid(index3d, 0);
                }
            }
        }
    }

    private static int[] BuildCyclothemCoalZoneSlots(CompiledProceduralDeposit compiled)
    {
        var slots = new int[Enum.GetValues<CyclothemCoalZone>().Length];
        Array.Fill(slots, -1);

        // Bituminous, pyritic and high-ash coal are all the same mineable coal block.
        int coalSlot = compiled.GetSlotId(ProceduralMaterialSlots.BituminousCoal);
        slots[(int)CyclothemCoalZone.Bituminous] = coalSlot;
        slots[(int)CyclothemCoalZone.PyriticCoal] = coalSlot;
        slots[(int)CyclothemCoalZone.HighAshCoal] = coalSlot;

        slots[(int)CyclothemCoalZone.ShaleParting] = compiled.GetSlotId(ProceduralMaterialSlots.ShaleParting);
        slots[(int)CyclothemCoalZone.OrganicClaystone] = compiled.GetSlotId(ProceduralMaterialSlots.SeatEarth);
        slots[(int)CyclothemCoalZone.Fireclay] = compiled.GetSlotId(ProceduralMaterialSlots.Fireclay);
        slots[(int)CyclothemCoalZone.ChannelSandstone] = compiled.GetSlotId(ProceduralMaterialSlots.ChannelSand);
        return slots;
    }
}
