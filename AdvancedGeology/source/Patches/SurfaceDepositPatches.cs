using HarmonyLib;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;
using Vintagestory.ServerMods;

namespace AdvancedGeology.Patches;

/// <summary>Prevents forest-floor generation from replacing soil deposits.</summary>
[HarmonyPatch(typeof(ForestFloorSystem), "CheckAndReplaceForestFloor")]
public static class Patch_ForestFloorSystem_CheckAndReplaceForestFloor
{
    public static bool Prefix(IBlockAccessor ___api, BlockPos pos)
    {
        return !ShouldPreserveSurfaceBlock(___api.GetBlock(pos));
    }

    public static bool ShouldPreserveSurfaceBlock(Block block)
    {
        return block is BlockSoilDeposit;
    }
}
