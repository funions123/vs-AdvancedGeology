using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace AdvancedGeology.WorldGen.ProceduralDeposits;

public enum GreisenDepositFamily
{
    CornishTin,
    MolybdenumBismuth,
    RareMetal
}

public enum GreisenStockworkZone
{
    None = 0,
    Cassiterite,
    Wolframite,
    Chalcopyrite,
    Arsenopyrite,
    Sphalerite,
    Galena,
    Molybdenite,
    Bismuthinite,
    NativeBismuth,
    Pyrite,
    Columbite,
    Tantalite,
    Microlite,
    Lepidolite,
    Spodumene,
    Pollucite,
    Beryl,
    Topaz,
    Tourmaline,
    Fluorite,
    Albite,
    Quartz,
    Breccia,

    // Surface weathering hooks (as enums only; Main will wire weathering)
    Gossan,
    Kaolinite
}

[JsonObject(MemberSerialization.OptIn)]
public sealed class GreisenStockworkDefinition
{
    [JsonProperty]
    public GreisenDepositFamily Family { get; set; } = GreisenDepositFamily.CornishTin;

    [JsonProperty]
    public int HorizontalRadius { get; set; } = 48;

    [JsonProperty]
    public int VerticalHalfHeight { get; set; } = 36;

    [JsonProperty]
    public double CupolaRadiusX { get; set; } = 30.0;

    [JsonProperty]
    public double CupolaRadiusZ { get; set; } = 26.0;

    [JsonProperty]
    public double CupolaHeight { get; set; } = 21.0;

    [JsonProperty]
    public double LodeThickness { get; set; } = 3.3;

    [JsonProperty]
    public int VeinMin { get; set; } = 6;

    [JsonProperty]
    public int VeinMax { get; set; } = 9;

    [JsonProperty]
    public int PodMin { get; set; } = 5;

    [JsonProperty]
    public int PodMax { get; set; } = 7;

    [JsonProperty]
    public double MainStrikeDeg { get; set; } = 27.0;

    [JsonProperty]
    public double MainDipDeg { get; set; } = 76.0;

    [JsonProperty]
    public double CrossStrikeDeg { get; set; } = -54.0;

    [JsonProperty]
    public double CrossDipDeg { get; set; } = 68.0;

    [JsonProperty]
    public int WeatheringDepthMax { get; set; } = 4;
}

public readonly record struct GreisenStockworkSample(
    bool InsideVein,
    bool InHalo,
    bool InCap,
    bool InPod,
    int InsideVeinCount,
    double BestVeinScore,
    double CenterRatio,
    double DistanceAboveGranite,
    double LocalY);

/// <summary>
/// Cassiterite, wolframite, sulfides, rare-metal minerals, quartz, albite, topaz, tourmaline, fluorite, and breccia occupy granite cupolas, lodes, stockworks, and replacement pods.
/// </summary>
internal sealed class GreisenStockworkPlan
{
    private const ulong PlanSalt = 0x4752454953454E53UL;

    private readonly ulong featureId;
    private readonly int originX;
    private readonly int originY;
    private readonly int originZ;
    private readonly GreisenDepositFamily family;
    private readonly double seed;
    private readonly double cupolaRadiusX;
    private readonly double cupolaRadiusZ;
    private readonly double cupolaHeight;
    private readonly double baseGraniteRoof;
    private readonly GreisenSheet[] mainVeins;
    private readonly GreisenSheet[] crosscourses;
    private readonly GreisenPod[] replacementPods;

    public GreisenDepositFamily Family => family;
    public int OriginX => originX;
    public int OriginY => originY;
    public int OriginZ => originZ;
    public double Seed => seed;
    public int MainVeinCount => mainVeins.Length;
    public int CrosscourseCount => crosscourses.Length;
    public int PodCount => replacementPods.Length;

    private GreisenStockworkPlan(
        ulong featureId,
        in ProceduralDepositInstance instance,
        GreisenDepositFamily family,
        double seed,
        double cupolaRadiusX,
        double cupolaRadiusZ,
        double cupolaHeight,
        double baseGraniteRoof,
        GreisenSheet[] mainVeins,
        GreisenSheet[] crosscourses,
        GreisenPod[] replacementPods)
    {
        this.featureId = featureId;
        originX = instance.CenterX;
        originY = instance.CenterY;
        originZ = instance.CenterZ;
        this.family = family;
        this.seed = seed;
        this.cupolaRadiusX = cupolaRadiusX;
        this.cupolaRadiusZ = cupolaRadiusZ;
        this.cupolaHeight = cupolaHeight;
        this.baseGraniteRoof = baseGraniteRoof;
        this.mainVeins = mainVeins;
        this.crosscourses = crosscourses;
        this.replacementPods = replacementPods;
    }

