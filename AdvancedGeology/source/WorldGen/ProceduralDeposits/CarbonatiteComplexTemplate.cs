using System;
using System.Collections.Generic;

namespace AdvancedGeology.WorldGen.ProceduralDeposits;

public enum CarbonatiteMaterialZone
{
    None,
    CarbonatiteMatrix,
    Pyrochlore,
    Monazite,
    Bastnasite,
    Fluorite,
    Apatite,
    Magnetite,
    Breccia
}

public readonly record struct CarbonatiteComplexSample(
    bool Inside,
    CarbonatiteMaterialZone Zone,
    bool IsCap,
    bool IsSill,
    bool IsDyke,
    bool IsRing,
    bool IsCapVein,
    double Radial,
    double VerticalT);

public sealed class CarbonatiteComplexPlan
{
    private const ulong PlanSalt = 0x434152424F4E4154UL; // "CARBONAT"
    private const ulong GrainSalt = 0x475241494E434152UL; // "GRAINCAR"
    private const ulong EdgeSalt = 0x454447454E4F4953UL; // "EDGENOIS"

    private readonly ulong featureId;
    private readonly int originX;
    private readonly int originY;
    private readonly int originZ;
    private readonly double systemX;
    private readonly double systemZ;
    private readonly double seed;
    private readonly double topTiltDeg;
    private readonly double topTiltDirectionDeg;
    private readonly SillData mainSill;
    private readonly DykeData[] dykes;
    private readonly CapVeinData[] capVeins;

    public ulong FeatureId => featureId;
    public int OriginX => originX;
    public int OriginY => originY;
    public int OriginZ => originZ;

    private CarbonatiteComplexPlan(
        ulong featureId,
        int originX,
        int originY,
        int originZ,
        double systemX,
        double systemZ,
        double seed,
        double topTiltDeg,
        double topTiltDirectionDeg,
        SillData mainSill,
        DykeData[] dykes,
        CapVeinData[] capVeins)
    {
        this.featureId = featureId;
        this.originX = originX;
        this.originY = originY;
        this.originZ = originZ;
        this.systemX = systemX;
        this.systemZ = systemZ;
        this.seed = seed;
        this.topTiltDeg = topTiltDeg;
        this.topTiltDirectionDeg = topTiltDirectionDeg;
        this.mainSill = mainSill;
        this.dykes = dykes;
        this.capVeins = capVeins;
    }

