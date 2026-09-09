using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace AdvancedGeology.WorldGen.ProceduralDeposits;

[JsonObject(MemberSerialization.OptIn)]
public sealed class OrogenicQuartzGoldDefinition
{
    [JsonProperty] public int HorizontalRadius { get; set; } = 34;
    [JsonProperty] public int VerticalHalfHeight { get; set; } = 30;
    [JsonProperty] public int LodeMin { get; set; } = 3;
    [JsonProperty] public int LodeMax { get; set; } = 5;
    [JsonProperty] public int SplayMin { get; set; } = 2;
    [JsonProperty] public int SplayMax { get; set; } = 4;
}

[JsonObject(MemberSerialization.OptIn)]
public sealed class StratiformChromititeDefinition
{
    [JsonProperty] public int HorizontalRadius { get; set; } = 34;
    [JsonProperty] public int VerticalHalfHeight { get; set; } = 34;
    [JsonProperty] public int SeamMin { get; set; } = 2;
    [JsonProperty] public int SeamMax { get; set; } = 3;
    [JsonProperty] public int TitanomagnetiteLayerMin { get; set; } = 3;
    [JsonProperty] public int TitanomagnetiteLayerMax { get; set; } = 5;
    [JsonProperty] public int DykeMin { get; set; } = 1;
    [JsonProperty] public int DykeMax { get; set; } = 3;
}

[JsonObject(MemberSerialization.OptIn)]
public sealed class MarinePhosphoriteDefinition
{
    [JsonProperty] public int HorizontalRadius { get; set; } = 34;
    [JsonProperty] public int VerticalHalfHeight { get; set; } = 28;
    [JsonProperty] public int BedMin { get; set; } = 2;
    [JsonProperty] public int BedMax { get; set; } = 4;
    [JsonProperty] public double MarginDensity { get; set; } = 0.30;
}

[JsonObject(MemberSerialization.OptIn)]
public sealed class LapisLazuliMarbleDefinition
{
    [JsonProperty] public int HorizontalRadius { get; set; } = 34;
    [JsonProperty] public int VerticalHalfHeight { get; set; } = 28;
    [JsonProperty] public int LensMin { get; set; } = 3;
    [JsonProperty] public int LensMax { get; set; } = 5;
    [JsonProperty] public double LapisDensity { get; set; } = 0.70;
    [JsonProperty] public double CalciteRichDensity { get; set; } = 0.35;
}

[JsonObject(MemberSerialization.OptIn)]
public sealed class IvittuutCryoliteDefinition
{
    [JsonProperty] public int HorizontalRadius { get; set; } = 34;
    [JsonProperty] public int VerticalHalfHeight { get; set; } = 30;
    [JsonProperty] public int VeinMin { get; set; } = 3;
    [JsonProperty] public int VeinMax { get; set; } = 5;
}

[JsonObject(MemberSerialization.OptIn)]
public sealed class AnorthositeIlmeniteDefinition
{
    [JsonProperty] public int HorizontalRadius { get; set; } = 34;
    [JsonProperty] public int VerticalHalfHeight { get; set; } = 28;
    [JsonProperty] public int SatelliteMin { get; set; } = 2;
    [JsonProperty] public int SatelliteMax { get; set; } = 5;
    [JsonProperty] public int DykeMin { get; set; } = 1;
    [JsonProperty] public int DykeMax { get; set; } = 2;
}

/// <summary>
/// Native gold, pyrite, arsenopyrite, quartz, and carbonate occupy a steep anastomosing lode array with subsidiary splays and intersection-controlled high-grade shoots.
/// </summary>
internal sealed class OrogenicQuartzGoldPlan : IAdditionalDepositPlan
{
    private const ulong Salt = 0x474F4C444C4F4445UL;
    private readonly int ox, oy, oz;
    private readonly double cs, ss, seed;
    private readonly Lode[] lodes;
    public int HorizontalRadius { get; }
    public int VerticalHalfHeight { get; }
    public int LodeCount { get; }
    public int SplayCount => lodes.Length - LodeCount;
    private readonly record struct Lode(double Cs, double Ss, double Sd, double Cd, double Offset, double CenterY, double Length, double Height, double Width, double Phase);

    private OrogenicQuartzGoldPlan(in ProceduralDepositInstance i, OrogenicQuartzGoldDefinition s, double strike, double seed, Lode[] lodes, int principal)
    { ox = i.CenterX; oy = i.CenterY; oz = i.CenterZ; HorizontalRadius = s.HorizontalRadius; VerticalHalfHeight = s.VerticalHalfHeight; double r = strike * Math.PI / 180; cs = Math.Cos(r); ss = Math.Sin(r); this.seed = seed; this.lodes = lodes; LodeCount = principal; }

    public static OrogenicQuartzGoldPlan Create(in ProceduralDepositInstance i, OrogenicQuartzGoldDefinition s)
    {
        var r = new ProceduralDepositRandom(i.FeatureId ^ Salt);
        double strike = r.Range(0, 180), seed = r.Range(0, 100);
        int n = r.NextInt(s.LodeMin, s.LodeMax);
        int splays = r.NextInt(s.SplayMin, s.SplayMax);
        var lodes = new Lode[n + splays];
        for (int k = 0; k < n; k++) lodes[k] = Make(strike + r.Range(-12, 12), r.Range(65, 86), (k - (n - 1) / 2.0) * r.Range(7, 11), r.Range(-4, 3), r.Range(18, 28), r.Range(23, 32), r.Range(1.5, 3), seed + k * 19);
        for (int k = 0; k < splays; k++) lodes[n + k] = Make(strike + r.Range(28, 55) * (k % 2 == 1 ? 1 : -1), r.Range(45, 75), r.Range(-14, 14), r.Range(-7, 7), r.Range(10, 18), r.Range(13, 23), r.Range(.8, 1.8), seed + 77 + k * 13);
        return new OrogenicQuartzGoldPlan(i, s, strike, seed, lodes, n);
    }

