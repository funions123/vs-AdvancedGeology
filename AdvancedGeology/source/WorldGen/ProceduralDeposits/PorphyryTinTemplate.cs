using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using Vintagestory.API.Server;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace AdvancedGeology.WorldGen.ProceduralDeposits;

public enum PorphyryTinZone
{
    None = 0,
    Cassiterite,
    Stannite,
    Chalcopyrite,
    Sphalerite,
    Galena,
    Bismuthinite,
    Stibnite,
    TourmalineBreccia,
    QuartzVein,
    IronSulfides
}

/// <summary>Vein generation stage, which controls the mineral zoning.</summary>
internal enum PorphyryTinStage
{
    Early = 0,
    Middle,
    Late
}

[JsonObject(MemberSerialization.OptIn)]
public sealed class PorphyryTinDefinition
{
    // Model extent is 72x60. The alteration halo reaches topRadius + 7 (max 22) from a plug
    // centre offset up to 4 blocks, and radial veins reach length 19 from the wobbling axis.
    [JsonProperty]
    public int HorizontalRadius { get; set; } = 40;

    [JsonProperty]
    public int VerticalHalfHeight { get; set; } = 30;

    [JsonProperty]
    public double CenterSpread { get; set; } = 4.0;

    [JsonProperty]
    public double BottomMin { get; set; } = -29.0;

    [JsonProperty]
    public double BottomMax { get; set; } = -25.0;

    [JsonProperty]
    public double TopMin { get; set; } = 6.0;

    [JsonProperty]
    public double TopMax { get; set; } = 11.0;

    [JsonProperty]
    public double BaseRadiusMin { get; set; } = 6.5;

    [JsonProperty]
    public double BaseRadiusMax { get; set; } = 9.0;

    [JsonProperty]
    public double TopRadiusMin { get; set; } = 11.0;

    [JsonProperty]
    public double TopRadiusMax { get; set; } = 15.0;

    [JsonProperty]
    public double BrecciaTopMin { get; set; } = 17.0;

    [JsonProperty]
    public double BrecciaTopMax { get; set; } = 23.0;

    // Early stage radial vein set.
    [JsonProperty]
    public int EarlyVeinCountMin { get; set; } = 3;

    [JsonProperty]
    public int EarlyVeinCountMax { get; set; } = 5;

    [JsonProperty]
    public double EarlyCenterYMin { get; set; } = -12.0;

    [JsonProperty]
    public double EarlyCenterYMax { get; set; } = -8.0;

    [JsonProperty]
    public double EarlyLengthMin { get; set; } = 10.0;

    [JsonProperty]
    public double EarlyLengthMax { get; set; } = 17.0;

    [JsonProperty]
    public double EarlyHeightMin { get; set; } = 15.0;

    [JsonProperty]
    public double EarlyHeightMax { get; set; } = 23.0;

    [JsonProperty]
    public double EarlyWidthMin { get; set; } = 0.22;

    [JsonProperty]
    public double EarlyWidthMax { get; set; } = 0.42;

    [JsonProperty]
    public double EarlyOffsetSpread { get; set; } = 3.0;

    // Middle stage radial vein set.
    [JsonProperty]
    public int MiddleVeinCountMin { get; set; } = 3;

    [JsonProperty]
    public int MiddleVeinCountMax { get; set; } = 5;

    [JsonProperty]
    public double MiddleCenterYMin { get; set; } = -2.0;

    [JsonProperty]
    public double MiddleCenterYMax { get; set; } = 3.0;

    [JsonProperty]
    public double MiddleLengthMin { get; set; } = 11.0;

    [JsonProperty]
    public double MiddleLengthMax { get; set; } = 19.0;

    [JsonProperty]
    public double MiddleHeightMin { get; set; } = 12.0;

    [JsonProperty]
    public double MiddleHeightMax { get; set; } = 19.0;

    [JsonProperty]
    public double MiddleWidthMin { get; set; } = 0.2;

    [JsonProperty]
    public double MiddleWidthMax { get; set; } = 0.4;

    [JsonProperty]
    public double MiddleOffsetSpread { get; set; } = 5.0;

    // Late stage radial vein set.
    [JsonProperty]
    public int LateVeinCountMin { get; set; } = 2;

    [JsonProperty]
    public int LateVeinCountMax { get; set; } = 4;

    [JsonProperty]
    public double LateCenterYMin { get; set; } = 8.0;

    [JsonProperty]
    public double LateCenterYMax { get; set; } = 12.0;

