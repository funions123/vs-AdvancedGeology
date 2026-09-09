using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using Vintagestory.API.Server;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace AdvancedGeology.WorldGen.ProceduralDeposits;

public enum PorphyryCopperMolyZone
{
    None = 0,
    Bornite,
    Chalcopyrite,
    Molybdenite,
    Chalcocite,
    Quartz,
    Pyrite,
    Gossan,
    Limonite,
    Potassic,
    Phyllic,
    Propylitic
}

[JsonObject(MemberSerialization.OptIn)]
public sealed class PorphyryCopperMolyDefinition
{
    [JsonProperty] public int HorizontalRadius { get; set; } = 36;
    [JsonProperty] public int VerticalHalfHeight { get; set; } = 30;
    [JsonProperty] public int SupergeneDepth { get; set; } = 8;
    [JsonProperty] public double CenterSpread { get; set; } = 4.0;
    [JsonProperty] public double RadiusXMin { get; set; } = 18.0;
    [JsonProperty] public double RadiusXMax { get; set; } = 23.0;
    [JsonProperty] public double RadiusZMin { get; set; } = 16.0;
    [JsonProperty] public double RadiusZMax { get; set; } = 21.0;
    [JsonProperty] public double BottomMin { get; set; } = -29.0;
    [JsonProperty] public double BottomMax { get; set; } = -24.0;
    [JsonProperty] public double TopMin { get; set; } = 17.0;
    [JsonProperty] public double TopMax { get; set; } = 22.0;
    [JsonProperty] public double SupergeneChance { get; set; } = 0.68;
    [JsonProperty] public int VeinletCountMin { get; set; } = 28;
    [JsonProperty] public int VeinletCountMax { get; set; } = 40;
}

public readonly record struct PorphyryCopperMolySample(
    PorphyryCopperMolyZone Zone,
    int Grade = 0,
    bool InVein = false,
    bool Disseminated = false,
    bool Weathered = false,
    double StockRadius = 0.0,
    double StockVertical = 0.0);

/// <summary>
/// Bornite, chalcopyrite, molybdenite, pyrite, quartz, chalcocite, limonite, and gossan form a stock-centered vein stockwork with vertically and radially zoned mineralization.
/// </summary>
internal sealed class PorphyryCopperMolyPlan
{
    private const ulong PlanSalt = 0x504F525043554D4FUL; // "PORPCUMO"
    private const double FloorY = -30.0;

    private readonly int originX;
    private readonly int originY;
    private readonly int originZ;
    private readonly double centerX;
    private readonly double centerZ;
    private readonly double radiusX;
    private readonly double radiusZ;
    private readonly double bottom;
    private readonly double top;
    private readonly double phase;
    private readonly bool hasInternalSupergene;
    private readonly int supergeneDepth;
    private readonly Veinlet[] veinlets;

    public int VeinletCount => veinlets.Length;
    public bool HasInternalSupergene => hasInternalSupergene;
    public double Phase => phase;

    private PorphyryCopperMolyPlan(
        in ProceduralDepositInstance instance,
        double centerX,
        double centerZ,
        double radiusX,
        double radiusZ,
        double bottom,
        double top,
        double phase,
        bool hasInternalSupergene,
        int supergeneDepth,
        Veinlet[] veinlets)
    {
        originX = instance.CenterX;
        originY = instance.CenterY;
        originZ = instance.CenterZ;
        this.centerX = centerX;
        this.centerZ = centerZ;
        this.radiusX = radiusX;
        this.radiusZ = radiusZ;
        this.bottom = bottom;
        this.top = top;
        this.phase = phase;
        this.hasInternalSupergene = hasInternalSupergene;
        this.supergeneDepth = supergeneDepth;
        this.veinlets = veinlets;
    }