    private static Lode Make(double strikeDeg, double dipDeg, double offset, double centerY, double length, double height, double width, double phase)
    { double sr = strikeDeg * Math.PI / 180, dr = dipDeg * Math.PI / 180; return new(Math.Cos(sr), Math.Sin(sr), Math.Sin(dr), Math.Cos(dr), offset, centerY, length, height, width, phase); }

    public AdditionalDepositSample Evaluate(int wx, int wy, int wz)
    {
        double x = wx - ox, y = wy - oy, z = wz - oz;
        double a = x * cs - z * ss, c = x * ss + z * cs, warp = Math.Sin(a * .08 + seed);
        if (a * a / 1024 + c * c / 784 + (y + 3 + warp) * (y + 3 + warp) / 625 > 1) return default;
        bool found = false; double bestRatio = double.MaxValue, bestU = 0, bestQ = 0; int intersections = 0;
        for (int k = 0; k < lodes.Length; k++)
        {
            Lode v = lodes[k];
            double la = x * v.Cs - z * v.Ss, lc = x * v.Ss + z * v.Cs - v.Offset, dy = y - v.CenterY;
            double bend = Math.Sin(la * .12 + y * .07 + v.Phase) * 1.4, signed = lc * v.Sd + dy * v.Cd - bend;
            double u = la / v.Length, q = dy / v.Height, foot = Math.Sqrt(u * u + q * q);
            double tap = AdditionalDepositMath.Clamp(1 - foot * foot, 0, 1);
            double pinch = Math.Sin(la * .18 + v.Phase); pinch *= pinch;
            double half = v.Width * .5 * (.7 + .4 * pinch) * Math.Sqrt(tap), d = Math.Abs(signed);
            if (foot < 1 && d < half)
            {
                intersections++;
                double ratio = d / Math.Max(.1, half);
                if (ratio < bestRatio) { found = true; bestRatio = ratio; bestU = u; bestQ = q; }
            }
        }
        if (!found) return default;
        double field = Math.Sin(x * .19 - z * .14 + seed) + .5 * Math.Cos(y * .27 + x * .05);
        double shoot = Math.Cos(bestU * 6.2 + seed) - .45 * Math.Abs(bestQ) + (intersections > 1 ? .55 : 0);
        if (bestRatio < .34 && shoot > .48) return new(ProceduralMaterialSlots.Gold, 3);
        if (bestRatio < .62 && shoot > -.35) return new(ProceduralMaterialSlots.Gold, 1);
        if (bestRatio < .78) return field > .15 ? new(ProceduralMaterialSlots.Pyrite) : new(ProceduralMaterialSlots.Arsenopyrite);
        return field > .25 ? new(ProceduralMaterialSlots.Quartz) : new(ProceduralMaterialSlots.Carbonate);
    }
}

/// <summary>
/// Chromite, vanadiferous titanomagnetite, ilmenite, pyroxenite, gabbro, anorthosite, and basalt form vertically separated Critical, Main, and Upper Zone layers cut by dykes.
/// </summary>
internal sealed class StratiformChromititePlan : IAdditionalDepositPlan
{
    private const ulong Salt = 0x4348524F4D524545UL;
    private readonly int ox, oy, oz;
    private readonly double cs, ss, tanDip, seed;
    private readonly Reef[] chromitites;
    private readonly Reef[] titanomagnetites;
    private readonly Dyke[] dykes;
    public int HorizontalRadius { get; }
    public int VerticalHalfHeight { get; }
    public int ReefCount => chromitites.Length;
    public int TitanomagnetiteLayerCount => titanomagnetites.Length;
    public int DykeCount => dykes.Length;
    private readonly record struct Reef(double Level, double Half, double Phase);
    private readonly record struct Dyke(double Cs, double Ss, double Offset, double Width, double Phase);

    private StratiformChromititePlan(
        in ProceduralDepositInstance i,
        StratiformChromititeDefinition s,
        double strike,
        double dip,
        double seed,
        Reef[] chromitites,
        Reef[] titanomagnetites,
        Dyke[] dykes)
    {
        ox = i.CenterX;
        oy = i.CenterY;
        oz = i.CenterZ;
        HorizontalRadius = s.HorizontalRadius;
        VerticalHalfHeight = s.VerticalHalfHeight;
        double r = strike * Math.PI / 180;
        cs = Math.Cos(r);
        ss = Math.Sin(r);
        tanDip = Math.Tan(dip * Math.PI / 180);
        this.seed = seed;
        this.chromitites = chromitites;
        this.titanomagnetites = titanomagnetites;
        this.dykes = dykes;
    }