    [JsonProperty]
    public double LateLengthMin { get; set; } = 10.0;

    [JsonProperty]
    public double LateLengthMax { get; set; } = 18.0;

    [JsonProperty]
    public double LateHeightMin { get; set; } = 8.0;

    [JsonProperty]
    public double LateHeightMax { get; set; } = 14.0;

    [JsonProperty]
    public double LateWidthMin { get; set; } = 0.18;

    [JsonProperty]
    public double LateWidthMax { get; set; } = 0.36;

    [JsonProperty]
    public double LateOffsetSpread { get; set; } = 7.0;

    [JsonProperty]
    public double VeinAngleJitter { get; set; } = 0.14;

    [JsonProperty]
    public double VeinRelaySpread { get; set; } = 1.4;

    [JsonProperty]
    public double VeinCenterYJitter { get; set; } = 2.0;

    // Concentric ring veins.
    [JsonProperty]
    public int RingVeinCountMin { get; set; } = 2;

    [JsonProperty]
    public int RingVeinCountMax { get; set; } = 3;

    [JsonProperty]
    public double RingRadiusMin { get; set; } = 8.0;

    [JsonProperty]
    public double RingRadiusMax { get; set; } = 16.0;

    [JsonProperty]
    public double RingHalfHeightMin { get; set; } = 6.0;

    [JsonProperty]
    public double RingHalfHeightMax { get; set; } = 11.0;

    [JsonProperty]
    public double RingWidthMin { get; set; } = 0.2;

    [JsonProperty]
    public double RingWidthMax { get; set; } = 0.42;

    // Breccia clasts.
    [JsonProperty]
    public int ClastCountMin { get; set; } = 18;

    [JsonProperty]
    public int ClastCountMax { get; set; } = 30;

    [JsonProperty]
    public double ClastRadialMin { get; set; } = 2.0;

    [JsonProperty]
    public double ClastRadialMax { get; set; } = 10.0;

    [JsonProperty]
    public double ClastRadiusXMin { get; set; } = 0.8;

    [JsonProperty]
    public double ClastRadiusXMax { get; set; } = 1.8;

    [JsonProperty]
    public double ClastRadiusYMin { get; set; } = 0.7;

    [JsonProperty]
    public double ClastRadiusYMax { get; set; } = 1.5;

    [JsonProperty]
    public double ClastRadiusZMin { get; set; } = 0.8;

    [JsonProperty]
    public double ClastRadiusZMax { get; set; } = 1.7;

    /// <summary>
    /// Fraction of Sb-Bi sulfosalt voxels replaced by stibnite. The plan calls for 10%, so the
    /// grain-hash gate opens above 0.90.
    /// </summary>
    [JsonProperty]
    public double StibniteSpeckleThreshold { get; set; } = 0.90;
}

/// <summary>
/// Result of classifying one voxel against a <see cref="PorphyryTinPlan"/>.
/// </summary>
public readonly record struct PorphyryTinSample(
    PorphyryTinZone Zone,
    int Grade = 0,
    bool InVein = false,
    bool InBreccia = false,
    bool InPlug = false,
    double SpeckleNoise = 0.0);

/// <summary>
/// Cassiterite, stannite, copper-lead-zinc sulfides, bismuthinite, stibnite, quartz, and tourmaline breccia form successive radial and ring veins around a porphyry plug.
/// </summary>
internal sealed class PorphyryTinPlan
{
    private const ulong PlanSalt = 0x504F5250485954UL; // "PORPHYT"

    // Local model floor: y < -30 is below the modelled edifice (GRID_Y / 2).
    private const double BottomLimitY = -30.0;

    private readonly ulong featureId;
    private readonly int originX;
    private readonly int originY;
    private readonly int originZ;

    private readonly double centerX;
    private readonly double centerZ;
    private readonly double bottom;
    private readonly double top;
    private readonly double baseRadius;
    private readonly double topRadius;
    private readonly double brecciaTop;
    private readonly double phase;
    private readonly double stibniteThreshold;

    private readonly RadialVein[] radialVeins;
    private readonly RingVein[] ringVeins;
    private readonly Clast[] clasts;

    public int OriginX => originX;
    public int OriginY => originY;
    public int OriginZ => originZ;
    public double Phase => phase;
    public double Bottom => bottom;
    public double Top => top;
    public double BrecciaTop => brecciaTop;
    public int RadialVeinCount => radialVeins.Length;
    public int RingVeinCount => ringVeins.Length;
    public int ClastCount => clasts.Length;