    public static PorphyryCopperMolyPlan Create(
        in ProceduralDepositInstance instance,
        PorphyryCopperMolyDefinition settings)
    {
        var random = new ProceduralDepositRandom(instance.FeatureId ^ PlanSalt);
        double centerX = random.Range(-settings.CenterSpread, settings.CenterSpread);
        double centerZ = random.Range(-settings.CenterSpread, settings.CenterSpread);
        double radiusX = random.Range(settings.RadiusXMin, settings.RadiusXMax);
        double radiusZ = random.Range(settings.RadiusZMin, settings.RadiusZMax);
        double bottom = random.Range(settings.BottomMin, settings.BottomMax);
        double top = random.Range(settings.TopMin, settings.TopMax);
        double phase = random.Range(0.0, 100.0);
        bool internalSupergene = random.Range(0.0, 1.0) < settings.SupergeneChance;

        int count = random.NextInt(settings.VeinletCountMin, settings.VeinletCountMax);
        var veinlets = new Veinlet[count];
        for (int i = 0; i < count; i++)
        {
            double strike = random.Range(0.0, Math.PI);
            double dip = random.Range(55.0, 88.0) * Math.PI / 180.0;
            veinlets[i] = new Veinlet(
                Math.Cos(strike),
                Math.Sin(strike),
                Math.Sin(dip),
                Math.Cos(dip),
                random.Range(-15.0, 15.0),
                random.Range(-8.0, 8.0),
                random.Range(-7.0, 5.0),
                random.Range(10.0, 24.0),
                random.Range(13.0, 26.0),
                phase + i * 13.0,
                random.Range(0.18, 0.40));
        }

        return new PorphyryCopperMolyPlan(
            instance, centerX, centerZ, radiusX, radiusZ, bottom, top, phase,
            internalSupergene, settings.SupergeneDepth, veinlets);
    }

    public PorphyryCopperMolySample Evaluate(
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

        StockSample stock = SampleStock(x, y, z);
        if (!stock.Inside) return default;

        int depth = surfaceY < int.MaxValue ? surfaceY - worldY + 1 : int.MaxValue;
        VeinSample vein = SampleStockwork(x, y, z);
        double oreField = Math.Sin(x * 0.17 - z * 0.12 + phase)
            + 0.55 * Math.Cos(y * 0.19 + x * 0.08);
        double deepBias = Clamp((0.56 - stock.Vertical) / 0.36, 0.0, 1.0);

        PorphyryCopperMolyZone zone;
        bool disseminated = false;
        if (vein.Inside)
        {
            if (hasInternalSupergene && stock.Vertical > 0.62 && depth > 5 && depth < 15 && oreField > 0.15)
            {
                zone = PorphyryCopperMolyZone.Chalcocite;
            }
            else if (vein.Core && deepBias > 0.45 && stock.Radius < 0.62 && oreField > 0.35)
            {
                zone = PorphyryCopperMolyZone.Molybdenite;
            }
            else if (stock.Radius < 0.48 && oreField > 0.35)
            {
                zone = PorphyryCopperMolyZone.Bornite;
            }
            else if (stock.Radius < 0.78 && oreField > -0.2)
            {
                zone = PorphyryCopperMolyZone.Chalcopyrite;
            }
            else if (stock.Radius > 0.58 && oreField > 0.25)
            {
                zone = PorphyryCopperMolyZone.Pyrite;
            }
            else
            {
                zone = PorphyryCopperMolyZone.Quartz;
            }
        }
        else
        {
            double dissemination = Math.Sin(x * 0.22 + z * 0.18 + phase * 0.6)
                + 0.45 * Math.Cos(y * 0.25 - z * 0.09);
            if (stock.Radius < 0.5 && dissemination > 1.25)
            {
                zone = deepBias > 0.55
                    ? PorphyryCopperMolyZone.Molybdenite
                    : PorphyryCopperMolyZone.Bornite;
                disseminated = true;
            }
            else if (stock.Radius < 0.78 && dissemination > 1.32)
            {
                zone = PorphyryCopperMolyZone.Chalcopyrite;
                disseminated = true;
            }
            else if (stock.Radius < 0.5) zone = PorphyryCopperMolyZone.Potassic;
            else if (stock.Radius < 0.8) zone = PorphyryCopperMolyZone.Phyllic;
            else zone = dissemination > -0.55
                ? PorphyryCopperMolyZone.Propylitic
                : PorphyryCopperMolyZone.None;
        }

        return Finish(zone, x, y, z, depth, vein.Inside, disseminated, stock, weatheringEnabled);
    }

