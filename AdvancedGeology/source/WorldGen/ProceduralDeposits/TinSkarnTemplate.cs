using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using Vintagestory.API.Server;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace AdvancedGeology.WorldGen.ProceduralDeposits;

public enum TinSkarnZone
{
    None = 0,
    Cassiterite,
    Scheelite,
    Sphalerite,
    Chalcopyrite,
    Galena,
    IronSulfides,
    Fluorite
}

[JsonObject(MemberSerialization.OptIn)]
public sealed class TinSkarnDefinition
{
    // Model extent is 72x60. Manto bodies reach ra up to 21 * sqrt(3/2) from a centre offset up
    // to 14 blocks off the intrusive source.
    [JsonProperty]
    public int HorizontalRadius { get; set; } = 44;

    [JsonProperty]
    public int VerticalHalfHeight { get; set; } = 30;

    [JsonProperty]
    public double StrikeDegMin { get; set; } = 0.0;

    [JsonProperty]
    public double StrikeDegMax { get; set; } = 180.0;

    [JsonProperty]
    public double BedDipDegMin { get; set; } = 4.0;

    [JsonProperty]
    public double BedDipDegMax { get; set; } = 12.0;

    [JsonProperty]
    public double SourceSpread { get; set; } = 5.0;

    [JsonProperty]
    public int MantoCountMin { get; set; } = 2;

    [JsonProperty]
    public int MantoCountMax { get; set; } = 4;

    [JsonProperty]
    public double MantoFirstLevelMin { get; set; } = -10.0;

    [JsonProperty]
    public double MantoFirstLevelMax { get; set; } = -5.0;

    [JsonProperty]
    public double MantoLevelStepMin { get; set; } = 4.0;

    [JsonProperty]
    public double MantoLevelStepMax { get; set; } = 7.0;

    [JsonProperty]
    public double MantoStrikeJitter { get; set; } = 9.0;

    [JsonProperty]
    public double MantoCenterXSpread { get; set; } = 14.0;

    [JsonProperty]
    public double MantoCenterZSpread { get; set; } = 11.0;

    [JsonProperty]
    public double MantoRadiusAlongMin { get; set; } = 13.0;

    [JsonProperty]
    public double MantoRadiusAlongMax { get; set; } = 21.0;

    [JsonProperty]
    public double MantoRadiusAcrossMin { get; set; } = 6.0;

    [JsonProperty]
    public double MantoRadiusAcrossMax { get; set; } = 11.0;

    [JsonProperty]
    public double MantoRadiusVerticalMin { get; set; } = 1.8;

    [JsonProperty]
    public double MantoRadiusVerticalMax { get; set; } = 3.4;

    [JsonProperty]
    public int ChimneyCountMin { get; set; } = 2;

    [JsonProperty]
    public int ChimneyCountMax { get; set; } = 4;

    [JsonProperty]
    public double ChimneyStrikeJitter { get; set; } = 35.0;

    [JsonProperty]
    public double ChimneyDipDegMin { get; set; } = 68.0;

    [JsonProperty]
    public double ChimneyDipDegMax { get; set; } = 86.0;

    [JsonProperty]
    public double ChimneyCenterJitter { get; set; } = 3.0;

    [JsonProperty]
    public double ChimneyDropMin { get; set; } = 4.0;

    [JsonProperty]
    public double ChimneyDropMax { get; set; } = 9.0;

    [JsonProperty]
    public double ChimneyHeightMin { get; set; } = 10.0;

    [JsonProperty]
    public double ChimneyHeightMax { get; set; } = 18.0;

    [JsonProperty]
    public double ChimneyRadiusMin { get; set; } = 1.5;

    [JsonProperty]
    public double ChimneyRadiusMax { get; set; } = 2.8;

    [JsonProperty]
    public double PodCenterJitter { get; set; } = 2.5;

    [JsonProperty]
    public double PodLevelJitter { get; set; } = 1.0;

    [JsonProperty]
    public double PodRadiusXMin { get; set; } = 3.5;

    [JsonProperty]
    public double PodRadiusXMax { get; set; } = 6.0;

    [JsonProperty]
    public double PodRadiusYMin { get; set; } = 2.2;

    [JsonProperty]
    public double PodRadiusYMax { get; set; } = 4.0;

    [JsonProperty]
    public double PodRadiusZMin { get; set; } = 3.0;

