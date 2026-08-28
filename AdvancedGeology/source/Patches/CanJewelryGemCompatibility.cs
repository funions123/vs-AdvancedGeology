using System.Collections;
using System.Reflection;
using System.Text;
using HarmonyLib;
using Newtonsoft.Json.Linq;
using Vintagestory.API.Common;
using Vintagestory.API.Server;

namespace AdvancedGeology.Patches;

/// <summary>
/// Bridges AdvancedGeology rough gems into CAN Jewelry's native cutting and socket systems.
/// All CAN types are resolved at runtime so CAN remains an optional dependency.
/// </summary>
internal static class CanJewelryGemCompatibility
{
    private const string CanSystemTypeName = "canjewelry.src.canjewelry";
    private const string CanConfigTypeName = "canjewelry.src.Config";
    private const string CanEncrustableTypeName = "canjewelry.src.CB.EncrustableCB";
    private const string CanCuttableTypeName = "canjewelry.src.cb.CANGemCuttableCB";
    private const string RuntimeDataPath = "advancedgeology:config/canjewelry-gems.json";

    private static readonly HashSet<string> GeoAddonsSharedGems =
    [
        "spinelred",
        "topazblue",
        "tourmalinerubellite",
        "tourmalineschorl",
        "tourmalineverdelite"
    ];

    private static readonly string[] JewelryEligibility =
    [
        "cansimplenecklace",
        "cantiara",
        "cancoronet",
        "canhoruseye",
        "canmonocle",
        "canarmband",
        "cannadiyannecklace",
        "canring",
        "canrottenkingmask"
    ];

    private static readonly Dictionary<string, GemDefinition> Gems = new(StringComparer.Ordinal);
    private static readonly HashSet<string> MissingTargetWarnings = new(StringComparer.Ordinal);
    private static ICoreAPI? coreApi;
    private static bool adornmentsEnabled;

    public static void Apply(Harmony harmony, ICoreAPI api)
    {
        coreApi = api;
        adornmentsEnabled = api.ModLoader.IsModEnabled("canjewelryadornments");

        Type? cuttableType = AccessTools.TypeByName(CanCuttableTypeName);
        MethodInfo? getMatchingRecipes = cuttableType == null
            ? null
            : AccessTools.Method(cuttableType, "GetMatchingRecipes", [typeof(ItemStack)]);
        if (getMatchingRecipes != null)
        {
            harmony.Patch(
                getMatchingRecipes,
                prefix: new HarmonyMethod(typeof(CanJewelryGemCompatibility), nameof(BeforeGetMatchingRecipes)));
        }
        else
        {
            api.Logger.Warning(
                "[AdvancedGeology] CAN Jewelry compat: GetMatchingRecipes was not found; AG rough gems cannot enter the cutting table");
        }

        Type? configType = AccessTools.TypeByName(CanConfigTypeName);
        MethodInfo? expandItemGroups = configType == null
            ? null
            : AccessTools.Method(configType, "ExpandItemGroups", Type.EmptyTypes);
        if (expandItemGroups != null)
        {
            harmony.Patch(
                expandItemGroups,
                prefix: new HarmonyMethod(typeof(CanJewelryGemCompatibility), nameof(BeforeExpandItemGroups)));
        }
        else
        {
            api.Logger.Warning(
                "[AdvancedGeology] CAN Jewelry compat: Config.ExpandItemGroups was not found; received client configs cannot be extended");
        }

        if (adornmentsEnabled)
        {
            Type? encrustableType = AccessTools.TypeByName(CanEncrustableTypeName);
            MethodInfo? getSocketTiers = encrustableType == null
                ? null
                : AccessTools.Method(encrustableType, "GetSocketsTiers", [typeof(ItemStack)]);
            MethodInfo? getMaxSockets = encrustableType == null
                ? null
                : AccessTools.Method(encrustableType, "GetMaxAmountSockets", [typeof(ItemStack)]);
            if (getSocketTiers != null && getMaxSockets != null)
            {
                harmony.Patch(
                    getSocketTiers,
                    postfix: new HarmonyMethod(typeof(CanJewelryGemCompatibility), nameof(AfterGetSocketTiers)));
                harmony.Patch(
                    getMaxSockets,
                    postfix: new HarmonyMethod(typeof(CanJewelryGemCompatibility), nameof(AfterGetMaxSockets)));
            }
            else
            {
                api.Logger.Warning(
                    "[AdvancedGeology] CAN Jewelry balance: socket query methods were not found; Adornments material balance is unchanged");
            }
        }
    }

