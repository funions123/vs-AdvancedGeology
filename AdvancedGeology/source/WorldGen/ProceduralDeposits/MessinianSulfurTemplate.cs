using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using Vintagestory.API.Server;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace AdvancedGeology.WorldGen.ProceduralDeposits;

public enum MessinianSulfurZone
{
    None = 0,
    HighGradeSulfur,
    DisseminatedSulfur,
    AuthigenicCarbonate,
    PrimaryGypsum,
    BurialGypsum,
    EvaporiteBreccia,
    CalciteAragonite,
    Celestine
}

[JsonObject(MemberSerialization.OptIn)]
public sealed class MessinianSulfurDefinition
{
    [JsonProperty] public int HorizontalRadius { get; set; } = 35;
    [JsonProperty] public int VerticalHalfHeight { get; set; } = 30;
    [JsonProperty] public int LensMin { get; set; } = 3;
    [JsonProperty] public int LensMax { get; set; } = 5;
    [JsonProperty] public int BrecciaMin { get; set; } = 2;
    [JsonProperty] public int BrecciaMax { get; set; } = 4;
    [JsonProperty] public double BasinRadiusAlongMin { get; set; } = 28;
    [JsonProperty] public double BasinRadiusAlongMax { get; set; } = 32;
    [JsonProperty] public double BasinRadiusAcrossMin { get; set; } = 24;
    [JsonProperty] public double BasinRadiusAcrossMax { get; set; } = 28;
    [JsonProperty] public double PrimaryGypsumHalfThickness { get; set; } = 0.55;
    [JsonProperty] public double CelestineGemFraction { get; set; } = 0.10;
}

public readonly record struct MessinianSulfurSample(
    MessinianSulfurZone Zone,
    bool InLens = false,
    bool InBreccia = false,
    double LensRadius = double.PositiveInfinity,
    double GradeField = 0.0,
    double StratigraphicY = 0.0);

/// <summary>
/// Native sulfur, authigenic carbonate, gypsum, celestine, calcite-aragonite, and evaporite breccia form stratiform replacement lenses and burial breccias within an evaporite basin.
/// </summary>
internal sealed class MessinianSulfurPlan
{
    private const ulong PlanSalt = 0x4D45535353554C46UL;
    private readonly int originX, originY, originZ;
    private readonly double cosStrike, sinStrike, tanDip, basinX, basinZ, radiusAlong, radiusAcross, seed, primaryGypsumHalfThickness;
    private readonly Body[] lenses;
    private readonly Body[] breccias;

    public int LensCount => lenses.Length;
    public int BrecciaCount => breccias.Length;
    public double StrikeDeg { get; }
    public double DipDeg { get; }
    public double RadiusAlong => radiusAlong;
    public double RadiusAcross => radiusAcross;

    private MessinianSulfurPlan(in ProceduralDepositInstance instance, double strike, double dip, double basinX, double basinZ, double radiusAlong, double radiusAcross, double seed, double primaryGypsumHalfThickness, Body[] lenses, Body[] breccias)
    {
        originX = instance.CenterX; originY = instance.CenterY; originZ = instance.CenterZ;
        StrikeDeg = strike; DipDeg = dip;
        double radians = strike * Math.PI / 180.0;
        cosStrike = Math.Cos(radians); sinStrike = Math.Sin(radians); tanDip = Math.Tan(dip * Math.PI / 180.0);
        this.basinX = basinX; this.basinZ = basinZ; this.radiusAlong = radiusAlong; this.radiusAcross = radiusAcross;
        this.seed = seed; this.primaryGypsumHalfThickness = primaryGypsumHalfThickness; this.lenses = lenses; this.breccias = breccias;
    }

