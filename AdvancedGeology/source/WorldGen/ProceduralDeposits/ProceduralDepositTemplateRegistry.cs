using System;
using System.Collections.Generic;
using Vintagestory.API.Server;

namespace AdvancedGeology.WorldGen.ProceduralDeposits;

internal interface IProceduralDepositTemplate
{
    string Code { get; }
    IReadOnlyList<string> RequiredMaterialSlots { get; }
    bool RequiresPlan { get; }
    int GetMaximumHorizontalReach(ProceduralDepositDefinition definition);
    object? CreatePlan(in ProceduralDepositInstance instance, ProceduralDepositDefinition definition);
    bool Validate(ProceduralDepositDefinition definition, out string error);
    void Realize(
        ProceduralDepositWorldGenSystem system,
        in DepositCandidate candidate,
        IChunkColumnGenerateRequest request,
        int baseX,
        int baseZ);
}

internal static class ProceduralDepositTemplateRegistry
{
    private static readonly Dictionary<string, IProceduralDepositTemplate> Templates =
        new(StringComparer.OrdinalIgnoreCase);

    static ProceduralDepositTemplateRegistry()
    {
        Register(new EllipsoidProceduralTemplate());
        Register(new EpithermalProceduralTemplate());
        Register(new SheetedPlateProceduralTemplate());
        Register(new SplineNetworkProceduralTemplate());
        Register(new OoliticIronstoneProceduralTemplate());
        Register(new GreisenStockworkProceduralTemplate());
        Register(new PrabornaPyrolusiteProceduralTemplate());
        Register(new CarbonatiteComplexProceduralTemplate());
        Register(new SchwazTetrahedriteProceduralTemplate());
        Register(new MetasomaticSideriteProceduralTemplate());
        Register(new BandedIronFormationProceduralTemplate());
        Register(new MississippiValleyProceduralTemplate());
        Register(new BimodalFelsicVmsProceduralTemplate());
        Register(new CyprusVmsProceduralTemplate());
        Register(new BesshiVmsProceduralTemplate());
        Register(new AlluvialLigniteProceduralTemplate());
        Register(new FoldedAnthraciteProceduralTemplate());
        Register(new ReopenedArsenideVeinProceduralTemplate());
        Register(new CyclothemCoalProceduralTemplate());
        Register(new StratiformCinnabarProceduralTemplate());
        Register(new PorphyryTinProceduralTemplate());
        Register(new PlayaBorateProceduralTemplate());
        Register(new TinSkarnProceduralTemplate());
        Register(new VolcanicAluniteProceduralTemplate());
        Register(new CobaltArsenideVeinProceduralTemplate());
        Register(new IocgBrecciaProceduralTemplate());
        Register(new SudburyContactNickelProceduralTemplate());
        Register(new LacustrineBorateProceduralTemplate());
        Register(new LacustrineAlumProceduralTemplate());
        Register(new PorphyryCopperMolyProceduralTemplate());
        Register(new StratiformCopperProceduralTemplate());
        Register(new VeinGraphiteProceduralTemplate());
        Register(new FlakeGraphiteSchistProceduralTemplate());
        Register(new UnconformityUraniumProceduralTemplate());
        Register(new SparryMagnesiteProceduralTemplate());
        Register(new CryptocrystallineMagnesiteProceduralTemplate());
        Register(new PeralkalineHreeProceduralTemplate());
        Register(new MessinianSulfurProceduralTemplate());
        Register(new OrogenicQuartzGoldProceduralTemplate());
        Register(new StratiformChromititeProceduralTemplate());
        Register(new MarinePhosphoriteProceduralTemplate());
        Register(new LapisLazuliMarbleProceduralTemplate());
        Register(new IvittuutCryoliteProceduralTemplate());
        Register(new AnorthositeIlmeniteProceduralTemplate());
    }

    public static void Register(IProceduralDepositTemplate template)
    {
        if (!Templates.TryAdd(template.Code, template))
        {
            throw new InvalidOperationException($"Procedural deposit template {template.Code} is already registered");
        }
    }

    public static bool TryGet(string code, out IProceduralDepositTemplate template)
    {
        return Templates.TryGetValue(code, out template!);
    }
}