    public static void InitializeAndReconcile(ICoreAPI api)
    {
        if (!LoadDefinitions(api)) return;

        int reordered = ReorderTextureFallbacks(api);
        int deduplicated = DeduplicateGemStates(api);
        int recipeEntriesRemoved = 0;
        int disabledGridBridges = 0;

        if (api.ModLoader.IsModEnabled("geoaddons"))
        {
            recipeEntriesRemoved = RemoveSharedGemsFromBaseRecipes(api);
        }
        else
        {
            disabledGridBridges = DisableUnloadedGeoAddonsGridBridges(api);
        }

        object? config = GetCanConfig();
        int injected = config == null ? 0 : InjectMissingConfig(config);
        int socketItemsBalanced = config == null ? 0 : ApplySocketItemBalance(config);

        api.Logger.Notification(
            "[AdvancedGeology] CAN Jewelry compat: loaded {0} gems, reordered {1} texture fallbacks, removed {2} duplicate states and {3} shared base-recipe entries, disabled {4} unloaded-GeoAddons grid bridges, injected {5} config entries, balanced {6} socket items",
            Gems.Count,
            reordered,
            deduplicated,
            recipeEntriesRemoved,
            disabledGridBridges,
            injected,
            socketItemsBalanced);
    }

    public static void ScheduleRuntimeVerification(ICoreServerAPI api)
    {
        api.Event.ServerRunPhase(
            EnumServerRunPhase.RunGame,
            () => api.Event.RegisterCallback(_ => FinalizeRoughGemTooltipsAndVerify(api), 1000));
    }

    private static void FinalizeRoughGemTooltipsAndVerify(ICoreServerAPI api)
    {
        object? config = GetCanConfig();
        IDictionary? gemToBuff = config == null ? null : FieldDictionary(config, "gem_type_to_buff");
        IDictionary? possibleBuffs = config == null ? null : FieldDictionary(config, "PossibleGemBuffs");
        IDictionary? eligibleItems = config == null ? null : FieldDictionary(config, "buffNameToPossibleItem");

        int missingItems = 0;
        int missingConfigEntries = 0;
        foreach ((string code, GemDefinition definition) in Gems)
        {
            foreach (string quality in new[] { "chipped", "flawed", "normal" })
            {
                if (api.World.GetItem(new AssetLocation("canjewelry", $"gem-rough-{quality}-{code}")) == null)
                    missingItems++;
            }
            foreach (string quality in new[] { "normal", "flawless", "exquisite" })
            {
                if (api.World.GetItem(new AssetLocation("canjewelry", $"gem-cut-{quality}-{code}")) == null)
                    missingItems++;
            }

            if (!definition.ProvidedByCanBase && !definition.ProvidedByCanGeoAddons)
            {
                if (gemToBuff?.Contains(code) != true) missingConfigEntries++;
                if (possibleBuffs?.Contains(code) != true) missingConfigEntries++;
                if (eligibleItems?.Contains(code) != true) missingConfigEntries++;
            }

            Item? roughGem = api.World.GetItem(new AssetLocation("game", $"gem-{code}-rough"));
            JToken? attributesToken = roughGem?.Attributes?.Token;
            if (attributesToken != null && gemToBuff?[code] is string configuredBuff)
            {
                attributesToken["canGemTypeToAttribute"] = configuredBuff;
            }
        }

        int directCutFailures = 0;
        foreach (string code in new[] { "jadeimperial", "topazblue", "ruby", "citrine" })
        {
            Item? roughGem = api.World.GetItem(new AssetLocation("game", $"gem-{code}-rough"));
            CollectibleBehavior? behavior = roughGem?.CollectibleBehaviors?
                .FirstOrDefault(candidate => candidate.GetType().FullName == CanCuttableTypeName);
            MethodInfo? matcher = behavior == null
                ? null
                : AccessTools.Method(behavior.GetType(), "GetMatchingRecipes", [typeof(ItemStack)]);
            if (behavior == null || matcher == null)
            {
                directCutFailures++;
                continue;
            }

            ItemStack stack = new(roughGem!);
            stack.Attributes.SetString("potential", "medium");
            object? matches = matcher.Invoke(behavior, [stack]);
            if (matches is not ICollection collection || collection.Count != 3) directCutFailures++;
        }

        int socketBalanceFailures = VerifySocketBalance(api, config);

        if (missingItems > 0 || missingConfigEntries > 0 || directCutFailures > 0 || socketBalanceFailures > 0)
        {
            api.Logger.Warning(
                "[AdvancedGeology] CAN Jewelry compat verification failed: {0} missing processed items, {1} missing config entries, {2} direct-cut sample failures, {3} socket-balance failures",
                missingItems,
                missingConfigEntries,
                directCutFailures,
                socketBalanceFailures);
            return;
        }

        api.Logger.Notification(
            "[AdvancedGeology] CAN Jewelry compat verification passed: 336 AG processed items, 147 injected config entries, 4 direct-cut samples with 3 shapes each, gold-first socket balance");
    }