    private PorphyryTinPlan(
        ulong featureId,
        in ProceduralDepositInstance instance,
        double centerX,
        double centerZ,
        double bottom,
        double top,
        double baseRadius,
        double topRadius,
        double brecciaTop,
        double phase,
        double stibniteThreshold,
        RadialVein[] radialVeins,
        RingVein[] ringVeins,
        Clast[] clasts)
    {
        this.featureId = featureId;
        originX = instance.CenterX;
        originY = instance.CenterY;
        originZ = instance.CenterZ;
        this.centerX = centerX;
        this.centerZ = centerZ;
        this.bottom = bottom;
        this.top = top;
        this.baseRadius = baseRadius;
        this.topRadius = topRadius;
        this.brecciaTop = brecciaTop;
        this.phase = phase;
        this.stibniteThreshold = stibniteThreshold;
        this.radialVeins = radialVeins;
        this.ringVeins = ringVeins;
        this.clasts = clasts;
    }

    /// <summary>
    /// Builds the deposit geometry in deterministic parameter order.
    /// </summary>
    public static PorphyryTinPlan Create(
        in ProceduralDepositInstance instance,
        PorphyryTinDefinition settings)
    {
        var random = new ProceduralDepositRandom(instance.FeatureId ^ PlanSalt);

        double centerX = random.Range(-settings.CenterSpread, settings.CenterSpread);
        double centerZ = random.Range(-settings.CenterSpread, settings.CenterSpread);
        double bottom = random.Range(settings.BottomMin, settings.BottomMax);
        double top = random.Range(settings.TopMin, settings.TopMax);
        double baseRadius = random.Range(settings.BaseRadiusMin, settings.BaseRadiusMax);
        double topRadius = random.Range(settings.TopRadiusMin, settings.TopRadiusMax);
        double brecciaTop = random.Range(settings.BrecciaTopMin, settings.BrecciaTopMax);
        double phase = random.Range(0.0, 100.0);

        // Three vein generations, built in deterministic order.
        int earlyCount = random.NextInt(settings.EarlyVeinCountMin, settings.EarlyVeinCountMax);
        double earlyCenterY = random.Range(settings.EarlyCenterYMin, settings.EarlyCenterYMax);
        int middleCount = random.NextInt(settings.MiddleVeinCountMin, settings.MiddleVeinCountMax);
        double middleCenterY = random.Range(settings.MiddleCenterYMin, settings.MiddleCenterYMax);
        int lateCount = random.NextInt(settings.LateVeinCountMin, settings.LateVeinCountMax);
        double lateCenterY = random.Range(settings.LateCenterYMin, settings.LateCenterYMax);

        var veins = new List<RadialVein>(earlyCount + middleCount + lateCount);
        int index = 0;

        AppendStage(
            veins,
            random,
            settings,
            PorphyryTinStage.Early,
            earlyCount,
            earlyCenterY,
            settings.EarlyLengthMin,
            settings.EarlyLengthMax,
            settings.EarlyHeightMin,
            settings.EarlyHeightMax,
            settings.EarlyWidthMin,
            settings.EarlyWidthMax,
            settings.EarlyOffsetSpread,
            phase,
            ref index);
        AppendStage(
            veins,
            random,
            settings,
            PorphyryTinStage.Middle,
            middleCount,
            middleCenterY,
            settings.MiddleLengthMin,
            settings.MiddleLengthMax,
            settings.MiddleHeightMin,
            settings.MiddleHeightMax,
            settings.MiddleWidthMin,
            settings.MiddleWidthMax,
            settings.MiddleOffsetSpread,
            phase,
            ref index);
        AppendStage(
            veins,
            random,
            settings,
            PorphyryTinStage.Late,
            lateCount,
            lateCenterY,
            settings.LateLengthMin,
            settings.LateLengthMax,
            settings.LateHeightMin,
            settings.LateHeightMax,
            settings.LateWidthMin,
            settings.LateWidthMax,
            settings.LateOffsetSpread,
            phase,
            ref index);

        int ringCount = random.NextInt(settings.RingVeinCountMin, settings.RingVeinCountMax);
        var rings = new RingVein[ringCount];
        for (int i = 0; i < ringCount; i++)
        {
            double radius = random.Range(settings.RingRadiusMin, settings.RingRadiusMax);
            double halfHeight = random.Range(settings.RingHalfHeightMin, settings.RingHalfHeightMax);
            // The first ring is a middle-stage structure; the rest are late.
            double ringCenterY = i == 0
                ? random.Range(0.0, 6.0)
                : random.Range(8.0, 14.0);
            double width = random.Range(settings.RingWidthMin, settings.RingWidthMax);
            rings[i] = new RingVein(
                i == 0 ? PorphyryTinStage.Middle : PorphyryTinStage.Late,
                radius,
                halfHeight,
                ringCenterY,
                width,
                phase + 91.0 + i * 17.0);
        }

        int clastCount = random.NextInt(settings.ClastCountMin, settings.ClastCountMax);
        var clasts = new Clast[clastCount];
        for (int i = 0; i < clastCount; i++)
        {
            double a = random.Range(0.0, 1.0) * Math.PI * 2.0;
            double r = Math.Sqrt(random.Range(0.0, 1.0))
                * random.Range(settings.ClastRadialMin, settings.ClastRadialMax);
            double y = random.Range(top + 1.0, brecciaTop - 1.0);
            clasts[i] = new Clast(
                centerX + Math.Cos(a) * r,
                y,
                centerZ + Math.Sin(a) * r,
                random.Range(settings.ClastRadiusXMin, settings.ClastRadiusXMax),
                random.Range(settings.ClastRadiusYMin, settings.ClastRadiusYMax),
                random.Range(settings.ClastRadiusZMin, settings.ClastRadiusZMax));
        }

        return new PorphyryTinPlan(
            instance.FeatureId,
            instance,
            centerX,
            centerZ,
            bottom,
            top,
            baseRadius,
            topRadius,
            brecciaTop,
            phase,
            settings.StibniteSpeckleThreshold,
            veins.ToArray(),
            rings,
            clasts);
    }

