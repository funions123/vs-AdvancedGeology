using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Vintagestory.API.Server;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace AdvancedGeology.WorldGen.ProceduralDeposits;

public enum MississippiValleyZone
{
    None = 0,
    Galena,
    Sphalerite,
    Chalcopyrite,
    Pyrite,
    Barite,
    Fluorite,
    Limonite,
    Smithsonite,
    Cerussite,
    Breccia,
    Gossan,
    GreyGossan,
    YellowGossan
}

[JsonObject(MemberSerialization.OptIn)]
public sealed class MississippiValleyDefinition
{
    [JsonProperty]
    public int HorizontalRadius { get; set; } = 42;

    [JsonProperty]
    public int VerticalHalfHeight { get; set; } = 35;
}

public readonly record struct MississippiValleySample(
    MississippiValleyZone Zone,
    int Grade = 1);

/// <summary>
/// Galena, sphalerite, minor chalcopyrite and pyrite, barite, fluorite, breccia, and secondary oxide minerals form carbonate-hosted mantos, collapse pipes, and fault-fed veins.
/// </summary>
internal sealed class MississippiValleyPlan
{
    private const ulong PlanSalt = 0x4D565450625A6E31UL; // "MVTPbZn1"
    private const double FaultAnchorY = -8.0;

    private readonly ulong featureId;
    private readonly int originX;
    private readonly int originY;
    private readonly int originZ;

    private readonly double bedDipDeg;
    private readonly double faultDipDeg;
    private readonly double faultStrikeDeg;
    private readonly double masterFaultX;
    private readonly double masterFaultZ;
    private readonly double seed;
    private readonly string clusterPattern;

    private readonly BrecciaPipe[] brecciaPipes;
    private readonly FaultSplay[] faultSplays;
    private readonly MantoLobe[] mantoLobes;
    private readonly CarbonateVein[] carbonateVeins;
    private readonly ReplacementChannel[] replacementChannels;

    public int OriginX => originX;
    public int OriginY => originY;
    public int OriginZ => originZ;
    public double BedDipDeg => bedDipDeg;
    public double FaultDipDeg => faultDipDeg;
    public double FaultStrikeDeg => faultStrikeDeg;
    public double MasterFaultX => masterFaultX;
    public double MasterFaultZ => masterFaultZ;
    public double Seed => seed;
    public string ClusterPattern => clusterPattern;
    public int BrecciaPipeCount => brecciaPipes.Length;
    public int FaultSplayCount => faultSplays.Length;
    public int MantoLobeCount => mantoLobes.Length;
    public int CarbonateVeinCount => carbonateVeins.Length;
    public int ReplacementChannelCount => replacementChannels.Length;

    private MississippiValleyPlan(
        ulong featureId,
        in ProceduralDepositInstance instance,
        double bedDipDeg,
        double faultDipDeg,
        double faultStrikeDeg,
        double masterFaultX,
        double masterFaultZ,
        double seed,
        string clusterPattern,
        BrecciaPipe[] brecciaPipes,
        FaultSplay[] faultSplays,
        MantoLobe[] mantoLobes,
        CarbonateVein[] carbonateVeins,
        ReplacementChannel[] replacementChannels)
    {
        this.featureId = featureId;
        originX = instance.CenterX;
        originY = instance.CenterY;
        originZ = instance.CenterZ;
        this.bedDipDeg = bedDipDeg;
        this.faultDipDeg = faultDipDeg;
        this.faultStrikeDeg = faultStrikeDeg;
        this.masterFaultX = masterFaultX;
        this.masterFaultZ = masterFaultZ;
        this.seed = seed;
        this.clusterPattern = clusterPattern;
        this.brecciaPipes = brecciaPipes;
        this.faultSplays = faultSplays;
        this.mantoLobes = mantoLobes;
        this.carbonateVeins = carbonateVeins;
        this.replacementChannels = replacementChannels;
    }

