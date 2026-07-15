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

/// <summary>
/// Deposit generator for layered surface deposits. Supports up to 3 layers, generated bottom-up
/// and anchored on the rock surface: bottom (in rock), middle (a fixed thin layer in soil directly
/// above rock), and top, which repeats through all remaining soil up to the surface. Anchoring at
/// the rock keeps the column continuous under thick soil (worldgen mods generate soil 8+ deep).
/// </summary>
[JsonObject(MemberSerialization.OptIn)]
public class LayeredSurfaceDepositGenerator : DepositGeneratorBase
{
    // Top layer

    /// <summary>
    /// The block to check for in the soil layer (replaced with TopLayerBlock).
    /// </summary>
    [JsonProperty]
    public LayeredDepositBlock SoilInBlock;

    /// <summary>
    /// The block to place in the soil layer.
    /// </summary>
    [JsonProperty]
    public LayeredDepositBlock TopLayerBlock;

    // The top layer has no thickness setting; it always fills all soil above the middle layer
    // up to the surface, so thicker-than-vanilla soil extends the cap.

    // Middle layer

    /// <summary>
    /// Optional block to place as a middle layer in soil, above the rock layer.
    /// </summary>
    [JsonProperty]
    public LayeredDepositBlock MiddleLayerBlock;

    /// <summary>
    /// How many middle-block layers to place directly above the rock.
    /// </summary>
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
    /// How many bottom-block layers to place in rock.
    /// </summary>
    [JsonProperty]
    public int BottomLayerThickness = 1;

    /// <summary>
    /// If true, only generate the bottom layer when valid rock sits immediately below the soil,
    /// preventing deposits from "reaching through" invalid rock layers.
    /// </summary>
    [JsonProperty]
    public bool RequireImmediateRock = true;

    // General settings

    /// <summary>
    /// Radius in blocks, capped at 64.
    /// </summary>
    [JsonProperty]
    public NatFloat Radius;

    /// <summary>
    /// Maximum Y roughness; deposits won't appear on cliffs steeper than this.
    /// </summary>
    [JsonProperty]
    public int MaxYRoughness = 999;

    /// <summary>
    /// Whether to use the block callback for the last layer (for grass coverage).
    /// </summary>
    [JsonProperty]
    public bool WithLastLayerBlockCallback;

    protected int worldheight;
    protected int radiusX, radiusZ;

    // Resolved blocks by inblock ID
    protected Dictionary<int, LayeredResolvedDepositBlock> topLayerBlockByInBlockId = new Dictionary<int, LayeredResolvedDepositBlock>();
    protected Dictionary<int, LayeredResolvedDepositBlock> middleLayerBlockByInBlockId = new Dictionary<int, LayeredResolvedDepositBlock>();
    protected Dictionary<int, LayeredResolvedDepositBlock> bottomLayerBlockByInBlockId = new Dictionary<int, LayeredResolvedDepositBlock>();

    // Track which rock types are valid for bottom layer
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

        // Resolve top layer blocks (soil -> clay/other)
        if (SoilInBlock != null && TopLayerBlock != null)
        {
            Block[] soilBlocks = Api.World.SearchBlocks(SoilInBlock.Code);
            foreach (var block in soilBlocks)
            {
                if (SoilInBlock.AllowedVariants != null && !WildcardUtil.Match(SoilInBlock.Code, block.Code, SoilInBlock.AllowedVariants)) continue;
                if (SoilInBlock.AllowedVariantsByInBlock != null && !SoilInBlock.AllowedVariantsByInBlock.ContainsKey(block.Code)) continue;

                string key = SoilInBlock.Name;
                string value = WildcardUtil.GetWildcardValue(SoilInBlock.Code, block.Code);

                topLayerBlockByInBlockId[block.BlockId] = TopLayerBlock.Resolve(variant.fromFile, Api, block, key, value);
            }
        }

        // Resolve rock blocks and populate validRockBlockIds, bottom layer, and middle layer
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

                    if (topLayerBlockByInBlockId.ContainsKey(blockId))
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

                // Determine which layers can be placed based on rock validity
                bool canPlaceOnRock = !RequireImmediateRock || foundValidRock;
                bool shouldPlaceBottomLayer = canPlaceOnRock && bottomLayerBlockByInBlockId.Count > 0;
                bool shouldPlaceMiddleLayer = canPlaceOnRock && middleLayerBlockByInBlockId.Count > 0;

                // Layers are generated bottom-up, anchored on the rock surface, so soil columns of
                // any thickness (worldgen mods make them 8+ deep) get a continuous deposit column:
                // bottom layer in rock, middle layer directly above rock, then the top layer
                // repeated through all remaining soil up to the surface.

                // Generate bottom layer (rock -> ore) in the rock, downward from the rock surface
                if (shouldPlaceBottomLayer)
                {
                    int bottomLayersPlaced = 0;
                    for (int y = rockStartY; y > 0 && bottomLayersPlaced < BottomLayerThickness; y--)
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

                // Generate middle layer (soil -> middle block) upward from the rock surface,
                // exactly MiddleLayerThickness blocks (clamped to the available soil)
                int middleLayersPlaced = 0;
                if (shouldPlaceMiddleLayer &&
                    middleLayerBlockByInBlockId.TryGetValue(rockBlockId, out LayeredResolvedDepositBlock resolvedMiddleBlock) &&
                    resolvedMiddleBlock.Blocks.Length > 0)
                {
                    Block middlePlaceBlock = resolvedMiddleBlock.Blocks[0];

                    int middleCount = Math.Min(MiddleLayerThickness, surfaceY - rockStartY);

                    for (int y = rockStartY + 1; y <= rockStartY + middleCount; y++)
                    {
                        int index3d = ((y % chunksize) * chunksize + lz) * chunksize + lx;
                        IChunkBlocks chunkdata = chunks[y / chunksize].Data;
                        int blockId = chunkdata.GetBlockIdUnsafe(index3d);

                        // Interrupted column (air pocket, water, non-soil): stop stacking
                        if (!topLayerBlockByInBlockId.ContainsKey(blockId)) break;

                        chunkdata.SetBlockUnsafe(index3d, middlePlaceBlock.BlockId);
                        chunkdata.SetFluid(index3d, 0);
                        middleLayersPlaced++;
                    }
                }

                // Generate top layer (soil -> clay) upward from above the middle layer, repeating
                // the top block through all remaining soil up to the surface
                for (int y = rockStartY + middleLayersPlaced + 1; y <= surfaceY; y++)
                {
                    int index3d = ((y % chunksize) * chunksize + lz) * chunksize + lx;
                    IChunkBlocks chunkdata = chunks[y / chunksize].Data;
                    int blockId = chunkdata.GetBlockIdUnsafe(index3d);

                    if (!topLayerBlockByInBlockId.TryGetValue(blockId, out LayeredResolvedDepositBlock resolvedTopBlock) || resolvedTopBlock.Blocks.Length == 0)
                    {
                        // Interrupted column: stop stacking
                        break;
                    }

                    Block placeblock = resolvedTopBlock.Blocks[0];

                    if (WithLastLayerBlockCallback && y == surfaceY)
                    {
                        // Surface block goes through the world gen callback so grass coverage forms
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
        miny = pos.Y - 8 - MiddleLayerThickness - BottomLayerThickness;
        maxy = pos.Y;
    }
}
