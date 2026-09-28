using System;
using System.Collections.Generic;
using System.Linq;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;

namespace AdvancedGeology.Byproducts;

/// <summary>
/// Reads and writes the per-unit trace-element composition carried by an item stack.
/// Missing elements have zero concentration. The attribute is ignored for stack equality;
/// merge patches conserve each element's total amount instead.
/// </summary>
public static class ByproductComposition
{
    public const string AttributeKey = "advancedgeology:byproductComposition";
    public const string LegacySilverAttributeKey = "silverGrade";

    private const int CurrentVersion = 1;
    private const string VersionKey = "version";
    private const string ElementsKey = "elements";
    private const string SilverElement = "Ag";

    private static readonly IReadOnlyDictionary<string, double> EmptyComposition =
        new Dictionary<string, double>(0, StringComparer.Ordinal);

    /// <summary>
    /// Reads all valid positive concentrations. A legacy <c>silverGrade</c> value is migrated to
    /// version 1 as Ag when touched. Malformed or unsupported compound versions are discarded so
    /// an equality-ignored attribute cannot silently survive a merge without conservation.
    /// </summary>
    public static IReadOnlyDictionary<string, double> Read(ItemStack? stack)
    {
        ITreeAttribute? attributes = stack?.Attributes;
        if (attributes == null) return EmptyComposition;

        bool hasCompound = attributes.HasAttribute(AttributeKey);
        bool hasLegacy = attributes.HasAttribute(LegacySilverAttributeKey);
        if (!hasCompound && !hasLegacy) return EmptyComposition;

        ITreeAttribute? compound = hasCompound ? attributes.GetTreeAttribute(AttributeKey) : null;
        if (compound != null)
        {
            int version = compound.GetInt(VersionKey, -1);
            if (version == CurrentVersion)
            {
                Dictionary<string, double> composition = ReadElements(compound.GetTreeAttribute(ElementsKey));
                attributes.RemoveAttribute(LegacySilverAttributeKey);
                if (composition.Count != 0) return composition;

                attributes.RemoveAttribute(AttributeKey);
                return EmptyComposition;
            }

            // Unsupported data cannot safely participate in equality-ignored stack merges.
            // Remove it below, then still honor a valid legacy value if one is present.
        }

        // A malformed/obsolete current key must not mask a valid legacy silver value.
        attributes.RemoveAttribute(AttributeKey);

        if (!hasLegacy) return EmptyComposition;
        double legacySilver = attributes.GetDouble(LegacySilverAttributeKey, 0.0);
        if (!IsValidConcentration(legacySilver))
        {
            if (attributes.HasAttribute(LegacySilverAttributeKey))
            {
                attributes.RemoveAttribute(LegacySilverAttributeKey);
            }
            return EmptyComposition;
        }

        Dictionary<string, double> migrated = new(1, StringComparer.Ordinal)
        {
            [SilverElement] = legacySilver
        };
        Write(stack, migrated);
        return migrated;
    }

    /// <summary>
    /// Replaces the stack composition with a canonical sparse version 1 tree. Non-positive,
    /// non-finite, and blank-key entries are omitted. An empty composition removes the attribute.
    /// </summary>
    public static void Write(ItemStack? stack, IReadOnlyDictionary<string, double> composition)
    {
        ITreeAttribute? attributes = stack?.Attributes;
        if (attributes == null) return;

        attributes.RemoveAttribute(LegacySilverAttributeKey);
        if (composition == null || composition.Count == 0)
        {
            attributes.RemoveAttribute(AttributeKey);
            return;
        }

        TreeAttribute elements = new();
        foreach ((string element, double concentration) in composition)
        {
            if (string.IsNullOrWhiteSpace(element) || !IsValidConcentration(concentration)) continue;
            elements.SetDouble(element, concentration);
        }

        if (elements.Count == 0)
        {
            attributes.RemoveAttribute(AttributeKey);
            return;
        }

        TreeAttribute compound = new();
        compound.SetInt(VersionKey, CurrentVersion);
        compound.SetAttribute(ElementsKey, elements);
        attributes[AttributeKey] = compound;
    }

