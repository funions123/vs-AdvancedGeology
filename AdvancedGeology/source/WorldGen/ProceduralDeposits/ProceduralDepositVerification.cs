using System;
using System.Collections.Generic;
using System.Linq;
using Vintagestory.API.Common;

namespace AdvancedGeology.WorldGen.ProceduralDeposits;

public readonly record struct ProceduralDepositVerificationResult(
    int VoxelCount,
    int DeepPrimaryCount,
    int MarginalOxideCount,
    int MarginalEnrichedCount,
    int ErodedOxideCount,
    ulong FeatureId);

public readonly record struct EpithermalDepositVerificationResult(
    int SurfaceExposureColumns,
    int AttemptedAboveSurfaceVoxels,
    int RetainedBelowSurfaceVoxels,
    int GeyseritePipeSamples,
    int NativeSilverSamples,
    int AcanthiteSamples,
    int ElectrumSamples,
    int MinimumCapThickness,
    int MaximumCapThickness,
    int SmearSeedCount,
    int SmearSeedSampleCount,
    int MinimumSmearHeight,
    int MaximumSmearHeight,
    int FlatSmearFootprint,
    int SlopedSmearFootprint);

public readonly record struct SplineDepositVerificationResult(
    int SegmentCount,
    int BranchCount,
    int PodCount,
    int InsideVoxels,
    int IndexMismatches,
    int WallVoxels,
    int SchorlVoxels,
    int OrthoclaseVoxels,
    int IntermediateVoxels,
    int CoreVoxels,
    int AlbiteVoxels,
    int LepidoliteVoxels,
    int PolluciteVoxels,
    int SpodumeneVoxels,
    int BerylVoxels,
    int CassiteriteVoxels,
    int TantaliteVoxels,
    int ColumbiteVoxels,
    int MaxHorizontalOffset,
    int MaxVerticalOffset,
    int PoorVoxels,
    int MediumVoxels,
    int RichVoxels,
    int BountifulVoxels,
    double CoreMeanGrade,
    double WallMeanGrade);

public readonly record struct TerrainOrientedPlateVerificationResult(
    double FittedGradientX,
    double FittedGradientZ,
    double MaximumFramePlaneError,
    int MemberCount,
    double MainDipDegrees,
    int FlatInsideSamples,
    int TiltedRetainedSamples,
    int PrimarySamples,
    int GangueSamples,
    int HaloSamples,
    int AlbiteSamples,
    int BrecciaSamples,
    int SecondarySamples,
    int GradeSpread,
    double MaximumVerticalTilt);

public readonly record struct SurfaceWeatheringVerificationResult(
    int PegmatiteRedClaySamples,
    int PegmatiteKaoliniteSamples,
    int KirunaGossanSamples,
    int KirunaKaoliniteSamples,
    int MartiteShallowSamples,
    int MartiteDeepSamples,
    bool QuartzSurfaceLayerValid);

public readonly record struct OoliticIronstoneVerificationResult(
    int BedCount,
    int InsideSamples,
    int HematiteSamples,
    int LimoniteSamples,
    int SideriteSamples,
    int MagnetiteSamples,
    int BrecciaSamples,
    int GradeSpread,
    bool Deterministic);

public readonly record struct SchwazTetrahedriteVerificationResult(
    int MantoLensCount,
    int FeederFaultCount,
    int TetrahedriteSamples,
    int BariteSamples,
    int ChalcopyriteSamples,
    int MalachiteSamples,
    int AzuriteSamples,
    int ChalcociteSamples,
    int RedGossanSamples,
    int GreenGossanSamples,
    int BountifulSupergeneSamples,
    bool Deterministic);

public readonly record struct MetasomaticSideriteVerificationResult(
    int BodyCount,
    int VeinCount,
    int SideriteSamples,
    int HematiteSamples,
    int LimoniteSamples,
    int QuartzSamples,
    int PyriteSamples,
    int BrecciaSamples,
    int TraceSulfideSamples,
    bool Deterministic);

public readonly record struct BandedIronFormationVerificationResult(
    int BandCount,
    int EnrichmentCount,
    int MagnetiteSamples,
    int HematiteSamples,
    int HighGradeHematiteSamples,
    int SideriteSamples,
    int PyriteSamples,
    int ChertSamples,
    int JaspiliteSamples,
    int LimoniteSamples,
    int BrecciaSamples,
    bool Deterministic);

public readonly record struct MississippiValleyVerificationResult(
    int BrecciaPipeCount,
    int MantoLobeCount,
    int GalenaSamples,
    int SphaleriteSamples,
    int CerussiteSamples,
    int SmithsoniteSamples,
    int LimoniteSamples,
    int BariteSamples,
    int FluoriteSamples,
    int BrecciaSamples,
    int RedGossanSamples,
    int GreyGossanSamples,
    int YellowGossanSamples,
    bool Deterministic);

public readonly record struct VmsVerificationResult(
    string Code,
    int LensCount,
    int StringerCount,
    int ChalcopyriteSamples,
    int SphaleriteSamples,
    int PyriteSamples,
    int ChertSamples,
    int ChertSpeckleSamples,
    double SpeckleFraction,
    int ChalcociteSamples,
    int SmithsoniteSamples,
    int LimoniteSamples,
    int RedGossanSamples,
    int YellowGossanSamples,
    int ExtraSamples,
    bool Deterministic);
public readonly record struct GradedOreVerificationResult(
    int GreisenGradeSpread,
    int CarbonatiteGradeSpread,
    int PrabornaGradeSpread,
    int MetasomaticSideriteGradeSpread,
    int ArsenideGradeSpread,
    bool Deterministic);


public readonly record struct AlluvialLigniteVerificationResult(
    int UnitCount,
    int PeatLensCount,
    int MinorBedCount,
    int LigniteSamples,
    int WoodyLigniteSamples,
    int PyriticLigniteSamples,
    int HighAshLigniteSamples,
    int HighAshPrunedSamples,
    double HighAshPrunedFraction,
    int ClayPartingSamples,
    int ChannelSandSamples,
    bool Deterministic);

public readonly record struct FoldedAnthraciteVerificationResult(
    int SeamCount,
    int AnthraciteSamples,
    int CrushedAnthraciteSamples,
    int SlatePartingSamples,
    int RepeatedLimbSamples,
    int RepeatedColumnCount,
    bool Deterministic);

public readonly record struct ReopenedArsenideVeinVerificationResult(
    int PrimaryVeinCount,
    int LateCrosscutCount,
    int UraniniteSamples,
    int NativeBismuthSamples,
    int NativeSilverSamples,
    int NickelineSamples,
    int CobaltiteSamples,
    int QuartzSamples,
    int FluoriteSamples,
    int BariteSamples,
    int CarbonateSamples,
    int TorberniteSamples,
    int AnnabergiteSamples,
    int ErythriteSamples,
    int RedGossanSamples,
    int GreenGossanSamples,
    int PinkGossanSamples,
    double CobaltiteSpeckleFraction,
    bool Deterministic);

public readonly record struct SixDepositVerificationResult(
    int CoalSeams,
    int CoalSamples,
    double CoalHighAshPrunedFraction,
    int CoalPartingSamples,
    int CoalSeatEarthSamples,
    int CoalFireclaySamples,
    int CoalChannelSamples,
    int CinnabarSamples,
    int CinnabarQuartzSamples,
    int CinnabarPyriteSamples,
    int TinCassiteriteSamples,
    int TinStanniteSamples,
    int TinBismuthiniteSamples,
    int TinStibniteSamples,
    int TinTourmalineSamples,
    double TinStibniteSpeckleFraction,
    int TinGradeSpread,
    int BorateSamples,
    int BorateGypsumSamples,
    int SkarnCassiteriteSamples,
    int SkarnScheeliteSamples,
    int SkarnFluoriteSamples,
    double SkarnFluoriteSpeckleFraction,
    int SkarnGradeSpread,
    int AluniteSamples,
    int AluniteQuartzSamples,
    int AluniteGypsumSamples,
    bool Deterministic);

public readonly record struct CobaltArsenideVeinVerificationResult(
    int PrincipalVeinCount,
    int SplayCount,
    int NickelineSamples,
    int CobaltiteSamples,
    double CobaltiteSpeckleFraction,
    int NativeSilverSamples,
    int NativeBismuthSamples,
    int AcanthiteSamples,
    int PyriteSamples,
    int CarbonateSamples,
    int ErythriteSamples,
    int AnnabergiteSamples,
    int GradeSpread,
    bool NoUraniumFacies,
    bool Deterministic);

public readonly record struct IocgBrecciaVerificationResult(
    int BrecciaCount,
    int FaultCount,
    int BorniteSamples,
    int ChalcopyriteSamples,
    int ChalcociteSamples,
    int HematiteSamples,
    int MagnetiteSamples,
    int PyriteSamples,
    int BrecciaClastSamples,
    int ApatiteSamples,
    int FluoriteSamples,
    int BariteSamples,
    int GradeSpread,
    bool RoofPruned,
    int AlterationSamples,
    bool Deterministic);

public readonly record struct SudburyContactNickelVerificationResult(
    int EmbaymentCount,
    int OrePodCount,
    int FootwallVeinCount,
    int OffsetDikeCount,
    int PentlanditeSamples,
    int ChalcopyriteSamples,
    int PyrrhotiteSamples,
    int MagnetiteSamples,
    int QuartzDioriteSamples,
    int GangueSamples,
    int XenolithSamples,
    int PgmEnrichedSamples,
    int GradeSpread,
    bool HostSequencePruned,
    bool Deterministic);

public readonly record struct LacustrineBorateVerificationResult(
    int LensCount,
    int BoraxSamples,
    int KerniteSamples,
    int UlexiteSamples,
    int ColemaniteSamples,
    int TronaSamples,
    int GypsumSamples,
    int ClayPartingSamples,
    bool HostPruned,
    bool TuffPruned,
    bool NoGrades,
    bool Deterministic);

public readonly record struct PorphyryCopperMolyVerificationResult(
    int VeinletCount,
    int BorniteSamples,
    int ChalcopyriteSamples,
    int MolybdeniteSamples,
    int ChalcociteSamples,
    int QuartzSamples,
    int PyriteSamples,
    int DisseminatedSamples,
    int RedGossanSamples,
    int LimoniteSamples,
    int GradeSpread,
    bool SupergeneBoundsValid,
    bool Deterministic);

public readonly record struct StratiformCopperVerificationResult(
    int LensCount,
    int FaultCount,
    int NativeCopperSamples,
    int ChalcociteSamples,
    int BorniteSamples,
    int ChalcopyriteSamples,
    int MalachiteSamples,
    int AzuriteSamples,
    int PyriteSamples,
    int CarbonateSamples,
    int RedGossanSamples,
    int LimoniteSamples,
    int GradeSpread,
    bool OreRequiresLens,
    bool Deterministic);

public readonly record struct ClimateFilterVerificationResult(
    bool DisabledBypassesFilter,
    bool InclusiveBounds,
    bool RejectsOutsideBounds,
    bool RejectsInvalidBounds);

public readonly record struct ProceduralEngineVerificationResult(
    bool PriorityOrderValid,
    bool TemplateRegistryValid,
    bool ExclusiveOreGenerationValid,
    bool IntrusionPolicyValid,
    bool TerrainClampValid,
    bool BuriedWeatheringValid,
    bool LooseSedimentProtected);

public static class ProceduralDepositVerification
{
    public static ProceduralDepositVerificationResult Run()
    {
        const long worldSeed = 24681357;
        var definition = new ProceduralDepositDefinition
        {
            Code = "verification-lens",
            Geometry = new EllipsoidGeometryDefinition
            {
                RadiusX = 40,
                RadiusY = 10,
                RadiusZ = 8,
                EdgeNoise = 0
            },
            Supergene = new SupergeneDefinition
            {
                Enabled = true,
                OxidationDepth = 8,
                EnrichmentThickness = 4
            }
        };
        ulong codeHash = ProceduralDepositMath.HashString(definition.Code);
        ulong featureId = ProceduralDepositMath.FeatureId(worldSeed, codeHash, -2, 3);
        if (featureId != ProceduralDepositMath.FeatureId(worldSeed, codeHash, -2, 3))
        {
            throw new InvalidOperationException("Procedural feature identity is not deterministic");
        }
        if (featureId == ProceduralDepositMath.FeatureId(worldSeed, codeHash, -1, 3))
        {
            throw new InvalidOperationException("Adjacent placement cells produced the same feature identity");
        }
        if (ProceduralDepositMath.FloorDiv(-1, 32) != -1 || ProceduralDepositMath.FloorMod(-1, 32) != 31)
        {
            throw new InvalidOperationException("Negative chunk coordinate division is incorrect");
        }

        EllipsoidGeometryDefinition geometry = definition.Geometry;
        var instance = new ProceduralDepositInstance(
            featureId,
            0,
            100,
            0,
            1,
            0,
            0.10,
            -0.04);

        HashSet<(int X, int Y, int Z)> forward = GenerateByChunkOrder(instance, geometry, new[] { -2, -1, 0, 1 });
        HashSet<(int X, int Y, int Z)> reverse = GenerateByChunkOrder(instance, geometry, new[] { 1, 0, -1, -2 });
        if (!forward.SetEquals(reverse))
        {
            throw new InvalidOperationException("Chunk order changed procedural deposit geometry");
        }

        CountWeathering(instance, geometry, 140, definition.Supergene, out int deepPrimary, out _, out _);
        CountWeathering(instance, geometry, 105, definition.Supergene, out _, out int marginalOxide, out int marginalEnriched);
        CountWeathering(instance, geometry, 94, definition.Supergene, out _, out int erodedOxide, out _);
        if (deepPrimary == 0 || marginalOxide == 0 || marginalEnriched == 0 || erodedOxide == 0)
        {
            throw new InvalidOperationException("Deep, marginal, and eroded weathering scenarios were not all represented");
        }

        var slopeSettings = new TerrainOrientationDefinition
        {
            DipFactor = 0.5,
            IgnoreBelowTerrainSlopeDeg = 0,
            MaxTerrainSlopeDeg = 20,
            MinAppliedDipDeg = 0,
            MaxAppliedDipDeg = 8
        };
        ProceduralTerrainPlane limited = ProceduralDepositMath.LimitTerrainPlane(
            new ProceduralTerrainPlane(Math.Tan(20 * Math.PI / 180.0), 0),
            slopeSettings);
        double limitedDegrees = limited.SlopeRadians * 180.0 / Math.PI;
        if (Math.Abs(limitedDegrees - 8.0) > 0.0001)
        {
            throw new InvalidOperationException("Terrain-derived dip limits were not applied correctly");
        }

        return new ProceduralDepositVerificationResult(
            forward.Count,
            deepPrimary,
            marginalOxide,
            marginalEnriched,
            erodedOxide,
            featureId);
    }

    public static EpithermalDepositVerificationResult RunEpithermal()
    {
        const int surfaceY = 110;
        ulong featureId = ProceduralDepositMath.FeatureId(
            24681357,
            ProceduralDepositMath.HashString("epithermal-verification"),
            2,
            -3);
        var settings = new EpithermalVeinDefinition();
        var deepInstance = new ProceduralDepositInstance(featureId, 0, 50, 0, 1, 0, 0, 0);
        var highInstance = new ProceduralDepositInstance(featureId, 0, 135, 0, 1, 0, 0, 0);
        EpithermalVeinPlan deepPlan = EpithermalVeinPlan.Create(deepInstance, settings);
        EpithermalVeinPlan highPlan = EpithermalVeinPlan.Create(highInstance, settings);
        EpithermalVeinPlan repeatedPlan = EpithermalVeinPlan.Create(highInstance, settings);

        int deepExposure = 0;
        int highExposure = 0;
        int minimumCap = int.MaxValue;
        int maximumCap = int.MinValue;
        for (int x = -60; x <= 60; x++)
        {
            for (int z = -60; z <= 60; z++)
            {
                if (deepPlan.Sample(x, surfaceY, z).Inside) deepExposure++;
                EpithermalVeinSample highSample = highPlan.Sample(x, surfaceY, z);
                EpithermalVeinSample repeatedSample = repeatedPlan.Sample(x, surfaceY, z);
                if (highSample != repeatedSample)
                {
                    throw new InvalidOperationException("Epithermal plan reconstruction is not deterministic");
                }
                if (!highSample.Inside) continue;

                highExposure++;
                int cap = highPlan.GetGeyseriteCapThickness(x, z, settings);
                minimumCap = Math.Min(minimumCap, cap);
                maximumCap = Math.Max(maximumCap, cap);
            }
        }
        if (deepExposure != 0 || highExposure == 0 || minimumCap < 3 || maximumCap > 6)
        {
            throw new InvalidOperationException("Epithermal elevation or cap-thickness scenarios are invalid");
        }

        int attemptedAbove = 0;
        int retainedBelow = 0;
        for (int x = -45; x <= 45; x += 2)
        {
            for (int z = -45; z <= 45; z += 2)
            {
                for (int y = 70; y <= 178; y += 2)
                {
                    if (!highPlan.Sample(x, y, z).Inside) continue;
                    if (y > surfaceY) attemptedAbove++;
                    else retainedBelow++;
                }
            }
        }
        if (attemptedAbove == 0 || retainedBelow == 0)
        {
            throw new InvalidOperationException("High epithermal prototype did not cross the erosion surface");
        }

        int pipes = 0;
        int nativeSilver = 0;
        int acanthite = 0;
        int electrum = 0;
        var centerSample = new EpithermalVeinSample(true, 0.2, 0.2, 0);
        for (int x = -24; x <= 24; x++)
        {
            for (int z = -24; z <= 24; z++)
            {
                _ = ProceduralDepositWorldGenSystem.SelectEpithermalZone(
                    featureId,
                    settings,
                    centerSample,
                    x,
                    surfaceY - 5,
                    z,
                    5,
                    out bool pipe);
                if (pipe) pipes++;

                EpithermalMaterialZone supergene = ProceduralDepositWorldGenSystem.SelectEpithermalZone(
                    featureId,
                    settings,
                    centerSample,
                    x,
                    surfaceY - 16,
                    z,
                    16,
                    out _);
                if (supergene == EpithermalMaterialZone.NativeSilver) nativeSilver++;
                if (supergene == EpithermalMaterialZone.Acanthite) acanthite++;

                EpithermalMaterialZone primary = ProceduralDepositWorldGenSystem.SelectEpithermalZone(
                    featureId,
                    settings,
                    centerSample,
                    x,
                    surfaceY - 30,
                    z,
                    30,
                    out _);
                if (primary == EpithermalMaterialZone.Electrum) electrum++;
            }
        }
        if (pipes == 0 || nativeSilver == 0 || acanthite == 0 || electrum == 0)
        {
            throw new InvalidOperationException("Epithermal material zones did not produce every required phase");
        }

        int smearSeeds = 0;
        int smearSamples = 0;
        int minimumSmearHeight = int.MaxValue;
        int maximumSmearHeight = int.MinValue;
        for (int x = -80; x <= 80; x++)
        {
            for (int z = -80; z <= 80; z++)
            {
                smearSamples++;
                if (!ProceduralDepositMath.IsSmearSeed(featureId, x, z, settings.SmearSeedFraction)) continue;

                smearSeeds++;
                int height = ProceduralDepositMath.SmearHeight(
                    featureId,
                    x,
                    z,
                    settings.SmearHeightMin,
                    settings.SmearHeightMax);
                minimumSmearHeight = Math.Min(minimumSmearHeight, height);
                maximumSmearHeight = Math.Max(maximumSmearHeight, height);
            }
        }

        double seedRate = (double)smearSeeds / smearSamples;
        if (Math.Abs(seedRate - settings.SmearSeedFraction) > 0.03)
        {
            throw new InvalidOperationException(
                $"Smear seed rate {seedRate:0.###} does not match the configured every-fourth fraction");
        }
        if (minimumSmearHeight != settings.SmearHeightMin || maximumSmearHeight != settings.SmearHeightMax)
        {
            throw new InvalidOperationException("Smear heights did not span the configured 5-10 block range");
        }

        const int seedSurface = 100;
        int apexY = seedSurface + 1;
        int flatFootprint = CountSmearFootprint(apexY, 8, seedSurface);
        int slopedFootprint = CountSmearFootprint(apexY, 8, seedSurface - 5);
        if (flatFootprint != 9 || slopedFootprint <= flatFootprint)
        {
            throw new InvalidOperationException("Smear pyramid footprint does not widen with descent below the apex");
        }
        if (CountSmearFootprint(apexY, 8, seedSurface + 2) != 0)
        {
            throw new InvalidOperationException("Smear pyramid covered ground above its apex");
        }
        if (CountSmearFootprint(apexY, 8, seedSurface - 12) != 0)
        {
            throw new InvalidOperationException("Smear pyramid extended below its configured height");
        }

        return new EpithermalDepositVerificationResult(
            highExposure,
            attemptedAbove,
            retainedBelow,
            pipes,
            nativeSilver,
            acanthite,
            electrum,
            minimumCap,
            maximumCap,
            smearSeeds,
            smearSamples,
            minimumSmearHeight,
            maximumSmearHeight,
            flatFootprint,
            slopedFootprint);
    }

