using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Util;
using Vintagestory.ServerMods;

namespace AdvancedGeology.WorldGen;

/// <summary>
/// Resolved deposit block holding the block instances to place.
/// </summary>
public class LayeredResolvedDepositBlock
{
    public Block[] Blocks = Array.Empty<Block>();
}

/// <summary>
/// Configuration for a deposit block layer.
/// </summary>
[JsonObject(MemberSerialization.OptIn)]
public class LayeredDepositBlock
{
    [JsonProperty]
    public AssetLocation Code;

    [JsonProperty]
    public string Name;

    [JsonProperty]
    public string[] AllowedVariants;

    [JsonProperty]
    public Dictionary<AssetLocation, string[]> AllowedVariantsByInBlock;

    /// <summary>
    /// Resolves the block code pattern to block instances (reimplements the VS internal DepositBlock.Resolve).
    /// </summary>
    public LayeredResolvedDepositBlock Resolve(string fileForLogging, ICoreServerAPI api, Block inblock, string key, string value)
    {
        AssetLocation blockLoc = Code.Clone();
        blockLoc.Path = blockLoc.Path.Replace("{" + key + "}", value);

        Block[] foundBlocks = api.World.SearchBlocks(blockLoc);

        if (foundBlocks.Length == 0)
        {
            api.World.Logger.Warning("LayeredDeposit {0}: No block with code/wildcard '{1}' was found (unresolved code: {2})", fileForLogging, blockLoc, Code);
        }

        if (AllowedVariants != null)
        {
            List<Block> filteredBlocks = new List<Block>();
            for (int i = 0; i < foundBlocks.Length; i++)
            {
                if (WildcardUtil.Match(blockLoc, foundBlocks[i].Code, AllowedVariants))
                {
                    filteredBlocks.Add(foundBlocks[i]);
                }
            }

            if (filteredBlocks.Count == 0)
            {
                api.World.Logger.Warning("LayeredDeposit {0}: AllowedVariants for {1} does not match any block!", fileForLogging, blockLoc);
            }

            foundBlocks = filteredBlocks.ToArray();
        }

        if (AllowedVariantsByInBlock != null)
        {
            if (AllowedVariantsByInBlock.TryGetValue(inblock.Code, out string[] allowedVariants))
            {
                List<Block> filteredBlocks = new List<Block>();
                for (int i = 0; i < foundBlocks.Length; i++)
                {
                    string wildcardValue = WildcardUtil.GetWildcardValue(blockLoc, foundBlocks[i].Code);
                    if (allowedVariants.Contains(wildcardValue))
                    {
                        filteredBlocks.Add(foundBlocks[i]);
                    }
                }

                if (filteredBlocks.Count == 0)
                {
                    api.World.Logger.Warning("LayeredDeposit {0}: AllowedVariantsByInBlock for {1} does not match any block!", fileForLogging, blockLoc);
                }

                foundBlocks = filteredBlocks.ToArray();
            }
            else
            {
                foundBlocks = Array.Empty<Block>();
            }
        }

        return new LayeredResolvedDepositBlock()
        {
            Blocks = foundBlocks
        };
    }
}

/// <summary>Generates rock-anchored deposits with optional soil layers.</summary>
[JsonObject(MemberSerialization.OptIn)]
public class LayeredSurfaceDepositGenerator : DepositGeneratorBase
{
    // Soil column

    /// <summary>Soil blocks traversed by the deposit column.</summary>
    [JsonProperty]
    public LayeredDepositBlock SoilInBlock;

    /// <summary>Optional replacement for soil above the deposit.</summary>
    [JsonProperty]
    public LayeredDepositBlock TopLayerBlock;

    // Fills soil above the middle layer.

    // Middle layer

    /// <summary>Optional soil layer above rock.</summary>
    [JsonProperty]
    public LayeredDepositBlock MiddleLayerBlock;

    /// <summary>Middle-layer thickness.</summary>
    [JsonProperty]
    public int MiddleLayerThickness = 0;

    // Bottom layer

    /// <summary>
    /// The block to check for in the rock layer (replaced with BottomLayerBlock).
    /// </summary>
    [JsonProperty]
    public LayeredDepositBlock RockInBlock;

    /// <summary>
    /// The block to place in the rock layer.
    /// </summary>
    [JsonProperty]
    public LayeredDepositBlock BottomLayerBlock;

    /// <summary>
    /// Fewest bottom-block layers to place in rock.
    /// </summary>
    [JsonProperty]
    public int BottomLayerThickness = 1;

    /// <summary>Maximum rock-layer thickness.</summary>
    [JsonProperty]
    public int BottomLayerThicknessMax = 0;