    public static GreisenStockworkPlan Create(
        in ProceduralDepositInstance instance,
        GreisenStockworkDefinition settings)
    {
        var random = new ProceduralDepositRandom(instance.FeatureId ^ PlanSalt);
        double seed = random.Range(0.0, 1000.0);

        return settings.Family switch
        {
            GreisenDepositFamily.CornishTin => CreateCornishTinPlan(instance, settings, random, seed),
            GreisenDepositFamily.MolybdenumBismuth => CreateMoBiPlan(instance, settings, random, seed),
            GreisenDepositFamily.RareMetal => CreateRareMetalPlan(instance, settings, random, seed),
            _ => CreateCornishTinPlan(instance, settings, random, seed)
        };
    }

    private static GreisenStockworkPlan CreateCornishTinPlan(
        in ProceduralDepositInstance instance,
        GreisenStockworkDefinition settings,
        ProceduralDepositRandom random,
        double seed)
    {
        double mainStrike = settings.MainStrikeDeg + random.Range(-7.0, 7.0);
        double mainDip = settings.MainDipDeg + random.Range(-5.0, 5.0);
        double crossStrike = settings.CrossStrikeDeg + random.Range(-9.0, 9.0);
        double crossDip = settings.CrossDipDeg + random.Range(-6.0, 6.0);
        double lodeThickness = settings.LodeThickness * random.Range(0.85, 1.15);
        double cupolaHeight = settings.CupolaHeight * random.Range(0.9, 1.1);

        int principalCount = 5;
        int splayCount = 2;
        var sheets = new List<GreisenSheet>(principalCount + splayCount);

        double offset = random.Range(-26.0, -23.0);
        for (int i = 0; i < principalCount; i++)
        {
            if (i > 0) offset += random.Range(8.0, 14.0);
            sheets.Add(GreisenSheet.Create(
                mainStrike + random.Range(-7.0, 7.0),
                mainDip + random.Range(-6.0, 6.0),
                offset,
                random.Range(-4.0, 4.0),
                random.Range(-5.0, 1.0),
                random.Range(34.0, 45.0),
                random.Range(29.0, 37.0),
                lodeThickness * random.Range(0.78, 1.22),
                seed + i * 11.7,
                random.Range(-13.0, 13.0),
                random.Range(3.0, 6.0),
                random.Range(-1.8, 1.8),
                seed * 0.7 + i * 4.3,
                1.5,
                0.0));
        }

        // Short oblique splays modeling en-echelon branches
        for (int i = 0; i < splayCount; i++)
        {
            GreisenSheet parent = sheets[i + 2];
            double direction = i == 0 ? -1.0 : 1.0;
            sheets.Add(GreisenSheet.Create(
                parent.StrikeDeg + direction * random.Range(12.0, 21.0),
                parent.DipDeg + random.Range(-8.0, 8.0),
                parent.Offset + direction * random.Range(3.0, 6.0),
                parent.CenterAlong + direction * random.Range(7.0, 14.0),
                parent.CenterY + random.Range(2.0, 7.0),
                random.Range(14.0, 23.0),
                random.Range(14.0, 21.0),
                lodeThickness * random.Range(0.68, 0.92),
                seed + 67.0 + i * 15.0,
                random.Range(-6.0, 6.0),
                random.Range(2.5, 4.5),
                random.Range(-1.0, 1.0),
                seed + 19.0 + i * 5.0,
                1.5,
                0.0));
        }

        // Later crosscourses (faults with throw)
        int crossCount = 3;
        var crosses = new List<GreisenSheet>(crossCount);
        double crossOffset = random.Range(-23.0, -19.0);
        for (int i = 0; i < crossCount; i++)
        {
            if (i > 0) crossOffset += random.Range(15.0, 22.0);
            crosses.Add(GreisenSheet.Create(
                crossStrike + random.Range(-10.0, 10.0),
                crossDip + random.Range(-9.0, 9.0),
                crossOffset,
                random.Range(-6.0, 6.0),
                random.Range(7.0, 12.0),
                random.Range(31.0, 42.0),
                random.Range(19.0, 27.0),
                random.Range(1.8, 2.7),
                seed + 101.0 + i * 17.0,
                random.Range(-10.0, 10.0),
                random.Range(3.0, 6.0),
                random.Range(-1.2, 1.2),
                seed + 37.0 + i * 6.0,
                1.5,
                random.Range(-1.8, 1.8)));
        }

        return new GreisenStockworkPlan(
            instance.FeatureId,
            instance,
            GreisenDepositFamily.CornishTin,
            seed,
            34.0,
            30.0,
            cupolaHeight,
            -20.0,
            sheets.ToArray(),
            crosses.ToArray(),
            Array.Empty<GreisenPod>());
    }