    public static CarbonatiteComplexPlan Create(
        in ProceduralDepositInstance instance,
        CarbonatiteComplexDefinition settings)
    {
        var random = new ProceduralDepositRandom(instance.FeatureId ^ PlanSalt);

        int dykeCount = random.NextInt(settings.DykeCountMin, settings.DykeCountMax);
        int feederCount = random.NextInt(settings.FeederCountMin, Math.Min(settings.FeederCountMax, dykeCount));
        int ringCount = random.NextInt(settings.RingCountMin, settings.RingCountMax);
        int capVeinCount = random.NextInt(settings.CapVeinCountMin, settings.CapVeinCountMax);

        double regionalStrikeDeg = random.Range(settings.RegionalStrikeMinDeg, settings.RegionalStrikeMaxDeg);
        double topTiltDeg = random.Range(settings.TopTiltMinDeg, settings.TopTiltMaxDeg);
        double topTiltDirectionDeg = random.Range(0.0, 360.0);
        double systemX = random.Range(-4.0, 4.0);
        double systemZ = random.Range(-4.0, 4.0);
        double seed = random.Range(0.0, 100.0);

        double strikeRad = regionalStrikeDeg * Math.PI / 180.0;
        var mainSill = new SillData(
            CenterX: systemX + random.Range(-2.0, 2.0),
            CenterZ: systemZ + random.Range(-2.0, 2.0),
            CenterY: random.Range(6.0, 8.0),
            RadiusAlong: random.Range(settings.SillRadiusAlongMin, settings.SillRadiusAlongMax),
            RadiusAcross: random.Range(settings.SillRadiusAcrossMin, settings.SillRadiusAcrossMax),
            RadiusVertical: random.Range(settings.SillRadiusVerticalMin, settings.SillRadiusVerticalMax),
            SinStrike: Math.Sin(strikeRad),
            CosStrike: Math.Cos(strikeRad),
            Phase: seed + 17.0
        );

        var feederRoots = new (double X, double Y, double Z)[feederCount];
        for (int i = 0; i < feederCount; i++)
        {
            double centeredIndex = i - (feederCount - 1) / 2.0;
            feederRoots[i] = (
                X: systemX + centeredIndex * random.Range(6.0, 10.0) + random.Range(-1.5, 1.5),
                Y: -34.0,
                Z: systemZ + random.Range(-2.0, 2.0)
            );
        }

        var dykeList = new List<DykeData>(dykeCount);
        for (int i = 0; i < dykeCount; i++)
        {
            int feederIndex = Math.Min(feederRoots.Length - 1, i * feederRoots.Length / dykeCount);
            var root = feederRoots[feederIndex];
            double position = dykeCount == 1 ? 0.0 : (double)i / (dykeCount - 1);
            double topAlong = (position - 0.5) * mainSill.RadiusAlong * 1.15 + random.Range(-3.0, 3.0);
            double topAcross = random.Range(-0.28, 0.28) * mainSill.RadiusAcross;

            double topX = mainSill.CenterX + topAlong * mainSill.CosStrike + topAcross * mainSill.SinStrike;
            double topZ = mainSill.CenterZ - topAlong * mainSill.SinStrike + topAcross * mainSill.CosStrike;

            double straightMidX = (root.X + topX) * 0.5;
            double straightMidZ = (root.Z + topZ) * 0.5;
            double pathX = topX - root.X;
            double pathZ = topZ - root.Z;
            double pathLength = Math.Max(0.1, Math.Sqrt(pathX * pathX + pathZ * pathZ));
            double perpendicularX = -pathZ / pathLength;
            double perpendicularZ = pathX / pathLength;
            double divergence = (i % 2 == 0 ? -1.0 : 1.0) * random.Range(8.0, 17.0);

            double directionRad = topTiltDirectionDeg * Math.PI / 180.0;
            double projection = (topX - systemX) * Math.Cos(directionRad) + (topZ - systemZ) * Math.Sin(directionRad);
            double topTiltOffset = Math.Tan(topTiltDeg * Math.PI / 180.0) * projection;

            dykeList.Add(new DykeData(
                rootX: root.X,
                rootZ: root.Z,
                midX: straightMidX + perpendicularX * divergence + random.Range(-3.0, 3.0),
                midZ: straightMidZ + perpendicularZ * divergence + random.Range(-3.0, 3.0),
                topX: topX,
                topZ: topZ,
                rootY: root.Y,
                topY: mainSill.CenterY + topTiltOffset + random.Range(-0.7, 0.7),
                rootRadius: random.Range(2.8, 4.4),
                topRadius: random.Range(5.0, 8.0),
                scaleX: random.Range(0.78, 1.16),
                scaleZ: random.Range(0.78, 1.16),
                topDomeHeight: random.Range(2.0, 4.2),
                phase: seed + i * 23.0,
                feederIndex: feederIndex,
                ringBands: new List<RingBandData>()
            ));
        }

        for (int i = 0; i < ringCount; i++)
        {
            var dyke = dykeList[i % dykeList.Count];
            int bandIndex = i / dykeList.Count;
            dyke.RingBands.Add(new RingBandData(
                Gap: 0.18 + bandIndex * 0.14 + random.Range(-0.025, 0.025),
                Phase: seed + i * 19.0
            ));
        }

        var capVeinList = new List<CapVeinData>(capVeinCount);
        for (int i = 0; i < capVeinCount; i++)
        {
            int sourceDykeIndex = i % dykeList.Count;
            var source = dykeList[sourceDykeIndex];
            double centerX = source.TopX + random.Range(-2.5, 2.5);
            double centerZ = source.TopZ + random.Range(-2.5, 2.5);

            double directionRad = topTiltDirectionDeg * Math.PI / 180.0;
            double projection = (centerX - systemX) * Math.Cos(directionRad) + (centerZ - systemZ) * Math.Sin(directionRad);
            double localTiltOffset = Math.Tan(topTiltDeg * Math.PI / 180.0) * projection;
            double localSillY = mainSill.CenterY + localTiltOffset;
            double capBaseY = localSillY + mainSill.RadiusVertical * 0.70;

            double veinStrikeRad = (regionalStrikeDeg + random.Range(-48.0, 48.0)) * Math.PI / 180.0;
            double veinDipRad = random.Range(58.0, 84.0) * Math.PI / 180.0;

            capVeinList.Add(new CapVeinData(
                CenterX: centerX,
                CenterZ: centerZ,
                CenterY: capBaseY + random.Range(5.0, 8.0),
                LengthHalf: random.Range(12.0, 23.0),
                HeightHalf: random.Range(7.0, 11.0),
                Thickness: random.Range(1.2, 2.3),
                SinStrike: Math.Sin(veinStrikeRad),
                CosStrike: Math.Cos(veinStrikeRad),
                SinDip: Math.Sin(veinDipRad),
                CosDip: Math.Cos(veinDipRad),
                Phase: seed + 101.0 + i * 13.0,
                SourceDykeIndex: sourceDykeIndex
            ));
        }

        return new CarbonatiteComplexPlan(
            instance.FeatureId,
            instance.CenterX,
            instance.CenterY,
            instance.CenterZ,
            systemX,
            systemZ,
            seed,
            topTiltDeg,
            topTiltDirectionDeg,
            mainSill,
            dykeList.ToArray(),
            capVeinList.ToArray());
    }