internal sealed class EllipsoidProceduralTemplate : IProceduralDepositTemplate
{
    private static readonly string[] RequiredSlots = { ProceduralMaterialSlots.Primary };

    public string Code => "ellipsoid";
    public IReadOnlyList<string> RequiredMaterialSlots => RequiredSlots;
    public bool RequiresPlan => false;

    public int GetMaximumHorizontalReach(ProceduralDepositDefinition definition)
    {
        return (int)Math.Ceiling(
            Math.Max(definition.Geometry.RadiusX, definition.Geometry.RadiusZ)
            + definition.Supergene.BloomRadius);
    }

    public object? CreatePlan(in ProceduralDepositInstance instance, ProceduralDepositDefinition definition) => null;

    public bool Validate(ProceduralDepositDefinition definition, out string error)
    {
        if (definition.Geometry.RadiusX <= 0
            || definition.Geometry.RadiusY <= 0
            || definition.Geometry.RadiusZ <= 0)
        {
            error = "ellipsoid radii must be positive";
            return false;
        }
        if (definition.Geometry.YawMaxDeg < definition.Geometry.YawMinDeg)
        {
            error = "ellipsoid yaw range is inverted";
            return false;
        }
        if (definition.Geometry.EdgeNoise < 0 || definition.Geometry.EdgeNoise > 1)
        {
            error = "ellipsoid edgeNoise must be between zero and one";
            return false;
        }
        error = string.Empty;
        return true;
    }

    public void Realize(
        ProceduralDepositWorldGenSystem system,
        in DepositCandidate candidate,
        IChunkColumnGenerateRequest request,
        int baseX,
        int baseZ)
    {
        system.RealizeEllipsoidCandidate(candidate, request, baseX, baseZ);
    }
}

internal sealed class EpithermalProceduralTemplate : IProceduralDepositTemplate
{
    private static readonly string[] RequiredSlots =
    {
        ProceduralMaterialSlots.Quartz,
        ProceduralMaterialSlots.Acanthite,
        ProceduralMaterialSlots.Electrum,
        ProceduralMaterialSlots.NativeSilver,
        ProceduralMaterialSlots.Gossan,
        ProceduralMaterialSlots.Geyserite
    };

    public string Code => "epithermalVein";
    public IReadOnlyList<string> RequiredMaterialSlots => RequiredSlots;
    public bool RequiresPlan => true;

    public int GetMaximumHorizontalReach(ProceduralDepositDefinition definition)
    {
        return definition.Epithermal.HorizontalRadius
            + Math.Max(0, Math.Max(
                definition.Epithermal.SmearHeightMin,
                definition.Epithermal.SmearHeightMax));
    }

    public object CreatePlan(in ProceduralDepositInstance instance, ProceduralDepositDefinition definition)
    {
        return EpithermalVeinPlan.Create(instance, definition.Epithermal);
    }

    public bool Validate(ProceduralDepositDefinition definition, out string error)
    {
        EpithermalVeinDefinition settings = definition.Epithermal;
        bool valid = settings.HorizontalRadius >= 1
            && settings.VerticalHalfHeight >= 1
            && settings.PrincipalVeinMin >= 1
            && settings.PrincipalVeinMax >= settings.PrincipalVeinMin
            && settings.SplayMin >= 0
            && settings.SplayMax >= settings.SplayMin
            && settings.LeachedDepth >= 1
            && settings.SupergeneDepth > settings.LeachedDepth
            && settings.GeyseriteCapMin >= 3
            && settings.GeyseriteCapMax <= 6
            && settings.GeyseriteCapMax >= settings.GeyseriteCapMin
            && settings.GeyseritePipeCenterRatio > 0
            && settings.GeyseritePipeCenterRatio <= 1
            && settings.BoilingMaxY >= settings.BoilingMinY
            && settings.SmearSeedFraction >= 0
            && settings.SmearSeedFraction <= 1
            && settings.SmearHeightMin >= 0
            && settings.SmearHeightMax >= settings.SmearHeightMin;
        error = valid ? string.Empty : "invalid epithermal geometry, zoning, cap, or smear settings";
        return valid;
    }

    public void Realize(
        ProceduralDepositWorldGenSystem system,
        in DepositCandidate candidate,
        IChunkColumnGenerateRequest request,
        int baseX,
        int baseZ)
    {
        system.RealizeEpithermalCandidate(candidate, request, baseX, baseZ);
    }
}

