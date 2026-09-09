using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Vintagestory.API.Server;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace AdvancedGeology.WorldGen.ProceduralDeposits;

public enum SchwazTetrahedriteZone
{
    None = 0,
    Tetrahedrite,
    Chalcopyrite,
    Barite,
    Malachite,
    Azurite,
    Limonite,
    Gossan,
    GreenGossan,
    Chalcocite
}

[JsonObject(MemberSerialization.OptIn)]
public sealed class SchwazTetrahedriteDefinition
{
    [JsonProperty]
    public int HorizontalRadius { get; set; } = 42;

    [JsonProperty]
    public int VerticalHalfHeight { get; set; } = 35;

    [JsonProperty]
    public int SupergeneDepth { get; set; } = 8;
}

public readonly record struct SchwazTetrahedriteSample(
    SchwazTetrahedriteZone Zone,
    int Grade,
    bool InReactionLens,
    bool InPrimaryOre,
    bool IsTetrahedrite,
    bool IsBarite,
    bool IsChalcopyrite,
    bool IsAzurite,
    double GrainNoise);

/// <summary>
/// Tetrahedrite, chalcopyrite, barite, chalcocite, malachite, azurite, limonite, and gossan form carbonate-replacement mantos connected to steep feeder faults.
/// </summary>
internal sealed class SchwazTetrahedritePlan
{
    private const ulong PlanSalt = 0x53434857415A5445UL; // "SCHWAZTE"

    private const double DolomiteTop = 11.0;
    private const double DolomiteBottom = -13.0;
    private const double SupergeneDepth = 8.0;
    private const double HypogeneHeightScale = 2.0;

    private readonly ulong featureId;
    private readonly int originX;
    private readonly int originY;
    private readonly int originZ;
    private readonly double seed;
    private readonly double bedDipDeg;
    private readonly double faultDipDeg;
    private readonly double faultStrikeDeg;
    private readonly double feederX;
    private readonly double feederZ;
    private readonly double mantoStrikeDeg;
    private readonly double mantoBedY;
    private readonly SchwazMantoLens[] mantoLenses;
    private readonly SchwazFeederFault[] feederFaults;

    public int OriginX => originX;
    public int OriginY => originY;
    public int OriginZ => originZ;
    public double Seed => seed;
    public double BedDipDeg => bedDipDeg;
    public double FaultDipDeg => faultDipDeg;
    public double FaultStrikeDeg => faultStrikeDeg;
    public double MantoStrikeDeg => mantoStrikeDeg;
    public double MantoBedY => mantoBedY;
    public int MantoLensCount => mantoLenses.Length;
    public int FeederFaultCount => feederFaults.Length;

    private SchwazTetrahedritePlan(
        ulong featureId,
        in ProceduralDepositInstance instance,
        double seed,
        double bedDipDeg,
        double faultDipDeg,
        double faultStrikeDeg,
        double feederX,
        double feederZ,
        double mantoStrikeDeg,
        double mantoBedY,
        SchwazMantoLens[] mantoLenses,
        SchwazFeederFault[] feederFaults)
    {
        this.featureId = featureId;
        originX = instance.CenterX;
        originY = instance.CenterY;
        originZ = instance.CenterZ;
        this.seed = seed;
        this.bedDipDeg = bedDipDeg;
        this.faultDipDeg = faultDipDeg;
        this.faultStrikeDeg = faultStrikeDeg;
        this.feederX = feederX;
        this.feederZ = feederZ;
        this.mantoStrikeDeg = mantoStrikeDeg;
        this.mantoBedY = mantoBedY;
        this.mantoLenses = mantoLenses;
        this.feederFaults = feederFaults;
    }

