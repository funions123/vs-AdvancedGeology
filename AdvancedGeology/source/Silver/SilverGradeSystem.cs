using System.Linq;
using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;

namespace AdvancedGeology.Silver;

/// <summary>
/// Certain ores may have a silver content stored in their attributes.
/// Active only when AdvancedMetallurgy is installed.
/// </summary>
public static class SilverGradeSystem
{
    public const string AttrKey = "silverGrade";

    // Noise wavelength in blocks. All veins inside a given 50-wide area will have the same silver grade.
    private const double Wavelength = 50.0;

    private static readonly HashSet<string> SilverBearingOres = new()
    {
        "tetrahedrite", "galena", "freibergite", "cerussite"
    };

    private static NormalizedSimplexNoise? noise;

    public static bool Active { get; private set; }

    public static void SetActive(bool active) => Active = active;

    public static void BuildNoise(long worldSeed)
    {
        noise = NormalizedSimplexNoise.FromDefaultOctaves(2, 1.0 / Wavelength, 0.5, worldSeed);
    }

    /// <summary>
    /// Excludes the silver attribute from stack-equality so graded stacks still merge.
    /// </summary>
    public static void RegisterIgnoredAttribute()
    {
        if (GlobalConstants.IgnoredStackAttributes.Contains(AttrKey)) return;
        GlobalConstants.IgnoredStackAttributes =
            GlobalConstants.IgnoredStackAttributes.Append(AttrKey).ToArray();
    }

    public static bool IsSilverBearingOre(string? oreType) =>
        oreType != null && SilverBearingOres.Contains(oreType);

    /// <summary>
    /// Quantized silver grade for a world position: silver units per unit of base metal,
    /// 0 = barren. Deterministic so the same world position always yields the same grade.
    /// </summary>
    public static double SampleGrade(int x, int y, int z)
    {
        if (noise == null) return 0.0;
        double n = noise.Noise(x, y, z);

        // Thresholds tuned so barren dominates.
        if (n > 0.85) return 0.08;   // rich
        if (n > 0.76) return 0.05;   // medium
        if (n > 0.66) return 0.02;   // poor
        return 0.0;                  // barren
    }

    public static double GetGrade(ItemStack? stack) =>
        stack?.Attributes?.GetDouble(AttrKey, 0.0) ?? 0.0;

    /// <summary>
    /// Weighted-average merge; combining stacks dilutes/concentrates the grade.
    /// Total silver (grade * size) is conserved.
    /// </summary>
    public static double MergeGrade(double sinkGrade, int sinkSize, double sourceGrade, int movedSize)
    {
        int total = sinkSize + movedSize;
        if (total <= 0) return sinkGrade;
        return (sinkGrade * sinkSize + sourceGrade * movedSize) / total;
    }

    public static void SetGrade(ItemStack? stack, double grade)
    {
        if (stack?.Attributes == null) return;
        if (grade > 0.0) stack.Attributes.SetDouble(AttrKey, grade);
        else stack.Attributes.RemoveAttribute(AttrKey);
    }
}