    private static void AppendStage(
        List<RadialVein> veins,
        ProceduralDepositRandom random,
        PorphyryTinDefinition settings,
        PorphyryTinStage stage,
        int count,
        double stageCenterY,
        double lengthMin,
        double lengthMax,
        double heightMin,
        double heightMax,
        double widthMin,
        double widthMax,
        double offsetSpread,
        double phase,
        ref int index)
    {
        for (int i = 0; i < count; i++)
        {
    // Angular distribution: a = (i + .35*random()) * PI/count + randomRange(-.14,.14)
            double angle = (i + 0.35 * random.Range(0.0, 1.0)) * Math.PI / count
                + random.Range(-settings.VeinAngleJitter, settings.VeinAngleJitter);
            double offset = random.Range(-offsetSpread, offsetSpread);
            double length = random.Range(lengthMin, lengthMax);
            double height = random.Range(heightMin, heightMax);
            double width = random.Range(widthMin, widthMax);
            double centerY = stageCenterY
                + random.Range(-settings.VeinCenterYJitter, settings.VeinCenterYJitter);
            double relay = random.Range(-settings.VeinRelaySpread, settings.VeinRelaySpread);

            veins.Add(new RadialVein(
                stage,
                Math.Cos(angle),
                Math.Sin(angle),
                offset,
                length,
                height,
                width,
                centerY,
                phase + index * 13.0,
                relay));
            index++;
        }
    }

    public PorphyryTinSample Evaluate(int worldX, int worldY, int worldZ, int surfaceY = int.MaxValue)
    {
        if (worldY > surfaceY) return default;

        double x = worldX - originX;
        double y = worldY - originY;
        double z = worldZ - originZ;
        if (y < BottomLimitY) return default;

        double field = Math.Sin(x * 0.13 - z * 0.1 + phase)
            + 0.48 * Math.Cos(y * 0.17 + x * 0.06 - phase * 0.3);
        PlugSample plug = SamplePlug(x, y, z);
        BrecciaSample breccia = SampleBreccia(x, y, z);

        // Later stages overprint earlier ones.
        for (int s = 2; s >= 0; s--)
        {
            var stage = (PorphyryTinStage)s;
            VeinHit? radial = SampleRadial(x, y, z, stage);
            VeinHit? ring = radial ?? SampleRing(x, y, z, stage);
            if (!ring.HasValue) continue;

            VeinHit hit = ring.Value;
            PorphyryTinZone zone = VeinMaterial(stage, hit.Radial, field);
            return Finish(zone, x, y, z, true, false, false);
        }

        if (breccia.Inside)
        {
            if (InClast(x, y, z)) return new PorphyryTinSample(PorphyryTinZone.None, 0, false, true, false);

            PorphyryTinZone zone = BrecciaMaterial(breccia, field, x, y, z);
            return Finish(zone, x, y, z, false, true, false);
        }

        if (plug.Inside)
        {
            if (plug.R < 0.42 && plug.T < 0.55 && field > 0.85)
            {
                return Finish(PorphyryTinZone.Cassiterite, x, y, z, false, false, true);
            }

            return new PorphyryTinSample(PorphyryTinZone.None, 0, false, false, true);
        }

        return default;
    }

