using System;
using System.Collections.Generic;

namespace AdvancedGeology.WorldGen.ProceduralDeposits;

internal enum SheetedPlateZone
{
    Primary,
    Secondary,
    Gangue,
    RareGangue,
    Albite,
    Quartz,
    Breccia
}

internal enum SheetedPlateMemberKind
{
    Main,
    Subsheet,
    Satellite
}

internal readonly record struct SheetedPlateSample(
    bool Inside,
    bool InHalo,
    double CenterRatio,
    double LocalY,
    SheetedPlateMemberKind MemberKind);

/// <summary>
/// Primary and secondary ores, quartz, albite, gangue, and breccia form a dominant dipping plate with parallel subsheets and independent steep satellite veins.
/// </summary>
internal sealed class SheetedPlatePlan
{
    private const ulong PlanSalt = 0x5348454554504C54UL;

    private readonly int originX;
    private readonly int originY;
    private readonly int originZ;
    private readonly ProceduralTerrainFrame terrainFrame;
    private readonly PlateMember[] members;
    private readonly double haloThickness;

    public int MemberCount => members.Length;
    public ProceduralTerrainFrame TerrainFrame => terrainFrame;
    public double MainDipDegrees { get; }

    private SheetedPlatePlan(
        in ProceduralDepositInstance instance,
        ProceduralTerrainFrame terrainFrame,
        PlateMember[] members,
        double haloThickness,
        double mainDipDegrees)
    {
        originX = instance.CenterX;
        originY = instance.CenterY;
        originZ = instance.CenterZ;
        this.terrainFrame = terrainFrame;
        this.members = members;
        this.haloThickness = haloThickness;
        MainDipDegrees = mainDipDegrees;
    }

    public static SheetedPlatePlan Create(
        in ProceduralDepositInstance instance,
        SheetedPlateDefinition settings)
    {
        var random = new ProceduralDepositRandom(instance.FeatureId ^ PlanSalt);
        var frame = ProceduralTerrainFrame.FromGradients(instance.GradientX, instance.GradientZ);
        double strike = random.Range(-8, 8);
        double dip = random.Range(settings.MainDipMinDeg, settings.MainDipMaxDeg);
        var members = new List<PlateMember>(1 + settings.SubsheetMax + settings.SatelliteMax)
        {
            PlateMember.Create(
                strike,
                dip,
                1,
                random.Range(-3, 3),
                random.Range(-3, 3),
                random.Range(1, 5),
                settings.MainLengthHalf,
                settings.MainHeightHalf,
                settings.MainThickness,
                random.Range(0, Math.PI * 2),
                random.Range(-12, 12),
                random.Range(3, 6),
                random.Range(-1.8, 1.8),
                random.Range(0, Math.PI * 2),
                SheetedPlateMemberKind.Main)
        };

        int subsheetCount = random.NextInt(settings.SubsheetMin, settings.SubsheetMax);
        double subsheetOffset = random.Range(
            -settings.HorizontalRadius * 0.45,
            -settings.HorizontalRadius * 0.25);
        for (int index = 0; index < subsheetCount; index++)
        {
            if (index > 0) subsheetOffset += random.Range(
                settings.HorizontalRadius * 0.22,
                settings.HorizontalRadius * 0.36);
            members.Add(PlateMember.Create(
                strike + random.Range(-7, 7),
                dip + random.Range(-5, 6),
                1,
                subsheetOffset,
                random.Range(-settings.MainLengthHalf * 0.25, settings.MainLengthHalf * 0.25),
                random.Range(-2.5, 2.5),
                random.Range(settings.MainLengthHalf * 0.48, settings.MainLengthHalf * 0.75),
                random.Range(settings.MainHeightHalf * 0.50, settings.MainHeightHalf * 0.78),
                settings.MainThickness * random.Range(0.55, 0.8),
                random.Range(0, Math.PI * 2),
                random.Range(-8, 8),
                random.Range(2.5, 5),
                random.Range(-1.2, 1.2),
                random.Range(0, Math.PI * 2),
                SheetedPlateMemberKind.Subsheet));
        }

        int satelliteCount = random.NextInt(settings.SatelliteMin, settings.SatelliteMax);
        for (int index = 0; index < satelliteCount; index++)
        {
            members.Add(PlateMember.Create(
                strike + random.Range(34, 78) * (index % 2 == 0 ? 1 : -1),
                random.Range(70, 88),
                random.NextInt(0, 1) == 0 ? -1 : 1,
                random.Range(-settings.HorizontalRadius * 0.40, settings.HorizontalRadius * 0.40),
                random.Range(-settings.MainLengthHalf * 0.20, settings.MainLengthHalf * 0.20),
                random.Range(-4, 3),
                random.Range(settings.MainLengthHalf * 0.35, settings.MainLengthHalf * 0.55),
                random.Range(settings.MainHeightHalf * 0.40, settings.MainHeightHalf * 0.68),
                settings.MainThickness * random.Range(0.45, 0.7),
                random.Range(0, Math.PI * 2),
                random.Range(-5, 5),
                random.Range(2, 4),
                random.Range(-0.8, 0.8),
                random.Range(0, Math.PI * 2),
                SheetedPlateMemberKind.Satellite));
        }

        return new SheetedPlatePlan(instance, frame, members.ToArray(), settings.HaloThickness, dip);
    }