    private static int VerifySocketBalance(ICoreServerAPI api, object? config)
    {
        int failures = 0;
        IDictionary? levels = config == null ? null : FieldDictionary(config, "LevelOfSocketByType");
        Dictionary<string, int> expectedLevels = new(StringComparer.Ordinal)
        {
            ["canjewelry:cansocket-tinbronze"] = 1,
            ["canjewelry:cansocket-bismuthbronze"] = 1,
            ["canjewelry:cansocket-blackbronze"] = 2,
            ["canjewelry:cansocket-iron"] = 1,
            ["canjewelry:cansocket-meteoriciron"] = 1,
            ["canjewelry:cansocket-steel"] = 2,
            ["canjewelry:cansocket-gold"] = 3,
            ["canjewelry:cansocket-silver"] = 2
        };
        foreach ((string code, int expected) in expectedLevels)
        {
            if (levels?[code] is not int actual || actual != expected) failures++;
        }

        if (!adornmentsEnabled) return failures;

        Type? encrustableType = AccessTools.TypeByName(CanEncrustableTypeName);
        MethodInfo? getSocketTiers = encrustableType == null
            ? null
            : AccessTools.Method(encrustableType, "GetSocketsTiers", [typeof(ItemStack)]);
        Item? tiara = api.World.GetItem(new AssetLocation("canjewelry", "cantiara-normal-tiara"));
        if (getSocketTiers == null || tiara == null) return failures + 1;

        Dictionary<string, int[]> expectedJewelry = new(StringComparer.Ordinal)
        {
            ["gold"] = [3, 3, 3],
            ["silver"] = [2, 2],
            ["steel"] = [2, 2],
            ["blackbronze"] = [2, 2],
            ["iron"] = [1],
            ["blacksteel"] = [1]
        };
        foreach ((string material, int[] expected) in expectedJewelry)
        {
            ItemStack stack = new(tiara);
            stack.Attributes.SetString("carcassus", material);
            object? result = getSocketTiers.Invoke(null, [stack]);
            if (result is not int[] tiers || !tiers.SequenceEqual(expected)) failures++;
        }

        foreach (string material in new[] { "gold", "steel" })
        {
            Item? coronet = api.World.SearchItems(new AssetLocation("canjewelry:cancoronet-*"))
                .FirstOrDefault(item => item.Variant?["loop"] == material);
            if (coronet == null)
            {
                failures++;
                continue;
            }
            ItemStack stack = new(coronet);
            object? result = getSocketTiers.Invoke(null, [stack]);
            int[] expected = material == "gold" ? [3, 3, 3] : [2, 2];
            if (result is not int[] tiers || !tiers.SequenceEqual(expected)) failures++;
        }

        Item? necklace = api.World.GetItem(new AssetLocation("canjewelry", "cansimplenecklace-normal-neck"));
        if (necklace == null)
        {
            failures++;
        }
        else
        {
            foreach ((string material, int expected) in new[]
                     {
                         ("gold", 3),
                         ("silver", 2),
                         ("steel", 2),
                         ("blackbronze", 2),
                         ("meteoriciron", 1)
                     })
            {
                ItemStack stack = new(necklace);
                stack.Attributes.SetString("loop", material);
                object? result = getSocketTiers.Invoke(null, [stack]);
                if (result is not int[] tiers || !tiers.SequenceEqual([expected])) failures++;
            }
        }
        return failures;
    }
    private static void BeforeGetMatchingRecipes(ref ItemStack stack)
    {
        ICoreAPI? api = coreApi;
        if (api == null || stack?.Collectible?.Code == null) return;
        if (stack.Collectible.Code.Domain != "game") return;

        string? gemCode = stack.Collectible.Variant?["ore"];
        if (gemCode == null || !Gems.ContainsKey(gemCode)) return;
        if (stack.Collectible.Code.Path != $"gem-{gemCode}-rough") return;

        string? quality = stack.Attributes.GetString("potential") switch
        {
            "low" => "chipped",
            "medium" => "flawed",
            "high" => "normal",
            _ => null
        };
        if (quality == null) return;

        AssetLocation targetCode = new("canjewelry", $"gem-rough-{quality}-{gemCode}");
        Item? target = api.World.GetItem(targetCode);
        if (target == null)
        {
            if (MissingTargetWarnings.Add(targetCode.ToString()))
            {
                api.Logger.Warning(
                    "[AdvancedGeology] CAN Jewelry compat: matching item {0} is missing; leaving the AG rough gem untouched",
                    targetCode);
            }
            return;
        }

        stack = new ItemStack(target, stack.StackSize);
    }