    public static MessinianSulfurPlan Create(in ProceduralDepositInstance instance, MessinianSulfurDefinition settings)
    {
        var random = new ProceduralDepositRandom(instance.FeatureId ^ PlanSalt);
        double strike = random.Range(0, 180), dip = random.Range(2, 9), basinX = random.Range(-3, 3), basinZ = random.Range(-3, 3);
        double radiusAlong = random.Range(settings.BasinRadiusAlongMin, settings.BasinRadiusAlongMax);
        double radiusAcross = random.Range(settings.BasinRadiusAcrossMin, settings.BasinRadiusAcrossMax);
        double seed = random.Range(0, 100);
        int lensCount = random.NextInt(settings.LensMin, settings.LensMax);
        double scale = Math.Sqrt(4.0 / lensCount);
        var lenses = new Body[lensCount];
        for (int i = 0; i < lensCount; i++) lenses[i] = new Body(random.Range(-12, 12), random.Range(-9, 9), random.Range(-5, 5), random.Range(13, 21) * scale, random.Range(7, 12) * scale, random.Range(2.3, 4.4), seed + i * 23);
        int brecciaCount = random.NextInt(settings.BrecciaMin, settings.BrecciaMax);
        var breccias = new Body[brecciaCount];
        for (int i = 0; i < brecciaCount; i++) breccias[i] = new Body(random.Range(-14, 14), random.Range(-12, 12), random.Range(-7, 7), random.Range(8, 14), random.Range(5, 9), random.Range(4, 7), seed + 71 + i * 17);
        return new MessinianSulfurPlan(instance, strike, dip, basinX, basinZ, radiusAlong, radiusAcross, seed, settings.PrimaryGypsumHalfThickness, lenses, breccias);
    }

    public MessinianSulfurSample Evaluate(int worldX, int worldY, int worldZ)
    {
        double x = worldX - originX, y = worldY - originY, z = worldZ - originZ;
        Local(x, z, out double along, out double across);
        double envelopeWarp = 1.1 * Math.Sin(along * .08 + seed) + .7 * Math.Cos(across * .11 - seed * .3);
        double q = Square(along / radiusAlong) + Square(across / radiusAcross) + Square((y + 3 + envelopeWarp) / 25);
        if (y < -30 || q > 1) return default;

        double stratY = StratigraphicY(along, across, y);
        double field = Math.Sin(x * .2 - z * .16 + seed) + .52 * Math.Cos(y * .27 + x * .05);
        BodyHit bestBreccia = default;
        for (int i = 0; i < breccias.Length; i++) { BodyHit hit = Sample(along, across, stratY, breccias[i]); if (hit.Inside && (!bestBreccia.Inside || hit.Radius < bestBreccia.Radius)) bestBreccia = hit; }
        BodyHit bestLens = default; bool nearLens = false;
        for (int i = 0; i < lenses.Length; i++) { BodyHit hit = Sample(along, across, stratY, lenses[i]); if (hit.Inside && (!bestLens.Inside || hit.Radius < bestLens.Radius)) bestLens = hit; if (hit.Halo) nearLens = true; }

        if (bestLens.Inside)
        {
            double reductant = Clamp(1 - (stratY + 10) / 18, 0, 1);
            double grade = 1 - bestLens.Radius + .32 * reductant + (bestBreccia.Inside ? .35 : 0) + .12 * field;
            MessinianSulfurZone zone = grade > .82 ? MessinianSulfurZone.HighGradeSulfur
                : grade > .43 ? MessinianSulfurZone.DisseminatedSulfur
                : bestLens.Radius < .91 ? (field > .55 ? MessinianSulfurZone.Celestine : MessinianSulfurZone.CalciteAragonite)
                : MessinianSulfurZone.AuthigenicCarbonate;
            return new(zone, true, bestBreccia.Inside, bestLens.Radius, grade, stratY);
        }
        if (nearLens) return new(MessinianSulfurZone.AuthigenicCarbonate, false, bestBreccia.Inside, double.PositiveInfinity, 0, stratY);
        if (bestBreccia.Inside)
        {
            MessinianSulfurZone zone = bestBreccia.Radius < .72 && field > .3 ? MessinianSulfurZone.AuthigenicCarbonate
                : field > 1.02 ? MessinianSulfurZone.CalciteAragonite : MessinianSulfurZone.EvaporiteBreccia;
            return new(zone, false, true, double.PositiveInfinity, 0, stratY);
        }

        // Retain the evaporite interval, but narrow the primary gypsum branch to approximately one voxel.
        if (stratY > -10 && stratY <= 9)
        {
            double phase = (stratY + seed) * .7;
            bool primary = Math.Sin(phase) > .08;
            if (!primary) return new(MessinianSulfurZone.BurialGypsum, false, false, double.PositiveInfinity, 0, stratY);
            double nearestBand = Math.Abs(Math.Asin(Clamp(Math.Sin(phase), -1, 1))) / .7;
            if (nearestBand <= primaryGypsumHalfThickness) return new(MessinianSulfurZone.PrimaryGypsum, false, false, double.PositiveInfinity, 0, stratY);
        }
        return default;
    }

