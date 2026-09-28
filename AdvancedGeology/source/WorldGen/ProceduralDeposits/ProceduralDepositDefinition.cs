using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using Vintagestory.API.Common;
using Vintagestory.API.Server;
using Vintagestory.API.Util;

namespace AdvancedGeology.WorldGen.ProceduralDeposits;

[JsonObject(MemberSerialization.OptIn)]
public sealed class ProceduralDepositDefinition
{
    [JsonProperty]
    public string Code { get; set; } = string.Empty;

    [JsonProperty]
    public bool Enabled { get; set; } = true;

    [JsonProperty]
    public ProceduralProspectingDefinition Prospecting { get; set; } = new();

    [JsonProperty]
    public int Priority { get; set; } = 100;

    [JsonProperty]
    public string OverlapPolicy { get; set; } = "higherPriorityWins";

    [JsonProperty]
    public string Template { get; set; } = "ellipsoid";

    [JsonProperty]
    public bool Intrude { get; set; }

    [JsonProperty]
    public ProceduralPlacementDefinition Placement { get; set; } = new();

    [JsonProperty]
    public EllipsoidGeometryDefinition Geometry { get; set; } = new();

    [JsonProperty]
    public EpithermalVeinDefinition Epithermal { get; set; } = new();

    [JsonProperty]
    public SplineNetworkDefinition Spline { get; set; } = new();

    [JsonProperty]
    public SheetedPlateDefinition SheetedPlate { get; set; } = new();

    [JsonProperty]
    public OoliticIronstoneDefinition Oolitic { get; set; } = new();

    [JsonProperty]
    public CarbonatiteComplexDefinition Carbonatite { get; set; } = new();
    [JsonProperty]
    public GreisenStockworkDefinition Greisen { get; set; } = new();

    [JsonProperty]
    public PrabornaPyrolusiteDefinition Praborna { get; set; } = new();

    [JsonProperty]
    public SchwazTetrahedriteDefinition Tetrahedrite { get; set; } = new();

    [JsonProperty]
    public MetasomaticSideriteDefinition MetasomaticSiderite { get; set; } = new();

    [JsonProperty]
    public BandedIronFormationDefinition BandedIron { get; set; } = new();

    [JsonProperty]
    public MississippiValleyDefinition Mvt { get; set; } = new();

    [JsonProperty]
    public BimodalFelsicVmsDefinition BimodalFelsicVms { get; set; } = new();

    [JsonProperty]
    public CyprusVmsDefinition CyprusVms { get; set; } = new();

    [JsonProperty]
    public BesshiVmsDefinition BesshiVms { get; set; } = new();

    [JsonProperty]
    public AlluvialLigniteDefinition Lignite { get; set; } = new();

    [JsonProperty]
    public FoldedAnthraciteDefinition Anthracite { get; set; } = new();

    [JsonProperty]
    public ReopenedArsenideVeinDefinition ArsenideVein { get; set; } = new();

    [JsonProperty]
    public TerrainOrientationDefinition TerrainOrientation { get; set; } = new();

    [JsonProperty]
    public CyclothemCoalDefinition CyclothemCoal { get; set; } = new();

    [JsonProperty]
    public StratiformCinnabarDefinition StratiformCinnabar { get; set; } = new();

    [JsonProperty]
    public PorphyryTinDefinition PorphyryTin { get; set; } = new();

    [JsonProperty]
    public PlayaBorateDefinition PlayaBorate { get; set; } = new();

    [JsonProperty]
    public TinSkarnDefinition TinSkarn { get; set; } = new();

    [JsonProperty]
    public VolcanicAluniteDefinition VolcanicAlunite { get; set; } = new();

    [JsonProperty]
    public CobaltArsenideVeinDefinition CobaltArsenideVein { get; set; } = new();

    [JsonProperty]
    public IocgBrecciaDefinition IocgBreccia { get; set; } = new();

    [JsonProperty]
    public SudburyContactNickelDefinition SudburyNickel { get; set; } = new();

    [JsonProperty]
    public LacustrineBorateDefinition LacustrineBorate { get; set; } = new();

    [JsonProperty]
    public PorphyryCopperMolyDefinition PorphyryCopperMoly { get; set; } = new();

    [JsonProperty]
    public StratiformCopperDefinition StratiformCopper { get; set; } = new();

    [JsonProperty]
    public VeinGraphiteDefinition VeinGraphite { get; set; } = new();

    [JsonProperty]
    public FlakeGraphiteSchistDefinition FlakeGraphiteSchist { get; set; } = new();

    [JsonProperty]
    public UnconformityUraniumDefinition UnconformityUranium { get; set; } = new();

    [JsonProperty]
    public SparryMagnesiteDefinition SparryMagnesite { get; set; } = new();

    [JsonProperty]
    public CryptocrystallineMagnesiteDefinition CryptocrystallineMagnesite { get; set; } = new();

    [JsonProperty]
    public PeralkalineHreeDefinition PeralkalineHree { get; set; } = new();

    [JsonProperty]
    public MessinianSulfurDefinition MessinianSulfur { get; set; } = new();

    [JsonProperty]
    public OrogenicQuartzGoldDefinition OrogenicQuartzGold { get; set; } = new();

    [JsonProperty]
    public StratiformChromititeDefinition StratiformChromitite { get; set; } = new();

    [JsonProperty]
    public MarinePhosphoriteDefinition MarinePhosphorite { get; set; } = new();

    [JsonProperty]
    public LapisLazuliMarbleDefinition LapisLazuliMarble { get; set; } = new();

    [JsonProperty]
    public IvittuutCryoliteDefinition IvittuutCryolite { get; set; } = new();

    [JsonProperty]
    public AnorthositeIlmeniteDefinition AnorthositeIlmenite { get; set; } = new();

    [JsonProperty]
    public ProceduralClimateDefinition Climate { get; set; } = new();