    public static StratiformChromititePlan Create(in ProceduralDepositInstance i, StratiformChromititeDefinition s)
    {
        var r = new ProceduralDepositRandom(i.FeatureId ^ Salt);
        double strike = r.Range(0, 180), dip = r.Range(1, 6), seed = r.Range(0, 100);

        int subsidiaryChromitites = r.NextInt(s.SeamMin, s.SeamMax);
        var chromitites = new Reef[subsidiaryChromitites + 1];
        chromitites[0] = new(-18, r.Range(1.2, 2.1), seed);
        for (int k = 0; k < subsidiaryChromitites; k++)
        {
            chromitites[k + 1] = new(-27 + k * r.Range(4.0, 6.0), r.Range(.5, 1.0), seed + 19 + k * 13);
        }

        int titanomagnetiteCount = r.NextInt(s.TitanomagnetiteLayerMin, s.TitanomagnetiteLayerMax);
        var titanomagnetites = new Reef[titanomagnetiteCount];
        double titanomagnetiteLevel = 13;
        for (int k = 0; k < titanomagnetiteCount; k++)
        {
            titanomagnetites[k] = new(titanomagnetiteLevel, r.Range(1.3, 2.5), seed + 71 + k * 17);
            titanomagnetiteLevel += r.Range(4.0, 6.0);
        }

        int dykeCount = r.NextInt(s.DykeMin, s.DykeMax);
        var dykes = new Dyke[dykeCount];
        for (int k = 0; k < dykeCount; k++)
        {
            double a = r.Range(0, Math.PI);
            dykes[k] = new(Math.Cos(a), Math.Sin(a), r.Range(-18, 18), r.Range(.8, 1.5), seed + k * 17);
        }
        return new StratiformChromititePlan(i, s, strike, dip, seed, chromitites, titanomagnetites, dykes);
    }

    public AdditionalDepositSample Evaluate(int wx, int wy, int wz)
    {
        double x = wx - ox, y = wy - oy, z = wz - oz;
        double a = x * cs - z * ss, c = x * ss + z * cs;
        double warp = Math.Sin(a * .07 + seed);
        if (a * a / 1089 + c * c / 900 + (y + 3 + warp) * (y + 3 + warp) / 961 > 1) return default;
        foreach (Dyke d in dykes)
        {
            if (Math.Abs(x * d.Ss + z * d.Cs - d.Offset - Math.Sin(x * .08 + d.Phase)) < d.Width)
            {
                return new(ProceduralMaterialSlots.Dyke);
            }
        }

        double stratY = y - tanDip * c - Math.Sin(a * .055 + seed) * 1.1;
        for (int i = 0; i < chromitites.Length; i++)
        {
            Reef reef = chromitites[i];
            double roll = reef.Level + 1.2 * Math.Sin(a * .09 + reef.Phase) + .7 * Math.Cos(c * .12 - reef.Phase * .3);
            double potential = Math.Sin(a * .18 + reef.Phase) * Math.Cos(c * .16 - reef.Phase * .4);
            double pothole = Math.Sin(a * .11 - reef.Phase) + Math.Cos(c * .14 + reef.Phase);
            double distance = Math.Abs(stratY - roll);
            if (distance < reef.Half * (.75 + .35 * Math.Max(0, potential)) && pothole > -1.3)
            {
                double q = distance / reef.Half;
                if (i == 0 && q < .52) return new(ProceduralMaterialSlots.Chromite, 3);
                if (q < .78) return new(ProceduralMaterialSlots.Chromite, 1);
                return new(ProceduralMaterialSlots.Chromite, 0);
            }
        }

        foreach (Reef reef in titanomagnetites)
        {
            double trough = 1.5 * Math.Max(0, Math.Cos(a * .075 + reef.Phase) * Math.Cos(c * .09 - reef.Phase * .35));
            double roll = reef.Level - trough + .8 * Math.Sin(a * .08 + reef.Phase) + .5 * Math.Cos(c * .1 - reef.Phase * .3);
            double merge = Math.Sin(a * .13 + reef.Phase) + Math.Cos(c * .14 - reef.Phase * .4);
            double localHalf = reef.Half * (.78 + .32 * Math.Max(0, merge));
            double distance = Math.Abs(stratY - roll);
            if (distance < localHalf)
            {
                double q = distance / localHalf;
                double ilmeniteBand = Math.Sin(a * .17 + reef.Phase) * Math.Cos(c * .15 - reef.Phase * .2);
                if (q > .80 && ilmeniteBand > .15) return new(ProceduralMaterialSlots.Ilmenite, 2);
                if (q < .38 && merge > .15) return new(ProceduralMaterialSlots.Titanomagnetite, 3);
                if (q < .68) return new(ProceduralMaterialSlots.Titanomagnetite, 2);
                return new(ProceduralMaterialSlots.Titanomagnetite, 1);
            }
        }
        if (stratY < -10) return new(ProceduralMaterialSlots.CriticalZone);
        if (stratY < 10) return new(ProceduralMaterialSlots.MainZone);
        return new(ProceduralMaterialSlots.UpperZone);
    }
}

/// <summary>
/// Phosphorite and apatite form stacked, gently dipping, pinch-and-swell beds concentrated along one side of a marine shelf.
/// </summary>
internal sealed class MarinePhosphoritePlan : IAdditionalDepositPlan
{
    private const ulong Salt = 0x50484F5350534845UL;
    private readonly int ox, oy, oz;
    private readonly double cs, ss, tanDip, shelf, seed, marginDensity;
    private readonly Bed[] beds;
    public int HorizontalRadius { get; }
    public int VerticalHalfHeight { get; }
    public int BedCount => beds.Length;
    public double Shelf => shelf;
    private readonly record struct Bed(double Level, double Half, double Along, double Across, double RadiusAlong, double RadiusAcross, double Phase);