    private PorphyryTinSample Finish(
        PorphyryTinZone zone,
        double x,
        double y,
        double z,
        bool inVein,
        bool inBreccia,
        bool inPlug)
    {
        double speckle = 0.0;
        if (zone == PorphyryTinZone.Bismuthinite)
        {
            // Sb-Bi sulfosalts are bismuthinite with 10% stibnite speckling.
            speckle = Hash3D(x * 2.1 + phase, y * 2.1, z * 2.1);
            if (speckle >= stibniteThreshold) zone = PorphyryTinZone.Stibnite;
        }

        int grade = 0;
        if (IsGraded(zone))
        {
            double grainNoise = Hash3D(x * 2.7 + phase * 1.3, y * 2.7, z * 2.7);
            grade = grainNoise > 0.82 ? 3 : (grainNoise > 0.55 ? 2 : (grainNoise > 0.25 ? 1 : 0));
        }

        return new PorphyryTinSample(zone, grade, inVein, inBreccia, inPlug, speckle);
    }

    internal static bool IsGraded(PorphyryTinZone zone)
    {
        return zone == PorphyryTinZone.Cassiterite
            || zone == PorphyryTinZone.Stannite
            || zone == PorphyryTinZone.Chalcopyrite
            || zone == PorphyryTinZone.Sphalerite
            || zone == PorphyryTinZone.Galena
            || zone == PorphyryTinZone.Bismuthinite
            || zone == PorphyryTinZone.Stibnite;
    }

    /// <summary><c>axisAt</c>: the plug axis wobbles upward.</summary>
    private AxisPosition AxisAt(double y)
    {
        double t = Clamp((y - bottom) / (brecciaTop - bottom), 0.0, 1.0);
        return new AxisPosition(
            centerX + Math.Sin(t * 3.2 + phase) * 1.5 * t,
            centerZ + Math.Cos(t * 2.7 - phase * 0.3) * 1.2 * t);
    }

    /// <summary>Model <c>samplePlug</c>.</summary>
    private PlugSample SamplePlug(double x, double y, double z)
    {
        if (y < bottom || y > top) return new PlugSample(false, double.PositiveInfinity, 0.0);

        double t = (y - bottom) / (top - bottom);
        AxisPosition a = AxisAt(y);
        double radius = baseRadius + (topRadius - baseRadius) * Math.Pow(t, 0.8);
        double u = (x - a.X) / radius;
        double v = (z - a.Z) / (radius * 0.84);
        double warp = 0.07 * Math.Sin(u * 4.0 + phase) * Math.Cos(v * 3.4);
        double r = Math.Sqrt(u * u + v * v) + warp;
        return new PlugSample(r <= 1.0, r, t);
    }

    /// <summary><c>sampleBreccia</c>: the flaring tourmaline breccia carapace.</summary>
    private BrecciaSample SampleBreccia(double x, double y, double z)
    {
        double baseY = top - 3.0;
        if (y < baseY || y > brecciaTop) return new BrecciaSample(false, double.PositiveInfinity, 0.0);

        double h = Clamp((y - baseY) / (brecciaTop - baseY), 0.0, 1.0);
        AxisPosition a = AxisAt(y);
        double flare = topRadius * (0.55 + 0.5 * h);
        double u = (x - a.X) / flare;
        double v = (z - a.Z) / (flare * 0.86);
        double warp = 0.1 * Math.Sin(u * 4.2 + phase + y * 0.08) * Math.Cos(v * 3.3);
        double r = Math.Sqrt(u * u + v * v) + warp;
        return new BrecciaSample(r <= 1.0, r, h);
    }

    /// <summary>Model <c>inClast</c>.</summary>
    private bool InClast(double x, double y, double z)
    {
        for (int i = 0; i < clasts.Length; i++)
        {
            Clast c = clasts[i];
            double dx = (x - c.X) / c.RadiusX;
            double dy = (y - c.Y) / c.RadiusY;
            double dz = (z - c.Z) / c.RadiusZ;
            if (dx * dx + dy * dy + dz * dz <= 1.0) return true;
        }

        return false;
    }

