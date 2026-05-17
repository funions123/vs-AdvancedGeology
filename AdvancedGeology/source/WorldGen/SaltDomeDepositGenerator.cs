using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Util;
using Vintagestory.ServerMods;

namespace AdvancedGeology.WorldGen;

/// <summary>
/// Configuration for a cap layer block (what to place on top of the dome).
/// </summary>
[JsonObject(MemberSerialization.OptIn)]
public class DomeCapBlock
{
    [JsonProperty]
    public AssetLocation Code;

    [JsonProperty]
    public string Name;

    [JsonProperty]
    public string[] AllowedVariants;

    /// <summary>
    /// Thickness of this cap layer in blocks.
    /// </summary>
    [JsonProperty]
    public NatFloat Thickness;

    /// <summary>
    /// Resolved blocks for this cap layer.
    /// </summary>
    public Block[] ResolvedBlocks;

    public void Resolve(string fileForLogging, ICoreServerAPI api)
    {
        Block[] foundBlocks = api.World.SearchBlocks(Code);

        if (foundBlocks.Length == 0)
        {
            api.World.Logger.Warning("SaltDomeDeposit {0}: No block with code/wildcard '{1}' was found", fileForLogging, Code);
        }

        if (AllowedVariants != null)
        {
            List<Block> filteredBlocks = new List<Block>();
            for (int i = 0; i < foundBlocks.Length; i++)
            {
                if (WildcardUtil.Match(Code, foundBlocks[i].Code, AllowedVariants))
                {
                    filteredBlocks.Add(foundBlocks[i]);
                }
            }
            foundBlocks = filteredBlocks.ToArray();
        }

        ResolvedBlocks = foundBlocks;
    }

    public Block GetBlock(LCGRandom rand)
    {
        if (ResolvedBlocks == null || ResolvedBlocks.Length == 0) return null;
        return ResolvedBlocks[rand.NextInt(ResolvedBlocks.Length)];
    }
}

/// <summary>
/// Deposit generator for salt dome structures: a hemispherical dome of the primary block
/// (halite) with configurable cap rock layers on top (e.g. limestone, sulfur).
/// </summary>
[JsonObject(MemberSerialization.OptIn)]
public class SaltDomeDepositGenerator : DepositGeneratorBase
{
    /// <summary>The block to check for (mother rock that gets replaced).</summary>
    [JsonProperty]
    public DepositBlock InBlock;

    /// <summary>The main dome block to place (e.g. halite).</summary>
    [JsonProperty]
    public DepositBlock PlaceBlock;

    /// <summary>
    /// Cap layers to place on top of the dome, bottom to top; first sits on halite, last outside.
    /// </summary>
    [JsonProperty]
    public DomeCapBlock[] CapLayers;

    /// <summary>Horizontal radius of the dome base (cylindrical stock). Default 25 +- 7 blocks.</summary>
    [JsonProperty]
    public NatFloat Radius;

    /// <summary>
    /// How much the dome center rises above the rim (flat-topped cylindrical shape). Default
    /// 7 ± 3 blocks; 0 for a perfectly flat top.
    /// </summary>
    [JsonProperty]
    public NatFloat DomeRise;

    /// <summary>Relative Y position where the dome base sits (0 = bottom, 1 = surface).</summary>
    [JsonProperty]
    public NatFloat YPosRel;

    /// <summary>Maximum Y roughness; skips positions with surface variation greater than this.</summary>
    [JsonProperty]
    public int MaxYRoughness = 999;

    /// <summary>Minimum distance from surface for the dome top. 0 or negative allows breaching.</summary>
    [JsonProperty]
    public int MinDistanceFromSurface = 5;

    /// <summary>If true, the dome extends to the mantle (y=1) for a proper diapir/salt stock.</summary>
    [JsonProperty]
    public bool ExtendToMantle = true;

    /// <summary>If true, the dome can breach the surface (exposed salt domes in dry climates).</summary>
    [JsonProperty]
    public bool CanBreachSurface = false;

    /// <summary>
    /// If true, the dome pushes up terrain above it into a hill. Only applies when surface is
    /// below DeformBelowY (low-lying flat areas).
    /// </summary>
    [JsonProperty]
    public bool DeformTerrain = true;