    public static MississippiValleyPlan Create(
        in ProceduralDepositInstance instance,
        MississippiValleyDefinition settings)
    {
        var random = new ProceduralDepositRandom(instance.FeatureId ^ PlanSalt);

        double bedDipDeg = random.Range(3.0, 8.0);
        double faultDipDeg = random.Range(58.0, 72.0);
        double faultStrikeDeg = random.Range(0.0, 180.0);
        double masterFaultX = random.Range(-2.0, 2.0);
        double masterFaultZ = random.Range(-2.0, 2.0);
        double seed = random.Range(0.0, 100.0);

        double pipeRoll = random.Range(0.0, 1.0);
        int pipeCount = pipeRoll < 0.35 ? 1 : (pipeRoll < 0.80 ? 2 : 3);
        double strikeRad = faultStrikeDeg * Math.PI / 180.0;
        double cosStrike = Math.Cos(strikeRad);
        double sinStrike = Math.Sin(strikeRad);

        string clusterPattern;
        var positions = new List<PipePosition>();

        if (pipeCount == 1)
        {
            clusterPattern = "single-center";
            positions.Add(new PipePosition(random.Range(-2.0, 2.0), random.Range(-2.5, 2.5)));
        }
        else
        {
            double patternRoll = random.Range(0.0, 1.0);
            if (patternRoll < 0.25) clusterPattern = "near-linear";
            else if (patternRoll < 0.65) clusterPattern = "en echelon";
            else if (patternRoll < 0.90) clusterPattern = "branched";
            else clusterPattern = "curved";

            if (pipeCount == 2)
            {
                positions.Add(new PipePosition(-(4.0 + random.Range(0.0, 1.0) * 7.0), 0.0));
                positions.Add(new PipePosition(4.0 + random.Range(0.0, 1.0) * 9.0, 0.0));
            }
            else
            {
                positions.Add(new PipePosition(-(9.0 + random.Range(0.0, 1.0) * 7.0), 0.0));
                positions.Add(new PipePosition(random.Range(-2.0, 2.0), 0.0));
                positions.Add(new PipePosition(9.0 + random.Range(0.0, 1.0) * 7.0, 0.0));
            }

            double direction = random.Range(0.0, 1.0) < 0.5 ? -1.0 : 1.0;
            if (clusterPattern == "near-linear")
            {
                for (int i = 0; i < positions.Count; i++)
                {
                    positions[i] = positions[i] with { Across = random.Range(-2.25, 2.25) };
                }
            }
            else if (clusterPattern == "en echelon")
            {
                double step = random.Range(3.0, 6.5);
                double centerIndex = (positions.Count - 1) * 0.5;
                for (int i = 0; i < positions.Count; i++)
                {
                    double across = direction * (i - centerIndex) * step + random.Range(-1.25, 1.25);
                    positions[i] = positions[i] with { Across = across };
                }
            }
            else if (clusterPattern == "branched")
            {
                for (int i = 0; i < positions.Count; i++)
                {
                    positions[i] = positions[i] with { Across = random.Range(-1.75, 1.75) };
                }
                int branchIndex = positions.Count - 1;
                double along = (random.Range(0.0, 1.0) - 0.35) * 8.0;
                double across = direction * random.Range(7.0, 11.0);
                positions[branchIndex] = new PipePosition(along, across);
            }
            else // curved
            {
                double maxAlong = 0.0;
                for (int i = 0; i < positions.Count; i++)
                {
                    maxAlong = Math.Max(maxAlong, Math.Abs(positions[i].Along));
                }
                double curveAmplitude = random.Range(5.0, 9.0);
                for (int i = 0; i < positions.Count; i++)
                {
                    double normalizedAlong = positions[i].Along / Math.Max(1.0, maxAlong);
                    double across = direction * normalizedAlong * normalizedAlong * curveAmplitude + random.Range(-1.25, 1.25);
                    positions[i] = positions[i] with { Across = across };
                }
            }
        }

        positions.Sort((left, right) => left.Along.CompareTo(right.Along));

        var brecciaPipes = new BrecciaPipe[pipeCount];
        var faultSplays = new FaultSplay[pipeCount];

        int mainPipeIndex = 0;
        for (int i = 1; i < positions.Count; i++)
        {
            if (Math.Abs(positions[i].Along) < Math.Abs(positions[mainPipeIndex].Along))
            {
                mainPipeIndex = i;
            }
        }

        for (int i = 0; i < pipeCount; i++)
        {
            double along = positions[i].Along;
            double across = positions[i].Across;
            double centerX = masterFaultX + along * cosStrike + across * sinStrike;
            double centerZ = masterFaultZ - along * sinStrike + across * cosStrike;
            bool isMainPipe = i == mainPipeIndex;
            double radius = isMainPipe ? random.Range(5.2, 7.0) : random.Range(3.3, 5.0);
            double phase = seed + 37.0 + i * 23.0;

            brecciaPipes[i] = new BrecciaPipe(
                centerX,
                centerZ,
                along,
                across,
                radius,
                phase);

            faultSplays[i] = new FaultSplay(
                centerX,
                centerZ,
                faultStrikeDeg + random.Range(-15.0, 15.0),
                faultDipDeg + random.Range(-7.0, 7.0),
                seed + 91.0 + i * 17.0);
        }

        int lobeCount = Math.Min(4, Math.Max(2, pipeCount + 1));
        var mantoLobes = new MantoLobe[lobeCount];

        for (int i = 0; i < lobeCount; i++)
        {
            double centerAlong;
            double centerAcross;
            double bridgeHalfLength = 0.0;

            if (pipeCount > 1 && i < pipeCount - 1)
            {
                BrecciaPipe left = brecciaPipes[i];
                BrecciaPipe right = brecciaPipes[i + 1];
                centerAlong = (left.CenterAlong + right.CenterAlong) * 0.5;
                centerAcross = (left.CenterAcross + right.CenterAcross) * 0.5;
                bridgeHalfLength = Math.Abs(right.CenterAlong - left.CenterAlong) * 0.5;
            }
            else
            {
                BrecciaPipe source = brecciaPipes[i % pipeCount];
                centerAlong = source.CenterAlong + random.Range(-2.5, 2.5);
                centerAcross = source.CenterAcross + random.Range(-2.0, 2.0);
            }

            double r1 = random.Range(10.0, 16.0);
            double r2 = bridgeHalfLength + random.Range(6.0, 10.0);
            double radiusAlong = Math.Max(r1, r2);
            double radiusAcross = random.Range(6.5, 10.5);
            double halfThickness = random.Range(1.1, 2.2);
            double centerStratY = -5.5 + (i % 2) * random.Range(2.0, 3.5);

            mantoLobes[i] = new MantoLobe(
                centerAlong,
                centerAcross,
                centerStratY,
                radiusAlong,
                radiusAcross,
                halfThickness,
                seed + i * 19.0);
        }

        var carbonateVeinsList = new List<CarbonateVein>();
        for (int i = 0; i < brecciaPipes.Length; i++)
        {
            BrecciaPipe pipe = brecciaPipes[i];
            int veinCount = 2 + random.NextInt(0, 3);
            for (int j = 0; j < veinCount; j++)
            {
                MantoLobe target = mantoLobes[0];
                double targetDistance = double.PositiveInfinity;
                for (int k = 0; k < mantoLobes.Length; k++)
                {
                    double distance = Math.Abs(mantoLobes[k].CenterAlong - pipe.CenterAlong)
                        + 0.6 * Math.Abs(mantoLobes[k].CenterAcross - pipe.CenterAcross);
                    if (distance < targetDistance)
                    {
                        target = mantoLobes[k];
                        targetDistance = distance;
                    }
                }

                double targetDx = target.CenterAlong - pipe.CenterAlong;
                double targetDz = target.CenterAcross - pipe.CenterAcross;
                double targetDistanceXZ = Math.Sqrt(targetDx * targetDx + targetDz * targetDz);
                double targetScale = targetDistanceXZ > 7.0 ? 7.0 / targetDistanceXZ : 1.0;
                double endAlong = pipe.CenterAlong + targetDx * targetScale + random.Range(-1.5, 1.5);
                double endAcross = pipe.CenterAcross + targetDz * targetScale + random.Range(-1.5, 1.5);
                double endX = masterFaultX + endAlong * cosStrike + endAcross * sinStrike;
                double endZ = masterFaultZ - endAlong * sinStrike + endAcross * cosStrike;
                double endStratY = target.CenterStratY + random.Range(-0.5, 0.5) * target.HalfThickness;
                double endY = endStratY
                    + Math.Tan(bedDipDeg * Math.PI / 180.0) * endX
                    + 0.06 * endZ
                    + Math.Sin(endX * 0.06 + seed) * 1.5;

                carbonateVeinsList.Add(new CarbonateVein(
                    pipe.CenterX + random.Range(-0.35, 0.35) * pipe.Radius,
                    random.Range(-16.0, -12.0),
                    pipe.CenterZ + random.Range(-0.35, 0.35) * pipe.Radius,
                    endX,
                    endY,
                    endZ,
                    random.Range(0.38, 0.93),
                    pipe.Phase + j * 11.0));
            }
        }

        var sortedLobes = new List<MantoLobe>(mantoLobes);
        sortedLobes.Sort((left, right) => left.CenterAlong.CompareTo(right.CenterAlong));

        int span = Math.Max(1, sortedLobes.Count - 1);
        int channelCount = span + random.NextInt(0, 2);
        var replacementChannels = new ReplacementChannel[channelCount];

        for (int i = 0; i < channelCount; i++)
        {
            MantoLobe left = sortedLobes[i % span];
            MantoLobe right = sortedLobes[Math.Min(sortedLobes.Count - 1, i % span + 1)];

            replacementChannels[i] = new ReplacementChannel(
                left.CenterAlong + random.Range(-0.225, 0.225) * left.RadiusAlong,
                left.CenterAcross + random.Range(-0.175, 0.175) * left.RadiusAcross,
                right.CenterAlong + random.Range(-0.225, 0.225) * right.RadiusAlong,
                right.CenterAcross + random.Range(-0.175, 0.175) * right.RadiusAcross,
                (left.CenterStratY + right.CenterStratY) * 0.5 + random.Range(-0.6, 0.6),
                random.Range(0.75, 1.60),
                random.Range(0.28, 0.70),
                seed + 173.0 + i * 13.0);
        }

        return new MississippiValleyPlan(
            instance.FeatureId,
            instance,
            bedDipDeg,
            faultDipDeg,
            faultStrikeDeg,
            masterFaultX,
            masterFaultZ,
            seed,
            clusterPattern,
            brecciaPipes,
            faultSplays,
            mantoLobes,
            carbonateVeinsList.ToArray(),
            replacementChannels);
    }