    private MarinePhosphoritePlan(in ProceduralDepositInstance i, MarinePhosphoriteDefinition s, double strike, double dip, double shelf, double seed, Bed[] beds)
    { ox = i.CenterX; oy = i.CenterY; oz = i.CenterZ; HorizontalRadius = s.HorizontalRadius; VerticalHalfHeight = s.VerticalHalfHeight; double r = strike * Math.PI / 180; cs = Math.Cos(r); ss = Math.Sin(r); tanDip = Math.Tan(dip * Math.PI / 180); this.shelf = shelf; this.seed = seed; marginDensity = s.MarginDensity; this.beds = beds; }

    public static MarinePhosphoritePlan Create(in ProceduralDepositInstance i, MarinePhosphoriteDefinition s)
    {
        var r = new ProceduralDepositRandom(i.FeatureId ^ Salt);
        double strike = r.Range(0, 180), dip = r.Range(1, 6);
        double shelf = r.Range(0, 1) < .5 ? -1 : 1;
        double seed = r.Range(0, 100);
        int count = r.NextInt(s.BedMin, s.BedMax);
        var beds = new Bed[count];
        double level = -11;
        for (int k = 0; k < count; k++)
        {
            beds[k] = new(level + r.Range(-1, 1), r.Range(1.1, 2.5), r.Range(-5, 5), shelf * r.Range(2, 8), r.Range(20, 29), r.Range(10, 17), seed + k * 21);
            level += r.Range(6, 9);
        }
        return new MarinePhosphoritePlan(i, s, strike, dip, shelf, seed, beds);
    }

    public AdditionalDepositSample Evaluate(int wx, int wy, int wz)
    {
        double x = wx - ox, y = wy - oy, z = wz - oz;
        double a = x * cs - z * ss, c = x * ss + z * cs;
        double warp = Math.Sin(a * .08 + seed);
        if (a * a / 1089 + c * c / 841 + (y + 3 + warp) * (y + 3 + warp) / 676 > 1) return default;
        double stratY = y - tanDip * c - Math.Sin(a * .055 + seed) * .9;
        for (int k = 0; k < beds.Length; k++)
        {
            Bed b = beds[k];
            double u = (a - b.Along) / b.RadiusAlong, v = (c - b.Across) / b.RadiusAcross, w = (stratY - b.Level) / b.Half;
            double foot = Math.Sqrt(u * u + v * v) + .08 * Math.Sin(u * 4 + b.Phase) * Math.Cos(v * 3);
            double tap = Math.Max(0, 1 - foot * foot);
            if (!(foot < 1 && Math.Abs(w) < .35 + .75 * tap)) continue;
            double outer = shelf * c / 29;
            double condense = Math.Sin(a * .17 + b.Phase) + .5 * Math.Cos(c * .21 - b.Phase * .4);
            if (foot < .76 && outer > .02)
            {
                if (condense > 1.15 && Math.Abs(w) < .38) return new(ProceduralMaterialSlots.Apatite);
                return new(ProceduralMaterialSlots.Phosphorite);
            }
            return new(ProceduralMaterialSlots.Phosphorite, 0, marginDensity);
        }
        return default;
    }
}

/// <summary>
/// Lapis lazuli, calcite, pyrite, and carbonate marble form stacked lenses within steeply dipping, compositionally banded marble.
/// </summary>
internal sealed class LapisLazuliMarblePlan : IAdditionalDepositPlan
{
    private const ulong Salt = 0x4C41504953424C55UL;
    private readonly int ox, oy, oz;
    private readonly double cs, ss, tanDip, seed, lapisDensity, calciteRichDensity;
    private readonly Lens[] lenses;
    public int HorizontalRadius { get; }
    public int VerticalHalfHeight { get; }
    public int LensCount => lenses.Length;
    private readonly record struct Lens(double Along, double Level, double RadiusAlong, double RadiusAcross, double RadiusVertical, double Phase);

    private LapisLazuliMarblePlan(in ProceduralDepositInstance i, LapisLazuliMarbleDefinition s, double strike, double dip, double seed, Lens[] lenses)
    { ox = i.CenterX; oy = i.CenterY; oz = i.CenterZ; HorizontalRadius = s.HorizontalRadius; VerticalHalfHeight = s.VerticalHalfHeight; double r = strike * Math.PI / 180; cs = Math.Cos(r); ss = Math.Sin(r); tanDip = Math.Tan(dip * Math.PI / 180); this.seed = seed; lapisDensity = s.LapisDensity; calciteRichDensity = s.CalciteRichDensity; this.lenses = lenses; }

    public static LapisLazuliMarblePlan Create(in ProceduralDepositInstance i, LapisLazuliMarbleDefinition s)
    {
        var r = new ProceduralDepositRandom(i.FeatureId ^ Salt);
        double strike = r.Range(0, 180), dip = r.Range(24, 48), seed = r.Range(0, 100);
        int n = r.NextInt(s.LensMin, s.LensMax);
        var lenses = new Lens[n];
        double level = -9;
        for (int k = 0; k < n; k++)
        {
            lenses[k] = new(r.Range(-15, 15), level, r.Range(9, 16), r.Range(3, 6), r.Range(2, 4), seed + k * 21);
            level += r.Range(6, 9);
        }
        return new LapisLazuliMarblePlan(i, s, strike, dip, seed, lenses);
    }