    public static SplineDepositVerificationResult RunSpline()
    {
        const int centerY = 120;
        ulong featureId = ProceduralDepositMath.FeatureId(
            1357924680,
            ProceduralDepositMath.HashString("spline-verification"),
            5,
            -2);
        var settings = new SplineNetworkDefinition();
        var instance = new ProceduralDepositInstance(featureId, 0, centerY, 0, 1, 0, 0, 0);
        SplineNetworkPlan plan = SplineNetworkPlan.Create(instance, settings)
            ?? throw new InvalidOperationException("Spline plan creation failed for the reference instance");
        SplineNetworkPlan repeated = SplineNetworkPlan.Create(instance, settings)
            ?? throw new InvalidOperationException("Spline plan reconstruction failed");

        var zoneCounts = new int[13];
        var gradeCounts = new int[CompiledProceduralDeposit.GradeCount];
        long coreGradeSum = 0;
        int coreGradeVoxels = 0;
        long wallGradeSum = 0;
        int wallGradeVoxels = 0;
        int inside = 0;
        int mismatches = 0;
        int hangingWallViolations = 0;
        int footwallViolations = 0;
        int maxHorizontal = 0;
        int maxVertical = 0;
        int limit = settings.HorizontalRadius + 6;
        int verticalLimit = settings.VerticalHalfHeight + 6;

        for (int x = -limit; x <= limit; x += 2)
        {
            for (int z = -limit; z <= limit; z += 2)
            {
                for (int y = centerY - verticalLimit; y <= centerY + verticalLimit; y += 2)
                {
                    SplineNetworkSample indexed = plan.Sample(x, y, z);
                    SplineNetworkSample exhaustive = plan.SampleExhaustive(x, y, z);
                    if (indexed.Inside != exhaustive.Inside) mismatches++;
                    if (indexed != repeated.Sample(x, y, z))
                    {
                        throw new InvalidOperationException("Spline plan reconstruction is not deterministic");
                    }
                    if (!indexed.Inside) continue;

                    inside++;
                    maxHorizontal = Math.Max(maxHorizontal, Math.Max(Math.Abs(x), Math.Abs(z)));
                    maxVertical = Math.Max(maxVertical, Math.Abs(y - centerY));

                    SplineMaterialZone zone = ProceduralDepositWorldGenSystem.SelectSplineZone(
                        featureId,
                        settings,
                        indexed,
                        x,
                        y,
                        z);
                    zoneCounts[(int)zone]++;
                    int grade = ProceduralDepositWorldGenSystem.SelectSplineGrade(featureId, indexed, x, y, z);
                    gradeCounts[grade]++;
                    if (zone == SplineMaterialZone.Core)
                    {
                        coreGradeSum += grade;
                        coreGradeVoxels++;
                    }
                    else if (zone is SplineMaterialZone.Wall or SplineMaterialZone.Schorl)
                    {
                        wallGradeSum += grade;
                        wallGradeVoxels++;
                    }

                    switch (zone)
                    {
                        case SplineMaterialZone.Pollucite:
                        case SplineMaterialZone.Lepidolite:
                        case SplineMaterialZone.Beryl:
                            if (indexed.SignedThickness <= 0) hangingWallViolations++;
                            break;
                        case SplineMaterialZone.Spodumene:
                            // Spodumene laths straddle the core boundary in the reference
                            // generator, so only the band membership is guaranteed.
                            if (Math.Abs(indexed.SignedThickness - 0.08) >= 0.28) hangingWallViolations++;
                            break;
                        case SplineMaterialZone.Cassiterite:
                        case SplineMaterialZone.Tantalite:
                        case SplineMaterialZone.Columbite:
                            if (indexed.SignedThickness >= 0) footwallViolations++;
                            break;
                    }
                }
            }
        }

        if (mismatches != 0)
        {
            throw new InvalidOperationException("Spline spatial index disagrees with exhaustive segment evaluation");
        }
        if (inside == 0 || plan.SegmentCount == 0 || plan.BranchCount < 2)
        {
            throw new InvalidOperationException("Spline plan produced no branching body");
        }
        if (maxHorizontal > settings.HorizontalRadius || maxVertical > settings.VerticalHalfHeight)
        {
            throw new InvalidOperationException("Spline body exceeded its declared placement bounds");
        }
        for (int zone = 0; zone < zoneCounts.Length; zone++)
        {
            if (zoneCounts[zone] == 0)
            {
                throw new InvalidOperationException(
                    $"Spline zoning produced no {(SplineMaterialZone)zone} voxels");
            }
        }
        if (hangingWallViolations != 0 || footwallViolations != 0)
        {
            throw new InvalidOperationException(
                "Spline zoning placed Li-Cs-Be or Sn-Ta-Nb minerals on the wrong dike wall");
        }
        for (int grade = 0; grade < gradeCounts.Length; grade++)
        {
            if (gradeCounts[grade] == 0)
            {
                throw new InvalidOperationException($"Spline grading produced no grade-{grade} voxels");
            }
        }
        if (coreGradeVoxels == 0 || wallGradeVoxels == 0)
        {
            throw new InvalidOperationException("Spline grading has no core or wall sample to compare");
        }

        double coreMeanGrade = (double)coreGradeSum / coreGradeVoxels;
        double wallMeanGrade = (double)wallGradeSum / wallGradeVoxels;
        if (coreMeanGrade <= wallMeanGrade)
        {
            throw new InvalidOperationException("Spline ore grade does not increase toward the dike centre");
        }

        // Tips and roots must terminate: material may not continue along the trunk axis
        // past the plan extent even though the nearest segment endpoint stays close.
        for (int distance = settings.HorizontalRadius + 1; distance <= settings.HorizontalRadius + 24; distance++)
        {
            for (int y = centerY - verticalLimit; y <= centerY + verticalLimit; y++)
            {
                if (plan.SampleExhaustive(distance, y, 0).Inside
                    || plan.SampleExhaustive(-distance, y, 0).Inside
                    || plan.SampleExhaustive(0, y, distance).Inside
                    || plan.SampleExhaustive(0, y, -distance).Inside)
                {
                    throw new InvalidOperationException("Spline body extends past its horizontal bound");
                }
            }
        }

        return new SplineDepositVerificationResult(
            plan.SegmentCount,
            plan.BranchCount,
            plan.PodCount,
            inside,
            mismatches,
            zoneCounts[(int)SplineMaterialZone.Wall],
            zoneCounts[(int)SplineMaterialZone.Schorl],
            zoneCounts[(int)SplineMaterialZone.Orthoclase],
            zoneCounts[(int)SplineMaterialZone.Intermediate],
            zoneCounts[(int)SplineMaterialZone.Core],
            zoneCounts[(int)SplineMaterialZone.Albite],
            zoneCounts[(int)SplineMaterialZone.Lepidolite],
            zoneCounts[(int)SplineMaterialZone.Pollucite],
            zoneCounts[(int)SplineMaterialZone.Spodumene],
            zoneCounts[(int)SplineMaterialZone.Beryl],
            zoneCounts[(int)SplineMaterialZone.Cassiterite],
            zoneCounts[(int)SplineMaterialZone.Tantalite],
            zoneCounts[(int)SplineMaterialZone.Columbite],
            maxHorizontal,
            maxVertical,
            gradeCounts[0],
            gradeCounts[1],
            gradeCounts[2],
            gradeCounts[3],
            coreMeanGrade,
            wallMeanGrade);
    }

    public static TerrainOrientedPlateVerificationResult RunTerrainOrientedPlate()
    {
        const double expectedX = 0.08;
        const double expectedZ = -0.045;
        var accumulator = new ProceduralTerrainPlaneAccumulator();
        for (int z = -32; z <= 32; z += 4)
        {
            for (int x = -32; x <= 32; x += 4)
            {
                if (x * x + z * z > 32 * 32) continue;
                double relief = Math.Sin(x * 0.41) * Math.Cos(z * 0.27) * 2.5;
                accumulator.Add(x, z, 100 + expectedX * x + expectedZ * z + relief);
            }
        }
        if (!accumulator.TrySolve(out ProceduralTerrainPlane fitted)
            || Math.Abs(fitted.GradientX - expectedX) > 0.01
            || Math.Abs(fitted.GradientZ - expectedZ) > 0.01)
        {
            throw new InvalidOperationException("Average terrain plane did not recover the regional slope through local relief");
        }

        ProceduralTerrainFrame frame = ProceduralTerrainFrame.FromGradients(
            fitted.GradientX,
            fitted.GradientZ);
        double maximumPlaneError = 0;
        double maximumVerticalTilt = 0;
        for (int z = -40; z <= 40; z += 4)
        {
            for (int x = -40; x <= 40; x += 4)
            {
                (double worldX, double worldY, double worldZ) = frame.ToWorld(x, 0, z);
                double planeY = fitted.GradientX * worldX + fitted.GradientZ * worldZ;
                maximumPlaneError = Math.Max(maximumPlaneError, Math.Abs(worldY - planeY));
                maximumVerticalTilt = Math.Max(maximumVerticalTilt, Math.Abs(worldY));
            }
        }
        if (maximumPlaneError > 1e-9 || maximumVerticalTilt < 3)
        {
            throw new InvalidOperationException("Terrain frame is not tangent to the fitted average surface");
        }

        ulong featureId = ProceduralDepositMath.FeatureId(
            97531,
            ProceduralDepositMath.HashString("sheeted-plate-verification"),
            -3,
            4);
        var settings = new SheetedPlateDefinition();
        var flatInstance = new ProceduralDepositInstance(featureId, 0, 100, 0, 1, 0, 0, 0);
        var tiltedInstance = new ProceduralDepositInstance(
            featureId,
            0,
            100,
            0,
            1,
            0,
            fitted.GradientX,
            fitted.GradientZ);
        SheetedPlatePlan flat = SheetedPlatePlan.Create(flatInstance, settings);
        SheetedPlatePlan tilted = SheetedPlatePlan.Create(tiltedInstance, settings);
        if (flat.MemberCount != tilted.MemberCount || flat.MemberCount < 2)
        {
            throw new InvalidOperationException("Terrain tilt changed deterministic plate-family membership");
        }
        if (flat.MainDipDegrees < settings.MainDipMinDeg
            || flat.MainDipDegrees > settings.MainDipMaxDeg
            || flat.MainDipDegrees > 20)
        {
            throw new InvalidOperationException("Default sheeted plate is not intrinsically shallow");
        }

        int flatInside = 0;
        int retained = 0;
        int primary = 0;
        int gangue = 0;
        int halo = 0;
        int albite = 0;
        int breccia = 0;
        int secondary = 0;
        var plateGrades = new bool[CompiledProceduralDeposit.GradeCount];
        for (int x = -64; x <= 64; x += 2)
        {
            for (int z = -64; z <= 64; z += 2)
            {
                for (int y = 52; y <= 148; y += 2)
                {
                    SheetedPlateSample sample = flat.Sample(x, y, z);
                    if (!sample.Inside)
                    {
                        if (!sample.InHalo) continue;

                        halo++;
                        switch (ProceduralDepositWorldGenSystem.SelectSheetedPlateZone(
                            featureId,
                            settings,
                            sample,
                            x,
                            y,
                            z))
                        {
                            case SheetedPlateZone.Albite:
                                albite++;
                                break;
                            case SheetedPlateZone.Breccia:
                                breccia++;
                                break;
                        }
                        continue;
                    }
                    flatInside++;
                    SheetedPlateZone zone = ProceduralDepositWorldGenSystem.SelectSheetedPlateZone(
                        featureId,
                        settings,
                        sample,
                        x,
                        y,
                        z);
                    if (zone == SheetedPlateZone.Primary) primary++;
                    if (zone is SheetedPlateZone.Gangue or SheetedPlateZone.RareGangue) gangue++;
                    if (zone == SheetedPlateZone.Secondary) secondary++;
                    plateGrades[ProceduralDepositWorldGenSystem.SelectSheetedPlateGrade(
                        featureId,
                        sample,
                        x,
                        y,
                        z)] = true;

                    (double localX, double localY, double localZ) = flat.TerrainFrame.ToLocal(x, y - 100, z);
                    (double tx, double ty, double tz) = frame.ToWorld(localX, localY, localZ);
                    if (tilted.Sample(tx, 100 + ty, tz).Inside)
                    {
                        retained++;
                    }
                }
            }
        }
        int gradeSpread = 0;
        foreach (bool seen in plateGrades)
        {
            if (seen) gradeSpread++;
        }
        if (flatInside == 0 || primary == 0 || gangue == 0 || secondary == 0)
        {
            throw new InvalidOperationException("Sheeted-plate template did not produce magnetite, apatite, and hematite samples");
        }
        if (halo == 0 || albite == 0 || breccia == 0)
        {
            throw new InvalidOperationException("Sheeted-plate halo did not produce albite and breccia samples");
        }
        if (gradeSpread < CompiledProceduralDeposit.GradeCount)
        {
            throw new InvalidOperationException($"Sheeted-plate grading produced only {gradeSpread} distinct grades");
        }
        if (retained < flatInside * 0.99)
        {
            throw new InvalidOperationException(
                $"Terrain-aligned plate retained only {retained}/{flatInside} local samples after rigid transformation");
        }
        return new TerrainOrientedPlateVerificationResult(
            fitted.GradientX,
            fitted.GradientZ,
            maximumPlaneError,
            flat.MemberCount,
            flat.MainDipDegrees,
            flatInside,
            retained,
            primary,
            gangue,
            halo,
            albite,
            breccia,
            secondary,
            gradeSpread,
            maximumVerticalTilt);
    }
    public static SurfaceWeatheringVerificationResult RunSurfaceWeathering()
    {
        var weathering = new SurfaceWeatheringDefinition { Enabled = true };
        const int redClaySlot = 11;
        const int kaoliniteSlot = 12;
        const int gossanSlot = 13;

        int pegmatiteRed = 0;
        int pegmatiteKaolinite = 0;
        foreach (SplineMaterialZone zone in new[]
                 {
                     SplineMaterialZone.Intermediate,
                     SplineMaterialZone.Albite,
                     SplineMaterialZone.Spodumene,
                     SplineMaterialZone.Pollucite,
                     SplineMaterialZone.Schorl,
                     SplineMaterialZone.Lepidolite,
                     SplineMaterialZone.Beryl,
                     SplineMaterialZone.Cassiterite,
                     SplineMaterialZone.Tantalite,
                     SplineMaterialZone.Columbite
                 })
        {
            int depthOne = ProceduralDepositWorldGenSystem.GetPegmatiteWeatheredSlot(
                weathering,
                zone,
                1,
                redClaySlot,
                kaoliniteSlot);
            int depthTwo = ProceduralDepositWorldGenSystem.GetPegmatiteWeatheredSlot(
                weathering,
                zone,
                2,
                redClaySlot,
                kaoliniteSlot);
            if (depthOne == redClaySlot) pegmatiteRed++;
            if (depthTwo == kaoliniteSlot) pegmatiteKaolinite++;
        }
        if (pegmatiteRed != 8
            || pegmatiteKaolinite != 4
            || ProceduralDepositWorldGenSystem.GetPegmatiteWeatheredSlot(
                weathering,
                SplineMaterialZone.Wall,
                1,
                redClaySlot,
                kaoliniteSlot) != -1
            || ProceduralDepositWorldGenSystem.GetPegmatiteWeatheredSlot(
                weathering,
                SplineMaterialZone.Core,
                1,
                redClaySlot,
                kaoliniteSlot) != -1
            || ProceduralDepositWorldGenSystem.GetPegmatiteWeatheredSlot(
                weathering,
                SplineMaterialZone.Intermediate,
                7,
                redClaySlot,
                kaoliniteSlot) != -1)
        {
            throw new InvalidOperationException($"Pegmatite surface weathering mineral progression is incorrect: red={pegmatiteRed}, kaolinite={pegmatiteKaolinite}");
        }

        int kirunaGossan = 0;
        int kirunaKaolinite = 0;
        foreach (SheetedPlateZone zone in new[]
                 {
                     SheetedPlateZone.Primary,
                     SheetedPlateZone.Gangue,
                     SheetedPlateZone.RareGangue
                 })
        {
            if (ProceduralDepositWorldGenSystem.GetKirunaWeatheredSlot(
                    weathering,
                    zone,
                    1,
                    kaoliniteSlot,
                    gossanSlot) == gossanSlot)
            {
                kirunaGossan++;
            }
            if (ProceduralDepositWorldGenSystem.GetKirunaWeatheredSlot(
                    weathering,
                    zone,
                    3,
                    kaoliniteSlot,
                    gossanSlot) == kaoliniteSlot)
            {
                kirunaKaolinite++;
            }
        }
        if (kirunaGossan != 1
            || kirunaKaolinite != 2
            || ProceduralDepositWorldGenSystem.GetKirunaWeatheredSlot(
                weathering,
                SheetedPlateZone.Gangue,
                4,
                kaoliniteSlot,
                gossanSlot) != -1)
        {
            throw new InvalidOperationException("Kiruna gossan and apatite weathering depths are incorrect");
        }

        const ulong featureId = 0x574541544845524DUL;
        int shallowMartite = 0;
        int deepMartite = 0;
        for (int x = -32; x <= 32; x++)
        {
            for (int z = -32; z <= 32; z++)
            {
                if (ProceduralDepositWorldGenSystem.IsMartitized(featureId, 2, x, 80, z)) shallowMartite++;
                if (ProceduralDepositWorldGenSystem.IsMartitized(featureId, 8, x, 80, z)) deepMartite++;
            }
        }
        if (shallowMartite <= deepMartite || deepMartite == 0)
        {
            throw new InvalidOperationException("Kiruna martitization does not taper from shallow hematite to deep magnetite");
        }

        bool quartzSurfaceLayerValid =
            ProceduralDepositWorldGenSystem.GetPegmatiteWeatheredSlot(
                weathering,
                SplineMaterialZone.Core,
                1,
                redClaySlot,
                kaoliniteSlot) == -1
            && ProceduralDepositWorldGenSystem.GetKirunaWeatheredSlot(
                weathering,
                SheetedPlateZone.Quartz,
                1,
                kaoliniteSlot,
                gossanSlot) == -1;
        if (!quartzSurfaceLayerValid)
        {
            throw new InvalidOperationException("Quartz must remain unaltered in the surface weathering layer");
        }

        return new SurfaceWeatheringVerificationResult(
            pegmatiteRed,
            pegmatiteKaolinite,
            kirunaGossan,
            kirunaKaolinite,
            shallowMartite,
            deepMartite,
            quartzSurfaceLayerValid);
    }

