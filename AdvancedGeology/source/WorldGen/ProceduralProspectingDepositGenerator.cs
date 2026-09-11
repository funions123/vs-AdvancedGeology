using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.ServerMods;

namespace AdvancedGeology.WorldGen;

/// <summary>
/// Registers procedural major minerals with the native prospecting workspace without generating
/// conventional deposits. AdvancedGeology replaces the normal noise maps with displaced,
/// distorted signal fields derived from deterministic procedural feature placement.
/// </summary>
public sealed class ProceduralProspectingDepositGenerator : DepositGeneratorBase
{
    public ProceduralProspectingDepositGenerator(
        ICoreServerAPI api,
        DepositVariant variant,
        LCGRandom depositRand,
        NormalizedSimplexNoise noiseGen)
        : base(api, variant, depositRand, noiseGen)
    {
    }

    public override void GenDeposit(
        IBlockAccessor blockAccessor,
        IServerChunk[] chunks,
        int originChunkX,
        int originChunkZ,
        BlockPos pos,
        ref Dictionary<BlockPos, DepositVariant> subDepositsToPlace)
    {
    }

    public override float GetMaxRadius() => 0;

    public override void GetPropickReading(
        BlockPos pos,
        int oreDist,
        int[] blockColumn,
        out double ppt,
        out double totalFactor)
    {
        ProceduralDeposits.ProceduralDepositWorldGenSystem? system = Api.ModLoader
            .GetModSystem<ProceduralDeposits.ProceduralDepositWorldGenSystem>();
        if (system == null || !system.TryGetProspectingReading(variant.Code, pos, out double signal, out double measuredPpt))
        {
            totalFactor = 0;
            ppt = 0;
            return;
        }

        totalFactor = signal;
        ppt = measuredPpt;
    }

    public override void GetYMinMax(BlockPos pos, out double miny, out double maxy)
    {
        miny = 1;
        maxy = Api.World.BlockAccessor.MapSizeY - 2;
    }
}