    /// <summary>
    /// Maximum hill height from terrain deformation. Actual height varies with dome size and
    /// distance from center.
    /// </summary>
    [JsonProperty]
    public NatFloat DeformHeight;

    /// <summary>
    /// Only deform terrain when surface Y is below this, keeping hills off mountains and existing
    /// features. Default 125 (slightly above sea level).
    /// </summary>
    [JsonProperty]
    public int DeformBelowY = 125;

    /// <summary>
    /// Steepness of the deformation curve: higher is gentler. 1.0 = cosine curve, 2.0 = gentler,
    /// 0.5 = steeper. Default 1.0.
    /// </summary>
    [JsonProperty]
    public float DeformCurvePower = 2.0f;

    protected int worldheight;
    protected Block[] placeBlocks;
    protected HashSet<int> inBlockIds = new HashSet<int>();
    protected float absAvgQuantity;

    // Block layer generation for terrain deformation
    protected BlockLayerConfig blockLayerConfig;
    protected List<int> blockLayerIds = new List<int>();
    protected SimplexNoise distort2dx;
    protected SimplexNoise distort2dz;

    // Tall grass blocks for vegetation on deformed terrain
    protected Block[] tallGrassBlocks;

    public SaltDomeDepositGenerator(ICoreServerAPI api, DepositVariant variant, LCGRandom depositRand, NormalizedSimplexNoise noiseGen)
        : base(api, variant, depositRand, noiseGen)
    {
        worldheight = api.World.BlockAccessor.MapSizeY;

        // Set defaults first in case JSON parsing fails
        Radius = NatFloat.createUniform(12, 3);
        DomeRise = NatFloat.createUniform(7, 3);
        YPosRel = NatFloat.createUniform(0.5f, 0.2f);
        DeformHeight = NatFloat.createUniform(5, 3);

        if (variant?.Attributes != null)
        {
            try
            {
                JsonConvert.PopulateObject(variant.Attributes.Token.ToString(), this);
            }
            catch (Exception e)
            {
                api.World.Logger.Error("SaltDomeDeposit {0}: Failed to parse attributes: {1}", variant.fromFile, e.Message);
            }
        }
    }

