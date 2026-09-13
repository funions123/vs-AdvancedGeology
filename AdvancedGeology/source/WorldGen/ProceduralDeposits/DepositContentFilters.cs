using System.Collections.Generic;
using System.Linq;

namespace AdvancedGeology.WorldGen.ProceduralDeposits;

/// <summary>
/// Player-facing content gates for procedural deposits.
/// </summary>
public static class DepositContentFilters
{
    /// <summary>
    /// Minerals a vanilla playthrough actually consumes, either directly in a vanilla recipe or
    /// by smelting into a vanilla metal. Keys use the normalized prospecting mineral names from
    /// <see cref="ProceduralDepositWorldGenSystem.NormalizeProspectingMineral"/> so this gate and
    /// the density map agree on naming.
    /// </summary>
    private static readonly HashSet<string> VanillaProgressionMinerals = new(StringComparer.Ordinal)
    {
        // Iron
        "hematite", "magnetite", "limonite", "siderite",
        // Copper
        "chalcopyrite", "chalcocite", "bornite", "nativecopper", "malachite", "azurite", "tetrahedrite",
        // Tin
        "cassiterite", "stannite",
        // Zinc, lead, silver, gold
        "sphalerite", "galena", "silver", "gold",
        // Bismuth
        "bismuthinite", "nativebismuth",
        // Nickel
        "pentlandite", "nickeline",
        // Chromium, titanium, manganese metals
        "chromite", "ilmenite", "rhodochrosite",
        // Fuels
        "bituminouscoal", "anthracite", "lignite",
        // Non-metal vanilla resources
        "sulfur", "cinnabar", "borax", "kernite", "fluorite", "graphite", "lapis",
        // Refractory brick material
        "magnesite",
        // Vanilla mordant
        "alum"
    };

    /// <summary>
    /// Gem accessory slots and the gangue slot each one degrades to when gems are disabled.
    /// Substituting the material keeps deposit shape, volume, and slot validation intact instead
    /// of deleting an ore deposit for its decorative minerals.
    /// </summary>
    private static readonly (string Slot, string[] Fallbacks)[] GemSlotFallbacks =
    {
        (ProceduralMaterialSlots.Topaz, new[] { ProceduralMaterialSlots.Quartz, ProceduralMaterialSlots.Core, ProceduralMaterialSlots.Wall }),
        (ProceduralMaterialSlots.Tourmaline, new[] { ProceduralMaterialSlots.Quartz, ProceduralMaterialSlots.Core, ProceduralMaterialSlots.Wall }),
        (ProceduralMaterialSlots.Schorl, new[] { ProceduralMaterialSlots.Core, ProceduralMaterialSlots.Quartz, ProceduralMaterialSlots.Wall }),
        (ProceduralMaterialSlots.Spessartine, new[] { ProceduralMaterialSlots.Quartz, ProceduralMaterialSlots.Breccia }),
        (ProceduralMaterialSlots.CelestineGem, new[] { ProceduralMaterialSlots.Celestine })
    };

    /// <summary>
    /// True when the deposit has at least one major mineral a vanilla playthrough consumes.
    /// </summary>
    public static bool IsVanillaProgressionDeposit(ProceduralDepositDefinition definition)
    {
        foreach (string mineral in definition.Prospecting.MajorMinerals)
        {
            if (string.IsNullOrWhiteSpace(mineral)) continue;
            if (VanillaProgressionMinerals.Contains(
                ProceduralDepositWorldGenSystem.NormalizeProspectingMineral(mineral.ToLowerInvariant())))
            {
                return true;
            }
        }

        return false;
    }

    public static bool IsVanillaProgressionMineral(string mineral)
    {
        return !string.IsNullOrWhiteSpace(mineral)
            && VanillaProgressionMinerals.Contains(
                ProceduralDepositWorldGenSystem.NormalizeProspectingMineral(mineral.ToLowerInvariant()));
    }

    public static bool IsGemMaterialSlot(string slot)
    {
        foreach ((string gemSlot, string[] _) in GemSlotFallbacks)
        {
            if (string.Equals(gemSlot, slot, StringComparison.Ordinal)) return true;
        }

        return false;
    }

    /// <summary>
    /// Replaces every gem accessory material with the deposit's own gangue material. Slots with no
    /// declared fallback are dropped, which is only reachable for optional decorative slots.
    /// </summary>
    public static KeyValuePair<string, string>[] ApplyGemSuppression(
        KeyValuePair<string, string>[] materials)
    {
        bool hasGemSlot = false;
        foreach (KeyValuePair<string, string> material in materials)
        {
            if (IsGemMaterialSlot(material.Key))
            {
                hasGemSlot = true;
                break;
            }
        }

        if (!hasGemSlot) return materials;

        var byKey = new Dictionary<string, string>(materials.Length, StringComparer.Ordinal);
        foreach (KeyValuePair<string, string> material in materials)
        {
            byKey[material.Key] = material.Value;
        }

        var result = new List<KeyValuePair<string, string>>(materials.Length);
        foreach (KeyValuePair<string, string> material in materials)
        {
            if (!IsGemMaterialSlot(material.Key))
            {
                result.Add(material);
                continue;
            }

            string? replacement = null;
            foreach ((string gemSlot, string[] fallbacks) in GemSlotFallbacks)
            {
                if (!string.Equals(gemSlot, material.Key, StringComparison.Ordinal)) continue;

                foreach (string fallback in fallbacks)
                {
                    if (byKey.TryGetValue(fallback, out string? code) && !string.IsNullOrWhiteSpace(code))
                    {
                        replacement = code;
                        break;
                    }
                }
                break;
            }

            if (replacement != null) result.Add(new KeyValuePair<string, string>(material.Key, replacement));
        }

        return result.ToArray();
    }
}