    public AdditionalDepositSample Evaluate(int wx, int wy, int wz)
    {
        double x = wx - ox, y = wy - oy, z = wz - oz;
        double a = x * cs - z * ss, c = x * ss + z * cs;
        double warp = Math.Sin(a * .07 + seed);
        if (a * a / 1024 + c * c / 841 + (y + 3 + warp) * (y + 3 + warp) / 676 > 1) return default;
        double stratY = y - tanDip * c * .55 - Math.Sin(a * .06 + seed) * 1.1;
        bool carbonate = Math.Sin((stratY + seed) * .34) > .15;
        double field = Math.Sin(x * .21 - z * .17 + seed) + .5 * Math.Cos(y * .28 + x * .05);
        for (int k = 0; k < lenses.Length; k++)
        {
            Lens l = lenses[k];
            double u = (a - l.Along) / l.RadiusAlong, v = c / l.RadiusAcross, w = (stratY - l.Level) / l.RadiusVertical;
            double neckTerm = Math.Sin((a - l.Along) * .18 + l.Phase); neckTerm *= neckTerm;
            double neck = .72 + .35 * neckTerm;
            double vs = v / neck;
            double q = Math.Sqrt(u * u + vs * vs + w * w) + .08 * Math.Sin(u * 4 + l.Phase) * Math.Cos(v * 3);
            if (q < 1)
            {
                if (q < .42 && field > .05) return new(ProceduralMaterialSlots.Lapis);
                if (q < .7) return new(ProceduralMaterialSlots.Lapis, 0, lapisDensity);
                if (q < .9) return new(ProceduralMaterialSlots.Lapis, 0, calciteRichDensity);
                return field > 1.15 ? new(ProceduralMaterialSlots.Pyrite) : new(ProceduralMaterialSlots.Calcite);
            }
            if (q < 1.3 && carbonate) return new(ProceduralMaterialSlots.Matrix, 0, .3);
        }
        return default;
    }
}

/// <summary>
/// Cryolite, fluorite, siderite, quartz, topaz, microcline, and intrusive matrix form a zoned stock with a dense central ore body and crosscutting veins.
/// </summary>
internal sealed class IvittuutCryolitePlan : IAdditionalDepositPlan
{
    private const ulong Salt = 0x435259534F4C4954UL;
    private readonly int ox, oy, oz;
    private readonly double cx, cz, rx, rz, seed;
    private readonly Vein[] veins;
    public int HorizontalRadius { get; }
    public int VerticalHalfHeight { get; }
    public int VeinCount => veins.Length;
    private readonly record struct Vein(double Cs, double Ss, double Offset, double Phase);

    private IvittuutCryolitePlan(in ProceduralDepositInstance i, IvittuutCryoliteDefinition s, double cx, double cz, double rx, double rz, double seed, Vein[] veins)
    { ox = i.CenterX; oy = i.CenterY; oz = i.CenterZ; HorizontalRadius = s.HorizontalRadius; VerticalHalfHeight = s.VerticalHalfHeight; this.cx = cx; this.cz = cz; this.rx = rx; this.rz = rz; this.seed = seed; this.veins = veins; }

    public static IvittuutCryolitePlan Create(in ProceduralDepositInstance i, IvittuutCryoliteDefinition s)
    {
        var r = new ProceduralDepositRandom(i.FeatureId ^ Salt);
        double cx = r.Range(-3, 3), cz = r.Range(-3, 3), rx = r.Range(22, 28), rz = r.Range(18, 24);
        r.Range(8, 14);
        double seed = r.Range(0, 100);
        int n = r.NextInt(s.VeinMin, s.VeinMax);
        var veins = new Vein[n];
        for (int k = 0; k < n; k++) { double a = r.Range(0, Math.PI); veins[k] = new(Math.Cos(a), Math.Sin(a), r.Range(-14, 14), seed + k * 19); }
        return new IvittuutCryolitePlan(i, s, cx, cz, rx, rz, seed, veins);
    }

    public AdditionalDepositSample Evaluate(int wx, int wy, int wz)
    {
        double x = wx - ox, y = wy - oy, z = wz - oz;
        if ((x - cx) * (x - cx) / 1089 + (z - cz) * (z - cz) / 900 + Math.Pow(y + 3 + Math.Sin(x * .08 + seed), 2) / 676 > 1) return default;
        double su = (x - cx) / rx, sv = (z - cz) / rz, sw = (y + 8) / 27;
        double stockQ = Math.Sqrt(su * su + sv * sv + sw * sw) + .06 * Math.Sin(su * 4 + seed) * Math.Cos(sv * 3);
        if (stockQ >= 1) return default;
        double ou = (x - cx - 2) / 15, ov = (z - cz + 1) / 12, ow = (y + 1) / 16;
        double oreQ = Math.Sqrt(ou * ou + ov * ov + ow * ow) + .1 * Math.Sin(ou * 4 + seed) * Math.Cos(ow * 3);
        double veinDistance = 9;
        for (int k = 0; k < veins.Length; k++)
        {
            Vein v = veins[k];
            veinDistance = Math.Min(veinDistance, Math.Abs(x * v.Ss + z * v.Cs - v.Offset - Math.Sin(y * .14 + v.Phase)));
        }
        double field = Math.Sin(x * .2 - z * .16 + seed) + .5 * Math.Cos(y * .27 + x * .05);
        if (veinDistance < .7 && oreQ < 1.25) return field > .35 ? new(ProceduralMaterialSlots.Fluorite) : new(ProceduralMaterialSlots.Topaz);
        if (oreQ < 1)
        {
            if (oreQ < .54) return new(ProceduralMaterialSlots.Cryolite);
            if (oreQ < .76) return field > .1 ? new(ProceduralMaterialSlots.Cryolite) : new(ProceduralMaterialSlots.Fluorite);
            if (oreQ < .93) return field > .25 ? new(ProceduralMaterialSlots.Siderite, 1) : new(ProceduralMaterialSlots.Cryolite);
            return default;
        }
        if (oreQ < 1.27) return field > .65 ? new(ProceduralMaterialSlots.Quartz) : default;
        if (stockQ < .72 && field > 1.08) return new(ProceduralMaterialSlots.Microcline);
        return new(ProceduralMaterialSlots.Matrix);
    }
}