    public static OoliticIronstoneVerificationResult RunOoliticIronstone()
    {
        ulong featureId = ProceduralDepositMath.FeatureId(
            864209,
            ProceduralDepositMath.HashString("oolitic-verification"),
            3,
            -5);
        var settings = new OoliticIronstoneDefinition();
        var instance = new ProceduralDepositInstance(featureId, 0, 100, 0, 1, 0, 0, 0);
        OoliticIronstonePlan plan = OoliticIronstonePlan.Create(instance, settings);
        OoliticIronstonePlan repeated = OoliticIronstonePlan.Create(instance, settings);

        int inside = 0;
        int hematite = 0;
        int limonite = 0;
        int siderite = 0;
        int magnetite = 0;
        int breccia = 0;
        var grades = new bool[CompiledProceduralDeposit.GradeCount];
        bool deterministic = true;

        for (int x = -52; x <= 52; x += 2)
        {
            for (int z = -52; z <= 52; z += 2)
            {
                for (int y = 76; y <= 124; y++)
                {
                    OoliticIronstoneSample sample = plan.Sample(x, y, z);
                    OoliticIronstoneSample repeatedSample = repeated.Sample(x, y, z);
                    if (sample != repeatedSample) deterministic = false;

                    OoliticIronstoneZone zone = ProceduralDepositWorldGenSystem.SelectOoliticIronstoneZone(
                        featureId,
                        sample,
                        x,
                        y,
                        z);
                    switch (zone)
                    {
                        case OoliticIronstoneZone.Hematite:
                            hematite++;
                            break;
                        case OoliticIronstoneZone.Limonite:
                            limonite++;
                            break;
                        case OoliticIronstoneZone.Siderite:
                            siderite++;
                            break;
                        case OoliticIronstoneZone.Magnetite:
                            magnetite++;
                            break;
                        case OoliticIronstoneZone.Breccia:
                            breccia++;
                            break;
                    }
                    if (!sample.Inside) continue;

                    inside++;
                    grades[ProceduralDepositWorldGenSystem.SelectOoliticIronstoneGrade(
                        featureId,
                        sample,
                        x,
                        y,
                        z)] = true;
                }
            }
        }

        int gradeSpread = grades.Count(seen => seen);
        if (!deterministic
            || plan.BedCount < settings.BedMin
            || plan.BedCount > settings.BedMax
            || inside == 0
            || hematite == 0
            || limonite == 0
            || siderite == 0
            || magnetite == 0
            || breccia == 0
            || gradeSpread < 3)
        {
            throw new InvalidOperationException(
                "Oolitic ironstone plan did not produce deterministic stratiform iron facies and breccia");
        }

        return new OoliticIronstoneVerificationResult(
            plan.BedCount,
            inside,
            hematite,
            limonite,
            siderite,
            magnetite,
            breccia,
            gradeSpread,
            deterministic);
    }

    public static SchwazTetrahedriteVerificationResult RunSchwazTetrahedrite()
    {
        ulong featureId = ProceduralDepositMath.FeatureId(
            517333,
            ProceduralDepositMath.HashString("schwaz-verification"),
            -2,
            4);
        var settings = new SchwazTetrahedriteDefinition();
        var instance = new ProceduralDepositInstance(featureId, 0, 100, 0, 1, 0, 0, 0);
        SchwazTetrahedritePlan plan = SchwazTetrahedritePlan.Create(instance, settings);
        SchwazTetrahedritePlan repeated = SchwazTetrahedritePlan.Create(instance, settings);

        int tetrahedrite = 0;
        int barite = 0;
        int chalcopyrite = 0;
        int malachite = 0;
        int azurite = 0;
        int chalcocite = 0;
        int redGossan = 0;
        int greenGossan = 0;
        int bountifulSupergene = 0;
        bool deterministic = true;

        // Sweep several terrain elevations so the dolomite window intersects the surface
        // the way a real height map does; the supergene sequence keys on that intersection.
        foreach (int surfaceY in new[] { 100, 104, 108, 112, 116 })
        {
            for (int x = -46; x <= 46; x += 2)
            {
                for (int z = -46; z <= 46; z += 2)
                {
                    for (int y = 70; y <= surfaceY; y++)
                    {
                    SchwazTetrahedriteSample sample = plan.Evaluate(x, y, z, surfaceY);
                    if (sample != repeated.Evaluate(x, y, z, surfaceY)) deterministic = false;

                    switch (sample.Zone)
                    {
                        case SchwazTetrahedriteZone.Tetrahedrite: tetrahedrite++; break;
                        case SchwazTetrahedriteZone.Barite: barite++; break;
                        case SchwazTetrahedriteZone.Chalcopyrite: chalcopyrite++; break;
                        case SchwazTetrahedriteZone.Malachite: malachite++; break;
                        case SchwazTetrahedriteZone.Azurite: azurite++; break;
                        case SchwazTetrahedriteZone.Chalcocite: chalcocite++; break;
                        case SchwazTetrahedriteZone.Gossan: redGossan++; break;
                        case SchwazTetrahedriteZone.GreenGossan: greenGossan++; break;
                    }

                    int depth = surfaceY - y + 1;
                    if ((depth == 6 || depth == 7) && sample.Grade == 3) bountifulSupergene++;
                    if (depth <= 1
                        && sample.Zone != SchwazTetrahedriteZone.None
                        && sample.Zone != SchwazTetrahedriteZone.Gossan
                        && sample.Zone != SchwazTetrahedriteZone.GreenGossan)
                    {
                        throw new InvalidOperationException(
                            "Schwaz tetrahedrite surface voxel produced ore instead of stained gossan soil");
                    }
                }
            }
        }
        }

        if (!deterministic
            || plan.MantoLensCount != 5
            || plan.FeederFaultCount != 5
            || tetrahedrite == 0
            || barite == 0
            || chalcopyrite == 0
            || malachite == 0
            || azurite == 0
            || chalcocite == 0
            || redGossan == 0
            || greenGossan == 0
            || greenGossan >= redGossan
            || bountifulSupergene == 0)
        {
            throw new InvalidOperationException(
                "Schwaz tetrahedrite plan did not produce the deterministic hypogene and supergene sequence: " +
                $"deterministic={deterministic} lenses={plan.MantoLensCount} faults={plan.FeederFaultCount} " +
                $"tetrahedrite={tetrahedrite} barite={barite} chalcopyrite={chalcopyrite} malachite={malachite} " +
                $"azurite={azurite} chalcocite={chalcocite} gossan={redGossan}/{greenGossan} bountiful={bountifulSupergene}");
        }

        return new SchwazTetrahedriteVerificationResult(
            plan.MantoLensCount,
            plan.FeederFaultCount,
            tetrahedrite,
            barite,
            chalcopyrite,
            malachite,
            azurite,
            chalcocite,
            redGossan,
            greenGossan,
            bountifulSupergene,
            deterministic);
    }

    public static MetasomaticSideriteVerificationResult RunMetasomaticSiderite()
    {
        ulong featureId = ProceduralDepositMath.FeatureId(
            733117,
            ProceduralDepositMath.HashString("metasomatic-siderite-verification"),
            5,
            -3);
        var settings = new MetasomaticSideriteDefinition();
        var instance = new ProceduralDepositInstance(featureId, 0, 100, 0, 1, 0, 0, 0);
        MetasomaticSideritePlan plan = MetasomaticSideritePlan.Create(instance, settings);
        MetasomaticSideritePlan repeated = MetasomaticSideritePlan.Create(instance, settings);

        int siderite = 0;
        int hematite = 0;
        int limonite = 0;
        int quartz = 0;
        int pyrite = 0;
        int breccia = 0;
        int traceSulfide = 0;
        bool deterministic = true;

        for (int x = -44; x <= 44; x++)
        {
            for (int z = -44; z <= 44; z++)
            {
                int surfaceY = 116;
                for (int y = 72; y <= surfaceY; y++)
                {
                    MetasomaticSideriteZone zone = plan.EvaluateZone(x, y, z, surfaceY);
                    if (zone != repeated.EvaluateZone(x, y, z, surfaceY)) deterministic = false;

                    switch (zone)
                    {
                        case MetasomaticSideriteZone.Siderite: siderite++; break;
                        case MetasomaticSideriteZone.Hematite: hematite++; break;
                        case MetasomaticSideriteZone.Limonite: limonite++; break;
                        case MetasomaticSideriteZone.Quartz: quartz++; break;
                        case MetasomaticSideriteZone.Pyrite: pyrite++; traceSulfide++; break;
                        case MetasomaticSideriteZone.Galena:
                        case MetasomaticSideriteZone.Sphalerite:
                        case MetasomaticSideriteZone.Chalcopyrite:
                            traceSulfide++;
                            break;
                        case MetasomaticSideriteZone.Breccia: breccia++; break;
                    }
                }
            }
        }

        if (!deterministic
            || plan.BodyCount < settings.BodyCountMin
            || plan.BodyCount > settings.BodyCountMax
            || plan.VeinCount < settings.VeinCountMin
            || plan.VeinCount > settings.VeinCountMax
            || siderite == 0
            || hematite == 0
            || limonite == 0
            || quartz == 0
            || pyrite == 0
            || breccia == 0
            || traceSulfide <= pyrite)
        {
            throw new InvalidOperationException(
                "Metasomatic siderite plan did not produce deterministic replacement, vein and halo facies");
        }

        return new MetasomaticSideriteVerificationResult(
            plan.BodyCount,
            plan.VeinCount,
            siderite,
            hematite,
            limonite,
            quartz,
            pyrite,
            breccia,
            traceSulfide,
            deterministic);
    }

    public static BandedIronFormationVerificationResult RunBandedIronFormation()
    {
        ulong featureId = ProceduralDepositMath.FeatureId(
            918277,
            ProceduralDepositMath.HashString("banded-iron-verification"),
            -6,
            2);
        var settings = new BandedIronFormationDefinition();
        var instance = new ProceduralDepositInstance(featureId, 0, 100, 0, 1, 0, 0, 0);
        BandedIronFormationPlan plan = BandedIronFormationPlan.Create(instance, settings);
        BandedIronFormationPlan repeated = BandedIronFormationPlan.Create(instance, settings);

        int magnetite = 0;
        int hematite = 0;
        int highGrade = 0;
        int siderite = 0;
        int pyrite = 0;
        int chert = 0;
        int jaspilite = 0;
        int limonite = 0;
        int breccia = 0;
        bool deterministic = true;

        // Sweep terrain elevations so the weathering window (depth <= 8) intersects the
        // banded package the way a real height map does.
        foreach (int surfaceY in new[] { 102, 106, 110, 114, 118 })
        {
            for (int x = -48; x <= 48; x += 2)
            {
                for (int z = -48; z <= 48; z += 2)
                {
                    for (int y = 68; y <= surfaceY; y++)
                    {
                    BandedIronFormationZone zone = plan.EvaluateZone(x, y, z, surfaceY, out int grade);
                    if (zone != repeated.EvaluateZone(x, y, z, surfaceY, out int repeatedGrade)
                        || grade != repeatedGrade)
                    {
                        deterministic = false;
                    }

                    switch (zone)
                    {
                        case BandedIronFormationZone.Magnetite: magnetite++; break;
                        case BandedIronFormationZone.Hematite: hematite++; break;
                        case BandedIronFormationZone.HighGradeHematite: highGrade++; break;
                        case BandedIronFormationZone.Siderite: siderite++; break;
                        case BandedIronFormationZone.Pyrite: pyrite++; break;
                        case BandedIronFormationZone.Chert: chert++; break;
                        case BandedIronFormationZone.Jaspilite: jaspilite++; break;
                        case BandedIronFormationZone.Limonite: limonite++; break;
                        case BandedIronFormationZone.Breccia: breccia++; break;
                    }
                }
            }
        }
        }

        if (!deterministic
            || plan.BandCount < settings.BandCountMin
            || plan.BandCount > settings.BandCountMax
            || plan.EnrichmentCount < settings.EnrichmentCountMin
            || plan.EnrichmentCount > settings.EnrichmentCountMax
            || magnetite == 0
            || hematite == 0
            || highGrade == 0
            || siderite == 0
            || pyrite == 0
            || chert == 0
            || jaspilite == 0
            || limonite == 0
            || breccia == 0)
        {
            throw new InvalidOperationException(
                "Banded iron formation plan did not produce deterministic banding, enrichment and breccia facies: " +
                $"deterministic={deterministic} bands={plan.BandCount} enrichment={plan.EnrichmentCount} " +
                $"magnetite={magnetite} hematite={hematite} highGrade={highGrade} siderite={siderite} " +
                $"pyrite={pyrite} chert={chert} jaspilite={jaspilite} limonite={limonite} breccia={breccia}");
        }

        return new BandedIronFormationVerificationResult(
            plan.BandCount,
            plan.EnrichmentCount,
            magnetite,
            hematite,
            highGrade,
            siderite,
            pyrite,
            chert,
            jaspilite,
            limonite,
            breccia,
            deterministic);
    }

    public static MississippiValleyVerificationResult RunMississippiValley()
    {
        ulong featureId = ProceduralDepositMath.FeatureId(
            442119,
            ProceduralDepositMath.HashString("mvt-verification"),
            4,
            -7);
        var settings = new MississippiValleyDefinition();
        var instance = new ProceduralDepositInstance(featureId, 0, 100, 0, 1, 0, 0, 0);
        MississippiValleyPlan plan = MississippiValleyPlan.Create(instance, settings);
        MississippiValleyPlan repeated = MississippiValleyPlan.Create(instance, settings);

        int galena = 0;
        int sphalerite = 0;
        int cerussite = 0;
        int smithsonite = 0;
        int limonite = 0;
        int barite = 0;
        int fluorite = 0;
        int breccia = 0;
        int redGossan = 0;
        int greyGossan = 0;
        int yellowGossan = 0;
        bool deterministic = true;

        foreach (int surfaceY in new[] { 100, 104, 108, 112 })
        {
            for (int x = -44; x <= 44; x += 2)
            {
                for (int z = -44; z <= 44; z += 2)
                {
                    for (int y = 70; y <= surfaceY; y++)
                    {
                        MississippiValleySample sample = plan.Evaluate(x, y, z, surfaceY, true);
                        if (sample != repeated.Evaluate(x, y, z, surfaceY, true)) deterministic = false;

                        switch (sample.Zone)
                        {
                            case MississippiValleyZone.Galena: galena++; break;
                            case MississippiValleyZone.Sphalerite: sphalerite++; break;
                            case MississippiValleyZone.Cerussite: cerussite++; break;
                            case MississippiValleyZone.Smithsonite: smithsonite++; break;
                            case MississippiValleyZone.Limonite: limonite++; break;
                            case MississippiValleyZone.Barite: barite++; break;
                            case MississippiValleyZone.Fluorite: fluorite++; break;
                            case MississippiValleyZone.Breccia: breccia++; break;
                            case MississippiValleyZone.Gossan: redGossan++; break;
                            case MississippiValleyZone.GreyGossan: greyGossan++; break;
                            case MississippiValleyZone.YellowGossan: yellowGossan++; break;
                        }

                        int depth = surfaceY - y + 1;
                        bool isSoil = sample.Zone == MississippiValleyZone.Gossan
                            || sample.Zone == MississippiValleyZone.GreyGossan
                            || sample.Zone == MississippiValleyZone.YellowGossan;
                        if (isSoil && depth > 3)
                        {
                            throw new InvalidOperationException(
                                $"MVT placed stained gossan soil below depth 3 (depth {depth})");
                        }
                        if (depth > 8
                            && (sample.Zone == MississippiValleyZone.Cerussite
                                || sample.Zone == MississippiValleyZone.Smithsonite))
                        {
                            throw new InvalidOperationException(
                                $"MVT placed a supergene carbonate below depth 8 (depth {depth})");
                        }
                    }
                }
            }
        }

        if (!deterministic
            || galena == 0 || sphalerite == 0 || cerussite == 0 || smithsonite == 0
            || limonite == 0 || barite == 0 || fluorite == 0 || breccia == 0
            || redGossan == 0 || greyGossan == 0 || yellowGossan == 0)
        {
            throw new InvalidOperationException(
                "Mississippi Valley plan did not produce the deterministic hypogene and supergene succession: " +
                $"deterministic={deterministic} galena={galena} sphalerite={sphalerite} cerussite={cerussite} " +
                $"smithsonite={smithsonite} limonite={limonite} barite={barite} fluorite={fluorite} " +
                $"breccia={breccia} gossan={redGossan}/{greyGossan}/{yellowGossan}");
        }

        return new MississippiValleyVerificationResult(
            plan.BrecciaPipeCount,
            plan.MantoLobeCount,
            galena,
            sphalerite,
            cerussite,
            smithsonite,
            limonite,
            barite,
            fluorite,
            breccia,
            redGossan,
            greyGossan,
            yellowGossan,
            deterministic);
    }