internal sealed class SplineNetworkProceduralTemplate : IProceduralDepositTemplate
{
    private static readonly string[] RequiredSlots =
    {
        ProceduralMaterialSlots.Wall,
        ProceduralMaterialSlots.Orthoclase,
        ProceduralMaterialSlots.Intermediate,
        ProceduralMaterialSlots.Core
    };

    public string Code => "splineNetwork";
    public IReadOnlyList<string> RequiredMaterialSlots => RequiredSlots;
    public bool RequiresPlan => true;

    public int GetMaximumHorizontalReach(ProceduralDepositDefinition definition)
    {
        return Math.Max(8, definition.Spline.HorizontalRadius);
    }

    public object? CreatePlan(in ProceduralDepositInstance instance, ProceduralDepositDefinition definition)
    {
        return SplineNetworkPlan.Create(instance, definition.Spline);
    }

    public bool Validate(ProceduralDepositDefinition definition, out string error)
    {
        SplineNetworkDefinition settings = definition.Spline;
        bool valid = settings.HorizontalRadius >= 8
            && settings.VerticalHalfHeight >= 8
            && settings.BranchMin >= 0
            && settings.BranchMax >= settings.BranchMin
            && settings.TrunkLength >= 16
            && settings.BranchLengthFraction > 0
            && settings.BranchLengthFraction <= 1
            && settings.SegmentLength >= 2
            && settings.SegmentLength <= 16
            && settings.UpwardSlopeMin >= 0
            && settings.UpwardSlopeMax >= settings.UpwardSlopeMin
            && settings.UpwardSlopeMax <= 2
            && settings.MaxStepAngleDeg > 0
            && settings.MaxStepAngleDeg <= 15
            && settings.SheetRollMinDeg >= 0
            && settings.SheetRollMaxDeg >= settings.SheetRollMinDeg
            && settings.SheetRollMaxDeg <= 90
            && settings.BranchDivergenceDeg >= 0
            && settings.BranchDivergenceDeg <= 90
            && settings.Thickness >= 1
            && settings.Width >= settings.Thickness
            && settings.BranchScale > 0
            && settings.BranchScale <= 1
            && settings.PinchScaleMin > 0
            && settings.PinchScaleMax >= settings.PinchScaleMin
            && settings.LepidoliteLensMin >= 0
            && settings.LepidoliteLensMax >= settings.LepidoliteLensMin
            && settings.CoreRatio > 0
            && settings.IntermediateRatio > settings.CoreRatio
            && settings.IntermediateRatio <= 1
            && IsFraction(settings.AlbiteFraction)
            && IsFraction(settings.SchorlFraction)
            && IsFraction(settings.LepidoliteFraction)
            && IsFraction(settings.PolluciteFraction)
            && IsFraction(settings.SpodumeneFraction)
            && IsFraction(settings.BerylFraction)
            && IsFraction(settings.CassiteriteFraction)
            && IsFraction(settings.TantaliteFraction)
            && IsFraction(settings.ColumbiteFraction)
            && settings.PolluciteFraction + settings.LepidoliteFraction <= 1
            && settings.CassiteriteFraction
                + settings.TantaliteFraction
                + settings.ColumbiteFraction <= 1;
        error = valid ? string.Empty : "invalid spline network geometry or zoning settings";
        return valid;
    }

    private static bool IsFraction(double value) => value is >= 0 and <= 1;

    public void Realize(
        ProceduralDepositWorldGenSystem system,
        in DepositCandidate candidate,
        IChunkColumnGenerateRequest request,
        int baseX,
        int baseZ)
    {
        system.RealizeSplineCandidate(candidate, request, baseX, baseZ);
    }
}
internal sealed class SheetedPlateProceduralTemplate : IProceduralDepositTemplate
{
    private static readonly string[] RequiredSlots = { ProceduralMaterialSlots.Primary };

    public string Code => "sheetedPlate";
    public IReadOnlyList<string> RequiredMaterialSlots => RequiredSlots;
    public bool RequiresPlan => true;