    [JsonProperty]
    public double PodRadiusZMax { get; set; } = 5.0;

    [JsonProperty]
    public double StratigraphicFloor { get; set; } = -27.0;

    [JsonProperty]
    public double StratigraphicCeiling { get; set; } = 15.0;

    /// <summary>
    /// Minimum spatial-hash value for fluorite speckles; 0.90 selects the upper 10%.
    /// </summary>
    [JsonProperty]
    public double FluoriteSpeckleThreshold { get; set; } = 0.90;
}

/// <summary>
/// Result of classifying one voxel in a carbonate-replacement tin skarn.
/// </summary>
public readonly record struct TinSkarnSample(
    TinSkarnZone Zone,
    int Grade = 0,
    bool InManto = false,
    bool InChimney = false,
    bool InPod = false,
    bool InFluoriteCarbonate = false,
    double SpeckleNoise = 0.0);
/// <summary>
/// Cassiterite, scheelite, copper-lead-zinc sulfides, iron sulfides, and fluorite form bedding-parallel carbonate-replacement mantos fed by steep chimneys and proximal pods.
/// </summary>
internal sealed class TinSkarnPlan
{
    private const ulong PlanSalt = 0x54494E534B41524EUL; // "TINSKARN"

    // Local model floor: y < -30 is below the modelled skarn package (GRID_Y / 2).
    private const double BottomY = -30.0;

    private readonly ulong featureId;
    private readonly int originX;
    private readonly int originY;
    private readonly int originZ;


    private readonly double strikeDeg;
    private readonly double dipDeg;
    private readonly double sourceX;
    private readonly double sourceZ;
    private readonly double seed;
    private readonly double stratigraphicFloor;
    private readonly double stratigraphicCeiling;
    private readonly double fluoriteThreshold;

    private readonly double sinStrike;
    private readonly double cosStrike;
    private readonly double tanDip;

    private readonly Manto[] mantos;
    private readonly Chimney[] chimneys;
    private readonly Pod[] pods;

    public int OriginX => originX;
    public int OriginY => originY;
    public int OriginZ => originZ;
    public double Seed => seed;
    public double StrikeDeg => strikeDeg;
    public double DipDeg => dipDeg;
    public int MantoCount => mantos.Length;
    public int ChimneyCount => chimneys.Length;
    public int PodCount => pods.Length;

    private TinSkarnPlan(
        ulong featureId,
        in ProceduralDepositInstance instance,
        double strikeDeg,
        double dipDeg,
        double sourceX,
        double sourceZ,
        double seed,
        double stratigraphicFloor,
        double stratigraphicCeiling,
        double fluoriteThreshold,
        Manto[] mantos,
        Chimney[] chimneys,
        Pod[] pods)
    {
        this.featureId = featureId;
        originX = instance.CenterX;
        originY = instance.CenterY;
        originZ = instance.CenterZ;
        this.strikeDeg = strikeDeg;
        this.dipDeg = dipDeg;
        this.sourceX = sourceX;
        this.sourceZ = sourceZ;
        this.seed = seed;
        this.stratigraphicFloor = stratigraphicFloor;
        this.stratigraphicCeiling = stratigraphicCeiling;
        this.fluoriteThreshold = fluoriteThreshold;
        this.mantos = mantos;
        this.chimneys = chimneys;
        this.pods = pods;

        double strikeRad = strikeDeg * Math.PI / 180.0;
        sinStrike = Math.Sin(strikeRad);
        cosStrike = Math.Cos(strikeRad);
        tanDip = Math.Tan(dipDeg * Math.PI / 180.0);
    }