    public override void Init()
    {
        if (Radius == null)
        {
            Api.Server.LogWarning("SaltDomeDeposit {0} has no radius property defined. Defaulting to 12±3", variant.fromFile);
            Radius = NatFloat.createUniform(12, 3);
        }

        if (DomeRise == null)
        {
            Api.Server.LogWarning("SaltDomeDeposit {0}: DomeRise was null, using default", variant.fromFile);
            DomeRise = NatFloat.createUniform(7, 3); // Flat-topped with slight center bulge
        }

        if (YPosRel == null)
        {
            Api.Server.LogWarning("SaltDomeDeposit {0}: YPosRel was null, using default", variant.fromFile);
            YPosRel = NatFloat.createUniform(0.5f, 0.2f);
        }

        if (InBlock != null)
        {
            Block[] foundBlocks = Api.World.SearchBlocks(InBlock.Code);

            if (foundBlocks.Length == 0)
            {
                Api.World.Logger.Warning("SaltDomeDeposit {0}: No block with code/wildcard '{1}' was found for inblock", variant.fromFile, InBlock.Code);
            }

            if (InBlock.AllowedVariants != null)
            {
                List<Block> filteredBlocks = new List<Block>();
                for (int i = 0; i < foundBlocks.Length; i++)
                {
                    if (WildcardUtil.Match(InBlock.Code, foundBlocks[i].Code, InBlock.AllowedVariants))
                    {
                        filteredBlocks.Add(foundBlocks[i]);
                    }
                }
                foundBlocks = filteredBlocks.ToArray();
            }

            foreach (var block in foundBlocks)
            {
                inBlockIds.Add(block.BlockId);
            }
        }
        else
        {
            Api.Server.LogWarning("SaltDomeDeposit {0}: InBlock is NULL!", variant.fromFile);
        }

        if (PlaceBlock != null)
        {
            Block[] foundBlocks = Api.World.SearchBlocks(PlaceBlock.Code);

            if (foundBlocks.Length == 0)
            {
                Api.World.Logger.Warning("SaltDomeDeposit {0}: No block with code/wildcard '{1}' was found for placeblock", variant.fromFile, PlaceBlock.Code);
            }

            if (PlaceBlock.AllowedVariants != null)
            {
                List<Block> filteredBlocks = new List<Block>();
                for (int i = 0; i < foundBlocks.Length; i++)
                {
                    if (WildcardUtil.Match(PlaceBlock.Code, foundBlocks[i].Code, PlaceBlock.AllowedVariants))
                    {
                        filteredBlocks.Add(foundBlocks[i]);
                    }
                }
                foundBlocks = filteredBlocks.ToArray();
            }

            placeBlocks = foundBlocks;
        }
        else
        {
            Api.Server.LogWarning("SaltDomeDeposit {0}: PlaceBlock is NULL!", variant.fromFile);
        }

        if (CapLayers != null)
        {
            foreach (var cap in CapLayers)
            {
                cap.Resolve(variant.fromFile, Api);
            }
        }

        // Calculate average deposit volume for propick readings.
        {
            LCGRandom rnd = new LCGRandom(Api.World.Seed);
            float avgRadius = 0;
            float avgDomeHeight = 0;

            for (int j = 0; j < 100; j++)
            {
                float r = Math.Min(15, Radius.nextFloat(1, rnd));
                avgRadius += r;

                float relY = GameMath.Clamp(YPosRel.nextFloat(1, rnd), 0, 1);
                float rise = Math.Max(1, DomeRise.nextFloat(1, rnd));
                // Approximate dome top - sedimentary layers typically span ~40-60% of world height
                float approxDomeTop = relY * worldheight * 0.5f + rise;
                float domeBase = ExtendToMantle ? 1 : Math.Max(1, approxDomeTop - 50);
                avgDomeHeight += approxDomeTop - domeBase;
            }

            avgRadius /= 100;
            avgDomeHeight /= 100;
            absAvgQuantity = GameMath.PI * avgRadius * avgRadius * avgDomeHeight;
        }

        // Initialize block layer config for terrain deformation soil generation
        if (DeformTerrain)
        {
            blockLayerConfig = BlockLayerConfig.GetInstance(Api);
            distort2dx = new SimplexNoise(new double[] { 14, 9, 6, 3 }, new double[] { 1 / 100.0, 1 / 50.0, 1 / 25.0, 1 / 12.5 }, Api.World.SeaLevel + 20980);
            distort2dz = new SimplexNoise(new double[] { 14, 9, 6, 3 }, new double[] { 1 / 100.0, 1 / 50.0, 1 / 25.0, 1 / 12.5 }, Api.World.SeaLevel + 20981);

            // Initialize tall grass blocks for vegetation
            tallGrassBlocks = Api.World.SearchBlocks(new AssetLocation("game:tallgrass-*-free"));
        }
    }

