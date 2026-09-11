using System;
using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.ServerMods;

namespace AdvancedGeology.WorldGen.ProceduralDeposits;


public readonly record struct ProceduralProspectingFeature(
    ulong FeatureId,
    int CenterX,
    int CenterZ,
    int SignalRadius,
    int CenterShift,
    string[] MajorMinerals,
    double SignalPeak);

public static class ProceduralProspectingSignal
{
    private const ulong ShiftAngleSalt = 0x50524F5350414E47UL;
    private const ulong ShiftDistanceSalt = 0x50524F5350444953UL;
    private const ulong WarpXSalt = 0x50524F5350574158UL;
    private const ulong WarpZSalt = 0x50524F535057415AUL;
    private const ulong IntensitySalt = 0x50524F5350494E54UL;
    public static ProceduralProspectingFeature Create(
        ulong featureId,
        int centerX,
        int centerZ,
        int signalRadius,
        int centerShift,
        string[] majorMinerals)
    {
        var feature = new ProceduralProspectingFeature(
            featureId,
            centerX,
            centerZ,
            signalRadius,
            centerShift,
            majorMinerals,
            1.0);
        return feature with { SignalPeak = Peak(feature) };
    }


    public static double Sample(in ProceduralProspectingFeature feature, double worldX, double worldZ)
    {
        double angle = ProceduralDepositMath.UnitDouble(feature.FeatureId ^ ShiftAngleSalt) * Math.Tau;
        double shiftDistance = feature.CenterShift * Math.Sqrt(ProceduralDepositMath.UnitDouble(feature.FeatureId ^ ShiftDistanceSalt));
        double signalX = feature.CenterX + Math.Cos(angle) * shiftDistance;
        double signalZ = feature.CenterZ + Math.Sin(angle) * shiftDistance;

        double warpScale = Math.Max(72.0, feature.SignalRadius * 1.65);
        double warpAmplitude = Math.Min(36.0, feature.SignalRadius * 0.48);
        double warpX = (ProceduralDepositMath.SmoothNoise2D(feature.FeatureId, worldX / warpScale, worldZ / warpScale, WarpXSalt) - 0.5) * 2.0 * warpAmplitude;
        double warpZ = (ProceduralDepositMath.SmoothNoise2D(feature.FeatureId, worldX / warpScale, worldZ / warpScale, WarpZSalt) - 0.5) * 2.0 * warpAmplitude;
        double dx = worldX + warpX - signalX;
        double dz = worldZ + warpZ - signalZ;
        double distance = Math.Sqrt(dx * dx + dz * dz);
        double t = Math.Clamp(1.0 - distance / feature.SignalRadius, 0.0, 1.0);
        if (t <= 0.0) return 0.0;

        double broadSignal = t * t * (3.0 - 2.0 * t);
        double intensityNoise = ProceduralDepositMath.SmoothNoise2D(
            feature.FeatureId,
            worldX / (warpScale * 0.7),
            worldZ / (warpScale * 0.7),
            IntensitySalt);
        return Math.Clamp(broadSignal * (0.85 + intensityNoise * 0.30), 0.0, 1.0);
    }

    public static double Peak(in ProceduralProspectingFeature feature)
    {
        double maximum = 0.0;
        int searchRadius = feature.CenterShift + 48;
        for (int z = feature.CenterZ - searchRadius; z <= feature.CenterZ + searchRadius; z += 2)
        {
            for (int x = feature.CenterX - searchRadius; x <= feature.CenterX + searchRadius; x += 2)
            {
                maximum = Math.Max(maximum, Sample(feature, x, z));
            }
        }
        return maximum;
    }

    public static double NormalizedSample(in ProceduralProspectingFeature feature, double worldX, double worldZ)
    {
        return feature.SignalPeak > 0.0
            ? Math.Clamp(Sample(feature, worldX, worldZ) / feature.SignalPeak, 0.0, 1.0)
            : 0.0;
    }
}

public sealed partial class ProceduralDepositWorldGenSystem
{
    private const int ProspectingScanRadius = GlobalConstants.ChunkSize;
    private const int ProspectingMapPadding = 1;
    private readonly Dictionary<ulong, Dictionary<string, double>> featureBasePpt = new();

