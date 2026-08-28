using System.Reflection;
using HarmonyLib;
using Vintagestory.API.Common;
using Vintagestory.API.Server;

namespace AdvancedGeology.Patches;

/// <summary>
/// Leaves CAN Jewelry's cutting, grinding, buff, and socket systems intact while making
/// AdvancedGeology worldgen the sole source of newly obtained rough gems.
/// </summary>
internal static class CanJewelryAcquisitionPatch
{
    private const string CanSystemTypeName = "canjewelry.src.canjewelry";
    private const string CanPanTypeName = "canjewelry.src.blocks.CANBlockPan";
    private static int panSuppressionLogged;

    public static void Apply(Harmony harmony, ICoreAPI api)
    {
        Type? canSystemType = AccessTools.TypeByName(CanSystemTypeName);
        MethodInfo? startServerSide = canSystemType == null
            ? null
            : AccessTools.Method(canSystemType, nameof(ModSystem.StartServerSide), [typeof(ICoreServerAPI)]);

        Type? canPanType = AccessTools.TypeByName(CanPanTypeName);
        MethodInfo? panOnLoaded = canPanType == null
            ? null
            : AccessTools.Method(canPanType, nameof(Block.OnLoaded), [typeof(ICoreAPI)]);

        if (startServerSide == null || panOnLoaded == null)
        {
            api.Logger.Warning(
                "[AdvancedGeology] CAN Jewelry detected, but its gem acquisition hooks were not found; CAN gem drops remain enabled");
            return;
        }

        harmony.Patch(
            startServerSide,
            prefix: new HarmonyMethod(typeof(CanJewelryAcquisitionPatch), nameof(BeforeCanServerStart)),
            finalizer: new HarmonyMethod(typeof(CanJewelryAcquisitionPatch), nameof(AfterCanServerStart)));
        harmony.Patch(
            panOnLoaded,
            postfix: new HarmonyMethod(typeof(CanJewelryAcquisitionPatch), nameof(AfterCanPanLoaded)));

        api.Logger.Notification(
            "[AdvancedGeology] CAN Jewelry compat: disabled block-break and panning gem acquisition");
    }

    private static void BeforeCanServerStart(MethodBase __originalMethod, ICoreServerAPI api, out DropTableState? __state)
    {
        __state = null;

        FieldInfo? configField = AccessTools.Field(__originalMethod.DeclaringType, "config");
        object? config = configField?.GetValue(null);
        FieldInfo? dropsField = config == null ? null : AccessTools.Field(config.GetType(), "gems_drops_table");
        object? drops = dropsField?.GetValue(config);
        if (config == null || dropsField == null || drops == null) return;

        object? emptyDrops = Activator.CreateInstance(drops.GetType());
        if (emptyDrops == null) return;

        dropsField.SetValue(config, emptyDrops);
        int sourceCount = (int?)AccessTools.Property(drops.GetType(), "Count")?.GetValue(drops) ?? 0;
        __state = new DropTableState(config, dropsField, drops, api, sourceCount);
    }

    private static Exception? AfterCanServerStart(Exception? __exception, DropTableState? __state)
    {
        __state?.RestoreAndLog();
        return __exception;
    }

    private static void AfterCanPanLoaded(object __instance, ICoreAPI api)
    {
        FieldInfo? dropsField = AccessTools.Field(__instance.GetType(), "dropsBySourceMat");
        object? drops = dropsField?.GetValue(__instance);
        if (dropsField != null && drops != null)
        {
            object? emptyDrops = Activator.CreateInstance(drops.GetType());
            if (emptyDrops != null) dropsField.SetValue(__instance, emptyDrops);

            if (api.Side == EnumAppSide.Server && sourceCount(drops) > 0 &&
                System.Threading.Interlocked.Exchange(ref panSuppressionLogged, 1) == 0)
            {
                api.Logger.Notification(
                    "[AdvancedGeology] CAN Jewelry compat: suppressed {0} configured panning source patterns",
                    sourceCount(drops));
            }
        }

        // The client builds its interaction list before this postfix. Remove the now-invalid
        // add-material and pan prompts along with the effective drop table.
        FieldInfo? interactionsField = AccessTools.Field(__instance.GetType(), "interactions");
        Type? interactionType = interactionsField?.FieldType.GetElementType();
        if (interactionsField != null && interactionType != null)
        {
            interactionsField.SetValue(__instance, Array.CreateInstance(interactionType, 0));
        }
    }

    private static int sourceCount(object table) =>
        (int?)AccessTools.Property(table.GetType(), "Count")?.GetValue(table) ?? 0;

    private sealed class DropTableState(
        object config,
        FieldInfo dropsField,
        object drops,
        ICoreServerAPI api,
        int sourceCount)
    {
        public void RestoreAndLog()
        {
            dropsField.SetValue(config, drops);
            api.Logger.Notification(
                "[AdvancedGeology] CAN Jewelry compat: suppressed block-break gem injection from {0} configured source patterns",
                sourceCount);
        }
    }
}