    public int GetMaximumHorizontalReach(ProceduralDepositDefinition definition)
    {
        SheetedPlateDefinition settings = definition.SheetedPlate;
        double terrainTilt = definition.TerrainOrientation.Enabled
            ? definition.TerrainOrientation.MaxAppliedDipDeg * Math.PI / 180
            : 0;
        return settings.HorizontalRadius
            + (int)Math.Ceiling(settings.VerticalHalfHeight * Math.Sin(terrainTilt));
    }

    public object CreatePlan(in ProceduralDepositInstance instance, ProceduralDepositDefinition definition)
    {
        return SheetedPlatePlan.Create(instance, definition.SheetedPlate);
    }

    public bool Validate(ProceduralDepositDefinition definition, out string error)
    {
        SheetedPlateDefinition settings = definition.SheetedPlate;
        bool valid = settings.HorizontalRadius >= 16
            && settings.VerticalHalfHeight >= 16
            && settings.MainDipMinDeg >= 0
            && settings.MainDipMaxDeg >= settings.MainDipMinDeg
            && settings.MainDipMaxDeg <= 90
            && settings.MainLengthHalf > 0
            && settings.MainHeightHalf > 0
            && settings.MainThickness > 0
            && settings.SubsheetMin >= 0
            && settings.SubsheetMax >= settings.SubsheetMin
            && settings.SatelliteMin >= 0
            && settings.SatelliteMax >= settings.SatelliteMin
            && settings.HaloThickness >= 0
            && IsFraction(settings.GangueFraction)
            && IsFraction(settings.RareGangueFraction)
            && IsFraction(settings.SecondaryFraction)
            && IsFraction(settings.AlbiteFraction)
            && IsFraction(settings.BrecciaFraction);
        error = valid ? string.Empty : "invalid sheeted-plate geometry or zoning settings";
        return valid;
    }

    public void Realize(
        ProceduralDepositWorldGenSystem system,
        in DepositCandidate candidate,
        IChunkColumnGenerateRequest request,
        int baseX,
        int baseZ)
    {
        system.RealizeSheetedPlateCandidate(candidate, request, baseX, baseZ);
    }

    private static bool IsFraction(double value) => value is >= 0 and <= 1;
}

internal sealed class OoliticIronstoneProceduralTemplate : IProceduralDepositTemplate
{
    private static readonly string[] RequiredSlots =
    {
        ProceduralMaterialSlots.Hematite,
        ProceduralMaterialSlots.Limonite,
        ProceduralMaterialSlots.Siderite,
        ProceduralMaterialSlots.Magnetite
    };

    public string Code => "ooliticIronstone";
    public IReadOnlyList<string> RequiredMaterialSlots => RequiredSlots;
    public bool RequiresPlan => true;

    public int GetMaximumHorizontalReach(ProceduralDepositDefinition definition)
    {
        return definition.Oolitic.HorizontalRadius;
    }

    public object CreatePlan(in ProceduralDepositInstance instance, ProceduralDepositDefinition definition)
    {
        return OoliticIronstonePlan.Create(instance, definition.Oolitic);
    }

    public bool Validate(ProceduralDepositDefinition definition, out string error)
    {
        OoliticIronstoneDefinition settings = definition.Oolitic;
        bool valid = settings.HorizontalRadius >= 16
            && settings.VerticalHalfHeight >= 8
            && settings.BedMin >= 1
            && settings.BedMax >= settings.BedMin
            && settings.DipMinDeg >= 0
            && settings.DipMaxDeg >= settings.DipMinDeg
            && settings.DipMaxDeg <= 20
            && settings.BedSpacingMin > 0
            && settings.BedSpacingMax >= settings.BedSpacingMin
            && settings.RadiusAlongMin > 0
            && settings.RadiusAlongMax >= settings.RadiusAlongMin
            && settings.RadiusAcrossMin > 0
            && settings.RadiusAcrossMax >= settings.RadiusAcrossMin
            && settings.ThicknessMin > 0
            && settings.ThicknessMax >= settings.ThicknessMin;
        error = valid ? string.Empty : "invalid oolitic ironstone bed settings";
        return valid;
    }

    public void Realize(
        ProceduralDepositWorldGenSystem system,
        in DepositCandidate candidate,
        IChunkColumnGenerateRequest request,
        int baseX,
        int baseZ)
    {
        system.RealizeOoliticIronstoneCandidate(candidate, request, baseX, baseZ);
    }
}

