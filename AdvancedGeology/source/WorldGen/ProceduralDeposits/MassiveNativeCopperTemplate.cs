using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace AdvancedGeology.WorldGen.ProceduralDeposits;

[JsonObject(MemberSerialization.OptIn)]
public sealed class MassiveNativeCopperDefinition
{
    [JsonProperty] public int HorizontalRadius { get; set; } = 42;
    [JsonProperty] public int VerticalHalfHeight { get; set; } = 35;
    [JsonProperty] public int OxidationDepth { get; set; } = 8;
}

internal enum MassiveNativeCopperZone
{
    None,
    Massive,
    Stringer,
    Oxide,
    Amygdule,
    Breccia
}

internal readonly record struct MassiveNativeCopperSample(MassiveNativeCopperZone Zone, int Grade = 0);

/// <summary>Finite Keweenaw-style copper mass with attached fissure veins in basalt.</summary>
internal sealed class MassiveNativeCopperPlan
{
    private const ulong PlanSalt = 0x4D41535343555052UL;
    private readonly int centerX;
    private readonly int centerY;
    private readonly int centerZ;
    private readonly double cosStrike;
    private readonly double sinStrike;
    private readonly double tanDip;
    private readonly double seed;
    private readonly double phase;
    private readonly double massY;
    private readonly double radiusAlong;
    private readonly double radiusAcross;
    private readonly double radiusVertical;
    private readonly int oxidationDepth;
    private readonly Vein[] veins;

    public int VeinCount => veins.Length;

    private MassiveNativeCopperPlan(in ProceduralDepositInstance instance, double strike, double dip,
        double seed, double phase, double massY, double radiusAlong, double radiusAcross,
        double radiusVertical, int oxidationDepth, Vein[] veins)
    {
        centerX = instance.CenterX;
        centerY = instance.CenterY;
        centerZ = instance.CenterZ;
        double radians = strike * Math.PI / 180.0;
        cosStrike = Math.Cos(radians);
        sinStrike = Math.Sin(radians);
        tanDip = Math.Tan(dip * Math.PI / 180.0);
        this.seed = seed;
        this.phase = phase;
        this.massY = massY;
        this.radiusAlong = radiusAlong;
        this.radiusAcross = radiusAcross;
        this.radiusVertical = radiusVertical;
        this.oxidationDepth = oxidationDepth;
        this.veins = veins;
    }

    public static MassiveNativeCopperPlan Create(in ProceduralDepositInstance instance,
        MassiveNativeCopperDefinition settings)
    {
        var random = new ProceduralDepositRandom(instance.FeatureId ^ PlanSalt);
        double strike = random.Range(0, 180);
        double dip = random.Range(1.5, 6);
        double seed = random.Range(0, 100);
        double massY = random.Range(-7, -1);
        double along = random.Range(25, 33);
        double across = random.Range(11, 16);
        double vertical = random.Range(7, 11);
        double phase = seed + random.Range(0, 2 * Math.PI);
        int count = random.NextInt(2, 4);
        var veins = new Vein[count];
        for (int i = 0; i < count; i++)
        {
            double angle = (strike + random.Range(-28, 28)) * Math.PI / 180.0;
            double offset = random.Range(-10, 10);
            double y = massY + random.Range(-2, 3);
            double veinDip = random.Range(62, 82) * Math.PI / 180.0;
            veins[i] = new Vein(Math.Cos(angle), Math.Sin(angle), offset, y,
                Math.Sin(veinDip), Math.Cos(veinDip), random.Range(18, 30),
                random.Range(13, 22), seed + i * 31);
        }
        return new MassiveNativeCopperPlan(instance, strike, dip, seed, phase, massY, along,
            across, vertical, settings.OxidationDepth, veins);
    }