    public CarbonatiteComplexSample Sample(int worldX, int worldY, int worldZ)
    {
        double x = worldX - originX;
        double y = worldY - originY;
        double z = worldZ - originZ;

        if (y < -35.0)
        {
            return new CarbonatiteComplexSample(false, CarbonatiteMaterialZone.None, false, false, false, false, false, 0, 0);
        }

        var cap = SampleCap(x, y, z);
        var sill = SampleSill(x, y, z);

        double grain = Hash3D(x * 1.8 + seed, y * 1.8, z * 1.8);

        CapVeinSample bestCapVein = default;
        bool hasBestCapVein = false;
        for (int i = 0; i < capVeins.Length; i++)
        {
            var sample = SampleCapVein(x, y, z, capVeins[i]);
            if (sample.Score < double.PositiveInfinity && (!hasBestCapVein || sample.Score < bestCapVein.Score))
            {
                bestCapVein = sample;
                hasBestCapVein = true;
            }
        }

        if (cap.Inside)
        {
            if (hasBestCapVein && bestCapVein.Inside)
            {
                CarbonatiteMaterialZone veinZone = grain > 0.58
                    ? CarbonatiteMaterialZone.Bastnasite
                    : (grain > 0.28 ? CarbonatiteMaterialZone.Fluorite : CarbonatiteMaterialZone.Monazite);
                return new CarbonatiteComplexSample(true, veinZone, true, false, false, false, true, cap.Footprint, cap.HeightNorm);
            }

            double dykeTopProximity = double.PositiveInfinity;
            for (int i = 0; i < dykes.Length; i++)
            {
                double dx = x - dykes[i].TopX;
                double dz = z - dykes[i].TopZ;
                dykeTopProximity = Math.Min(dykeTopProximity, Math.Sqrt(dx * dx + dz * dz));
            }

            bool sillContactZone = cap.HeightNorm < 0.22;
            bool dykeFedZone = dykeTopProximity < 6.5;

            if (sillContactZone)
            {
                if (grain > 0.96)
                {
                    return new CarbonatiteComplexSample(true, CarbonatiteMaterialZone.Bastnasite, true, false, false, false, false, cap.Footprint, cap.HeightNorm);
                }
                return new CarbonatiteComplexSample(true, CarbonatiteMaterialZone.Breccia, true, false, false, false, false, cap.Footprint, cap.HeightNorm);
            }

            if (dykeFedZone)
            {
                if (grain > 0.94)
                {
                    return new CarbonatiteComplexSample(true, CarbonatiteMaterialZone.Monazite, true, false, false, false, false, cap.Footprint, cap.HeightNorm);
                }
                return new CarbonatiteComplexSample(true, CarbonatiteMaterialZone.Breccia, true, false, false, false, false, cap.Footprint, cap.HeightNorm);
            }

            return new CarbonatiteComplexSample(true, CarbonatiteMaterialZone.Breccia, true, false, false, false, false, cap.Footprint, cap.HeightNorm);
        }

        DykeSample bestDyke = default;
        bool hasBestDyke = false;
        DykeSample bestRing = default;
        bool hasBestRing = false;

        for (int i = 0; i < dykes.Length; i++)
        {
            var sample = SampleDyke(x, y, z, dykes[i]);
            if (sample.Inside && (!hasBestDyke || sample.Radial < bestDyke.Radial))
            {
                bestDyke = sample;
                hasBestDyke = true;
            }
            if (sample.Ring && (!hasBestRing || sample.RingDistance < bestRing.RingDistance))
            {
                bestRing = sample;
                hasBestRing = true;
            }
        }

        if (hasBestRing)
        {
            return new CarbonatiteComplexSample(true, CarbonatiteMaterialZone.CarbonatiteMatrix, false, false, false, true, false, bestRing.Radial, bestRing.VerticalT);
        }

        if (cap.SubCap && (sill.Inside || hasBestDyke))
        {
            double reePotential = Math.Sin(x * 0.23 + seed) * Math.Cos(z * 0.25 - seed * 0.4) + (y / 12.0) * 0.45;
            if (Math.Abs(Math.Sin(x * 0.34 + z * 0.28 + y * 0.25)) < 0.12 && grain > 0.30)
            {
                return new CarbonatiteComplexSample(true, CarbonatiteMaterialZone.Fluorite, false, sill.Inside, hasBestDyke, false, false, sill.Radial, 0);
            }
            if (reePotential > 0.34 && sill.Lateral > 0.18)
            {
                return new CarbonatiteComplexSample(true, CarbonatiteMaterialZone.Bastnasite, false, sill.Inside, hasBestDyke, false, false, sill.Radial, 0);
            }
            if (reePotential > 0.16 && grain > 0.52)
            {
                return new CarbonatiteComplexSample(true, CarbonatiteMaterialZone.Monazite, false, sill.Inside, hasBestDyke, false, false, sill.Radial, 0);
            }
        }

        if (sill.Inside)
        {
            double sillBand = Math.Sin(sill.Lateral * Math.PI * 7.0 + sill.Along * 0.11 + seed);
            if (sillBand > 0.74)
            {
                return new CarbonatiteComplexSample(true, CarbonatiteMaterialZone.Apatite, false, true, false, false, false, sill.Radial, 0);
            }
            if (Math.Abs(Math.Sin(sill.Along * 0.31 + sill.Across * 0.22)) < 0.10 && grain > 0.46)
            {
                return new CarbonatiteComplexSample(true, CarbonatiteMaterialZone.Fluorite, false, true, false, false, false, sill.Radial, 0);
            }
            if (grain > 0.88 && sill.Lateral < 0.72)
            {
                return new CarbonatiteComplexSample(true, CarbonatiteMaterialZone.Bastnasite, false, true, false, false, false, sill.Radial, 0);
            }
            if (grain > 0.82 && sill.Lateral < 0.55)
            {
                return new CarbonatiteComplexSample(true, CarbonatiteMaterialZone.Monazite, false, true, false, false, false, sill.Radial, 0);
            }
            return new CarbonatiteComplexSample(true, CarbonatiteMaterialZone.CarbonatiteMatrix, false, true, false, false, false, sill.Radial, 0);
        }

        if (hasBestDyke)
        {
            double radial = bestDyke.Radial;
            double verticalT = bestDyke.VerticalT;

            if (radial < 0.38)
            {
                double pyrochloreField = Math.Sin(x * 0.42 + y * 0.21 + seed) * Math.Cos(z * 0.45 - y * 0.16);
                if (pyrochloreField > 0.18)
                {
                    return new CarbonatiteComplexSample(true, CarbonatiteMaterialZone.Pyrochlore, false, false, true, false, false, radial, verticalT);
                }
                return new CarbonatiteComplexSample(true, CarbonatiteMaterialZone.CarbonatiteMatrix, false, false, true, false, false, radial, verticalT);
            }

            double apatiteBand = Math.Sin(radial * Math.PI * 7.0 + y * 0.18 + seed);
            if (radial > 0.38 && radial < 0.82 && apatiteBand > 0.66)
            {
                return new CarbonatiteComplexSample(true, CarbonatiteMaterialZone.Apatite, false, false, true, false, false, radial, verticalT);
            }

            if (radial > 0.30 && radial < 0.78 && y < 2.0)
            {
                double magnetiteLayer = Math.Cos(radial * Math.PI * 9.0 - y * 0.26 + seed);
                if (magnetiteLayer > 0.72 && grain > 0.22)
                {
                    return new CarbonatiteComplexSample(true, CarbonatiteMaterialZone.Magnetite, false, false, true, false, false, radial, verticalT);
                }
            }

            if (verticalT > 0.68 && radial < 0.72 && grain > 0.82)
            {
                return new CarbonatiteComplexSample(true, CarbonatiteMaterialZone.Fluorite, false, false, true, false, false, radial, verticalT);
            }

            return new CarbonatiteComplexSample(true, CarbonatiteMaterialZone.CarbonatiteMatrix, false, false, true, false, false, radial, verticalT);
        }

        return new CarbonatiteComplexSample(false, CarbonatiteMaterialZone.None, false, false, false, false, false, 0, 0);
    }