    [JsonProperty]
    public SourceRockDefinition Source { get; set; } = new();

    [JsonProperty]
    public SupergeneDefinition Supergene { get; set; } = new();

    [JsonProperty]
    public SurfaceWeatheringDefinition Weathering { get; set; } = new();

    [JsonProperty]
    public SurfaceNuggetDefinition SurfaceNuggets { get; set; } = new();

    [JsonProperty]
    public ProceduralPaletteDefinition Palette { get; set; } = new();
    /// <summary>
    /// Per-deposit, per-ore-slot trace content: each element maps to [minimum, maximum]
    /// fractions of the dropped ore item's metal units. Unlisted elements are absent.
    /// </summary>
    [JsonProperty]
    public Dictionary<string, Dictionary<string, double[]>> Byproducts { get; set; } = new(StringComparer.Ordinal);

    internal bool ValidateByproducts(out string error)
    {
        error = string.Empty;
        if (Byproducts == null)
        {
            error = "byproducts cannot be null";
            return false;
        }

        foreach ((string mineral, Dictionary<string, double[]>? elements) in Byproducts)
        {
            if (string.IsNullOrWhiteSpace(mineral)
                || !Palette.Materials.TryGetValue(mineral, out string? materialCode))
            {
                error = $"byproduct mineral slot '{mineral}' is not declared in palette.materials";
                return false;
            }
            int pathOffset = materialCode.IndexOf(':') + 1;
            if (!materialCode.AsSpan(pathOffset).StartsWith("ore-", StringComparison.Ordinal))
            {
                error = $"byproduct mineral slot '{mineral}' does not resolve from an ore block pattern";
                return false;
            }
            if (elements == null || elements.Count == 0)
            {
                error = $"byproduct mineral slot '{mineral}' has no elements";
                return false;
            }

            foreach ((string element, double[]? grades) in elements)
            {
                if (string.IsNullOrWhiteSpace(element) || grades is not { Length: 2 }
                    || !double.IsFinite(grades[0]) || !double.IsFinite(grades[1])
                    || grades[0] <= 0 || grades[1] < grades[0])
                {
                    error = $"byproduct range for '{mineral}/{element}' must be [minimum, maximum], with 0 < minimum <= maximum";
                    return false;
                }
            }
        }
        return true;
    }
}

internal sealed class CompiledByproductRule
{
    public CompiledByproductRule(string mineral, Dictionary<string, double[]> elements)
    {
        Mineral = mineral;
        Elements = elements
            .OrderBy(entry => entry.Key, StringComparer.Ordinal)
            .ToDictionary(
                entry => entry.Key,
                entry => (double[])entry.Value.Clone(),
                StringComparer.Ordinal);
    }

    public string Mineral { get; }
    public IReadOnlyDictionary<string, double[]> Elements { get; }
}


[JsonObject(MemberSerialization.OptIn)]
public sealed class ProceduralProspectingDefinition
{
    [JsonProperty]
    public string[] MajorMinerals { get; set; } = Array.Empty<string>();

    [JsonProperty]
    public int SignalRadius { get; set; } = 64;

    [JsonProperty]
    public int CenterShift { get; set; } = 72;
}

[JsonObject(MemberSerialization.OptIn)]
public sealed class ProceduralPlacementDefinition
{
    [JsonProperty]
    public int CellSize { get; set; } = 192;

    [JsonProperty]
    public double Chance { get; set; } = 0.02;

    [JsonProperty]
    public string YMode { get; set; } = "surfaceDepth";

    [JsonProperty]
    public int MinDepth { get; set; } = 10;

    [JsonProperty]
    public int MaxDepth { get; set; } = 50;


    [JsonProperty]
    public int SourceOffset { get; set; } = 12;
}

[JsonObject(MemberSerialization.OptIn)]
public sealed class ProceduralClimateDefinition
{
    [JsonProperty]
    public bool Enabled { get; set; }

    [JsonProperty]
    public double MinRain { get; set; } = 0.0;

    [JsonProperty]
    public double MaxRain { get; set; } = 1.0;

    [JsonProperty]
    public double MinTemp { get; set; } = -40.0;

    [JsonProperty]
    public double MaxTemp { get; set; } = 50.0;
}

[JsonObject(MemberSerialization.OptIn)]
public sealed class EllipsoidGeometryDefinition
{
    [JsonProperty]
    public double RadiusX { get; set; } = 18;

    [JsonProperty]
    public double RadiusY { get; set; } = 6;

    [JsonProperty]
    public double RadiusZ { get; set; } = 10;

    [JsonProperty]
    public double YawMinDeg { get; set; }

    [JsonProperty]
    public double YawMaxDeg { get; set; } = 180;

    [JsonProperty]
    public double EdgeNoise { get; set; } = 0.08;
}

[JsonObject(MemberSerialization.OptIn)]
public sealed class EpithermalVeinDefinition
{
    [JsonProperty]
    public int HorizontalRadius { get; set; } = 76;

    [JsonProperty]
    public int VerticalHalfHeight { get; set; } = 44;

    [JsonProperty]
    public int PrincipalVeinMin { get; set; } = 3;

    [JsonProperty]
    public int PrincipalVeinMax { get; set; } = 7;

    [JsonProperty]
    public int SplayMin { get; set; } = 1;

    [JsonProperty]
    public int SplayMax { get; set; } = 3;

    [JsonProperty]
    public int LeachedDepth { get; set; } = 12;

    [JsonProperty]
    public int SupergeneDepth { get; set; } = 22;

    [JsonProperty]
    public int GeyseriteCapMin { get; set; } = 3;

    [JsonProperty]
    public int GeyseriteCapMax { get; set; } = 6;

    [JsonProperty]
    public double GeyseritePipeCenterRatio { get; set; } = 0.42;

    [JsonProperty]
    public double BoilingMinY { get; set; } = -9;

    [JsonProperty]
    public double BoilingMaxY { get; set; } = 14;