    private void RefreshLoadedProspectingMaps(Vec2i mapCoord, IMapRegion mapRegion)
    {
        GenerateProspectingMaps(mapRegion, mapCoord.X, mapCoord.Y, null);
    }

    private void GenerateProspectingMaps(IMapRegion mapRegion, int regionX, int regionZ, ITreeAttribute? chunkGenParams)
    {
        if (definitions.Count == 0 || serverApi == null) return;

        int regionSize = serverApi.WorldManager.RegionSize;
        int innerSize = regionSize / TerraGenConfig.oreMapScale;
        int mapSize = innerSize + ProspectingMapPadding;
        var maps = new Dictionary<string, IntDataMap2D>(StringComparer.Ordinal);
        var features = new List<ProceduralProspectingFeature>();

        foreach (CompiledProceduralDeposit compiled in definitions)
        {
            CollectProspectingFeatures(compiled, regionX, regionZ, regionSize, features);
        }

        foreach (CompiledProceduralDeposit compiled in definitions)
        {
            foreach (string mineral in compiled.Definition.Prospecting.MajorMinerals)
            {
                if (maps.ContainsKey(mineral)) continue;
                maps.Add(mineral, new IntDataMap2D
                {
                    Size = mapSize,
                    BottomRightPadding = ProspectingMapPadding,
                    Data = new int[mapSize * mapSize]
                });
            }
        }

        int regionBaseX = regionX * regionSize;
        int regionBaseZ = regionZ * regionSize;
        foreach (ProceduralProspectingFeature feature in features)
        {
            foreach (string mineral in feature.MajorMinerals)
            {
                IntDataMap2D map = maps[mineral];
                for (int z = 0; z < mapSize; z++)
                {
                    double worldZ = regionBaseZ + (double)z / innerSize * regionSize;
                    int row = z * mapSize;
                    for (int x = 0; x < mapSize; x++)
                    {
                        double worldX = regionBaseX + (double)x / innerSize * regionSize;
                        int value = (int)Math.Round(ProceduralProspectingSignal.NormalizedSample(feature, worldX, worldZ) * 255.0);
                        if (value > map.Data[row + x]) map.Data[row + x] = value;
                    }
                }
            }
        }

        foreach ((string mineral, IntDataMap2D map) in maps)
        {
            mapRegion.OreMaps[mineral] = map;
        }
    }

    private void CollectProspectingFeatures(
        CompiledProceduralDeposit compiled,
        int regionX,
        int regionZ,
        int regionSize,
        List<ProceduralProspectingFeature> features)
    {
        ProceduralDepositDefinition definition = compiled.Definition;
        ProceduralProspectingDefinition prospecting = definition.Prospecting;
        int influence = prospecting.SignalRadius + prospecting.CenterShift + 36;
        int cellSize = definition.Placement.CellSize;
        int baseX = regionX * regionSize;
        int baseZ = regionZ * regionSize;
        int minCellX = ProceduralDepositMath.FloorDiv(baseX - influence, cellSize);
        int maxCellX = ProceduralDepositMath.FloorDiv(baseX + regionSize + influence, cellSize);
        int minCellZ = ProceduralDepositMath.FloorDiv(baseZ - influence, cellSize);
        int maxCellZ = ProceduralDepositMath.FloorDiv(baseZ + regionSize + influence, cellSize);

        for (int cellZ = minCellZ; cellZ <= maxCellZ; cellZ++)
        {
            for (int cellX = minCellX; cellX <= maxCellX; cellX++)
            {
                ulong featureId = ProceduralDepositMath.FeatureId(serverApi!.World.Seed, compiled.CodeHash, cellX, cellZ);
                if (!TryGetFeatureCenter(definition, featureId, cellX, cellZ, out int centerX, out int centerZ)) continue;
                if (!MatchesClimate(definition, centerX, centerZ)) continue;

                features.Add(ProceduralProspectingSignal.Create(
                    featureId,
                    centerX,
                    centerZ,
                    prospecting.SignalRadius,
                    prospecting.CenterShift,
                    prospecting.MajorMinerals));
            }
        }
    }