    public static SchwazTetrahedritePlan Create(
        in ProceduralDepositInstance instance,
        SchwazTetrahedriteDefinition settings)
    {
        var random = new ProceduralDepositRandom(instance.FeatureId ^ PlanSalt);

        double bedDipDeg = random.Range(11.0, 22.0);
        double faultDipDeg = random.Range(56.0, 75.0);
        double faultStrikeDeg = random.Range(8.0, 52.0);
        double feederX = random.Range(-7.0, 7.0);
        double feederZ = random.Range(-7.0, 7.0);
        double mantoStrikeDeg = faultStrikeDeg + random.Range(-14.0, 14.0);
        double mantoBedY = random.Range(0.8, 3.0);
        double seed = random.Range(0.0, 100.0);

        double strikeRad = mantoStrikeDeg * Math.PI / 180.0;
        double cosStrike = Math.Cos(strikeRad);
        double sinStrike = Math.Sin(strikeRad);

        // 5 irregular oblong dolomite mantos following warped bed
        var lenses = new SchwazMantoLens[5];
        for (int i = 0; i < 5; i++)
        {
            double side = i % 2 == 0 ? -1.0 : 1.0;
            double along = i == 0 ? random.Range(-4.0, 4.0) : side * random.Range(8.0, 28.0);
            double across = random.Range(-7.0, 7.0);
            double cx = feederX + along * cosStrike + across * sinStrike;
            double cz = feederZ - along * sinStrike + across * cosStrike;

            lenses[i] = SchwazMantoLens.Create(
                cx,
                cz,
                mantoBedY + random.Range(-2.0, 2.0) + (i == 4 ? 2.0 : 0.0),
                i == 0 ? random.Range(17.0, 24.0) : random.Range(10.0, 18.0),
                i == 0 ? random.Range(8.0, 13.0) : random.Range(5.0, 9.0),
                (i == 0 ? random.Range(3.6, 5.2) : random.Range(2.4, 4.2)) * HypogeneHeightScale,
                mantoStrikeDeg + random.Range(-11.0, 11.0),
                seed + i * 17.0);
        }

        // 3 steep feeder faults + 2 splays (5 faults total)
        var faults = new SchwazFeederFault[5];
        double offset = random.Range(-7.0, -4.0);
        for (int i = 0; i < 3; i++)
        {
            if (i > 0) offset += random.Range(5.0, 10.0);
            faults[i] = SchwazFeederFault.Create(
                faultStrikeDeg + random.Range(-9.0, 9.0),
                faultDipDeg + random.Range(-7.0, 7.0),
                offset,
                random.Range(-6.0, 6.0),
                random.Range(-3.0, 1.0),
                random.Range(22.0, 34.0),
                random.Range(20.0, 31.0) * HypogeneHeightScale,
                random.Range(2.0, 3.2),
                seed + 61.0 + i * 13.0,
                random.Range(-10.0, 10.0),
                random.Range(3.0, 6.0),
                random.Range(-1.5, 1.5),
                seed + i * 5.0,
                false);
        }

        for (int i = 0; i < 2; i++)
        {
            SchwazFeederFault parent = faults[i];
            faults[3 + i] = SchwazFeederFault.Create(
                parent.StrikeDeg + (i == 0 ? -1.0 : 1.0) * random.Range(12.0, 24.0),
                parent.DipDeg + random.Range(-8.0, 8.0),
                parent.Offset + (i == 0 ? -1.0 : 1.0) * random.Range(2.0, 4.0),
                parent.CenterAlong + (i == 0 ? -1.0 : 1.0) * random.Range(7.0, 13.0),
                parent.CenterY + random.Range(1.0, 5.0),
                random.Range(11.0, 19.0),
                random.Range(12.0, 20.0) * HypogeneHeightScale,
                random.Range(1.2, 2.0),
                seed + 113.0 + i * 17.0,
                random.Range(-5.0, 5.0),
                random.Range(2.0, 4.0),
                random.Range(-1.0, 1.0),
                seed + 41.0 + i * 7.0,
                true);
        }

        return new SchwazTetrahedritePlan(
            instance.FeatureId,
            instance,
            seed,
            bedDipDeg,
            faultDipDeg,
            faultStrikeDeg,
            feederX,
            feederZ,
            mantoStrikeDeg,
            mantoBedY,
            lenses,
            faults);
    }