    public MassiveNativeCopperSample Evaluate(int worldX, int worldY, int worldZ, int surfaceY)
    {
        if (worldY > surfaceY || worldY < centerY - 35 || worldY > centerY + 35)
            return default;

        double x = worldX - centerX;
        double y = worldY - centerY;
        double z = worldZ - centerZ;
        if (Math.Abs(x) > 42 || Math.Abs(z) > 42) return default;
        double along = x * cosStrike - z * sinStrike;
        double across = x * sinStrike + z * cosStrike;
        double strat = y - tanDip * across - Math.Sin(along * 0.055 + seed) * 0.9;
        // The visualizer shows a conglomerate roof. Leave the actual roof unchanged;
        // only basalt voxels below its geometric boundary are eligible for replacement.
        if (strat > massY + radiusVertical + 7) return default;

        double u = (along + Math.Sin(phase) * 2) / radiusAlong;
        double v = (across + Math.Cos(phase) * 1.5) / radiusAcross;
        double w = (y - massY) / radiusVertical;
        double radius = Math.Sqrt(u * u + v * v + w * w)
            + Math.Sin(u * 3.2 + phase) * Math.Cos(v * 2.6) * 0.08;
        bool coreVein = false;
        bool haloVein = false;
        foreach (Vein vein in veins)
        {
            double veinAlong = x * vein.CosStrike - z * vein.SinStrike;
            double veinAcross = x * vein.SinStrike + z * vein.CosStrike - vein.Offset;
            double veinY = y - vein.Y;
            double footprint = Math.Sqrt(veinAlong * veinAlong / (vein.Length * vein.Length)
                + veinY * veinY / (vein.Height * vein.Height));
            double distance = Math.Abs(veinAcross * vein.SinDip + veinY * vein.CosDip
                - Math.Sin(veinAlong * 0.13 + vein.Phase));
            coreVein |= footprint < 1 && distance < 0.8;
            haloVein |= footprint < 1.1 && distance < 2.4;
        }

        int depth = surfaceY - worldY;
        if (radius <= 1.03)
        {
            if (depth >= 1 && depth <= oxidationDepth && Hash(x * 1.7, y * 1.7, z * 1.7) > 0.82)
                return new MassiveNativeCopperSample(MassiveNativeCopperZone.Oxide,
                    Hash(x * 2.7 + seed, y * 2.7, z * 2.7) > 0.5 ? 1 : 0);
            double grain = Hash(x * 1.3 + seed, y * 1.3, z * 1.3);
            if (radius <= 0.58 && grain > 0.14)
                return new MassiveNativeCopperSample(MassiveNativeCopperZone.Massive, 3);
            if (coreVein || grain > 0.34)
                return new MassiveNativeCopperSample(MassiveNativeCopperZone.Stringer,
                    grain > 0.76 ? 2 : grain > 0.45 ? 1 : 0);
            if (grain > 0.10) return new MassiveNativeCopperSample(MassiveNativeCopperZone.Amygdule,
                grain > 0.22 ? 1 : 0);
            return default; // Flow-top basalt remains the existing basalt block.
        }
        if (coreVein)
            return new MassiveNativeCopperSample(MassiveNativeCopperZone.Stringer,
                Hash(x * 1.3 + seed, y * 1.3, z * 1.3) > 0.7 ? 2 : 1);
        if (haloVein && Math.Sin(x * 0.15 + z * 0.12 + seed) > -0.25)
            return new MassiveNativeCopperSample(MassiveNativeCopperZone.Breccia);
        return default; // Outer flow-top halo and intact basalt remain in place.
    }

    private static double Hash(double x, double y, double z)
    {
        double value = Math.Sin(x * 12.9898 + y * 78.233 + z * 37.719) * 43758.5453;
        return value - Math.Floor(value);
    }

    private readonly record struct Vein(double CosStrike, double SinStrike, double Offset,
        double Y, double SinDip, double CosDip, double Length, double Height, double Phase);
}

internal sealed class MassiveNativeCopperProceduralTemplate : IProceduralDepositTemplate
{
    private static readonly string[] Slots =
    [
        ProceduralMaterialSlots.NativeCopper,
        ProceduralMaterialSlots.Malachite,
        ProceduralMaterialSlots.Azurite,
        ProceduralMaterialSlots.Pyrite,
        ProceduralMaterialSlots.Quartz,
        ProceduralMaterialSlots.Carbonate
    ];