    public bool TryGetProspectingReading(string mineral, BlockPos pos, out double totalFactor, out double ppt)
    {
        totalFactor = 0.0;
        ppt = 0.0;
        if (serverApi == null || definitions.Count == 0) return false;

        foreach (CompiledProceduralDeposit compiled in definitions)
        {
            if (!compiled.Definition.Prospecting.MajorMinerals.Contains(mineral, StringComparer.Ordinal)) continue;

            ProceduralDepositDefinition definition = compiled.Definition;
            int influence = definition.Prospecting.SignalRadius + definition.Prospecting.CenterShift + 36;
            int cellSize = definition.Placement.CellSize;
            int minCellX = ProceduralDepositMath.FloorDiv(pos.X - influence, cellSize);
            int maxCellX = ProceduralDepositMath.FloorDiv(pos.X + influence, cellSize);
            int minCellZ = ProceduralDepositMath.FloorDiv(pos.Z - influence, cellSize);
            int maxCellZ = ProceduralDepositMath.FloorDiv(pos.Z + influence, cellSize);

            for (int cellZ = minCellZ; cellZ <= maxCellZ; cellZ++)
            {
                for (int cellX = minCellX; cellX <= maxCellX; cellX++)
                {
                    ulong featureId = ProceduralDepositMath.FeatureId(serverApi.World.Seed, compiled.CodeHash, cellX, cellZ);
                    if (!TryGetFeatureCenter(definition, featureId, cellX, cellZ, out int centerX, out int centerZ)) continue;
                    if (!MatchesClimate(definition, centerX, centerZ)) continue;
                    if (!HasActualEligibleHost(compiled, centerX, centerZ)) continue;
                    if (!TryGetFeatureBasePpt(featureId, centerX, centerZ, mineral, out double basePpt)) continue;

                    ProceduralProspectingFeature feature = ProceduralProspectingSignal.Create(
                        featureId,
                        centerX,
                        centerZ,
                        definition.Prospecting.SignalRadius,
                        definition.Prospecting.CenterShift,
                        Array.Empty<string>());
                    double signal = ProceduralProspectingSignal.NormalizedSample(feature, pos.X, pos.Z);
                    double contribution = basePpt * signal;
                    if (contribution <= ppt) continue;
                    totalFactor = signal;
                    ppt = contribution;
                }
            }
        }

        return ppt > 0.0;
    }

    private bool TryGetFeatureBasePpt(
        ulong featureId,
        int centerX,
        int centerZ,
        string mineral,
        out double ppt)
    {
        if (!featureBasePpt.TryGetValue(featureId, out Dictionary<string, double>? mineralPpt))
        {
            if (!TryScanFeatureBasePpt(centerX, centerZ, out mineralPpt))
            {
                ppt = 0.0;
                return false;
            }
            featureBasePpt.Add(featureId, mineralPpt);
        }

        ppt = mineralPpt.TryGetValue(mineral, out double measured) ? measured : 0.0;
        return ppt > 0.0;
    }