    /// <summary>
    /// Produces the per-unit composition after moving <paramref name="movedCount"/> source items
    /// into <paramref name="sinkCount"/> sink items. Missing elements contribute zero, so the
    /// amount of every element (concentration times item count) is conserved.
    /// </summary>
    public static IReadOnlyDictionary<string, double> Merge(
        IReadOnlyDictionary<string, double> sink,
        int sinkCount,
        IReadOnlyDictionary<string, double> source,
        int movedCount)
    {
        int validSinkCount = Math.Max(0, sinkCount);
        int validMovedCount = Math.Max(0, movedCount);
        long totalCount = (long)validSinkCount + validMovedCount;
        if (totalCount == 0) return EmptyComposition;

        Dictionary<string, double> totals = new(StringComparer.Ordinal);
        AddWeighted(totals, sink, validSinkCount);
        AddWeighted(totals, source, validMovedCount);

        if (totals.Count == 0) return EmptyComposition;

        double divisor = totalCount;
        List<string>? invalidKeys = null;
        foreach (string element in totals.Keys)
        {
            double concentration = totals[element] / divisor;
            if (IsValidConcentration(concentration))
            {
                totals[element] = concentration;
            }
            else
            {
                (invalidKeys ??= new List<string>()).Add(element);
            }
        }

        if (invalidKeys != null)
        {
            foreach (string element in invalidKeys) totals.Remove(element);
        }

        return totals.Count == 0 ? EmptyComposition : totals;
    }

    /// <summary>
    /// Combines weighted source material while normalizing by a caller-observed output count.
    /// Used by transformations whose consumed and produced item counts differ.
    /// </summary>
    public static IReadOnlyDictionary<string, double> MergeToCount(
        IReadOnlyDictionary<string, double> sink,
        int sinkCount,
        IReadOnlyDictionary<string, double> source,
        int consumedCount,
        int outputCount)
    {
        int validSinkCount = Math.Max(0, sinkCount);
        int validConsumedCount = Math.Max(0, consumedCount);
        if (outputCount <= 0) return EmptyComposition;

        Dictionary<string, double> totals = new(StringComparer.Ordinal);
        AddWeighted(totals, sink, validSinkCount);
        AddWeighted(totals, source, validConsumedCount);
        if (totals.Count == 0) return EmptyComposition;

        List<string>? invalidKeys = null;
        foreach (string element in totals.Keys)
        {
            double concentration = totals[element] / outputCount;
            if (IsValidConcentration(concentration))
            {
                totals[element] = concentration;
            }
            else
            {
                (invalidKeys ??= new List<string>()).Add(element);
            }
        }

        if (invalidKeys != null)
        {
            foreach (string element in invalidKeys) totals.Remove(element);
        }
        return totals.Count == 0 ? EmptyComposition : totals;
    }

    /// <summary>
    /// Registers both the new compound and legacy silver keys as stack-equality exceptions.
    /// Call once during <c>StartPre</c>, before inventories begin comparing stacks.
    /// </summary>
    public static void RegisterIgnoredAttribute()
    {
        string[] current = GlobalConstants.IgnoredStackAttributes;
        bool addCurrent = !current.Contains(AttributeKey, StringComparer.Ordinal);
        bool addLegacy = !current.Contains(LegacySilverAttributeKey, StringComparer.Ordinal);
        if (!addCurrent && !addLegacy) return;

        string[] updated = new string[current.Length + (addCurrent ? 1 : 0) + (addLegacy ? 1 : 0)];
        Array.Copy(current, updated, current.Length);
        int index = current.Length;
        if (addCurrent) updated[index++] = AttributeKey;
        if (addLegacy) updated[index] = LegacySilverAttributeKey;
        GlobalConstants.IgnoredStackAttributes = updated;
    }

    private static Dictionary<string, double> ReadElements(ITreeAttribute? elements)
    {
        if (elements == null) return new Dictionary<string, double>(0, StringComparer.Ordinal);

        Dictionary<string, double> composition = new(elements.Count, StringComparer.Ordinal);
        foreach (KeyValuePair<string, IAttribute> entry in elements)
        {
            if (string.IsNullOrWhiteSpace(entry.Key)) continue;
            double concentration = elements.GetDecimal(entry.Key, double.NaN);
            if (IsValidConcentration(concentration)) composition[entry.Key] = concentration;
        }
        return composition;
    }

    private static void AddWeighted(
        Dictionary<string, double> totals,
        IReadOnlyDictionary<string, double>? composition,
        int count)
    {
        if (composition == null || count <= 0) return;

        foreach ((string element, double concentration) in composition)
        {
            if (string.IsNullOrWhiteSpace(element) || !IsValidConcentration(concentration)) continue;
            double amount = concentration * count;
            if (!double.IsFinite(amount) || amount <= 0.0) continue;
            totals[element] = totals.TryGetValue(element, out double existing) ? existing + amount : amount;
        }
    }

    private static bool IsValidConcentration(double concentration) =>
        double.IsFinite(concentration) && concentration > 0.0;
}