    public override void GenDeposit(IBlockAccessor blockAccessor, IServerChunk[] chunks, int chunkX, int chunkZ, BlockPos depoCenterPos, ref Dictionary<BlockPos, DepositVariant> subDepositsToPlace)
    {
        // Safety checks for required properties
        if (Radius == null || DomeRise == null || YPosRel == null || chunks == null || chunks.Length == 0)
        {
            return;
        }

        if (placeBlocks == null || placeBlocks.Length == 0)
        {
            return;
        }

        // Cap radius to 15 so deposit fits within a single 32x32 chunk
        int radius = Math.Min(15, (int)Radius.nextFloat(1, DepositRand));
        int domeRise = Math.Max(1, (int)DomeRise.nextFloat(1, DepositRand)); // Height of center above rim

        if (radius <= 0) return;

        // Calculate total cap thickness
        int totalCapThickness = 0;
        int[] capThicknesses = null;
        if (CapLayers != null && CapLayers.Length > 0)
        {
            capThicknesses = new int[CapLayers.Length];
            for (int i = 0; i < CapLayers.Length; i++)
            {
                capThicknesses[i] = CapLayers[i].Thickness != null ? (int)CapLayers[i].Thickness.nextFloat(1, DepositRand) : 1;
                totalCapThickness += capThicknesses[i];
            }
        }

        int baseX = chunkX * chunksize;
        int baseZ = chunkZ * chunksize;

        // Only generate if the original deposit center is within this chunk
        // This prevents every chunk from getting a deposit
        if (depoCenterPos.X < baseX || depoCenterPos.X >= baseX + chunksize ||
            depoCenterPos.Z < baseZ || depoCenterPos.Z >= baseZ + chunksize)
            return;

        // Snap deposit center to chunk center so it always fits within a single chunk
        depoCenterPos.X = baseX + chunksize / 2;
        depoCenterPos.Z = baseZ + chunksize / 2;

        // Get surface height at deposit center for surface-following mode
        if (chunks[0] == null || chunks[0].MapChunk == null) return;
        IMapChunk heremapchunk = chunks[0].MapChunk;
        if (heremapchunk.WorldGenTerrainHeightMap == null) return;

        int centerLx = chunksize / 2;
        int centerLz = chunksize / 2;
        int surfaceY = heremapchunk.WorldGenTerrainHeightMap[centerLz * chunksize + centerLx];

        // Scan the rock column to find valid sedimentary rock layers
        // We need to find the top and bottom of the valid rock range
        int validTopY = -1;  // Highest Y with valid sedimentary rock
        int validBottomY = -1;  // Lowest Y with valid sedimentary rock

        if (inBlockIds.Count > 0)
        {
            // Scan from surface down to bedrock
            for (int y = surfaceY; y >= 1; y--)
            {
                int chunkIndex = y / chunksize;
                if (chunkIndex < 0 || chunkIndex >= chunks.Length || chunks[chunkIndex]?.Data == null)
                    continue;

                int index3d = ((y % chunksize) * chunksize + centerLz) * chunksize + centerLx;
                int blockId = chunks[chunkIndex].Data.GetBlockIdUnsafe(index3d);

                if (inBlockIds.Contains(blockId))
                {
                    // Found valid sedimentary rock
                    if (validTopY < 0)
                    {
                        validTopY = y;  // First valid rock from top = top of sedimentary layer
                    }
                    validBottomY = y;  // Keep updating bottom as we go down
                }
            }

            // No valid sedimentary rock found in this column - don't spawn here
            if (validTopY < 0 || validBottomY < 0)
            {
                return;
            }
        }
        else
        {
            // No inblock restriction - use full column
            validTopY = surfaceY;
            validBottomY = 1;
        }

        // Calculate dome top Y position within the valid sedimentary range
        // yPosRel determines where the dome top sits within the valid rock layers
        // 0.0 = at bottom of sedimentary, 1.0 = at top of sedimentary
        float relY = YPosRel.nextFloat(1, DepositRand);
        int sedimentaryThickness = validTopY - validBottomY;
        int domeTopY = validBottomY + (int)(relY * sedimentaryThickness);

        // For capped domes, ensure top + caps don't breach surface (unless CanBreachSurface)
        int totalTopY = domeTopY + totalCapThickness;
        if (!CanBreachSurface && totalTopY > surfaceY - MinDistanceFromSurface)
        {
            int adjustment = totalTopY - (surfaceY - MinDistanceFromSurface);
            domeTopY -= adjustment;
        }

        // For surface-breaching domes, cap at surface level
        if (CanBreachSurface && domeTopY > surfaceY)
        {
            domeTopY = surfaceY;
        }

        // Dome base
        int domeBaseY = ExtendToMantle ? 1 : Math.Max(1, domeTopY - 50);

        if (domeTopY < 10) return; // Dome too shallow

        // Deform the circle slightly (but keep within chunk bounds)
        float deform = GameMath.Clamp(DepositRand.NextFloat() - 0.5f, -0.2f, 0.2f);
        int radiusX = Math.Min(15, radius - (int)(radius * deform));
        int radiusZ = Math.Min(15, radius + (int)(radius * deform));

        float xRadSqInv = 1f / (radiusX * radiusX);
        float zRadSqInv = 1f / (radiusZ * radiusZ);

        // Deposit is centered in chunk, so bounds are simple
        int minx = depoCenterPos.X - radiusX;
        int maxx = depoCenterPos.X + radiusX;
        int minz = depoCenterPos.Z - radiusZ;
        int maxz = depoCenterPos.Z + radiusZ;

        int blocksPlaced = 0;
        BlockPos tmpPos = new BlockPos(0, 0, 0, 0);

        for (int posx = minx; posx < maxx; posx++)
        {
            int distx = posx - depoCenterPos.X;
            float xSq = distx * distx * xRadSqInv;

            for (int posz = minz; posz < maxz; posz++)
            {
                int distz = posz - depoCenterPos.Z;
                float zSq = distz * distz * zRadSqInv;

                // Check horizontal distance from center
                float horizontalDistSq = xSq + zSq;
                if (horizontalDistSq > 1) continue;

                // Add noise distortion to the edge
                double noiseVal = 1 - (radius > 3 && DistortNoiseGen != null ? DistortNoiseGen.Noise(posx / 3.0, posz / 3.0) * 0.15 : 0);
                if (horizontalDistSq > noiseVal) continue;

                // Calculate dome top at this XZ position using flat-topped paraboloid formula
                // Creates a cylindrical stock with a gentle dome rise at center
                // At edge (horizontalDistSq=1): top = domeTopY - domeRise (the rim)
                // At center (horizontalDistSq=0): top = domeTopY (the peak)
                int localDomeTop = domeTopY - (int)(domeRise * horizontalDistSq);

                // Get local surface height from map chunk (may be different chunk)
                IMapChunk localMapChunk = blockAccessor.GetMapChunk(posx / chunksize, posz / chunksize);
                int localSurfaceY = surfaceY; // Default to center surface
                if (localMapChunk?.WorldGenTerrainHeightMap != null)
                {
                    int localLx = posx % chunksize;
                    int localLz = posz % chunksize;
                    if (localLx < 0) localLx += chunksize;
                    if (localLz < 0) localLz += chunksize;
                    localSurfaceY = localMapChunk.WorldGenTerrainHeightMap[localLz * chunksize + localLx];
                }

                // For surface-breaching domes, cap at local surface
                if (CanBreachSurface && localDomeTop > localSurfaceY)
                {
                    localDomeTop = localSurfaceY;
                }

                // Check Y roughness at this position
                if (Math.Abs(surfaceY - localSurfaceY) > MaxYRoughness) continue;

                // Place dome blocks (halite) from base (y=1 if ExtendToMantle) to local dome top
                // Salt domes extend through all rock types once spawned
                for (int y = domeBaseY; y <= localDomeTop; y++)
                {
                    if (y < 1 || y >= worldheight - 1) continue;

                    tmpPos.Set(posx, y, posz);
                    Block existingBlock = blockAccessor.GetBlock(tmpPos);

                    // Only replace solid rock blocks (any rock type)
                    if (existingBlock == null || existingBlock.BlockMaterial != EnumBlockMaterial.Stone) continue;

                    if (placeBlocks != null && placeBlocks.Length > 0)
                    {
                        Block placeBlock = placeBlocks[DepositRand.NextInt(placeBlocks.Length)];
                        blockAccessor.SetBlock(placeBlock.BlockId, tmpPos);
                        blocksPlaced++;
                    }
                }

                // Place cap layers on top of the dome
                if (CapLayers != null && capThicknesses != null)
                {
                    int capY = localDomeTop + 1;

                    for (int capIndex = 0; capIndex < CapLayers.Length; capIndex++)
                    {
                        var capLayer = CapLayers[capIndex];
                        int capThickness = capThicknesses[capIndex];

                        for (int t = 0; t < capThickness; t++)
                        {
                            int y = capY + t;
                            if (y < 1 || y >= worldheight - 1) continue;

                            tmpPos.Set(posx, y, posz);
                            Block existingBlock = blockAccessor.GetBlock(tmpPos);

                            // Only replace solid rock blocks (any rock type)
                            if (existingBlock == null || existingBlock.BlockMaterial != EnumBlockMaterial.Stone) continue;

                            Block capBlock = capLayer.GetBlock(DepositRand);
                            if (capBlock != null)
                            {
                                blockAccessor.SetBlock(capBlock.BlockId, tmpPos);
                            }
                        }

                        capY += capThickness;
                    }
                }
            }
        }

        // Terrain deformation - push up land to create salt dome hills
        if (blocksPlaced > 0 && DeformTerrain && surfaceY < DeformBelowY && blockLayerConfig != null)
        {
            float deformAvg = DeformHeight?.avg ?? 5f;
            float deformVar = DeformHeight?.var ?? 3f;
            int deformSeed = (depoCenterPos.X * 31337) ^ (depoCenterPos.Z * 16411);
            float deformRand = ((deformSeed & 0x7FFFFFFF) / (float)int.MaxValue) * 2f - 1f; // -1 to 1
            int maxDeformHeight = Math.Max(0, (int)(deformAvg + deformVar * deformRand));
            if (maxDeformHeight > 0)
            {
                int regionChunkSize = Api.WorldManager.RegionSize / chunksize;

                for (int posx = minx; posx <= maxx; posx++)
                {
                    int distx = posx - depoCenterPos.X;
                    float xSq = distx * distx * xRadSqInv;

                    for (int posz = minz; posz <= maxz; posz++)
                    {
                        int distz = posz - depoCenterPos.Z;
                        float zSq = distz * distz * zRadSqInv;

                        float horizontalDistSq = xSq + zSq;
                        if (horizontalDistSq > 1) continue;

                        // Calculate hill height using smooth cosine falloff for gentle slopes
                        // DeformCurvePower adjusts the curve: higher = gentler slopes, lower = steeper
                        float distFromCenter = (float)Math.Sqrt(horizontalDistSq);
                        float baseFalloff = (float)(1.0 + Math.Cos(Math.PI * distFromCenter)) / 2f;
                        float smoothFalloff = (float)Math.Pow(baseFalloff, 1.0f / DeformCurvePower);
                        int hillHeight = (int)Math.Round(maxDeformHeight * smoothFalloff);
                        if (hillHeight <= 0) continue;

                        // All positions are within current chunk
                        int localLx = posx - baseX;
                        int localLz = posz - baseZ;
                        int mapIndex = localLz * chunksize + localLx;

                        // Scan blocks directly to find solid ground surface (skip water)
                        int localSurfaceY = 1;
                        int rockBlockId = 0;
                        int existingRockSurfaceY = 1;
                        bool isUnderwater = false;

                        // Scan from a reasonable height down to find solid ground
                        for (int scanY = Math.Min(DeformBelowY + 20, worldheight - 2); scanY >= 1; scanY--)
                        {
                            tmpPos.Set(posx, scanY, posz);
                            Block block = blockAccessor.GetBlock(tmpPos);

                            if (block != null && block.Id != 0)
                            {
                                // Skip over water - we want the actual ground surface
                                if (block.BlockMaterial == EnumBlockMaterial.Water)
                                {
                                    isUnderwater = true;
                                    continue;
                                }

                                // Found solid ground surface
                                if (localSurfaceY == 1) localSurfaceY = scanY;

                                if (block.BlockMaterial == EnumBlockMaterial.Stone)
                                {
                                    rockBlockId = block.Id;
                                    existingRockSurfaceY = scanY;
                                    break;
                                }
                            }
                        }

                        // Skip if ground is already too high (on a hill/mountain)
                        if (localSurfaceY >= DeformBelowY) continue;
                        if (rockBlockId == 0) continue;

                        // Skip positions where local terrain differs too much from center
                        // This prevents pillars on hillsides while still allowing gentle terrain variation
                        int heightDiff = Math.Abs(localSurfaceY - surfaceY);
                        if (heightDiff > hillHeight) continue;

                        // Add hill height to local surface
                        int newSurfaceY = localSurfaceY + hillHeight;

                        // Recheck if still underwater at the new deformed height
                        if (isUnderwater)
                        {
                            tmpPos.Set(posx, newSurfaceY + 1, posz);
                            Block blockAbove = blockAccessor.GetBlock(tmpPos);
                            isUnderwater = blockAbove != null && blockAbove.BlockMaterial == EnumBlockMaterial.Water;
                        }

                        // Fill from existing rock surface to new surface with rock
                        for (int y = existingRockSurfaceY + 1; y <= newSurfaceY; y++)
                        {
                            if (y >= worldheight - 1) break;

                            tmpPos.Set(posx, y, posz);
                            blockAccessor.SetBlock(rockBlockId, tmpPos);
                        }

                        // Get climate for soil generation
                        IntDataMap2D climateMap = heremapchunk.MapRegion.ClimateMap;
                        int rdx = chunkX % regionChunkSize;
                        int rdz = chunkZ % regionChunkSize;
                        float climateStep = (float)climateMap.InnerSize / regionChunkSize;

                        double distxNoise = distort2dx.Noise(posx, posz);
                        double distzNoise = distort2dz.Noise(posx, posz);
                        int climate = climateMap.GetUnpaddedColorLerped(
                            rdx * climateStep + climateStep * (localLx + (float)distxNoise) / chunksize,
                            rdz * climateStep + climateStep * (localLz + (float)distzNoise) / chunksize
                        );

                        int tempUnscaled = (climate >> 16) & 0xff;
                        float temp = Climate.GetScaledAdjustedTemperatureFloat(tempUnscaled, newSurfaceY - TerraGenConfig.seaLevel);
                        float rainRel = Climate.GetRainFall((climate >> 8) & 0xff, newSurfaceY) / 255f;
                        float heightRel = ((float)newSurfaceY - TerraGenConfig.seaLevel) / ((float)worldheight - TerraGenConfig.seaLevel);
                        float fertilityRel = Climate.GetFertilityFromUnscaledTemp((int)(rainRel * 255), tempUnscaled, heightRel) / 255f;

                        if (isUnderwater)
                        {
                            // Use lake bed layer for underwater positions
                            float yRel = (float)newSurfaceY / worldheight;
                            int lakeBedId = blockLayerConfig.LakeBedLayer.GetSuitable(temp, rainRel, yRel, DepositRand, rockBlockId);
                            if (lakeBedId != 0)
                            {
                                tmpPos.Set(posx, newSurfaceY, posz);
                                blockAccessor.SetBlock(lakeBedId, tmpPos);
                            }
                        }
                        else
                        {
                            // Calculate and place soil layers
                            float depthf = TerraGenConfig.SoilThickness(rainRel, temp, newSurfaceY - TerraGenConfig.seaLevel, 1f);
                            int soilDepth = (int)depthf;
                            if (depthf - soilDepth > DepositRand.NextFloat()) soilDepth++;
                            soilDepth = Math.Min(soilDepth, 5);

                            double posRand = (double)GameMath.MurmurHash3(posx, 1, posz) / int.MaxValue;
                            double transitionRand = (posRand + 1) * blockLayerConfig.blockLayerTransitionSize;
                            BlockPos herePos = new BlockPos(posx, newSurfaceY, posz, 0);

                            blockLayerIds.Clear();
                            int posY = newSurfaceY;
                            for (int j = 0; j < blockLayerConfig.Blocklayers.Length && blockLayerIds.Count < soilDepth; j++)
                            {
                                BlockLayer bl = blockLayerConfig.Blocklayers[j];
                                float yDist = bl.CalcYDistance(posY, worldheight);
                                float trfDist = bl.CalcTrfDistance(temp, rainRel, fertilityRel);

                                if (trfDist + yDist <= transitionRand)
                                {
                                    int soilBlockId = bl.GetBlockId(transitionRand, temp, rainRel, fertilityRel, rockBlockId, herePos, worldheight);
                                    if (soilBlockId != 0)
                                    {
                                        blockLayerIds.Add(soilBlockId);
                                        for (int t = 1; t < bl.Thickness && blockLayerIds.Count < soilDepth; t++)
                                        {
                                            blockLayerIds.Add(soilBlockId);
                                        }
                                        posY--;
                                    }
                                }
                            }

                            // Place soil layers
                            int soilY = newSurfaceY;
                            for (int i = 0; i < blockLayerIds.Count; i++)
                            {
                                if (soilY >= worldheight - 1) break;
                                tmpPos.Set(posx, soilY, posz);
                                blockAccessor.SetBlock(blockLayerIds[i], tmpPos);
                                soilY--;
                            }

                            // Place tall grass on suitable terrain
                            if (tallGrassBlocks != null && tallGrassBlocks.Length > 0 && temp > 0 && rainRel > 0.2f)
                            {
                                // Random chance to place grass, higher in wetter/warmer climates
                                float grassChance = Math.Min(0.7f, rainRel * 0.8f);
                                if (DepositRand.NextFloat() < grassChance)
                                {
                                    tmpPos.Set(posx, newSurfaceY + 1, posz);
                                    Block grassBlock = tallGrassBlocks[DepositRand.NextInt(tallGrassBlocks.Length)];
                                    blockAccessor.SetBlock(grassBlock.BlockId, tmpPos);
                                }
                            }
                        }

                        // Update heightmap at the end
                        heremapchunk.WorldGenTerrainHeightMap[mapIndex] = (ushort)newSurfaceY;
                        heremapchunk.RainHeightMap[mapIndex] = (ushort)newSurfaceY;
                    }
                }
            }
        }

        if (blocksPlaced > 0)
        {
            // Handle child deposits (like sylvite) - only if we placed blocks
            if (variant.ChildDeposits != null)
            {
                foreach (var childVariant in variant.ChildDeposits)
                {
                    // Initialize child deposit generator if not already done
                    if (childVariant.GeneratorInst == null)
                    {
                        childVariant.InitWithoutGenerator(Api);
                        childVariant.GeneratorInst = new ChildDepositGenerator(Api, childVariant, DepositRand, DistortNoiseGen);
                        if (childVariant.Attributes != null)
                        {
                            JsonUtil.Populate(childVariant.Attributes.Token, childVariant.GeneratorInst);
                        }
                    }

                    // Add child deposit at dome center position
                    float rndVal = DepositRand.NextFloat();
                    if (childVariant.TriesPerChunk > rndVal)
                    {
                        if (subDepositsToPlace == null)
                        {
                            subDepositsToPlace = new Dictionary<BlockPos, DepositVariant>();
                        }
                        subDepositsToPlace[depoCenterPos.Copy()] = childVariant;
                    }
                }
            }
        }
    }