    public double GetStratigraphicY(double x, double y, double z)
    {
        double dipRad = bedDipDeg * Math.PI / 180.0;
        double strikeRad = mantoStrikeDeg * Math.PI / 180.0;
        double acrossStrike = x * Math.Sin(strikeRad) + z * Math.Cos(strikeRad);
        double warp = Math.Sin(x * 0.055 + seed) * 1.6 + Math.Cos(z * 0.065 - seed * 0.6) * 1.2;
        return y - Math.Tan(dipRad) * acrossStrike - warp;
    }

    public SchwazTetrahedriteSample Evaluate(int worldX, int worldY, int worldZ, int surfaceY = int.MaxValue)
    {
        if (worldY > surfaceY)
        {
            return new SchwazTetrahedriteSample(SchwazTetrahedriteZone.None, 0, false, false, false, false, false, false, 0.0);
        }

        double x = worldX - originX;
        double y = worldY - originY;
        double z = worldZ - originZ;

    // Model bounds the package at deposit-local y < -35
        if (y < -35.0)
        {
            return new SchwazTetrahedriteSample(SchwazTetrahedriteZone.None, 0, false, false, false, false, false, false, 0.0);
        }

        double stratY = GetStratigraphicY(x, y, z);
        bool isUpperSlate = stratY > DolomiteTop;
        bool isBasalSlate = stratY < DolomiteBottom;
        bool isDolomite = !isUpperSlate && !isBasalSlate;

        if (!isDolomite)
        {
            return new SchwazTetrahedriteSample(SchwazTetrahedriteZone.None, 0, false, false, false, false, false, false, 0.0);
        }

        int depth = surfaceY < int.MaxValue ? surfaceY - worldY + 1 : 999;

        MantoSample? bestManto = null;
        for (int i = 0; i < mantoLenses.Length; i++)
        {
            MantoSample sample = mantoLenses[i].Sample(x, stratY, z);
            if (sample.InHalo && (!bestManto.HasValue || sample.Radial < bestManto.Value.Radial))
            {
                bestManto = sample;
            }
        }

        FaultSample? bestFault = null;
        for (int i = 0; i < feederFaults.Length; i++)
        {
            FaultSample sample = feederFaults[i].Sample(x, y, z);
            if (sample.Score < double.PositiveInfinity && (!bestFault.HasValue || sample.Score < bestFault.Value.Score))
            {
                bestFault = sample;
            }
        }

        bool inManto = isDolomite && bestManto.HasValue && bestManto.Value.Inside;
        bool inFault = isDolomite && bestFault.HasValue && bestFault.Value.Inside;
        bool inPrimaryOre = inManto || inFault;
        bool inReactionLens = isDolomite && (
            inPrimaryOre
            || (bestManto.HasValue && bestManto.Value.InHalo)
            || (bestFault.HasValue && bestFault.Value.InHalo));

        if (!inReactionLens)
        {
            return new SchwazTetrahedriteSample(SchwazTetrahedriteZone.None, 0, false, false, false, false, false, false, 0.0);
        }

        double grainNoise = Hash3D(x * 2.2 + seed, y * 2.2, z * 2.2);
        double pocketNoise = Math.Sin(x * 0.31 + seed) * Math.Cos(z * 0.29 - seed * 0.7) + Math.Sin(y * 0.47);

        bool isTetrahedrite = false;
        bool isBarite = false;
        bool isChalcopyrite = false;

        if (inPrimaryOre)
        {
            bool inMantoCore = inManto && bestManto!.Value.VerticalNorm < 0.58 && bestManto.Value.LateralNorm < 0.68;
            bool inFaultCore = inFault && bestFault!.Value.Score < 0.48;
            if ((inMantoCore || inFaultCore) && grainNoise > 0.24)
            {
                isTetrahedrite = true;
            }
            else if ((pocketNoise > 0.30 || (bestFault.HasValue && bestFault.Value.Distance > 1.2)) && grainNoise > 0.42)
            {
                isBarite = true;
            }
            else if (grainNoise > 0.70)
            {
                isChalcopyrite = true;
            }
        }

        bool isAzurite = false;
        if (bestFault.HasValue && bestFault.Value.Inside && bestFault.Value.Distance < 1.2 && grainNoise > 0.56)
        {
            isAzurite = true;
        }
        else if (bestManto.HasValue && bestManto.Value.LateralNorm < 0.78 && grainNoise > 0.82)
        {
            isAzurite = true;
        }

        // Supergene sequence:

        SchwazTetrahedriteZone zone = SchwazTetrahedriteZone.None;
        int grade = 1; // medium grade index

        if (depth <= 1)
        {
            // Depth 1: stained gossan soil. Chalcopyrite and barite columns stain red;
            // tetrahedrite / reaction-lens columns stain 75% red, 25% green.
            zone = (isBarite || isChalcopyrite || Hash3D(x * 0.31 + seed, 0.17, z * 0.29) >= 0.25)
                ? SchwazTetrahedriteZone.Gossan
                : SchwazTetrahedriteZone.GreenGossan;
            grade = 0;
        }
        else if (depth <= SupergeneDepth)
        {
            if (isBarite)
            {
                // Barite stays unaltered below the depth-1 gossan soil
                zone = SchwazTetrahedriteZone.Barite;
                grade = 0;
            }
            else
            {
                zone = isChalcopyrite
                    ? SchwazTetrahedriteZone.Chalcocite
                    : (isAzurite ? SchwazTetrahedriteZone.Azurite : SchwazTetrahedriteZone.Malachite);

                // Bountiful quality at depth 6-7
                grade = (depth == 6 || depth == 7) ? 3 : (grainNoise > 0.65 ? 2 : 1);
            }
        }
        else if (inPrimaryOre)
        {
            if (isTetrahedrite)
            {
                zone = SchwazTetrahedriteZone.Tetrahedrite;
                grade = grainNoise > 0.70 ? 2 : (grainNoise > 0.40 ? 1 : 0);
            }
            else if (isBarite)
            {
                zone = SchwazTetrahedriteZone.Barite;
                grade = 0;
            }
            else if (isChalcopyrite)
            {
                zone = SchwazTetrahedriteZone.Chalcopyrite;
                grade = grainNoise > 0.70 ? 2 : 1;
            }
            else
            {
                zone = SchwazTetrahedriteZone.None;
            }
        }
        else
        {
            zone = SchwazTetrahedriteZone.None;
        }

        return new SchwazTetrahedriteSample(
            zone,
            grade,
            inReactionLens,
            inPrimaryOre,
            isTetrahedrite,
            isBarite,
            isChalcopyrite,
            isAzurite,
            grainNoise);
    }

