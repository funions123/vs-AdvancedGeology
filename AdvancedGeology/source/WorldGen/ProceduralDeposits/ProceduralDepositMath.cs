using System;

namespace AdvancedGeology.WorldGen.ProceduralDeposits;

public readonly record struct ProceduralTerrainPlane(double GradientX, double GradientZ)
{
    public double SlopeRadians => Math.Atan(Math.Sqrt(GradientX * GradientX + GradientZ * GradientZ));
}

public readonly record struct ProceduralTerrainFrame(
    double XX,
    double XY,
    double XZ,
    double YX,
    double YY,
    double YZ,
    double ZX,
    double ZY,
    double ZZ)
{
    public static ProceduralTerrainFrame FromGradients(double gradientX, double gradientZ)
    {
        double xScale = Math.Sqrt(1 + gradientX * gradientX);
        double xx = 1 / xScale;
        double xy = gradientX / xScale;
        double xz = 0;

        double yScale = Math.Sqrt(1 + gradientX * gradientX + gradientZ * gradientZ);
        double yx = -gradientX / yScale;
        double yy = 1 / yScale;
        double yz = -gradientZ / yScale;

        double zx = yy * xz - yz * xy;
        double zy = yz * xx - yx * xz;
        double zz = yx * xy - yy * xx;
        double zScale = Math.Sqrt(zx * zx + zy * zy + zz * zz);
        zx /= zScale;
        zy /= zScale;
        zz /= zScale;

        return new ProceduralTerrainFrame(xx, xy, xz, yx, yy, yz, zx, zy, zz);
    }

    public (double X, double Y, double Z) ToWorld(double x, double y, double z)
    {
        return (
            x * XX + y * YX + z * ZX,
            x * XY + y * YY + z * ZY,
            x * XZ + y * YZ + z * ZZ);
    }

    public (double X, double Y, double Z) ToLocal(double x, double y, double z)
    {
        return (
            x * XX + y * XY + z * XZ,
            x * YX + y * YY + z * YZ,
            x * ZX + y * ZY + z * ZZ);
    }
}

public readonly record struct ProceduralDepositInstance(
    ulong FeatureId,
    int CenterX,
    int CenterY,
    int CenterZ,
    double CosYaw,
    double SinYaw,
    double GradientX,
    double GradientZ);

public struct ProceduralTerrainPlaneAccumulator
{
    private int count;
    private double sumX;
    private double sumZ;
    private double sumY;
    private double sumXX;
    private double sumZZ;
    private double sumXZ;
    private double sumXY;
    private double sumZY;

    public void Add(double x, double z, double y)
    {
        count++;
        sumX += x;
        sumZ += z;
        sumY += y;
        sumXX += x * x;
        sumZZ += z * z;
        sumXZ += x * z;
        sumXY += x * y;
        sumZY += z * y;
    }

    public bool TrySolve(out ProceduralTerrainPlane plane)
    {
        if (count < 3)
        {
            plane = default;
            return false;
        }

        double covarianceXX = sumXX - sumX * sumX / count;
        double covarianceZZ = sumZZ - sumZ * sumZ / count;
        double covarianceXZ = sumXZ - sumX * sumZ / count;
        double covarianceXY = sumXY - sumX * sumY / count;
        double covarianceZY = sumZY - sumZ * sumY / count;
        double determinant = covarianceXX * covarianceZZ - covarianceXZ * covarianceXZ;
        if (Math.Abs(determinant) < 1e-9)
        {
            plane = default;
            return false;
        }

        plane = new ProceduralTerrainPlane(
            (covarianceXY * covarianceZZ - covarianceZY * covarianceXZ) / determinant,
            (covarianceZY * covarianceXX - covarianceXY * covarianceXZ) / determinant);
        return true;
    }
}

public readonly record struct ProceduralColumnSample(
    bool Intersects,
    double RoofY,
    double BaseY,
    double HorizontalMetric);

public enum EllipsoidMaterialZone
{
    Primary,
    Oxide,
    Enriched
}