    /// <summary>
    /// Builds the deposit geometry in deterministic parameter order.
    /// </summary>
    public static TinSkarnPlan Create(
        in ProceduralDepositInstance instance,
        TinSkarnDefinition settings)
    {
        var random = new ProceduralDepositRandom(instance.FeatureId ^ PlanSalt);

        double strikeDeg = random.Range(settings.StrikeDegMin, settings.StrikeDegMax);
        double dipDeg = random.Range(settings.BedDipDegMin, settings.BedDipDegMax);
        double sourceX = random.Range(-settings.SourceSpread, settings.SourceSpread);
        double sourceZ = random.Range(-settings.SourceSpread, settings.SourceSpread);
        double seed = random.Range(0.0, 100.0);

        int count = random.NextInt(settings.MantoCountMin, settings.MantoCountMax);
        double scale = Math.Sqrt(3.0 / count);
        double level = random.Range(settings.MantoFirstLevelMin, settings.MantoFirstLevelMax);

        var mantos = new Manto[count];
        for (int i = 0; i < count; i++)
        {
            double a = (strikeDeg + random.Range(-settings.MantoStrikeJitter, settings.MantoStrikeJitter))
                * Math.PI / 180.0;
            double cx = sourceX + random.Range(-settings.MantoCenterXSpread, settings.MantoCenterXSpread);
            double cz = sourceZ + random.Range(-settings.MantoCenterZSpread, settings.MantoCenterZSpread);
            double mantoLevel = level + i * random.Range(settings.MantoLevelStepMin, settings.MantoLevelStepMax);

            mantos[i] = new Manto(
                cx,
                cz,
                mantoLevel,
                Math.Cos(a),
                Math.Sin(a),
                random.Range(settings.MantoRadiusAlongMin, settings.MantoRadiusAlongMax) * scale,
                random.Range(settings.MantoRadiusAcrossMin, settings.MantoRadiusAcrossMax) * scale,
                random.Range(settings.MantoRadiusVerticalMin, settings.MantoRadiusVerticalMax),
                seed + i * 17.0);
        }

        int chimneyCount = random.NextInt(settings.ChimneyCountMin, settings.ChimneyCountMax);
        var chimneys = new Chimney[chimneyCount];
        var pods = new Pod[chimneyCount];
        for (int i = 0; i < chimneyCount; i++)
        {
            Manto m = mantos[i % mantos.Length];
            double a = (strikeDeg + random.Range(-settings.ChimneyStrikeJitter, settings.ChimneyStrikeJitter))
                * Math.PI / 180.0;
            double dip = random.Range(settings.ChimneyDipDegMin, settings.ChimneyDipDegMax) * Math.PI / 180.0;

            chimneys[i] = new Chimney(
                m.CenterX + random.Range(-settings.ChimneyCenterJitter, settings.ChimneyCenterJitter),
                m.CenterZ + random.Range(-settings.ChimneyCenterJitter, settings.ChimneyCenterJitter),
                m.Level - random.Range(settings.ChimneyDropMin, settings.ChimneyDropMax),
                Math.Cos(a),
                Math.Sin(a),
                Math.Sin(dip),
                Math.Cos(dip),
                random.Range(settings.ChimneyHeightMin, settings.ChimneyHeightMax),
                random.Range(settings.ChimneyRadiusMin, settings.ChimneyRadiusMax),
                seed + 61.0 + i * 19.0);

            pods[i] = new Pod(
                m.CenterX + random.Range(-settings.PodCenterJitter, settings.PodCenterJitter),
                m.Level + random.Range(-settings.PodLevelJitter, settings.PodLevelJitter),
                m.CenterZ + random.Range(-settings.PodCenterJitter, settings.PodCenterJitter),
                random.Range(settings.PodRadiusXMin, settings.PodRadiusXMax),
                random.Range(settings.PodRadiusYMin, settings.PodRadiusYMax),
                random.Range(settings.PodRadiusZMin, settings.PodRadiusZMax),
                seed + 101.0 + i * 23.0);
        }

        return new TinSkarnPlan(
            instance.FeatureId,
            instance,
            strikeDeg,
            dipDeg,
            sourceX,
            sourceZ,
            seed,
            settings.StratigraphicFloor,
            settings.StratigraphicCeiling,
            settings.FluoriteSpeckleThreshold,
            mantos,
            chimneys,
            pods);
    }

    /// <summary><c>stratY</c>: gently dipping carbonate stratigraphy.</summary>
    public double GetStratigraphicY(double x, double y, double z)
    {
        double across = x * sinStrike + z * cosStrike;
        return y - tanDip * across - Math.Sin((x - z) * 0.055 + seed) * 0.6;
    }