internal sealed class GreisenStockworkProceduralTemplate : IProceduralDepositTemplate
{
    private static readonly string[] RequiredSlots =
    {
        ProceduralMaterialSlots.Quartz
    };

    public string Code => "greisenStockwork";
    public IReadOnlyList<string> RequiredMaterialSlots => RequiredSlots;
    public bool RequiresPlan => true;

    public int GetMaximumHorizontalReach(ProceduralDepositDefinition definition)
    {
        return definition.Greisen.HorizontalRadius;
    }

    public object CreatePlan(in ProceduralDepositInstance instance, ProceduralDepositDefinition definition)
    {
        return GreisenStockworkPlan.Create(instance, definition.Greisen);
    }

    public bool Validate(ProceduralDepositDefinition definition, out string error)
    {
        GreisenStockworkDefinition settings = definition.Greisen;
        bool valid = settings.HorizontalRadius >= 16
            && settings.VerticalHalfHeight >= 8
            && settings.CupolaRadiusX > 0
            && settings.CupolaRadiusZ > 0
            && settings.CupolaHeight > 0
            && settings.LodeThickness > 0
            && settings.VeinMin >= 1
            && settings.VeinMax >= settings.VeinMin;
        error = valid ? string.Empty : "invalid greisen stockwork settings";
        return valid;
    }

    public void Realize(
        ProceduralDepositWorldGenSystem system,
        in DepositCandidate candidate,
        IChunkColumnGenerateRequest request,
        int baseX,
        int baseZ)
    {
        system.RealizeGreisenStockworkCandidate(candidate, request, baseX, baseZ);
    }
}

internal sealed class PrabornaPyrolusiteProceduralTemplate : IProceduralDepositTemplate
{
    private static readonly string[] RequiredSlots =
    {
        ProceduralMaterialSlots.Pyrolusite,
        ProceduralMaterialSlots.Braunite,
        ProceduralMaterialSlots.Rhodochrosite,
        ProceduralMaterialSlots.Quartz
    };

    public string Code => "prabornaPyrolusite";
    public IReadOnlyList<string> RequiredMaterialSlots => RequiredSlots;
    public bool RequiresPlan => true;

    public int GetMaximumHorizontalReach(ProceduralDepositDefinition definition)
    {
        return definition.Praborna.HorizontalRadius;
    }

    public object CreatePlan(in ProceduralDepositInstance instance, ProceduralDepositDefinition definition)
    {
        return PrabornaPyrolusitePlan.Create(instance, definition.Praborna);
    }

    public bool Validate(ProceduralDepositDefinition definition, out string error)
    {
        PrabornaPyrolusiteDefinition settings = definition.Praborna;
        bool valid = settings.HorizontalRadius >= 16
            && settings.VerticalHalfHeight >= 8
            && settings.LensCount >= 1
            && settings.QuartzVeinCount >= 1;
        error = valid ? string.Empty : "invalid praborna pyrolusite settings";
        return valid;
    }

    public void Realize(
        ProceduralDepositWorldGenSystem system,
        in DepositCandidate candidate,
        IChunkColumnGenerateRequest request,
        int baseX,
        int baseZ)
    {
        system.RealizePrabornaPyrolusiteCandidate(candidate, request, baseX, baseZ);
    }
}

internal sealed class CarbonatiteComplexProceduralTemplate : IProceduralDepositTemplate
{
    private static readonly string[] RequiredSlots = { ProceduralMaterialSlots.Carbonatite };
    public string Code => "carbonatiteComplex";
    public IReadOnlyList<string> RequiredMaterialSlots => RequiredSlots;
    public bool RequiresPlan => true;
    public int GetMaximumHorizontalReach(ProceduralDepositDefinition definition) => definition.Carbonatite.HorizontalRadius;
    public object CreatePlan(in ProceduralDepositInstance instance, ProceduralDepositDefinition definition) =>
        CarbonatiteComplexPlan.Create(instance, definition.Carbonatite);
    public bool Validate(ProceduralDepositDefinition definition, out string error)
    {
        CarbonatiteComplexDefinition s = definition.Carbonatite;
        bool valid = s.HorizontalRadius >= 16 && s.VerticalHalfHeight >= 16
            && s.DykeCountMin >= 1 && s.DykeCountMax >= s.DykeCountMin
            && s.FeederCountMin >= 1 && s.FeederCountMax >= s.FeederCountMin
            && s.RingCountMin >= 1 && s.RingCountMax >= s.RingCountMin
            && s.CapVeinCountMin >= 1 && s.CapVeinCountMax >= s.CapVeinCountMin;
        error = valid ? string.Empty : "invalid carbonatite complex settings";
        return valid;
    }
    public void Realize(ProceduralDepositWorldGenSystem system, in DepositCandidate candidate, IChunkColumnGenerateRequest request, int baseX, int baseZ) =>
        system.RealizeCarbonatiteComplexCandidate(candidate, request, baseX, baseZ);
}