    private static GreisenStockworkPlan CreateMoBiPlan(
        in ProceduralDepositInstance instance,
        GreisenStockworkDefinition settings,
        ProceduralDepositRandom random,
        double seed)
    {
        double strikeDeg = settings.MainStrikeDeg + random.Range(-10.0, 15.0);
        double cupolaHeight = random.Range(19.0, 24.0);
        double radiusX = random.Range(26.0, 33.0);
        double radiusZ = random.Range(22.0, 29.0);
        int veinCount = random.NextInt(settings.VeinMin, settings.VeinMax);

        var veins = new GreisenSheet[veinCount];
        for (int i = 0; i < veinCount; i++)
        {
            double angle = i * Math.PI * 2.0 / veinCount + random.Range(-0.30, 0.30);
            double radial = random.Range(4.0, 21.0);
            double centerX = Math.Cos(angle) * radial;
            double centerZ = Math.Sin(angle) * radial;
            double vStrike = strikeDeg + random.Range(-50.0, 50.0);
            double vDip = random.Range(58.0, 84.0);

            double strikeRad = vStrike * Math.PI / 180.0;
            double along = centerX * Math.Cos(strikeRad) - centerZ * Math.Sin(strikeRad);
            double across = centerX * Math.Sin(strikeRad) + centerZ * Math.Cos(strikeRad);

            veins[i] = GreisenSheet.Create(
                vStrike,
                vDip,
                across,
                along,
                random.Range(-4.0, 5.0),
                random.Range(15.0, 29.0),
                random.Range(18.0, 30.0),
                random.Range(1.2, 2.8),
                seed + 31.0 + i * 17.0,
                random.Range(-8.0, 8.0),
                random.Range(2.5, 5.0),
                random.Range(-1.2, 1.2),
                seed + 11.0 + i * 5.0,
                random.Range(0.8, 1.9),
                0.0);
        }

        return new GreisenStockworkPlan(
            instance.FeatureId,
            instance,
            GreisenDepositFamily.MolybdenumBismuth,
            seed,
            radiusX,
            radiusZ,
            cupolaHeight,
            -21.0,
            veins,
            Array.Empty<GreisenSheet>(),
            Array.Empty<GreisenPod>());
    }

    private static GreisenStockworkPlan CreateRareMetalPlan(
        in ProceduralDepositInstance instance,
        GreisenStockworkDefinition settings,
        ProceduralDepositRandom random,
        double seed)
    {
        double strikeDeg = settings.MainStrikeDeg + random.Range(-10.0, 15.0);
        double cupolaHeight = random.Range(19.0, 24.0);
        double radiusX = random.Range(26.0, 33.0);
        double radiusZ = random.Range(22.0, 28.0);
        int veinCount = random.NextInt(settings.VeinMin, settings.VeinMax);
        int podCount = random.NextInt(settings.PodMin, settings.PodMax);

        var veins = new GreisenSheet[veinCount];
        for (int i = 0; i < veinCount; i++)
        {
            double angle = i * Math.PI * 2.0 / veinCount + random.Range(-0.35, 0.35);
            double radial = random.Range(4.0, 19.0);
            double centerX = Math.Cos(angle) * radial;
            double centerZ = Math.Sin(angle) * radial;
            double vStrike = strikeDeg + random.Range(-50.0, 50.0);
            double vDip = random.Range(58.0, 84.0);

            double strikeRad = vStrike * Math.PI / 180.0;
            double along = centerX * Math.Cos(strikeRad) - centerZ * Math.Sin(strikeRad);
            double across = centerX * Math.Sin(strikeRad) + centerZ * Math.Cos(strikeRad);

            veins[i] = GreisenSheet.Create(
                vStrike,
                vDip,
                across,
                along,
                random.Range(-4.0, 5.0),
                random.Range(16.0, 30.0),
                random.Range(18.0, 30.0),
                random.Range(1.2, 2.8),
                seed + 31.0 + i * 17.0,
                random.Range(-8.0, 8.0),
                random.Range(2.5, 5.0),
                random.Range(-1.2, 1.2),
                seed + 11.0 + i * 5.0,
                random.Range(0.8, 1.8),
                0.0);
        }

        var pods = new GreisenPod[podCount];
        for (int i = 0; i < podCount; i++)
        {
            double podAngle = (i * Math.PI * 2.0) / podCount + random.Range(-0.4, 0.4);
            double podDist = random.Range(4.0, 16.0);
            double px = Math.Cos(podAngle) * podDist;
            double pz = Math.Sin(podAngle) * podDist;

            double radialNorm = (px * px) / (radiusX * radiusX) + (pz * pz) / (radiusZ * radiusZ);
            double roofWarp = Math.Sin(px * 0.09 + seed) * 1.4 + Math.Cos(pz * 0.07 - seed * 0.6) * 1.0;
            double roofAtPod = -22.0 + cupolaHeight * Math.Exp(-1.30 * radialNorm) + roofWarp;
            double py = roofAtPod + random.Range(-4.0, 1.0);

            pods[i] = new GreisenPod(
                px,
                py,
                pz,
                random.Range(3.5, 6.0),
                random.Range(2.5, 4.5),
                random.Range(3.5, 6.0),
                i % 3,
                random.Range(0, Math.PI));
        }

        return new GreisenStockworkPlan(
            instance.FeatureId,
            instance,
            GreisenDepositFamily.RareMetal,
            seed,
            radiusX,
            radiusZ,
            cupolaHeight,
            -22.0,
            veins,
            Array.Empty<GreisenSheet>(),
            pods);
    }