    public CarbonatiteMaterialZone SelectZone(int worldX, int worldY, int worldZ)
    {
        return Sample(worldX, worldY, worldZ).Zone;
    }

    public static CarbonatiteMaterialZone SelectZone(in CarbonatiteComplexSample sample)
    {
        return sample.Zone;
    }

    private double GetTopTiltOffset(double x, double z)
    {
        double directionRad = topTiltDirectionDeg * Math.PI / 180.0;
        double projection = (x - systemX) * Math.Cos(directionRad) + (z - systemZ) * Math.Sin(directionRad);
        return Math.Tan(topTiltDeg * Math.PI / 180.0) * projection;
    }

    private CapSample SampleCap(double x, double y, double z)
    {
        double dx = x - mainSill.CenterX;
        double dz = z - mainSill.CenterZ;
        double along = dx * mainSill.CosStrike - dz * mainSill.SinStrike;
        double across = dx * mainSill.SinStrike + dz * mainSill.CosStrike;
        double u = along / (mainSill.RadiusAlong + 3.0);
        double v = across / (mainSill.RadiusAcross + 4.0);
        double azimuth = Math.Atan2(v, u);
        double edgeWarp = Math.Sin(u * 3.5 + seed) * Math.Cos(v * 3.0) * 0.14
            + Math.Sin(u * 7.0 - v * 4.0 + seed * 0.5) * 0.07
            + Math.Sin(azimuth * 5.0 + seed) * 0.05;
        double footprint = Math.Sqrt(u * u + v * v) + edgeWarp;

        double edgeNoise = Hash3D(x * 0.72 + seed, 0.0, z * 0.72);
        double featherThreshold = Math.Clamp((footprint - 0.88) / 0.22, 0.0, 1.0);
        bool featheredEdge = footprint <= 0.88 || (footprint <= 1.10 && edgeNoise > featherThreshold);

        double tiltedSillY = mainSill.CenterY + GetTopTiltOffset(x, z);
        double capBaseY = tiltedSillY + mainSill.RadiusVertical * 0.70;
        double capTopY = capBaseY + 17.0;
        double roof = capTopY - (u * u + v * v) * 7.0;
        double heightNorm = Math.Clamp((y - capBaseY) / Math.Max(0.1, roof - capBaseY), 0.0, 1.0);

        bool inside = featheredEdge && y >= capBaseY && y <= roof;
        bool subCap = footprint <= 1.10 && y >= tiltedSillY - 1.0 && y < capBaseY;

        return new CapSample(footprint, capBaseY, roof, heightNorm, inside, subCap);
    }