    /// <summary><c>sampleRadial</c>: bent, relayed, tip-tapered radial vein.</summary>
    private VeinHit? SampleRadial(double x, double y, double z, PorphyryTinStage stage)
    {
        VeinHit? best = null;
        AxisPosition a = AxisAt(y);
        double dx = x - a.X;
        double dz = z - a.Z;

        for (int i = 0; i < radialVeins.Length; i++)
        {
            RadialVein f = radialVeins[i];
            if (f.Stage != stage) continue;

            double along = dx * f.CosAngle - dz * f.SinAngle;
            double across = dx * f.SinAngle + dz * f.CosAngle - f.Offset;
            double uy = (y - f.CenterY) / f.Height;
            double u = along / f.Length;
            double foot = Math.Sqrt(u * u + uy * uy);
            if (foot > 1.05) continue;

            double taper = Math.Sqrt(Clamp(1.0 - foot * foot, 0.0, 1.0));
            double bend = Math.Sin(along * 0.14 + y * 0.08 + f.Phase) * 0.65
                + f.Relay * Math.Tanh((along - 2.0) / 3.0);
            double dist = Math.Abs(across - bend);
            double half = Math.Max(0.1, f.Width * taper);
            double score = dist / half;
            double radial = Clamp(Math.Sqrt(dx * dx + dz * dz) / (topRadius + 3.0), 0.0, 1.0);

            if (score <= 1.0 && (!best.HasValue || score < best.Value.Score))
            {
                best = new VeinHit(score, score < 0.45, radial);
            }
        }

        return best;
    }

    /// <summary><c>sampleRing</c>: concentric ring vein with a lobed radius.</summary>
    private VeinHit? SampleRing(double x, double y, double z, PorphyryTinStage stage)
    {
        VeinHit? best = null;
        AxisPosition a = AxisAt(y);
        double dx = x - a.X;
        double dz = z - a.Z;
        double d = Math.Sqrt(dx * dx + dz * dz);

        for (int i = 0; i < ringVeins.Length; i++)
        {
            RingVein f = ringVeins[i];
            if (f.Stage != stage) continue;

            double v = (y - f.CenterY) / f.HalfHeight;
            if (Math.Abs(v) > 1.0) continue;

            double taper = Math.Sqrt(Clamp(1.0 - v * v, 0.0, 1.0));
            double target = f.Radius + Math.Sin(Math.Atan2(dz, dx) * 3.0 + f.Phase) * 1.2;
            double score = Math.Abs(d - target) / Math.Max(0.12, f.Width * taper);

            if (score <= 1.0 && (!best.HasValue || score < best.Value.Score))
            {
                best = new VeinHit(score, score < 0.45, Clamp(d / (topRadius + 3.0), 0.0, 1.0));
            }
        }

        return best;
    }

    /// <summary><c>veinMaterial</c>: stage-dependent radial mineral zoning.</summary>
    private static PorphyryTinZone VeinMaterial(PorphyryTinStage stage, double radial, double field)
    {
        if (stage == PorphyryTinStage.Early)
        {
            if (radial < 0.55 && field > -0.38) return PorphyryTinZone.Cassiterite;
            if (radial < 0.76 && field > 0.42) return PorphyryTinZone.Cassiterite;
            if (field > -0.18) return PorphyryTinZone.TourmalineBreccia;
            return PorphyryTinZone.QuartzVein;
        }

        if (stage == PorphyryTinStage.Middle)
        {
            if (radial < 0.48)
            {
                return field > -0.22 ? PorphyryTinZone.Stannite : PorphyryTinZone.Chalcopyrite;
            }

            if (radial < 0.76)
            {
                return field > 0.38 ? PorphyryTinZone.Stannite : PorphyryTinZone.Chalcopyrite;
            }

            if (field > -0.35) return PorphyryTinZone.Sphalerite;
            return PorphyryTinZone.IronSulfides;
        }

        if (radial > 0.62)
        {
            return field > 0.05 ? PorphyryTinZone.Bismuthinite : PorphyryTinZone.Galena;
        }

        if (radial > 0.38)
        {
            return field > 0.48 ? PorphyryTinZone.Bismuthinite : PorphyryTinZone.Galena;
        }

        return field > 0.22 ? PorphyryTinZone.Sphalerite : PorphyryTinZone.Galena;
    }

