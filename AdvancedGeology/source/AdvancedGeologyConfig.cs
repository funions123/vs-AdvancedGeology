namespace AdvancedGeology;

public sealed class AdvancedGeologyConfig
{
    /// <summary>
    /// Disables AdvancedGeology deposits that do not contribute to vanilla progression.
    /// Their blocks and items remain registered for existing worlds and creative use.
    /// </summary>
    public bool DisableNonVanillaProgressionGenerators { get; set; } = false;
}