    public static double Hash3D(double x, double y, double z)
    {
        double n = Math.Sin(x * 12.9898 + y * 78.233 + z * 37.719) * 43758.5453;
        return n - Math.Floor(n);
    }

    private static MantoSample SampleManto(double xRot, double zRot, double stratY, in MantoLobe lobe)
    {
        double along = xRot - lobe.CenterAlong;
        double across = zRot - lobe.CenterAcross;
        double u = along / lobe.RadiusAlong;
        double v = across / lobe.RadiusAcross;
        double edgeWarp = Math.Sin(u * 3.7 + lobe.Phase) * Math.Cos(v * 3.2) * 0.14
            + Math.Sin(u * 7.1 - v * 4.3 + lobe.Phase * 0.5) * 0.06;
        double footprint = Math.Sqrt(u * u + v * v) + edgeWarp;
        double taper = Math.Max(0.0, 1.0 - footprint * footprint);
        double halfThickness = 0.35 + lobe.HalfThickness * taper;
        double verticalDistance = Math.Abs(stratY - lobe.CenterStratY);
        return new MantoSample(
            footprint <= 1.04 && verticalDistance <= halfThickness,
            footprint <= 1.24 && verticalDistance <= halfThickness + 1.8,
            footprint + verticalDistance / Math.Max(0.35, halfThickness),
            footprint);
    }