/// <summary>
/// Ilmenite, magnetite, apatite, anorthosite, and mafic roots form a principal irregular oxide body, smaller satellite bodies, and crosscutting dykes.
/// </summary>
internal sealed class AnorthositeIlmenitePlan : IAdditionalDepositPlan
{
    private const ulong Salt = 0x494C4D454E414E4FUL;
    private readonly int ox, oy, oz;
    private readonly double cs, ss, seed;
    private readonly Body[] bodies;
    private readonly Dyke[] dykes;
    public int HorizontalRadius { get; }
    public int VerticalHalfHeight { get; }
    public int BodyCount => bodies.Length;
    public int DykeCount => dykes.Length;
    private readonly record struct Body(double Along, double Across, double Y, double RadiusAlong, double RadiusAcross, double RadiusVertical, double Phase, bool Main);
    private readonly record struct Dyke(double Cs, double Ss, double Offset, double Width);

    private AnorthositeIlmenitePlan(in ProceduralDepositInstance i, AnorthositeIlmeniteDefinition s, double strike, double seed, Body[] bodies, Dyke[] dykes)
    {
        ox = i.CenterX;
        oy = i.CenterY;
        oz = i.CenterZ;
        HorizontalRadius = s.HorizontalRadius;
        VerticalHalfHeight = s.VerticalHalfHeight;
        double r = strike * Math.PI / 180;
        cs = Math.Cos(r);
        ss = Math.Sin(r);
        this.seed = seed;
        this.bodies = bodies;
        this.dykes = dykes;
    }

    public static AnorthositeIlmenitePlan Create(in ProceduralDepositInstance i, AnorthositeIlmeniteDefinition s)
    {
        var r = new ProceduralDepositRandom(i.FeatureId ^ Salt);
        double strike = r.Range(0, 180), seed = r.Range(0, 100);
        int satellites = r.NextInt(s.SatelliteMin, s.SatelliteMax);
        var bodies = new Body[satellites + 1];
        bodies[0] = new(0, 0, -1, r.Range(23, 30), r.Range(9, 14), r.Range(14, 20), seed, true);
        for (int k = 0; k < satellites; k++) bodies[k + 1] = new(r.Range(-20, 20), r.Range(-15, 15), r.Range(-12, 9), r.Range(6, 12), r.Range(4, 8), r.Range(5, 10), seed + 21 + k * 13, false);
        int dykeCount = r.NextInt(s.DykeMin, s.DykeMax);
        var dykes = new Dyke[dykeCount];
        for (int k = 0; k < dykeCount; k++) { double a = r.Range(0, Math.PI); dykes[k] = new(Math.Cos(a), Math.Sin(a), r.Range(-17, 17), r.Range(.8, 1.5)); }
        return new AnorthositeIlmenitePlan(i, s, strike, seed, bodies, dykes);
    }

    public AdditionalDepositSample Evaluate(int wx, int wy, int wz)
    {
        double x = wx - ox, y = wy - oy, z = wz - oz;
        double a = x * cs - z * ss, c = x * ss + z * cs;
        double warp = Math.Sin(a * .07 + seed);
        if (a * a / 1024 + c * c / 841 + (y + 3 + warp) * (y + 3 + warp) / 625 > 1) return default;
        for (int k = 0; k < dykes.Length; k++)
        {
            Dyke d = dykes[k];
            if (Math.Abs(x * d.Ss + z * d.Cs - d.Offset) < d.Width) return new(ProceduralMaterialSlots.Dyke);
        }
        bool found = false, near = false;
        double bestQ = double.MaxValue, bu = 0, bv = 0, bw = 0, bp = 0; bool bMain = false;
        for (int k = 0; k < bodies.Length; k++)
        {
            Body b = bodies[k];
            double u = (a - b.Along) / b.RadiusAlong, v = (c - b.Across) / b.RadiusAcross, w = (y - b.Y) / b.RadiusVertical;
            double bodyWarp = .1 * Math.Sin(u * 4 + b.Phase) * Math.Cos(v * 3) + .05 * Math.Sin(w * 5 - b.Phase * .3);
            double q = Math.Sqrt(u * u + v * v + w * w) + bodyWarp;
            if (q < 1 && q < bestQ) { found = true; bestQ = q; bu = u; bv = v; bw = w; bp = b.Phase; bMain = b.Main; }
            if (q < 1.25) near = true;
        }
        double field = Math.Sin(x * .2 - z * .16 + seed) + .5 * Math.Cos(y * .27 + x * .05);
        if (found)
        {
            double raft = Math.Sin(bu * 8 + bp) * Math.Cos(bv * 7 - bp * .3) * Math.Sin(bw * 6 + bp);
            if (raft > .74 && bestQ < .82) return new(ProceduralMaterialSlots.Raft);
            if (bw < -.58 && !bMain) return new(ProceduralMaterialSlots.Root);
            if (bestQ < .48) return field > 1.05 ? new(ProceduralMaterialSlots.Magnetite, 2) : new(ProceduralMaterialSlots.Ilmenite, 3);
            if (bestQ < .76) return field > .75 ? new(ProceduralMaterialSlots.Magnetite, 1) : new(ProceduralMaterialSlots.Ilmenite, 2);
            if (field > 1.15) return new(ProceduralMaterialSlots.Apatite);
            return new(ProceduralMaterialSlots.Ilmenite, 1);
        }
        if (near) return new(ProceduralMaterialSlots.Root, 0, .25);
        return default;
    }
}

