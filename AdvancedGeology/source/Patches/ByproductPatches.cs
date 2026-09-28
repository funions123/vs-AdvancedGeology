using System.Collections.Generic;
using AdvancedGeology.Byproducts;
using HarmonyLib;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace AdvancedGeology.Patches;

/// <summary>Installs the item and block lifecycle hooks for generic byproduct compositions.</summary>
public static class ByproductPatches
{
    public static void Apply(Harmony harmony)
    {
        harmony.CreateClassProcessor(typeof(Patch_Block_GetDrops)).Patch();
        harmony.CreateClassProcessor(typeof(Patch_Block_OnBlockRemoved)).Patch();
        harmony.CreateClassProcessor(typeof(Patch_Block_OnBlockPlaced)).Patch();
        harmony.CreateClassProcessor(typeof(Patch_CollectibleObject_GetMergableQuantity)).Patch();
        harmony.CreateClassProcessor(typeof(Patch_CollectibleObject_TryMergeStacks)).Patch();
        harmony.CreateClassProcessor(typeof(Patch_CollectibleObject_DoSmelt)).Patch();
    }
}

/// <summary>
/// Copies contained trace units to ore drops while the owning block and its deposit
/// provenance are still present. Unconfigured and old ore drops remain barren.
/// </summary>
[HarmonyPatch(typeof(Block), nameof(Block.GetDrops))]
public static class Patch_Block_GetDrops
{
    public static void Postfix(
        Block __instance,
        IWorldAccessor world,
        BlockPos pos,
        ItemStack[]? __result)
    {
        if (world.Side != EnumAppSide.Server || __result == null || pos == null) return;

        foreach (ItemStack stack in __result)
        {
            if (stack?.Collectible is ItemOre
                && ByproductSystem.TryGetComposition(world, pos, __instance.BlockId, stack, out IReadOnlyDictionary<string, double> composition))
            {
                ByproductComposition.Write(stack, composition);
            }
        }
    }
}

/// <summary>
/// Clears provenance when the owning block lifetime ends. Vintage Story normally invokes this
/// after the replacement is in the chunk; checking that the old id is gone avoids deleting a live
/// record if the method is called directly at the wrong time. IBlockAccessor.ExchangeBlock bypasses
/// both lifecycle callbacks, so callers using ExchangeBlock must explicitly call
/// <see cref="ByproductSystem.Remove"/>.
/// </summary>
[HarmonyPatch(typeof(Block), nameof(Block.OnBlockRemoved))]
public static class Patch_Block_OnBlockRemoved
{
    public static void Postfix(Block __instance, IWorldAccessor world, BlockPos pos)
    {
        if (world.Side != EnumAppSide.Server || pos == null) return;
        if (world.BlockAccessor.GetBlockId(pos) != __instance.BlockId)
        {
            ByproductSystem.Remove(world, pos);
        }
    }
}


/// <summary>
/// Clears stale provenance only for an explicit item-stack placement whose block is still live.
/// Null-stack placements include worldgen callbacks and are intentionally left to the worldgen
/// RecordPlacement/ClearPlacement contract, so natural ore provenance is not erased by hook order.
/// ExchangeBlock bypasses this callback and must call <see cref="ByproductSystem.Remove"/> itself.
/// </summary>
[HarmonyPatch(typeof(Block), nameof(Block.OnBlockPlaced))]
public static class Patch_Block_OnBlockPlaced
{
    public static void Postfix(
        Block __instance,
        IWorldAccessor world,
        BlockPos blockPos,
        ItemStack? byItemStack)
    {
        if (world.Side != EnumAppSide.Server || blockPos == null || byItemStack == null) return;
        if (world.BlockAccessor.GetBlockId(blockPos) == __instance.BlockId)
        {
            ByproductSystem.Remove(world, blockPos);
        }
    }
}

/// <summary>
/// Prevents untagged ore from silently diluting ore carrying trace elements. Legacy silver
/// is migrated by Read before merge eligibility is checked; two populated stacks still mix.
/// </summary>
[HarmonyPatch(typeof(CollectibleObject), nameof(CollectibleObject.GetMergableQuantity))]
public static class Patch_CollectibleObject_GetMergableQuantity
{
    public static bool Prefix(ItemStack sinkStack, ItemStack sourceStack, ref int __result)
    {
        if (sinkStack?.Collectible is not ItemOre || sourceStack?.Collectible is not ItemOre) return true;
        bool sinkHasTrace = ByproductComposition.Read(sinkStack).Count > 0;
        bool sourceHasTrace = ByproductComposition.Read(sourceStack).Count > 0;
        if (sinkHasTrace == sourceHasTrace) return true;
        __result = 0;
        return false;
    }
}