    public double GetGraniteRoof(double x, double z)
    {
        double radial = (x * x) / (cupolaRadiusX * cupolaRadiusX) + (z * z) / (cupolaRadiusZ * cupolaRadiusZ);
        return family switch
        {
            GreisenDepositFamily.CornishTin =>
                baseGraniteRoof + cupolaHeight * Math.Exp(-1.35 * radial)
                + Math.Sin(x * 0.09 + z * 0.065 + seed) * 1.5,

            GreisenDepositFamily.MolybdenumBismuth =>
                baseGraniteRoof + cupolaHeight * Math.Exp(-1.35 * radial)
                + Math.Sin(x * 0.09 + seed) * 1.5
                + Math.Cos(z * 0.07 - seed * 0.6) * 1.1,

            GreisenDepositFamily.RareMetal =>
                baseGraniteRoof + cupolaHeight * Math.Exp(-1.30 * radial)
                + Math.Sin(x * 0.09 + seed) * 1.4
                + Math.Cos(z * 0.07 - seed * 0.6) * 1.0,

            _ => baseGraniteRoof
        };
    }

    public GreisenStockworkSample Sample(int worldX, int worldY, int worldZ)
    {
        double x = worldX - originX;
        double y = worldY - originY;
        double z = worldZ - originZ;

        double graniteRoof = GetGraniteRoof(x, z);
        double distanceAboveGranite = y - graniteRoof;
        double radial = (x * x) / (cupolaRadiusX * cupolaRadiusX) + (z * z) / (cupolaRadiusZ * cupolaRadiusZ);

        bool inCap = family switch
        {
            GreisenDepositFamily.MolybdenumBismuth =>
                radial <= 1.10 && y >= graniteRoof - 5.5 && y <= graniteRoof + 4.5,
            GreisenDepositFamily.RareMetal =>
                radial <= 1.15 && y >= graniteRoof - 6.0 && y <= graniteRoof + 4.0,
            _ => false
        };

        bool inPod = false;
        foreach (GreisenPod pod in replacementPods)
        {
            if (pod.Contains(x, y, z))
            {
                inPod = true;
                break;
            }
        }

        double cumulativeThrow = 0.0;
        double bestCrossScore = double.PositiveInfinity;
        bool inCrossHalo = false;
        foreach (GreisenSheet cross in crosscourses)
        {
            SheetSample cs = cross.Sample(x, y, z, 0.0);
            if (cs.SignedDistance > 0.0) cumulativeThrow += cross.ThrowAmount;
            if (cs.Score < bestCrossScore) bestCrossScore = cs.Score;
            if (cs.InHalo) inCrossHalo = true;
        }

        double bestVeinScore = double.PositiveInfinity;
        double bestCenterRatio = double.PositiveInfinity;
        bool insideVein = false;
        bool inHalo = inCrossHalo;
        int insideVeinCount = 0;

        foreach (GreisenSheet vein in mainVeins)
        {
            SheetSample vs = vein.Sample(x, y, z, cumulativeThrow);
            if (vs.Inside) insideVeinCount++;
            if (vs.Score < bestVeinScore)
            {
                bestVeinScore = vs.Score;
                bestCenterRatio = vs.CenterRatio;
            }
            if (vs.Inside) insideVein = true;
            if (vs.InHalo) inHalo = true;
        }

        return new GreisenStockworkSample(
            insideVein,
            inHalo,
            inCap,
            inPod,
            insideVeinCount,
            bestVeinScore,
            bestCenterRatio,
            distanceAboveGranite,
            y);
    }