    /// <summary>Requires valid rock directly below the soil.</summary>
    [JsonProperty]
    public bool RequireImmediateRock = true;

    // General settings

    /// <summary>
    /// Radius in blocks, capped at 64.
    /// </summary>
    [JsonProperty]
    public NatFloat Radius;

    /// <summary>Maximum permitted surface relief.</summary>
    [JsonProperty]
    public int MaxYRoughness = 999;

    /// <summary>Uses worldgen placement for the surface block.</summary>
    [JsonProperty]
    public bool WithLastLayerBlockCallback;

    protected int worldheight;
    protected int radiusX, radiusZ;

    // Resolved blocks by inblock ID
    protected Dictionary<int, LayeredResolvedDepositBlock> topLayerBlockByInBlockId = new Dictionary<int, LayeredResolvedDepositBlock>();
    protected Dictionary<int, LayeredResolvedDepositBlock> middleLayerBlockByInBlockId = new Dictionary<int, LayeredResolvedDepositBlock>();
    protected Dictionary<int, LayeredResolvedDepositBlock> bottomLayerBlockByInBlockId = new Dictionary<int, LayeredResolvedDepositBlock>();

    // Soil blocks traversed without replacement.
    protected HashSet<int> soilBlockIds = new HashSet<int>();

    // Valid rock hosts.
    protected HashSet<int> validRockBlockIds = new HashSet<int>();

    public LayeredSurfaceDepositGenerator(ICoreServerAPI api, DepositVariant variant, LCGRandom depositRand, NormalizedSimplexNoise noiseGen)
        : base(api, variant, depositRand, noiseGen)
    {
        worldheight = api.World.BlockAccessor.MapSizeY;
    }

    public override void Init()
    {
        if (Radius == null)
        {
            Api.Server.LogWarning("LayeredSurfaceDeposit {0} has no radius property defined. Defaulting to uniform radius 10", variant.fromFile);
            Radius = NatFloat.createUniform(10, 0);
        }

        if (variant.Climate != null && Radius.avg + Radius.var >= 32)
        {
            Api.Server.LogWarning("LayeredSurfaceDeposit {0} has Climate defined and radius > 32 blocks - this is not supported. Defaulting to uniform radius 10", variant.fromFile);
            Radius = NatFloat.createUniform(10, 0);
        }

        // Resolve traversable soil and optional replacements.
        if (SoilInBlock != null)
        {
            Block[] soilBlocks = Api.World.SearchBlocks(SoilInBlock.Code);
            foreach (var block in soilBlocks)
            {
                if (SoilInBlock.AllowedVariants != null && !WildcardUtil.Match(SoilInBlock.Code, block.Code, SoilInBlock.AllowedVariants)) continue;
                if (SoilInBlock.AllowedVariantsByInBlock != null && !SoilInBlock.AllowedVariantsByInBlock.ContainsKey(block.Code)) continue;

                soilBlockIds.Add(block.BlockId);

                if (TopLayerBlock == null) continue;

                string key = SoilInBlock.Name;
                string value = WildcardUtil.GetWildcardValue(SoilInBlock.Code, block.Code);

                topLayerBlockByInBlockId[block.BlockId] = TopLayerBlock.Resolve(variant.fromFile, Api, block, key, value);
            }
        }

        // Resolve rock hosts and layer blocks.
        if (RockInBlock != null)
        {
            Block[] rockBlocks = Api.World.SearchBlocks(RockInBlock.Code);
            foreach (var block in rockBlocks)
            {
                if (RockInBlock.AllowedVariants != null && !WildcardUtil.Match(RockInBlock.Code, block.Code, RockInBlock.AllowedVariants)) continue;
                if (RockInBlock.AllowedVariantsByInBlock != null && !RockInBlock.AllowedVariantsByInBlock.ContainsKey(block.Code)) continue;

                string key = RockInBlock.Name;
                string value = WildcardUtil.GetWildcardValue(RockInBlock.Code, block.Code);

                validRockBlockIds.Add(block.BlockId);

                if (BottomLayerBlock != null)
                {
                    bottomLayerBlockByInBlockId[block.BlockId] = BottomLayerBlock.Resolve(variant.fromFile, Api, block, key, value);
                }

                if (MiddleLayerBlock != null && MiddleLayerThickness > 0)
                {
                    middleLayerBlockByInBlockId[block.BlockId] = MiddleLayerBlock.Resolve(variant.fromFile, Api, block, key, value);
                }
            }
        }
    }