    private void Local(double x, double z, out double along, out double across) { double dx = x - basinX, dz = z - basinZ; along = dx * cosStrike - dz * sinStrike; across = dx * sinStrike + dz * cosStrike; }
    private double StratigraphicY(double along, double across, double y) => y - tanDip * across - Math.Sin(along * .055 + seed * .4) * 1.15 - .55 * Math.Cos(across * .09 - seed * .2);
    private static BodyHit Sample(double along, double across, double stratY, Body body) { double u = (along - body.Along) / body.RadiusAlong, v = (across - body.Across) / body.RadiusAcross, w = (stratY - body.Level) / body.RadiusVertical; double warp = .1 * Math.Sin(u * 4 + body.Phase) * Math.Cos(v * 3.1) + .04 * Math.Sin(w * 5 - body.Phase * .3); double r = Math.Sqrt(u * u + v * v + w * w) + warp; return new(r <= 1, r <= 1.32, r); }
    private static double Square(double value) => value * value;
    private static double Clamp(double value, double min, double max) => Math.Max(min, Math.Min(max, value));
    private readonly record struct Body(double Along, double Across, double Level, double RadiusAlong, double RadiusAcross, double RadiusVertical, double Phase);
    private readonly record struct BodyHit(bool Inside, bool Halo, double Radius);
}

internal sealed class MessinianSulfurProceduralTemplate : IProceduralDepositTemplate
{
    private static readonly string[] RequiredSlots = [ProceduralMaterialSlots.Sulfur, ProceduralMaterialSlots.AuthigenicCarbonate, ProceduralMaterialSlots.Gypsum, ProceduralMaterialSlots.Breccia, ProceduralMaterialSlots.Celestine, ProceduralMaterialSlots.CelestineGem];
    public string Code => "messinianSulfur";
    public IReadOnlyList<string> RequiredMaterialSlots => RequiredSlots;
    public bool RequiresPlan => true;
    public int GetMaximumHorizontalReach(ProceduralDepositDefinition definition) => definition.MessinianSulfur.HorizontalRadius;
    public object CreatePlan(in ProceduralDepositInstance instance, ProceduralDepositDefinition definition) => MessinianSulfurPlan.Create(instance, definition.MessinianSulfur);
    public bool Validate(ProceduralDepositDefinition definition, out string error)
    {
        MessinianSulfurDefinition s = definition.MessinianSulfur;
        bool valid = s.HorizontalRadius >= 32 && s.VerticalHalfHeight >= 25 && s.LensMin >= 1 && s.LensMax >= s.LensMin && s.BrecciaMin >= 1 && s.BrecciaMax >= s.BrecciaMin && s.BasinRadiusAlongMax >= s.BasinRadiusAlongMin && s.BasinRadiusAcrossMax >= s.BasinRadiusAcrossMin && s.PrimaryGypsumHalfThickness > 0 && s.PrimaryGypsumHalfThickness <= 1 && s.CelestineGemFraction >= 0 && s.CelestineGemFraction <= 1;
        error = valid ? string.Empty : "invalid Messinian sulfur settings"; return valid;
    }
    public void Realize(ProceduralDepositWorldGenSystem system, in DepositCandidate candidate, IChunkColumnGenerateRequest request, int baseX, int baseZ) => system.RealizeMessinianSulfurCandidate(candidate, request, baseX, baseZ);
}

public sealed partial class ProceduralDepositWorldGenSystem
{
    private const ulong CelestineGemSalt = 0x43454C455354474DUL;
    private const ulong SulfurDensitySalt = 0x53554C4644454E53UL;
    private const ulong CarbonateDensitySalt = 0x4341524244454E53UL;