    [JsonProperty]
    public double SmearSeedFraction { get; set; } = 0.25;

    [JsonProperty]
    public int SmearHeightMin { get; set; } = 5;

    [JsonProperty]
    public int SmearHeightMax { get; set; } = 10;
}

[JsonObject(MemberSerialization.OptIn)]
public sealed class SplineNetworkDefinition
{
    [JsonProperty]
    public int HorizontalRadius { get; set; } = 72;

    [JsonProperty]
    public int VerticalHalfHeight { get; set; } = 48;

    [JsonProperty]
    public int BranchMin { get; set; } = 2;

    [JsonProperty]
    public int BranchMax { get; set; } = 3;

    [JsonProperty]
    public double TrunkLength { get; set; } = 100;

    [JsonProperty]
    public double BranchLengthFraction { get; set; } = 0.55;

    [JsonProperty]
    public double SegmentLength { get; set; } = 6;

    [JsonProperty]
    public double UpwardSlopeMin { get; set; } = 0.30;

    [JsonProperty]
    public double UpwardSlopeMax { get; set; } = 0.46;

    [JsonProperty]
    public double MaxStepAngleDeg { get; set; } = 3.5;

    [JsonProperty]
    public double SheetRollMinDeg { get; set; }

    [JsonProperty]
    public double SheetRollMaxDeg { get; set; } = 8;

    [JsonProperty]
    public double BranchDivergenceDeg { get; set; } = 40;

    [JsonProperty]
    public double Thickness { get; set; } = 9;

    [JsonProperty]
    public double Width { get; set; } = 30;

    [JsonProperty]
    public double BranchScale { get; set; } = 0.62;

    [JsonProperty]
    public double PinchScaleMin { get; set; } = 0.80;

    [JsonProperty]
    public double PinchScaleMax { get; set; } = 1.20;

    [JsonProperty]
    public int LepidoliteLensMin { get; set; } = 2;

    [JsonProperty]
    public int LepidoliteLensMax { get; set; } = 3;

    [JsonProperty]
    public double CoreRatio { get; set; } = 0.3;

    [JsonProperty]
    public double IntermediateRatio { get; set; } = 0.65;

    [JsonProperty]
    public double LensAlongMin { get; set; } = 0.15;

    [JsonProperty]
    public double LensAlongMax { get; set; } = 0.8;

    [JsonProperty]
    public double AlbiteFraction { get; set; } = 0.14;

    [JsonProperty]
    public double SchorlFraction { get; set; } = 0.18;

    [JsonProperty]
    public double LepidoliteFraction { get; set; } = 0.16;

    [JsonProperty]
    public double PolluciteFraction { get; set; } = 0.05;

    [JsonProperty]
    public double SpodumeneFraction { get; set; } = 0.12;

    [JsonProperty]
    public double BerylFraction { get; set; } = 0.07;

    [JsonProperty]
    public double CassiteriteFraction { get; set; } = 0.09;

    [JsonProperty]
    public double TantaliteFraction { get; set; } = 0.07;

    [JsonProperty]
    public double ColumbiteFraction { get; set; } = 0.07;
}
[JsonObject(MemberSerialization.OptIn)]
public sealed class SheetedPlateDefinition
{
    [JsonProperty]
    public int HorizontalRadius { get; set; } = 22;

    [JsonProperty]
    public int VerticalHalfHeight { get; set; } = 24;

    [JsonProperty]
    public double MainDipMinDeg { get; set; } = 8;

    [JsonProperty]
    public double MainDipMaxDeg { get; set; } = 16;

    [JsonProperty]
    public double MainLengthHalf { get; set; } = 15;

    [JsonProperty]
    public double MainHeightHalf { get; set; } = 15;

    [JsonProperty]
    public double MainThickness { get; set; } = 7;

    [JsonProperty]
    public int SubsheetMin { get; set; } = 1;

    [JsonProperty]
    public int SubsheetMax { get; set; } = 3;

    [JsonProperty]
    public int SatelliteMin { get; set; }

    [JsonProperty]
    public int SatelliteMax { get; set; } = 2;

    [JsonProperty]
    public double HaloThickness { get; set; } = 3;

    [JsonProperty]
    public double GangueFraction { get; set; } = 0.18;

    [JsonProperty]
    public double RareGangueFraction { get; set; } = 0.05;

    [JsonProperty]
    public double SecondaryFraction { get; set; } = 0.12;

    [JsonProperty]
    public double AlbiteFraction { get; set; } = 0.45;

    [JsonProperty]
    public double BrecciaFraction { get; set; } = 0.05;
}

[JsonObject(MemberSerialization.OptIn)]
public sealed class OoliticIronstoneDefinition
{
    [JsonProperty]
    public int HorizontalRadius { get; set; } = 48;

    [JsonProperty]
    public int VerticalHalfHeight { get; set; } = 22;

    [JsonProperty]
    public int BedMin { get; set; } = 3;

    [JsonProperty]
    public int BedMax { get; set; } = 5;

    [JsonProperty]
    public double DipMinDeg { get; set; } = 3;

    [JsonProperty]
    public double DipMaxDeg { get; set; } = 10;

    [JsonProperty]
    public double BedSpacingMin { get; set; } = 3;

    [JsonProperty]
    public double BedSpacingMax { get; set; } = 5;

    [JsonProperty]
    public double RadiusAlongMin { get; set; } = 27;

    [JsonProperty]
    public double RadiusAlongMax { get; set; } = 42;

    [JsonProperty]
    public double RadiusAcrossMin { get; set; } = 25;

    [JsonProperty]
    public double RadiusAcrossMax { get; set; } = 38;

    [JsonProperty]
    public double ThicknessMin { get; set; } = 1.4;

    [JsonProperty]
    public double ThicknessMax { get; set; } = 3;
}