    /// <summary><c>brecciaMaterial</c>: height-zoned ore corridors in the carapace.</summary>
    private PorphyryTinZone BrecciaMaterial(in BrecciaSample b, double field, double x, double y, double z)
    {
        double corridor = Math.Sin(x * 0.31 + phase) * Math.Cos(z * 0.27 - phase * 0.4)
            + 0.5 * Math.Sin((x + z) * 0.19 + y * 0.08);
        bool oreCorridor = corridor > 0.48 && b.R < 0.72;

        if (b.H < 0.32)
        {
            if (oreCorridor && b.R < 0.48 && field > -0.15) return PorphyryTinZone.Cassiterite;
            return field > 0.05 ? PorphyryTinZone.TourmalineBreccia : PorphyryTinZone.QuartzVein;
        }

        if (b.H < 0.68)
        {
            if (oreCorridor && b.R < 0.5)
            {
                return field > 0.05 ? PorphyryTinZone.Stannite : PorphyryTinZone.Chalcopyrite;
            }

            if (oreCorridor && field > 0.38) return PorphyryTinZone.Sphalerite;
            return field > 0.18 ? PorphyryTinZone.IronSulfides : PorphyryTinZone.TourmalineBreccia;
        }

        if (oreCorridor && b.R > 0.42)
        {
            return field > 0.1 ? PorphyryTinZone.Bismuthinite : PorphyryTinZone.Galena;
        }

        return field > 0.28 ? PorphyryTinZone.TourmalineBreccia : PorphyryTinZone.QuartzVein;
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

    private readonly record struct AxisPosition(double X, double Z);

    private readonly record struct PlugSample(bool Inside, double R, double T);

    private readonly record struct BrecciaSample(bool Inside, double R, double H);

    private readonly record struct VeinHit(double Score, bool Core, double Radial);

    private readonly record struct RadialVein(
        PorphyryTinStage Stage,
        double CosAngle,
        double SinAngle,
        double Offset,
        double Length,
        double Height,
        double Width,
        double CenterY,
        double Phase,
        double Relay);

    private readonly record struct RingVein(
        PorphyryTinStage Stage,
        double Radius,
        double HalfHeight,
        double CenterY,
        double Width,
        double Phase);

    private readonly record struct Clast(
        double X,
        double Y,
        double Z,
        double RadiusX,
        double RadiusY,
        double RadiusZ);
}

internal sealed class PorphyryTinProceduralTemplate : IProceduralDepositTemplate
{
    private static readonly string[] RequiredSlots =
    {
        ProceduralMaterialSlots.Cassiterite,
        ProceduralMaterialSlots.Stannite,
        ProceduralMaterialSlots.Chalcopyrite,
        ProceduralMaterialSlots.Sphalerite,
        ProceduralMaterialSlots.Galena,
        ProceduralMaterialSlots.Bismuthinite,
        ProceduralMaterialSlots.Stibnite,
        ProceduralMaterialSlots.Pyrite,
        ProceduralMaterialSlots.Tourmaline,
        ProceduralMaterialSlots.Quartz
    };

    public string Code => "porphyryTin";
    public IReadOnlyList<string> RequiredMaterialSlots => RequiredSlots;
    public bool RequiresPlan => true;

    public int GetMaximumHorizontalReach(ProceduralDepositDefinition definition)
    {
        return definition.PorphyryTin.HorizontalRadius;
    }

    public object CreatePlan(in ProceduralDepositInstance instance, ProceduralDepositDefinition definition)
    {
        return PorphyryTinPlan.Create(instance, definition.PorphyryTin);
    }