    internal void RealizeMessinianSulfurCandidate(DepositCandidate candidate, IChunkColumnGenerateRequest request, int baseX, int baseZ)
    {
        CompiledProceduralDeposit compiled = candidate.Compiled;
        MessinianSulfurDefinition settings = compiled.Definition.MessinianSulfur;
        MessinianSulfurPlan plan = candidate.MessinianSulfurPlan;
        ushort[] heightMap = request.Chunks[0].MapChunk.WorldGenTerrainHeightMap;
        int worldHeight = serverApi!.World.BlockAccessor.MapSizeY;
        int minimumY = GameMath.Clamp(candidate.Instance.CenterY - settings.VerticalHalfHeight, 1, worldHeight - 2);
        int maximumPrototypeY = GameMath.Clamp(candidate.Instance.CenterY + settings.VerticalHalfHeight, 1, worldHeight - 2);

        int sulfurSlot = compiled.GetSlotId(ProceduralMaterialSlots.Sulfur);
        int carbonateSlot = compiled.GetSlotId(ProceduralMaterialSlots.AuthigenicCarbonate);
        int gypsumSlot = compiled.GetSlotId(ProceduralMaterialSlots.Gypsum);
        int brecciaSlot = compiled.GetSlotId(ProceduralMaterialSlots.Breccia);
        int celestineSlot = compiled.GetSlotId(ProceduralMaterialSlots.Celestine);
        int celestineGemSlot = compiled.GetSlotId(ProceduralMaterialSlots.CelestineGem);

        for (int localX = 0; localX < ChunkSize; localX++)
        {
            int worldX = baseX + localX;
            for (int localZ = 0; localZ < ChunkSize; localZ++)
            {
                int worldZ = baseZ + localZ;
                int maximumY = Math.Min(heightMap[localZ * ChunkSize + localX], maximumPrototypeY);
                for (int y = minimumY; y <= maximumY; y++)
                {
                    MessinianSulfurSample sample = plan.Evaluate(worldX, y, worldZ);
                    if (sample.Zone == MessinianSulfurZone.None) continue;

                    int slot;
                    double density = 1.0;
                    ulong densitySalt = 0;
                    switch (sample.Zone)
                    {
                        case MessinianSulfurZone.HighGradeSulfur:
                            slot = sulfurSlot;
                            break;
                        case MessinianSulfurZone.DisseminatedSulfur:
                            slot = sulfurSlot;
                            density = .45;
                            densitySalt = SulfurDensitySalt;
                            break;
                        case MessinianSulfurZone.AuthigenicCarbonate:
                            slot = carbonateSlot;
                            break;
                        case MessinianSulfurZone.PrimaryGypsum:
                        case MessinianSulfurZone.BurialGypsum:
                            slot = gypsumSlot;
                            break;
                        case MessinianSulfurZone.EvaporiteBreccia:
                            slot = brecciaSlot;
                            break;
                        case MessinianSulfurZone.CalciteAragonite:
                            slot = carbonateSlot;
                            density = .35;
                            densitySalt = CarbonateDensitySalt;
                            break;
                        case MessinianSulfurZone.Celestine:
                            bool gem = ProceduralDepositMath.CoordinateNoise(candidate.Instance.FeatureId, worldX, y, worldZ, CelestineGemSalt) < settings.CelestineGemFraction;
                            slot = gem ? celestineGemSlot : celestineSlot;
                            break;
                        default:
                            continue;
                    }

                    if (density < 1 && ProceduralDepositMath.CoordinateNoise(candidate.Instance.FeatureId, worldX, y, worldZ, densitySalt) >= density) continue;
                    int chunkY = y / ChunkSize;
                    if ((uint)chunkY >= (uint)request.Chunks.Length) continue;
                    int index3d = ((y % ChunkSize) * ChunkSize + localZ) * ChunkSize + localX;
                    IChunkBlocks data = request.Chunks[chunkY].Data;
                    int hostBlockId = data.GetBlockIdUnsafe(index3d);
                    if (!CanReplaceWithProceduralRock(compiled, hostBlockId)) continue;
                    int placeBlockId = compiled.ResolveBlock(slot, hostBlockId);
                    if (placeBlockId == 0 || placeBlockId == hostBlockId) continue;
                    data.SetBlockUnsafe(index3d, placeBlockId);
                    AdvancedGeology.Byproducts.ByproductSystem.RecordPlacement(request.Chunks[chunkY], index3d, placeBlockId, candidate.Instance.FeatureId, compiled);
                    data.SetFluid(index3d, 0);
                }
            }
        }
    }
}