    internal static double Hash3D(double x, double y, double z)
    {
        double n = Math.Sin(x * 12.9898 + y * 78.233 + z * 37.719) * 43758.5453;
        return n - Math.Floor(n);
    }

    private static double Clamp(double value, double min, double max)
    {
        return Math.Max(min, Math.Min(max, value));
    }

    private readonly record struct MantoSample(
        double Radial,
        double VerticalNorm,
        double LateralNorm,
        bool Inside,
        bool InHalo,
        double Taper);

    private readonly struct SchwazMantoLens
    {
        public readonly double CenterX;
        public readonly double CenterZ;
        public readonly double CenterStratY;
        public readonly double RadiusAlong;
        public readonly double RadiusAcross;
        public readonly double RadiusVertical;
        public readonly double SinStrike;
        public readonly double CosStrike;
        public readonly double Phase;

        private SchwazMantoLens(
            double centerX,
            double centerZ,
            double centerStratY,
            double radiusAlong,
            double radiusAcross,
            double radiusVertical,
            double sinStrike,
            double cosStrike,
            double phase)
        {
            CenterX = centerX;
            CenterZ = centerZ;
            CenterStratY = centerStratY;
            RadiusAlong = radiusAlong;
            RadiusAcross = radiusAcross;
            RadiusVertical = radiusVertical;
            SinStrike = sinStrike;
            CosStrike = cosStrike;
            Phase = phase;
        }

        public static SchwazMantoLens Create(
            double centerX,
            double centerZ,
            double centerStratY,
            double radiusAlong,
            double radiusAcross,
            double radiusVertical,
            double strikeDeg,
            double phase)
        {
            double strikeRad = strikeDeg * Math.PI / 180.0;
            return new SchwazMantoLens(
                centerX,
                centerZ,
                centerStratY,
                radiusAlong,
                radiusAcross,
                radiusVertical,
                Math.Sin(strikeRad),
                Math.Cos(strikeRad),
                phase);
        }

        public MantoSample Sample(double x, double stratY, double z)
        {
            double dx = x - CenterX;
            double dz = z - CenterZ;
            double along = dx * CosStrike - dz * SinStrike;
            double across = dx * SinStrike + dz * CosStrike;
            double u = along / RadiusAlong;
            double v = across / RadiusAcross;
            double w = (stratY - CenterStratY) / RadiusVertical;

            double edgeWarp = Math.Sin(u * 3.6 + Phase) * Math.Cos(v * 3.1) * 0.15
                + Math.Sin(u * 7.0 - v * 4.5 + Phase * 0.5) * 0.07;
            double radial = Math.Sqrt(u * u + v * v + w * w) + edgeWarp;
            double taper = Clamp(1.0 - radial * radial, 0.0, 1.0);

            return new MantoSample(
                radial,
                w,
                Math.Sqrt(u * u + v * v),
                radial <= 1.03,
                radial <= 1.16,
                taper);
        }
    }