    public static VmsVerificationResult RunBimodalFelsicVms()
    {
        ulong featureId = ProceduralDepositMath.FeatureId(
            551277,
            ProceduralDepositMath.HashString("felsic-vms-verification"),
            -3,
            6);
        var settings = new BimodalFelsicVmsDefinition();
        var instance = new ProceduralDepositInstance(featureId, 0, 100, 0, 1, 0, 0, 0);
        BimodalFelsicVmsPlan plan = BimodalFelsicVmsPlan.Create(instance, settings);
        BimodalFelsicVmsPlan repeated = BimodalFelsicVmsPlan.Create(instance, settings);

        int chalcopyrite = 0, sphalerite = 0, pyrite = 0, chert = 0, speckle = 0;
        int chalcocite = 0, smithsonite = 0, limonite = 0, redGossan = 0, yellowGossan = 0;
        int greyGossanAndGalenaCarbonate = 0;
        bool deterministic = true;

        foreach (int surfaceY in new[] { 106, 112, 118, 124 })
        {
            for (int x = -44; x <= 44; x += 2)
            {
                for (int z = -44; z <= 44; z += 2)
                {
                    for (int y = 70; y <= surfaceY; y++)
                    {
                        BimodalFelsicVmsSample sample = plan.Evaluate(x, y, z, surfaceY, true);
                        if (sample != repeated.Evaluate(x, y, z, surfaceY, true)) deterministic = false;

                        switch (sample.Zone)
                        {
                            case BimodalFelsicVmsZone.Chalcopyrite: chalcopyrite++; break;
                            case BimodalFelsicVmsZone.Sphalerite: sphalerite++; break;
                            case BimodalFelsicVmsZone.Pyrite: pyrite++; break;
                            case BimodalFelsicVmsZone.Chert: chert++; break;
                            case BimodalFelsicVmsZone.ChertHematite:
                            case BimodalFelsicVmsZone.ChertMagnetite:
                            case BimodalFelsicVmsZone.ChertPyrite:
                                speckle++;
                                break;
                            case BimodalFelsicVmsZone.Chalcocite: chalcocite++; break;
                            case BimodalFelsicVmsZone.Smithsonite: smithsonite++; break;
                            case BimodalFelsicVmsZone.Limonite: limonite++; break;
                            case BimodalFelsicVmsZone.Gossan: redGossan++; break;
                            case BimodalFelsicVmsZone.YellowGossan: yellowGossan++; break;
                            case BimodalFelsicVmsZone.GreyGossan:
                            case BimodalFelsicVmsZone.Cerussite:
                                greyGossanAndGalenaCarbonate++;
                                break;
                        }

    // Speckle facies must be exactly the top 20% of The grain hash.
                        // The reported fraction is lower than 0.20 because the plain chert-exhalite
                        // facies contributes unspeckled chert outside the three speckled bands.
                        bool isSpeckle = sample.Zone == BimodalFelsicVmsZone.ChertHematite
                            || sample.Zone == BimodalFelsicVmsZone.ChertMagnetite
                            || sample.Zone == BimodalFelsicVmsZone.ChertPyrite;
                        if (isSpeckle)
                        {
                            double fineGrain = BimodalFelsicVmsPlan.Hash3D(
                                x * 2.1 + plan.Seed,
                                (y - 100) * 2.1,
                                z * 2.1);
                            if (fineGrain < 0.80)
                            {
                                throw new InvalidOperationException(
                                    $"Bimodal-felsic VMS placed a chert speckle below the 20% threshold (grain {fineGrain:F3})");
                            }
                        }
                    }
                }
            }
        }

        double fraction = (chert + speckle) > 0 ? (double)speckle / (chert + speckle) : 0.0;
        if (!deterministic
            || chalcopyrite == 0 || sphalerite == 0 || pyrite == 0 || chert == 0 || speckle == 0
            || chalcocite == 0 || smithsonite == 0 || limonite == 0
            || redGossan == 0 || yellowGossan == 0
            || fraction < 0.02 || fraction > 0.30)
        {
            throw new InvalidOperationException(
                "Bimodal-felsic VMS plan did not produce the deterministic facies set: " +
                $"deterministic={deterministic} chalcopyrite={chalcopyrite} sphalerite={sphalerite} pyrite={pyrite} " +
                $"chert={chert} speckle={speckle} fraction={fraction:F3} chalcocite={chalcocite} " +
                $"smithsonite={smithsonite} limonite={limonite} gossan={redGossan}/{yellowGossan}");
        }

        return new VmsVerificationResult(
            "bimodalFelsicVms",
            plan.LensCount,
            plan.StringerCount,
            chalcopyrite,
            sphalerite,
            pyrite,
            chert,
            speckle,
            fraction,
            chalcocite,
            smithsonite,
            limonite,
            redGossan,
            yellowGossan,
            greyGossanAndGalenaCarbonate,
            deterministic);
    }

    public static VmsVerificationResult RunCyprusVms()
    {
        ulong featureId = ProceduralDepositMath.FeatureId(
            663391,
            ProceduralDepositMath.HashString("cyprus-vms-verification"),
            7,
            2);
        var settings = new CyprusVmsDefinition();
        var instance = new ProceduralDepositInstance(featureId, 0, 100, 0, 1, 0, 0, 0);
        CyprusVmsPlan plan = CyprusVmsPlan.Create(instance, settings);
        CyprusVmsPlan repeated = CyprusVmsPlan.Create(instance, settings);

        int chalcopyrite = 0, sphalerite = 0, pyrite = 0, chert = 0, speckle = 0;
        int chalcocite = 0, smithsonite = 0, limonite = 0, redGossan = 0, yellowGossan = 0, umber = 0;
        bool deterministic = true;

        foreach (int surfaceY in new[] { 106, 112, 118, 124 })
        {
            for (int x = -44; x <= 44; x += 2)
            {
                for (int z = -44; z <= 44; z += 2)
                {
                    for (int y = 70; y <= surfaceY; y++)
                    {
                        CyprusVmsSample sample = plan.Evaluate(x, y, z, surfaceY, true);
                        if (sample != repeated.Evaluate(x, y, z, surfaceY, true)) deterministic = false;

                        switch (sample.Zone)
                        {
                            case CyprusVmsZone.Chalcopyrite: chalcopyrite++; break;
                            case CyprusVmsZone.Sphalerite: sphalerite++; break;
                            case CyprusVmsZone.Pyrite: pyrite++; break;
                            case CyprusVmsZone.Chert: chert++; break;
                            case CyprusVmsZone.ChertHematite:
                            case CyprusVmsZone.ChertMagnetite:
                                speckle++;
                                break;
                            case CyprusVmsZone.Umber: umber++; break;
                            case CyprusVmsZone.Chalcocite: chalcocite++; break;
                            case CyprusVmsZone.Smithsonite: smithsonite++; break;
                            case CyprusVmsZone.Limonite: limonite++; break;
                            case CyprusVmsZone.Gossan: redGossan++; break;
                            case CyprusVmsZone.YellowGossan: yellowGossan++; break;
                        }

    // Speckles must be exactly the top 20% of The grain hash; the
                        // global fraction is diluted by unspeckled chert from the umber/exhalite cap.
                        if (sample.Zone == CyprusVmsZone.ChertHematite
                            || sample.Zone == CyprusVmsZone.ChertMagnetite)
                        {
                            double fineGrain = CyprusVmsPlan.Hash3D(
                                x * 2.1 + plan.Seed,
                                (y - 100) * 2.1,
                                z * 2.1);
                            if (fineGrain < 0.80)
                            {
                                throw new InvalidOperationException(
                                    $"Cyprus VMS placed a chert speckle below the 20% threshold (grain {fineGrain:F3})");
                            }
                        }
                    }
                }
            }
        }

        double fraction = (chert + speckle) > 0 ? (double)speckle / (chert + speckle) : 0.0;
        if (!deterministic
            || chalcopyrite == 0 || pyrite == 0 || chert == 0 || speckle == 0 || umber == 0
            || chalcocite == 0 || limonite == 0 || redGossan == 0
            || fraction < 0.02 || fraction > 0.30)
        {
            throw new InvalidOperationException(
                "Cyprus VMS plan did not produce the deterministic facies set: " +
                $"deterministic={deterministic} chalcopyrite={chalcopyrite} sphalerite={sphalerite} pyrite={pyrite} " +
                $"chert={chert} speckle={speckle} fraction={fraction:F3} umber={umber} chalcocite={chalcocite} " +
                $"smithsonite={smithsonite} limonite={limonite} gossan={redGossan}/{yellowGossan}");
        }

        return new VmsVerificationResult(
            "cyprusVms",
            plan.LensCount,
            plan.StringerCount,
            chalcopyrite,
            sphalerite,
            pyrite,
            chert,
            speckle,
            fraction,
            chalcocite,
            smithsonite,
            limonite,
            redGossan,
            yellowGossan,
            umber,
            deterministic);
    }

    public static VmsVerificationResult RunBesshiVms()
    {
        ulong featureId = ProceduralDepositMath.FeatureId(
            774463,
            ProceduralDepositMath.HashString("besshi-vms-verification"),
            -5,
            -2);
        var settings = new BesshiVmsDefinition();
        var instance = new ProceduralDepositInstance(featureId, 0, 100, 0, 1, 0, 0, 0);
        BesshiVmsPlan plan = BesshiVmsPlan.Create(instance, settings);
        BesshiVmsPlan repeated = BesshiVmsPlan.Create(instance, settings);

        int chalcopyrite = 0, sphalerite = 0, pyrite = 0, chert = 0, speckle = 0;
        int chalcocite = 0, smithsonite = 0, limonite = 0, redGossan = 0, yellowGossan = 0, magnetite = 0;
        bool deterministic = true;

        foreach (int surfaceY in new[] { 106, 112, 118, 124 })
        {
            for (int x = -44; x <= 44; x += 2)
            {
                for (int z = -44; z <= 44; z += 2)
                {
                    for (int y = 70; y <= surfaceY; y++)
                    {
                        BesshiVmsSample sample = plan.Evaluate(x, y, z, surfaceY);
                        if (sample != repeated.Evaluate(x, y, z, surfaceY)) deterministic = false;

                        switch (sample.Zone)
                        {
                            case BesshiVmsZone.Chalcopyrite: chalcopyrite++; break;
                            case BesshiVmsZone.Sphalerite: sphalerite++; break;
                            case BesshiVmsZone.Pyrite: pyrite++; break;
                            case BesshiVmsZone.Magnetite: magnetite++; break;
                            case BesshiVmsZone.Chert: chert++; break;
                            case BesshiVmsZone.ChertHematite: speckle++; break;
                            case BesshiVmsZone.Chalcocite: chalcocite++; break;
                            case BesshiVmsZone.Smithsonite: smithsonite++; break;
                            case BesshiVmsZone.Limonite: limonite++; break;
                            case BesshiVmsZone.Gossan: redGossan++; break;
                            case BesshiVmsZone.YellowGossan: yellowGossan++; break;
                        }

    // Speckles must be exactly the top 20% of The grain hash.
                        if (sample.Zone == BesshiVmsZone.ChertHematite)
                        {
                            double fineGrain = BesshiVmsPlan.Hash3D(
                                x * 2.1 + plan.Seed,
                                (y - 100) * 2.1,
                                z * 2.1);
                            if (fineGrain < 0.80)
                            {
                                throw new InvalidOperationException(
                                    $"Besshi VMS placed a chert speckle below the 20% threshold (grain {fineGrain:F3})");
                            }
                        }
                    }
                }
            }
        }

        double fraction = (chert + speckle) > 0 ? (double)speckle / (chert + speckle) : 0.0;
        if (!deterministic
            || chalcopyrite == 0 || sphalerite == 0 || pyrite == 0 || chert == 0 || speckle == 0
            || chalcocite == 0 || smithsonite == 0 || limonite == 0 || redGossan == 0 || yellowGossan == 0
            || fraction < 0.02 || fraction > 0.30)
        {
            throw new InvalidOperationException(
                "Besshi VMS plan did not produce the deterministic facies set: " +
                $"deterministic={deterministic} chalcopyrite={chalcopyrite} sphalerite={sphalerite} pyrite={pyrite} " +
                $"magnetite={magnetite} chert={chert} speckle={speckle} fraction={fraction:F3} " +
                $"chalcocite={chalcocite} smithsonite={smithsonite} limonite={limonite} " +
                $"gossan={redGossan}/{yellowGossan}");
        }

        return new VmsVerificationResult(
            "besshiVms",
            plan.MassiveLensCount,
            plan.StringerCount,
            chalcopyrite,
            sphalerite,
            pyrite,
            chert,
            speckle,
            fraction,
            chalcocite,
            smithsonite,
            limonite,
            redGossan,
            yellowGossan,
            magnetite,
            deterministic);
    }

    public static AlluvialLigniteVerificationResult RunAlluvialLignite()
    {
        ulong featureId = ProceduralDepositMath.FeatureId(771244, 0, -5, 11);
        var settings = new AlluvialLigniteDefinition();
        var instance = new ProceduralDepositInstance(featureId, 0, 100, 0, 1, 0, 0, 0);
        AlluvialLignitePlan plan = AlluvialLignitePlan.Create(instance, settings);
        AlluvialLignitePlan repeated = AlluvialLignitePlan.Create(instance, settings);

        int lignite = 0, woody = 0, pyritic = 0, highAsh = 0, highAshPruned = 0;
        int clay = 0, sand = 0;
        bool deterministic = true;

        // Sweep several terrain elevations; the deposit is clipped against the height map.
        foreach (int surfaceY in new[] { 104, 110, 116, 122 })
        {
            for (int x = -40; x <= 40; x += 2)
            {
                for (int z = -40; z <= 40; z += 2)
                {
                    for (int y = 70; y <= surfaceY; y++)
                    {
                        AlluvialLigniteSample sample = plan.Evaluate(x, y, z, surfaceY);
                        if (sample != repeated.Evaluate(x, y, z, surfaceY)) deterministic = false;

                        // Nothing may be placed outside a peat lens, a minor bed or the channel.
                        if (sample.Zone != AlluvialLigniteZone.None
                            && !sample.InsidePeat
                            && !sample.InsideChannel)
                        {
                            throw new InvalidOperationException(
                                $"Alluvial lignite placed {sample.Zone} outside every lens and channel");
                        }

                        if (sample.HighAshPruned) highAshPruned++;

                        switch (sample.Zone)
                        {
                            case AlluvialLigniteZone.Lignite: lignite++; break;
                            case AlluvialLigniteZone.WoodyLignite: woody++; break;
                            case AlluvialLigniteZone.PyriticLignite: pyritic++; break;
                            case AlluvialLigniteZone.HighAshLignite: highAsh++; break;
                            case AlluvialLigniteZone.CarbonaceousClay: clay++; break;
                            case AlluvialLigniteZone.ChannelSand: sand++; break;
                        }
                    }
                }
            }
        }

        int highAshTotal = highAsh + highAshPruned;
        double prunedFraction = highAshTotal > 0 ? (double)highAshPruned / highAshTotal : 0.0;
        if (!deterministic
            || plan.UnitCount < 1 || plan.UnitCount > 3
            || plan.PeatLensCount != plan.UnitCount * settings.LensesPerUnit
            || lignite == 0 || woody == 0 || pyritic == 0 || highAsh == 0
            || clay == 0 || sand == 0
            || prunedFraction < 0.20 || prunedFraction > 0.30)
        {
            throw new InvalidOperationException(
                "Alluvial lignite plan did not produce the deterministic facies set: " +
                $"deterministic={deterministic} units={plan.UnitCount} lenses={plan.PeatLensCount} " +
                $"minorBeds={plan.MinorBedCount} lignite={lignite} woody={woody} pyritic={pyritic} " +
                $"highAsh={highAsh} pruned={highAshPruned} fraction={prunedFraction:F3} " +
                $"clay={clay} sand={sand}");
        }

        return new AlluvialLigniteVerificationResult(
            plan.UnitCount,
            plan.PeatLensCount,
            plan.MinorBedCount,
            lignite,
            woody,
            pyritic,
            highAsh,
            highAshPruned,
            prunedFraction,
            clay,
            sand,
            deterministic);
    }

    public static FoldedAnthraciteVerificationResult RunFoldedAnthracite()
    {
        ulong featureId = ProceduralDepositMath.FeatureId(319887, 0, 7, -4);
        var settings = new FoldedAnthraciteDefinition();
        var instance = new ProceduralDepositInstance(featureId, 0, 100, 0, 1, 0, 0, 0);
        FoldedAnthracitePlan plan = FoldedAnthracitePlan.Create(instance, settings);
        FoldedAnthracitePlan repeated = FoldedAnthracitePlan.Create(instance, settings);

        int anthracite = 0, crushed = 0, parting = 0, repeatedLimb = 0, repeatedColumns = 0;
        bool deterministic = true;

        foreach (int surfaceY in new[] { 104, 112, 120 })
        {
            for (int x = -44; x <= 44; x += 2)
            {
                for (int z = -44; z <= 44; z += 2)
                {
                    // Detect the thrust repetition: one seam index occupying two disjoint runs
                    // in a single vertical column.
                    int lastSeam = -1;
                    int lastRunEndY = int.MinValue;
                    bool columnRepeats = false;

                    for (int y = 60; y <= surfaceY; y++)
                    {
                        FoldedAnthraciteSample sample = plan.Evaluate(x, y, z, surfaceY);
                        if (sample != repeated.Evaluate(x, y, z, surfaceY)) deterministic = false;
                        if (sample.Zone == FoldedAnthraciteZone.None) continue;

                        if (sample.Repeated) repeatedLimb++;
                        if (sample.SeamIndex == lastSeam && y - lastRunEndY >= 3) columnRepeats = true;
                        if (sample.SeamIndex != lastSeam || y - lastRunEndY > 1) lastSeam = sample.SeamIndex;
                        lastRunEndY = y;

                        switch (sample.Zone)
                        {
                            case FoldedAnthraciteZone.Anthracite: anthracite++; break;
                            case FoldedAnthraciteZone.CrushedAnthracite: crushed++; break;
                            case FoldedAnthraciteZone.SlateParting: parting++; break;
                        }
                    }

                    if (columnRepeats) repeatedColumns++;
                }
            }
        }

        if (!deterministic
            || plan.SeamCount < 1 || plan.SeamCount > 3
            || anthracite == 0 || crushed == 0 || parting == 0
            || repeatedLimb == 0 || repeatedColumns == 0)
        {
            throw new InvalidOperationException(
                "Folded anthracite plan did not produce the deterministic seam package: " +
                $"deterministic={deterministic} seams={plan.SeamCount} anthracite={anthracite} " +
                $"crushed={crushed} parting={parting} repeatedLimb={repeatedLimb} " +
                $"repeatedColumns={repeatedColumns}");
        }

        return new FoldedAnthraciteVerificationResult(
            plan.SeamCount,
            anthracite,
            crushed,
            parting,
            repeatedLimb,
            repeatedColumns,
            deterministic);
    }