    public override void GenDeposit(IBlockAccessor blockAccessor, IServerChunk[] chunks, int chunkX, int chunkZ, BlockPos depoCenterPos, ref Dictionary<BlockPos, DepositVariant> subDepositsToPlace)
    {
        int radius = Math.Min(64, (int)Radius.nextFloat(1, DepositRand));
        if (radius <= 0) return;

        // Deform the circle slightly (+/- 25%)
        float deform = GameMath.Clamp(DepositRand.NextFloat() - 0.5f, -0.25f, 0.25f);
        radiusX = radius - (int)(radius * deform);
        radiusZ = radius + (int)(radius * deform);

        int baseX = chunkX * chunksize;
        int baseZ = chunkZ * chunksize;

        // Skip if deposit is outside this chunk
        if (depoCenterPos.X + radiusX < baseX - 6 || depoCenterPos.Z + radiusZ < baseZ - 6 ||
            depoCenterPos.X - radiusX >= baseX + chunksize + 6 || depoCenterPos.Z - radiusZ >= baseZ + chunksize + 6)
            return;

        IMapChunk heremapchunk = chunks[0].MapChunk;

        float xRadSqInv = 1f / (radiusX * radiusX);
        float zRadSqInv = 1f / (radiusZ * radiusZ);

        int lx, lz;
        int distx, distz;

        // Clamp to chunk boundaries
        int minx = GameMath.Clamp(depoCenterPos.X - radiusX, baseX, baseX + chunksize);
        int maxx = GameMath.Clamp(depoCenterPos.X + radiusX, baseX, baseX + chunksize);
        int minz = GameMath.Clamp(depoCenterPos.Z - radiusZ, baseZ, baseZ + chunksize);
        int maxz = GameMath.Clamp(depoCenterPos.Z + radiusZ, baseZ, baseZ + chunksize);

        for (int posx = minx; posx < maxx; posx++)
        {
            lx = posx - baseX;
            distx = posx - depoCenterPos.X;
            float xSq = distx * distx * xRadSqInv;

            for (int posz = minz; posz < maxz; posz++)
            {
                lz = posz - baseZ;
                distz = posz - depoCenterPos.Z;

                // Distort the circle with noise
                double val = 1 - (radius > 3 ? DistortNoiseGen.Noise(posx / 3.0, posz / 3.0) * 0.2 : 0);
                double distanceToEdge = val - (xSq + distz * distz * zRadSqInv);
                if (distanceToEdge < 0) continue;

                int surfaceY = heremapchunk.WorldGenTerrainHeightMap[lz * chunksize + lx];
                if (surfaceY >= worldheight) continue;

                // Check Y roughness
                if (Math.Abs(depoCenterPos.Y - surfaceY) > MaxYRoughness) continue;

                // Find where rock starts and check if it's valid
                int rockStartY = -1;
                int rockBlockId = 0;
                bool foundValidRock = false;

                for (int y = surfaceY; y > 0; y--)
                {
                    int index3d = ((y % chunksize) * chunksize + lz) * chunksize + lx;
                    IChunkBlocks chunkdata = chunks[y / chunksize].Data;
                    int blockId = chunkdata.GetBlockIdUnsafe(index3d);

                    if (soilBlockIds.Contains(blockId))
                    {
                        continue;
                    }

                    // Hit rock (or something else)
                    rockStartY = y;
                    rockBlockId = blockId;

                    if (validRockBlockIds.Contains(blockId))
                    {
                        foundValidRock = true;
                    }
                    break;
                }

                if (rockStartY <= 0) continue;

                // Gate rock-anchored layers on host validity.
                bool canPlaceOnRock = !RequireImmediateRock || foundValidRock;
                bool shouldPlaceBottomLayer = canPlaceOnRock && bottomLayerBlockByInBlockId.Count > 0;
                bool shouldPlaceMiddleLayer = canPlaceOnRock && middleLayerBlockByInBlockId.Count > 0;


                // Place the variable-thickness rock layer.
                if (shouldPlaceBottomLayer)
                {
                    int bottomLayersPlaced = 0;
                    int bottomTarget = SelectBottomThickness(posx, posz, distanceToEdge, val);
                    for (int y = rockStartY; y > 0 && bottomLayersPlaced < bottomTarget; y--)
                    {
                        int index3d = ((y % chunksize) * chunksize + lz) * chunksize + lx;
                        IChunkBlocks chunkdata = chunks[y / chunksize].Data;
                        int blockId = chunkdata.GetBlockIdUnsafe(index3d);

                        if (bottomLayerBlockByInBlockId.TryGetValue(blockId, out LayeredResolvedDepositBlock resolvedBottomBlock) && resolvedBottomBlock.Blocks.Length > 0)
                        {
                            Block placeblock = resolvedBottomBlock.Blocks[0];
                            chunkdata.SetBlockUnsafe(index3d, placeblock.BlockId);
                            chunkdata.SetFluid(index3d, 0);
                            bottomLayersPlaced++;
                        }
                        else
                        {
                            break;
                        }
                    }
                }

                // Place the fixed middle layer.
                int middleLayersPlaced = 0;
                if (shouldPlaceMiddleLayer &&
                    middleLayerBlockByInBlockId.TryGetValue(rockBlockId, out LayeredResolvedDepositBlock resolvedMiddleBlock) &&
                    resolvedMiddleBlock.Blocks.Length > 0)
                {
                    Block middlePlaceBlock = resolvedMiddleBlock.Blocks[0];

                    int middleCount = Math.Min(MiddleLayerThickness, Math.Max(0, surfaceY - rockStartY - 1));

                    for (int y = rockStartY + 1; y <= rockStartY + middleCount; y++)
                    {
                        int index3d = ((y % chunksize) * chunksize + lz) * chunksize + lx;
                        IChunkBlocks chunkdata = chunks[y / chunksize].Data;
                        int blockId = chunkdata.GetBlockIdUnsafe(index3d);

                        // Stop at non-soil gaps.
                        if (!soilBlockIds.Contains(blockId)) break;

                        chunkdata.SetBlockUnsafe(index3d, middlePlaceBlock.BlockId);
                        chunkdata.SetFluid(index3d, 0);
                        middleLayersPlaced++;
                    }
                }

                // Fill remaining soil to the surface.
                for (int y = rockStartY + middleLayersPlaced + 1; y <= surfaceY; y++)
                {
                    int index3d = ((y % chunksize) * chunksize + lz) * chunksize + lx;
                    IChunkBlocks chunkdata = chunks[y / chunksize].Data;
                    int blockId = chunkdata.GetBlockIdUnsafe(index3d);

                    if (!topLayerBlockByInBlockId.TryGetValue(blockId, out LayeredResolvedDepositBlock resolvedTopBlock) || resolvedTopBlock.Blocks.Length == 0)
                    {
                        // Stop at non-soil gaps.
                        break;
                    }

                    Block placeblock = resolvedTopBlock.Blocks[0];

                    if (WithLastLayerBlockCallback && y == surfaceY)
                    {
                        // Let worldgen apply surface grass.
                        BlockPos targetPos = new BlockPos(posx, y, posz);
                        placeblock.TryPlaceBlockForWorldGen(blockAccessor, targetPos, BlockFacing.UP, DepositRand);
                    }
                    else
                    {
                        chunkdata.SetBlockUnsafe(index3d, placeblock.BlockId);
                        chunkdata.SetFluid(index3d, 0);
                    }
                }
            }
        }
    }