/// <summary>Conserves every trace element when all or part of one stack moves into another.</summary>
[HarmonyPatch(typeof(CollectibleObject), nameof(CollectibleObject.TryMergeStacks))]
public static class Patch_CollectibleObject_TryMergeStacks
{
    public readonly record struct MergeState(
        IReadOnlyDictionary<string, double> Sink,
        IReadOnlyDictionary<string, double> Source,
        int SinkCount);

    public static void Prefix(ItemStackMergeOperation op, out MergeState? __state)
    {
        __state = null;
        ItemStack? sink = op?.SinkSlot?.Itemstack;
        ItemStack? source = op?.SourceSlot?.Itemstack;
        if (sink == null || source == null) return;

        IReadOnlyDictionary<string, double> sinkComposition = ByproductComposition.Read(sink);
        IReadOnlyDictionary<string, double> sourceComposition = ByproductComposition.Read(source);
        if (sinkComposition.Count == 0 && sourceComposition.Count == 0) return;

        __state = new MergeState(sinkComposition, sourceComposition, sink.StackSize);
    }

    public static void Postfix(ItemStackMergeOperation op, MergeState? __state)
    {
        if (__state is not { } state || op?.SinkSlot?.Itemstack is not ItemStack sink) return;
        int moved = sink.StackSize - state.SinkCount;
        if (moved <= 0) return;

        ByproductComposition.Write(
            sink,
            ByproductComposition.Merge(state.Sink, state.SinkCount, state.Source, moved));
    }
}

/// <summary>
/// Conserves each trace element across Vintage Story's single-input DoSmelt contract. The prefix
/// snapshots input and output counts; the postfix measures exactly how many input units were
/// consumed and output units produced. Input concentration is weighted by consumed units, not by
/// produced units, so recipes such as two nuggets to one ingot neither lose nor duplicate material.
/// A changed/replaced input or output collectible is treated as an unknown process and left alone.
/// Snapshotting also prevents DoSmelt's internal TryMergeStacks call from double-counting material.
/// </summary>
[HarmonyPatch(typeof(CollectibleObject), nameof(CollectibleObject.DoSmelt))]
public static class Patch_CollectibleObject_DoSmelt
{
    public readonly record struct SmeltState(
        IReadOnlyDictionary<string, double> Input,
        IReadOnlyDictionary<string, double> Output,
        CollectibleObject InputCollectible,
        int InputCount,
        CollectibleObject ExpectedOutputCollectible,
        int OutputCount);

    public static void Prefix(
        IWorldAccessor world,
        ItemSlot inputSlot,
        ItemSlot outputSlot,
        out SmeltState? __state)
    {
        __state = null;
        ItemStack? input = inputSlot?.Itemstack;
        if (input == null) return;

        CombustibleProperties? props = input.Collectible.GetCombustibleProperties(world, input, null);
        ItemStack? expectedOutputStack = props?.SmeltedStack?.ResolvedItemstack;
        if (expectedOutputStack == null) return;
        if (expectedOutputStack.Collectible == null && !expectedOutputStack.ResolveBlockOrItem(world)) return;
        CollectibleObject expectedOutput = expectedOutputStack.Collectible!;

        ItemStack? output = outputSlot?.Itemstack;
        if (output != null && !ReferenceEquals(output.Collectible, expectedOutput)) return;
        IReadOnlyDictionary<string, double> inputComposition = ByproductComposition.Read(input);
        IReadOnlyDictionary<string, double> outputComposition = ByproductComposition.Read(output);
        if (inputComposition.Count == 0 && outputComposition.Count == 0) return;

        __state = new SmeltState(
            inputComposition,
            outputComposition,
            input.Collectible,
            input.StackSize,
            expectedOutput,
            output?.StackSize ?? 0);
    }

    public static void Postfix(ItemSlot inputSlot, ItemSlot outputSlot, SmeltState? __state)
    {
        if (__state is not { } state || outputSlot?.Itemstack is not ItemStack output) return;
        if (!ReferenceEquals(state.ExpectedOutputCollectible, output.Collectible)) return;

        int produced = output.StackSize - state.OutputCount;
        if (produced <= 0) return;

        int remainingInput;
        if (inputSlot?.Itemstack == null)
        {
            remainingInput = 0;
        }
        else
        {
            if (!ReferenceEquals(inputSlot.Itemstack.Collectible, state.InputCollectible)) return;
            remainingInput = inputSlot.Itemstack.StackSize;
        }

        int consumed = state.InputCount - remainingInput;
        if (consumed <= 0) return;

        IReadOnlyDictionary<string, double> composition;
        int outputCount = state.OutputCount + produced;
        if (consumed == produced)
        {
            composition = ByproductComposition.Merge(
                state.Output,
                state.OutputCount,
                state.Input,
                consumed);
        }
        else
        {
            composition = ByproductComposition.MergeToCount(
                state.Output,
                state.OutputCount,
                state.Input,
                consumed,
                outputCount);
        }
        ByproductComposition.Write(output, composition);
    }
}