    public static ReopenedArsenideVeinVerificationResult RunReopenedArsenideVein()
    {
        ulong featureId = ProceduralDepositMath.FeatureId(884517, 0, -9, 2);
        var settings = new ReopenedArsenideVeinDefinition();
        var instance = new ProceduralDepositInstance(featureId, 0, 100, 0, 1, 0, 0, 0);
        ReopenedArsenideVeinPlan plan = ReopenedArsenideVeinPlan.Create(instance, settings);
        ReopenedArsenideVeinPlan repeated = ReopenedArsenideVeinPlan.Create(instance, settings);

        int uraninite = 0, bismuth = 0, silver = 0, nickeline = 0, cobaltite = 0;
        int quartz = 0, fluorite = 0, barite = 0, carbonate = 0;
        int torbernite = 0, annabergite = 0, erythrite = 0;
        int redGossan = 0, greenGossan = 0, pinkGossan = 0;
        int arsenideVoxels = 0;
        bool deterministic = true;

        foreach (int surfaceY in new[] { 106, 112, 118, 124 })
        {
            for (int x = -44; x <= 44; x += 2)
            {
                for (int z = -44; z <= 44; z += 2)
                {
                    double graniteTopY = plan.GetGraniteTopY(x, z);

                    for (int y = 60; y <= surfaceY; y++)
                    {
                        ReopenedArsenideVeinSample sample = plan.Evaluate(x, y, z, surfaceY);
                        if (sample != repeated.Evaluate(x, y, z, surfaceY)) deterministic = false;
                        if (sample.Zone == ReopenedArsenideVeinZone.None) continue;

                        if (y < graniteTopY)
                        {
                            throw new InvalidOperationException(
                                $"Reopened arsenide vein placed {sample.Zone} below the cupola roof " +
                                $"(y={y} roof={graniteTopY:F2})");
                        }

                        int depth = surfaceY - y + 1;

                        // Stained soils only at depth 1-2, secondary arsenates only at depth 3-8.
                        bool isSoil = sample.Zone == ReopenedArsenideVeinZone.Gossan
                            || sample.Zone == ReopenedArsenideVeinZone.GreenGossan
                            || sample.Zone == ReopenedArsenideVeinZone.PinkGossan;
                        bool isSecondary = sample.Zone == ReopenedArsenideVeinZone.Torbernite
                            || sample.Zone == ReopenedArsenideVeinZone.Annabergite
                            || sample.Zone == ReopenedArsenideVeinZone.Erythrite;
                        if (isSoil && depth > 2)
                        {
                            throw new InvalidOperationException(
                                $"Reopened arsenide vein placed {sample.Zone} at depth {depth}, below the soil window");
                        }
                        if (isSecondary && (depth < 3 || depth > 8))
                        {
                            throw new InvalidOperationException(
                                $"Reopened arsenide vein placed {sample.Zone} at depth {depth}, outside the 3-8 window");
                        }

                        // Cobaltite and erythrite exist only where the 10% speckle gate opens.
                        if (sample.Zone == ReopenedArsenideVeinZone.Cobaltite
                            || sample.Zone == ReopenedArsenideVeinZone.Erythrite
                            || sample.Zone == ReopenedArsenideVeinZone.PinkGossan)
                        {
                            if (sample.BloomNoise < 0.90)
                            {
                                throw new InvalidOperationException(
                                    $"Reopened arsenide vein placed {sample.Zone} below the 10% cobaltite " +
                                    $"speckle threshold (grain {sample.BloomNoise:F3})");
                            }
                        }

                        switch (sample.Zone)
                        {
                            case ReopenedArsenideVeinZone.Uraninite: uraninite++; break;
                            case ReopenedArsenideVeinZone.NativeBismuth: bismuth++; break;
                            case ReopenedArsenideVeinZone.NativeSilver: silver++; break;
                            case ReopenedArsenideVeinZone.Nickeline: nickeline++; arsenideVoxels++; break;
                            case ReopenedArsenideVeinZone.Cobaltite: cobaltite++; arsenideVoxels++; break;
                            case ReopenedArsenideVeinZone.Quartz: quartz++; break;
                            case ReopenedArsenideVeinZone.Fluorite: fluorite++; break;
                            case ReopenedArsenideVeinZone.Barite: barite++; break;
                            case ReopenedArsenideVeinZone.Carbonate: carbonate++; break;
                            case ReopenedArsenideVeinZone.Torbernite: torbernite++; break;
                            case ReopenedArsenideVeinZone.Annabergite: annabergite++; break;
                            case ReopenedArsenideVeinZone.Erythrite: erythrite++; break;
                            case ReopenedArsenideVeinZone.Gossan: redGossan++; break;
                            case ReopenedArsenideVeinZone.GreenGossan: greenGossan++; break;
                            case ReopenedArsenideVeinZone.PinkGossan: pinkGossan++; break;
                        }
                    }
                }
            }
        }

        double speckleFraction = arsenideVoxels > 0 ? (double)cobaltite / arsenideVoxels : 0.0;
        if (!deterministic
            || plan.PrimaryVeinCount < 4 || plan.PrimaryVeinCount > 7
            || plan.LateCrosscutCount < 1 || plan.LateCrosscutCount > 2
            || uraninite == 0 || bismuth == 0 || silver == 0 || nickeline == 0 || cobaltite == 0
            || quartz == 0 || fluorite == 0 || barite == 0 || carbonate == 0
            || torbernite == 0 || annabergite == 0 || erythrite == 0
            || redGossan == 0 || greenGossan == 0)
        {
            throw new InvalidOperationException(
                "Reopened arsenide vein plan did not produce the deterministic zonation: " +
                $"deterministic={deterministic} primary={plan.PrimaryVeinCount} late={plan.LateCrosscutCount} " +
                $"uraninite={uraninite} bismuth={bismuth} silver={silver} nickeline={nickeline} " +
                $"cobaltite={cobaltite} quartz={quartz} fluorite={fluorite} barite={barite} " +
                $"carbonate={carbonate} torbernite={torbernite} annabergite={annabergite} " +
                $"erythrite={erythrite} gossan={redGossan}/{greenGossan}/{pinkGossan}");
        }

        return new ReopenedArsenideVeinVerificationResult(
            plan.PrimaryVeinCount,
            plan.LateCrosscutCount,
            uraninite,
            bismuth,
            silver,
            nickeline,
            cobaltite,
            quartz,
            fluorite,
            barite,
            carbonate,
            torbernite,
            annabergite,
            erythrite,
            redGossan,
            greenGossan,
            pinkGossan,
            speckleFraction,
            deterministic);
    }

    /// <summary>
    /// Every ore that carries a metal must vary in grade, otherwise "graded" is cosmetic. The four
    /// realizers that used to resolve at grade 0 now select a richness, so each must reach all four
    /// grades across a deposit and stay deterministic.
    /// </summary>
    public static GradedOreVerificationResult RunGradedMetalOres()
    {
        bool deterministic = true;

        var greisenGrades = new HashSet<int>();
        ulong greisenFeature = ProceduralDepositMath.FeatureId(551903, 0, 4, -6);
        var greisenSettings = new GreisenStockworkDefinition();
        GreisenStockworkPlan greisen = GreisenStockworkPlan.Create(
            new ProceduralDepositInstance(greisenFeature, 0, 100, 0, 1, 0, 0, 0),
            greisenSettings);

        var prabornaGrades = new HashSet<int>();
        ulong prabornaFeature = ProceduralDepositMath.FeatureId(662014, 0, -3, 9);
        var prabornaSettings = new PrabornaPyrolusiteDefinition();
        PrabornaPyrolusitePlan praborna = PrabornaPyrolusitePlan.Create(
            new ProceduralDepositInstance(prabornaFeature, 0, 100, 0, 1, 0, 0, 0),
            prabornaSettings);

        var sideriteGrades = new HashSet<int>();
        ulong sideriteFeature = ProceduralDepositMath.FeatureId(773125, 0, 6, 2);
        var sideriteSettings = new MetasomaticSideriteDefinition();
        MetasomaticSideritePlan siderite = MetasomaticSideritePlan.Create(
            new ProceduralDepositInstance(sideriteFeature, 0, 100, 0, 1, 0, 0, 0),
            sideriteSettings);

        var carbonatiteGrades = new HashSet<int>();
        ulong carbonatiteFeature = ProceduralDepositMath.FeatureId(884236, 0, -7, -1);
        var carbonatiteSettings = new CarbonatiteComplexDefinition();
        CarbonatiteComplexPlan carbonatite = CarbonatiteComplexPlan.Create(
            new ProceduralDepositInstance(carbonatiteFeature, 0, 100, 0, 1, 0, 0, 0),
            carbonatiteSettings);

        var arsenideGrades = new HashSet<int>();
        ulong arsenideFeature = ProceduralDepositMath.FeatureId(995347, 0, 1, 5);
        var arsenideSettings = new ReopenedArsenideVeinDefinition();
        ReopenedArsenideVeinPlan arsenide = ReopenedArsenideVeinPlan.Create(
            new ProceduralDepositInstance(arsenideFeature, 0, 100, 0, 1, 0, 0, 0),
            arsenideSettings);

        for (int x = -40; x <= 40; x += 2)
        {
            for (int z = -40; z <= 40; z += 2)
            {
                for (int y = 70; y <= 118; y++)
                {
                    if (greisen.EvaluateZone(x, y, z, 118) != GreisenStockworkZone.None)
                    {
                        int grade = ProceduralDepositWorldGenSystem.SelectGreisenGrade(
                            greisenFeature, greisen.Sample(x, y, z), x, y, z);
                        int again = ProceduralDepositWorldGenSystem.SelectGreisenGrade(
                            greisenFeature, greisen.Sample(x, y, z), x, y, z);
                        if (grade != again) deterministic = false;
                        greisenGrades.Add(grade);
                    }

                    if (praborna.EvaluateZone(x, y, z, 118) != PrabornaPyrolusiteZone.None)
                    {
                        prabornaGrades.Add(ProceduralDepositWorldGenSystem.SelectPrabornaGrade(
                            prabornaFeature, praborna.Sample(x, y, z), x, y, z));
                    }

                    if (siderite.EvaluateZone(x, y, z, 118) != MetasomaticSideriteZone.None)
                    {
                        sideriteGrades.Add(ProceduralDepositWorldGenSystem.SelectMetasomaticSideriteGrade(
                            sideriteFeature, siderite.SampleDetails(x, y, z, 118), x, y, z));
                    }

                    CarbonatiteComplexSample carbonatiteSample = carbonatite.Sample(x, y, z);
                    if (carbonatiteSample.Inside && carbonatiteSample.Zone != CarbonatiteMaterialZone.None)
                    {
                        carbonatiteGrades.Add(ProceduralDepositWorldGenSystem.SelectCarbonatiteGrade(
                            carbonatiteFeature, carbonatiteSample, x, y, z));
                    }

                    ReopenedArsenideVeinSample arsenideSample = arsenide.Evaluate(x, y, z, 118);
                    if (arsenideSample.Zone != ReopenedArsenideVeinZone.None)
                    {
                        arsenideGrades.Add(arsenideSample.Grade);
                    }
                }
            }
        }

        if (!deterministic
            || greisenGrades.Count < 4
            || carbonatiteGrades.Count < 4
            || prabornaGrades.Count < 4
            || sideriteGrades.Count < 4
            || arsenideGrades.Count < 4)
        {
            throw new InvalidOperationException(
                "Graded metal ores did not reach every grade: " +
                $"deterministic={deterministic} " +
                $"greisen=[{string.Join(",", greisenGrades.Order())}] " +
                $"carbonatite=[{string.Join(",", carbonatiteGrades.Order())}] " +
                $"praborna=[{string.Join(",", prabornaGrades.Order())}] " +
                $"siderite=[{string.Join(",", sideriteGrades.Order())}] " +
                $"arsenide=[{string.Join(",", arsenideGrades.Order())}]");
        }


        return new GradedOreVerificationResult(
            greisenGrades.Count,
            carbonatiteGrades.Count,
            prabornaGrades.Count,
            sideriteGrades.Count,
            arsenideGrades.Count,
            deterministic);
    }
    /// <summary>
    /// Deterministic coverage for the six deposits: every mapped facies must occur, the two
    /// hash-gated replacement rules must hit their stated fractions, graded ores must vary in grade,
    /// </summary>
    public static SixDepositVerificationResult RunSixDeposits()
    {
        bool deterministic = true;

        // --- Cyclothem bituminous coal -------------------------------------------------------
        ulong coalFeature = ProceduralDepositMath.FeatureId(618254, 0, 3, -7);
        var coalSettings = new CyclothemCoalDefinition();
        var coalInstance = new ProceduralDepositInstance(coalFeature, 0, 100, 0, 1, 0, 0, 0);
        CyclothemCoalPlan coal = CyclothemCoalPlan.Create(coalInstance, coalSettings);
        CyclothemCoalPlan coalRepeat = CyclothemCoalPlan.Create(coalInstance, coalSettings);

        int coalSamples = 0, coalHighAsh = 0, coalHighAshPruned = 0;
        int coalParting = 0, coalSeat = 0, coalFireclay = 0, coalChannel = 0;
        foreach (int surfaceY in new[] { 104, 112, 120 })
        {
            for (int x = -40; x <= 40; x += 2)
            {
                for (int z = -40; z <= 40; z += 2)
                {
                    for (int y = 70; y <= surfaceY; y++)
                    {
                        CyclothemCoalSample s = coal.Evaluate(x, y, z, surfaceY);
                        if (s != coalRepeat.Evaluate(x, y, z, surfaceY)) deterministic = false;
                        if (s.HighAshPruned) coalHighAshPruned++;

                        switch (s.Zone)
                        {
                            case CyclothemCoalZone.Bituminous:
                            case CyclothemCoalZone.PyriticCoal:
                                coalSamples++;
                                break;
                            case CyclothemCoalZone.HighAshCoal:
                                coalSamples++;
                                coalHighAsh++;
                                break;
                            case CyclothemCoalZone.ShaleParting: coalParting++; break;
                            case CyclothemCoalZone.OrganicClaystone: coalSeat++; break;
                            case CyclothemCoalZone.Fireclay: coalFireclay++; break;
                            case CyclothemCoalZone.ChannelSandstone: coalChannel++; break;
                        }
                    }
                }
            }
        }

        int coalHighAshTotal = coalHighAsh + coalHighAshPruned;
        double coalPrunedFraction = coalHighAshTotal > 0
            ? (double)coalHighAshPruned / coalHighAshTotal
            : 0.0;

        // --- Almaden stratiform cinnabar -----------------------------------------------------
        ulong cinnabarFeature = ProceduralDepositMath.FeatureId(729361, 0, -4, 6);
        var cinnabarSettings = new StratiformCinnabarDefinition();
        var cinnabarInstance = new ProceduralDepositInstance(cinnabarFeature, 0, 100, 0, 1, 0, 0, 0);
        StratiformCinnabarPlan cinnabar = StratiformCinnabarPlan.Create(cinnabarInstance, cinnabarSettings);
        StratiformCinnabarPlan cinnabarRepeat = StratiformCinnabarPlan.Create(cinnabarInstance, cinnabarSettings);

        int cinnabarSamples = 0, cinnabarQuartz = 0, cinnabarPyrite = 0;
        foreach (int surfaceY in new[] { 106, 116 })
        {
            for (int x = -42; x <= 42; x += 2)
            {
                for (int z = -42; z <= 42; z += 2)
                {
                    for (int y = 70; y <= surfaceY; y++)
                    {
                        StratiformCinnabarSample s = cinnabar.Evaluate(x, y, z, surfaceY);
                        if (s != cinnabarRepeat.Evaluate(x, y, z, surfaceY)) deterministic = false;

                        switch (s.Zone)
                        {
                            case StratiformCinnabarZone.DenseCinnabar:
                            case StratiformCinnabarZone.CinnabarImpregnation:
                            case StratiformCinnabarZone.CinnabarJointFill:
                                cinnabarSamples++;
                                break;
                            case StratiformCinnabarZone.QuartzCarbonate: cinnabarQuartz++; break;
                            case StratiformCinnabarZone.Pyrite: cinnabarPyrite++; break;
                        }
                    }
                }
            }
        }

        // --- Bolivian porphyry tin -----------------------------------------------------------
        ulong tinFeature = ProceduralDepositMath.FeatureId(834172, 0, 5, 1);
        var tinSettings = new PorphyryTinDefinition();
        var tinInstance = new ProceduralDepositInstance(tinFeature, 0, 100, 0, 1, 0, 0, 0);
        PorphyryTinPlan tin = PorphyryTinPlan.Create(tinInstance, tinSettings);
        PorphyryTinPlan tinRepeat = PorphyryTinPlan.Create(tinInstance, tinSettings);

        int tinCassiterite = 0, tinStannite = 0, tinBismuthinite = 0, tinStibnite = 0, tinTourmaline = 0;
        var tinGrades = new HashSet<int>();
        foreach (int surfaceY in new[] { 108, 118 })
        {
            for (int x = -38; x <= 38; x += 2)
            {
                for (int z = -38; z <= 38; z += 2)
                {
                    for (int y = 70; y <= surfaceY; y++)
                    {
                        PorphyryTinSample s = tin.Evaluate(x, y, z, surfaceY);
                        if (s != tinRepeat.Evaluate(x, y, z, surfaceY)) deterministic = false;
                        if (s.Zone == PorphyryTinZone.None) continue;

                        // Stibnite only exists where the 10% speckle gate opened.
                        if (s.Zone == PorphyryTinZone.Stibnite
                            && s.SpeckleNoise < tinSettings.StibniteSpeckleThreshold)
                        {
                            throw new InvalidOperationException(
                                $"Porphyry tin placed stibnite below the speckle gate (grain {s.SpeckleNoise:F3})");
                        }

                        if (PorphyryTinPlan.IsGraded(s.Zone)) tinGrades.Add(s.Grade);
                        else if (s.Grade != 0)
                        {
                            throw new InvalidOperationException(
                                $"Porphyry tin assigned grade {s.Grade} to ungraded zone {s.Zone}");
                        }

                        switch (s.Zone)
                        {
                            case PorphyryTinZone.Cassiterite: tinCassiterite++; break;
                            case PorphyryTinZone.Stannite: tinStannite++; break;
                            case PorphyryTinZone.Bismuthinite: tinBismuthinite++; break;
                            case PorphyryTinZone.Stibnite: tinStibnite++; break;
                            case PorphyryTinZone.TourmalineBreccia: tinTourmaline++; break;
                        }
                    }
                }
            }
        }

        int tinSulfosalt = tinBismuthinite + tinStibnite;
        double tinSpeckleFraction = tinSulfosalt > 0 ? (double)tinStibnite / tinSulfosalt : 0.0;

        // --- Playa borates -------------------------------------------------------------------
        ulong borateFeature = ProceduralDepositMath.FeatureId(945283, 0, -6, -2);
        var borateSettings = new PlayaBorateDefinition();
        var borateInstance = new ProceduralDepositInstance(borateFeature, 0, 100, 0, 1, 0, 0, 0);
        PlayaBoratePlan borate = PlayaBoratePlan.Create(borateInstance, borateSettings);
        PlayaBoratePlan borateRepeat = PlayaBoratePlan.Create(borateInstance, borateSettings);

        int borateSamples = 0, borateGypsum = 0;
        for (int x = -34; x <= 34; x++)
        {
            for (int z = -34; z <= 34; z++)
            {
                for (int y = 84; y <= 120; y++)
                {
                    PlayaBorateSample s = borate.Evaluate(x, y, z, 120);
                    if (s != borateRepeat.Evaluate(x, y, z, 120)) deterministic = false;

                    switch (s.Zone)
                    {
                        case PlayaBorateZone.BoraxCrust:
                        case PlayaBorateZone.UlexiteNodule:
                        case PlayaBorateZone.ColemaniteNodule:
                            borateSamples++;
                            break;
                        case PlayaBorateZone.GypsumCrust: borateGypsum++; break;
                    }
                }
            }
        }

        // --- Tin skarn replacement -----------------------------------------------------------
        ulong skarnFeature = ProceduralDepositMath.FeatureId(156394, 0, 2, 8);
        var skarnSettings = new TinSkarnDefinition();
        var skarnInstance = new ProceduralDepositInstance(skarnFeature, 0, 100, 0, 1, 0, 0, 0);
        TinSkarnPlan skarn = TinSkarnPlan.Create(skarnInstance, skarnSettings);
        TinSkarnPlan skarnRepeat = TinSkarnPlan.Create(skarnInstance, skarnSettings);

        int skarnCassiterite = 0, skarnScheelite = 0, skarnFluorite = 0, skarnFluoriteZone = 0;
        var skarnGrades = new HashSet<int>();
        foreach (int surfaceY in new[] { 106, 116 })
        {
            for (int x = -42; x <= 42; x += 2)
            {
                for (int z = -42; z <= 42; z += 2)
                {
                    for (int y = 70; y <= surfaceY; y++)
                    {
                        TinSkarnSample s = skarn.Evaluate(x, y, z, surfaceY);
                        if (s != skarnRepeat.Evaluate(x, y, z, surfaceY)) deterministic = false;
                        if (s.InFluoriteCarbonate) skarnFluoriteZone++;

                        if (s.Zone == TinSkarnZone.None) continue;

                        // Fluorite only survives where the 10% speckle gate opened.
                        if (s.Zone == TinSkarnZone.Fluorite
                            && s.SpeckleNoise < skarnSettings.FluoriteSpeckleThreshold)
                        {
                            throw new InvalidOperationException(
                                $"Tin skarn placed fluorite below the speckle gate (grain {s.SpeckleNoise:F3})");
                        }

                        if (TinSkarnPlan.IsGraded(s.Zone)) skarnGrades.Add(s.Grade);
                        else if (s.Grade != 0)
                        {
                            throw new InvalidOperationException(
                                $"Tin skarn assigned grade {s.Grade} to ungraded zone {s.Zone}");
                        }

                        switch (s.Zone)
                        {
                            case TinSkarnZone.Cassiterite: skarnCassiterite++; break;
                            case TinSkarnZone.Scheelite: skarnScheelite++; break;
                            case TinSkarnZone.Fluorite: skarnFluorite++; break;
                        }
                    }
                }
            }
        }

        double skarnSpeckleFraction = skarnFluoriteZone > 0
            ? (double)skarnFluorite / skarnFluoriteZone
            : 0.0;

        // --- Volcanic alunite ----------------------------------------------------------------
        ulong aluniteFeature = ProceduralDepositMath.FeatureId(267415, 0, -1, 4);
        var aluniteSettings = new VolcanicAluniteDefinition();
        var aluniteInstance = new ProceduralDepositInstance(aluniteFeature, 0, 100, 0, 1, 0, 0, 0);
        VolcanicAlunitePlan alunite = VolcanicAlunitePlan.Create(aluniteInstance, aluniteSettings);
        VolcanicAlunitePlan aluniteRepeat = VolcanicAlunitePlan.Create(aluniteInstance, aluniteSettings);

        int aluniteSamples = 0, aluniteQuartz = 0, aluniteGypsum = 0;
        foreach (int surfaceY in new[] { 112, 122 })
        {
            for (int x = -40; x <= 40; x += 2)
            {
                for (int z = -40; z <= 40; z += 2)
                {
                    for (int y = 80; y <= surfaceY; y++)
                    {
                        VolcanicAluniteSample s = alunite.Evaluate(x, y, z, surfaceY);
                        if (s != aluniteRepeat.Evaluate(x, y, z, surfaceY)) deterministic = false;

                        switch (s.Zone)
                        {
                            case VolcanicAluniteZone.Alunite: aluniteSamples++; break;
                            case VolcanicAluniteZone.QuartzAlunite:
                            case VolcanicAluniteZone.VuggySilica:
                                aluniteQuartz++;
                                break;
                            case VolcanicAluniteZone.Gypsum: aluniteGypsum++; break;
                        }
                    }
                }
            }
        }

        if (!deterministic
            || coalSamples == 0 || coalParting == 0 || coalSeat == 0 || coalFireclay == 0
            || coalChannel == 0 || coalPrunedFraction < 0.20 || coalPrunedFraction > 0.30
            || cinnabarSamples == 0 || cinnabarQuartz == 0 || cinnabarPyrite == 0
            || tinCassiterite == 0 || tinStannite == 0 || tinBismuthinite == 0
            || tinStibnite == 0 || tinTourmaline == 0 || tinGrades.Count < 4
            || borateSamples == 0 || borateGypsum == 0
            || skarnCassiterite == 0 || skarnScheelite == 0 || skarnFluorite == 0
            || skarnGrades.Count < 4
            || aluniteSamples == 0 || aluniteQuartz == 0 || aluniteGypsum == 0)
        {
            throw new InvalidOperationException(
                "Six-deposit port did not produce the deterministic facies set: " +
                $"deterministic={deterministic} coal={coalSamples}/{coalParting}/{coalSeat}/" +
                $"{coalFireclay}/{coalChannel} coalPruned={coalPrunedFraction:F3} " +
                $"cinnabar={cinnabarSamples}/{cinnabarQuartz}/{cinnabarPyrite} " +
                $"tin={tinCassiterite}/{tinStannite}/{tinBismuthinite}/{tinStibnite}/{tinTourmaline} " +
                $"tinGrades=[{string.Join(",", tinGrades.Order())}] " +
                $"borate={borateSamples}/{borateGypsum} " +
                $"skarn={skarnCassiterite}/{skarnScheelite}/{skarnFluorite} " +
                $"skarnGrades=[{string.Join(",", skarnGrades.Order())}] " +
                $"alunite={aluniteSamples}/{aluniteQuartz}/{aluniteGypsum}");
        }

        return new SixDepositVerificationResult(
            coal.SeamCount,
            coalSamples,
            coalPrunedFraction,
            coalParting,
            coalSeat,
            coalFireclay,
            coalChannel,
            cinnabarSamples,
            cinnabarQuartz,
            cinnabarPyrite,
            tinCassiterite,
            tinStannite,
            tinBismuthinite,
            tinStibnite,
            tinTourmaline,
            tinSpeckleFraction,
            tinGrades.Count,
            borateSamples,
            borateGypsum,
            skarnCassiterite,
            skarnScheelite,
            skarnFluorite,
            skarnSpeckleFraction,
            skarnGrades.Count,
            aluniteSamples,
            aluniteQuartz,
            aluniteGypsum,
            deterministic);
    }