[JsonObject(MemberSerialization.OptIn)]
public sealed class CarbonatiteComplexDefinition
{
    [JsonProperty] public int HorizontalRadius { get; set; } = 50;
    [JsonProperty] public int VerticalHalfHeight { get; set; } = 45;
    [JsonProperty] public int DykeCountMin { get; set; } = 2;
    [JsonProperty] public int DykeCountMax { get; set; } = 5;
    [JsonProperty] public int FeederCountMin { get; set; } = 1;
    [JsonProperty] public int FeederCountMax { get; set; } = 2;
    [JsonProperty] public int RingCountMin { get; set; } = 1;
    [JsonProperty] public int RingCountMax { get; set; } = 3;
    [JsonProperty] public int CapVeinCountMin { get; set; } = 2;
    [JsonProperty] public int CapVeinCountMax { get; set; } = 5;
    [JsonProperty] public double RegionalStrikeMinDeg { get; set; } = 8;
    [JsonProperty] public double RegionalStrikeMaxDeg { get; set; } = 48;
    [JsonProperty] public double TopTiltMinDeg { get; set; } = 2;
    [JsonProperty] public double TopTiltMaxDeg { get; set; } = 7;
    [JsonProperty] public double SillRadiusAlongMin { get; set; } = 23;
    [JsonProperty] public double SillRadiusAlongMax { get; set; } = 29;
    [JsonProperty] public double SillRadiusAcrossMin { get; set; } = 13;
    [JsonProperty] public double SillRadiusAcrossMax { get; set; } = 18;
    [JsonProperty] public double SillRadiusVerticalMin { get; set; } = 2.6;
    [JsonProperty] public double SillRadiusVerticalMax { get; set; } = 3.8;
}


[JsonObject(MemberSerialization.OptIn)]
public sealed class TerrainOrientationDefinition
{
    [JsonProperty]
    public bool Enabled { get; set; }

    [JsonProperty]
    public int SampleRadius { get; set; } = 4;

    [JsonProperty]
    public int SampleSpacing { get; set; } = 2;

    [JsonProperty]
    public double DipFactor { get; set; } = 0.65;

    [JsonProperty]
    public double IgnoreBelowTerrainSlopeDeg { get; set; } = 1.5;

    [JsonProperty]
    public double MaxTerrainSlopeDeg { get; set; } = 20;

    [JsonProperty]
    public double MinAppliedDipDeg { get; set; }

    [JsonProperty]
    public double MaxAppliedDipDeg { get; set; } = 12;
}

[JsonObject(MemberSerialization.OptIn)]
public sealed class SourceRockDefinition
{
    [JsonProperty]
    public bool Enabled { get; set; }

    [JsonProperty]
    public string[] EligibleRockVariants { get; set; } = Array.Empty<string>();

    [JsonProperty]
    public int SearchMinDepth { get; set; }

    [JsonProperty]
    public int SearchMaxDepth { get; set; } = 115;

    [JsonProperty]
    public int MinimumThickness { get; set; } = 6;
}

[JsonObject(MemberSerialization.OptIn)]
public sealed class SupergeneDefinition
{
    [JsonProperty]
    public bool Enabled { get; set; }

    [JsonProperty]
    public int OxidationDepth { get; set; } = 10;

    [JsonProperty]
    public int OxidationDepthNoise { get; set; } = 2;

    [JsonProperty]
    public int EnrichmentThickness { get; set; } = 3;

    [JsonProperty]
    public int GossanSourceDepth { get; set; } = 5;

    [JsonProperty]
    public int GossanSoilDepth { get; set; } = 2;

    [JsonProperty]
    public double BloomRadius { get; set; }
}

[JsonObject(MemberSerialization.OptIn)]
public sealed class SurfaceWeatheringDefinition
{
    [JsonProperty]
    public bool Enabled { get; set; }

    [JsonProperty]
    public double GossanSmearSeedFraction { get; set; } = 0.25;

    [JsonProperty]
    public int GossanSmearHeightMin { get; set; } = 5;

    [JsonProperty]
    public int GossanSmearHeightMax { get; set; } = 10;
}

[JsonObject(MemberSerialization.OptIn)]
public sealed class SurfaceNuggetDefinition
{
    [JsonProperty]
    public bool Enabled { get; set; } = true;

    /// <summary>Per-column surface nugget probability.</summary>
    [JsonProperty]
    public double Chance { get; set; } = 0.12;

    /// <summary>Maximum ore depth that can seed surface nuggets.</summary>
    [JsonProperty]
    public int MaxDepth { get; set; } = 30;
}

[JsonObject(MemberSerialization.OptIn)]
public sealed class ProceduralPaletteDefinition
{
    [JsonProperty]
    public string[] ReplaceableRockVariants { get; set; } = Array.Empty<string>();

    [JsonProperty]
    public Dictionary<string, string> Materials { get; set; } = new(StringComparer.Ordinal);

    [JsonProperty]
    public string Primary { get; set; } = string.Empty;

    [JsonProperty]
    public string Oxide { get; set; } = string.Empty;

    [JsonProperty]
    public string Enriched { get; set; } = string.Empty;

    [JsonProperty]
    public string Quartz { get; set; } = string.Empty;

    [JsonProperty]
    public string Acanthite { get; set; } = string.Empty;

    [JsonProperty]
    public string Electrum { get; set; } = string.Empty;

    [JsonProperty]
    public string NativeSilver { get; set; } = string.Empty;

    [JsonProperty]
    public string Geyserite { get; set; } = string.Empty;

    [JsonProperty]
    public string Gossan { get; set; } = string.Empty;

    [JsonProperty]
    public string[] GossanSurfaceExclusions { get; set; } = Array.Empty<string>();

