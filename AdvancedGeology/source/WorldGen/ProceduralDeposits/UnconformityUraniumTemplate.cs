using System;

namespace AdvancedGeology.WorldGen.ProceduralDeposits;

/// <summary>
/// Uraninite, quartz, and hematite form a finite unconformity-bound lens, a fault-rooted feeder,
/// and smaller perched lenses.
/// </summary>
internal sealed class UnconformityUraniumPlan : IAdditionalDepositPlan
{
    private const ulong Salt = 0x554E434F4E465552UL;

    private readonly int originX;
    private readonly int originY;
    private readonly int originZ;
    private readonly double unconformityBase;
    private readonly double tiltX;
    private readonly double tiltZ;
    private readonly double seed;
    private readonly UraniumFault[] faults;
    private readonly Lens mainLens;
    private readonly Root root;
    private readonly Lens[] perchedLenses;

    private readonly record struct UraniumFault(
        double CosStrike,
        double SinStrike,
        double SinDip,
        double CosDip,
        double Offset,
        double CenterY,
        double Length,
        double Height,
        double Width,
        double Phase);

    private readonly record struct Lens(
        double X,
        double Y,
        double Z,
        double CosStrike,
        double SinStrike,
        double RadiusAlong,
        double RadiusAcross,
        double RadiusVertical,
        double Phase);

    private readonly record struct Root(double Top, double Bottom, double Along, double HalfWidth, double Phase);

    public int HorizontalRadius { get; }
    public int VerticalHalfHeight { get; }
    public int FaultCount => faults.Length;
    public int PerchedCount => perchedLenses.Length;

    private UnconformityUraniumPlan(
        in ProceduralDepositInstance instance,
        UnconformityUraniumDefinition settings,
        double unconformityBase,
        double tiltX,
        double tiltZ,
        double seed,
        UraniumFault[] faults,
        Lens mainLens,
        Root root,
        Lens[] perchedLenses)
    {
        originX = instance.CenterX;
        originY = instance.CenterY;
        originZ = instance.CenterZ;
        HorizontalRadius = settings.HorizontalRadius;
        VerticalHalfHeight = settings.VerticalHalfHeight;
        this.unconformityBase = unconformityBase;
        this.tiltX = tiltX;
        this.tiltZ = tiltZ;
        this.seed = seed;
        this.faults = faults;
        this.mainLens = mainLens;
        this.root = root;
        this.perchedLenses = perchedLenses;
    }

    public static UnconformityUraniumPlan Create(
        in ProceduralDepositInstance instance,
        UnconformityUraniumDefinition settings)
    {
        var random = new ProceduralDepositRandom(instance.FeatureId ^ Salt);
        double unconformityBase = random.Range(1, 7);
        double tiltX = random.Range(-0.05, 0.05);
        double tiltZ = random.Range(-0.04, 0.04);
        double strike = random.Range(0, 180);
        double dip = random.Range(52, 76);
        double seed = random.Range(0, 100);
        double strikeRadians = strike * Math.PI / 180;
        double cosStrike = Math.Cos(strikeRadians);
        double sinStrike = Math.Sin(strikeRadians);

        int splayCount = random.NextInt(settings.SplayMin, settings.SplayMax);
        var faults = new UraniumFault[splayCount + 1];
        faults[0] = CreateFault(
            strike,
            dip,
            random.Range(-5, 5),
            random.Range(-8, -1),
            random.Range(24, 32),
            random.Range(24, 32),
            random.Range(2.2, 3.4),
            seed);
        for (int index = 0; index < splayCount; index++)
        {
            faults[index + 1] = CreateFault(
                strike + random.Range(-26, 26),
                dip + random.Range(-10, 10),
                faults[0].Offset + (index % 2 == 0 ? -1 : 1) * random.Range(4, 9),
                random.Range(-14, -4),
                random.Range(14, 22),
                random.Range(16, 24),
                random.Range(1.3, 2.2),
                seed + 41 + index * 13);
        }

        double feederX = faults[0].Offset * sinStrike;
        double feederZ = faults[0].Offset * cosStrike;
        double alongOffset = random.Range(-6, 6);
        double lensX = feederX + alongOffset * cosStrike;
        double lensZ = feederZ - alongOffset * sinStrike;
        double lensY = UnconformityY(unconformityBase, tiltX, tiltZ, seed, lensX, lensZ);
        var mainLens = new Lens(
            lensX,
            lensY - random.Range(0.5, 2.5),
            lensZ,
            cosStrike,
            sinStrike,
            random.Range(11, 17),
            random.Range(4, 6.5),
            random.Range(2.6, 4.2),
            seed + 7.3);
        var root = new Root(
            mainLens.Y + 1.5,
            random.Range(-29, -24),
            lensX * cosStrike - lensZ * sinStrike,
            random.Range(8, 12),
            seed + 53);

        int perchedCount = random.NextInt(settings.PerchedMin, settings.PerchedMax);
        var perchedLenses = new Lens[perchedCount];
        for (int index = 0; index < perchedCount; index++)
        {
            double perchedX = lensX + random.Range(-7, 7);
            double perchedZ = lensZ + random.Range(-6, 6);
            perchedLenses[index] = new Lens(
                perchedX,
                UnconformityY(unconformityBase, tiltX, tiltZ, seed, perchedX, perchedZ) + random.Range(5, 12),
                perchedZ,
                cosStrike,
                sinStrike,
                random.Range(4, 7),
                random.Range(2.4, 4),
                random.Range(1.4, 2.4),
                seed + 83 + index * 9);
        }

        return new UnconformityUraniumPlan(
            instance,
            settings,
            unconformityBase,
            tiltX,
            tiltZ,
            seed,
            faults,
            mainLens,
            root,
            perchedLenses);
    }