    private static void BeforeExpandItemGroups(object __instance)
    {
        ApplySocketItemBalance(__instance);
        if (Gems.Count > 0) InjectMissingConfig(__instance);
    }

    private static void AfterGetSocketTiers(ItemStack itemstack, ref int[] __result)
    {
        int[]? balanced = BalancedAdornmentsTiers(itemstack);
        if (balanced != null) __result = balanced;
    }

    private static void AfterGetMaxSockets(ItemStack itemstack, ref int __result)
    {
        int[]? balanced = BalancedAdornmentsTiers(itemstack);
        if (balanced != null) __result = balanced.Length;
    }

    private static int[]? BalancedAdornmentsTiers(ItemStack stack)
    {
        if (!adornmentsEnabled || stack?.Collectible?.Code?.Domain != "canjewelry") return null;

        string path = stack.Collectible.Code.Path;
        bool isCoronet = path.StartsWith("cancoronet-", StringComparison.Ordinal);
        bool isTiara = path.StartsWith("cantiara-", StringComparison.Ordinal);
        bool supported = isCoronet || isTiara ||
            path.StartsWith("canring-", StringComparison.Ordinal) ||
            path.StartsWith("canarmband-", StringComparison.Ordinal) ||
            path.StartsWith("cannadiyannecklace-", StringComparison.Ordinal) ||
            path.StartsWith("canrottenkingmask-", StringComparison.Ordinal) ||
            path.StartsWith("cansimplenecklace-", StringComparison.Ordinal) ||
            path.StartsWith("canmonocle-", StringComparison.Ordinal) ||
            path.StartsWith("canhoruseye-", StringComparison.Ordinal);
        if (!supported) return null;

        string? material = MaterialValue(stack, "metal") ??
            MaterialValue(stack, "loop") ??
            MaterialValue(stack, "carcassus");
        if (material == null) return null;

        int tier = material switch
        {
            "gold" => 3,
            "silver" or "steel" or "blackbronze" => 2,
            _ => 1
        };
        int sockets = isCoronet || isTiara ? tier : 1;
        return Enumerable.Repeat(tier, sockets).ToArray();
    }