    public GreisenStockworkZone EvaluateZone(int worldX, int worldY, int worldZ, int surfaceY = int.MaxValue)
    {
        double x = worldX - originX;
        double y = worldY - originY;
        double z = worldZ - originZ;

        int depth = surfaceY < int.MaxValue ? surfaceY - worldY : 999;
        GreisenStockworkZone rawZone = family switch
        {
            GreisenDepositFamily.CornishTin => EvaluateCornishTin(x, y, z),
            GreisenDepositFamily.MolybdenumBismuth => EvaluateMoBi(x, y, z),
            GreisenDepositFamily.RareMetal => EvaluateRareMetal(x, y, z),
            _ => GreisenStockworkZone.None
        };

        return ApplyWeatheringHook(rawZone, depth);
    }

    private GreisenStockworkZone EvaluateCornishTin(double x, double y, double z)
    {
        double graniteRoof = GetGraniteRoof(x, z);
        bool isGranite = y <= graniteRoof;
        double distanceAboveGranite = y - graniteRoof;

        SheetSample? bestCross = null;
        double cumulativeThrow = 0.0;
        foreach (GreisenSheet cross in crosscourses)
        {
            SheetSample sample = cross.Sample(x, y, z, 0.0);
            if (bestCross == null || sample.Score < bestCross.Value.Score) bestCross = sample;
            if (sample.SignedDistance > 0.0) cumulativeThrow += cross.ThrowAmount;
        }

        SheetSample? bestMain = null;
        foreach (GreisenSheet sheet in mainVeins)
        {
            SheetSample sample = sheet.Sample(x, y, z, cumulativeThrow);
            if (bestMain == null || sample.Score < bestMain.Value.Score) bestMain = sample;
        }

        double grain = Hash3D(x * 2.15 + seed, y * 2.15, z * 2.15);
        double band = Math.Abs(Math.Sin(x * 0.48 + z * 0.31 + y * 0.20 + seed));

        // Crosscourses cut and offset older main lodes
        if (bestCross.HasValue && bestCross.Value.Inside && distanceAboveGranite > 7.0)
        {
            if (grain > 0.76) return GreisenStockworkZone.Galena;
            if (grain > 0.48) return GreisenStockworkZone.Sphalerite;
            if (grain > 0.32) return GreisenStockworkZone.Fluorite;
            return GreisenStockworkZone.Quartz;
        }

        // Main lodes
        if (bestMain.HasValue && bestMain.Value.Inside)
        {
            if (distanceAboveGranite <= 4.0)
            {
                if (grain > 0.62) return GreisenStockworkZone.Cassiterite;
                if (grain > 0.48) return GreisenStockworkZone.Wolframite;
                if (band < 0.22) return GreisenStockworkZone.Tourmaline;
                return GreisenStockworkZone.Quartz;
            }

            if (distanceAboveGranite <= 17.0)
            {
                if (grain > 0.74) return GreisenStockworkZone.Cassiterite;
                if (grain > 0.48) return GreisenStockworkZone.Chalcopyrite;
                if (grain > 0.36) return GreisenStockworkZone.Cassiterite; // stannite -> cassiterite
                if (grain > 0.22) return GreisenStockworkZone.Arsenopyrite;
                return band < 0.30 ? GreisenStockworkZone.Tourmaline : GreisenStockworkZone.Quartz;
            }

            if (grain > 0.76) return GreisenStockworkZone.Sphalerite;
            if (grain > 0.60) return GreisenStockworkZone.Galena;
            if (grain > 0.48) return GreisenStockworkZone.Arsenopyrite;
            return GreisenStockworkZone.Quartz;
        }

        return GreisenStockworkZone.None;
    }

