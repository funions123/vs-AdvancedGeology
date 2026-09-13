namespace AdvancedGeology;

public sealed class AdvancedGeologyConfig
{
    /// <summary>
    /// Disables every gem deposit and degrades gem accessory minerals inside ore deposits to
    /// their gangue. Gem blocks and items stay registered for existing worlds and creative use.
    /// </summary>
    public bool IHateGems { get; set; }

    /// <summary>
    /// Disables procedural deposits whose major minerals feed no vanilla recipe or vanilla metal.
    /// Their blocks and items stay registered for existing worlds and creative use.
    /// </summary>
    public bool VanillaOresOnly { get; set; }

    /// <summary>
    /// Straight multiplier on the spawn chance of every mineral deposit: a deposit that seeds in 1% of
    /// its placement cells generates in 2% of them at 2.0. Applies to both procedural deposits and the
    /// surviving conventional mineral, gem, and soil deposit assets. Rock clusters are lithology rather
    /// than mineralization and are left alone. 1.0 is unmodified generation; 0 disables mineral deposits.
    /// Effective per-deposit chances are clamped to the engine's valid range.
    /// </summary>
    public double GlobalMineralAbundance { get; set; } = 1.0;

    /// <summary>
    /// Configuration used by world generation. Assigned once during <c>StartPre</c>, before any
    /// asset suppression or deposit compilation runs.
    /// </summary>
    public static AdvancedGeologyConfig Active { get; internal set; } = new();
}