    private PorphyryCopperMolySample Finish(
        PorphyryCopperMolyZone hypogene,
        double x,
        double y,
        double z,
        int depth,
        bool inVein,
        bool disseminated,
        in StockSample stock,
        bool weatheringEnabled)
    {
        PorphyryCopperMolyZone zone = hypogene;
        bool weathered = false;
        int forcedGrade = -1;

        if (weatheringEnabled && depth >= 1 && depth <= supergeneDepth)
        {
            bool copper = zone == PorphyryCopperMolyZone.Bornite
                || zone == PorphyryCopperMolyZone.Chalcopyrite
                || zone == PorphyryCopperMolyZone.Chalcocite;
            if (depth <= 3 && (copper || zone == PorphyryCopperMolyZone.Pyrite))
            {
                zone = PorphyryCopperMolyZone.Gossan;
                weathered = true;
                forcedGrade = 0;
            }
            else if (depth >= 4 && copper)
            {
                zone = PorphyryCopperMolyZone.Chalcocite;
                weathered = true;
                forcedGrade = depth is 6 or 7 ? 3 : 1;
            }
            else if (depth >= 4 && zone == PorphyryCopperMolyZone.Pyrite)
            {
                zone = PorphyryCopperMolyZone.Limonite;
                weathered = true;
                forcedGrade = 1;
            }
        }

        int grade = forcedGrade >= 0 ? forcedGrade : SelectGrade(zone, x, y, z, inVein, disseminated);
        return new PorphyryCopperMolySample(zone, grade, inVein, disseminated, weathered, stock.Radius, stock.Vertical);
    }

    private int SelectGrade(
        PorphyryCopperMolyZone zone,
        double x,
        double y,
        double z,
        bool inVein,
        bool disseminated)
    {
        if (!IsGraded(zone)) return 0;
        double noise = Hash3D(x * 2.7 + phase * 1.3, y * 2.7, z * 2.7);
        int grade = noise > 0.82 ? 3 : noise > 0.55 ? 2 : noise > 0.25 ? 1 : 0;
        if (zone is PorphyryCopperMolyZone.Molybdenite or PorphyryCopperMolyZone.Bornite)
        {
            grade = Math.Min(3, grade + 1);
        }
        if (disseminated) grade = Math.Min(2, Math.Max(1, grade));
        return grade;
    }

    internal static bool IsGraded(PorphyryCopperMolyZone zone) =>
        zone is PorphyryCopperMolyZone.Bornite
            or PorphyryCopperMolyZone.Chalcopyrite
            or PorphyryCopperMolyZone.Molybdenite
            or PorphyryCopperMolyZone.Chalcocite
            or PorphyryCopperMolyZone.Limonite;

    private StockSample SampleStock(double x, double y, double z)
    {
        double vertical = (y - bottom) / (top - bottom);
        if (vertical < 0.0 || vertical > 1.0) return new StockSample(false, double.PositiveInfinity, vertical);
        double taper = 0.72 + 0.28 * Math.Sin(Math.PI * vertical);
        double u = (x - centerX) / (radiusX * taper);
        double v = (z - centerZ) / (radiusZ * taper);
        double warp = Math.Sin(u * 3.5 + phase) * Math.Cos(v * 3.0) * 0.06;
        double radius = Math.Sqrt(u * u + v * v) + warp;
        return new StockSample(radius <= 1.0, radius, vertical);
    }