    private bool TryScanFeatureBasePpt(int centerX, int centerZ, out Dictionary<string, double> mineralPpt)
    {
        mineralPpt = new Dictionary<string, double>(StringComparer.Ordinal);
        int minimumX = centerX - ProspectingScanRadius;
        int maximumX = centerX + ProspectingScanRadius;
        int minimumZ = centerZ - ProspectingScanRadius;
        int maximumZ = centerZ + ProspectingScanRadius;
        int minimumChunkX = ProceduralDepositMath.FloorDiv(minimumX, ChunkSize);
        int maximumChunkX = ProceduralDepositMath.FloorDiv(maximumX, ChunkSize);
        int minimumChunkZ = ProceduralDepositMath.FloorDiv(minimumZ, ChunkSize);
        int maximumChunkZ = ProceduralDepositMath.FloorDiv(maximumZ, ChunkSize);
        int maximumHeight = 0;

        for (int chunkZ = minimumChunkZ; chunkZ <= maximumChunkZ; chunkZ++)
        {
            for (int chunkX = minimumChunkX; chunkX <= maximumChunkX; chunkX++)
            {
                IMapChunk? mapChunk = serverApi!.World.BlockAccessor.GetMapChunk(chunkX, chunkZ);
                if (mapChunk?.CurrentPass != EnumWorldGenPass.Done) return false;
                foreach (ushort height in mapChunk.WorldGenTerrainHeightMap)
                {
                    if (height > maximumHeight) maximumHeight = height;
                }
            }
        }

        var counts = new Dictionary<string, int>(StringComparer.Ordinal);
        var minimum = new BlockPos(minimumX, 0, minimumZ);
        var maximum = new BlockPos(maximumX, maximumHeight, maximumZ);
        serverApi!.World.BlockAccessor.WalkBlocks(minimum, maximum, (block, _, _, _) =>
        {
            if (block.BlockMaterial != EnumBlockMaterial.Ore
                || block.Variant == null
                || !block.Variant.TryGetValue("type", out string? type)) return;

            string code = NormalizeProspectingMineral(type);
            counts[code] = counts.TryGetValue(code, out int count) ? count + 1 : 1;
        });

        int diameter = ProspectingScanRadius * 2;
        long volume = (long)diameter * diameter * maximumHeight;
        if (volume <= 0) return true;
        foreach ((string code, int count) in counts)
        {
            mineralPpt[code] = (double)count / volume * 1000.0;
        }
        return true;
    }

    public static string NormalizeProspectingMineral(string type)
    {
        int separator = type.LastIndexOf('_');
        string mineral = separator >= 0 ? type[(separator + 1)..] : type;
        return mineral switch
        {
            "nativegold" => "gold",
            "nativesilver" => "silver",
            "lapislazuli" => "lapis",
            _ => mineral
        };
    }


    private bool HasActualEligibleHost(CompiledProceduralDeposit compiled, int centerX, int centerZ)
    {
        int reach = Math.Max(8, compiled.MaximumHorizontalReach / 2);
        ReadOnlySpan<(int X, int Z)> offsets =
        [
            (0, 0),
            (reach, 0),
            (-reach, 0),
            (0, reach),
            (0, -reach)
        ];

        foreach ((int offsetX, int offsetZ) in offsets)
        {
            int worldX = centerX + offsetX;
            int worldZ = centerZ + offsetZ;
            IMapChunk? mapChunk = serverApi!.World.BlockAccessor.GetMapChunkAtBlockPos(new BlockPos(worldX, 0, worldZ));
            if (mapChunk?.CurrentPass != EnumWorldGenPass.Done) continue;

            int localX = ProceduralDepositMath.FloorMod(worldX, ChunkSize);
            int localZ = ProceduralDepositMath.FloorMod(worldZ, ChunkSize);
            int surfaceY = mapChunk.WorldGenTerrainHeightMap[localZ * ChunkSize + localX];
            var blockPos = new BlockPos(worldX, surfaceY, worldZ);
            for (int y = surfaceY; y > 0; y -= 2)
            {
                blockPos.Y = y;
                if (compiled.CanReplaceRock(serverApi.World.BlockAccessor.GetBlock(blockPos).BlockId)) return true;
            }
        }

        return false;
    }


    private static bool TryGetFeatureCenter(
        ProceduralDepositDefinition definition,
        ulong featureId,
        int cellX,
        int cellZ,
        out int centerX,
        out int centerZ)
    {
        if (ProceduralDepositMath.UnitDouble(featureId ^ ChanceSalt) >= definition.Placement.Chance)
        {
            centerX = 0;
            centerZ = 0;
            return false;
        }

        int cellSize = definition.Placement.CellSize;
        centerX = cellX * cellSize + (int)(ProceduralDepositMath.UnitDouble(featureId ^ XSalt) * cellSize);
        centerZ = cellZ * cellSize + (int)(ProceduralDepositMath.UnitDouble(featureId ^ ZSalt) * cellSize);
        return true;
    }
}