    private readonly record struct FaultSample(
        double SignedDistance,
        double Distance,
        double Score,
        bool Inside,
        bool InHalo);

    private readonly struct SchwazFeederFault
    {
        public readonly double StrikeDeg;
        public readonly double DipDeg;
        public readonly double SinStrike;
        public readonly double CosStrike;
        public readonly double SinDip;
        public readonly double CosDip;
        public readonly double Offset;
        public readonly double CenterAlong;
        public readonly double CenterY;
        public readonly double LengthHalf;
        public readonly double HeightHalf;
        public readonly double Thickness;
        public readonly double Phase;
        public readonly double RelayCenter;
        public readonly double RelayWidth;
        public readonly double RelayShift;
        public readonly double PinchPhase;
        public readonly bool IsSplay;

        private SchwazFeederFault(
            double strikeDeg,
            double dipDeg,
            double sinStrike,
            double cosStrike,
            double sinDip,
            double cosDip,
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
            bool isSplay)
        {
            StrikeDeg = strikeDeg;
            DipDeg = dipDeg;
            SinStrike = sinStrike;
            CosStrike = cosStrike;
            SinDip = sinDip;
            CosDip = cosDip;
            Offset = offset;
            CenterAlong = centerAlong;
            CenterY = centerY;
            LengthHalf = lengthHalf;
            HeightHalf = heightHalf;
            Thickness = thickness;
            Phase = phase;
            RelayCenter = relayCenter;
            RelayWidth = relayWidth;
            RelayShift = relayShift;
            PinchPhase = pinchPhase;
            IsSplay = isSplay;
        }

        public static SchwazFeederFault Create(
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
            bool isSplay)
        {
            double strikeRad = strikeDeg * Math.PI / 180.0;
            double dipRad = dipDeg * Math.PI / 180.0;
            return new SchwazFeederFault(
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
                isSplay);
        }

        public FaultSample Sample(double x, double y, double z)
        {
            double along = x * CosStrike - z * SinStrike;
            double across = x * SinStrike + z * CosStrike - Offset;

            double bend = Math.Sin(along * 0.14 + y * 0.08 + Phase) * 2.0;
            double kink = Math.Cos(along * 0.33 + Phase * 1.2) * 0.65;
            double relay = RelayShift * Math.Tanh((along - RelayCenter) / RelayWidth);
            double signedDistance = across * SinDip + (y - CenterY) * CosDip - bend - kink - relay;

            double u = (along - CenterAlong) / LengthHalf;
            double v = (y - CenterY) / HeightHalf;
            double edgeWarp = Math.Sin(u * 4.0 + Phase) * Math.Cos(v * 3.0) * 0.14;
            double footprint = Math.Sqrt(u * u + v * v) + edgeWarp;
            double taper = Clamp(1.0 - footprint * footprint, 0.0, 1.0);
            double pinch = 0.78 + 0.22 * Math.Sin(along * 0.18 + PinchPhase);
            double halfThickness = Math.Max(0.12, Thickness * 0.5 * pinch * Math.Sqrt(taper));
            double distance = Math.Abs(signedDistance);

            double score = footprint <= 1.18 ? distance / halfThickness : double.PositiveInfinity;
            bool inside = footprint <= 1.04 && distance <= halfThickness;
            bool inHalo = footprint <= 1.14 && distance <= halfThickness + 3.0 * taper;

            return new FaultSample(
                signedDistance,
                distance,
                score,
                inside,
                inHalo);
        }
    }
}