    private GreisenStockworkZone EvaluateMoBi(double x, double y, double z)
    {
        double graniteRoof = GetGraniteRoof(x, z);
        double radial = (x * x) / (cupolaRadiusX * cupolaRadiusX) + (z * z) / (cupolaRadiusZ * cupolaRadiusZ);
        bool inCap = radial <= 1.10 && y >= graniteRoof - 5.5 && y <= graniteRoof + 4.5;
        double grain = Hash3D(x * 1.9 + seed, y * 1.9, z * 1.9);

        SheetSample? bestVein = null;
        foreach (GreisenSheet vein in mainVeins)
        {
            SheetSample sample = vein.Sample(x, y, z, 0.0);
            if (sample.Score < double.PositiveInfinity && (bestVein == null || sample.Score < bestVein.Value.Score))
            {
                bestVein = sample;
            }
        }

        // Endogreisen apical cap replacement
        if (inCap)
        {
            if (grain > 0.92) return GreisenStockworkZone.Molybdenite;
            if (grain > 0.86) return GreisenStockworkZone.Bismuthinite;
            if (grain > 0.81) return GreisenStockworkZone.NativeBismuth;
            if (grain > 0.74) return GreisenStockworkZone.Topaz;
            if (grain > 0.65) return GreisenStockworkZone.Fluorite;
            if (grain > 0.54) return GreisenStockworkZone.Tourmaline;
            if (grain > 0.32) return GreisenStockworkZone.Quartz;
            return GreisenStockworkZone.None;
        }

        // Sheeted stockwork veins
        if (bestVein.HasValue && bestVein.Value.Inside)
        {
            if (bestVein.Value.CenterRatio < 0.35)
            {
                if (grain > 0.80) return GreisenStockworkZone.Molybdenite;
                if (grain > 0.65) return GreisenStockworkZone.Bismuthinite;
                if (grain > 0.52) return GreisenStockworkZone.NativeBismuth;
                if (grain > 0.44) return GreisenStockworkZone.Topaz;
                return GreisenStockworkZone.Quartz;
            }

            if (bestVein.Value.CenterRatio < 0.60)
            {
                if (grain > 0.88) return GreisenStockworkZone.Molybdenite;
                if (grain > 0.80) return GreisenStockworkZone.Wolframite;
                if (grain > 0.74) return GreisenStockworkZone.Cassiterite;
                if (grain > 0.62) return GreisenStockworkZone.Arsenopyrite;
                if (grain > 0.52) return GreisenStockworkZone.Pyrite;
                if (grain > 0.32) return GreisenStockworkZone.Quartz;
                return GreisenStockworkZone.None;
            }

            // Selvages
            if (grain > 0.75) return GreisenStockworkZone.Tourmaline;
            if (grain > 0.58) return GreisenStockworkZone.Topaz;
            if (grain > 0.40) return GreisenStockworkZone.Fluorite;
            return GreisenStockworkZone.None;
        }

        // Alteration halos and breccia
        if (bestVein.HasValue && bestVein.Value.InHalo)
        {
            double greisenField = Math.Sin(x * 0.20 + seed) + Math.Cos(z * 0.17 - seed * 0.5);
            if (grain > 0.90) return GreisenStockworkZone.Breccia;
            if (greisenField > 0.60) return GreisenStockworkZone.Tourmaline;
            if (greisenField < -0.50) return GreisenStockworkZone.Topaz;
            if (grain > 0.75) return GreisenStockworkZone.Fluorite;
            return GreisenStockworkZone.None;
        }

        return GreisenStockworkZone.None;
    }