    private SillSample SampleSill(double x, double y, double z)
    {
        double dx = x - mainSill.CenterX;
        double dz = z - mainSill.CenterZ;
        double along = dx * mainSill.CosStrike - dz * mainSill.SinStrike;
        double across = dx * mainSill.SinStrike + dz * mainSill.CosStrike;
        double u = along / mainSill.RadiusAlong;
        double v = across / mainSill.RadiusAcross;
        double tiltedCenterY = mainSill.CenterY + GetTopTiltOffset(x, z);
        double w = (y - tiltedCenterY) / mainSill.RadiusVertical;
        double edgeWarp = Math.Sin(u * 3.4 + mainSill.Phase) * Math.Cos(v * 3.1) * 0.12
            + Math.Sin(u * 7.0 - v * 4.2 + mainSill.Phase * 0.5) * 0.06;
        double radial = Math.Sqrt(u * u + v * v + w * w) + edgeWarp;
        double lateral = Math.Sqrt(u * u + v * v);

        return new SillSample(radial, lateral, w, radial <= 1.0, radial <= 1.18, along, across);
    }

    private DykeSample SampleDyke(double x, double y, double z, in DykeData dyke)
    {
        double verticalT = Math.Clamp((y - dyke.RootY) / (dyke.TopY - dyke.RootY), 0.0, 1.0);
        double inverseT = 1.0 - verticalT;
        double pathEnvelope = Math.Sin(Math.PI * verticalT);

        double axisX = inverseT * inverseT * dyke.RootX
            + 2.0 * inverseT * verticalT * dyke.MidX
            + verticalT * verticalT * dyke.TopX
            + pathEnvelope * (
                Math.Sin(y * 0.19 + dyke.Phase) * 2.6
                + Math.Sin(y * 0.47 + dyke.Phase * 0.6) * 0.8
            );

        double axisZ = inverseT * inverseT * dyke.RootZ
            + 2.0 * inverseT * verticalT * dyke.MidZ
            + verticalT * verticalT * dyke.TopZ
            + pathEnvelope * (
                Math.Cos(y * 0.17 + dyke.Phase * 0.8) * 2.4
                + Math.Cos(y * 0.41 + dyke.Phase * 1.2) * 0.7
            );

        double radius = dyke.RootRadius + verticalT * (dyke.TopRadius - dyke.RootRadius);
        double localRadius = radius * (
            0.92
            + Math.Sin(y * 0.21 + dyke.Phase) * 0.08
            + Math.Cos(y * 0.43 - dyke.Phase) * 0.04
        );

        double dx = x - axisX;
        double dz = z - axisZ;
        double scaledDx = dx / (localRadius * dyke.ScaleX);
        double scaledDz = dz / (localRadius * dyke.ScaleZ);
        double radial = Math.Sqrt(scaledDx * scaledDx + scaledDz * scaledDz);

        double clampedRadial = Math.Clamp(radial, 0.0, 1.0);
        double roofRelief = dyke.TopDomeHeight * (1.0 - clampedRadial * clampedRadial)
            + Math.Sin(dx * 0.38 + dyke.Phase) * 0.9
            + Math.Cos(dz * 0.33 - dyke.Phase * 0.7) * 0.7;
        double topBoundary = dyke.TopY + roofRelief;

        double ringDistance = double.PositiveInfinity;
        for (int i = 0; i < dyke.RingBands.Count; i++)
        {
            var band = dyke.RingBands[i];
            double bandRadius = 1.0 + band.Gap + Math.Sin(y * 0.16 + band.Phase) * 0.035;
            ringDistance = Math.Min(ringDistance, Math.Abs(radial - bandRadius));
        }

        bool ring = ringDistance < 0.10 && y > -18.0 && y < mainSill.CenterY + 3.0;
        bool inside = radial <= 1.0 && y >= dyke.RootY - 1.0 && y <= topBoundary;
        bool halo = radial <= 1.34 && y >= dyke.RootY && y <= topBoundary + 2.0;

        return new DykeSample(radial, ringDistance, ring, inside, halo, verticalT, axisX, axisZ, topBoundary);
    }