    internal void MigrateLegacyMaterials()
    {
        Materials ??= new Dictionary<string, string>(StringComparer.Ordinal);
        AddLegacy(ProceduralMaterialSlots.Primary, Primary);
        AddLegacy(ProceduralMaterialSlots.Oxide, Oxide);
        AddLegacy(ProceduralMaterialSlots.Enriched, Enriched);
        AddLegacy(ProceduralMaterialSlots.Quartz, Quartz);
        AddLegacy(ProceduralMaterialSlots.Acanthite, Acanthite);
        AddLegacy(ProceduralMaterialSlots.Electrum, Electrum);
        AddLegacy(ProceduralMaterialSlots.NativeSilver, NativeSilver);
        AddLegacy(ProceduralMaterialSlots.Geyserite, Geyserite);
        AddLegacy(ProceduralMaterialSlots.Gossan, Gossan);
    }

    private void AddLegacy(string slot, string code)
    {
        if (!string.IsNullOrWhiteSpace(code) && !Materials.ContainsKey(slot)) Materials[slot] = code;
    }
}

public static class ProceduralMaterialSlots
{
    public const string Primary = "primary";
    public const string Oxide = "oxide";
    public const string Enriched = "enriched";
    public const string Quartz = "quartz";
    public const string Acanthite = "acanthite";
    public const string Carbonatite = "carbonatite";
    public const string Electrum = "electrum";
    public const string NativeSilver = "native-silver";
    public const string Geyserite = "geyserite";
    public const string Gossan = "gossan";
    public const string RedClay = "red-clay";
    public const string Kaolinite = "kaolinite";
    public const string Wall = "wall";
    public const string Orthoclase = "orthoclase";
    public const string Olivine = "olivine";
    public const string Soapstone = "soapstone";
    public const string Intermediate = "intermediate";
    public const string Core = "core";
    public const string Schorl = "schorl";
    public const string Lepidolite = "lepidolite";
    public const string Pollucite = "pollucite";
    public const string Spodumene = "spodumene";
    public const string Beryl = "beryl";
    public const string Cassiterite = "cassiterite";
    public const string Tantalite = "tantalite";
    public const string Columbite = "columbite";
    public const string Secondary = "secondary";
    public const string Gangue = "gangue";
    public const string RareGangue = "rare-gangue";
    public const string Alteration = "alteration";
    public const string Albite = "albite";
    public const string Calcite = "calcite";
    public const string Breccia = "breccia";
    public const string Hematite = "hematite";
    public const string Limonite = "limonite";
    public const string Siderite = "siderite";
    public const string Magnetite = "magnetite";
    public const string Wolframite = "wolframite";
    public const string Chalcopyrite = "chalcopyrite";
    public const string Arsenopyrite = "arsenopyrite";
    public const string Sphalerite = "sphalerite";
    public const string Galena = "galena";
    public const string Fluorite = "fluorite";
    public const string Tourmaline = "tourmaline";
    public const string Molybdenite = "molybdenite";
    public const string Bismuthinite = "bismuthinite";
    public const string NativeBismuth = "native-bismuth";
    public const string Pyrite = "pyrite";
    public const string Topaz = "topaz";
    public const string Microlite = "microlite";
    public const string Pyrochlore = "pyrochlore";
    public const string Monazite = "monazite";
    public const string Bastnasite = "bastnasite";
    public const string Apatite = "apatite";
    public const string Pyrolusite = "pyrolusite";
    public const string Braunite = "braunite";
    public const string Rhodochrosite = "rhodochrosite";
    public const string Spessartine = "spessartine";
    public const string BlackGossan = "black-gossan";
    public const string Tetrahedrite = "tetrahedrite";
    public const string Malachite = "malachite";
    public const string Azurite = "azurite";
    public const string Barite = "barite";
    public const string Chalcocite = "chalcocite";
    public const string Chert = "chert";
    public const string GreenGossan = "green-gossan";
    public const string Smithsonite = "smithsonite";
    public const string Cerussite = "cerussite";
    public const string Umber = "umber";
    public const string GreyGossan = "grey-gossan";
    public const string YellowGossan = "yellow-gossan";
    public const string ChertHematite = "chert-hematite";
    public const string ChertMagnetite = "chert-magnetite";
    public const string ChertPyrite = "chert-pyrite";
    public const string Lignite = "lignite";
    public const string ClayParting = "clay-parting";
    public const string ChannelSand = "channel-sand";
    public const string Anthracite = "anthracite";
    public const string SlateParting = "slate-parting";
    public const string Uraninite = "uraninite";
    public const string Nickeline = "nickeline";
    public const string Cobaltite = "cobaltite";
    public const string Carbonate = "carbonate";
    public const string Torbernite = "torbernite";
    public const string Annabergite = "annabergite";
    public const string Erythrite = "erythrite";
    public const string PinkGossan = "pink-gossan";
    public const string BituminousCoal = "bituminous-coal";
    public const string ShaleParting = "shale-parting";
    public const string SeatEarth = "seat-earth";
    public const string Fireclay = "fireclay";
    public const string Cinnabar = "cinnabar";
    public const string Stannite = "stannite";
    public const string Stibnite = "stibnite";
    public const string Scheelite = "scheelite";
    public const string Borax = "borax";
    public const string Gypsum = "gypsum";
    public const string Alunite = "alunite";
    public const string Alum = "alum";
    public const string Bornite = "bornite";
    public const string Trona = "trona";
    public const string Pentlandite = "pentlandite";
    public const string Sperrylite = "sperrylite";
    public const string Kernite = "kernite";
    public const string NativeCopper = "native-copper";
    public const string Graphite = "graphite";
    public const string Magnesite = "magnesite";
    public const string Xenotime = "xenotime";
    public const string Gittinsite = "gittinsite";
    public const string Zirconia = "zirconia";
    public const string Matrix = "matrix";
    public const string Sulfur = "sulfur";
    public const string AuthigenicCarbonate = "authigenic-carbonate";
    public const string Celestine = "celestine";
    public const string CelestineGem = "celestine-gem";
    public const string Gold = "gold";
    public const string Chromite = "chromite";
    public const string Phosphorite = "phosphorite";
    public const string Lapis = "lapis";
    public const string Cryolite = "cryolite";
    public const string Ilmenite = "ilmenite";
    public const string Titanomagnetite = "titanomagnetite";
    public const string CriticalZone = "critical-zone";
    public const string MainZone = "main-zone";
    public const string UpperZone = "upper-zone";
    public const string Dyke = "dyke";
    public const string Raft = "raft";
    public const string Root = "root";
    public const string Microcline = "microcline";
}




