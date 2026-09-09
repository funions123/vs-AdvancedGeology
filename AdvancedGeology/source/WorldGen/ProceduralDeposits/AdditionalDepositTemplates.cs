using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using Vintagestory.API.Server;

namespace AdvancedGeology.WorldGen.ProceduralDeposits;

internal interface IAdditionalDepositPlan
{
    int HorizontalRadius { get; }
    int VerticalHalfHeight { get; }
    AdditionalDepositSample Evaluate(int worldX, int worldY, int worldZ);
}

internal readonly record struct AdditionalDepositSample(string? Slot, int Grade = 0, double Density = 1.0);

[JsonObject(MemberSerialization.OptIn)]
public sealed class VeinGraphiteDefinition
{
    [JsonProperty] public int HorizontalRadius { get; set; } = 36;
    [JsonProperty] public int VerticalHalfHeight { get; set; } = 36;
    [JsonProperty] public int PrincipalMin { get; set; } = 3;
    [JsonProperty] public int PrincipalMax { get; set; } = 5;
    [JsonProperty] public int BranchMin { get; set; } = 1;
    [JsonProperty] public int BranchMax { get; set; } = 2;
}

[JsonObject(MemberSerialization.OptIn)]
public sealed class FlakeGraphiteSchistDefinition
{
    [JsonProperty] public int HorizontalRadius { get; set; } = 34;
    [JsonProperty] public int VerticalHalfHeight { get; set; } = 30;
    [JsonProperty] public int HorizonMin { get; set; } = 2;
    [JsonProperty] public int HorizonMax { get; set; } = 3;
    [JsonProperty] public int ShearMin { get; set; } = 2;
    [JsonProperty] public int ShearMax { get; set; } = 4;
}

[JsonObject(MemberSerialization.OptIn)]
public sealed class UnconformityUraniumDefinition
{
    [JsonProperty] public int HorizontalRadius { get; set; } = 38;
    [JsonProperty] public int VerticalHalfHeight { get; set; } = 34;
    [JsonProperty] public int SplayMin { get; set; } = 1;
    [JsonProperty] public int SplayMax { get; set; } = 2;
    [JsonProperty] public int PerchedMin { get; set; } = 0;
    [JsonProperty] public int PerchedMax { get; set; } = 2;
}

[JsonObject(MemberSerialization.OptIn)]
public sealed class SparryMagnesiteDefinition
{
    [JsonProperty] public int HorizontalRadius { get; set; } = 34;
    [JsonProperty] public int VerticalHalfHeight { get; set; } = 30;
    [JsonProperty] public int FaultMin { get; set; } = 2;
    [JsonProperty] public int FaultMax { get; set; } = 4;
    [JsonProperty] public int MantoMin { get; set; } = 3;
    [JsonProperty] public int MantoMax { get; set; } = 5;
}

[JsonObject(MemberSerialization.OptIn)]
public sealed class CryptocrystallineMagnesiteDefinition
{
    [JsonProperty] public int HorizontalRadius { get; set; } = 34;
    [JsonProperty] public int VerticalHalfHeight { get; set; } = 28;
    [JsonProperty] public int VeinMin { get; set; } = 5;
    [JsonProperty] public int VeinMax { get; set; } = 8;
    [JsonProperty] public int PodMin { get; set; } = 5;
    [JsonProperty] public int PodMax { get; set; } = 9;
}

[JsonObject(MemberSerialization.OptIn)]
public sealed class PeralkalineHreeDefinition
{
    [JsonProperty] public int HorizontalRadius { get; set; } = 34;
    [JsonProperty] public int VerticalHalfHeight { get; set; } = 34;
    [JsonProperty] public int PegmatiteMin { get; set; } = 3;
    [JsonProperty] public int PegmatiteMax { get; set; } = 5;
    [JsonProperty] public int FractureMin { get; set; } = 3;
    [JsonProperty] public int FractureMax { get; set; } = 5;
}

internal abstract class AdditionalProceduralTemplate<TPlan> : IProceduralDepositTemplate where TPlan : IAdditionalDepositPlan
{
    public abstract string Code { get; }
    public abstract IReadOnlyList<string> RequiredMaterialSlots { get; }
    public bool RequiresPlan => true;
    protected abstract int Radius(ProceduralDepositDefinition definition);
    protected abstract bool IsValid(ProceduralDepositDefinition definition);
    protected abstract TPlan Build(in ProceduralDepositInstance instance, ProceduralDepositDefinition definition);