    /// <summary>
    /// Cobalt-type five-element vein coverage: the full wall-to-core arsenide succession must
    /// occur, the 10% cobaltite speckle gate must hold, grades must vary, and the deposit must
    /// contain no uranium facies at all (this variant drops uranium from the shared palette).
    /// </summary>
    public static CobaltArsenideVeinVerificationResult RunCobaltArsenideVein()
    {
        ulong featureId = ProceduralDepositMath.FeatureId(371482, 0, 6, -3);
        var settings = new CobaltArsenideVeinDefinition();
        var instance = new ProceduralDepositInstance(featureId, 0, 100, 0, 1, 0, 0, 0);
        CobaltArsenideVeinPlan plan = CobaltArsenideVeinPlan.Create(instance, settings);
        CobaltArsenideVeinPlan repeated = CobaltArsenideVeinPlan.Create(instance, settings);

        int nickeline = 0, cobaltite = 0, silver = 0, bismuth = 0, acanthite = 0;
        int pyrite = 0, carbonate = 0, erythrite = 0, annabergite = 0;
        int arsenideVoxels = 0;
        var grades = new HashSet<int>();
        bool deterministic = true;

        foreach (int surfaceY in new[] { 104, 112, 120, 128 })
        {
            for (int x = -44; x <= 44; x += 2)
            {
                for (int z = -44; z <= 44; z += 2)
                {
                    for (int y = 68; y <= surfaceY; y++)
                    {
                        CobaltArsenideVeinSample s = plan.Evaluate(x, y, z, surfaceY);
                        if (s != repeated.Evaluate(x, y, z, surfaceY)) deterministic = false;
                        if (s.Zone == CobaltArsenideVeinZone.None) continue;

                        // Skutterudite must always have resolved through the speckle gate.
                        if (s.Zone == CobaltArsenideVeinZone.Skutterudite)
                        {
                            throw new InvalidOperationException(
                                "Cobalt arsenide vein emitted an unresolved skutterudite zone");
                        }

                        if (s.Zone == CobaltArsenideVeinZone.Scorodite)
                        {
                            throw new InvalidOperationException(
                                "Cobalt arsenide vein emitted pruned scorodite");
                        }

                        if (s.Zone == CobaltArsenideVeinZone.Cobaltite && s.SpeckleNoise < 0.90)
                        {
                            throw new InvalidOperationException(
                                $"Cobalt arsenide vein placed cobaltite below the 10% speckle gate (grain {s.SpeckleNoise:F3})");
                        }

                        if (CobaltArsenideVeinPlan.IsGraded(s.Zone)) grades.Add(s.Grade);
                        else if (s.Grade != 0)
                        {
                            throw new InvalidOperationException(
                                $"Cobalt arsenide vein assigned grade {s.Grade} to ungraded zone {s.Zone}");
                        }

                        switch (s.Zone)
                        {
                            case CobaltArsenideVeinZone.Nickeline:
                            case CobaltArsenideVeinZone.NiCoDiarsenide:
                                nickeline++;
                                arsenideVoxels++;
                                break;
                            case CobaltArsenideVeinZone.Cobaltite:
                                cobaltite++;
                                arsenideVoxels++;
                                break;
                            case CobaltArsenideVeinZone.NativeSilver: silver++; break;
                            case CobaltArsenideVeinZone.NativeBismuth: bismuth++; break;
                            case CobaltArsenideVeinZone.Acanthite: acanthite++; break;
                            case CobaltArsenideVeinZone.EarlyPyrite: pyrite++; break;
                            case CobaltArsenideVeinZone.CarbonateGangue: carbonate++; break;
                            case CobaltArsenideVeinZone.Erythrite: erythrite++; break;
                            case CobaltArsenideVeinZone.Annabergite: annabergite++; break;
                        }
                    }
                }
            }
        }

        // This variant has no uranium: the zone enum must not contain a uraninite or torbernite
        // facies at all, so a palette regression cannot reintroduce one silently.
        string[] zoneNames = Enum.GetNames<CobaltArsenideVeinZone>();
        bool noUranium = true;
        foreach (string name in zoneNames)
        {
            if (name.Contains("Uranin", StringComparison.OrdinalIgnoreCase)
                || name.Contains("Torbernite", StringComparison.OrdinalIgnoreCase))
            {
                noUranium = false;
            }
        }

        double speckleFraction = arsenideVoxels > 0 ? (double)cobaltite / arsenideVoxels : 0.0;
        if (!deterministic
            || !noUranium
            || plan.PrincipalVeinCount < 4 || plan.PrincipalVeinCount > 8
            || plan.SplayCount < 1 || plan.SplayCount > 3
            || nickeline == 0 || cobaltite == 0 || silver == 0 || bismuth == 0
            || acanthite == 0 || pyrite == 0 || carbonate == 0
            || erythrite == 0 || annabergite == 0
            || grades.Count < 4)
        {
            throw new InvalidOperationException(
                "Cobalt arsenide vein plan did not produce the deterministic zonation: " +
                $"deterministic={deterministic} noUranium={noUranium} " +
                $"principal={plan.PrincipalVeinCount} splays={plan.SplayCount} " +
                $"nickeline={nickeline} cobaltite={cobaltite} speckle={speckleFraction:F3} " +
                $"silver={silver} bismuth={bismuth} acanthite={acanthite} pyrite={pyrite} " +
                $"carbonate={carbonate} erythrite={erythrite} annabergite={annabergite} " +
                $"grades=[{string.Join(",", grades.Order())}]");
        }

        return new CobaltArsenideVeinVerificationResult(
            plan.PrincipalVeinCount,
            plan.SplayCount,
            nickeline,
            cobaltite,
            speckleFraction,
            silver,
            bismuth,
            acanthite,
            pyrite,
            carbonate,
            erythrite,
            annabergite,
            grades.Count,
            noUranium,
            deterministic);
    }

    /// <summary>
    /// IOCG breccia coverage: the full copper succession plus both iron oxides and both gangue
    /// </summary>
    public static IocgBrecciaVerificationResult RunIocgBreccia()
    {
        // Chalcocite requires copper > 1.25 inside the narrow upper slab (verticalNorm > 0.45), so
        // it is genuinely rare: a survey of 40 feature ids found it in 8. This feature carries the
        // full facies suite including chalcocite, so the coverage assertion below is meaningful.
        ulong featureId = ProceduralDepositMath.FeatureId(119785, 0, 15, -15);
        var settings = new IocgBrecciaDefinition();
        var instance = new ProceduralDepositInstance(featureId, 0, 100, 0, 1, 0, 0, 0);
        IocgBrecciaPlan plan = IocgBrecciaPlan.Create(instance, settings);
        IocgBrecciaPlan repeated = IocgBrecciaPlan.Create(instance, settings);

        int bornite = 0, chalcopyrite = 0, chalcocite = 0, hematite = 0, magnetite = 0;
        int pyrite = 0, clast = 0, apatite = 0, fluorite = 0, barite = 0;
        var grades = new HashSet<int>();
        bool deterministic = true;
        bool roofPruned = true;
        int alteration = 0;

        foreach (int surfaceY in new[] { 108, 118, 128 })
        {
            for (int x = -38; x <= 38; x++)
            {
                for (int z = -38; z <= 38; z++)
                {
                    for (int y = 72; y <= surfaceY; y++)
                    {
                        IocgBrecciaSample s = plan.Evaluate(x, y, z, surfaceY);
                        if (s != repeated.Evaluate(x, y, z, surfaceY)) deterministic = false;

                        if (y - 100 > plan.GetRoofY(x, z) && s.Zone != IocgBrecciaZone.None)
                        {
                            roofPruned = false;
                        }

                        if (s.Zone == IocgBrecciaZone.None) continue;

                        if (s.Zone == IocgBrecciaZone.GraniteHost
                            || s.Zone == IocgBrecciaZone.VolcanicRoof)
                        {
                            throw new InvalidOperationException(
                                $"IOCG breccia emitted pruned host zone {s.Zone}");
                        }

                        if (s.Zone == IocgBrecciaZone.Potassic
                            || s.Zone == IocgBrecciaZone.SodicCalcic
                            || s.Zone == IocgBrecciaZone.ChloriteSericite)
                        {
                            alteration++;
                        }

                        if (IocgBrecciaPlan.IsGraded(s.Zone)) grades.Add(s.Grade);
                        else if (s.Grade != 0)
                        {
                            throw new InvalidOperationException(
                                $"IOCG breccia assigned grade {s.Grade} to ungraded zone {s.Zone}");
                        }

                        switch (s.Zone)
                        {
                            case IocgBrecciaZone.Bornite: bornite++; break;
                            case IocgBrecciaZone.Chalcopyrite: chalcopyrite++; break;
                            case IocgBrecciaZone.Chalcocite: chalcocite++; break;
                            case IocgBrecciaZone.Hematite: hematite++; break;
                            case IocgBrecciaZone.Magnetite: magnetite++; break;
                            case IocgBrecciaZone.Pyrite: pyrite++; break;
                            case IocgBrecciaZone.BrecciaClast: clast++; break;
                            case IocgBrecciaZone.ApatiteCarbonate: apatite++; break;
                            case IocgBrecciaZone.Fluorite: fluorite++; break;
                            case IocgBrecciaZone.Barite: barite++; break;
                        }
                    }
                }
            }
        }

        if (!deterministic
            || !roofPruned
            || plan.BrecciaCount < 1 || plan.BrecciaCount > 3
            || plan.FaultCount < 2 || plan.FaultCount > 5
            || bornite == 0 || chalcopyrite == 0 || chalcocite == 0
            || hematite == 0 || magnetite == 0 || pyrite == 0
            || clast == 0 || apatite == 0 || fluorite == 0 || barite == 0
            || alteration == 0
            || grades.Count < 4)
        {
            throw new InvalidOperationException(
                "IOCG breccia plan did not produce the deterministic zonation: " +
                $"deterministic={deterministic} roofPruned={roofPruned} " +
                $"breccias={plan.BrecciaCount} faults={plan.FaultCount} " +
                $"bornite={bornite} chalcopyrite={chalcopyrite} chalcocite={chalcocite} " +
                $"hematite={hematite} magnetite={magnetite} pyrite={pyrite} clast={clast} " +
                $"apatite={apatite} fluorite={fluorite} barite={barite} alteration={alteration} " +
                $"grades=[{string.Join(",", grades.Order())}]");
        }

        return new IocgBrecciaVerificationResult(
            plan.BrecciaCount,
            plan.FaultCount,
            bornite,
            chalcopyrite,
            chalcocite,
            hematite,
            magnetite,
            pyrite,
            clast,
            apatite,
            fluorite,
            barite,
            grades.Count,
            roofPruned,
            alteration,
            deterministic);
    }

    /// <summary>
    /// Sudbury contact and footwall Ni-Cu-PGE coverage: contact ore, contact veinlets, footwall
    /// veins and the offset dike must all produce their zones, PGM enrichment must be reported as
    /// </summary>
    public static SudburyContactNickelVerificationResult RunSudburyContactNickel()
    {
        ulong featureId = ProceduralDepositMath.FeatureId(556677, 0, 9, -5);
        var settings = new SudburyContactNickelDefinition();
        var instance = new ProceduralDepositInstance(featureId, 0, 110, 0, 1, 0, 0, 0);
        SudburyContactNickelPlan plan = SudburyContactNickelPlan.Create(instance, settings);
        SudburyContactNickelPlan repeated = SudburyContactNickelPlan.Create(instance, settings);

        int pentlandite = 0, chalcopyrite = 0, pyrrhotite = 0, magnetite = 0;
        int quartzDiorite = 0, gangue = 0, xenolith = 0, pgm = 0;
        var grades = new HashSet<int>();
        bool deterministic = true;
        bool hostSequencePruned = true;

        foreach (int surfaceY in new[] { 120, 135, 150 })
        {
            for (int x = -58; x <= 58; x += 2)
            {
                for (int z = -58; z <= 58; z += 2)
                {
                    for (int y = 60; y <= surfaceY; y += 1)
                    {
                        SudburyContactNickelSample s = plan.Evaluate(x, y, z, surfaceY);
                        if (s != repeated.Evaluate(x, y, z, surfaceY)) deterministic = false;
                        if (s.Zone == SudburyContactNickelZone.None) continue;

                        if (s.Zone == SudburyContactNickelZone.Norite
                            || s.Zone == SudburyContactNickelZone.UpperSic
                            || s.Zone == SudburyContactNickelZone.FootwallRock
                            || s.Zone == SudburyContactNickelZone.SudburyBreccia)
                        {
                            hostSequencePruned = false;
                        }

                        if (SudburyContactNickelPlan.IsGraded(s.Zone)) grades.Add(s.Grade);
                        else if (s.Grade != 0)
                        {
                            throw new InvalidOperationException(
                                $"Sudbury nickel assigned grade {s.Grade} to ungraded zone {s.Zone}");
                        }

                        if (s.PgmEnriched) pgm++;

                        switch (s.Zone)
                        {
                            case SudburyContactNickelZone.Pentlandite: pentlandite++; break;
                            case SudburyContactNickelZone.Chalcopyrite: chalcopyrite++; break;
                            case SudburyContactNickelZone.Pyrrhotite: pyrrhotite++; break;
                            case SudburyContactNickelZone.Magnetite: magnetite++; break;
                            case SudburyContactNickelZone.QuartzDiorite: quartzDiorite++; break;
                            case SudburyContactNickelZone.HydrothermalGangue: gangue++; break;
                            case SudburyContactNickelZone.InclusionXenolith: xenolith++; break;
                        }
                    }
                }
            }
        }

        if (!deterministic
            || !hostSequencePruned
            || plan.EmbaymentCount < 2 || plan.EmbaymentCount > 4
            || plan.OrePodCount != plan.EmbaymentCount
            || plan.FootwallVeinCount < 3 || plan.FootwallVeinCount > 6
            || plan.OffsetDikeCount < 1 || plan.OffsetDikeCount > 2
            || pentlandite == 0 || chalcopyrite == 0 || pyrrhotite == 0 || magnetite == 0
            || quartzDiorite == 0 || gangue == 0 || xenolith == 0
            || pgm == 0
            || grades.Count < 4)
        {
            throw new InvalidOperationException(
                "Sudbury contact nickel plan did not produce the deterministic zonation: " +
                $"deterministic={deterministic} hostPruned={hostSequencePruned} " +
                $"embayments={plan.EmbaymentCount} pods={plan.OrePodCount} " +
                $"veinlets={plan.ContactVeinletCount} footwall={plan.FootwallVeinCount} " +
                $"dikes={plan.OffsetDikeCount} pentlandite={pentlandite} " +
                $"chalcopyrite={chalcopyrite} pyrrhotite={pyrrhotite} magnetite={magnetite} " +
                $"diorite={quartzDiorite} gangue={gangue} xenolith={xenolith} pgm={pgm} " +
                $"grades=[{string.Join(",", grades.Order())}]");
        }

        return new SudburyContactNickelVerificationResult(
            plan.EmbaymentCount,
            plan.OrePodCount,
            plan.FootwallVeinCount,
            plan.OffsetDikeCount,
            pentlandite,
            chalcopyrite,
            pyrrhotite,
            magnetite,
            quartzDiorite,
            gangue,
            xenolith,
            pgm,
            grades.Count,
            hostSequencePruned,
            deterministic);
    }