internal sealed class OrogenicQuartzGoldProceduralTemplate : AdditionalProceduralTemplate<OrogenicQuartzGoldPlan>
{
    private static readonly string[] Slots = [ProceduralMaterialSlots.Gold, ProceduralMaterialSlots.Quartz, ProceduralMaterialSlots.Pyrite, ProceduralMaterialSlots.Arsenopyrite, ProceduralMaterialSlots.Carbonate];
    public override string Code => "orogenicQuartzGold";
    public override IReadOnlyList<string> RequiredMaterialSlots => Slots;
    protected override int Radius(ProceduralDepositDefinition d) => d.OrogenicQuartzGold.HorizontalRadius;
    protected override bool IsValid(ProceduralDepositDefinition d) => d.OrogenicQuartzGold.HorizontalRadius >= 30 && d.OrogenicQuartzGold.VerticalHalfHeight >= 26 && d.OrogenicQuartzGold.LodeMin >= 1 && d.OrogenicQuartzGold.LodeMax >= d.OrogenicQuartzGold.LodeMin && d.OrogenicQuartzGold.SplayMax >= d.OrogenicQuartzGold.SplayMin;
    protected override OrogenicQuartzGoldPlan Build(in ProceduralDepositInstance i, ProceduralDepositDefinition d) => OrogenicQuartzGoldPlan.Create(i, d.OrogenicQuartzGold);
}

internal sealed class StratiformChromititeProceduralTemplate : AdditionalProceduralTemplate<StratiformChromititePlan>
{
    private static readonly string[] Slots =
    [
        ProceduralMaterialSlots.Chromite,
        ProceduralMaterialSlots.Titanomagnetite,
        ProceduralMaterialSlots.Ilmenite,
        ProceduralMaterialSlots.CriticalZone,
        ProceduralMaterialSlots.MainZone,
        ProceduralMaterialSlots.UpperZone,
        ProceduralMaterialSlots.Dyke
    ];
    public override string Code => "stratiformChromitite";
    public override IReadOnlyList<string> RequiredMaterialSlots => Slots;
    protected override int Radius(ProceduralDepositDefinition d) => d.StratiformChromitite.HorizontalRadius;
    protected override bool IsValid(ProceduralDepositDefinition d) => d.StratiformChromitite.HorizontalRadius >= 30 && d.StratiformChromitite.VerticalHalfHeight >= 30 && d.StratiformChromitite.SeamMin >= 1 && d.StratiformChromitite.SeamMax >= d.StratiformChromitite.SeamMin && d.StratiformChromitite.TitanomagnetiteLayerMin >= 1 && d.StratiformChromitite.TitanomagnetiteLayerMax >= d.StratiformChromitite.TitanomagnetiteLayerMin && d.StratiformChromitite.DykeMin >= 1 && d.StratiformChromitite.DykeMax >= d.StratiformChromitite.DykeMin;
    protected override StratiformChromititePlan Build(in ProceduralDepositInstance i, ProceduralDepositDefinition d) => StratiformChromititePlan.Create(i, d.StratiformChromitite);
}

internal sealed class MarinePhosphoriteProceduralTemplate : AdditionalProceduralTemplate<MarinePhosphoritePlan>
{
    private static readonly string[] Slots = [ProceduralMaterialSlots.Phosphorite, ProceduralMaterialSlots.Apatite];
    public override string Code => "marinePhosphorite";
    public override IReadOnlyList<string> RequiredMaterialSlots => Slots;
    protected override int Radius(ProceduralDepositDefinition d) => d.MarinePhosphorite.HorizontalRadius;
    protected override bool IsValid(ProceduralDepositDefinition d) => d.MarinePhosphorite.HorizontalRadius >= 30 && d.MarinePhosphorite.VerticalHalfHeight >= 24 && d.MarinePhosphorite.BedMin >= 1 && d.MarinePhosphorite.BedMax >= d.MarinePhosphorite.BedMin && d.MarinePhosphorite.MarginDensity is > 0 and <= 1;
    protected override MarinePhosphoritePlan Build(in ProceduralDepositInstance i, ProceduralDepositDefinition d) => MarinePhosphoritePlan.Create(i, d.MarinePhosphorite);
}

internal sealed class LapisLazuliMarbleProceduralTemplate : AdditionalProceduralTemplate<LapisLazuliMarblePlan>
{
    private static readonly string[] Slots = [ProceduralMaterialSlots.Lapis, ProceduralMaterialSlots.Calcite, ProceduralMaterialSlots.Pyrite, ProceduralMaterialSlots.Matrix];
    public override string Code => "lapisLazuliMarble";
    public override IReadOnlyList<string> RequiredMaterialSlots => Slots;
    protected override int Radius(ProceduralDepositDefinition d) => d.LapisLazuliMarble.HorizontalRadius;
    protected override bool IsValid(ProceduralDepositDefinition d) => d.LapisLazuliMarble.HorizontalRadius >= 30 && d.LapisLazuliMarble.VerticalHalfHeight >= 24 && d.LapisLazuliMarble.LensMin >= 1 && d.LapisLazuliMarble.LensMax >= d.LapisLazuliMarble.LensMin && d.LapisLazuliMarble.LapisDensity is > 0 and <= 1 && d.LapisLazuliMarble.CalciteRichDensity is > 0 and <= 1;
    protected override LapisLazuliMarblePlan Build(in ProceduralDepositInstance i, ProceduralDepositDefinition d) => LapisLazuliMarblePlan.Create(i, d.LapisLazuliMarble);
}