    private static string? MaterialValue(ItemStack stack, string key)
    {
        string? value = stack.Attributes.GetString(key, null);
        if (!string.IsNullOrWhiteSpace(value)) return value;
        return stack.Collectible.Variant?.TryGetValue(key, out string? variant) == true ? variant : null;
    }

    private static int ApplySocketItemBalance(object config)
    {
        IDictionary? levels = FieldDictionary(config, "LevelOfSocketByType");
        if (levels == null) return 0;

        Dictionary<string, int> desired = new(StringComparer.Ordinal)
        {
            ["canjewelry:cansocket-tinbronze"] = 1,
            ["canjewelry:cansocket-bismuthbronze"] = 1,
            ["canjewelry:cansocket-blackbronze"] = 2,
            ["canjewelry:cansocket-iron"] = 1,
            ["canjewelry:cansocket-meteoriciron"] = 1,
            ["canjewelry:cansocket-steel"] = 2,
            ["canjewelry:cansocket-gold"] = 3,
            ["canjewelry:cansocket-silver"] = 2
        };

        int changed = 0;
        foreach ((string code, int tier) in desired)
        {
            if (levels[code] is int current && current == tier) continue;
            levels[code] = tier;
            changed++;
        }
        return changed;
    }

    private static bool LoadDefinitions(ICoreAPI api)
    {
        IAsset? asset = api.Assets.TryGet(new AssetLocation(RuntimeDataPath));
        if (asset == null)
        {
            api.Logger.Warning("[AdvancedGeology] CAN Jewelry compat data is missing: {0}", RuntimeDataPath);
            return false;
        }

        JObject root;
        try
        {
            root = JObject.Parse(asset.ToText());
        }
        catch (Exception exception)
        {
            api.Logger.Warning("[AdvancedGeology] Could not parse CAN Jewelry compat data: {0}", exception.Message);
            return false;
        }

        Gems.Clear();
        foreach (JObject gem in root["gems"] as JArray ?? [])
        {
            string? code = gem.Value<string>("code");
            string? buff = gem.Value<string>("buff");
            if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(buff)) continue;

            Gems[code] = new GemDefinition(
                buff,
                gem.Value<bool>("providedByCanBase"),
                gem.Value<bool>("providedByCanGeoAddons"));
        }

        if (Gems.Count != 56)
        {
            api.Logger.Warning(
                "[AdvancedGeology] CAN Jewelry compat data contains {0} gems instead of 56; compatibility disabled",
                Gems.Count);
            Gems.Clear();
            return false;
        }

