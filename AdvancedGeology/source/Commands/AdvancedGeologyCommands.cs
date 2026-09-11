using System.Collections.Generic;
using AdvancedGeology.WorldGen.ProceduralDeposits;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace AdvancedGeology;

/// <summary>
/// Server test commands. `/ag delrock &lt;rocktype&gt;` clears one natural rock variant from the
/// caller's chunk and the eight chunks around it, exposing buried deposit geometry in place.
/// </summary>
internal static class AdvancedGeologyCommands
{
    public static void Register(ICoreServerAPI api)
    {
        api.ChatCommands
            .GetOrCreate("ag")
            .RequiresPrivilege(Privilege.controlserver)
            .BeginSubCommand("delrock")
            .WithDescription("Deletes every block of one rock variant within a one chunk radius, e.g. /ag delrock sandstone")
            .WithArgs(api.ChatCommands.Parsers.Word("rocktype"))
            .HandleWith(args => DeleteRock(api, args))
            .EndSubCommand();
    }


    private static TextCommandResult DeleteRock(ICoreServerAPI api, TextCommandCallingArgs args)
    {
        string requested = ((string)args[0]).Trim().ToLowerInvariant();
        if (requested.Length == 0) return TextCommandResult.Error("Specify a rock variant, e.g. /ag delrock sandstone");

        var targets = new HashSet<int>();
        foreach (Block? block in api.World.Blocks)
        {
            if (block?.Code == null) continue;
            if (!CompiledProceduralDeposit.TryGetRockVariant(block, out string variant)) continue;
            if (variant.Equals(requested, System.StringComparison.OrdinalIgnoreCase)) targets.Add(block.BlockId);
        }

        if (targets.Count == 0) return TextCommandResult.Error($"No rock variant named '{requested}' is registered");

        int chunkSize = GlobalConstants.ChunkSize;
        BlockPos center = (args.Caller.Pos ?? api.World.DefaultSpawnPosition.XYZ).AsBlockPos;
        int minX = (center.X / chunkSize - 1) * chunkSize;
        int minZ = (center.Z / chunkSize - 1) * chunkSize;
        int maxX = minX + 3 * chunkSize - 1;
        int maxZ = minZ + 3 * chunkSize - 1;
        int maxY = api.World.BlockAccessor.MapSizeY - 1;

        IBulkBlockAccessor accessor = api.World.BulkBlockAccessor;
        var position = new BlockPos(minX, 1, minZ, center.dimension);
        int removed = 0;

        for (int x = minX; x <= maxX; x++)
        {
            position.X = x;
            for (int z = minZ; z <= maxZ; z++)
            {
                position.Z = z;
                for (int y = 1; y <= maxY; y++)
                {
                    position.Y = y;
                    if (!targets.Contains(accessor.GetBlockId(position))) continue;

                    accessor.SetBlock(0, position);
                    removed++;
                }
            }
        }

        accessor.Commit();
        return TextCommandResult.Success(
            $"Removed {removed} {requested} block(s) from chunks X {minX / chunkSize}-{maxX / chunkSize}, Z {minZ / chunkSize}-{maxZ / chunkSize}");
    }
}