    private GreisenStockworkZone EvaluateRareMetal(double x, double y, double z)
    {
        double graniteRoof = GetGraniteRoof(x, z);
        double radial = (x * x) / (cupolaRadiusX * cupolaRadiusX) + (z * z) / (cupolaRadiusZ * cupolaRadiusZ);
        bool inCap = radial <= 1.15 && y >= graniteRoof - 6.0 && y <= graniteRoof + 4.0;
        double grain = Hash3D(x * 1.9 + seed, y * 1.9, z * 1.9);
        double grain2 = Hash3D(x * 3.7 - seed, y * 3.7, z * 3.7);

        // Metasomatic replacement pods
        foreach (GreisenPod pod in replacementPods)
        {
            double rSq = pod.RadiusSquared(x, y, z);
            if (rSq <= 1.0)
            {
                double r = Math.Sqrt(rSq);
                if (pod.Type == 0)
                {
                                // Lepidolite-tantalite pod with columbite and tantalite.
                    if (r < 0.38 && grain > 0.65)
                        return grain2 > 0.50 ? GreisenStockworkZone.Columbite : GreisenStockworkZone.Tantalite;
                    if (r < 0.55 && grain > 0.72) return GreisenStockworkZone.Microlite;
                    if (r < 0.65 && grain > 0.85) return GreisenStockworkZone.Cassiterite;
                    if (grain > 0.30) return GreisenStockworkZone.Lepidolite;
                    return GreisenStockworkZone.Albite;
                }
                else if (pod.Type == 1)
                {
                    // Spodumene - Pollucite Pod
                    if (r < 0.40 && grain > 0.60) return GreisenStockworkZone.Pollucite;
                    if (r < 0.68 && grain > 0.45) return GreisenStockworkZone.Spodumene;
                    if (grain > 0.80)
                        return grain2 > 0.50 ? GreisenStockworkZone.Columbite : GreisenStockworkZone.Tantalite;
                    if (grain > 0.40) return GreisenStockworkZone.Lepidolite;
                    return GreisenStockworkZone.Quartz;
                }
                else
                {
                    // Beryl - Microlite - Topaz Pod
                    if (r < 0.45 && grain > 0.60) return GreisenStockworkZone.Beryl;
                    if (r < 0.60 && grain > 0.68) return GreisenStockworkZone.Microlite;
                    if (grain > 0.65) return GreisenStockworkZone.Topaz;
                    if (grain > 0.35) return GreisenStockworkZone.Albite;
                    return GreisenStockworkZone.Lepidolite;
                }
            }
        }

        // Sheeted stockwork veins and intersections
        SheetSample? bestVein = null;
        int insideVeinCount = 0;
        foreach (GreisenSheet vein in mainVeins)
        {
            SheetSample sample = vein.Sample(x, y, z, 0.0);
            if (sample.Inside) insideVeinCount++;
            if (sample.Score < double.PositiveInfinity && (bestVein == null || sample.Score < bestVein.Value.Score))
            {
                bestVein = sample;
            }
        }

        // High-grade intersection zones
        if (insideVeinCount >= 2)
        {
            if (grain > 0.75)
                return grain2 > 0.50 ? GreisenStockworkZone.Columbite : GreisenStockworkZone.Tantalite;
            if (grain > 0.58) return GreisenStockworkZone.Microlite;
            if (grain > 0.42) return GreisenStockworkZone.Lepidolite;
            if (grain > 0.28) return GreisenStockworkZone.Topaz;
            return GreisenStockworkZone.Breccia;
        }

        // Single stockwork vein
        if (bestVein.HasValue && bestVein.Value.Inside)
        {
            if (bestVein.Value.CenterRatio < 0.28 && grain > 0.82)
                return grain2 > 0.50 ? GreisenStockworkZone.Columbite : GreisenStockworkZone.Tantalite;
            if (bestVein.Value.CenterRatio < 0.45 && grain > 0.75) return GreisenStockworkZone.Microlite;
            if (bestVein.Value.CenterRatio < 0.55 && grain > 0.86) return GreisenStockworkZone.Cassiterite;
            if (grain > 0.68) return GreisenStockworkZone.Lepidolite;
            if (grain > 0.50) return GreisenStockworkZone.Topaz;
            if (grain > 0.30) return GreisenStockworkZone.Quartz;
            return GreisenStockworkZone.None;
        }

        // Apical endogreisen cap
        if (inCap)
        {
            double relativeY = y - graniteRoof;
            if (relativeY >= -5.0 && relativeY <= -2.0 && grain2 > 0.48)
            {
                if (grain > 0.88)
                    return grain2 > 0.70 ? GreisenStockworkZone.Columbite : GreisenStockworkZone.Tantalite;
                if (grain > 0.80) return GreisenStockworkZone.Microlite;
                return GreisenStockworkZone.Albite;
            }

            if (grain > 0.90)
                return grain2 > 0.50 ? GreisenStockworkZone.Columbite : GreisenStockworkZone.Tantalite;
            if (grain > 0.82) return GreisenStockworkZone.Lepidolite;
            if (grain > 0.70) return GreisenStockworkZone.Topaz;
            if (grain > 0.58) return GreisenStockworkZone.Fluorite;
            if (grain > 0.44) return GreisenStockworkZone.Quartz;
            if (grain > 0.32) return GreisenStockworkZone.Albite;
            return GreisenStockworkZone.None;
        }

        // Vein halos
        if (bestVein.HasValue && bestVein.Value.InHalo)
        {
            double greisenField = Math.Sin(x * 0.20 + seed) + Math.Cos(z * 0.17 - seed * 0.5);
            if (grain > 0.92) return GreisenStockworkZone.Breccia;
            if (greisenField > 0.60) return GreisenStockworkZone.Tourmaline;
            if (greisenField < -0.50) return GreisenStockworkZone.Topaz;
            if (grain > 0.60) return GreisenStockworkZone.Fluorite;
            if (grain > 0.35) return GreisenStockworkZone.Albite;
            return GreisenStockworkZone.None;
        }

        return GreisenStockworkZone.None;
    }