    public TinSkarnSample Evaluate(int worldX, int worldY, int worldZ, int surfaceY = int.MaxValue)
    {
        if (worldY > surfaceY) return default;

        double x = worldX - originX;
        double y = worldY - originY;
        double z = worldZ - originZ;
        if (y < BottomY) return default;

        double stratY = GetStratigraphicY(x, y, z);
        if (stratY <= stratigraphicFloor || stratY >= stratigraphicCeiling) return default;

        SkarnPosition pos = GetSkarnPosition(x, y, z);

        // Proximal replacement pods take precedence.
        Pod? bestPodDef = null;
        double bestPodQ = double.PositiveInfinity;
        for (int i = 0; i < pods.Length; i++)
        {
            PodSample s = SamplePod(x, y, z, pods[i]);
            if (s.Inside && s.Q < bestPodQ)
            {
                bestPodQ = s.Q;
                bestPodDef = pods[i];
            }
        }

        if (bestPodDef.HasValue)
        {
            TinSkarnZone zone = PodMaterial(x, y, z, bestPodDef.Value, pos);
            return Finish(zone, x, y, z, false, false, true);
        }

        ChimneySample? bestChimney = null;
        for (int i = 0; i < chimneys.Length; i++)
        {
            ChimneySample s = SampleChimney(x, y, z, chimneys[i]);
            if (s.Inside && (!bestChimney.HasValue || s.Score < bestChimney.Value.Score))
            {
                bestChimney = s;
            }
        }

        if (bestChimney.HasValue)
        {
            TinSkarnZone zone = ChimneyMaterial(bestChimney.Value, pos);
            return Finish(zone, x, y, z, false, true, false);
        }

        MantoSample? bestManto = null;
        bool near = false;
        for (int i = 0; i < mantos.Length; i++)
        {
            MantoSample s = SampleManto(x, y, z, mantos[i]);
            if (s.Inside && (!bestManto.HasValue || s.R < bestManto.Value.R)) bestManto = s;
            if (s.Halo) near = true;
        }

        if (bestManto.HasValue)
        {
            TinSkarnZone zone = MantoMaterial(bestManto.Value, pos);
            return Finish(zone, x, y, z, true, false, false);
        }

        for (int i = 0; i < chimneys.Length && !near; i++)
        {
            if (SampleChimney(x, y, z, chimneys[i]).Halo) near = true;
        }

        for (int i = 0; i < pods.Length && !near; i++)
        {
            if (SamplePod(x, y, z, pods[i]).Halo) near = true;
        }

        if (near)
        {
            TinSkarnZone zone = pos.Field > 0.5 ? TinSkarnZone.None : TinSkarnZone.Fluorite;
            return Finish(zone, x, y, z, false, false, false);
        }

        return default;
    }

    /// <summary>
    /// Applies fluorite speckling and deterministic ore grades.
    /// </summary>
    private TinSkarnSample Finish(
        TinSkarnZone zone,
        double x,
        double y,
        double z,
        bool inManto,
        bool inChimney,
        bool inPod)
    {
        bool inFluoriteCarbonate = zone == TinSkarnZone.Fluorite;
        double speckle = 0.0;
        if (inFluoriteCarbonate)
        {
            speckle = Hash3D(x * 2.1 + seed, y * 2.1, z * 2.1);
            if (speckle < fluoriteThreshold) zone = TinSkarnZone.None;
        }

        int grade = 0;
        if (IsGraded(zone))
        {
            double grainNoise = Hash3D(x * 2.7 + seed * 1.3, y * 2.7, z * 2.7);
            grade = grainNoise > 0.82 ? 3 : (grainNoise > 0.55 ? 2 : (grainNoise > 0.25 ? 1 : 0));
        }

        return new TinSkarnSample(zone, grade, inManto, inChimney, inPod, inFluoriteCarbonate, speckle);
    }

    internal static bool IsGraded(TinSkarnZone zone)
    {
        return zone == TinSkarnZone.Cassiterite
            || zone == TinSkarnZone.Scheelite
            || zone == TinSkarnZone.Sphalerite
            || zone == TinSkarnZone.Chalcopyrite
            || zone == TinSkarnZone.Galena;
    }

    /// <summary><c>skarnPosition</c>: distance from the buried intrusive source.</summary>
    private SkarnPosition GetSkarnPosition(double x, double y, double z)
    {
        double dx = x - sourceX;
        double dz = z - sourceZ;
        double planar = Math.Sqrt(dx * dx + dz * dz);
        double sourceDistance = Math.Sqrt(dx * dx + dz * dz + Math.Pow((y + 24.0) * 0.42, 2.0));
        return new SkarnPosition(
            planar,
            Clamp(1.0 - sourceDistance / 43.0, 0.0, 1.0),
            Math.Sin(x * 0.13 - z * 0.09 + seed) + 0.45 * Math.Cos(y * 0.16 + x * 0.06 - seed * 0.3));
    }