    private static UraniumFault CreateFault(
        double strike,
        double dip,
        double offset,
        double centerY,
        double length,
        double height,
        double width,
        double phase)
    {
        double strikeRadians = strike * Math.PI / 180;
        double dipRadians = dip * Math.PI / 180;
        return new UraniumFault(
            Math.Cos(strikeRadians),
            Math.Sin(strikeRadians),
            Math.Sin(dipRadians),
            Math.Cos(dipRadians),
            offset,
            centerY,
            length,
            height,
            width,
            phase);
    }

    private static double UnconformityY(
        double baseY,
        double tiltX,
        double tiltZ,
        double seed,
        double x,
        double z)
    {
        return baseY + x * tiltX + z * tiltZ
            + Math.Sin(x * 0.052 + seed * 0.5) * 1.7
            + Math.Cos(z * 0.06 - seed * 0.35) * 1.4;
    }

    private static FaultSample SampleFault(double x, double y, double z, in UraniumFault fault)
    {
        double along = x * fault.CosStrike - z * fault.SinStrike;
        double across = x * fault.SinStrike + z * fault.CosStrike - fault.Offset;
        double vertical = y - fault.CenterY;
        double bend = Math.Sin(along * 0.12 + y * 0.06 + fault.Phase) * 1.4;
        double signed = across * fault.SinDip + vertical * fault.CosDip - bend;
        double alongNorm = along / fault.Length;
        double verticalNorm = vertical / fault.Height;
        double footprint = Math.Sqrt(alongNorm * alongNorm + verticalNorm * verticalNorm);
        double taper = AdditionalDepositMath.Clamp(1 - footprint * footprint, 0, 1);
        double distance = Math.Abs(signed);
        double halfWidth = fault.Width * 0.5 * Math.Sqrt(taper);
        return new FaultSample(
            footprint <= 1 && distance <= halfWidth,
            footprint <= 1.1 && distance <= (fault.Width * 0.5 + 3.2) * Math.Sqrt(Math.Max(taper, 0.05)),
            distance,
            halfWidth,
            along);
    }

    private static LensSample SampleLens(double x, double y, double z, in Lens lens)
    {
        double along = (x - lens.X) * lens.CosStrike - (z - lens.Z) * lens.SinStrike;
        double across = (x - lens.X) * lens.SinStrike + (z - lens.Z) * lens.CosStrike;
        double u = along / lens.RadiusAlong;
        double v = across / lens.RadiusAcross;
        double w = (y - lens.Y) / lens.RadiusVertical;
        double warp = 0.1 * Math.Sin(u * 4.1 + lens.Phase) * Math.Cos(v * 3.3)
            + 0.05 * Math.Sin(w * 4.6 - lens.Phase * 0.3);
        double radius = Math.Sqrt(u * u + v * v + w * w) + warp;
        return new LensSample(radius <= 1, radius);
    }