    public int GetMaximumHorizontalReach(ProceduralDepositDefinition definition) => Radius(definition);
    public object CreatePlan(in ProceduralDepositInstance instance, ProceduralDepositDefinition definition) => Build(instance, definition);
    public bool Validate(ProceduralDepositDefinition definition, out string error)
    {
        bool valid = IsValid(definition);
        error = valid ? string.Empty : $"invalid {Code} settings";
        return valid;
    }
    public void Realize(ProceduralDepositWorldGenSystem system, in DepositCandidate candidate, IChunkColumnGenerateRequest request, int baseX, int baseZ) =>
        system.RealizeAdditionalDepositCandidate(candidate, request, baseX, baseZ);
}

internal sealed class VeinGraphiteProceduralTemplate : AdditionalProceduralTemplate<VeinGraphitePlan>
{
    private static readonly string[] Slots = [ProceduralMaterialSlots.Graphite, ProceduralMaterialSlots.Quartz];
    public override string Code => "veinGraphite";
    public override IReadOnlyList<string> RequiredMaterialSlots => Slots;
    protected override int Radius(ProceduralDepositDefinition d) => d.VeinGraphite.HorizontalRadius;
    protected override bool IsValid(ProceduralDepositDefinition d) => d.VeinGraphite.HorizontalRadius >= 30 && d.VeinGraphite.VerticalHalfHeight >= 28 && d.VeinGraphite.PrincipalMin >= 1 && d.VeinGraphite.PrincipalMax >= d.VeinGraphite.PrincipalMin && d.VeinGraphite.BranchMax >= d.VeinGraphite.BranchMin;
    protected override VeinGraphitePlan Build(in ProceduralDepositInstance i, ProceduralDepositDefinition d) => VeinGraphitePlan.Create(i, d.VeinGraphite);
}

internal sealed class FlakeGraphiteSchistProceduralTemplate : AdditionalProceduralTemplate<FlakeGraphiteSchistPlan>
{
    private static readonly string[] Slots = [ProceduralMaterialSlots.Graphite];
    public override string Code => "flakeGraphiteSchist";
    public override IReadOnlyList<string> RequiredMaterialSlots => Slots;
    protected override int Radius(ProceduralDepositDefinition d) => d.FlakeGraphiteSchist.HorizontalRadius;
    protected override bool IsValid(ProceduralDepositDefinition d) => d.FlakeGraphiteSchist.HorizontalRadius >= 30 && d.FlakeGraphiteSchist.VerticalHalfHeight >= 26 && d.FlakeGraphiteSchist.HorizonMin >= 1 && d.FlakeGraphiteSchist.HorizonMax >= d.FlakeGraphiteSchist.HorizonMin && d.FlakeGraphiteSchist.ShearMax >= d.FlakeGraphiteSchist.ShearMin;
    protected override FlakeGraphiteSchistPlan Build(in ProceduralDepositInstance i, ProceduralDepositDefinition d) => FlakeGraphiteSchistPlan.Create(i, d.FlakeGraphiteSchist);
}

internal sealed class UnconformityUraniumProceduralTemplate : AdditionalProceduralTemplate<UnconformityUraniumPlan>
{
    private static readonly string[] Slots = [ProceduralMaterialSlots.Uraninite, ProceduralMaterialSlots.Quartz, ProceduralMaterialSlots.Hematite];
    public override string Code => "unconformityUranium";
    public override IReadOnlyList<string> RequiredMaterialSlots => Slots;
    protected override int Radius(ProceduralDepositDefinition d) => d.UnconformityUranium.HorizontalRadius;
    protected override bool IsValid(ProceduralDepositDefinition d) => d.UnconformityUranium.HorizontalRadius >= 34 && d.UnconformityUranium.VerticalHalfHeight >= 30 && d.UnconformityUranium.SplayMin >= 1 && d.UnconformityUranium.SplayMax >= d.UnconformityUranium.SplayMin;
    protected override UnconformityUraniumPlan Build(in ProceduralDepositInstance i, ProceduralDepositDefinition d) => UnconformityUraniumPlan.Create(i, d.UnconformityUranium);
}

