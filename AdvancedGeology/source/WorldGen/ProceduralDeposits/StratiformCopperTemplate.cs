using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using Vintagestory.API.Server;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace AdvancedGeology.WorldGen.ProceduralDeposits;

public enum StratiformCopperZone
{
    None = 0,
    NativeCopper,
    Chalcocite,
    Bornite,
    Chalcopyrite,
    Malachite,
    Azurite,
    Pyrite,
    Carbonate,
    Gossan,
    Limonite,
    BleachedSandstone
}

[JsonObject(MemberSerialization.OptIn)]
public sealed class StratiformCopperDefinition
{
    [JsonProperty] public int HorizontalRadius { get; set; } = 38;
    [JsonProperty] public int VerticalHalfHeight { get; set; } = 30;
    [JsonProperty] public int SupergeneDepth { get; set; } = 8;
    [JsonProperty] public double StrikeMinDeg { get; set; } = 0.0;
    [JsonProperty] public double StrikeMaxDeg { get; set; } = 180.0;
    [JsonProperty] public double BedDipMinDeg { get; set; } = 1.5;
    [JsonProperty] public double BedDipMaxDeg { get; set; } = 7.0;
    [JsonProperty] public double FrontOffsetMin { get; set; } = -6.0;
    [JsonProperty] public double FrontOffsetMax { get; set; } = 6.0;
    [JsonProperty] public int LensCountMin { get; set; } = 1;
    [JsonProperty] public int LensCountMax { get; set; } = 3;
    [JsonProperty] public int FaultCountMin { get; set; } = 1;
    [JsonProperty] public int FaultCountMax { get; set; } = 3;
}

public readonly record struct StratiformCopperSample(
    StratiformCopperZone Zone,
    int Grade = 0,
    bool InLens = false,
    bool InFaultCore = false,
    bool InHalo = false,
    bool Weathered = false,
    double Redox = 0.0);

/// <summary>
/// Native copper, chalcocite, bornite, chalcopyrite, pyrite, carbonate, and secondary copper minerals form finite bedding-parallel lenses along a sinuous redox front cut by faults.
/// </summary>
internal sealed class StratiformCopperPlan
{
    private const ulong PlanSalt = 0x5354524154434F50UL; // "STRATCOP"
    private const double FloorY = -30.0;

    private readonly int originX;
    private readonly int originY;
    private readonly int originZ;
    private readonly double cosStrike;
    private readonly double sinStrike;
    private readonly double tanDip;
    private readonly double frontOffset;
    private readonly double seed;
    private readonly int supergeneDepth;
    private readonly Lens[] lenses;
    private readonly Fault[] faults;

    public int LensCount => lenses.Length;
    public int FaultCount => faults.Length;
    public double Seed => seed;

    private StratiformCopperPlan(
        in ProceduralDepositInstance instance,
        double strikeDeg,
        double dipDeg,
        double frontOffset,
        double seed,
        int supergeneDepth,
        Lens[] lenses,
        Fault[] faults)
    {
        originX = instance.CenterX;
        originY = instance.CenterY;
        originZ = instance.CenterZ;
        double strike = strikeDeg * Math.PI / 180.0;
        cosStrike = Math.Cos(strike);
        sinStrike = Math.Sin(strike);
        tanDip = Math.Tan(dipDeg * Math.PI / 180.0);
        this.frontOffset = frontOffset;
        this.seed = seed;
        this.supergeneDepth = supergeneDepth;
        this.lenses = lenses;
        this.faults = faults;
    }

    public static StratiformCopperPlan Create(
        in ProceduralDepositInstance instance,
        StratiformCopperDefinition settings)
    {
        var random = new ProceduralDepositRandom(instance.FeatureId ^ PlanSalt);
        double strikeDeg = random.Range(settings.StrikeMinDeg, settings.StrikeMaxDeg);
        double dipDeg = random.Range(settings.BedDipMinDeg, settings.BedDipMaxDeg);
        double frontOffset = random.Range(settings.FrontOffsetMin, settings.FrontOffsetMax);
        double seed = random.Range(0.0, 100.0);
        int count = random.NextInt(settings.LensCountMin, settings.LensCountMax);
        double scale = Math.Sqrt(2.0 / count);
        var lenses = new Lens[count];
        for (int i = 0; i < count; i++)
        {
            double centered = i - (count - 1) / 2.0;
            double centerStratY = random.Range(-7.0, -3.0)
                + (i % 2) * random.Range(1.0, 2.5);
            lenses[i] = new Lens(
                centered * random.Range(12.0, 18.0) + random.Range(-3.0, 3.0),
                frontOffset + random.Range(-4.0, 4.0),
                centerStratY,
                random.Range(15.0, 23.0) * scale,
                random.Range(8.0, 13.0) * scale,
                random.Range(1.3, 2.5),
                seed + i * 23.0);
        }

        int faultCount = random.NextInt(settings.FaultCountMin, settings.FaultCountMax);
        var faults = new Fault[faultCount];
        for (int i = 0; i < faultCount; i++)
        {
            double side = random.Range(0.0, 1.0) < 0.5 ? -1.0 : 1.0;
            double strike = (strikeDeg + random.Range(45.0, 85.0) * side) * Math.PI / 180.0;
            double dip = random.Range(60.0, 82.0) * Math.PI / 180.0;
            faults[i] = new Fault(
                Math.Cos(strike), Math.Sin(strike), Math.Sin(dip), Math.Cos(dip),
                random.Range(-10.0, 10.0), -5.0,
                random.Range(15.0, 28.0), random.Range(18.0, 28.0),
                seed + 71.0 + i * 19.0);
        }

        return new StratiformCopperPlan(instance, strikeDeg, dipDeg, frontOffset, seed,
            settings.SupergeneDepth, lenses, faults);
    }