    /// <summary><c>sampleManto</c>: warped triaxial bedding-parallel replacement body.</summary>
    private MantoSample SampleManto(double x, double y, double z, in Manto m)
    {
        double dx = x - m.CenterX;
        double dz = z - m.CenterZ;
        double u = (dx * m.CosAngle - dz * m.SinAngle) / m.RadiusAlong;
        double v = (dx * m.SinAngle + dz * m.CosAngle) / m.RadiusAcross;
        double w = (GetStratigraphicY(x, y, z) - m.Level) / m.RadiusVertical;
        double warp = Math.Sin(u * 4.0 + m.Phase) * Math.Cos(v * 3.2) * 0.1
            + 0.04 * Math.Sin((u - v) * 7.0 - m.Phase);
        double r = Math.Sqrt(u * u + v * v + w * w) + warp;
        return new MantoSample(r <= 1.02, r <= 1.25, r, Math.Sqrt(u * u + v * v), u, v);
    }

    /// <summary><c>sampleChimney</c>: steep tapering feeder pipe.</summary>
    private static ChimneySample SampleChimney(double x, double y, double z, in Chimney c)
    {
        double dy = y - c.CenterY;
        if (Math.Abs(dy) > c.Height) return new ChimneySample(false, false, double.PositiveInfinity, 0.0);

        double t = dy * c.CosDip;
        double cx = c.CenterX + c.CosAngle * t;
        double cz = c.CenterZ - c.SinAngle * t;
        double dx = x - cx;
        double dz = z - cz;
        double d = Math.Sqrt(dx * dx + dz * dz);
        double taper = Math.Sqrt(Clamp(1.0 - dy / c.Height * (dy / c.Height), 0.0, 1.0));
        double radius = Math.Max(0.35, c.Radius * taper * (0.82 + 0.18 * Math.Sin(y * 0.22 + c.Phase)));
        return new ChimneySample(
            d <= radius,
            d <= radius + 2.2 * taper,
            d / Math.Max(0.2, radius),
            dy / c.Height);
    }

    /// <summary><c>samplePod</c>: warped ellipsoidal proximal replacement pod.</summary>
    private static PodSample SamplePod(double x, double y, double z, in Pod p)
    {
        double dx = (x - p.CenterX) / p.RadiusX;
        double dy = (y - p.CenterY) / p.RadiusY;
        double dz = (z - p.CenterZ) / p.RadiusZ;
        double q = dx * dx + dy * dy + dz * dz
            + 0.09 * Math.Sin((x - p.CenterX) * 0.7 + p.Phase) * Math.Cos((z - p.CenterZ) * 0.6);
        return new PodSample(q <= 1.0, q <= 1.45, q);
    }

    /// <summary><c>podMaterial</c>: replacement front across the pod.</summary>
    private static TinSkarnZone PodMaterial(double x, double y, double z, in Pod p, in SkarnPosition pos)
    {
        double dx = (x - p.CenterX) / p.RadiusX;
        double dz = (z - p.CenterZ) / p.RadiusZ;
        double front = dx * 0.72 + dz * 0.42 + 0.22 * Math.Sin(y * 0.35 + p.Phase);
        double core = Math.Sqrt(dx * dx + dz * dz);

        if (front < -0.28 && core < 0.88) return TinSkarnZone.Cassiterite;
        if (front < 0.12 && core < 0.94)
        {
            return pos.Field > 0.35 ? TinSkarnZone.Chalcopyrite : TinSkarnZone.IronSulfides;
        }

        if (front < 0.48)
        {
            return pos.Field > 0.55 ? TinSkarnZone.Sphalerite : TinSkarnZone.None;
        }

        return pos.Field > 0.72 ? TinSkarnZone.Galena : TinSkarnZone.Fluorite;
    }

    /// <summary>Model <c>chimneyMaterial</c>.</summary>
    private static TinSkarnZone ChimneyMaterial(in ChimneySample s, in SkarnPosition pos)
    {
        if (s.Score < 0.34 && pos.Field > -0.35) return TinSkarnZone.Cassiterite;
        if (s.Score < 0.62)
        {
            return pos.Field > 0.42 ? TinSkarnZone.Chalcopyrite : TinSkarnZone.IronSulfides;
        }

        if (s.Vertical > 0.18 && pos.Field > 0.2) return TinSkarnZone.Sphalerite;

        return pos.Field < -0.48 ? TinSkarnZone.Fluorite : TinSkarnZone.None;
    }