internal sealed class SparryMagnesiteProceduralTemplate : AdditionalProceduralTemplate<SparryMagnesitePlan>
{
    private static readonly string[] Slots = [ProceduralMaterialSlots.Magnesite, ProceduralMaterialSlots.Quartz];
    public override string Code => "sparryMagnesiteReplacement";
    public override IReadOnlyList<string> RequiredMaterialSlots => Slots;
    protected override int Radius(ProceduralDepositDefinition d) => d.SparryMagnesite.HorizontalRadius;
    protected override bool IsValid(ProceduralDepositDefinition d) => d.SparryMagnesite.HorizontalRadius >= 30 && d.SparryMagnesite.VerticalHalfHeight >= 25 && d.SparryMagnesite.FaultMin >= 1 && d.SparryMagnesite.FaultMax >= d.SparryMagnesite.FaultMin && d.SparryMagnesite.MantoMin >= 1 && d.SparryMagnesite.MantoMax >= d.SparryMagnesite.MantoMin;
    protected override SparryMagnesitePlan Build(in ProceduralDepositInstance i, ProceduralDepositDefinition d) => SparryMagnesitePlan.Create(i, d.SparryMagnesite);
}

internal sealed class CryptocrystallineMagnesiteProceduralTemplate : AdditionalProceduralTemplate<CryptocrystallineMagnesitePlan>
{
    private static readonly string[] Slots =
    [
        ProceduralMaterialSlots.Magnesite,
        ProceduralMaterialSlots.Quartz,
        ProceduralMaterialSlots.Olivine,
        ProceduralMaterialSlots.Soapstone
    ];
    public override string Code => "cryptocrystallineMagnesite";
    public override IReadOnlyList<string> RequiredMaterialSlots => Slots;
    protected override int Radius(ProceduralDepositDefinition d) => d.CryptocrystallineMagnesite.HorizontalRadius;
    protected override bool IsValid(ProceduralDepositDefinition d) => d.CryptocrystallineMagnesite.HorizontalRadius >= 30 && d.CryptocrystallineMagnesite.VerticalHalfHeight >= 24 && d.CryptocrystallineMagnesite.VeinMin >= 1 && d.CryptocrystallineMagnesite.VeinMax >= d.CryptocrystallineMagnesite.VeinMin && d.CryptocrystallineMagnesite.PodMin >= 1 && d.CryptocrystallineMagnesite.PodMax >= d.CryptocrystallineMagnesite.PodMin;
    protected override CryptocrystallineMagnesitePlan Build(in ProceduralDepositInstance i, ProceduralDepositDefinition d) => CryptocrystallineMagnesitePlan.Create(i, d.CryptocrystallineMagnesite);
}

internal sealed class PeralkalineHreeProceduralTemplate : AdditionalProceduralTemplate<PeralkalineHreePlan>
{
    private static readonly string[] Slots = [ProceduralMaterialSlots.Xenotime, ProceduralMaterialSlots.Gittinsite, ProceduralMaterialSlots.Zirconia, ProceduralMaterialSlots.Pyrochlore, ProceduralMaterialSlots.Bastnasite, ProceduralMaterialSlots.Quartz, ProceduralMaterialSlots.Fluorite, ProceduralMaterialSlots.Hematite, ProceduralMaterialSlots.Matrix];
    public override string Code => "peralkalineHreeComplex";
    public override IReadOnlyList<string> RequiredMaterialSlots => Slots;
    protected override int Radius(ProceduralDepositDefinition d) => d.PeralkalineHree.HorizontalRadius;
    protected override bool IsValid(ProceduralDepositDefinition d) => d.PeralkalineHree.HorizontalRadius >= 30 && d.PeralkalineHree.VerticalHalfHeight >= 30 && d.PeralkalineHree.PegmatiteMin >= 1 && d.PeralkalineHree.PegmatiteMax >= d.PeralkalineHree.PegmatiteMin && d.PeralkalineHree.FractureMin >= 1 && d.PeralkalineHree.FractureMax >= d.PeralkalineHree.FractureMin;
    protected override PeralkalineHreePlan Build(in ProceduralDepositInstance i, ProceduralDepositDefinition d) => PeralkalineHreePlan.Create(i, d.PeralkalineHree);
}

internal static class AdditionalDepositMath
{
    public static double Clamp(double v, double lo, double hi) => Math.Max(lo, Math.Min(hi, v));
    public static double Hash(double x, double y, double z)
    {
        double n = Math.Sin(x * 12.9898 + y * 78.233 + z * 37.719) * 43758.5453;
        return n - Math.Floor(n);
    }
}
