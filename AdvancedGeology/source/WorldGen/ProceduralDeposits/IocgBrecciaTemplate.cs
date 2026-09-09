using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using Vintagestory.API.Server;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace AdvancedGeology.WorldGen.ProceduralDeposits;

public enum IocgBrecciaZone
{
    None = 0,
    Bornite,
    Chalcopyrite,
    Chalcocite,
    Hematite,
    Magnetite,
    Pyrite,
    BrecciaClast,
    ApatiteCarbonate,
    Fluorite,
    Barite,
    Potassic,
    SodicCalcic,
    ChloriteSericite,
    GraniteHost,
    VolcanicRoof
}

[JsonObject(MemberSerialization.OptIn)]
public sealed class IocgBrecciaDefinition
{
    // Model extent is 72x60. Breccia bodies reach an offset of +-(count-1)/2 * 18 plus a radius
    // of 12, and the outer sodic-calcic halo extends to r = 1.62, so the reach must contain both.
    [JsonProperty]
    public int HorizontalRadius { get; set; } = 40;

    [JsonProperty]
    public int VerticalHalfHeight { get; set; } = 30;

    [JsonProperty]
    public double StrikeDegMin { get; set; } = 0.0;

    [JsonProperty]
    public double StrikeDegMax { get; set; } = 180.0;

    [JsonProperty]
    public double RoofBaseYMin { get; set; } = 15.0;

    [JsonProperty]
    public double RoofBaseYMax { get; set; } = 20.0;

    [JsonProperty]
    public int BrecciaCountMin { get; set; } = 1;

    [JsonProperty]
    public int BrecciaCountMax { get; set; } = 3;

    [JsonProperty]
    public double BrecciaAlongSpacingMin { get; set; } = 12.0;

    [JsonProperty]
    public double BrecciaAlongSpacingMax { get; set; } = 18.0;

    [JsonProperty]
    public double BrecciaAcrossSpread { get; set; } = 5.0;

    [JsonProperty]
    public double BrecciaCenterYSpread { get; set; } = 3.0;

    [JsonProperty]
    public double BrecciaRadiusXMin { get; set; } = 8.0;

    [JsonProperty]
    public double BrecciaRadiusXMax { get; set; } = 12.0;

    [JsonProperty]
    public double BrecciaRadiusZMin { get; set; } = 7.0;

    [JsonProperty]
    public double BrecciaRadiusZMax { get; set; } = 10.0;

    [JsonProperty]
    public double BrecciaRadiusYMin { get; set; } = 16.0;

    [JsonProperty]
    public double BrecciaRadiusYMax { get; set; } = 23.0;

    [JsonProperty]
    public int FaultCountMin { get; set; } = 2;

    [JsonProperty]
    public int FaultCountMax { get; set; } = 5;

    [JsonProperty]
    public double FaultStrikeJitter { get; set; } = 35.0;

    [JsonProperty]
    public double FaultDipDegMin { get; set; } = 60.0;

    [JsonProperty]
    public double FaultDipDegMax { get; set; } = 84.0;

    [JsonProperty]
    public double FaultOffsetSpread { get; set; } = 10.0;

    [JsonProperty]
    public double FaultCenterYMin { get; set; } = -5.0;

    [JsonProperty]
    public double FaultCenterYMax { get; set; } = 2.0;

    [JsonProperty]
    public double FaultLengthMin { get; set; } = 16.0;

    [JsonProperty]
    public double FaultLengthMax { get; set; } = 28.0;

    [JsonProperty]
    public double FaultHeightMin { get; set; } = 18.0;

    [JsonProperty]
    public double FaultHeightMax { get; set; } = 28.0;
}

/// <summary>
/// Result of classifying one voxel against an <see cref="IocgBrecciaPlan"/>.
/// </summary>
public readonly record struct IocgBrecciaSample(
    IocgBrecciaZone Zone,
    int Grade = 0,
    bool InBreccia = false,
    bool InFaultCore = false,
    double BrecciaRadius = double.PositiveInfinity);