public static class ProceduralDepositMath
{
    private const ulong GoldenGamma = 0x9E3779B97F4A7C15UL;
    private const ulong SmearSeedSalt = 0x534D454152534544UL;
    private const ulong SmearHeightSalt = 0x534D45415248474DUL;


    public static ulong Mix(ulong value)
    {
        value += GoldenGamma;
        value = (value ^ (value >> 30)) * 0xBF58476D1CE4E5B9UL;
        value = (value ^ (value >> 27)) * 0x94D049BB133111EBUL;
        return value ^ (value >> 31);
    }

    public static ulong Combine(ulong seed, long value)
    {
        return Mix(seed ^ Mix(unchecked((ulong)value)));
    }

    public static ulong FeatureId(long worldSeed, ulong codeHash, int cellX, int cellZ)
    {
        ulong seed = Combine(unchecked((ulong)worldSeed), unchecked((long)codeHash));
        seed = Combine(seed, cellX);
        return Combine(seed, cellZ);
    }

    public static ulong HashString(string value)
    {
        const ulong offset = 14695981039346656037UL;
        const ulong prime = 1099511628211UL;
        ulong hash = offset;
        foreach (char character in value)
        {
            hash ^= character;
            hash *= prime;
        }
        return hash;
    }

    public static double UnitDouble(ulong value)
    {
        return (Mix(value) >> 11) * (1.0 / (1UL << 53));
    }

    public static double Range(ulong seed, double minimum, double maximum)
    {
        return minimum + (maximum - minimum) * UnitDouble(seed);
    }

    public static int FloorDiv(int value, int divisor)
    {
        int quotient = value / divisor;
        int remainder = value % divisor;
        return remainder < 0 ? quotient - 1 : quotient;
    }

    public static int FloorMod(int value, int divisor)
    {
        int remainder = value % divisor;
        return remainder < 0 ? remainder + divisor : remainder;
    }

    public static ProceduralTerrainPlane LimitTerrainPlane(
        ProceduralTerrainPlane plane,
        TerrainOrientationDefinition settings)
    {
        double slope = plane.SlopeRadians;
        double slopeDeg = slope * 180.0 / Math.PI;
        if (slopeDeg < settings.IgnoreBelowTerrainSlopeDeg)
        {
            return default;
        }

        double terrainSlopeDeg = Math.Min(slopeDeg, settings.MaxTerrainSlopeDeg);
        double appliedDeg = Math.Clamp(
            terrainSlopeDeg * settings.DipFactor,
            settings.MinAppliedDipDeg,
            settings.MaxAppliedDipDeg);
        double magnitude = Math.Tan(appliedDeg * Math.PI / 180.0);
        double originalMagnitude = Math.Sqrt(plane.GradientX * plane.GradientX + plane.GradientZ * plane.GradientZ);
        if (originalMagnitude <= 1e-9) return default;

        double scale = magnitude / originalMagnitude;
        return new ProceduralTerrainPlane(plane.GradientX * scale, plane.GradientZ * scale);
    }

    public static double EllipsoidMetric(
        in ProceduralDepositInstance instance,
        EllipsoidGeometryDefinition geometry,
        double x,
        double y,
        double z)
    {
        double dx = x - instance.CenterX;
        double dz = z - instance.CenterZ;
        double along = dx * instance.CosYaw + dz * instance.SinYaw;
        double across = -dx * instance.SinYaw + dz * instance.CosYaw;
        double planeY = instance.CenterY + instance.GradientX * dx + instance.GradientZ * dz;
        double normalScale = Math.Sqrt(1.0 + instance.GradientX * instance.GradientX + instance.GradientZ * instance.GradientZ);
        double through = (y - planeY) / normalScale;

        return along * along / (geometry.RadiusX * geometry.RadiusX)
            + through * through / (geometry.RadiusY * geometry.RadiusY)
            + across * across / (geometry.RadiusZ * geometry.RadiusZ);
    }