    public override float GetMaxRadius()
    {
        // Deposits are constrained to fit within a single 32x32 chunk
        return 15f;
    }

    public override void GetPropickReading(BlockPos pos, int oreDist, int[] blockColumn, out double ppt, out double totalFactor)
    {
        int mapheight = Api.World.BlockAccessor.GetTerrainMapheightAt(pos);
        int qchunkblocks = mapheight * chunksize * chunksize;
        if (qchunkblocks == 0) { ppt = 0; totalFactor = 0; return; }

        double oreMapFactor = (oreDist & 0xff) / 255.0;
        double rockFactor = OreBearingBlockQuantityRelative(pos, blockColumn);
        totalFactor = oreMapFactor * rockFactor;

        double quantityOres = totalFactor * absAvgQuantity;
        double relq = quantityOres / qchunkblocks;
        ppt = relq * 1000;
    }

    private double OreBearingBlockQuantityRelative(BlockPos pos, int[] blockColumn)
    {
        if (inBlockIds.Count == 0) return 1;

        GetYMinMax(pos, out double minY, out double maxY);

        int q = 0;
        for (int ypos = 0; ypos < blockColumn.Length; ypos++)
        {
            if (ypos < minY || ypos > maxY) continue;
            if (inBlockIds.Contains(blockColumn[ypos])) q++;
        }

        return (double)q / blockColumn.Length;
    }

    public override void GetYMinMax(BlockPos pos, out double miny, out double maxy)
    {
        // Salt domes extend to mantle when ExtendToMantle is true
        miny = ExtendToMantle ? 1 : pos.Y - 10;

        // Max Y is the dome top (pos.Y + domeRise) plus cap layers
        float domeRiseMax = DomeRise != null ? DomeRise.avg + DomeRise.var : 10;
        int totalCapThickness = 0;
        if (CapLayers != null)
        {
            foreach (var cap in CapLayers)
            {
                totalCapThickness += cap.Thickness != null ? (int)(cap.Thickness.avg + cap.Thickness.var) : 1;
            }
        }

        maxy = pos.Y + domeRiseMax + totalCapThickness;
    }
}
