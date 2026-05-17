using System.Collections.Generic;
using AdvancedGeology.Silver;
using HarmonyLib;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace AdvancedGeology.Patches;

/// <summary>
/// Injects the position-derived silver grade into ore dropped from silver-bearing ore blocks.
/// Covers both OnBlockBroken and OnBlockExploded since both route through GetDrops.
/// </summary>
[HarmonyPatch(typeof(Block), nameof(Block.GetDrops))]
public static class Patch_Block_GetDrops
{
    public static void Postfix(Block __instance, BlockPos pos, ItemStack[] __result)
    {
        if (!SilverGradeSystem.Active || __result == null) return;
        if (__instance is not BlockOre ore || !SilverGradeSystem.IsSilverBearingOre(ore.OreName)) return;

        double grade = SilverGradeSystem.SampleGrade(pos.X, pos.Y, pos.Z);
        if (grade <= 0.0) return;

        foreach (ItemStack stack in __result)
        {
            if (stack?.Collectible is ItemOre) SilverGradeSystem.SetGrade(stack, grade);
        }
    }
}

/// <summary>
/// Carries the grade from a crushed ore stack onto the nugget item entities it spawns.
/// </summary>
[HarmonyPatch(typeof(ItemOre), nameof(ItemOre.OnContainedInteractStop))]
public static class Patch_ItemOre_Crush
{
    public class CrushState
    {
        public double Grade;
        public readonly HashSet<long> PreExisting = new();
    }

    private static IEnumerable<Entity> NuggetEntitiesAround(IWorldAccessor world, BlockSelection blockSel)
    {
        Vec3d center = blockSel.Position.ToVec3d().Add(0.5, 0.5, 0.5);
        return world.GetEntitiesAround(center, 2f, 2f, e => e is EntityItem);
    }

    public static void Prefix(ItemSlot slot, BlockEntityContainer be, BlockSelection blockSel, out CrushState __state)
    {
        __state = new CrushState();
        if (!SilverGradeSystem.Active || blockSel == null || be?.Api?.Side != EnumAppSide.Server) return;

        __state.Grade = SilverGradeSystem.GetGrade(slot?.Itemstack);
        if (__state.Grade <= 0.0) return;

        foreach (Entity e in NuggetEntitiesAround(be.Api.World, blockSel))
        {
            __state.PreExisting.Add(e.EntityId);
        }
    }

    public static void Postfix(BlockEntityContainer be, BlockSelection blockSel, CrushState __state)
    {
        if (__state.Grade <= 0.0 || blockSel == null || be?.Api?.Side != EnumAppSide.Server) return;

        foreach (Entity e in NuggetEntitiesAround(be.Api.World, blockSel))
        {
            if (__state.PreExisting.Contains(e.EntityId)) continue;

            ItemStack? stack = (e as EntityItem)?.Itemstack;
            if (stack?.Collectible?.Code?.Path is string path && path.StartsWith("nugget-"))
            {
                SilverGradeSystem.SetGrade(stack, __state.Grade);
            }
        }
    }
}

/// <summary>
/// Firepit smelt carries silver from the consumed nugget to the produced metal. Recomputed from pre-smelt state so it
/// stays correct whether the output slot was empty or merged into.
/// </summary>
[HarmonyPatch(typeof(CollectibleObject), nameof(CollectibleObject.DoSmelt))]
public static class Patch_CollectibleObject_DoSmelt
{
    public static void Prefix(ItemSlot inputSlot, ItemSlot outputSlot, out double[]? __state)
    {
        __state = null;
        if (!SilverGradeSystem.Active) return;

        ItemStack? outStack = outputSlot?.Itemstack;
        __state = new[]
        {
            SilverGradeSystem.GetGrade(inputSlot?.Itemstack),
            SilverGradeSystem.GetGrade(outStack),
            (double)(outStack?.StackSize ?? 0)
        };
    }

    public static void Postfix(ItemSlot outputSlot, double[]? __state)
    {
        if (__state == null) return;
        ItemStack? outStack = outputSlot?.Itemstack;
        if (outStack == null) return;

        int oldSize = (int)__state[2];
        int produced = outStack.StackSize - oldSize;
        if (produced <= 0) return;

        SilverGradeSystem.SetGrade(outStack, SilverGradeSystem.MergeGrade(__state[1], oldSize, __state[0], produced));
    }
}

/// <summary>
/// Weighted-average the silver grade when two stacks merge.
/// </summary>
[HarmonyPatch(typeof(CollectibleObject), nameof(CollectibleObject.TryMergeStacks))]
public static class Patch_CollectibleObject_TryMergeStacks
{
    public static void Prefix(ItemStackMergeOperation op, out double[]? __state)
    {
        __state = null;
        if (!SilverGradeSystem.Active) return;

        ItemStack? sink = op?.SinkSlot?.Itemstack;
        ItemStack? src = op?.SourceSlot?.Itemstack;
        if (sink == null || src == null) return;

        double sinkGrade = SilverGradeSystem.GetGrade(sink);
        double srcGrade = SilverGradeSystem.GetGrade(src);
        if (sinkGrade <= 0.0 && srcGrade <= 0.0) return;

        __state = new[] { sinkGrade, srcGrade, (double)sink.StackSize };
    }

    public static void Postfix(ItemStackMergeOperation op, double[]? __state)
    {
        if (__state == null) return;
        ItemStack? sink = op?.SinkSlot?.Itemstack;
        if (sink == null) return;

        int oldSize = (int)__state[2];
        int moved = sink.StackSize - oldSize;
        if (moved <= 0) return;

        SilverGradeSystem.SetGrade(sink, SilverGradeSystem.MergeGrade(__state[0], oldSize, __state[1], moved));
    }
}