internal sealed class SchwazTetrahedriteProceduralTemplate : IProceduralDepositTemplate
{
    private static readonly string[] RequiredSlots =
    {
        ProceduralMaterialSlots.Tetrahedrite,
        ProceduralMaterialSlots.Malachite,
        ProceduralMaterialSlots.Azurite,
        ProceduralMaterialSlots.Barite,
        ProceduralMaterialSlots.Chalcopyrite,
        ProceduralMaterialSlots.Chalcocite,
        ProceduralMaterialSlots.Gossan,
        ProceduralMaterialSlots.GreenGossan
    };

    public string Code => "schwazTetrahedrite";
    public IReadOnlyList<string> RequiredMaterialSlots => RequiredSlots;
    public bool RequiresPlan => true;

    public int GetMaximumHorizontalReach(ProceduralDepositDefinition definition)
    {
        return definition.Tetrahedrite.HorizontalRadius;
    }

    public object CreatePlan(in ProceduralDepositInstance instance, ProceduralDepositDefinition definition)
    {
        return SchwazTetrahedritePlan.Create(instance, definition.Tetrahedrite);
    }

    public bool Validate(ProceduralDepositDefinition definition, out string error)
    {
        SchwazTetrahedriteDefinition settings = definition.Tetrahedrite;
        bool valid = settings.HorizontalRadius >= 16
            && settings.VerticalHalfHeight >= 8
            && settings.SupergeneDepth >= 1;
        error = valid ? string.Empty : "invalid schwaz tetrahedrite settings";
        return valid;
    }

    public void Realize(
        ProceduralDepositWorldGenSystem system,
        in DepositCandidate candidate,
        IChunkColumnGenerateRequest request,
        int baseX,
        int baseZ)
    {
        system.RealizeSchwazTetrahedriteCandidate(candidate, request, baseX, baseZ);
    }
}