    public static GreisenStockworkZone ApplyWeatheringHook(GreisenStockworkZone zone, int depth)
    {
        if (zone == GreisenStockworkZone.None) return GreisenStockworkZone.None;

        if (depth <= 1)
        {
            return GreisenStockworkZone.Gossan;
        }

        if (depth <= 4)
        {
            return zone == GreisenStockworkZone.Quartz
                ? GreisenStockworkZone.Quartz
                : GreisenStockworkZone.Kaolinite;
        }

        return zone;
    }

    private static double Hash3D(double x, double y, double z)
    {
        double n = Math.Sin(x * 12.9898 + y * 78.233 + z * 37.719) * 43758.5453;
        return n - Math.Floor(n);
    }

    private readonly record struct SheetSample(
        double SignedDistance,
        double Distance,
        double Score,
        double CenterRatio,
        bool Inside,
        bool InHalo);

    private readonly record struct GreisenSheet(
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
        double Thickness,
        double Phase,
        double RelayCenter,
        double RelayWidth,
        double RelayShift,
        double PinchPhase,
        double BendAmp,
        double ThrowAmount)
    {
        public static GreisenSheet Create(
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
            double bendAmp,
            double throwAmount)
        {
            double strikeRad = strikeDeg * Math.PI / 180.0;
            double dipRad = dipDeg * Math.PI / 180.0;
            return new GreisenSheet(
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
                bendAmp,
                throwAmount);
        }

        public SheetSample Sample(double x, double y, double z, double offsetShift)
        {
            double along = x * CosStrike - z * SinStrike;
            double across = x * SinStrike + z * CosStrike - Offset - offsetShift;

            double broadBend = Math.Sin(along * 0.105 + y * 0.055 + Phase) * BendAmp;
            double localKink = Math.Cos(along * 0.29 + Phase * 1.3) * (BendAmp * 0.43);
            double relayStep = RelayShift * Math.Tanh((along - RelayCenter) / RelayWidth);
            double signedDistance = across * SinDip + (y - CenterY) * CosDip - broadBend - localKink - relayStep;

            double u = (along - CenterAlong) / LengthHalf;
            double v = (y - CenterY) / HeightHalf;
            double edgeWarp = Math.Sin(u * 3.6 + Phase) * Math.Cos(v * 2.8) * 0.13
                + Math.Sin(u * 7.2 - v * 3.1 + Phase * 0.4) * 0.06;
            double footprint = Math.Sqrt(u * u + v * v) + edgeWarp;
            double taper = Math.Clamp(1.0 - footprint * footprint, 0.0, 1.0);
            double pinchSwell = 0.78 + 0.22 * Math.Sin(along * 0.17 + PinchPhase)
                + 0.08 * Math.Cos(y * 0.31 - PinchPhase);
            double halfThickness = Math.Max(0.10, (Thickness * 0.5) * pinchSwell * Math.Sqrt(taper));
            double distance = Math.Abs(signedDistance);

            double score = footprint <= 1.18 ? distance / halfThickness : double.PositiveInfinity;
            bool inside = footprint <= 1.04 && distance <= halfThickness;
            bool inHalo = footprint <= 1.14 && distance <= halfThickness + 3.0 * taper;
            double centerRatio = distance / Math.Max(0.1, halfThickness);

            return new SheetSample(signedDistance, distance, score, centerRatio, inside, inHalo);
        }
    }

    private readonly record struct GreisenPod(
        double X,
        double Y,
        double Z,
        double Rx,
        double Ry,
        double Rz,
        int Type,
        double RotationY)
    {
        public double RadiusSquared(double px, double py, double pz)
        {
            double cosR = Math.Cos(RotationY);
            double sinR = Math.Sin(RotationY);
            double dx0 = px - X;
            double dz0 = pz - Z;
            double dx = (dx0 * cosR - dz0 * sinR) / Rx;
            double dy = (py - Y) / Ry;
            double dz = (dx0 * sinR + dz0 * cosR) / Rz;
            return dx * dx + dy * dy + dz * dz;
        }

        public bool Contains(double px, double py, double pz) => RadiusSquared(px, py, pz) <= 1.0;
    }
}