    private static VeinSample SampleCarbonateVein(double x, double y, double z, in CarbonateVein vein)
    {
        double vx = vein.EndX - vein.StartX;
        double vy = vein.EndY - vein.StartY;
        double vz = vein.EndZ - vein.StartZ;
        double lengthSquared = vx * vx + vy * vy + vz * vz;
        double t = Math.Clamp(
            ((x - vein.StartX) * vx + (y - vein.StartY) * vy + (z - vein.StartZ) * vz) / Math.Max(0.1, lengthSquared),
            0.0,
            1.0);
        double horizontalLength = Math.Sqrt(vx * vx + vz * vz);
        double bend = Math.Sin(Math.PI * t) * Math.Sin(t * 4.2 + vein.Phase) * 1.2;
        double bendX = horizontalLength > 0.1 ? -vz / horizontalLength * bend : 0.0;
        double bendZ = horizontalLength > 0.1 ? vx / horizontalLength * bend : 0.0;
        double centerX = vein.StartX + vx * t + bendX;
        double centerY = vein.StartY + vy * t + Math.Sin(Math.PI * t + vein.Phase) * 0.45;
        double centerZ = vein.StartZ + vz * t + bendZ;
        double dx = x - centerX;
        double dy = y - centerY;
        double dz = z - centerZ;
        double distance = Math.Sqrt(dx * dx + dy * dy + dz * dz);
        double radius = vein.Thickness * (0.82 + 0.18 * Math.Sin(t * 6.0 + vein.Phase));
        return new VeinSample(
            distance <= radius,
            distance <= radius + 1.1,
            distance / Math.Max(0.1, radius),
            t);
    }

    private static ChannelSample SampleReplacementChannel(double xRot, double zRot, double stratY, in ReplacementChannel channel)
    {
        double vx = channel.EndAlong - channel.StartAlong;
        double vz = channel.EndAcross - channel.StartAcross;
        double lengthSquared = vx * vx + vz * vz;
        double t = Math.Clamp(
            ((xRot - channel.StartAlong) * vx + (zRot - channel.StartAcross) * vz) / Math.Max(0.1, lengthSquared),
            0.0,
            1.0);
        double horizontalLength = Math.Sqrt(lengthSquared);
        double bend = Math.Sin(Math.PI * t) * Math.Sin(t * 5.1 + channel.Phase) * 1.0;
        double centerAlong = channel.StartAlong + vx * t + (horizontalLength > 0.1 ? -vz / horizontalLength * bend : 0.0);
        double centerAcross = channel.StartAcross + vz * t + (horizontalLength > 0.1 ? vx / horizontalLength * bend : 0.0);
        double horizontalDistance = Math.Sqrt(
            (xRot - centerAlong) * (xRot - centerAlong)
            + (zRot - centerAcross) * (zRot - centerAcross));
        double centerStratY = channel.CenterStratY + Math.Sin(t * 4.0 + channel.Phase) * 0.35;
        double verticalDistance = Math.Abs(stratY - centerStratY);
        return new ChannelSample(
            horizontalDistance <= channel.Width && verticalDistance <= channel.HalfThickness,
            horizontalDistance <= channel.Width + 1.2 && verticalDistance <= channel.HalfThickness + 1.0,
            horizontalDistance / Math.Max(0.1, channel.Width) + verticalDistance / Math.Max(0.1, channel.HalfThickness),
            t);
    }

    private double GetMasterFaultDistance(double x, double y, double z)
    {
        double strikeRad = faultStrikeDeg * Math.PI / 180.0;
        double dipRad = faultDipDeg * Math.PI / 180.0;
        double dx = x - masterFaultX;
        double dz = z - masterFaultZ;
        double along = dx * Math.Cos(strikeRad) - dz * Math.Sin(strikeRad);
        double across = dx * Math.Sin(strikeRad) + dz * Math.Cos(strikeRad);
        double kink = Math.Sin(along * 0.15 + y * 0.08 + seed) * 0.8;
        return Math.Abs(across * Math.Sin(dipRad) + (y - FaultAnchorY) * Math.Cos(dipRad) - kink);
    }

    private double GetSplayFaultDistance(double x, double y, double z, in FaultSplay splay)
    {
        double rawMerge = Math.Clamp((-8.0 - y) / 14.0, 0.0, 1.0);
        double merge = rawMerge * rawMerge * (3.0 - 2.0 * rawMerge);
        double originX = splay.OriginX * (1.0 - merge) + masterFaultX * merge;
        double originZ = splay.OriginZ * (1.0 - merge) + masterFaultZ * merge;
        double strikeDeg = splay.StrikeDeg * (1.0 - merge) + faultStrikeDeg * merge;
        double dipDeg = splay.DipDeg * (1.0 - merge) + faultDipDeg * merge;
        double strikeRad = strikeDeg * Math.PI / 180.0;
        double dipRad = dipDeg * Math.PI / 180.0;
        double dx = x - originX;
        double dz = z - originZ;
        double along = dx * Math.Cos(strikeRad) - dz * Math.Sin(strikeRad);
        double across = dx * Math.Sin(strikeRad) + dz * Math.Cos(strikeRad);
        double splayKink = Math.Sin(along * 0.17 + y * 0.09 + splay.Phase) * 0.9;
        double masterKink = Math.Sin(along * 0.15 + y * 0.08 + seed) * 0.8;
        double kink = splayKink * (1.0 - merge) + masterKink * merge;
        return Math.Abs(across * Math.Sin(dipRad) + (y - FaultAnchorY) * Math.Cos(dipRad) - kink);
    }