/// <summary>
/// Magnetite, hematite, bornite, chalcopyrite, chalcocite, pyrite, apatite-carbonate, fluorite, barite, and breccia form steep fault-controlled bodies with concentric alteration shells.
/// </summary>
internal sealed class IocgBrecciaPlan
{
    private const ulong PlanSalt = 0x494F4347425243UL; // "IOCGBRC"

    // Local model floor: y < -30 is below the modelled system (GRID_Y / 2).
    private const double FloorY = -30.0;

    private readonly int originX;
    private readonly int originY;
    private readonly int originZ;

    private readonly double strikeDeg;
    private readonly double roofBaseY;
    private readonly double seed;

    private readonly BrecciaBody[] breccias;
    private readonly FaultPlane[] faults;

    public int OriginX => originX;
    public int OriginY => originY;
    public int OriginZ => originZ;
    public double Seed => seed;
    public double StrikeDeg => strikeDeg;
    public double RoofBaseY => roofBaseY;
    public int BrecciaCount => breccias.Length;
    public int FaultCount => faults.Length;

    private IocgBrecciaPlan(
        in ProceduralDepositInstance instance,
        double strikeDeg,
        double roofBaseY,
        double seed,
        BrecciaBody[] breccias,
        FaultPlane[] faults)
    {
        originX = instance.CenterX;
        originY = instance.CenterY;
        originZ = instance.CenterZ;
        this.strikeDeg = strikeDeg;
        this.roofBaseY = roofBaseY;
        this.seed = seed;
        this.breccias = breccias;
        this.faults = faults;
    }

    /// <summary>
    /// Builds the deposit geometry in deterministic parameter order.
    /// </summary>
    public static IocgBrecciaPlan Create(
        in ProceduralDepositInstance instance,
        IocgBrecciaDefinition settings)
    {
        var random = new ProceduralDepositRandom(instance.FeatureId ^ PlanSalt);

        double strikeDeg = random.Range(settings.StrikeDegMin, settings.StrikeDegMax);
        double roofBaseY = random.Range(settings.RoofBaseYMin, settings.RoofBaseYMax);
        double seed = random.Range(0.0, 100.0);

        int count = random.NextInt(settings.BrecciaCountMin, settings.BrecciaCountMax);
    // Fewer bodies are individually larger, keeping total footprint roughly fixed.
        double scale = Math.Sqrt(2.0 / count);
        double strikeRad = strikeDeg * Math.PI / 180.0;
        double cosStrike = Math.Cos(strikeRad);
        double sinStrike = Math.Sin(strikeRad);

        var bodies = new BrecciaBody[count];
        int mainIndex = count / 2;
        for (int i = 0; i < count; i++)
        {
            double centered = i - (count - 1) / 2.0;
            double along = centered * random.Range(
                settings.BrecciaAlongSpacingMin,
                settings.BrecciaAlongSpacingMax);
            double across = random.Range(-settings.BrecciaAcrossSpread, settings.BrecciaAcrossSpread);
            bodies[i] = new BrecciaBody(
                along * cosStrike + across * sinStrike,
                -along * sinStrike + across * cosStrike,
                random.Range(-settings.BrecciaCenterYSpread, settings.BrecciaCenterYSpread),
                random.Range(settings.BrecciaRadiusXMin, settings.BrecciaRadiusXMax) * scale,
                random.Range(settings.BrecciaRadiusZMin, settings.BrecciaRadiusZMax) * scale,
                random.Range(settings.BrecciaRadiusYMin, settings.BrecciaRadiusYMax),
                seed + i * 29.0,
                i == mainIndex);
        }

        int faultCount = random.NextInt(settings.FaultCountMin, settings.FaultCountMax);
        var planes = new FaultPlane[faultCount];
        for (int i = 0; i < faultCount; i++)
        {
            double faultStrikeRad = (strikeDeg
                + random.Range(-settings.FaultStrikeJitter, settings.FaultStrikeJitter))
                * Math.PI / 180.0;
            double dipRad = random.Range(settings.FaultDipDegMin, settings.FaultDipDegMax)
                * Math.PI / 180.0;
            planes[i] = new FaultPlane(
                Math.Cos(faultStrikeRad),
                Math.Sin(faultStrikeRad),
                Math.Sin(dipRad),
                Math.Cos(dipRad),
                random.Range(-settings.FaultOffsetSpread, settings.FaultOffsetSpread),
                random.Range(settings.FaultCenterYMin, settings.FaultCenterYMax),
                random.Range(settings.FaultLengthMin, settings.FaultLengthMax),
                random.Range(settings.FaultHeightMin, settings.FaultHeightMax),
                seed + 91.0 + i * 17.0);
        }

        return new IocgBrecciaPlan(instance, strikeDeg, roofBaseY, seed, bodies, planes);
    }