internal sealed class CompiledProceduralDeposit
{
    private readonly Dictionary<string, int> slotIds;
    private readonly int[][][] resolvedBlocks;
    private readonly int[] resolvedSlotCounts;
    private readonly int[][] directBlockIds;
    private readonly int[][] fallbackBlockIds;
    private readonly HashSet<string> sourceRockVariants;
    private readonly bool[] replaceableHosts;
    private readonly bool[] naturalRockHosts;
    private readonly bool[] excludedGossanSurfaces;
    private readonly Dictionary<int, int> buriedWeatheringVariants = new();
    private readonly Dictionary<int, int> byproductSlotsByBlockId = new();
    private readonly Dictionary<int, CompiledByproductRule> byproductRulesBySlot = new();

    private static readonly string[] GradeNames = { "poor", "medium", "rich", "bountiful" };

    public IProceduralDepositTemplate Template { get; }
    public ProceduralDepositDefinition Definition { get; }
    public int SourceHostBlockCount { get; private set; }
    public int IntrusionTargetBlockCount { get; private set; }
    public int ExcludedGossanSurfaceCount { get; private set; }
    public string? ByproductValidationError { get; private set; }
    internal bool HasByproducts => byproductRulesBySlot.Count > 0;

    public ulong CodeHash { get; }
    public int MaximumHorizontalReach { get; }
    public int GossanBlockId => ResolveDirectSlot(ProceduralMaterialSlots.Gossan);
    public int GeyseriteBlockId => ResolveDirectSlot(ProceduralMaterialSlots.Geyserite);

    public CompiledProceduralDeposit(
        ProceduralDepositDefinition definition,
        IProceduralDepositTemplate template,
        ICoreServerAPI api)
    {
        Definition = definition;
        Template = template;
        CodeHash = ProceduralDepositMath.HashString(definition.Code);
        MaximumHorizontalReach = template.GetMaximumHorizontalReach(definition);

        definition.Palette.MigrateLegacyMaterials();
        sourceRockVariants = new HashSet<string>(definition.Source.EligibleRockVariants, StringComparer.OrdinalIgnoreCase);
        var replaceable = new HashSet<string>(definition.Palette.ReplaceableRockVariants, StringComparer.OrdinalIgnoreCase);
        KeyValuePair<string, string>[] materials = definition.Palette.Materials
            .OrderBy(entry => entry.Key, StringComparer.Ordinal)
            .ToArray();
        if (AdvancedGeologyConfig.Active.IHateGems)
        {
            materials = DepositContentFilters.ApplyGemSuppression(materials);
        }
        slotIds = new Dictionary<string, int>(materials.Length, StringComparer.Ordinal);
        int blockCount = api.World.Blocks.Count;
        resolvedBlocks = new int[materials.Length][][];
        resolvedSlotCounts = new int[materials.Length];
        directBlockIds = new int[materials.Length][];
        for (int slot = 0; slot < materials.Length; slot++)
        {
            string codePattern = materials[slot].Value;
            slotIds[materials[slot].Key] = slot;
            bool graded = codePattern.Contains("{grade}", StringComparison.Ordinal);
            bool hostIndexed = codePattern.Contains("{rock}", StringComparison.Ordinal);
            int gradeCount = graded ? GradeNames.Length : 1;
            resolvedBlocks[slot] = new int[gradeCount][];
            directBlockIds[slot] = new int[gradeCount];
            for (int grade = 0; grade < gradeCount; grade++)
            {
                if (hostIndexed)
                {
                    resolvedBlocks[slot][grade] = new int[blockCount];
                    continue;
                }
                directBlockIds[slot][grade] = ResolveDirectBlock(api, ApplyGrade(codePattern, graded, grade));
            }
        }

        // Use a declared-host variant when the live host lacks this material.
        fallbackBlockIds = new int[materials.Length][];
        string[] declaredHosts = definition.Palette.ReplaceableRockVariants
            .Where(variant => !string.IsNullOrWhiteSpace(variant))
            .ToArray();
        for (int slot = 0; slot < materials.Length; slot++)
        {
            string codePattern = materials[slot].Value;
            bool graded = codePattern.Contains("{grade}", StringComparison.Ordinal);
            int gradeCount = resolvedBlocks[slot].Length;
            fallbackBlockIds[slot] = new int[gradeCount];
            if (!codePattern.Contains("{rock}", StringComparison.Ordinal)) continue;

            for (int grade = 0; grade < gradeCount; grade++)
            {
                string coded = ApplyGrade(codePattern, graded, grade);
                foreach (string variant in declaredHosts)
                {
                    int resolved = ResolveDirectBlock(
                        api,
                        coded.Replace("{rock}", variant, StringComparison.Ordinal));
                    if (resolved == 0) continue;
                    fallbackBlockIds[slot][grade] = resolved;
                    break;
                }
            }
        }
        replaceableHosts = new bool[blockCount];
        naturalRockHosts = new bool[blockCount];

        foreach (Block? host in api.World.Blocks)
        {
            if (host?.Code == null || host.BlockId < 0 || host.BlockId >= blockCount) continue;
            if (!TryGetRockVariant(host, out string rockVariant)) continue;

            naturalRockHosts[host.BlockId] = true;
            IntrusionTargetBlockCount++;
            bool declaredHost = replaceable.Count == 0 || replaceable.Contains(rockVariant);
            if (declaredHost)
            {
                // Declared hosts remain valid even when a zone lacks a host-specific block.
                replaceableHosts[host.BlockId] = true;
                SourceHostBlockCount++;
            }
            if (!declaredHost && !definition.Intrude) continue;

            for (int slot = 0; slot < materials.Length; slot++)
            {
                string codePattern = materials[slot].Value;
                if (!codePattern.Contains("{rock}", StringComparison.Ordinal)) continue;

                bool graded = codePattern.Contains("{grade}", StringComparison.Ordinal);
                for (int grade = 0; grade < resolvedBlocks[slot].Length; grade++)
                {
                    string coded = ApplyGrade(codePattern, graded, grade);
                    ResolveSlot(api, host.BlockId, rockVariant, slot, grade, coded);
                }
            }
        }

        excludedGossanSurfaces = new bool[blockCount];
        foreach (string exclusion in definition.Palette.GossanSurfaceExclusions)
        {
            if (string.IsNullOrWhiteSpace(exclusion)) continue;

            var pattern = new AssetLocation(exclusion);
            foreach (Block? candidate in api.World.Blocks)
            {
                if (candidate?.Code == null || candidate.BlockId < 0 || candidate.BlockId >= blockCount) continue;
                if (!WildcardUtil.Match(pattern, candidate.Code)) continue;
                if (!excludedGossanSurfaces[candidate.BlockId]) ExcludedGossanSurfaceCount++;
                excludedGossanSurfaces[candidate.BlockId] = true;
            }
        }

        // Map surface variants to no-grass forms for buried weathering.
        foreach (int[] byGrade in directBlockIds)
        {
            foreach (int blockId in byGrade)
            {
                if (blockId == 0 || buriedWeatheringVariants.ContainsKey(blockId)) continue;
                Block? block = api.World.Blocks[blockId];
                string? path = block?.Code?.Path;
                if (path == null) continue;
                int separator = path.LastIndexOf('-');
                if (separator < 0) continue;
                string coverage = path[(separator + 1)..];
                if (coverage is not ("sparse" or "verysparse" or "normal")) continue;
                string buriedPath = string.Concat(path.AsSpan(0, separator + 1), "none");
                Block? buried = api.World.GetBlock(new AssetLocation(block!.Code.Domain, buriedPath));
                if (buried?.Code != null) buriedWeatheringVariants[blockId] = buried.BlockId;
            }
        }

        CompileByproducts();
    }

