namespace AdvancedGeology;

public sealed class AdvancedGeologyConfig
{
    /// <summary>Disables gem deposits and replaces gem accessories with gangue.</summary>
    public bool IHateGems { get; set; }

    /// <summary>Disables deposits outside vanilla progression.</summary>
    public bool VanillaOresOnly { get; set; }

    /// <summary>Scales mineral deposit frequency. Zero disables deposits.</summary>
    public double GlobalMineralAbundance { get; set; } = 1.0;

    /// <summary>Active world-generation configuration.</summary>
    public static AdvancedGeologyConfig Active { get; internal set; } = new();
}