    /// <summary><c>mantoMaterial</c>: proximal-to-distal replacement zoning.</summary>
    private TinSkarnZone MantoMaterial(in MantoSample s, in SkarnPosition pos)
    {
        double edge = s.Lateral;
        double front = 0.42 * (1.0 - pos.Proximity)
            + 0.68 * edge
            + 0.1 * Math.Sin(s.U * 5.0 + s.V * 3.0 + seed);

        if (pos.Proximity > 0.62 && edge < 0.48)
        {
            if (pos.Field > 1.15 && edge > 0.3) return TinSkarnZone.Scheelite;

            return TinSkarnZone.None;
        }

        if (front < 0.58)
        {
            return pos.Field > 0.28 ? TinSkarnZone.Cassiterite : TinSkarnZone.None;
        }

        if (front < 0.78)
        {
            if (pos.Field > 0.72) return TinSkarnZone.Cassiterite;
            if (pos.Field > -0.35) return TinSkarnZone.IronSulfides;
            return TinSkarnZone.None;
        }

        if (front < 0.98)
        {
            return pos.Field > -0.12 ? TinSkarnZone.Sphalerite : TinSkarnZone.IronSulfides;
        }

        return pos.Field > 0.05 ? TinSkarnZone.Galena : TinSkarnZone.Fluorite;
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

    private readonly record struct SkarnPosition(double Planar, double Proximity, double Field);

    private readonly record struct MantoSample(
        bool Inside,
        bool Halo,
        double R,
        double Lateral,
        double U,
        double V);

    private readonly record struct ChimneySample(bool Inside, bool Halo, double Score, double Vertical);

    private readonly record struct PodSample(bool Inside, bool Halo, double Q);

    private readonly record struct Manto(
        double CenterX,
        double CenterZ,
        double Level,
        double CosAngle,
        double SinAngle,
        double RadiusAlong,
        double RadiusAcross,
        double RadiusVertical,
        double Phase);

    private readonly record struct Chimney(
        double CenterX,
        double CenterZ,
        double CenterY,
        double CosAngle,
        double SinAngle,
        double SinDip,
        double CosDip,
        double Height,
        double Radius,
        double Phase);

    private readonly record struct Pod(
        double CenterX,
        double CenterY,
        double CenterZ,
        double RadiusX,
        double RadiusY,
        double RadiusZ,
        double Phase);
}

internal sealed class TinSkarnProceduralTemplate : IProceduralDepositTemplate
{
    private static readonly string[] RequiredSlots =
    {
        ProceduralMaterialSlots.Cassiterite,
        ProceduralMaterialSlots.Scheelite,
        ProceduralMaterialSlots.Sphalerite,
        ProceduralMaterialSlots.Chalcopyrite,
        ProceduralMaterialSlots.Galena,
        ProceduralMaterialSlots.Pyrite,
        ProceduralMaterialSlots.Fluorite
    };

    public string Code => "tinSkarn";
    public IReadOnlyList<string> RequiredMaterialSlots => RequiredSlots;
    public bool RequiresPlan => true;

    public int GetMaximumHorizontalReach(ProceduralDepositDefinition definition)
    {
        return definition.TinSkarn.HorizontalRadius;
    }

    public object CreatePlan(in ProceduralDepositInstance instance, ProceduralDepositDefinition definition)
    {
        return TinSkarnPlan.Create(instance, definition.TinSkarn);
    }