    /// <summary><c>getRoofY</c>: tilted, undulating volcanic roof contact.</summary>
    public double GetRoofY(double x, double z)
    {
        return roofBaseY + 0.035 * x - 0.025 * z + Math.Sin(x * 0.055 + seed * 0.6) * 1.8;
    }

    public IocgBrecciaSample Evaluate(int worldX, int worldY, int worldZ, int surfaceY = int.MaxValue)
    {
        if (worldY > surfaceY) return default;

        double x = worldX - originX;
        double y = worldY - originY;
        double z = worldZ - originZ;
        if (y < FloorY) return default;

        double roofY = GetRoofY(x, z);
        if (y > roofY) return default;

        BrecciaSample? best = null;
        for (int i = 0; i < breccias.Length; i++)
        {
            BrecciaSample sample = SampleBreccia(x, y, z, breccias[i]);
            if (sample.Inside && (!best.HasValue || sample.Radius < best.Value.Radius))
            {
                best = sample;
            }
        }

        bool faultCore = false;
        bool faultHalo = false;
        for (int i = 0; i < faults.Length; i++)
        {
            FaultSample sample = SampleFault(x, y, z, faults[i]);
            faultCore |= sample.Inside;
            faultHalo |= sample.Halo;
        }

        if (best.HasValue)
        {
            BrecciaSample body = best.Value;

            // Rotated clast field: angular wall-rock fragments floating in the breccia cement.
            double clast = Math.Sin(body.DeltaX * 0.72 + body.VerticalNorm * 2.1)
                + Math.Cos(body.DeltaZ * 0.78 - body.VerticalNorm * 1.7)
                + 0.45 * Math.Sin((body.DeltaX - body.DeltaZ) * 0.51 + body.Phase);
            if (clast > 1.18 && body.Radius < 0.9)
            {
                return Finish(IocgBrecciaZone.BrecciaClast, x, y, z, true, faultCore, body.Radius);
            }

            double copper = Math.Sin(x * 0.18 - z * 0.12 + body.Phase)
                + 0.55 * Math.Cos(y * 0.23 + x * 0.08);
            double gangue = Math.Sin(x * 0.31 + z * 0.27 - body.Phase) + 0.45 * Math.Cos(y * 0.29);

            // Copper is structurally controlled: fault intersections, then breccia cores.
            if ((faultCore && copper > 0.15) || (body.Radius < 0.58 && copper > 0.88))
            {
                if (body.VerticalNorm > 0.45 && copper > 1.25)
                {
                    return Finish(IocgBrecciaZone.Chalcocite, x, y, z, true, faultCore, body.Radius);
                }
                if (copper > 0.72)
                {
                    return Finish(IocgBrecciaZone.Bornite, x, y, z, true, faultCore, body.Radius);
                }
                return Finish(IocgBrecciaZone.Chalcopyrite, x, y, z, true, faultCore, body.Radius);
            }

            if (gangue > 1.28)
            {
                // Fluorite and barite share this gangue band.
                // A grain hash splits the band so each occurs as its own real ore.
                return Finish(
                    Hash3D(x * 1.9 + seed, y * 1.9, z * 1.9) < 0.5
                        ? IocgBrecciaZone.Fluorite
                        : IocgBrecciaZone.Barite,
                    x,
                    y,
                    z,
                    true,
                    faultCore,
                    body.Radius);
            }
            if (gangue < -1.2)
            {
                return Finish(IocgBrecciaZone.ApatiteCarbonate, x, y, z, true, faultCore, body.Radius);
            }
            if (body.VerticalNorm < -0.25 && copper < -0.35)
            {
                return Finish(IocgBrecciaZone.Magnetite, x, y, z, true, faultCore, body.Radius);
            }
            if (copper > 1.05)
            {
                return Finish(IocgBrecciaZone.Pyrite, x, y, z, true, faultCore, body.Radius);
            }
            return Finish(IocgBrecciaZone.Hematite, x, y, z, true, faultCore, body.Radius);
        }

        // Outside every body the nearest normalized breccia radius drives the alteration shells.
        double nearest = double.PositiveInfinity;
        for (int i = 0; i < breccias.Length; i++)
        {
            BrecciaSample sample = SampleBreccia(x, y, z, breccias[i]);
            if (sample.Radius < nearest) nearest = sample.Radius;
        }

        if (faultCore && nearest < 1.35)
        {
            double veinField = Math.Sin(x * 0.2 + z * 0.16 + seed);
            return veinField > 0.62
                ? Finish(IocgBrecciaZone.Chalcopyrite, x, y, z, false, true, nearest)
                : Finish(IocgBrecciaZone.Hematite, x, y, z, false, true, nearest);
        }

        if (nearest < 1.18) return Finish(IocgBrecciaZone.Potassic, x, y, z, false, false, nearest);
        if (nearest < 1.38 || faultHalo)
        {
            return Finish(IocgBrecciaZone.ChloriteSericite, x, y, z, false, false, nearest);
        }
        if (nearest < 1.62) return Finish(IocgBrecciaZone.SodicCalcic, x, y, z, false, false, nearest);

        return default;
    }

