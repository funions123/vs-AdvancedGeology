using System;
using System.Collections.Generic;

namespace AdvancedGeology.WorldGen.ProceduralDeposits;

internal enum EpithermalMaterialZone
{
    Quartz,
    Acanthite,
    Electrum,
    NativeSilver
}

internal readonly record struct EpithermalVeinSample(
    bool Inside,
    double Score,
    double CenterRatio,
    double LocalY);

/// <summary>
/// Quartz, acanthite, electrum, and native silver form steep, branching epithermal veins that merge downward into feeder structures.
/// </summary>
internal sealed class EpithermalVeinPlan
{
    private const double MergeStartY = -12;
    private const double MergeEndY = -28;

    private readonly ulong featureId;
    private readonly int originX;
    private readonly int originY;
    private readonly int originZ;
    private readonly EpithermalVeinMember[] veins;
    private readonly EpithermalFeeder[] feeders;

    private EpithermalVeinPlan(
        ulong featureId,
        int originX,
        int originY,
        int originZ,
        EpithermalVeinMember[] veins,
        EpithermalFeeder[] feeders)
    {
        this.featureId = featureId;
        this.originX = originX;
        this.originY = originY;
        this.originZ = originZ;
        this.veins = veins;
        this.feeders = feeders;
    }

    public static EpithermalVeinPlan Create(
        in ProceduralDepositInstance instance,
        EpithermalVeinDefinition definition)
    {
        var random = new EpithermalRandom(instance.FeatureId ^ 0x455049544845524DUL);
        double principalDip = random.Range(60, 78);
        double regionalStrike = random.Range(-30, 30);
        int dipDirection = random.NextInt(0, 1) == 0 ? -1 : 1;
        int principalCount = random.NextInt(definition.PrincipalVeinMin, definition.PrincipalVeinMax);
        int maximumFeeders = Math.Min(3, (principalCount + 1) / 2);
        int feederCount = random.NextInt(1, maximumFeeders);

        var feeders = new EpithermalFeeder[feederCount];
        for (int index = 0; index < feederCount; index++)
        {
            double centeredIndex = index - (feederCount - 1) / 2.0;
            feeders[index] = EpithermalFeeder.Create(
                regionalStrike + random.Range(-5, 5),
                random.Range(30, 45),
                centeredIndex * random.Range(7, 10) + random.Range(-1.5, 1.5),
                random.Range(-4, 4),
                random.Range(-2, 1),
                random.Range(13, 20),
                random.Range(1.3, 2.2),
                random.Range(0, Math.PI * 2),
                random.Range(0, Math.PI * 2));
        }

        var offsets = new double[principalCount];
        for (int index = 1; index < principalCount; index++)
        {
            offsets[index] = offsets[index - 1] + random.Range(8, 14);
        }
        double offsetCenter = (offsets[0] + offsets[^1]) * 0.5;
        double familyShift = random.Range(-3, 3);

        int splayCount = random.NextInt(
            definition.SplayMin,
            Math.Min(definition.SplayMax, principalCount));
        var members = new List<EpithermalVeinMember>(principalCount + splayCount);
        for (int index = 0; index < principalCount; index++)
        {
            int feederIndex = Math.Min(feederCount - 1, index * feederCount / principalCount);
            members.Add(EpithermalVeinMember.Create(
                regionalStrike + random.Range(-8, 8),
                principalDip + random.Range(-6, 6),
                dipDirection,
                offsets[index] - offsetCenter + familyShift,
                random.Range(-5, 5),
                random.Range(-4, 2),
                random.Range(28, 42),
                random.Range(31, 39),
                random.Range(3.2, 4.8),
                random.Range(0, Math.PI * 2),
                random.Range(-12, 12),
                random.Range(3, 6),
                random.Range(-1.8, 1.8),
                random.Range(0, Math.PI * 2),
                feederIndex));
        }

        for (int index = 0; index < splayCount; index++)
        {
            int parentIndex = Math.Min(
                principalCount - 1,
                (int)Math.Floor((index + 0.5) * principalCount / splayCount));
            EpithermalVeinMember parent = members[parentIndex];
            int direction = index % 2 == 0 ? -1 : 1;
            members.Add(EpithermalVeinMember.Create(
                parent.StrikeDegrees + direction * random.Range(13, 23),
                parent.DipDegrees + random.Range(-8, 8),
                parent.DipDirection,
                parent.Offset + direction * random.Range(3, 6),
                parent.CenterAlong + direction * random.Range(7, 14),
                parent.CenterY + random.Range(2, 7),
                random.Range(13, 23),
                random.Range(17, 25),
                random.Range(2, 3.4),
                random.Range(0, Math.PI * 2),
                random.Range(-6, 6),
                random.Range(2.5, 4.5),
                random.Range(-1.1, 1.1),
                random.Range(0, Math.PI * 2),
                parent.FeederIndex));
        }

        return new EpithermalVeinPlan(
            instance.FeatureId,
            instance.CenterX,
            instance.CenterY,
            instance.CenterZ,
            members.ToArray(),
            feeders);
    }