    public MississippiValleySample Evaluate(
        int worldX,
        int worldY,
        int worldZ,
        int surfaceY,
        bool weatheringEnabled)
    {
        if (worldY > surfaceY)
        {
            return new MississippiValleySample(MississippiValleyZone.None, 0);
        }

        double x = worldX - originX;
        double y = worldY - originY;
        double z = worldZ - originZ;

    // Deposit package lower limit: y < -26
        if (y < -26.0)
        {
            return new MississippiValleySample(MississippiValleyZone.None, 0);
        }

        int depth = surfaceY - worldY + 1;
        if (depth < 1)
        {
            return new MississippiValleySample(MississippiValleyZone.None, 0);
        }

        double bedDipRad = bedDipDeg * (Math.PI / 180.0);
        double stratY = y - (Math.Tan(bedDipRad) * x + 0.06 * z + Math.Sin(x * 0.06 + seed) * 1.5);

        // Breccia pipes
        PipeResult? bestPipe = null;
        double nearestPipeMargin = double.PositiveInfinity;
        for (int i = 0; i < brecciaPipes.Length; i++)
        {
            ref readonly BrecciaPipe pipe = ref brecciaPipes[i];
            double pipeX = pipe.CenterX + Math.Sin(y * 0.12 + pipe.Phase) * 1.4;
            double pipeZ = pipe.CenterZ + Math.Cos(y * 0.14 + pipe.Phase * 0.7) * 1.2;
            double radius = pipe.Radius + Math.Sin(y * 0.20 + pipe.Phase) * 0.65;
            double dx = x - pipeX;
            double dz = z - pipeZ;
            double distance = Math.Sqrt(dx * dx + dz * dz);
            double margin = distance - radius;
            nearestPipeMargin = Math.Min(nearestPipeMargin, margin);
            if (distance <= radius && stratY >= -16.0 && stratY <= 7.5)
            {
                double score = distance / Math.Max(0.5, radius);
                if (!bestPipe.HasValue || score < bestPipe.Value.Score)
                {
                    bestPipe = new PipeResult(pipe, radius, dx, dz, distance, score);
                }
            }
        }
        bool inBrecciaPipe = bestPipe.HasValue;

        // Fault splays and master fault
        double masterFaultDistance = GetMasterFaultDistance(x, y, z);
        double nearestSplayDistance = double.PositiveInfinity;
        for (int i = 0; i < faultSplays.Length; i++)
        {
            nearestSplayDistance = Math.Min(nearestSplayDistance, GetSplayFaultDistance(x, y, z, faultSplays[i]));
        }
        double distToFault = Math.Min(masterFaultDistance, nearestSplayDistance);
        bool inFaultDamage = distToFault <= 1.8 && stratY >= -24.0 && stratY <= 12.0;

        double masterStrikeRad = faultStrikeDeg * Math.PI / 180.0;
        double masterDx = x - masterFaultX;
        double masterDz = z - masterFaultZ;
        double xRot = masterDx * Math.Cos(masterStrikeRad) - masterDz * Math.Sin(masterStrikeRad);
        double zRot = masterDx * Math.Sin(masterStrikeRad) + masterDz * Math.Cos(masterStrikeRad);

        // Manto lobes
        MantoSample? bestManto = null;
        bool inMantoHalo = false;
        for (int i = 0; i < mantoLobes.Length; i++)
        {
            MantoSample sample = SampleManto(xRot, zRot, stratY, mantoLobes[i]);
            if (sample.InHalo) inMantoHalo = true;
            if (sample.Inside && (!bestManto.HasValue || sample.Score < bestManto.Value.Score))
            {
                bestManto = sample;
            }
        }

        // Carbonate veins
        VeinSample? bestCarbonateVein = null;
        bool inCarbonateVeinHalo = false;
        for (int i = 0; i < carbonateVeins.Length; i++)
        {
            VeinSample sample = SampleCarbonateVein(x, y, z, carbonateVeins[i]);
            if (sample.InHalo) inCarbonateVeinHalo = true;
            if (sample.Inside && (!bestCarbonateVein.HasValue || sample.Score < bestCarbonateVein.Value.Score))
            {
                bestCarbonateVein = sample;
            }
        }

        // Replacement channels
        ChannelSample? bestReplacementChannel = null;
        bool inReplacementChannelHalo = false;
        for (int i = 0; i < replacementChannels.Length; i++)
        {
            ChannelSample sample = SampleReplacementChannel(xRot, zRot, stratY, replacementChannels[i]);
            if (sample.InHalo) inReplacementChannelHalo = true;
            if (sample.Inside && (!bestReplacementChannel.HasValue || sample.Score < bestReplacementChannel.Value.Score))
            {
                bestReplacementChannel = sample;
            }
        }

        bool inReceptiveBed = Math.Abs(stratY + 5.0) <= 3.0;
        bool inMineralizedFault = distToFault <= 0.85
            && (inReceptiveBed || inBrecciaPipe || nearestPipeMargin <= 1.5)
            && stratY >= -18.5 && stratY <= 8.0;

        // Continuous low-frequency fields
        double replacementField = Math.Sin(x * 0.19 + z * 0.11 + seed)
            + 0.62 * Math.Cos(stratY * 0.47 - x * 0.08)
            + 0.35 * Math.Sin(z * 0.23 - y * 0.16 + seed * 0.4);
        double metalFacies = Math.Sin(x * 0.17 - z * 0.13 + seed * 0.7)
            + 0.55 * Math.Cos(stratY * 0.52 + x * 0.09);
        double ironFacies = Math.Cos(x * 0.21 + z * 0.18 - seed)
            + 0.40 * Math.Sin(stratY * 0.61);
        double cementFacies = Math.Sin(x * 0.28 - z * 0.24 + seed * 0.5)
            + 0.48 * Math.Cos(y * 0.31 + z * 0.12);
        double gangueFacies = Math.Sin(x * 0.34 + z * 0.29 + seed)
            + 0.58 * Math.Cos(y * 0.37 - x * 0.15);

        // Breccia clasts within dissolution-collapse pipe
        double pipeDx = bestPipe.HasValue ? bestPipe.Value.Dx : 0.0;
        double pipeDz = bestPipe.HasValue ? bestPipe.Value.Dz : 0.0;
        double pipeRadius = bestPipe.HasValue ? bestPipe.Value.Radius : 1.0;
        double pipePhase = bestPipe.HasValue ? bestPipe.Value.Pipe.Phase : seed;
        double distToPipe = bestPipe.HasValue ? bestPipe.Value.Distance : double.PositiveInfinity;
        double clastField = Math.Sin(pipeDx * 0.88 + stratY * 0.31)
            + Math.Cos(pipeDz * 0.93 - stratY * 0.27)
            + 0.55 * Math.Sin((pipeDx - pipeDz) * 0.64 + pipePhase);
        bool isBrecciaClast = inBrecciaPipe && distToPipe < pipeRadius * 0.92 && clastField > 1.05;
        bool brecciaMatrix = inBrecciaPipe && !isBrecciaClast;

        bool mineralizedManto = bestManto.HasValue && replacementField > -0.18;
        bool mineralizedBreccia = brecciaMatrix && replacementField > -0.18;
        bool mineralizedFault = inMineralizedFault && replacementField > 0.05;
        bool mineralizedCarbonateVein = bestCarbonateVein.HasValue && replacementField > -0.02;
        bool mineralizedReplacementChannel = bestReplacementChannel.HasValue && replacementField > -0.05;

        bool isPrimaryMineralized = mineralizedManto
            || mineralizedBreccia
            || mineralizedFault
            || mineralizedCarbonateVein
            || mineralizedReplacementChannel;

        if (isBrecciaClast)
        {
            return new MississippiValleySample(MississippiValleyZone.Breccia, 0);
        }

        if (!isPrimaryMineralized)
        {
            return new MississippiValleySample(MississippiValleyZone.None, 0);
        }

        bool openSpaceContext = brecciaMatrix || distToFault <= 0.48 || bestCarbonateVein.HasValue;

        // Grain noise for deterministic mineral grade variation (0=poor, 1=medium, 2=rich, 3=bountiful)
        double grainNoise = Hash3D(x * 2.7 + seed * 1.3, y * 2.7, z * 2.7);
        int grade = grainNoise > 0.82 ? 3 : (grainNoise > 0.55 ? 2 : (grainNoise > 0.25 ? 1 : 0));

        MississippiValleyZone hypogeneZone;

        // Fluorite and barite form sparse coherent cavity/cement pockets.
        if (openSpaceContext && gangueFacies > 1.22)
        {
            hypogeneZone = MississippiValleyZone.Fluorite;
        }
        else if (openSpaceContext && gangueFacies < -1.22)
        {
            hypogeneZone = MississippiValleyZone.Barite;
        }
        else
        {
            // Rare copper is restricted to highest-flux conduit intersections.
            double rareCopper = Hash3D(x * 2.3 + seed, y * 2.3, z * 2.3);
            if (rareCopper > 0.987 && (distToFault <= 0.48 || distToPipe < pipeRadius * 0.45))
            {
                hypogeneZone = MississippiValleyZone.Chalcopyrite;
            }
            else if (bestCarbonateVein.HasValue)
            {
                // Connector veins remain carbonate-dominant; sulfides form local shoots.
                if (cementFacies > -0.55 || replacementField < 0.45)
                {
                    return new MississippiValleySample(MississippiValleyZone.None, 0);
                }
                if (ironFacies > 0.82)
                {
                    hypogeneZone = MississippiValleyZone.Pyrite;
                }
                else
                {
                    hypogeneZone = metalFacies > 0.45 ? MississippiValleyZone.Sphalerite : MississippiValleyZone.Galena;
                }
            }
            else if (bestReplacementChannel.HasValue)
            {
                if (cementFacies > -0.45 || replacementField < 0.65)
                {
                    return new MississippiValleySample(MississippiValleyZone.None, 0);
                }
                if (ironFacies > 0.96)
                {
                    hypogeneZone = MississippiValleyZone.Pyrite;
                }
                else
                {
                    hypogeneZone = metalFacies > 0.30 ? MississippiValleyZone.Sphalerite : MississippiValleyZone.Galena;
                }
            }
            else
            {
                if (brecciaMatrix && cementFacies > 0.42)
                {
                    return new MississippiValleySample(MississippiValleyZone.None, 0);
                }
                if (ironFacies > 0.92)
                {
                    hypogeneZone = MississippiValleyZone.Pyrite;
                }
                else
                {
                    hypogeneZone = metalFacies > 0.08 ? MississippiValleyZone.Sphalerite : MississippiValleyZone.Galena;
                }
            }
        }

        // Supergene succession:
        // depth 1-3: stained gossan soil (galena -> grey gossan, sphalerite -> yellow gossan, pyrite/chalcopyrite -> red gossan)
        // depth 4-8: secondary oxidized ore (galena -> cerussite, sphalerite -> smithsonite, pyrite/chalcopyrite -> limonite)
        // barite, fluorite: unaltered at all depths
        // depth > 8: unaltered hypogene assemblage
        if (weatheringEnabled && depth <= 8)
        {
            if (depth <= 3)
            {
                switch (hypogeneZone)
                {
                    case MississippiValleyZone.Galena:
                        return new MississippiValleySample(MississippiValleyZone.GreyGossan, 0);
                    case MississippiValleyZone.Sphalerite:
                        return new MississippiValleySample(MississippiValleyZone.YellowGossan, 0);
                    case MississippiValleyZone.Pyrite:
                    case MississippiValleyZone.Chalcopyrite:
                        return new MississippiValleySample(MississippiValleyZone.Gossan, 0);
                    case MississippiValleyZone.Barite:
                        return new MississippiValleySample(MississippiValleyZone.Barite, 0);
                    case MississippiValleyZone.Fluorite:
                        return new MississippiValleySample(MississippiValleyZone.Fluorite, 0);
                    default:
                        return new MississippiValleySample(MississippiValleyZone.None, 0);
                }
            }
            else // depth 4..8
            {
                switch (hypogeneZone)
                {
                    case MississippiValleyZone.Galena:
                        return new MississippiValleySample(MississippiValleyZone.Cerussite, grade);
                    case MississippiValleyZone.Sphalerite:
                        return new MississippiValleySample(MississippiValleyZone.Smithsonite, grade);
                    case MississippiValleyZone.Pyrite:
                    case MississippiValleyZone.Chalcopyrite:
                        return new MississippiValleySample(MississippiValleyZone.Limonite, grade);
                    case MississippiValleyZone.Barite:
                        return new MississippiValleySample(MississippiValleyZone.Barite, 0);
                    case MississippiValleyZone.Fluorite:
                        return new MississippiValleySample(MississippiValleyZone.Fluorite, 0);
                    default:
                        return new MississippiValleySample(MississippiValleyZone.None, 0);
                }
            }
        }

        return new MississippiValleySample(hypogeneZone, grade);
    }