    /// <summary>
    /// Lacustrine borate coverage: the full sodium-to-calcium borate succession plus the kernite
    /// sublens, trona, gypsum and clay partings must occur; the marker tuff and the bounding marl
    /// </summary>
    public static LacustrineBorateVerificationResult RunLacustrineBorate()
    {
        ulong featureId = ProceduralDepositMath.FeatureId(224466, 0, -8, 2);
        var settings = new LacustrineBorateDefinition();
        var instance = new ProceduralDepositInstance(featureId, 0, 100, 0, 1, 0, 0, 0);
        LacustrineBoratePlan plan = LacustrineBoratePlan.Create(instance, settings);
        LacustrineBoratePlan repeated = LacustrineBoratePlan.Create(instance, settings);

        int borax = 0, kernite = 0, ulexite = 0, colemanite = 0;
        int trona = 0, gypsum = 0, parting = 0;
        bool deterministic = true;
        bool hostPruned = true;
        bool tuffPruned = true;

        foreach (int surfaceY in new[] { 106, 114, 122 })
        {
            for (int x = -32; x <= 32; x++)
            {
                for (int z = -32; z <= 32; z++)
                {
                    for (int y = 76; y <= surfaceY; y++)
                    {
                        LacustrineBorateSample s = plan.Evaluate(x, y, z, surfaceY);
                        if (s != repeated.Evaluate(x, y, z, surfaceY)) deterministic = false;

                        double stratY = plan.GetStratY(x, y - 100, z);

                        if (Math.Abs(stratY - settings.TuffMarkerLevel) < 0.3
                            && s.Zone != LacustrineBorateZone.None)
                        {
                            tuffPruned = false;
                        }
                        if ((stratY <= settings.ClaystoneBase || stratY >= settings.ClaystoneTop)
                            && s.Zone != LacustrineBorateZone.None)
                        {
                            hostPruned = false;
                        }

                        if (s.Zone == LacustrineBorateZone.None) continue;

                        if (s.Zone == LacustrineBorateZone.HostClaystone
                            || s.Zone == LacustrineBorateZone.HostMarl
                            || s.Zone == LacustrineBorateZone.TuffMarker)
                        {
                            hostPruned = false;
                        }

                        switch (s.Zone)
                        {
                            case LacustrineBorateZone.Borax: borax++; break;
                            case LacustrineBorateZone.Kernite: kernite++; break;
                            case LacustrineBorateZone.Ulexite: ulexite++; break;
                            case LacustrineBorateZone.Colemanite: colemanite++; break;
                            case LacustrineBorateZone.Trona: trona++; break;
                            case LacustrineBorateZone.Gypsum: gypsum++; break;
                            case LacustrineBorateZone.ClayParting: parting++; break;
                        }
                    }
                }
            }
        }

        if (!deterministic
            || !hostPruned
            || !tuffPruned
            || plan.LensCount < 1 || plan.LensCount > 3
            || borax == 0 || kernite == 0 || ulexite == 0 || colemanite == 0
            || trona == 0 || gypsum == 0 || parting == 0)
        {
            throw new InvalidOperationException(
                "Lacustrine borate plan did not produce the deterministic zonation: " +
                $"deterministic={deterministic} hostPruned={hostPruned} tuffPruned={tuffPruned} " +
                $"lenses={plan.LensCount} borax={borax} kernite={kernite} ulexite={ulexite} " +
                $"colemanite={colemanite} trona={trona} gypsum={gypsum} parting={parting}");
        }

        return new LacustrineBorateVerificationResult(
            plan.LensCount,
            borax,
            kernite,
            ulexite,
            colemanite,
            trona,
            gypsum,
            parting,
            hostPruned,
            tuffPruned,
            true,
            deterministic);
    }

    public static string RunLacustrineAlum()
    {
        ulong featureId = ProceduralDepositMath.FeatureId(224466, 0, -8, 2);
        var settings = new LacustrineBorateDefinition { Family = "alum" };
        var instance = new ProceduralDepositInstance(featureId, 0, 100, 0, 1, 0, 0, 0);
        LacustrineBoratePlan plan = LacustrineBoratePlan.Create(instance, settings);
        LacustrineBoratePlan repeated = LacustrineBoratePlan.Create(instance, settings);
        int kalinite = 0, alunogen = 0, halotrichite = 0, alunite = 0, gypsum = 0, parting = 0;
        bool deterministic = true, noBorates = true;
        for (int x = -32; x <= 32; x++)
        for (int z = -32; z <= 32; z++)
        for (int y = 76; y <= 122; y++)
        {
            LacustrineBorateSample sample = plan.Evaluate(x, y, z, 122);
            deterministic &= sample == repeated.Evaluate(x, y, z, 122);
            noBorates &= sample.Zone is not (LacustrineBorateZone.Borax or LacustrineBorateZone.Kernite
                or LacustrineBorateZone.Ulexite or LacustrineBorateZone.Colemanite or LacustrineBorateZone.Trona);
            switch (sample.Zone)
            {
                case LacustrineBorateZone.AlumKalinite: kalinite++; break;
                case LacustrineBorateZone.AlumAlunogen: alunogen++; break;
                case LacustrineBorateZone.AlumHalotrichite: halotrichite++; break;
                case LacustrineBorateZone.AlumAlunite: alunite++; break;
                case LacustrineBorateZone.Gypsum: gypsum++; break;
                case LacustrineBorateZone.ClayParting: parting++; break;
            }
        }
        bool familyValidation = LacustrineBorateProceduralTemplate.ValidateSettings(settings, out _)
            && !LacustrineBorateProceduralTemplate.ValidateSettings(new LacustrineBorateDefinition { Family = "invalid" }, out _);
        if (!deterministic || !noBorates || !familyValidation || kalinite == 0 || alunogen == 0
            || halotrichite == 0 || alunite == 0 || gypsum == 0 || parting == 0)
        {
            throw new InvalidOperationException($"Lacustrine alum contract failed: deterministic={deterministic} noBorates={noBorates} family={familyValidation} alum={kalinite}/{alunogen}/{halotrichite}/{alunite} gypsum={gypsum} parting={parting}");
        }
        return $"lacustrineAlum lenses={plan.LensCount} kalinite={kalinite} alunogen={alunogen} halotrichite={halotrichite} alunite={alunite} gypsum={gypsum} parting={parting} noBorates={noBorates} deterministic={deterministic}";
    }

    public static PorphyryCopperMolyVerificationResult RunPorphyryCopperMoly()
    {
        var settings = new PorphyryCopperMolyDefinition();
        PorphyryCopperMolyPlan? plan = null;
        PorphyryCopperMolyPlan? repeated = null;
        for (int probe = 0; probe < 64; probe++)
        {
            ulong id = ProceduralDepositMath.FeatureId(731111 + probe * 7919, 0, probe, -probe);
            var instance = new ProceduralDepositInstance(id, 0, 100, 0, 1, 0, 0, 0);
            PorphyryCopperMolyPlan candidate = PorphyryCopperMolyPlan.Create(instance, settings);
            if (!candidate.HasInternalSupergene) continue;
            plan = candidate;
            repeated = PorphyryCopperMolyPlan.Create(instance, settings);
            break;
        }
        if (plan is null || repeated is null) throw new InvalidOperationException("No porphyry verification feature enabled internal supergene");

        int bornite = 0, chalcopyrite = 0, molybdenite = 0, chalcocite = 0;
        int quartz = 0, pyrite = 0, disseminated = 0, gossan = 0, limonite = 0;
        var grades = new HashSet<int>();
        bool deterministic = true;
        bool bounds = true;

        foreach (int surfaceY in new[] { 104, 112, 120, 128 })
        {
            for (int x = -35; x <= 35; x++)
            for (int z = -35; z <= 35; z++)
            for (int y = 70; y <= surfaceY; y++)
            {
                PorphyryCopperMolySample s = plan.Evaluate(x, y, z, surfaceY, true);
                if (s != repeated.Evaluate(x, y, z, surfaceY, true)) deterministic = false;
                if (s.Disseminated) disseminated++;
                if (PorphyryCopperMolyPlan.IsGraded(s.Zone)) grades.Add(s.Grade);
                int depth = surfaceY - y + 1;
                if (s.Zone == PorphyryCopperMolyZone.Gossan && (depth < 1 || depth > 3)) bounds = false;
                if (s.Zone == PorphyryCopperMolyZone.Limonite && (depth < 4 || depth > 8)) bounds = false;
                switch (s.Zone)
                {
                    case PorphyryCopperMolyZone.Bornite: bornite++; break;
                    case PorphyryCopperMolyZone.Chalcopyrite: chalcopyrite++; break;
                    case PorphyryCopperMolyZone.Molybdenite: molybdenite++; break;
                    case PorphyryCopperMolyZone.Chalcocite: chalcocite++; break;
                    case PorphyryCopperMolyZone.Quartz: quartz++; break;
                    case PorphyryCopperMolyZone.Pyrite: pyrite++; break;
                    case PorphyryCopperMolyZone.Gossan: gossan++; break;
                    case PorphyryCopperMolyZone.Limonite: limonite++; break;
                }
            }
        }

        if (!deterministic || !bounds || plan.VeinletCount < 28 || plan.VeinletCount > 40
            || bornite == 0 || chalcopyrite == 0 || molybdenite == 0 || chalcocite == 0
            || quartz == 0 || pyrite == 0 || disseminated == 0 || gossan == 0 || limonite == 0
            || grades.Count < 4)
        {
            throw new InvalidOperationException($"Porphyry Cu-Mo coverage failed: veins={plan.VeinletCount} bornite={bornite} chalcopyrite={chalcopyrite} molybdenite={molybdenite} chalcocite={chalcocite} quartz={quartz} pyrite={pyrite} disseminated={disseminated} gossan={gossan} limonite={limonite} grades={grades.Count} bounds={bounds} deterministic={deterministic}");
        }
        return new PorphyryCopperMolyVerificationResult(plan.VeinletCount, bornite, chalcopyrite,
            molybdenite, chalcocite, quartz, pyrite, disseminated, gossan, limonite,
            grades.Count, bounds, deterministic);
    }

    public static StratiformCopperVerificationResult RunStratiformCopper()
    {
        var settings = new StratiformCopperDefinition();
        StratiformCopperPlan? plan = null;
        StratiformCopperPlan? repeated = null;
        int native = 0, chalcocite = 0, bornite = 0, chalcopyrite = 0, malachite = 0;
        int azurite = 0, pyrite = 0, carbonate = 0, gossan = 0, limonite = 0;
        int lensCount = 0, faultCount = 0;
        var grades = new HashSet<int>();
        bool deterministic = true;
        bool oreRequiresLens = true;

    // The malachite/azurite gate is deliberately rare (1.5% before the second
        // split), so aggregate deterministic features rather than weakening its threshold.
        for (int probe = 0; probe < 24; probe++)
        {
            ulong id = ProceduralDepositMath.FeatureId(842221 + probe * 3571, 0, probe, probe + 4);
            var instance = new ProceduralDepositInstance(id, 0, 100, 0, 1, 0, 0, 0);
            StratiformCopperPlan candidate = StratiformCopperPlan.Create(instance, settings);
            StratiformCopperPlan again = StratiformCopperPlan.Create(instance, settings);
            plan = candidate;
            repeated = again;
            lensCount += candidate.LensCount;
            faultCount += candidate.FaultCount;
            foreach (int surfaceY in new[] { 104, 112, 120 })
            {
                for (int x = -37; x <= 37; x += 2)
                for (int z = -37; z <= 37; z += 2)
                for (int y = 70; y <= surfaceY; y++)
                {
                    StratiformCopperSample s = candidate.Evaluate(x, y, z, surfaceY, true);
                    if (s != again.Evaluate(x, y, z, surfaceY, true)) deterministic = false;
                    bool ore = s.Zone is StratiformCopperZone.NativeCopper or StratiformCopperZone.Chalcocite
                        or StratiformCopperZone.Bornite or StratiformCopperZone.Chalcopyrite
                        or StratiformCopperZone.Malachite or StratiformCopperZone.Azurite
                        or StratiformCopperZone.Pyrite or StratiformCopperZone.Carbonate
                        or StratiformCopperZone.Gossan or StratiformCopperZone.Limonite;
                    if (ore && !s.InLens) oreRequiresLens = false;
                    if (StratiformCopperPlan.IsGraded(s.Zone)) grades.Add(s.Grade);
                    switch (s.Zone)
                    {
                        case StratiformCopperZone.NativeCopper: native++; break;
                        case StratiformCopperZone.Chalcocite: chalcocite++; break;
                        case StratiformCopperZone.Bornite: bornite++; break;
                        case StratiformCopperZone.Chalcopyrite: chalcopyrite++; break;
                        case StratiformCopperZone.Malachite: malachite++; break;
                        case StratiformCopperZone.Azurite: azurite++; break;
                        case StratiformCopperZone.Pyrite: pyrite++; break;
                        case StratiformCopperZone.Carbonate: carbonate++; break;
                        case StratiformCopperZone.Gossan: gossan++; break;
                        case StratiformCopperZone.Limonite: limonite++; break;
                    }
                }
            }
        }
        if (plan is null || repeated is null || !deterministic || !oreRequiresLens
            || native == 0 || chalcocite == 0 || bornite == 0 || chalcopyrite == 0
            || malachite == 0 || azurite == 0 || pyrite == 0 || carbonate == 0
            || gossan == 0 || limonite == 0 || grades.Count < 4)
        {
            throw new InvalidOperationException($"Stratiform copper coverage failed: native={native} chalcocite={chalcocite} bornite={bornite} chalcopyrite={chalcopyrite} malachite={malachite} azurite={azurite} pyrite={pyrite} carbonate={carbonate} gossan={gossan} limonite={limonite} grades={grades.Count} lensOnly={oreRequiresLens} deterministic={deterministic}");
        }
        return new StratiformCopperVerificationResult(lensCount, faultCount, native, chalcocite,
            bornite, chalcopyrite, malachite, azurite, pyrite, carbonate, gossan, limonite,
            grades.Count, oreRequiresLens, deterministic);
    }

    public static ClimateFilterVerificationResult RunClimateFilter()
    {
        var disabled = new ProceduralClimateDefinition
        {
            Enabled = false,
            MinRain = 0.8,
            MaxRain = 0.2,
            MinTemp = 30,
            MaxTemp = -10
        };
        bool disabledBypasses = ProceduralDepositWorldGenSystem.MatchesClimate(disabled, 0.5, 12.0);

        var dryWarm = new ProceduralClimateDefinition
        {
            Enabled = true,
            MinRain = 0.0,
            MaxRain = 0.30,
            MinTemp = 5.0,
            MaxTemp = 50.0
        };
        bool inclusive = ProceduralDepositWorldGenSystem.MatchesClimate(dryWarm, 0.0, 5.0)
            && ProceduralDepositWorldGenSystem.MatchesClimate(dryWarm, 0.30, 50.0);
        bool rejectsOutside = !ProceduralDepositWorldGenSystem.MatchesClimate(dryWarm, -0.001, 20.0)
            && !ProceduralDepositWorldGenSystem.MatchesClimate(dryWarm, 0.301, 20.0)
            && !ProceduralDepositWorldGenSystem.MatchesClimate(dryWarm, 0.2, 4.999)
            && !ProceduralDepositWorldGenSystem.MatchesClimate(dryWarm, 0.2, 50.001);

        bool rejectsInvalid = !ProceduralDepositWorldGenSystem.HasValidClimateBounds(new ProceduralClimateDefinition
            {
                MinRain = -0.01
            })
            && !ProceduralDepositWorldGenSystem.HasValidClimateBounds(new ProceduralClimateDefinition
            {
                MaxRain = 1.01
            })
            && !ProceduralDepositWorldGenSystem.HasValidClimateBounds(new ProceduralClimateDefinition
            {
                MinRain = 0.7,
                MaxRain = 0.3
            })
            && !ProceduralDepositWorldGenSystem.HasValidClimateBounds(new ProceduralClimateDefinition
            {
                MinTemp = 20,
                MaxTemp = 10
            })
            && !ProceduralDepositWorldGenSystem.HasValidClimateBounds(new ProceduralClimateDefinition
            {
                MinTemp = -101
            })
            && !ProceduralDepositWorldGenSystem.HasValidClimateBounds(new ProceduralClimateDefinition
            {
                MaxTemp = 101
            })
            && ProceduralDepositWorldGenSystem.HasValidClimateBounds(new ProceduralClimateDefinition());

        if (!disabledBypasses || !inclusive || !rejectsOutside || !rejectsInvalid)
        {
            throw new InvalidOperationException(
                "Climate filter contract failed: " +
                $"disabled={disabledBypasses} inclusive={inclusive} " +
                $"outside={rejectsOutside} invalid={rejectsInvalid}");
        }

        return new ClimateFilterVerificationResult(
            disabledBypasses,
            inclusive,
            rejectsOutside,
            rejectsInvalid);
    }