        return true;
    }

    private static object? GetCanConfig()
    {
        Type? systemType = AccessTools.TypeByName(CanSystemTypeName);
        return systemType == null ? null : AccessTools.Field(systemType, "config")?.GetValue(null);
    }

    private static int InjectMissingConfig(object config)
    {
        IDictionary? possibleBuffs = FieldDictionary(config, "PossibleGemBuffs");
        IDictionary? gemToBuff = FieldDictionary(config, "gem_type_to_buff");
        IDictionary? eligibleItems = FieldDictionary(config, "buffNameToPossibleItem");
        if (possibleBuffs == null || gemToBuff == null || eligibleItems == null) return 0;

        int injected = 0;
        foreach ((string code, GemDefinition definition) in Gems)
        {
            if (definition.ProvidedByCanBase || definition.ProvidedByCanGeoAddons) continue;

            if (!possibleBuffs.Contains(code))
            {
                possibleBuffs.Add(code, new HashSet<string>([definition.Buff], StringComparer.Ordinal));
                injected++;
            }
            if (!gemToBuff.Contains(code))
            {
                gemToBuff.Add(code, definition.Buff);
                injected++;
            }
            if (!eligibleItems.Contains(code))
            {
                eligibleItems.Add(code, new HashSet<string>(JewelryEligibility, StringComparer.Ordinal));
                injected++;
            }
        }
        return injected;
    }

    private static IDictionary? FieldDictionary(object instance, string fieldName) =>
        AccessTools.Field(instance.GetType(), fieldName)?.GetValue(instance) as IDictionary;

    private static int ReorderTextureFallbacks(ICoreAPI api)
    {
        int reordered = 0;
        foreach (string path in new[]
                 {
                     "canjewelry:itemtypes/resource/gem-rough.json",
                     "canjewelry:itemtypes/resource/gem-cut.json"
                 })
        {
            if (!TryReadObject(api, path, out IAsset asset, out JObject root)) continue;
            if (root["texturesByType"] is not JObject textures) continue;
            JProperty? fallback = textures.Property("*");
            if (fallback == null || fallback == textures.Properties().Last()) continue;

            JToken value = fallback.Value.DeepClone();
            fallback.Remove();
            textures.Add("*", value);
            SaveAsset(asset, root);
            reordered++;
        }
        return reordered;
    }

    private static int DeduplicateGemStates(ICoreAPI api)
    {
        int removed = 0;
        foreach (string path in new[]
                 {
                     "canjewelry:itemtypes/resource/gem-rough.json",
                     "canjewelry:itemtypes/resource/gem-cut.json"
                 })
        {
            if (!TryReadObject(api, path, out IAsset asset, out JObject root)) continue;
            if (root.SelectToken("variantgroups[2].states") is not JArray states) continue;

            int removedFromAsset = 0;
            HashSet<string> seen = new(StringComparer.Ordinal);
            for (int index = 0; index < states.Count; index++)
            {
                string? code = states[index]?.ToString();
                if (code != null && seen.Add(code)) continue;
                states.RemoveAt(index--);
                removed++;
                removedFromAsset++;
            }
            if (removedFromAsset > 0) SaveAsset(asset, root);
        }
        return removed;
    }

    private static int RemoveSharedGemsFromBaseRecipes(ICoreAPI api)
    {
        int removed = 0;
        foreach (string recipe in new[] { "round_cutting.json", "pear_cutting.json", "baguette_cutting.json" })
        {
            string path = $"canjewelry:recipes/gemcutting/{recipe}";
            if (!TryReadArray(api, path, out IAsset asset, out JArray recipes)) continue;
            int removedFromAsset = 0;

            foreach (JObject entry in recipes.OfType<JObject>())
            {
                if (entry.SelectToken("ingredient.allowedVariants") is not JArray variants) continue;
                for (int index = 0; index < variants.Count; index++)
                {
                    string? code = variants[index]?.ToString();
                    if (code == null || !GeoAddonsSharedGems.Contains(code)) continue;
                    variants.RemoveAt(index--);
                    removed++;
                    removedFromAsset++;
                }
            }
            if (removedFromAsset > 0) SaveAsset(asset, recipes);
        }
        return removed;
    }

    private static int DisableUnloadedGeoAddonsGridBridges(ICoreAPI api)
    {
        int disabled = 0;
        foreach (string path in new[]
                 {
                     "canjewelry:recipes/grid/can-geology-gems-to-new.json",
                     "canjewelry:recipes/grid/can-new-gems-to-geology.json"
                 })
        {
            IAsset? asset = api.Assets.TryGet(new AssetLocation(path));
            if (asset == null) continue;
            asset.Data = Encoding.UTF8.GetBytes("[]");
            disabled++;
        }
        return disabled;
    }

    private static bool TryReadObject(ICoreAPI api, string path, out IAsset asset, out JObject root)
    {
        asset = null!;
        root = null!;
        IAsset? found = api.Assets.TryGet(new AssetLocation(path));
        if (found == null) return false;
        try { root = JObject.Parse(found.ToText()); }
        catch { return false; }
        asset = found;
        return true;
    }

    private static bool TryReadArray(ICoreAPI api, string path, out IAsset asset, out JArray root)
    {
        asset = null!;
        root = null!;
        IAsset? found = api.Assets.TryGet(new AssetLocation(path));
        if (found == null) return false;
        try { root = JArray.Parse(found.ToText()); }
        catch { return false; }
        asset = found;
        return true;
    }

    private static void SaveAsset(IAsset asset, JToken root) =>
        asset.Data = Encoding.UTF8.GetBytes(root.ToString());

    private sealed record GemDefinition(string Buff, bool ProvidedByCanBase, bool ProvidedByCanGeoAddons);
}