    public StratiformCopperSample Evaluate(
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

        Local local = GetLocal(x, z);
        double stratY = y - tanDip * local.Across - Math.Sin(local.Along * 0.06 + seed) * 1.1;
        if (stratY > 9.0 || stratY < -18.0) return default;

        double front = frontOffset + Math.Sin(local.Along * 0.075 + seed) * 3.0
            + Math.Cos(local.Along * 0.031 - seed * 0.4) * 1.5;
        double redox = local.Across - front;

        LensSample? best = null;
        bool inHalo = false;
        for (int i = 0; i < lenses.Length; i++)
        {
            LensSample sample = SampleLens(local, stratY, lenses[i]);
            inHalo |= sample.Halo;
            if (sample.Inside && (!best.HasValue || sample.Score < best.Value.Score)) best = sample;
        }

        bool faultCore = false;
        bool faultHalo = false;
        for (int i = 0; i < faults.Length; i++)
        {
            FaultSample sample = SampleFault(x, y, z, faults[i]);
            faultCore |= sample.Core;
            faultHalo |= sample.Halo;
        }

        double replacement = Math.Sin(local.Along * 0.18 + stratY * 0.13 + seed)
            + 0.55 * Math.Cos(local.Across * 0.22 - stratY * 0.17);
        StratiformCopperZone zone = StratiformCopperZone.None;
        if (best.HasValue && replacement > -0.42)
        {
            if (Hash3D(x * 2.1, y * 2.1, z * 2.1) > 0.985 && stratY > -4.0)
            {
    // Model combines malachite and azurite; retain both with a stable 85/15 split.
                zone = Hash3D(x * 3.7 + seed, y * 3.7, z * 3.7) < 0.85
                    ? StratiformCopperZone.Malachite
                    : StratiformCopperZone.Azurite;
            }
            else if (replacement < -0.05) zone = StratiformCopperZone.Carbonate;
            else
            {
                double enhanced = faultCore ? 0.9 : 0.0;
                if (redox < -6.0 + enhanced)
                {
                    zone = replacement > 1.05
                        ? StratiformCopperZone.NativeCopper
                        : StratiformCopperZone.Chalcocite;
                }
                else if (redox < -1.5 + enhanced) zone = StratiformCopperZone.Bornite;
                else if (redox < 4.5 + enhanced) zone = StratiformCopperZone.Chalcopyrite;
                else zone = StratiformCopperZone.Pyrite;
            }
        }
        else if ((inHalo || faultHalo) && Math.Sin(x * 0.16 + z * 0.12 + seed) > -0.35)
        {
            zone = StratiformCopperZone.BleachedSandstone;
        }

        int depth = surfaceY < int.MaxValue ? surfaceY - worldY + 1 : int.MaxValue;
        return Finish(zone, x, y, z, depth, best.HasValue, faultCore, inHalo || faultHalo,
            redox, weatheringEnabled);
    }

    private StratiformCopperSample Finish(
        StratiformCopperZone hypogene,
        double x,
        double y,
        double z,
        int depth,
        bool inLens,
        bool faultCore,
        bool inHalo,
        double redox,
        bool weatheringEnabled)
    {
        StratiformCopperZone zone = hypogene;
        bool weathered = false;
        int forcedGrade = -1;
        bool copperSulfide = zone is StratiformCopperZone.Chalcocite
            or StratiformCopperZone.Bornite or StratiformCopperZone.Chalcopyrite;

        if (weatheringEnabled && depth >= 1 && depth <= supergeneDepth)
        {
            if (depth <= 3 && (copperSulfide || zone == StratiformCopperZone.Pyrite))
            {
                zone = StratiformCopperZone.Gossan;
                forcedGrade = 0;
                weathered = true;
            }
            else if (depth >= 4 && copperSulfide)
            {
                zone = StratiformCopperZone.Chalcocite;
                forcedGrade = depth is 6 or 7 ? 3 : 1;
                weathered = true;
            }
            else if (depth >= 4 && zone == StratiformCopperZone.Pyrite)
            {
                zone = StratiformCopperZone.Limonite;
                forcedGrade = 1;
                weathered = true;
            }
        }

        int grade = forcedGrade >= 0 ? forcedGrade : SelectGrade(zone, x, y, z);
        return new StratiformCopperSample(zone, grade, inLens, faultCore, inHalo, weathered, redox);
    }