    /// <summary>
    /// Picks how many rock layers this column replaces. Without BottomLayerThicknessMax the
    /// configured thickness is used unchanged. Otherwise the count interpolates from the maximum
    /// near the deposit centre to the minimum at the rim, with a small noise jitter so the base of
    /// the lens is irregular rather than a stepped cone.
    /// </summary>
    protected int SelectBottomThickness(int posx, int posz, double distanceToEdge, double edgeReference)
    {
        int minimum = Math.Max(1, BottomLayerThickness);
        int maximum = Math.Max(minimum, BottomLayerThicknessMax);
        if (maximum == minimum) return minimum;

        // distanceToEdge is 1-ish at the centre and 0 at the distorted rim.
        double normalized = edgeReference <= 0 ? 0 : GameMath.Clamp(distanceToEdge / edgeReference, 0, 1);
        double jitter = DistortNoiseGen.Noise(posx / 6.0, posz / 6.0) * 0.5 - 0.25;
        double scaled = minimum + (maximum - minimum) * GameMath.Clamp(normalized + jitter, 0, 1);
        return GameMath.Clamp((int)Math.Round(scaled), minimum, maximum);
    }

    public override float GetMaxRadius()
    {
        return (Radius.avg + Radius.var) * 1.3f;
    }

    public override void GetPropickReading(BlockPos pos, int oreDist, int[] blockColumn, out double ppt, out double totalFactor)
    {
        // Blank implementation
        ppt = 0;
        totalFactor = 0;
    }

    public override void GetYMinMax(BlockPos pos, out double miny, out double maxy)
    {
        // Top layer depth varies with soil thickness; 8 covers the deepest modded soil stacks.
        miny = pos.Y - 8 - MiddleLayerThickness - Math.Max(BottomLayerThickness, BottomLayerThicknessMax);
        maxy = pos.Y;
    }
}