    private IocgBrecciaSample Finish(
        IocgBrecciaZone zone,
        double x,
        double y,
        double z,
        bool inBreccia,
        bool inFaultCore,
        double brecciaRadius)
    {
        int grade = 0;
        if (IsGraded(zone))
        {
            double grainNoise = Hash3D(x * 2.7 + seed * 1.3, y * 2.7, z * 2.7);
            grade = grainNoise > 0.82 ? 3 : (grainNoise > 0.55 ? 2 : (grainNoise > 0.25 ? 1 : 0));
        }

        return new IocgBrecciaSample(zone, grade, inBreccia, inFaultCore, brecciaRadius);
    }

    internal static bool IsGraded(IocgBrecciaZone zone)
    {
        return zone == IocgBrecciaZone.Bornite
            || zone == IocgBrecciaZone.Chalcopyrite
            || zone == IocgBrecciaZone.Chalcocite
            || zone == IocgBrecciaZone.Hematite
            || zone == IocgBrecciaZone.Magnetite;
    }

    /// <summary>
    /// <c>sampleBreccia</c>: warped triaxial body in global coordinates.
    /// </summary>
    private static BrecciaSample SampleBreccia(double x, double y, double z, in BrecciaBody body)
    {
        double deltaX = x - body.CenterX;
        double deltaZ = z - body.CenterZ;
        double u = deltaX / body.RadiusX;
        double v = deltaZ / body.RadiusZ;
        double w = (y - body.CenterY) / body.RadiusY;
        double warp = Math.Sin(u * 4.0 + body.Phase) * Math.Cos(v * 3.0) * 0.1
            + Math.Sin(w * 5.0 - body.Phase * 0.3) * 0.05;
        double radius = Math.Sqrt(u * u + v * v + w * w) + warp;
        return new BrecciaSample(radius <= 1.0, radius, deltaX, deltaZ, w, body.Phase);
    }