    private CapVeinSample SampleCapVein(double x, double y, double z, in CapVeinData vein)
    {
        double dx = x - vein.CenterX;
        double dz = z - vein.CenterZ;
        double along = dx * vein.CosStrike - dz * vein.SinStrike;
        double across = dx * vein.SinStrike + dz * vein.CosStrike;
        double bend = Math.Sin(along * 0.18 + y * 0.10 + vein.Phase) * 0.9;
        double signedDistance = across * vein.SinDip + (y - vein.CenterY) * vein.CosDip - bend;

        double u = along / vein.LengthHalf;
        double v = (y - vein.CenterY) / vein.HeightHalf;
        double edgeWarp = Math.Sin(u * 4.0 + vein.Phase) * Math.Cos(v * 3.2) * 0.12;
        double footprint = Math.Sqrt(u * u + v * v) + edgeWarp;
        double taper = Math.Clamp(1.0 - footprint * footprint, 0.0, 1.0);
        double halfThickness = Math.Max(0.10, vein.Thickness * 0.5 * Math.Sqrt(taper));
        double distance = Math.Abs(signedDistance);

        double score = footprint <= 1.12 ? distance / halfThickness : double.PositiveInfinity;
        bool inside = footprint <= 1.03 && distance <= halfThickness;

        return new CapVeinSample(score, inside);
    }