    private VeinSample SampleStockwork(double x, double y, double z)
    {
        double best = double.PositiveInfinity;
        for (int i = 0; i < veinlets.Length; i++)
        {
            Veinlet vein = veinlets[i];
            double along = x * vein.CosStrike - z * vein.SinStrike;
            double vertical = y - vein.CenterY;
            double u = (along - vein.CenterAlong) / vein.Length;
            double v = vertical / vein.Height;
            double footprint = Math.Sqrt(u * u + v * v);
            if (footprint > 1.04) continue;
            double taper = Math.Sqrt(Clamp(1.0 - footprint * footprint, 0.0, 1.0));
            double across = x * vein.SinStrike + z * vein.CosStrike - vein.Offset;
            double signed = across * vein.SinDip + (y + 4.0) * vein.CosDip
                - Math.Sin(along * 0.16 + y * 0.07 + vein.Phase) * 0.65;
            double score = Math.Abs(signed) / Math.Max(0.08, vein.Width * taper);
            if (score < best) best = score;
        }
        return new VeinSample(best <= 1.0, best <= 0.42, best);
    }

    internal static double Hash3D(double x, double y, double z)
    {
        double n = Math.Sin(x * 12.9898 + y * 78.233 + z * 37.719) * 43758.5453;
        return n - Math.Floor(n);
    }

    private static double Clamp(double value, double min, double max) => Math.Max(min, Math.Min(max, value));

    private readonly record struct StockSample(bool Inside, double Radius, double Vertical);
    private readonly record struct VeinSample(bool Inside, bool Core, double Score);
    private readonly record struct Veinlet(
        double CosStrike, double SinStrike, double SinDip, double CosDip,
        double Offset, double CenterAlong, double CenterY, double Length, double Height,
        double Phase, double Width);
}

internal sealed class PorphyryCopperMolyProceduralTemplate : IProceduralDepositTemplate
{
    private static readonly string[] RequiredSlots =
    {
        ProceduralMaterialSlots.Bornite,
        ProceduralMaterialSlots.Chalcopyrite,
        ProceduralMaterialSlots.Molybdenite,
        ProceduralMaterialSlots.Chalcocite,
        ProceduralMaterialSlots.Quartz,
        ProceduralMaterialSlots.Pyrite,
        ProceduralMaterialSlots.Gossan,
        ProceduralMaterialSlots.Limonite
    };

    public string Code => "porphyryCopperMoly";
    public IReadOnlyList<string> RequiredMaterialSlots => RequiredSlots;
    public bool RequiresPlan => true;
    public int GetMaximumHorizontalReach(ProceduralDepositDefinition definition) => definition.PorphyryCopperMoly.HorizontalRadius;
    public object CreatePlan(in ProceduralDepositInstance instance, ProceduralDepositDefinition definition) =>
        PorphyryCopperMolyPlan.Create(instance, definition.PorphyryCopperMoly);

    public bool Validate(ProceduralDepositDefinition definition, out string error)
    {
        PorphyryCopperMolyDefinition s = definition.PorphyryCopperMoly;
        bool valid = s.HorizontalRadius >= 16
            && s.VerticalHalfHeight >= 8
            && s.SupergeneDepth >= 4
            && s.CenterSpread >= 0
            && s.RadiusXMin > 0 && s.RadiusXMax >= s.RadiusXMin
            && s.RadiusZMin > 0 && s.RadiusZMax >= s.RadiusZMin
            && s.BottomMax >= s.BottomMin
            && s.TopMax >= s.TopMin
            && s.TopMin > s.BottomMax
            && s.SupergeneChance >= 0 && s.SupergeneChance <= 1
            && s.VeinletCountMin >= 1 && s.VeinletCountMax >= s.VeinletCountMin;
        error = valid ? string.Empty : "invalid porphyry copper-moly settings";
        return valid;
    }

    public void Realize(ProceduralDepositWorldGenSystem system, in DepositCandidate candidate,
        IChunkColumnGenerateRequest request, int baseX, int baseZ) =>
        system.RealizePorphyryCopperMolyCandidate(candidate, request, baseX, baseZ);
}