    public AdditionalDepositSample Evaluate(int worldX, int worldY, int worldZ)
    {
        double x = worldX - originX;
        double y = worldY - originY;
        double z = worldZ - originZ;
        if (Math.Abs(x) > HorizontalRadius
            || Math.Abs(z) > HorizontalRadius
            || Math.Abs(y) > VerticalHalfHeight)
        {
            return default;
        }

        double unconformityY = UnconformityY(unconformityBase, tiltX, tiltZ, seed, x, z);
        FaultSample mainFault = SampleFault(x, y, z, faults[0]);
        bool faultCore = mainFault.Core;
        for (int index = 1; index < faults.Length; index++)
        {
            faultCore |= SampleFault(x, y, z, faults[index]).Core;
        }

        double band = Math.Sin(x * 0.23 + seed)
            + Math.Cos(z * 0.21 - seed * 0.5)
            + 0.6 * Math.Sin(y * 0.27 + x * 0.05);
        LensSample mainHit = SampleLens(x, y, z, mainLens);
        if (mainHit.Inside)
        {
            if (mainHit.Radius < 0.62) return new AdditionalDepositSample(ProceduralMaterialSlots.Uraninite, 3);
            if (mainHit.Radius < 0.84)
            {
                return band > -0.35
                    ? new AdditionalDepositSample(ProceduralMaterialSlots.Uraninite, 2)
                    : new AdditionalDepositSample(ProceduralMaterialSlots.Uraninite, 1);
            }
            return band > 0.3
                ? new AdditionalDepositSample(ProceduralMaterialSlots.Quartz)
                : new AdditionalDepositSample(ProceduralMaterialSlots.Uraninite, 0);
        }

        if (y <= root.Top && y >= root.Bottom && mainFault.Core)
        {
            double depth = AdditionalDepositMath.Clamp((root.Top - y) / (root.Top - root.Bottom), 0, 1);
            double taper = 0.58 + 0.42 * Math.Sin(Math.PI * depth);
            double alongNorm = (mainFault.Along - root.Along) / (root.HalfWidth * taper);
            if (Math.Abs(alongNorm) <= 1)
            {
                double shoot = Math.Cos(alongNorm * 4.8 + root.Phase) - 0.35 * depth;
                double ratio = mainFault.Distance / Math.Max(0.1, mainFault.HalfWidth);
                if (ratio < 0.42 && shoot > -0.5)
                {
                    return new AdditionalDepositSample(ProceduralMaterialSlots.Uraninite, 3);
                }
                if (ratio < 0.92) return new AdditionalDepositSample(ProceduralMaterialSlots.Uraninite, 1);
                return default;
            }
        }

        foreach (Lens perched in perchedLenses)
        {
            LensSample hit = SampleLens(x, y, z, perched);
            if (!hit.Inside) continue;
            if (hit.Radius < 0.55) return new AdditionalDepositSample(ProceduralMaterialSlots.Uraninite, 1);
            return band > 0.25 ? new AdditionalDepositSample(ProceduralMaterialSlots.Quartz) : default;
        }

        if (y <= unconformityY)
        {
            double depthBelow = unconformityY - y;
            if (faultCore && depthBelow < 4)
            {
                return new AdditionalDepositSample(ProceduralMaterialSlots.Hematite, 0);
            }

            double along = (x - mainLens.X) * mainLens.CosStrike - (z - mainLens.Z) * mainLens.SinStrike;
            double across = (x - mainLens.X) * mainLens.SinStrike + (z - mainLens.Z) * mainLens.CosStrike;
            double apronAlong = mainLens.RadiusAlong * 1.55;
            double apronAcross = mainLens.RadiusAcross * 1.8;
            double apronRadius = Math.Sqrt(
                along * along / (apronAlong * apronAlong)
                + across * across / (apronAcross * apronAcross));
            if (apronRadius < 1)
            {
                double edgeTaper = 1 - apronRadius * apronRadius;
                double thicknessWarp = 0.78
                    + 0.22 * Math.Sin(along * 0.24 + across * 0.17 + seed);
                double apronThickness = 3.2 * edgeTaper * thicknessWarp;
                if (depthBelow < apronThickness && band > -0.25)
                {
                    return new AdditionalDepositSample(ProceduralMaterialSlots.Hematite, 0);
                }
            }
            return default;
        }

        double above = y - unconformityY;
        double perchedRadius = 3.5 + 0.85 * above;
        double mainAlong = (x - mainLens.X) * mainLens.CosStrike - (z - mainLens.Z) * mainLens.SinStrike;
        double mainAcross = (x - mainLens.X) * mainLens.SinStrike + (z - mainLens.Z) * mainLens.CosStrike;
        double planarDistance = Math.Sqrt(mainAlong * mainAlong / (1.9 * 1.9) + mainAcross * mainAcross);
        if (above < 26 && planarDistance < perchedRadius)
        {
            return faultCore && above < 9 && band > 0.75
                ? new AdditionalDepositSample(ProceduralMaterialSlots.Quartz)
                : default;
        }
        if (faultCore && above < 12)
        {
            return band > 0.4 ? new AdditionalDepositSample(ProceduralMaterialSlots.Quartz) : default;
        }
        return default;
    }

    private readonly record struct FaultSample(
        bool Core,
        bool Halo,
        double Distance,
        double HalfWidth,
        double Along);

    private readonly record struct LensSample(bool Inside, double Radius);
}