    private static double Hash3D(double x, double y, double z)
    {
        double n = Math.Sin(x * 12.9898 + y * 78.233 + z * 37.719) * 43758.5453;
        return n - Math.Floor(n);
    }

    private readonly record struct SillData(
        double CenterX,
        double CenterZ,
        double CenterY,
        double RadiusAlong,
        double RadiusAcross,
        double RadiusVertical,
        double SinStrike,
        double CosStrike,
        double Phase);

    private sealed class DykeData
    {
        public double RootX { get; }
        public double RootZ { get; }
        public double MidX { get; }
        public double MidZ { get; }
        public double TopX { get; }
        public double TopZ { get; }
        public double RootY { get; }
        public double TopY { get; }
        public double RootRadius { get; }
        public double TopRadius { get; }
        public double ScaleX { get; }
        public double ScaleZ { get; }
        public double TopDomeHeight { get; }
        public double Phase { get; }
        public int FeederIndex { get; }
        public List<RingBandData> RingBands { get; }

        public DykeData(
            double rootX, double rootZ, double midX, double midZ,
            double topX, double topZ, double rootY, double topY,
            double rootRadius, double topRadius, double scaleX, double scaleZ,
            double topDomeHeight, double phase, int feederIndex, List<RingBandData> ringBands)
        {
            RootX = rootX;
            RootZ = rootZ;
            MidX = midX;
            MidZ = midZ;
            TopX = topX;
            TopZ = topZ;
            RootY = rootY;
            TopY = topY;
            RootRadius = rootRadius;
            TopRadius = topRadius;
            ScaleX = scaleX;
            ScaleZ = scaleZ;
            TopDomeHeight = topDomeHeight;
            Phase = phase;
            FeederIndex = feederIndex;
            RingBands = ringBands;
        }
    }

    private readonly record struct RingBandData(double Gap, double Phase);

    private readonly record struct CapVeinData(
        double CenterX,
        double CenterZ,
        double CenterY,
        double LengthHalf,
        double HeightHalf,
        double Thickness,
        double SinStrike,
        double CosStrike,
        double SinDip,
        double CosDip,
        double Phase,
        int SourceDykeIndex);

    private readonly record struct CapSample(
        double Footprint,
        double CapBaseY,
        double Roof,
        double HeightNorm,
        bool Inside,
        bool SubCap);

    private readonly record struct SillSample(
        double Radial,
        double Lateral,
        double VerticalNorm,
        bool Inside,
        bool Halo,
        double Along,
        double Across);

    private readonly record struct DykeSample(
        double Radial,
        double RingDistance,
        bool Ring,
        bool Inside,
        bool Halo,
        double VerticalT,
        double AxisX,
        double AxisZ,
        double TopBoundary);

    private readonly record struct CapVeinSample(double Score, bool Inside);
}