    private int SelectGrade(StratiformCopperZone zone, double x, double y, double z)
    {
        if (!IsGraded(zone)) return 0;
        double noise = Hash3D(x * 2.7 + seed * 1.3, y * 2.7, z * 2.7);
        int grade = noise > 0.82 ? 3 : noise > 0.55 ? 2 : noise > 0.25 ? 1 : 0;
        return zone switch
        {
            StratiformCopperZone.NativeCopper => Math.Max(2, grade),
            StratiformCopperZone.Chalcopyrite => Math.Min(2, grade),
            StratiformCopperZone.Malachite or StratiformCopperZone.Azurite => Math.Min(1, grade),
            _ => grade
        };
    }

    internal static bool IsGraded(StratiformCopperZone zone) =>
        zone is StratiformCopperZone.NativeCopper
            or StratiformCopperZone.Chalcocite
            or StratiformCopperZone.Bornite
            or StratiformCopperZone.Chalcopyrite
            or StratiformCopperZone.Malachite
            or StratiformCopperZone.Azurite
            or StratiformCopperZone.Limonite;

    private Local GetLocal(double x, double z) =>
        new(x * cosStrike - z * sinStrike, x * sinStrike + z * cosStrike);

    private static LensSample SampleLens(in Local local, double stratY, in Lens lens)
    {
        double u = (local.Along - lens.CenterAlong) / lens.RadiusAlong;
        double v = (local.Across - lens.CenterAcross) / lens.RadiusAcross;
        double warp = Math.Sin(u * 4.0 + lens.Phase) * Math.Cos(v * 3.0) * 0.1;
        double footprint = Math.Sqrt(u * u + v * v) + warp;
        double taper = Clamp(1.0 - footprint * footprint, 0.0, 1.0);
        double half = 0.35 + lens.HalfThickness * taper;
        double distance = Math.Abs(stratY - lens.CenterStratY);
        return new LensSample(
            footprint <= 1.04 && distance <= half,
            footprint <= 1.22 && distance <= half + 1.4,
            footprint + distance / Math.Max(0.35, half));
    }

    private static FaultSample SampleFault(double x, double y, double z, in Fault fault)
    {
        double along = x * fault.CosStrike - z * fault.SinStrike;
        double across = x * fault.SinStrike + z * fault.CosStrike;
        double vertical = y - fault.CenterY;
        double u = along / fault.Length;
        double v = vertical / fault.Height;
        double footprint = Math.Sqrt(u * u + v * v);
        double distance = Math.Abs(across * fault.SinDip + vertical * fault.CosDip
            - fault.Offset - Math.Sin(along * 0.14 + fault.Phase));
        return new FaultSample(footprint <= 1.0 && distance <= 0.55,
            footprint <= 1.08 && distance <= 1.8);
    }

    internal static double Hash3D(double x, double y, double z)
    {
        double n = Math.Sin(x * 12.9898 + y * 78.233 + z * 37.719) * 43758.5453;
        return n - Math.Floor(n);
    }

    private static double Clamp(double value, double min, double max) => Math.Max(min, Math.Min(max, value));

    private readonly record struct Local(double Along, double Across);
    private readonly record struct LensSample(bool Inside, bool Halo, double Score);
    private readonly record struct FaultSample(bool Core, bool Halo);
    private readonly record struct Lens(double CenterAlong, double CenterAcross, double CenterStratY,
        double RadiusAlong, double RadiusAcross, double HalfThickness, double Phase);
    private readonly record struct Fault(double CosStrike, double SinStrike, double SinDip, double CosDip,
        double Offset, double CenterY, double Length, double Height, double Phase);
}

internal sealed class StratiformCopperProceduralTemplate : IProceduralDepositTemplate
{
    private static readonly string[] RequiredSlots =
    {
        ProceduralMaterialSlots.NativeCopper,
        ProceduralMaterialSlots.Chalcocite,
        ProceduralMaterialSlots.Bornite,
        ProceduralMaterialSlots.Chalcopyrite,
        ProceduralMaterialSlots.Malachite,
        ProceduralMaterialSlots.Azurite,
        ProceduralMaterialSlots.Pyrite,
        ProceduralMaterialSlots.Carbonate,
        ProceduralMaterialSlots.Gossan,
        ProceduralMaterialSlots.Limonite
    };