internal readonly record struct DepositCandidate(
    CompiledProceduralDeposit Compiled,
    ProceduralDepositInstance Instance,
    IProceduralDepositTemplate Template,
    object? Plan)
{
    public EpithermalVeinPlan EpithermalPlan => (EpithermalVeinPlan)Plan!;
    public SplineNetworkPlan SplinePlan => (SplineNetworkPlan)Plan!;
    public SheetedPlatePlan SheetedPlatePlan => (SheetedPlatePlan)Plan!;
    public OoliticIronstonePlan OoliticIronstonePlan => (OoliticIronstonePlan)Plan!;
    public GreisenStockworkPlan GreisenPlan => (GreisenStockworkPlan)Plan!;
    public CarbonatiteComplexPlan CarbonatitePlan => (CarbonatiteComplexPlan)Plan!;
    public PrabornaPyrolusitePlan PrabornaPlan => (PrabornaPyrolusitePlan)Plan!;
    public SchwazTetrahedritePlan TetrahedritePlan => (SchwazTetrahedritePlan)Plan!;
    public MetasomaticSideritePlan MetasomaticSideritePlan => (MetasomaticSideritePlan)Plan!;
    public BandedIronFormationPlan BandedIronPlan => (BandedIronFormationPlan)Plan!;
    public MississippiValleyPlan MvtPlan => (MississippiValleyPlan)Plan!;
    public BimodalFelsicVmsPlan BimodalFelsicVmsPlan => (BimodalFelsicVmsPlan)Plan!;
    public CyprusVmsPlan CyprusVmsPlan => (CyprusVmsPlan)Plan!;
    public BesshiVmsPlan BesshiVmsPlan => (BesshiVmsPlan)Plan!;
    public AlluvialLignitePlan LignitePlan => (AlluvialLignitePlan)Plan!;
    public FoldedAnthracitePlan AnthracitePlan => (FoldedAnthracitePlan)Plan!;
    public ReopenedArsenideVeinPlan ArsenideVeinPlan => (ReopenedArsenideVeinPlan)Plan!;
    public CyclothemCoalPlan CyclothemCoalPlan => (CyclothemCoalPlan)Plan!;
    public StratiformCinnabarPlan StratiformCinnabarPlan => (StratiformCinnabarPlan)Plan!;
    public PorphyryTinPlan PorphyryTinPlan => (PorphyryTinPlan)Plan!;
    public PlayaBoratePlan PlayaBoratePlan => (PlayaBoratePlan)Plan!;
    public TinSkarnPlan TinSkarnPlan => (TinSkarnPlan)Plan!;
    public VolcanicAlunitePlan VolcanicAlunitePlan => (VolcanicAlunitePlan)Plan!;
    public CobaltArsenideVeinPlan CobaltArsenideVeinPlan => (CobaltArsenideVeinPlan)Plan!;
    public IocgBrecciaPlan IocgBrecciaPlan => (IocgBrecciaPlan)Plan!;
    public SudburyContactNickelPlan SudburyNickelPlan => (SudburyContactNickelPlan)Plan!;
    public LacustrineBoratePlan LacustrineBoratePlan => (LacustrineBoratePlan)Plan!;
    public PorphyryCopperMolyPlan PorphyryCopperMolyPlan => (PorphyryCopperMolyPlan)Plan!;
    public StratiformCopperPlan StratiformCopperPlan => (StratiformCopperPlan)Plan!;
    public IAdditionalDepositPlan AdditionalPlan => (IAdditionalDepositPlan)Plan!;
    public MessinianSulfurPlan MessinianSulfurPlan => (MessinianSulfurPlan)Plan!;
}