    private readonly record struct PipePosition(double Along, double Across);

    private readonly record struct BrecciaPipe(
        double CenterX,
        double CenterZ,
        double CenterAlong,
        double CenterAcross,
        double Radius,
        double Phase);

    private readonly record struct FaultSplay(
        double OriginX,
        double OriginZ,
        double StrikeDeg,
        double DipDeg,
        double Phase);

    private readonly record struct MantoLobe(
        double CenterAlong,
        double CenterAcross,
        double CenterStratY,
        double RadiusAlong,
        double RadiusAcross,
        double HalfThickness,
        double Phase);

    private readonly record struct CarbonateVein(
        double StartX,
        double StartY,
        double StartZ,
        double EndX,
        double EndY,
        double EndZ,
        double Thickness,
        double Phase);

    private readonly record struct ReplacementChannel(
        double StartAlong,
        double StartAcross,
        double EndAlong,
        double EndAcross,
        double CenterStratY,
        double Width,
        double HalfThickness,
        double Phase);

    private readonly record struct PipeResult(
        BrecciaPipe Pipe,
        double Radius,
        double Dx,
        double Dz,
        double Distance,
        double Score);

    private readonly record struct MantoSample(
        bool Inside,
        bool InHalo,
        double Score,
        double Footprint);

    private readonly record struct VeinSample(
        bool Inside,
        bool InHalo,
        double Score,
        double Progress);