public sealed partial class ProceduralDepositWorldGenSystem
{
    internal void RealizePorphyryCopperMolyCandidate(
        DepositCandidate candidate, IChunkColumnGenerateRequest request, int baseX, int baseZ)
    {
        CompiledProceduralDeposit compiled = candidate.Compiled;
        PorphyryCopperMolyDefinition settings = compiled.Definition.PorphyryCopperMoly;
        var plan = (PorphyryCopperMolyPlan)candidate.Plan!;
        ushort[] heightMap = request.Chunks[0].MapChunk.WorldGenTerrainHeightMap;
        int worldHeight = serverApi!.World.BlockAccessor.MapSizeY;
        int minimumY = GameMath.Clamp(candidate.Instance.CenterY - settings.VerticalHalfHeight, 1, worldHeight - 2);
        int maximumPrototypeY = GameMath.Clamp(candidate.Instance.CenterY + settings.VerticalHalfHeight, 1, worldHeight - 2);
        int[] slots = BuildPorphyryCopperMolySlots(compiled);
        int gossanSlot = compiled.GetSlotId(ProceduralMaterialSlots.Gossan);
        bool weathering = compiled.Definition.Weathering.Enabled;

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
                    PorphyryCopperMolySample sample = plan.Evaluate(worldX, y, worldZ, surfaceY, weathering);
                    if (sample.Zone == PorphyryCopperMolyZone.None) continue;
                    int targetSlot = slots[(int)sample.Zone];
                    if (targetSlot < 0) continue;
                    int chunkY = y / ChunkSize;
                    if ((uint)chunkY >= (uint)request.Chunks.Length) continue;
                    int localY = y % ChunkSize;
                    int index3d = ((localY * ChunkSize) + localZ) * ChunkSize + localX;
                    IChunkBlocks data = request.Chunks[chunkY].Data;
                    int hostBlockId = data.GetBlockIdUnsafe(index3d);
                    if (targetSlot == gossanSlot)
                    {
                        if (!CanPlaceWeatheredSoil(compiled, hostBlockId)) continue;
                    }
                    else if (!CanReplaceWithProceduralRock(compiled, hostBlockId)) continue;
                    int placeBlockId = targetSlot == gossanSlot
                        ? compiled.ResolveWeatheredBlock(targetSlot, sample.Grade, hostBlockId, y < surfaceY)
                        : compiled.ResolveBlock(targetSlot, sample.Grade, hostBlockId);
                    if (placeBlockId == 0 || placeBlockId == hostBlockId) continue;
                    data.SetBlockUnsafe(index3d, placeBlockId);
                    data.SetFluid(index3d, 0);
                }
            }
        }
    }

    private static int[] BuildPorphyryCopperMolySlots(CompiledProceduralDeposit compiled)
    {
        var slots = new int[Enum.GetValues<PorphyryCopperMolyZone>().Length];
        Array.Fill(slots, -1);
        slots[(int)PorphyryCopperMolyZone.Bornite] = compiled.GetSlotId(ProceduralMaterialSlots.Bornite);
        slots[(int)PorphyryCopperMolyZone.Chalcopyrite] = compiled.GetSlotId(ProceduralMaterialSlots.Chalcopyrite);
        slots[(int)PorphyryCopperMolyZone.Molybdenite] = compiled.GetSlotId(ProceduralMaterialSlots.Molybdenite);
        slots[(int)PorphyryCopperMolyZone.Chalcocite] = compiled.GetSlotId(ProceduralMaterialSlots.Chalcocite);
        slots[(int)PorphyryCopperMolyZone.Quartz] = compiled.GetSlotId(ProceduralMaterialSlots.Quartz);
        slots[(int)PorphyryCopperMolyZone.Pyrite] = compiled.GetSlotId(ProceduralMaterialSlots.Pyrite);
        slots[(int)PorphyryCopperMolyZone.Gossan] = compiled.GetSlotId(ProceduralMaterialSlots.Gossan);
        slots[(int)PorphyryCopperMolyZone.Limonite] = compiled.GetSlotId(ProceduralMaterialSlots.Limonite);
        return slots;
    }
}