    public bool IsExcludedGossanSurface(int blockId)
    {
        return (uint)blockId < (uint)excludedGossanSurfaces.Length && excludedGossanSurfaces[blockId];
    }

    public bool IsReplaceableHost(int blockId)
    {
        return (uint)blockId < (uint)replaceableHosts.Length && replaceableHosts[blockId];
    }

    public bool IsNaturalRock(int blockId)
    {
        return (uint)blockId < (uint)naturalRockHosts.Length && naturalRockHosts[blockId];
    }

    public bool CanReplaceRock(int blockId)
    {
        bool sourceHost = IsReplaceableHost(blockId);
        return AllowsRockReplacement(sourceHost, Definition.Intrude, IsNaturalRock(blockId));
    }

    internal static bool AllowsRockReplacement(bool sourceHost, bool intrude, bool naturalRock)
    {
        return sourceHost || (intrude && naturalRock);
    }

    public int GetSlotId(string name)
    {
        return slotIds.TryGetValue(name, out int slotId) ? slotId : -1;
    }

    internal bool TryGetByproductSlotForBlock(int blockId, out int slotId)
    {
        return byproductSlotsByBlockId.TryGetValue(blockId, out slotId);
    }

    internal bool TryGetByproductRule(int slotId, out CompiledByproductRule rule)
    {
        return byproductRulesBySlot.TryGetValue(slotId, out rule!);
    }

    private void CompileByproducts()
    {
        if (!Definition.ValidateByproducts(out string validationError))
        {
            ByproductValidationError = validationError;
            return;
        }

        foreach ((string mineral, Dictionary<string, double[]> elements) in Definition.Byproducts)
        {
            int slotId = GetSlotId(mineral);
            if (slotId < 0)
            {
                ByproductValidationError = $"byproduct mineral slot '{mineral}' was not compiled";
                break;
            }

            var rule = new CompiledByproductRule(mineral, elements);
            byproductRulesBySlot.Add(slotId, rule);
            for (int grade = 0; grade < resolvedBlocks[slotId].Length; grade++)
            {
                int[]? byHost = resolvedBlocks[slotId][grade];
                if (byHost != null)
                {
                    foreach (int blockId in byHost)
                    {
                        if (blockId != 0 && !RegisterByproductBlock(mineral, slotId, blockId)) break;
                    }
                }
                if (ByproductValidationError != null) break;
                int direct = directBlockIds[slotId][grade];
                if (direct != 0 && !RegisterByproductBlock(mineral, slotId, direct)) break;
                int fallback = fallbackBlockIds[slotId][grade];
                if (fallback != 0 && !RegisterByproductBlock(mineral, slotId, fallback)) break;
            }
            if (ByproductValidationError != null) break;
        }

        if (ByproductValidationError != null)
        {
            byproductSlotsByBlockId.Clear();
            byproductRulesBySlot.Clear();
        }
    }

    private bool RegisterByproductBlock(string mineral, int slotId, int blockId)
    {
        if (byproductSlotsByBlockId.TryGetValue(blockId, out int registeredSlot))
        {
            if (registeredSlot == slotId) return true;
            ByproductValidationError = $"byproduct mineral slot '{mineral}' shares block id {blockId} with another configured byproduct slot";
            return false;
        }
        for (int otherSlot = 0; otherSlot < resolvedBlocks.Length; otherSlot++)
        {
            if (otherSlot == slotId || !SlotResolvesToBlock(otherSlot, blockId)) continue;
            string? otherMineral = slotIds.FirstOrDefault(entry => entry.Value == otherSlot).Key;
            ByproductValidationError = $"byproduct mineral slot '{mineral}' shares block id {blockId} with palette slot '{otherMineral ?? otherSlot.ToString()}'";
            return false;
        }
        byproductSlotsByBlockId[blockId] = slotId;
        return true;
    }