    public string Code => "stratiformCopper";
    public IReadOnlyList<string> RequiredMaterialSlots => RequiredSlots;
    public bool RequiresPlan => true;
    public int GetMaximumHorizontalReach(ProceduralDepositDefinition definition) => definition.StratiformCopper.HorizontalRadius;
    public object CreatePlan(in ProceduralDepositInstance instance, ProceduralDepositDefinition definition) =>
        StratiformCopperPlan.Create(instance, definition.StratiformCopper);

    public bool Validate(ProceduralDepositDefinition definition, out string error)
    {
        StratiformCopperDefinition s = definition.StratiformCopper;
        bool valid = s.HorizontalRadius >= 16
            && s.VerticalHalfHeight >= 8
            && s.SupergeneDepth >= 4
            && s.StrikeMaxDeg >= s.StrikeMinDeg
            && s.BedDipMinDeg >= 0 && s.BedDipMaxDeg >= s.BedDipMinDeg && s.BedDipMaxDeg < 90
            && s.FrontOffsetMax >= s.FrontOffsetMin
            && s.LensCountMin >= 1 && s.LensCountMax >= s.LensCountMin
            && s.FaultCountMin >= 1 && s.FaultCountMax >= s.FaultCountMin;
        error = valid ? string.Empty : "invalid stratiform copper settings";
        return valid;
    }

    public void Realize(ProceduralDepositWorldGenSystem system, in DepositCandidate candidate,
        IChunkColumnGenerateRequest request, int baseX, int baseZ) =>
        system.RealizeStratiformCopperCandidate(candidate, request, baseX, baseZ);
}

public sealed partial class ProceduralDepositWorldGenSystem
{
    internal void RealizeStratiformCopperCandidate(
        DepositCandidate candidate, IChunkColumnGenerateRequest request, int baseX, int baseZ)
    {
        CompiledProceduralDeposit compiled = candidate.Compiled;
        StratiformCopperDefinition settings = compiled.Definition.StratiformCopper;
        var plan = (StratiformCopperPlan)candidate.Plan!;
        ushort[] heightMap = request.Chunks[0].MapChunk.WorldGenTerrainHeightMap;
        int worldHeight = serverApi!.World.BlockAccessor.MapSizeY;
        int minimumY = GameMath.Clamp(candidate.Instance.CenterY - settings.VerticalHalfHeight, 1, worldHeight - 2);
        int maximumPrototypeY = GameMath.Clamp(candidate.Instance.CenterY + settings.VerticalHalfHeight, 1, worldHeight - 2);
        int[] slots = BuildStratiformCopperSlots(compiled);
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
                    StratiformCopperSample sample = plan.Evaluate(worldX, y, worldZ, surfaceY, weathering);
                    if (sample.Zone == StratiformCopperZone.None) continue;
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

    private static int[] BuildStratiformCopperSlots(CompiledProceduralDeposit compiled)
    {
        var slots = new int[Enum.GetValues<StratiformCopperZone>().Length];
        Array.Fill(slots, -1);
        slots[(int)StratiformCopperZone.NativeCopper] = compiled.GetSlotId(ProceduralMaterialSlots.NativeCopper);
        slots[(int)StratiformCopperZone.Chalcocite] = compiled.GetSlotId(ProceduralMaterialSlots.Chalcocite);
        slots[(int)StratiformCopperZone.Bornite] = compiled.GetSlotId(ProceduralMaterialSlots.Bornite);
        slots[(int)StratiformCopperZone.Chalcopyrite] = compiled.GetSlotId(ProceduralMaterialSlots.Chalcopyrite);
        slots[(int)StratiformCopperZone.Malachite] = compiled.GetSlotId(ProceduralMaterialSlots.Malachite);
        slots[(int)StratiformCopperZone.Azurite] = compiled.GetSlotId(ProceduralMaterialSlots.Azurite);
        slots[(int)StratiformCopperZone.Pyrite] = compiled.GetSlotId(ProceduralMaterialSlots.Pyrite);
        slots[(int)StratiformCopperZone.Carbonate] = compiled.GetSlotId(ProceduralMaterialSlots.Carbonate);
        slots[(int)StratiformCopperZone.Gossan] = compiled.GetSlotId(ProceduralMaterialSlots.Gossan);
        slots[(int)StratiformCopperZone.Limonite] = compiled.GetSlotId(ProceduralMaterialSlots.Limonite);
        return slots;
    }
}