    public EpithermalVeinSample Sample(int worldX, int worldY, int worldZ)
    {
        double x = worldX - originX;
        double y = worldY - originY;
        double z = worldZ - originZ;
        double bestScore = double.PositiveInfinity;
        double bestCenterRatio = double.PositiveInfinity;
        bool inside = false;

        foreach (EpithermalVeinMember vein in veins)
        {
            EpithermalFeeder feeder = feeders[vein.FeederIndex];
            double merge = GetMergeFactor(y);
            double cosStrike = vein.CosStrike * (1 - merge) + feeder.CosStrike * merge;
            double sinStrike = vein.SinStrike * (1 - merge) + feeder.SinStrike * merge;
            double strikeLength = Math.Sqrt(cosStrike * cosStrike + sinStrike * sinStrike);
            cosStrike /= strikeLength;
            sinStrike /= strikeLength;

            double cosDip = vein.CosDip * (1 - merge) + feeder.CosDip * merge;
            double sinDip = vein.SinDip * (1 - merge) + feeder.SinDip * merge;
            double dipLength = Math.Sqrt(cosDip * cosDip + sinDip * sinDip);
            cosDip /= dipLength;
            sinDip /= dipLength;

            double effectiveOffset = vein.Offset * (1 - merge) + feeder.Offset * merge;
            double centerAlong = vein.CenterAlong * (1 - merge) + feeder.CenterAlong * merge;
            double centerY = vein.CenterY * (1 - merge) + feeder.CenterY * merge;
            double lengthHalf = vein.LengthHalf * (1 - merge) + feeder.LengthHalf * merge;
            double pinchPhase = vein.PinchPhase * (1 - merge) + feeder.PinchPhase * merge;

            double along = x * cosStrike - z * sinStrike;
            double across = x * sinStrike + z * cosStrike - effectiveOffset;
            double vertical = y - centerY;
            double planeAcross = -vertical * cosDip / Math.Max(0.12, sinDip) * vein.DipDirection;
            double upperBend = Math.Sin(along * 0.11 + y * 0.06 + vein.Phase) * 2.4
                + Math.Cos(along * 0.31 + vein.Phase * 1.4) * 0.7;
            double feederBend = Math.Sin(along * 0.12 + feeder.Phase) * 0.8;
            double bend = upperBend * (1 - merge) + feederBend * merge;
            double relay = vein.RelayShift
                * Math.Tanh((along - vein.RelayCenter) / vein.RelayWidth)
                * (1 - merge);
            double signedDistance = (across - planeAcross) * sinDip - bend - relay;

            double u = (along - centerAlong) / lengthHalf;
            double v = vertical / vein.HeightHalf;
            double upperWarp = Math.Sin(u * 3.6 + vein.Phase) * Math.Cos(v * 3.0) * 0.15
                + Math.Sin(u * 7.1 - v * 3.2 + vein.Phase * 0.5) * 0.07;
            double feederWarp = Math.Sin(u * 3.2 + feeder.Phase) * Math.Cos(v * 2.7) * 0.05;
            double footprint = Math.Sqrt(u * u + v * v)
                + upperWarp * (1 - merge)
                + feederWarp * merge;
            if (footprint > 1.18) continue;

            double edgeTaper = Math.Clamp(1 - footprint * footprint, 0, 1);
            double boilingFlaring = Math.Max(0, (y + 7) / 21) * 1.5 * (1 - merge);
            double pinchSwell = 0.82 + 0.18 * Math.Sin(along * 0.18 + pinchPhase);
            double baseThickness = vein.MaxThickness * (1 - merge) + feeder.Thickness * merge;
            double halfThickness = Math.Max(
                0.10,
                (baseThickness * 0.5 + boilingFlaring) * pinchSwell * Math.Sqrt(edgeTaper));
            double distance = Math.Abs(signedDistance);
            double score = distance / halfThickness;
            if (score >= bestScore) continue;

            bestScore = score;
            bestCenterRatio = score;
            inside = footprint <= 1.04 && distance <= halfThickness;
        }

        return new EpithermalVeinSample(inside, bestScore, bestCenterRatio, y);
    }