    public bool Validate(ProceduralDepositDefinition definition, out string error)
    {
        TinSkarnDefinition settings = definition.TinSkarn;
        bool valid = settings.HorizontalRadius >= 16
            && settings.VerticalHalfHeight >= 8
            && settings.MantoCountMin >= 1
            && settings.MantoCountMax >= settings.MantoCountMin
            && settings.ChimneyCountMin >= 1
            && settings.ChimneyCountMax >= settings.ChimneyCountMin
            && settings.StrikeDegMax >= settings.StrikeDegMin
            && settings.BedDipDegMax >= settings.BedDipDegMin
            && settings.MantoFirstLevelMax >= settings.MantoFirstLevelMin
            && settings.MantoLevelStepMin > 0.0
            && settings.MantoLevelStepMax >= settings.MantoLevelStepMin
            && settings.MantoRadiusAlongMin > 0.0
            && settings.MantoRadiusAlongMax >= settings.MantoRadiusAlongMin
            && settings.MantoRadiusAcrossMin > 0.0
            && settings.MantoRadiusAcrossMax >= settings.MantoRadiusAcrossMin
            && settings.MantoRadiusVerticalMin > 0.0
            && settings.MantoRadiusVerticalMax >= settings.MantoRadiusVerticalMin
            && settings.ChimneyDipDegMax >= settings.ChimneyDipDegMin
            && settings.ChimneyDropMax >= settings.ChimneyDropMin
            && settings.ChimneyHeightMin > 0.0
            && settings.ChimneyHeightMax >= settings.ChimneyHeightMin
            && settings.ChimneyRadiusMin > 0.0
            && settings.ChimneyRadiusMax >= settings.ChimneyRadiusMin
            && settings.PodRadiusXMin > 0.0
            && settings.PodRadiusXMax >= settings.PodRadiusXMin
            && settings.PodRadiusYMin > 0.0
            && settings.PodRadiusYMax >= settings.PodRadiusYMin
            && settings.PodRadiusZMin > 0.0
            && settings.PodRadiusZMax >= settings.PodRadiusZMin
            && settings.StratigraphicCeiling > settings.StratigraphicFloor
            && settings.FluoriteSpeckleThreshold is >= 0.0 and <= 1.0;
        error = valid ? string.Empty : "invalid tin skarn settings";
        return valid;
    }

    public void Realize(
        ProceduralDepositWorldGenSystem system,
        in DepositCandidate candidate,
        IChunkColumnGenerateRequest request,
        int baseX,
        int baseZ)
    {
        system.RealizeTinSkarnCandidate(candidate, request, baseX, baseZ);
    }
}

public sealed partial class ProceduralDepositWorldGenSystem
{
    internal void RealizeTinSkarnCandidate(
        DepositCandidate candidate,
        IChunkColumnGenerateRequest request,
        int baseX,
        int baseZ)
    {
        CompiledProceduralDeposit compiled = candidate.Compiled;
        TinSkarnDefinition settings = compiled.Definition.TinSkarn;
        var plan = (TinSkarnPlan)candidate.Plan!;
        ushort[] heightMap = request.Chunks[0].MapChunk.WorldGenTerrainHeightMap;
        int worldHeight = serverApi!.World.BlockAccessor.MapSizeY;
        int minimumY = GameMath.Clamp(candidate.Instance.CenterY - settings.VerticalHalfHeight, 1, worldHeight - 2);
        int maximumPrototypeY = GameMath.Clamp(candidate.Instance.CenterY + settings.VerticalHalfHeight, 1, worldHeight - 2);

        int[] zoneSlots = BuildTinSkarnZoneSlots(compiled);

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
                    TinSkarnSample sample = plan.Evaluate(worldX, y, worldZ, surfaceY);
                    if (sample.Zone == TinSkarnZone.None) continue;

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

    private static int[] BuildTinSkarnZoneSlots(CompiledProceduralDeposit compiled)
    {
        var slots = new int[Enum.GetValues<TinSkarnZone>().Length];
        Array.Fill(slots, -1);
        slots[(int)TinSkarnZone.Cassiterite] = compiled.GetSlotId(ProceduralMaterialSlots.Cassiterite);
        slots[(int)TinSkarnZone.Scheelite] = compiled.GetSlotId(ProceduralMaterialSlots.Scheelite);
        slots[(int)TinSkarnZone.Sphalerite] = compiled.GetSlotId(ProceduralMaterialSlots.Sphalerite);
        slots[(int)TinSkarnZone.Chalcopyrite] = compiled.GetSlotId(ProceduralMaterialSlots.Chalcopyrite);
        slots[(int)TinSkarnZone.Galena] = compiled.GetSlotId(ProceduralMaterialSlots.Galena);
        slots[(int)TinSkarnZone.IronSulfides] = compiled.GetSlotId(ProceduralMaterialSlots.Pyrite);
        slots[(int)TinSkarnZone.Fluorite] = compiled.GetSlotId(ProceduralMaterialSlots.Fluorite);
        return slots;
    }
}