public sealed partial class ProceduralDepositWorldGenSystem
{
    internal void RealizeSchwazTetrahedriteCandidate(
        DepositCandidate candidate,
        IChunkColumnGenerateRequest request,
        int baseX,
        int baseZ)
    {
        CompiledProceduralDeposit compiled = candidate.Compiled;
        SchwazTetrahedriteDefinition settings = compiled.Definition.Tetrahedrite;
        SchwazTetrahedritePlan plan = (SchwazTetrahedritePlan)candidate.Plan!;
        ushort[] heightMap = request.Chunks[0].MapChunk.WorldGenTerrainHeightMap;
        int worldHeight = serverApi!.World.BlockAccessor.MapSizeY;
        int minimumY = Math.Clamp(candidate.Instance.CenterY - settings.VerticalHalfHeight, 1, worldHeight - 2);
        int maximumPrototypeY = Math.Clamp(candidate.Instance.CenterY + settings.VerticalHalfHeight, 1, worldHeight - 2);

        int[] zoneSlots = BuildSchwazTetrahedriteZoneSlots(compiled);
        int gossanSlot = compiled.GetSlotId(ProceduralMaterialSlots.Gossan);
        int greenGossanSlot = compiled.GetSlotId(ProceduralMaterialSlots.GreenGossan);
        int malachiteSlot = compiled.GetSlotId(ProceduralMaterialSlots.Malachite);
        int azuriteSlot = compiled.GetSlotId(ProceduralMaterialSlots.Azurite);
        int chalcociteSlot = compiled.GetSlotId(ProceduralMaterialSlots.Chalcocite);
        int tetrahedriteSlot = compiled.GetSlotId(ProceduralMaterialSlots.Tetrahedrite);
        int chalcopyriteSlot = compiled.GetSlotId(ProceduralMaterialSlots.Chalcopyrite);
        int bariteSlot = compiled.GetSlotId(ProceduralMaterialSlots.Barite);
        int limoniteSlot = compiled.GetSlotId(ProceduralMaterialSlots.Limonite);

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
                    SchwazTetrahedriteSample sample = plan.Evaluate(worldX, y, worldZ, surfaceY);
                    if (sample.Zone == SchwazTetrahedriteZone.None) continue;

                    int chunkY = y / ChunkSize;
                    if ((uint)chunkY >= (uint)request.Chunks.Length) continue;
                    int localY = y % ChunkSize;
                    int index3d = ((localY * ChunkSize) + localZ) * ChunkSize + localX;
                    IChunkBlocks data = request.Chunks[chunkY].Data;
                    int hostBlockId = data.GetBlockIdUnsafe(index3d);

                    int targetSlot = zoneSlots[(int)sample.Zone];
                    int grade = sample.Grade;

                    // Weathering disabled: keep the hypogene assemblage all the way to the surface.
                    if (!compiled.Definition.Weathering.Enabled
                        && (sample.Zone == SchwazTetrahedriteZone.Gossan
                            || sample.Zone == SchwazTetrahedriteZone.GreenGossan))
                    {
                        continue;
                    }

                    if (targetSlot < 0) continue;

                    bool isSoil = targetSlot == gossanSlot || targetSlot == greenGossanSlot;
                    if (isSoil)
                    {
                        if (!CanPlaceWeatheredSoil(compiled, hostBlockId)) continue;
                    }
                    else if (!CanReplaceWithProceduralRock(compiled, hostBlockId))
                    {
                        continue;
                    }

                    int placeBlockId = isSoil
                        ? compiled.ResolveWeatheredBlock(targetSlot, grade, hostBlockId, y < surfaceY)
                        : compiled.ResolveBlock(targetSlot, grade, hostBlockId);
                    if (placeBlockId == 0) continue;

                    data.SetBlockUnsafe(index3d, placeBlockId);
                    data.SetFluid(index3d, 0);
                }
            }
        }
    }

    private static int[] BuildSchwazTetrahedriteZoneSlots(CompiledProceduralDeposit compiled)
    {
        var slots = new int[Enum.GetValues<SchwazTetrahedriteZone>().Length];
        Array.Fill(slots, -1);
        slots[(int)SchwazTetrahedriteZone.Tetrahedrite] = compiled.GetSlotId(ProceduralMaterialSlots.Tetrahedrite);
        slots[(int)SchwazTetrahedriteZone.Chalcopyrite] = compiled.GetSlotId(ProceduralMaterialSlots.Chalcopyrite);
        slots[(int)SchwazTetrahedriteZone.Barite] = compiled.GetSlotId(ProceduralMaterialSlots.Barite);
        slots[(int)SchwazTetrahedriteZone.Malachite] = compiled.GetSlotId(ProceduralMaterialSlots.Malachite);
        slots[(int)SchwazTetrahedriteZone.Azurite] = compiled.GetSlotId(ProceduralMaterialSlots.Azurite);
        slots[(int)SchwazTetrahedriteZone.Limonite] = compiled.GetSlotId(ProceduralMaterialSlots.Limonite);
        slots[(int)SchwazTetrahedriteZone.Gossan] = compiled.GetSlotId(ProceduralMaterialSlots.Gossan);
        slots[(int)SchwazTetrahedriteZone.GreenGossan] = compiled.GetSlotId(ProceduralMaterialSlots.GreenGossan);
        slots[(int)SchwazTetrahedriteZone.Chalcocite] = compiled.GetSlotId(ProceduralMaterialSlots.Chalcocite);
        return slots;
    }
}