    public string Code => "massiveNativeCopper";
    public IReadOnlyList<string> RequiredMaterialSlots => Slots;
    public bool RequiresPlan => true;
    public int GetMaximumHorizontalReach(ProceduralDepositDefinition definition) => definition.MassiveNativeCopper.HorizontalRadius;
    public object CreatePlan(in ProceduralDepositInstance instance, ProceduralDepositDefinition definition) =>
        MassiveNativeCopperPlan.Create(instance, definition.MassiveNativeCopper);
    public bool Validate(ProceduralDepositDefinition definition, out string error)
    {
        MassiveNativeCopperDefinition settings = definition.MassiveNativeCopper;
        bool valid = settings.HorizontalRadius == 42 && settings.VerticalHalfHeight == 35
            && settings.OxidationDepth >= 0 && settings.OxidationDepth <= 35;
        error = valid ? string.Empty : "massive native copper reach must cover the visualizer body and oxidation depth must be within its bounds";
        return valid;
    }

    public void Realize(ProceduralDepositWorldGenSystem system, in DepositCandidate candidate,
        IChunkColumnGenerateRequest request, int baseX, int baseZ) =>
        system.RealizeMassiveNativeCopperCandidate(candidate, request, baseX, baseZ);
}

public sealed partial class ProceduralDepositWorldGenSystem
{
    internal void RealizeMassiveNativeCopperCandidate(
        DepositCandidate candidate, IChunkColumnGenerateRequest request, int baseX, int baseZ)
    {
        CompiledProceduralDeposit compiled = candidate.Compiled;
        MassiveNativeCopperDefinition settings = compiled.Definition.MassiveNativeCopper;
        var plan = (MassiveNativeCopperPlan)candidate.Plan!;
        ushort[] heightMap = request.Chunks[0].MapChunk.WorldGenTerrainHeightMap;
        int worldHeight = serverApi!.World.BlockAccessor.MapSizeY;
        int minimumY = GameMath.Clamp(candidate.Instance.CenterY - settings.VerticalHalfHeight, 1, worldHeight - 2);
        int maximumPrototypeY = GameMath.Clamp(candidate.Instance.CenterY + settings.VerticalHalfHeight, 1, worldHeight - 2);
        int nativeSlot = compiled.GetSlotId(ProceduralMaterialSlots.NativeCopper);
        int malachiteSlot = compiled.GetSlotId(ProceduralMaterialSlots.Malachite);
        int azuriteSlot = compiled.GetSlotId(ProceduralMaterialSlots.Azurite);
        int quartzSlot = compiled.GetSlotId(ProceduralMaterialSlots.Quartz);
        int carbonateSlot = compiled.GetSlotId(ProceduralMaterialSlots.Carbonate);
        int pyriteSlot = compiled.GetSlotId(ProceduralMaterialSlots.Pyrite);

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
                    MassiveNativeCopperSample sample = plan.Evaluate(worldX, y, worldZ, surfaceY);
                    if (sample.Zone == MassiveNativeCopperZone.None) continue;
                    int chunkY = y / ChunkSize;
                    if ((uint)chunkY >= (uint)request.Chunks.Length) continue;
                    int index3d = (((y % ChunkSize) * ChunkSize) + localZ) * ChunkSize + localX;
                    IChunkBlocks data = request.Chunks[chunkY].Data;
                    int hostBlockId = data.GetBlockIdUnsafe(index3d);
                    if (!compiled.IsReplaceableHost(hostBlockId) || !CanReplaceWithProceduralRock(compiled, hostBlockId)) continue;
                    int slot = sample.Zone switch
                    {
                        MassiveNativeCopperZone.Massive or MassiveNativeCopperZone.Stringer => nativeSlot,
                        MassiveNativeCopperZone.Oxide => sample.Grade == 0 ? malachiteSlot : azuriteSlot,
                        MassiveNativeCopperZone.Amygdule => sample.Grade == 0 ? carbonateSlot : quartzSlot,
                        MassiveNativeCopperZone.Breccia => pyriteSlot,
                        _ => -1
                    };
                    if (slot < 0) continue;
                    int grade = sample.Zone is MassiveNativeCopperZone.Massive or MassiveNativeCopperZone.Stringer
                        ? sample.Grade : 0;
                    int placeBlockId = compiled.ResolveBlock(slot, grade, hostBlockId);
                    if (placeBlockId == 0 || placeBlockId == hostBlockId) continue;
                    data.SetBlockUnsafe(index3d, placeBlockId);
                    AdvancedGeology.Byproducts.ByproductSystem.RecordPlacement(request.Chunks[chunkY], index3d,
                        placeBlockId, candidate.Instance.FeatureId, compiled);
                    data.SetFluid(index3d, 0);
                }
            }
        }
    }
}