    public static ProceduralColumnSample SampleColumn(
        in ProceduralDepositInstance instance,
        EllipsoidGeometryDefinition geometry,
        double x,
        double z)
    {
        double dx = x - instance.CenterX;
        double dz = z - instance.CenterZ;
        double along = dx * instance.CosYaw + dz * instance.SinYaw;
        double across = -dx * instance.SinYaw + dz * instance.CosYaw;
        double horizontal = along * along / (geometry.RadiusX * geometry.RadiusX)
            + across * across / (geometry.RadiusZ * geometry.RadiusZ);
        if (horizontal > 1.0)
        {
            return new ProceduralColumnSample(false, 0, 0, horizontal);
        }

        double planeY = instance.CenterY + instance.GradientX * dx + instance.GradientZ * dz;
        double normalScale = Math.Sqrt(1.0 + instance.GradientX * instance.GradientX + instance.GradientZ * instance.GradientZ);
        double halfHeight = geometry.RadiusY * normalScale * Math.Sqrt(Math.Max(0, 1.0 - horizontal));
        return new ProceduralColumnSample(true, planeY + halfHeight, planeY - halfHeight, horizontal);
    }

    public static EllipsoidMaterialZone SelectMaterialZone(
        int surfaceY,
        int y,
        int oxidationDepth,
        int enrichmentThickness)
    {
        int depth = surfaceY - y;
        if (depth <= oxidationDepth) return EllipsoidMaterialZone.Oxide;
        if (depth <= oxidationDepth + enrichmentThickness) return EllipsoidMaterialZone.Enriched;
        return EllipsoidMaterialZone.Primary;
    }

    public static double CoordinateNoise(ulong featureId, int x, int y, int z, ulong salt = 0)
    {
        ulong hash = Combine(featureId ^ salt, x);
        hash = Combine(hash, y);
        hash = Combine(hash, z);
        return UnitDouble(hash);
    }

    public static double SmoothNoise2D(ulong featureId, double x, double z, ulong salt = 0)
    {
        int x0 = (int)Math.Floor(x);
        int z0 = (int)Math.Floor(z);
        double tx = x - x0;
        double tz = z - z0;
        tx = tx * tx * (3.0 - 2.0 * tx);
        tz = tz * tz * (3.0 - 2.0 * tz);

        double n00 = CoordinateNoise(featureId, x0, 0, z0, salt);
        double n10 = CoordinateNoise(featureId, x0 + 1, 0, z0, salt);
        double n01 = CoordinateNoise(featureId, x0, 0, z0 + 1, salt);
        double n11 = CoordinateNoise(featureId, x0 + 1, 0, z0 + 1, salt);
        double nx0 = n00 + (n10 - n00) * tx;
        double nx1 = n01 + (n11 - n01) * tx;
        return nx0 + (nx1 - nx0) * tz;
    }

    public static bool IsSmearSeed(ulong featureId, int worldX, int worldZ, double seedFraction)
    {
        return CoordinateNoise(featureId, worldX, 0, worldZ, SmearSeedSalt) < seedFraction;
    }

    public static int SmearHeight(
        ulong featureId,
        int worldX,
        int worldZ,
        int minimumHeight,
        int maximumHeight)
    {
        if (maximumHeight <= minimumHeight) return minimumHeight;
        double noise = CoordinateNoise(featureId, worldX, 1, worldZ, SmearHeightSalt);
        int height = minimumHeight + (int)(noise * (maximumHeight - minimumHeight + 1));
        return Math.Clamp(height, minimumHeight, maximumHeight);
    }

    public static bool SmearCovers(int apexY, int height, int radius, int targetSurfaceY)
    {
        int descent = apexY - targetSurfaceY;
        return descent >= radius && descent <= height;
    }

    public static int CompareDepositOrder(
        int leftPriority,
        ulong leftFeatureId,
        int rightPriority,
        ulong rightFeatureId)
    {
        int priority = rightPriority.CompareTo(leftPriority);
        return priority != 0 ? priority : leftFeatureId.CompareTo(rightFeatureId);
    }
}

internal struct ProceduralDepositRandom
{
    private ulong state;

    public ProceduralDepositRandom(ulong seed)
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