    private IEnumerable<int> EnumerateResolvedBlocks(int slotId)
    {
        foreach (int[]? byHost in resolvedBlocks[slotId])
        {
            if (byHost == null) continue;
            foreach (int blockId in byHost) yield return blockId;
        }
        foreach (int blockId in directBlockIds[slotId]) yield return blockId;
        foreach (int blockId in fallbackBlockIds[slotId]) yield return blockId;
    }

    private bool SlotResolvesToBlock(int slotId, int expectedBlockId)
    {
        foreach (int blockId in EnumerateResolvedBlocks(slotId))
        {
            if (blockId == expectedBlockId) return true;
        }
        return false;
    }

    public static int GradeCount => GradeNames.Length;

    public int ResolveBlock(int slotId, int hostBlockId)
    {
        return ResolveBlock(slotId, 0, hostBlockId);
    }

    /// <summary>
    /// Resolves a material slot at the requested quality grade. Slots whose code carries no
    /// {grade} token ignore the grade; a grade that is not registered for this ore steps down
    /// to the richest lower grade that is.
    /// </summary>
    public int ResolveBlock(int slotId, int gradeIndex, int hostBlockId)
    {
        if ((uint)slotId >= (uint)resolvedBlocks.Length) return 0;
        int[][] byGrade = resolvedBlocks[slotId];
        int[] direct = directBlockIds[slotId];
        int[] fallback = fallbackBlockIds[slotId];
        for (int grade = Math.Min(gradeIndex, byGrade.Length - 1); grade >= 0; grade--)
        {
            int[]? byHost = byGrade[grade];
            int hostResolved = byHost != null && (uint)hostBlockId < (uint)byHost.Length ? byHost[hostBlockId] : 0;
            int selected = SelectResolution(hostResolved, direct[grade], fallback[grade]);
            if (selected != 0) return selected;
        }
        return 0;
    }

    /// <summary>
    /// Resolution precedence for one grade: the ore registered for the live host rock wins;
    /// otherwise a host-independent material; otherwise the same ore registered in one of the
    /// deposit's own declared hosts. The last case covers intruding bodies, which may be
    /// emplaced in country rock their ore was never registered against.
    /// </summary>
    internal static int SelectResolution(int hostResolved, int direct, int declaredHostFallback)
    {
        if (hostResolved != 0) return hostResolved;
        if (direct != 0) return direct;
        return declaredHostFallback;
    }

    public int ResolveWeatheredBlock(int slotId, int gradeIndex, int hostBlockId, bool buried)
    {
        int resolved = ResolveBlock(slotId, gradeIndex, hostBlockId);
        return SelectWeatheringVariant(resolved, buried, buriedWeatheringVariants);
    }

    internal static int SelectWeatheringVariant(
        int resolved,
        bool buried,
        IReadOnlyDictionary<int, int> buriedVariants)
    {
        return buried && buriedVariants.TryGetValue(resolved, out int noGrass)
            ? noGrass
            : resolved;
    }

    public int ResolveDirectSlot(string name)
    {
        int slotId = GetSlotId(name);
        return (uint)slotId < (uint)directBlockIds.Length ? directBlockIds[slotId][0] : 0;
    }

    public bool HasResolvedSlot(string name)
    {
        int slotId = GetSlotId(name);
        if ((uint)slotId >= (uint)resolvedSlotCounts.Length) return false;
        if (resolvedSlotCounts[slotId] > 0) return true;
        foreach (int blockId in directBlockIds[slotId])
        {
            if (blockId != 0) return true;
        }
        return false;
    }

    public void CollectOutputBlockIds(bool[] output)
    {
        foreach (int[][] byGrade in resolvedBlocks)
        {
            foreach (int[]? byHost in byGrade)
            {
                if (byHost == null) continue;
                foreach (int blockId in byHost)
                {
                    if ((uint)blockId < (uint)output.Length && blockId != 0) output[blockId] = true;
                }
            }
        }
        foreach (int[] byGrade in directBlockIds)
        {
            foreach (int blockId in byGrade)
            {
                if ((uint)blockId < (uint)output.Length && blockId != 0) output[blockId] = true;
            }
        }
    }

    public bool IsEligibleSource(Block block)
    {
        return TryGetRockVariant(block, out string variant) && sourceRockVariants.Contains(variant);
    }

    public static bool TryGetRockVariant(Block block, out string variant)
    {
        variant = string.Empty;
        string? path = block.Code?.Path;
        if (path == null || !path.StartsWith("rock-", StringComparison.Ordinal)) return false;
        variant = path[5..];
        return variant.Length > 0;
    }

    private static string ApplyGrade(string codePattern, bool graded, int grade)
    {
        return graded
            ? codePattern.Replace("{grade}", GradeNames[grade], StringComparison.Ordinal)
            : codePattern;
    }

    private bool ResolveSlot(
        ICoreServerAPI api,
        int hostBlockId,
        string rockVariant,
        int slotId,
        int gradeIndex,
        string codePattern)
    {
        if (string.IsNullOrWhiteSpace(codePattern)) return false;
        string code = codePattern.Replace("{rock}", rockVariant, StringComparison.Ordinal);
        Block? block = api.World.GetBlock(new AssetLocation(code));
        if (block?.Code == null) return false;
        resolvedBlocks[slotId][gradeIndex][hostBlockId] = block.BlockId;
        resolvedSlotCounts[slotId]++;
        return true;
    }

    private static int ResolveDirectBlock(ICoreServerAPI api, string code)
    {
        if (string.IsNullOrWhiteSpace(code)) return 0;
        Block? block = api.World.GetBlock(new AssetLocation(code));
        return block?.Code == null ? 0 : block.BlockId;
    }
}