    public SheetedPlateSample Sample(int worldX, int worldY, int worldZ)
    {
        return Sample((double)worldX, worldY, worldZ);
    }

    internal SheetedPlateSample Sample(double worldX, double worldY, double worldZ)
    {
        (double x, double y, double z) = terrainFrame.ToLocal(
            worldX - originX,
            worldY - originY,
            worldZ - originZ);
        double bestScore = double.PositiveInfinity;
        var best = default(SheetedPlateSample);

        foreach (PlateMember member in members)
        {
            double along = x * member.CosStrike - z * member.SinStrike;
            double across = x * member.SinStrike + z * member.CosStrike - member.Offset;
            double bend = Math.Sin(along * 0.11 + y * 0.06 + member.Phase) * member.BendAmplitude
                + Math.Cos(along * 0.31 + member.Phase * 1.3) * member.BendAmplitude * 0.28;
            double relay = member.RelayShift
                * Math.Tanh((along - member.RelayCenter) / member.RelayWidth);
            double signedDistance = member.DipDirection * across * member.SinDip
                + (y - member.CenterY) * member.CosDip
                - bend
                - relay;
            double u = (along - member.CenterAlong) / member.LengthHalf;
            double v = (y - member.CenterY) / member.HeightHalf;
            double edgeWarp = Math.Sin(u * 3.6 + member.Phase) * Math.Cos(v * 3.0) * 0.14
                + Math.Sin(u * 7.0 - v * 3.2 + member.Phase * 0.5) * 0.06;
            double footprint = Math.Sqrt(u * u + v * v) + edgeWarp;
            if (footprint > 1.18) continue;

            double taper = Math.Clamp(1 - footprint * footprint, 0, 1);
            double pinch = 0.80 + 0.20 * Math.Sin(along * 0.18 + member.PinchPhase);
            double halfThickness = Math.Max(0.10, member.Thickness * 0.5 * pinch * Math.Sqrt(taper));
            double distance = Math.Abs(signedDistance);
            double score = distance / halfThickness;
            if (score >= bestScore) continue;

            bestScore = score;
            best = new SheetedPlateSample(
                footprint <= 1.04 && distance <= halfThickness,
                footprint <= 1.14 && distance <= halfThickness + haloThickness * taper,
                score,
                y,
                member.Kind);
        }
        return best;
    }

    private readonly record struct PlateMember(
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
        double Thickness,
        double BendAmplitude,
        double Phase,
        double RelayCenter,
        double RelayWidth,
        double RelayShift,
        double PinchPhase,
        SheetedPlateMemberKind Kind)
    {
        public static PlateMember Create(
            double strikeDeg,
            double dipDeg,
            int dipDirection,
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
            SheetedPlateMemberKind kind)
        {
            double strike = strikeDeg * Math.PI / 180;
            double dip = dipDeg * Math.PI / 180;
            return new PlateMember(
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
                thickness,
                thickness * 0.28,
                phase,
                relayCenter,
                relayWidth,
                relayShift,
                pinchPhase,
                kind);
        }
    }
}