    /// <summary>
    /// <c>sampleFault</c>: bent steep plane with an elliptical footprint taper.
    /// </summary>
    private static FaultSample SampleFault(double x, double y, double z, in FaultPlane fault)
    {
        double along = x * fault.CosStrike - z * fault.SinStrike;
        double across = x * fault.SinStrike + z * fault.CosStrike - fault.Offset;
        double vertical = y - fault.CenterY;
        double bend = Math.Sin(along * 0.13 + y * 0.07 + fault.Phase) * 1.1;
        double signed = across * fault.SinDip + vertical * fault.CosDip - bend;
        double u = along / fault.Length;
        double v = vertical / fault.Height;
        double footprint = Math.Sqrt(u * u + v * v);
        double taper = Clamp(1.0 - footprint * footprint, 0.0, 1.0);
        double distance = Math.Abs(signed);
        return new FaultSample(
            footprint <= 1.0 && distance <= 0.65 * taper,
            footprint <= 1.08 && distance <= 2.2 * taper);
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

    private readonly record struct BrecciaSample(
        bool Inside,
        double Radius,
        double DeltaX,
        double DeltaZ,
        double VerticalNorm,
        double Phase);

    private readonly record struct FaultSample(bool Inside, bool Halo);

    private readonly record struct BrecciaBody(
        double CenterX,
        double CenterZ,
        double CenterY,
        double RadiusX,
        double RadiusZ,
        double RadiusY,
        double Phase,
        bool IsMain);

    private readonly record struct FaultPlane(
        double CosStrike,
        double SinStrike,
        double SinDip,
        double CosDip,
        double Offset,
        double CenterY,
        double Length,
        double Height,
        double Phase);
}

internal sealed class IocgBrecciaProceduralTemplate : IProceduralDepositTemplate
{
    private static readonly string[] RequiredSlots =
    {
        ProceduralMaterialSlots.Bornite,
        ProceduralMaterialSlots.Chalcopyrite,
        ProceduralMaterialSlots.Chalcocite,
        ProceduralMaterialSlots.Hematite,
        ProceduralMaterialSlots.Magnetite,
        ProceduralMaterialSlots.Pyrite,
        ProceduralMaterialSlots.Breccia,
        ProceduralMaterialSlots.Apatite,
        ProceduralMaterialSlots.Fluorite,
        ProceduralMaterialSlots.Barite
    };

    public string Code => "iocgBreccia";
    public IReadOnlyList<string> RequiredMaterialSlots => RequiredSlots;
    public bool RequiresPlan => true;

    public int GetMaximumHorizontalReach(ProceduralDepositDefinition definition)
    {
        return definition.IocgBreccia.HorizontalRadius;
    }

    public object CreatePlan(in ProceduralDepositInstance instance, ProceduralDepositDefinition definition)
    {
        return IocgBrecciaPlan.Create(instance, definition.IocgBreccia);
    }

    public bool Validate(ProceduralDepositDefinition definition, out string error)
    {
        IocgBrecciaDefinition settings = definition.IocgBreccia;
        bool valid = settings.HorizontalRadius >= 16
            && settings.VerticalHalfHeight >= 8
            && settings.StrikeDegMax >= settings.StrikeDegMin
            && settings.RoofBaseYMax >= settings.RoofBaseYMin
            && settings.BrecciaCountMin >= 1
            && settings.BrecciaCountMax >= settings.BrecciaCountMin
            && settings.BrecciaAlongSpacingMin > 0.0
            && settings.BrecciaAlongSpacingMax >= settings.BrecciaAlongSpacingMin
            && settings.BrecciaAcrossSpread >= 0.0
            && settings.BrecciaCenterYSpread >= 0.0
            && settings.BrecciaRadiusXMin > 0.0
            && settings.BrecciaRadiusXMax >= settings.BrecciaRadiusXMin
            && settings.BrecciaRadiusZMin > 0.0
            && settings.BrecciaRadiusZMax >= settings.BrecciaRadiusZMin
            && settings.BrecciaRadiusYMin > 0.0
            && settings.BrecciaRadiusYMax >= settings.BrecciaRadiusYMin
            && settings.FaultCountMin >= 1
            && settings.FaultCountMax >= settings.FaultCountMin
            && settings.FaultStrikeJitter >= 0.0
            && settings.FaultDipDegMin > 0.0
            && settings.FaultDipDegMax >= settings.FaultDipDegMin
            && settings.FaultOffsetSpread >= 0.0
            && settings.FaultCenterYMax >= settings.FaultCenterYMin
            && settings.FaultLengthMin > 0.0
            && settings.FaultLengthMax >= settings.FaultLengthMin
            && settings.FaultHeightMin > 0.0
            && settings.FaultHeightMax >= settings.FaultHeightMin;
        error = valid ? string.Empty : "invalid iocg breccia settings";
        return valid;
    }