    public static ProceduralEngineVerificationResult RunEngineHardening()
    {
        bool priorityValid = ProceduralDepositMath.CompareDepositOrder(200, 9, 100, 1) < 0
            && ProceduralDepositMath.CompareDepositOrder(100, 1, 100, 9) < 0;
        if (!priorityValid) throw new InvalidOperationException("Deposit priority ordering is not deterministic");

        bool registryValid = ProceduralDepositTemplateRegistry.TryGet("ellipsoid", out IProceduralDepositTemplate ellipsoid)
            && ProceduralDepositTemplateRegistry.TryGet("epithermalVein", out IProceduralDepositTemplate epithermal)
            && ProceduralDepositTemplateRegistry.TryGet("sheetedPlate", out IProceduralDepositTemplate sheetedPlate)
            && ProceduralDepositTemplateRegistry.TryGet("splineNetwork", out IProceduralDepositTemplate spline)
            && ProceduralDepositTemplateRegistry.TryGet("ooliticIronstone", out IProceduralDepositTemplate oolitic)
            && ellipsoid.RequiredMaterialSlots.Contains(ProceduralMaterialSlots.Primary)
            && epithermal.RequiredMaterialSlots.Contains(ProceduralMaterialSlots.NativeSilver)
            && sheetedPlate.RequiredMaterialSlots.Contains(ProceduralMaterialSlots.Primary)
            && spline.RequiredMaterialSlots.Contains(ProceduralMaterialSlots.Core)
            && oolitic.RequiredMaterialSlots.Contains(ProceduralMaterialSlots.Hematite);
        if (!registryValid) throw new InvalidOperationException("Procedural template registry is incomplete");

        bool suppressionValid =
            AdvancedGeologyModSystem.IsConventionalOreDepositAssetPath("worldgen/deposits/metalore/iron.json")
            && AdvancedGeologyModSystem.IsConventionalOreDepositAssetPath("worldgen/deposits/mineralore/quartz.json")
            && AdvancedGeologyModSystem.IsConventionalOreDepositAssetPath("worldgen/deposits/coal.json")
            && AdvancedGeologyModSystem.IsConventionalOreDepositAssetPath("worldgen/deposits/customore/future.json")
            && !AdvancedGeologyModSystem.IsConventionalOreDepositAssetPath("worldgen/deposits/gem/diamond.json")
            && !AdvancedGeologyModSystem.IsConventionalOreDepositAssetPath("worldgen/deposits/rock/marble.json")
            && !AdvancedGeologyModSystem.IsConventionalOreDepositAssetPath("worldgen/deposits/soil/clay.json")
            && !AdvancedGeologyModSystem.IsConventionalOreDepositAssetPath("worldgen/proceduraldeposits/lct-pegmatite.json")
            && !AdvancedGeologyModSystem.IsConventionalOreDepositAssetPath("blocktypes/stone/ore-graded.json");
        if (!suppressionValid)
        {
            throw new InvalidOperationException("Exclusive ore generation boundary includes a preserved category");
        }

        bool intrusionValid =
            CompiledProceduralDeposit.AllowsRockReplacement(true, false, false)
            && CompiledProceduralDeposit.AllowsRockReplacement(true, true, false)
            && CompiledProceduralDeposit.AllowsRockReplacement(false, true, true)
            && !CompiledProceduralDeposit.AllowsRockReplacement(false, false, true)
            && !CompiledProceduralDeposit.AllowsRockReplacement(false, true, false);
        if (!intrusionValid)
        {
            throw new InvalidOperationException("Intrusion policy replaces non-rocks or ignores eligible source hosts");
        }

        bool terrainClampValid = ProceduralDepositWorldGenSystem.ClampWorldRelativeCenter(90, 100, 35) == 65
            && ProceduralDepositWorldGenSystem.ClampWorldRelativeCenter(40, 100, 35) == 40
            && ProceduralDepositWorldGenSystem.ClampWorldRelativeCenter(40, 20, 35) == 0;
        if (!terrainClampValid)

        {
            throw new InvalidOperationException("World-relative placement does not keep the deposit roof underground");
        }

        var buriedVariants = new Dictionary<int, int> { [17] = 19 };
        bool buriedWeatheringValid = CompiledProceduralDeposit.SelectWeatheringVariant(17, false, buriedVariants) == 17
            && CompiledProceduralDeposit.SelectWeatheringVariant(17, true, buriedVariants) == 19
            && CompiledProceduralDeposit.SelectWeatheringVariant(23, true, buriedVariants) == 23;
        if (!buriedWeatheringValid)
        {
            throw new InvalidOperationException("Buried weathering did not select the no-grass block variant");
        }

        bool looseSedimentProtected = !ProceduralDepositWorldGenSystem.IsWeatheringReplacementMaterial(EnumBlockMaterial.Sand, "sand-granite")
            && !ProceduralDepositWorldGenSystem.IsWeatheringReplacementMaterial(EnumBlockMaterial.Gravel, "gravel-granite")
            && !ProceduralDepositWorldGenSystem.IsWeatheringReplacementMaterial(EnumBlockMaterial.Gravel, "muddygravel")
            && ProceduralDepositWorldGenSystem.IsWeatheringReplacementMaterial(EnumBlockMaterial.Soil, "soil-medium-none")
            && ProceduralDepositWorldGenSystem.IsWeatheringReplacementMaterial(EnumBlockMaterial.Stone, "rock-granite");
        if (!looseSedimentProtected)
        {
            throw new InvalidOperationException("Weathering replacement admits sand, gravel, or muddy gravel");
        }

        return new ProceduralEngineVerificationResult(
            priorityValid,
            registryValid,
            suppressionValid,
            intrusionValid,
            terrainClampValid,
            buriedWeatheringValid,
            looseSedimentProtected);
    }
    public static string RunSixNewDeposits()
    {
        var instance = new ProceduralDepositInstance(0x5EEDUL, 100, 80, 100, 1, 0, 0, 0);
        VeinGraphitePlan vein = VeinGraphitePlan.Create(instance, new VeinGraphiteDefinition());
        FlakeGraphiteSchistPlan flake = FlakeGraphiteSchistPlan.Create(instance, new FlakeGraphiteSchistDefinition());
        SparryMagnesitePlan sparry = SparryMagnesitePlan.Create(instance, new SparryMagnesiteDefinition());
        CryptocrystallineMagnesitePlan crypto = CryptocrystallineMagnesitePlan.Create(instance, new CryptocrystallineMagnesiteDefinition());
        UnconformityUraniumPlan uranium = UnconformityUraniumPlan.Create(instance, new UnconformityUraniumDefinition());
        PeralkalineHreePlan hree = PeralkalineHreePlan.Create(instance, new PeralkalineHreeDefinition());

        bool counts = vein.PrincipalCount is >= 3 and <= 5 && vein.BranchCount is >= 1 and <= 2
            && flake.HorizonCount is >= 2 and <= 3 && flake.ShearCount is >= 2 and <= 4
            && sparry.FaultCount is >= 2 and <= 4 && sparry.MantoCount is >= 3 and <= 5
            && crypto.VeinCount is >= 5 and <= 8 && crypto.PodCount is >= 5 and <= 9
            && uranium.FaultCount is >= 2 and <= 3 && uranium.PerchedCount is >= 0 and <= 2
            && hree.PegmatiteCount is >= 3 and <= 5 && hree.FractureCount is >= 3 and <= 5;
        bool deterministic = vein.Evaluate(100, 80, 100) == VeinGraphitePlan.Create(instance, new VeinGraphiteDefinition()).Evaluate(100, 80, 100)
            && flake.Evaluate(100, 80, 100) == FlakeGraphiteSchistPlan.Create(instance, new FlakeGraphiteSchistDefinition()).Evaluate(100, 80, 100)
            && sparry.Evaluate(100, 80, 100) == SparryMagnesitePlan.Create(instance, new SparryMagnesiteDefinition()).Evaluate(100, 80, 100)
            && crypto.Evaluate(100, 80, 100) == CryptocrystallineMagnesitePlan.Create(instance, new CryptocrystallineMagnesiteDefinition()).Evaluate(100, 80, 100)
            && uranium.Evaluate(100, 80, 100) == UnconformityUraniumPlan.Create(instance, new UnconformityUraniumDefinition()).Evaluate(100, 80, 100)
            && hree.Evaluate(100, 80, 100) == PeralkalineHreePlan.Create(instance, new PeralkalineHreeDefinition()).Evaluate(100, 80, 100);
        int olivine = 0, soapstone = 0, magnesite = 0, quartz = 0;
        int uraniumOre = 0, uraniumHematite = 0;
        var hematiteColumns = new HashSet<(int X, int Z)>();
        for (int x = 66; x <= 134; x++)
        {
            for (int z = 66; z <= 134; z++)
            {
                for (int y = 52; y <= 108; y++)
                {
                    AdditionalDepositSample sample = crypto.Evaluate(x, y, z);
                    if (sample.Slot == ProceduralMaterialSlots.Olivine) olivine++;
                    else if (sample.Slot == ProceduralMaterialSlots.Soapstone) soapstone++;
                    else if (sample.Slot == ProceduralMaterialSlots.Magnesite) magnesite++;
                    else if (sample.Slot == ProceduralMaterialSlots.Quartz) quartz++;
                }
            }
        }
        for (int x = 62; x <= 138; x++)
        {
            for (int z = 62; z <= 138; z++)
            {
                for (int y = 46; y <= 114; y++)
                {
                    AdditionalDepositSample sample = uranium.Evaluate(x, y, z);
                    if (sample.Slot == ProceduralMaterialSlots.Uraninite) uraniumOre++;
                    else if (sample.Slot == ProceduralMaterialSlots.Hematite)
                    {
                        uraniumHematite++;
                        hematiteColumns.Add((x, z));
                    }
                }
            }
        }
        bool finiteHematite = uraniumHematite > 0
            && hematiteColumns.Count < 1800
            && hematiteColumns.All(column => column.X is > 62 and < 138 && column.Z is > 62 and < 138);
        if (!counts || !deterministic || olivine == 0 || soapstone == 0 || magnesite == 0 || quartz == 0
            || uraniumOre == 0 || !finiteHematite)
        {
            throw new InvalidOperationException(
                $"Six-deposit plan contract failed: counts={counts} deterministic={deterministic} " +
                $"olivine={olivine} soapstone={soapstone} magnesite={magnesite} quartz={quartz} " +
                $"uranium={uraniumOre} hematite={uraniumHematite}/{hematiteColumns.Count} finiteHematite={finiteHematite}");
        }
        return $"sixDeposits counts={counts} deterministic={deterministic} " +
            $"olivine={olivine} soapstone={soapstone} magnesite={magnesite} quartz={quartz} " +
            $"uranium={uraniumOre} hematite={uraniumHematite}/{hematiteColumns.Count} finiteHematite={finiteHematite}";
    }

    public static string RunMessinianSulfur()
    {
        var instance = new ProceduralDepositInstance(0x51F0A7UL, 0, 100, 0, 1, 0, 0, 0);
        var settings = new MessinianSulfurDefinition();
        MessinianSulfurPlan plan = MessinianSulfurPlan.Create(instance, settings);
        MessinianSulfurPlan repeated = MessinianSulfurPlan.Create(instance, settings);
        int high = 0, disseminated = 0, carbonate = 0, primaryGypsum = 0, burialGypsum = 0, breccia = 0, celestine = 0;
        bool deterministic = true;
        for (int x = -35; x <= 35; x++)
        for (int z = -35; z <= 35; z++)
        for (int y = -30; y <= 30; y++)
        {
            MessinianSulfurSample sample = plan.Evaluate(x, 100 + y, z);
            deterministic &= sample == repeated.Evaluate(x, 100 + y, z);
            switch (sample.Zone)
            {
                case MessinianSulfurZone.HighGradeSulfur: high++; break;
                case MessinianSulfurZone.DisseminatedSulfur: disseminated++; break;
                case MessinianSulfurZone.AuthigenicCarbonate:
                case MessinianSulfurZone.CalciteAragonite: carbonate++; break;
                case MessinianSulfurZone.PrimaryGypsum: primaryGypsum++; break;
                case MessinianSulfurZone.BurialGypsum: burialGypsum++; break;
                case MessinianSulfurZone.EvaporiteBreccia: breccia++; break;
                case MessinianSulfurZone.Celestine: celestine++; break;
            }
        }
        int gem = 0;
        const int trials = 100000;
        for (int i = 0; i < trials; i++)
        {
            if (ProceduralDepositMath.CoordinateNoise(instance.FeatureId, i, i % 211, i % 997, 0x43454C455354474DUL) < settings.CelestineGemFraction) gem++;
        }
        double gemFraction = gem / (double)trials;
        bool valid = deterministic && plan.LensCount is >= 3 and <= 5 && plan.BrecciaCount is >= 2 and <= 4
            && high > 0 && disseminated > 0 && carbonate > 0 && primaryGypsum > 0 && burialGypsum > 0 && breccia > 0 && celestine > 0
            && gemFraction is > .095 and < .105;
        if (!valid) throw new InvalidOperationException($"Messinian sulfur contract failed: high={high} disseminated={disseminated} carbonate={carbonate} gypsum={primaryGypsum}/{burialGypsum} bre breccia={breccia} celestine={celestine} gem={gemFraction:F3} deterministic={deterministic}");
        return $"messinianSulfur lenses={plan.LensCount} breccias={plan.BrecciaCount} sulfur={high}/{disseminated} gypsum={primaryGypsum}/{burialGypsum} celestine={celestine} gemFraction={gemFraction:F3} deterministic={deterministic}";
    }

    public static string RunResourceGapDeposits()
    {
        var instance = new ProceduralDepositInstance(0x6A9C13UL, 0, 100, 0, 1, 0, 0, 0);
        OrogenicQuartzGoldPlan gold = OrogenicQuartzGoldPlan.Create(instance, new OrogenicQuartzGoldDefinition());
        StratiformChromititePlan chromitite = StratiformChromititePlan.Create(instance, new StratiformChromititeDefinition());
        MarinePhosphoritePlan phosphorite = MarinePhosphoritePlan.Create(instance, new MarinePhosphoriteDefinition());
        LapisLazuliMarblePlan lapis = LapisLazuliMarblePlan.Create(instance, new LapisLazuliMarbleDefinition());
        IvittuutCryolitePlan cryolite = IvittuutCryolitePlan.Create(instance, new IvittuutCryoliteDefinition());
        AnorthositeIlmenitePlan ilmenite = AnorthositeIlmenitePlan.Create(instance, new AnorthositeIlmeniteDefinition());

        var counts = new Dictionary<string, int>(StringComparer.Ordinal);
        bool deterministic = true;
        IAdditionalDepositPlan[] plans = [gold, chromitite, phosphorite, lapis, cryolite, ilmenite];
        IAdditionalDepositPlan[] repeats =
        [
            OrogenicQuartzGoldPlan.Create(instance, new OrogenicQuartzGoldDefinition()),
            StratiformChromititePlan.Create(instance, new StratiformChromititeDefinition()),
            MarinePhosphoritePlan.Create(instance, new MarinePhosphoriteDefinition()),
            LapisLazuliMarblePlan.Create(instance, new LapisLazuliMarbleDefinition()),
            IvittuutCryolitePlan.Create(instance, new IvittuutCryoliteDefinition()),
            AnorthositeIlmenitePlan.Create(instance, new AnorthositeIlmeniteDefinition())
        ];
        for (int p = 0; p < plans.Length; p++)
        for (int x = -34; x <= 34; x += 1)
        for (int z = -34; z <= 34; z += 1)
        for (int y = -28; y <= 28; y += 1)
        {
            AdditionalDepositSample sample = plans[p].Evaluate(x, 100 + y, z);
            deterministic &= sample == repeats[p].Evaluate(x, 100 + y, z);
            if (sample.Slot == null) continue;
            counts[sample.Slot] = counts.GetValueOrDefault(sample.Slot) + 1;
        }

        int chromiteMaxY = int.MinValue, titanomagnetiteMinY = int.MaxValue;
        for (int x = -34; x <= 34; x++)
        for (int z = -34; z <= 34; z++)
        for (int y = -34; y <= 34; y++)
        {
            AdditionalDepositSample sample = chromitite.Evaluate(x, 100 + y, z);
            if (sample.Slot == ProceduralMaterialSlots.Chromite) chromiteMaxY = Math.Max(chromiteMaxY, y);
            else if (sample.Slot == ProceduralMaterialSlots.Titanomagnetite) titanomagnetiteMinY = Math.Min(titanomagnetiteMinY, y);
        }

        bool planCounts = gold.LodeCount is >= 3 and <= 5 && gold.SplayCount is >= 2 and <= 4
            && chromitite.ReefCount is >= 3 and <= 4
            && chromitite.TitanomagnetiteLayerCount is >= 3 and <= 5
            && chromitite.DykeCount is >= 1 and <= 3
            && phosphorite.BedCount is >= 2 and <= 4 && Math.Abs(phosphorite.Shelf) == 1
            && lapis.LensCount is >= 3 and <= 5
            && cryolite.VeinCount is >= 3 and <= 5
            && ilmenite.BodyCount is >= 3 and <= 6 && ilmenite.DykeCount is >= 1 and <= 2;
        string[] required =
        [
            ProceduralMaterialSlots.Gold, ProceduralMaterialSlots.Chromite, ProceduralMaterialSlots.Titanomagnetite,
            ProceduralMaterialSlots.CriticalZone, ProceduralMaterialSlots.MainZone, ProceduralMaterialSlots.UpperZone,
            ProceduralMaterialSlots.Phosphorite, ProceduralMaterialSlots.Apatite, ProceduralMaterialSlots.Lapis,
            ProceduralMaterialSlots.Cryolite, ProceduralMaterialSlots.Ilmenite, ProceduralMaterialSlots.Dyke
        ];
        var missing = required.Where(slot => counts.GetValueOrDefault(slot) == 0).ToArray();
        bool separatedPackages = chromiteMaxY < titanomagnetiteMinY
            && titanomagnetiteMinY - chromiteMaxY >= 12;
        if (!planCounts || !deterministic || missing.Length > 0 || !separatedPackages)
        {
            throw new InvalidOperationException(
                $"Resource-gap deposit contract failed: counts={planCounts} deterministic={deterministic} " +
                $"missing={string.Join(",", missing)} chromiteMaxY={chromiteMaxY} titanomagnetiteMinY={titanomagnetiteMinY}");
        }
        return $"resourceGap gold={counts.GetValueOrDefault(ProceduralMaterialSlots.Gold)} chromite={counts.GetValueOrDefault(ProceduralMaterialSlots.Chromite)} " +
            $"titanomagnetite={counts.GetValueOrDefault(ProceduralMaterialSlots.Titanomagnetite)} " +
            $"critical/main/upper={counts.GetValueOrDefault(ProceduralMaterialSlots.CriticalZone)}/{counts.GetValueOrDefault(ProceduralMaterialSlots.MainZone)}/{counts.GetValueOrDefault(ProceduralMaterialSlots.UpperZone)} " +
            $"chromiteMaxY={chromiteMaxY} titanomagnetiteMinY={titanomagnetiteMinY} " +
            $"phosphorite={counts.GetValueOrDefault(ProceduralMaterialSlots.Phosphorite)} apatite={counts.GetValueOrDefault(ProceduralMaterialSlots.Apatite)} " +
            $"lapis={counts.GetValueOrDefault(ProceduralMaterialSlots.Lapis)} cryolite={counts.GetValueOrDefault(ProceduralMaterialSlots.Cryolite)} " +
            $"ilmenite={counts.GetValueOrDefault(ProceduralMaterialSlots.Ilmenite)} deterministic={deterministic}";
    }


    private static int CountSmearFootprint(int apexY, int height, int targetSurfaceY)
    {
        int covered = 0;
        for (int offsetX = -height; offsetX <= height; offsetX++)
        {
            for (int offsetZ = -height; offsetZ <= height; offsetZ++)
            {
                int radius = Math.Max(Math.Abs(offsetX), Math.Abs(offsetZ));
                if (ProceduralDepositMath.SmearCovers(apexY, height, radius, targetSurfaceY)) covered++;
            }
        }
        return covered;
    }

    private static HashSet<(int X, int Y, int Z)> GenerateByChunkOrder(
        in ProceduralDepositInstance instance,
        EllipsoidGeometryDefinition geometry,
        int[] chunkOrder)
    {
        var voxels = new HashSet<(int X, int Y, int Z)>();
        foreach (int chunkX in chunkOrder)
        {
            int minX = chunkX * 32;
            int maxX = minX + 31;
            for (int x = minX; x <= maxX; x++)
            {
                for (int z = -10; z <= 10; z++)
                {
                    ProceduralColumnSample column = ProceduralDepositMath.SampleColumn(instance, geometry, x, z);
                    if (!column.Intersects) continue;
                    for (int y = (int)Math.Floor(column.BaseY); y <= (int)Math.Ceiling(column.RoofY); y++)
                    {
                        if (ProceduralDepositMath.EllipsoidMetric(instance, geometry, x, y, z) <= 1.0)
                        {
                            voxels.Add((x, y, z));
                        }
                    }
                }
            }
        }
        return voxels;
    }

    private static void CountWeathering(
        in ProceduralDepositInstance instance,
        EllipsoidGeometryDefinition geometry,
        int surfaceY,
        SupergeneDefinition supergene,
        out int primary,
        out int oxide,
        out int enriched)
    {
        primary = 0;
        oxide = 0;
        enriched = 0;
        for (int x = -40; x <= 40; x++)
        {
            for (int z = -8; z <= 8; z++)
            {
                ProceduralColumnSample column = ProceduralDepositMath.SampleColumn(instance, geometry, x, z);
                if (!column.Intersects) continue;
                int maxY = Math.Min(surfaceY, (int)Math.Ceiling(column.RoofY));
                for (int y = (int)Math.Floor(column.BaseY); y <= maxY; y++)
                {
                    if (ProceduralDepositMath.EllipsoidMetric(instance, geometry, x, y, z) > 1.0) continue;
                    switch (ProceduralDepositMath.SelectMaterialZone(
                        surfaceY,
                        y,
                        supergene.OxidationDepth,
                        supergene.EnrichmentThickness))
                    {
                        case EllipsoidMaterialZone.Primary:
                            primary++;
                            break;
                        case EllipsoidMaterialZone.Oxide:
                            oxide++;
                            break;
                        case EllipsoidMaterialZone.Enriched:
                            enriched++;
                            break;
                    }
                }
            }
        }
    }
}
