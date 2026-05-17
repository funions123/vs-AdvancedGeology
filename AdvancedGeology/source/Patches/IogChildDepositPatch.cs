using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.ServerMods;

namespace AdvancedGeology.Patches;

/// <summary>
/// Harmony patches on IOG's TiltedDiscDepositGenerator to populate the <c>subDepositsToPlace</c>
/// dictionary vanilla uses to spawn child deposits. IOG's generator ignores it, so without these
/// patches childDeposits injected into IOG variants never generate.
/// </summary>
public static class IogChildDepositPatch
{
    private static FieldInfo? _variantField;

    [ThreadStatic] private static List<BlockPos>? _trackedPositions;
    [ThreadStatic] private static bool _isTracking;

    public static void Apply(ICoreAPI api)
    {
        var harmony = new Harmony("advancedgeology.iogchilddeposits");

        Type? iogGenType = AccessTools.TypeByName(
            "InterestingOreGen.Generators.TiltedDiscDepositGenerator");
        if (iogGenType == null)
        {
            api.Logger.Warning("[AdvancedGeology] Could not find IOG TiltedDiscDepositGenerator type for Harmony patch");
            return;
        }

        _variantField = AccessTools.Field(typeof(DepositGeneratorBase), "variant");

        MethodInfo? genDeposit = AccessTools.Method(iogGenType, "GenDeposit");
        if (genDeposit == null)
        {
            api.Logger.Warning("[AdvancedGeology] Could not find IOG GenDeposit method for Harmony patch");
            return;
        }

        MethodInfo? tryPlaceMainOre = AccessTools.Method(iogGenType, "TryPlaceMainOre");
        if (tryPlaceMainOre == null)
        {
            api.Logger.Warning("[AdvancedGeology] Could not find IOG TryPlaceMainOre method for Harmony patch");
            return;
        }

        harmony.Patch(genDeposit,
            prefix: new HarmonyMethod(typeof(IogChildDepositPatch), nameof(GenDeposit_Prefix)),
            postfix: new HarmonyMethod(typeof(IogChildDepositPatch), nameof(GenDeposit_Postfix)));

        harmony.Patch(tryPlaceMainOre,
            postfix: new HarmonyMethod(typeof(IogChildDepositPatch), nameof(TryPlaceMainOre_Postfix)));

        api.Logger.Notification("[AdvancedGeology] Applied IOG child deposit Harmony patches");
    }

    /// <summary>
    /// Lazy-inits child generators and enables position tracking.
    /// </summary>
    public static void GenDeposit_Prefix(DepositGeneratorBase __instance)
    {
        var variant = (DepositVariant?)_variantField?.GetValue(__instance);
        if (variant?.ChildDeposits == null || variant.ChildDeposits.Length == 0) return;

        foreach (var child in variant.ChildDeposits)
        {
            if (child.GeneratorInst == null)
            {
                InitChildGenerator(__instance, variant, child);
            }
        }

        _isTracking = true;
        _trackedPositions ??= new List<BlockPos>();
        _trackedPositions.Clear();
    }

    /// <summary>
    /// Records each placed main-ore position while tracking is active.
    /// </summary>
    public static void TryPlaceMainOre_Postfix(BlockPos pos)
    {
        if (_isTracking)
        {
            _trackedPositions!.Add(pos.Copy());
        }
    }

    /// <summary>
    /// Rolls probability per tracked position and populates subDepositsToPlace.
    /// </summary>
    public static void GenDeposit_Postfix(
        DepositGeneratorBase __instance,
        ref Dictionary<BlockPos, DepositVariant> subDepositsToPlace)
    {
        if (!_isTracking) return;
        _isTracking = false;

        if (_trackedPositions == null || _trackedPositions.Count == 0) return;

        var variant = (DepositVariant?)_variantField?.GetValue(__instance);
        if (variant?.ChildDeposits == null) return;

        LCGRandom rand = __instance.DepositRand;
        float invChunkArea = 1f / (GlobalConstants.ChunkSize * GlobalConstants.ChunkSize);

        foreach (var pos in _trackedPositions)
        {
            for (int i = 0; i < variant.ChildDeposits.Length; i++)
            {
                float probability = variant.ChildDeposits[i].TriesPerChunk * invChunkArea;
                if (rand.NextFloat() < probability)
                {
                    subDepositsToPlace[pos.Copy()] = variant.ChildDeposits[i];
                }
            }
        }

        _trackedPositions.Clear();
    }

    /// <summary>
    /// Initializes a child deposit generator, replicating vanilla's DiscGenerator.Init() pattern.
    /// Creates the ChildDepositGenerator and calls ResolveAdd for each parent ore block.
    /// </summary>
    private static void InitChildGenerator(
        DepositGeneratorBase parent, DepositVariant parentVariant, DepositVariant child)
    {
        ICoreServerAPI api = parent.Api;

        child.InitWithoutGenerator(api);
        child.GeneratorInst = DepositGeneratorRegistry.CreateGenerator(
            child.Generator, child.Attributes,
            api, child, parent.DepositRand, parent.DistortNoiseGen);

        if (child.GeneratorInst is not ChildDepositGenerator childGen) return;

        // Parse parent variant's inblock/placeblock from attributes to resolve ore blocks
        JsonObject? inblockObj = parentVariant.Attributes?["inblock"];
        JsonObject? placeblockObj = parentVariant.Attributes?["placeblock"];
        if (inblockObj == null || placeblockObj == null) return;

        string inblockName = inblockObj["name"].AsString();
        string placeblockCode = placeblockObj["code"].AsString();   
        string[]? allowedRocks = inblockObj["allowedVariants"]?.AsArray<string>();
        if (allowedRocks == null || allowedRocks.Length == 0) return;

        foreach (string rock in allowedRocks)
        {
            string resolvedPattern = placeblockCode.Replace("{" + inblockName + "}", rock);
            Block[] parentOreBlocks = api.World.SearchBlocks(new AssetLocation(resolvedPattern));

            foreach (Block oreBlock in parentOreBlocks)
            {
                childGen.ResolveAdd(oreBlock, inblockName, rock);
            }
        }
    }
}