internal sealed class IvittuutCryoliteProceduralTemplate : AdditionalProceduralTemplate<IvittuutCryolitePlan>
{
    private static readonly string[] Slots = [ProceduralMaterialSlots.Cryolite, ProceduralMaterialSlots.Fluorite, ProceduralMaterialSlots.Topaz, ProceduralMaterialSlots.Quartz, ProceduralMaterialSlots.Siderite, ProceduralMaterialSlots.Microcline, ProceduralMaterialSlots.Matrix];
    public override string Code => "ivittuutCryolite";
    public override IReadOnlyList<string> RequiredMaterialSlots => Slots;
    protected override int Radius(ProceduralDepositDefinition d) => d.IvittuutCryolite.HorizontalRadius;
    protected override bool IsValid(ProceduralDepositDefinition d) => d.IvittuutCryolite.HorizontalRadius >= 30 && d.IvittuutCryolite.VerticalHalfHeight >= 26 && d.IvittuutCryolite.VeinMin >= 1 && d.IvittuutCryolite.VeinMax >= d.IvittuutCryolite.VeinMin;
    protected override IvittuutCryolitePlan Build(in ProceduralDepositInstance i, ProceduralDepositDefinition d) => IvittuutCryolitePlan.Create(i, d.IvittuutCryolite);
}

internal sealed class AnorthositeIlmeniteProceduralTemplate : AdditionalProceduralTemplate<AnorthositeIlmenitePlan>
{
    private static readonly string[] Slots = [ProceduralMaterialSlots.Ilmenite, ProceduralMaterialSlots.Magnetite, ProceduralMaterialSlots.Apatite, ProceduralMaterialSlots.Raft, ProceduralMaterialSlots.Root, ProceduralMaterialSlots.Dyke];
    public override string Code => "anorthositeIlmenite";
    public override IReadOnlyList<string> RequiredMaterialSlots => Slots;
    protected override int Radius(ProceduralDepositDefinition d) => d.AnorthositeIlmenite.HorizontalRadius;
    protected override bool IsValid(ProceduralDepositDefinition d) => d.AnorthositeIlmenite.HorizontalRadius >= 30 && d.AnorthositeIlmenite.VerticalHalfHeight >= 24 && d.AnorthositeIlmenite.SatelliteMin >= 1 && d.AnorthositeIlmenite.SatelliteMax >= d.AnorthositeIlmenite.SatelliteMin && d.AnorthositeIlmenite.DykeMin >= 1 && d.AnorthositeIlmenite.DykeMax >= d.AnorthositeIlmenite.DykeMin;
    protected override AnorthositeIlmenitePlan Build(in ProceduralDepositInstance i, ProceduralDepositDefinition d) => AnorthositeIlmenitePlan.Create(i, d.AnorthositeIlmenite);
}

public sealed partial class ProceduralDepositWorldGenSystem
{
    internal void RealizeAdditionalDepositCandidate(DepositCandidate candidate, IChunkColumnGenerateRequest request, int baseX, int baseZ)
    {
        CompiledProceduralDeposit compiled=candidate.Compiled;
        IAdditionalDepositPlan plan=candidate.AdditionalPlan;
        ushort[] heights=request.Chunks[0].MapChunk.WorldGenTerrainHeightMap;
        int worldHeight=serverApi!.World.BlockAccessor.MapSizeY;
        int minY=GameMath.Clamp(candidate.Instance.CenterY-plan.VerticalHalfHeight,1,worldHeight-2);
        int prototypeMax=GameMath.Clamp(candidate.Instance.CenterY+plan.VerticalHalfHeight,1,worldHeight-2);
        for(int lx=0;lx<ChunkSize;lx++){
            int wx=baseX+lx;
            for(int lz=0;lz<ChunkSize;lz++){
                int wz=baseZ+lz,maxY=Math.Min(heights[lz*ChunkSize+lx],prototypeMax);
                for(int y=minY;y<=maxY;y++){
                    AdditionalDepositSample sample=plan.Evaluate(wx,y,wz);
                    if(sample.Slot==null)continue;
                    int slot=compiled.GetSlotId(sample.Slot);
                    if(slot<0)continue;
                    if(sample.Density<1&&AdditionalDepositMath.Hash(wx+(int)(candidate.Instance.FeatureId%997),y,wz)>sample.Density)continue;
                    int cy=y/ChunkSize;if((uint)cy>=(uint)request.Chunks.Length)continue;
                    int index=((y%ChunkSize)*ChunkSize+lz)*ChunkSize+lx;
                    IChunkBlocks blocks=request.Chunks[cy].Data;
                    int host=blocks.GetBlockIdUnsafe(index);
                    if(!CanReplaceWithProceduralRock(compiled,host))continue;
                    int place=compiled.ResolveBlock(slot,sample.Grade,host);
                    if(place==0||place==host)continue;
                    blocks.SetBlockUnsafe(index,place);
                    blocks.SetFluid(index,0);
                }
            }
        }
    }
}