    private readonly record struct ChannelSample(
        bool Inside,
        bool InHalo,
        double Score,
        double Progress);
}

internal sealed class MississippiValleyProceduralTemplate : IProceduralDepositTemplate
{
    private static readonly string[] RequiredSlots =
    {
        ProceduralMaterialSlots.Galena,
        ProceduralMaterialSlots.Sphalerite,
        ProceduralMaterialSlots.Chalcopyrite,
        ProceduralMaterialSlots.Pyrite,
        ProceduralMaterialSlots.Barite,
        ProceduralMaterialSlots.Fluorite,
        ProceduralMaterialSlots.Limonite,
        ProceduralMaterialSlots.Smithsonite,
        ProceduralMaterialSlots.Cerussite,
        ProceduralMaterialSlots.Breccia,
        ProceduralMaterialSlots.Gossan,
        ProceduralMaterialSlots.GreyGossan,
        ProceduralMaterialSlots.YellowGossan
    };

    public string Code => "mississippiValley";
    public IReadOnlyList<string> RequiredMaterialSlots => RequiredSlots;
    public bool RequiresPlan => true;

    public int GetMaximumHorizontalReach(ProceduralDepositDefinition definition)
    {
        return definition.Mvt.HorizontalRadius;
    }

    public object CreatePlan(in ProceduralDepositInstance instance, ProceduralDepositDefinition definition)
    {
        return MississippiValleyPlan.Create(instance, definition.Mvt);
    }

    public bool Validate(ProceduralDepositDefinition definition, out string error)
    {
        MississippiValleyDefinition settings = definition.Mvt;
        bool valid = settings.HorizontalRadius >= 16
            && settings.VerticalHalfHeight >= 8;
        error = valid ? string.Empty : "invalid mississippi valley settings";
        return valid;
    }