    public void Realize(
        ProceduralDepositWorldGenSystem system,
        in DepositCandidate candidate,
        IChunkColumnGenerateRequest request,
        int baseX,
        int baseZ)
    {
        system.RealizeIocgBrecciaCandidate(candidate, request, baseX, baseZ);
    }
}

public sealed partial class ProceduralDepositWorldGenSystem
{
    internal void RealizeIocgBrecciaCandidate(
        DepositCandidate candidate,
        IChunkColumnGenerateRequest request,
        int baseX,
        int baseZ)
    {
        CompiledProceduralDeposit compiled = candidate.Compiled;
        IocgBrecciaDefinition settings = compiled.Definition.IocgBreccia;
        var plan = (IocgBrecciaPlan)candidate.Plan!;
        ushort[] heightMap = request.Chunks[0].MapChunk.WorldGenTerrainHeightMap;
        int worldHeight = serverApi!.World.BlockAccessor.MapSizeY;
        int minimumY = GameMath.Clamp(candidate.Instance.CenterY - settings.VerticalHalfHeight, 1, worldHeight - 2);
        int maximumPrototypeY = GameMath.Clamp(candidate.Instance.CenterY + settings.VerticalHalfHeight, 1, worldHeight - 2);

        int[] zoneSlots = BuildIocgBrecciaZoneSlots(compiled);

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
                    IocgBrecciaSample sample = plan.Evaluate(worldX, y, worldZ, surfaceY);
                    if (sample.Zone == IocgBrecciaZone.None) continue;

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

    private static int[] BuildIocgBrecciaZoneSlots(CompiledProceduralDeposit compiled)
    {
        var slots = new int[Enum.GetValues<IocgBrecciaZone>().Length];
        Array.Fill(slots, -1);

        slots[(int)IocgBrecciaZone.Bornite] = compiled.GetSlotId(ProceduralMaterialSlots.Bornite);
        slots[(int)IocgBrecciaZone.Chalcopyrite] = compiled.GetSlotId(ProceduralMaterialSlots.Chalcopyrite);
        slots[(int)IocgBrecciaZone.Chalcocite] = compiled.GetSlotId(ProceduralMaterialSlots.Chalcocite);
        slots[(int)IocgBrecciaZone.Hematite] = compiled.GetSlotId(ProceduralMaterialSlots.Hematite);
        slots[(int)IocgBrecciaZone.Magnetite] = compiled.GetSlotId(ProceduralMaterialSlots.Magnetite);
        slots[(int)IocgBrecciaZone.Pyrite] = compiled.GetSlotId(ProceduralMaterialSlots.Pyrite);
        slots[(int)IocgBrecciaZone.BrecciaClast] = compiled.GetSlotId(ProceduralMaterialSlots.Breccia);
        slots[(int)IocgBrecciaZone.ApatiteCarbonate] = compiled.GetSlotId(ProceduralMaterialSlots.Apatite);

        // The gangue band resolves to fluorite or barite,
        // split by a grain hash inside the plan.
        slots[(int)IocgBrecciaZone.Fluorite] = compiled.GetSlotId(ProceduralMaterialSlots.Fluorite);
        slots[(int)IocgBrecciaZone.Barite] = compiled.GetSlotId(ProceduralMaterialSlots.Barite);

        return slots;
    }
}