    public int GetGeyseriteCapThickness(
        int worldX,
        int worldZ,
        EpithermalVeinDefinition definition)
    {
        double noise = ProceduralDepositMath.SmoothNoise2D(
            featureId,
            worldX / 6.0,
            worldZ / 6.0,
            0x4745595345524954UL);
        int range = definition.GeyseriteCapMax - definition.GeyseriteCapMin + 1;
        return definition.GeyseriteCapMin + Math.Min(range - 1, (int)(noise * range));
    }

    private static double GetMergeFactor(double localY)
    {
        double linear = Math.Clamp(
            (MergeStartY - localY) / (MergeStartY - MergeEndY),
            0,
            1);
        return linear * linear * (3 - 2 * linear);
    }

    private readonly record struct EpithermalFeeder(
        double CosStrike,
        double SinStrike,
        double CosDip,
        double SinDip,
        double Offset,
        double CenterAlong,
        double CenterY,
        double LengthHalf,
        double Thickness,
        double Phase,
        double PinchPhase)
    {
        public static EpithermalFeeder Create(
            double strikeDegrees,
            double dipDegrees,
            double offset,
            double centerAlong,
            double centerY,
            double lengthHalf,
            double thickness,
            double phase,
            double pinchPhase)
        {
            double strike = strikeDegrees * Math.PI / 180;
            double dip = dipDegrees * Math.PI / 180;
            return new EpithermalFeeder(
                Math.Cos(strike),
                Math.Sin(strike),
                Math.Cos(dip),
                Math.Sin(dip),
                offset,
                centerAlong,
                centerY,
                lengthHalf,
                thickness,
                phase,
                pinchPhase);
        }
    }

    private readonly record struct EpithermalVeinMember(
        double StrikeDegrees,
        double DipDegrees,
        double CosStrike,
        double SinStrike,
        double CosDip,
        double SinDip,
        int DipDirection,
        double Offset,
        double CenterAlong,
        double CenterY,
        double LengthHalf,
        double HeightHalf,
        double MaxThickness,
        double Phase,
        double RelayCenter,
        double RelayWidth,
        double RelayShift,
        double PinchPhase,
        int FeederIndex)
    {
        public static EpithermalVeinMember Create(
            double strikeDegrees,
            double dipDegrees,
            int dipDirection,
            double offset,
            double centerAlong,
            double centerY,
            double lengthHalf,
            double heightHalf,
            double maxThickness,
            double phase,
            double relayCenter,
            double relayWidth,
            double relayShift,
            double pinchPhase,
            int feederIndex)
        {
            double strike = strikeDegrees * Math.PI / 180;
            double dip = dipDegrees * Math.PI / 180;
            return new EpithermalVeinMember(
                strikeDegrees,
                dipDegrees,
                Math.Cos(strike),
                Math.Sin(strike),
                Math.Cos(dip),
                Math.Sin(dip),
                dipDirection,
                offset,
                centerAlong,
                centerY,
                lengthHalf,
                heightHalf,
                maxThickness,
                phase,
                relayCenter,
                relayWidth,
                relayShift,
                pinchPhase,
                feederIndex);
        }
    }

    private struct EpithermalRandom
    {
        private ulong state;

        public EpithermalRandom(ulong seed)
        {
            state = seed;
        }

        public double Range(double minimum, double maximum)
        {
            state = ProceduralDepositMath.Mix(state);
            return minimum + (maximum - minimum) * ProceduralDepositMath.UnitDouble(state);
        }

        public int NextInt(int minimum, int maximumInclusive)
        {
            if (maximumInclusive <= minimum) return minimum;
            return minimum + (int)(Range(0, 1) * (maximumInclusive - minimum + 1));
        }
    }
}