    public void Realize(
        ProceduralDepositWorldGenSystem system,
        in DepositCandidate candidate,
        IChunkColumnGenerateRequest request,
        int baseX,
        int baseZ)
    {
        system.RealizeMississippiValleyCandidate(candidate, request, baseX, baseZ);
    }
}

public sealed partial class ProceduralDepositWorldGenSystem
{
    internal void RealizeMississippiValleyCandidate(
        DepositCandidate candidate,
        IChunkColumnGenerateRequest request,
        int baseX,
        int baseZ)
    {
        CompiledProceduralDeposit compiled = candidate.Compiled;
        MississippiValleyDefinition settings = compiled.Definition.Mvt;
        var plan = (MississippiValleyPlan)candidate.Plan!;
        ushort[] heightMap = request.Chunks[0].MapChunk.WorldGenTerrainHeightMap;
        int worldHeight = serverApi!.World.BlockAccessor.MapSizeY;
        int minimumY = Math.Clamp(candidate.Instance.CenterY - settings.VerticalHalfHeight, 1, worldHeight - 2);
        int maximumPrototypeY = Math.Clamp(candidate.Instance.CenterY + settings.VerticalHalfHeight, 1, worldHeight - 2);

        int[] zoneSlots = BuildMississippiValleyZoneSlots(compiled);
        int gossanSlot = compiled.GetSlotId(ProceduralMaterialSlots.Gossan);
        int greyGossanSlot = compiled.GetSlotId(ProceduralMaterialSlots.GreyGossan);
        int yellowGossanSlot = compiled.GetSlotId(ProceduralMaterialSlots.YellowGossan);

        bool weatheringEnabled = compiled.Definition.Weathering.Enabled;

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
                    MississippiValleySample sample = plan.Evaluate(worldX, y, worldZ, surfaceY, weatheringEnabled);
                    if (sample.Zone == MississippiValleyZone.None) continue;

                    int chunkY = y / ChunkSize;
                    if ((uint)chunkY >= (uint)request.Chunks.Length) continue;
                    int localY = y % ChunkSize;
                    int index3d = ((localY * ChunkSize) + localZ) * ChunkSize + localX;
                    IChunkBlocks data = request.Chunks[chunkY].Data;
                    int hostBlockId = data.GetBlockIdUnsafe(index3d);

                    int targetSlot = zoneSlots[(int)sample.Zone];
                    if (targetSlot < 0) continue;

                    bool isSoil = targetSlot == gossanSlot || targetSlot == greyGossanSlot || targetSlot == yellowGossanSlot;
                    if (isSoil)
                    {
                        if (!CanPlaceWeatheredSoil(compiled, hostBlockId)) continue;
                    }
                    else if (!CanReplaceWithProceduralRock(compiled, hostBlockId))
                    {
                        continue;
                    }

                    int placeBlockId = isSoil
                        ? compiled.ResolveWeatheredBlock(targetSlot, sample.Grade, hostBlockId, y < surfaceY)
                        : compiled.ResolveBlock(targetSlot, sample.Grade, hostBlockId);
                    if (placeBlockId == 0) continue;

                    data.SetBlockUnsafe(index3d, placeBlockId);
                    data.SetFluid(index3d, 0);
                }
            }
        }
    }

    private static int[] BuildMississippiValleyZoneSlots(CompiledProceduralDeposit compiled)
    {
        var slots = new int[Enum.GetValues<MississippiValleyZone>().Length];
        Array.Fill(slots, -1);
        slots[(int)MississippiValleyZone.Galena] = compiled.GetSlotId(ProceduralMaterialSlots.Galena);
        slots[(int)MississippiValleyZone.Sphalerite] = compiled.GetSlotId(ProceduralMaterialSlots.Sphalerite);
        slots[(int)MississippiValleyZone.Chalcopyrite] = compiled.GetSlotId(ProceduralMaterialSlots.Chalcopyrite);
        slots[(int)MississippiValleyZone.Pyrite] = compiled.GetSlotId(ProceduralMaterialSlots.Pyrite);
        slots[(int)MississippiValleyZone.Barite] = compiled.GetSlotId(ProceduralMaterialSlots.Barite);
        slots[(int)MississippiValleyZone.Fluorite] = compiled.GetSlotId(ProceduralMaterialSlots.Fluorite);
        slots[(int)MississippiValleyZone.Limonite] = compiled.GetSlotId(ProceduralMaterialSlots.Limonite);
        slots[(int)MississippiValleyZone.Smithsonite] = compiled.GetSlotId(ProceduralMaterialSlots.Smithsonite);
        slots[(int)MississippiValleyZone.Cerussite] = compiled.GetSlotId(ProceduralMaterialSlots.Cerussite);
        slots[(int)MississippiValleyZone.Breccia] = compiled.GetSlotId(ProceduralMaterialSlots.Breccia);
        slots[(int)MississippiValleyZone.Gossan] = compiled.GetSlotId(ProceduralMaterialSlots.Gossan);
        slots[(int)MississippiValleyZone.GreyGossan] = compiled.GetSlotId(ProceduralMaterialSlots.GreyGossan);
        slots[(int)MississippiValleyZone.YellowGossan] = compiled.GetSlotId(ProceduralMaterialSlots.YellowGossan);
        return slots;
    }
}