    public bool Validate(ProceduralDepositDefinition definition, out string error)
    {
        PorphyryTinDefinition settings = definition.PorphyryTin;
        bool valid = settings.HorizontalRadius >= 16
            && settings.VerticalHalfHeight >= 8
            && settings.BottomMax >= settings.BottomMin
            && settings.TopMax >= settings.TopMin
            && settings.TopMin > settings.BottomMax
            && settings.BaseRadiusMin > 0.0
            && settings.BaseRadiusMax >= settings.BaseRadiusMin
            && settings.TopRadiusMin > 0.0
            && settings.TopRadiusMax >= settings.TopRadiusMin
            && settings.BrecciaTopMax >= settings.BrecciaTopMin
            && settings.BrecciaTopMin > settings.TopMax
            && settings.EarlyVeinCountMin >= 1
            && settings.EarlyVeinCountMax >= settings.EarlyVeinCountMin
            && settings.MiddleVeinCountMin >= 1
            && settings.MiddleVeinCountMax >= settings.MiddleVeinCountMin
            && settings.LateVeinCountMin >= 1
            && settings.LateVeinCountMax >= settings.LateVeinCountMin
            && settings.RingVeinCountMin >= 1
            && settings.RingVeinCountMax >= settings.RingVeinCountMin
            && settings.ClastCountMin >= 0
            && settings.ClastCountMax >= settings.ClastCountMin
            && settings.EarlyLengthMin > 0.0
            && settings.EarlyLengthMax >= settings.EarlyLengthMin
            && settings.MiddleLengthMin > 0.0
            && settings.MiddleLengthMax >= settings.MiddleLengthMin
            && settings.LateLengthMin > 0.0
            && settings.LateLengthMax >= settings.LateLengthMin
            && settings.EarlyWidthMin > 0.0
            && settings.EarlyWidthMax >= settings.EarlyWidthMin
            && settings.MiddleWidthMin > 0.0
            && settings.MiddleWidthMax >= settings.MiddleWidthMin
            && settings.LateWidthMin > 0.0
            && settings.LateWidthMax >= settings.LateWidthMin
            && settings.RingRadiusMin > 0.0
            && settings.RingRadiusMax >= settings.RingRadiusMin
            && settings.RingHalfHeightMin > 0.0
            && settings.RingHalfHeightMax >= settings.RingHalfHeightMin
            && settings.RingWidthMin > 0.0
            && settings.RingWidthMax >= settings.RingWidthMin
            && settings.StibniteSpeckleThreshold is >= 0.0 and <= 1.0;
        error = valid ? string.Empty : "invalid porphyry tin settings";
        return valid;
    }

    public void Realize(
        ProceduralDepositWorldGenSystem system,
        in DepositCandidate candidate,
        IChunkColumnGenerateRequest request,
        int baseX,
        int baseZ)
    {
        system.RealizePorphyryTinCandidate(candidate, request, baseX, baseZ);
    }
}

public sealed partial class ProceduralDepositWorldGenSystem
{
    internal void RealizePorphyryTinCandidate(
        DepositCandidate candidate,
        IChunkColumnGenerateRequest request,
        int baseX,
        int baseZ)
    {
        CompiledProceduralDeposit compiled = candidate.Compiled;
        PorphyryTinDefinition settings = compiled.Definition.PorphyryTin;
        var plan = (PorphyryTinPlan)candidate.Plan!;
        ushort[] heightMap = request.Chunks[0].MapChunk.WorldGenTerrainHeightMap;
        int worldHeight = serverApi!.World.BlockAccessor.MapSizeY;
        int minimumY = GameMath.Clamp(candidate.Instance.CenterY - settings.VerticalHalfHeight, 1, worldHeight - 2);
        int maximumPrototypeY = GameMath.Clamp(candidate.Instance.CenterY + settings.VerticalHalfHeight, 1, worldHeight - 2);

        int[] zoneSlots = BuildPorphyryTinZoneSlots(compiled);

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
                    PorphyryTinSample sample = plan.Evaluate(worldX, y, worldZ, surfaceY);
                    if (sample.Zone == PorphyryTinZone.None) continue;

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

    private static int[] BuildPorphyryTinZoneSlots(CompiledProceduralDeposit compiled)
    {
        var slots = new int[Enum.GetValues<PorphyryTinZone>().Length];
        Array.Fill(slots, -1);
        slots[(int)PorphyryTinZone.Cassiterite] = compiled.GetSlotId(ProceduralMaterialSlots.Cassiterite);
        slots[(int)PorphyryTinZone.Stannite] = compiled.GetSlotId(ProceduralMaterialSlots.Stannite);
        slots[(int)PorphyryTinZone.Chalcopyrite] = compiled.GetSlotId(ProceduralMaterialSlots.Chalcopyrite);
        slots[(int)PorphyryTinZone.Sphalerite] = compiled.GetSlotId(ProceduralMaterialSlots.Sphalerite);
        slots[(int)PorphyryTinZone.Galena] = compiled.GetSlotId(ProceduralMaterialSlots.Galena);
        slots[(int)PorphyryTinZone.Bismuthinite] = compiled.GetSlotId(ProceduralMaterialSlots.Bismuthinite);
        slots[(int)PorphyryTinZone.Stibnite] = compiled.GetSlotId(ProceduralMaterialSlots.Stibnite);
        slots[(int)PorphyryTinZone.TourmalineBreccia] = compiled.GetSlotId(ProceduralMaterialSlots.Tourmaline);
        slots[(int)PorphyryTinZone.QuartzVein] = compiled.GetSlotId(ProceduralMaterialSlots.Quartz);
        slots[(int)PorphyryTinZone.IronSulfides] = compiled.GetSlotId(ProceduralMaterialSlots.Pyrite);
        return slots;
    }
}
