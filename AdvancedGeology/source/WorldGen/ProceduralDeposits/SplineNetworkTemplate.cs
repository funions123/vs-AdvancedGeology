using System;
using System.Collections.Generic;

namespace AdvancedGeology.WorldGen.ProceduralDeposits;

internal enum SplineMaterialZone
{
    Wall,
    Schorl,
    Orthoclase,
    Intermediate,
    Core,
    Albite,
    Lepidolite,
    Pollucite,
    Spodumene,
    Beryl,
    Cassiterite,
    Tantalite,
    Columbite
}

internal enum SplineReplacementBody
{
    None,
    Pollucite,
    Lepidolite
}

internal readonly record struct SplineNetworkSample(
    bool Inside,
    double SignedThickness,
    double ThicknessRatio,
    double WidthRatio,
    double AlongRatio,
    int BranchIndex,
    bool IsTrunk,
    SplineReplacementBody Body);

internal readonly record struct SplineColumn(int Start, int Count, int MinY, int MaxY)
{
    public bool HasSegments => Count > 0;
}

/// <summary>
/// Tourmaline, feldspars, quartz, lithium and cesium minerals, beryl, cassiterite, tantalum-niobium minerals, and wall rock form a curved branching pegmatite sheet network with replacement pods.
/// </summary>
internal sealed class SplineNetworkPlan
{
    private const int CellSize = 8;
    private const ulong PlanSalt = 0x53504C494E455457UL;

    private readonly SplineNode[] nodes;
    private readonly int[] segmentNodes;
    private readonly ReplacementPod[] pods;
    private readonly int[] cellSegmentStart;
    private readonly int[] cellSegments;
    private readonly int[] cellMinY;
    private readonly int[] cellMaxY;
    private readonly int originX;
    private readonly int originY;
    private readonly int originZ;
    private readonly int reach;
    private readonly int gridSizeX;
    private readonly int gridSizeZ;

    public int MinY { get; }
    public int MaxY { get; }
    public int SegmentCount => segmentNodes.Length;
    public int BranchCount { get; }
    public int PodCount => pods.Length;
    public double MaxHorizontalOffset { get; }
    public double MaxVerticalOffset { get; }

    private SplineNetworkPlan(
        in ProceduralDepositInstance instance,
        int reach,
        SplineNode[] nodes,
        int[] segmentNodes,
        ReplacementPod[] pods,
        int branchCount,
        double maxHorizontalOffset,
        double maxVerticalOffset)
    {
        originX = instance.CenterX;
        originY = instance.CenterY;
        originZ = instance.CenterZ;
        this.reach = reach;
        this.nodes = nodes;
        this.segmentNodes = segmentNodes;
        this.pods = pods;
        BranchCount = branchCount;
        MaxHorizontalOffset = maxHorizontalOffset;
        MaxVerticalOffset = maxVerticalOffset;

        gridSizeX = 2 * reach / CellSize + 1;
        gridSizeZ = gridSizeX;
        int cellCount = gridSizeX * gridSizeZ;
        cellSegmentStart = new int[cellCount + 1];
        cellMinY = new int[cellCount];
        cellMaxY = new int[cellCount];
        for (int cell = 0; cell < cellCount; cell++)
        {
            cellMinY[cell] = int.MaxValue;
            cellMaxY[cell] = int.MinValue;
        }

        var counts = new int[cellCount];
        for (int segment = 0; segment < segmentNodes.Length; segment++)
        {
            GetSegmentCellRange(segment, out int minCellX, out int maxCellX, out int minCellZ, out int maxCellZ);
            for (int cellX = minCellX; cellX <= maxCellX; cellX++)
            {
                for (int cellZ = minCellZ; cellZ <= maxCellZ; cellZ++)
                {
                    counts[cellZ * gridSizeX + cellX]++;
                }
            }
        }

        int running = 0;
        for (int cell = 0; cell < cellCount; cell++)
        {
            cellSegmentStart[cell] = running;
            running += counts[cell];
        }
        cellSegmentStart[cellCount] = running;
        cellSegments = new int[running];
        var cursor = new int[cellCount];

        int planMinY = int.MaxValue;
        int planMaxY = int.MinValue;
        for (int segment = 0; segment < segmentNodes.Length; segment++)
        {
            GetSegmentCellRange(segment, out int minCellX, out int maxCellX, out int minCellZ, out int maxCellZ);
            SplineNode start = nodes[segmentNodes[segment]];
            SplineNode end = nodes[segmentNodes[segment] + 1];
            int radius = (int)Math.Ceiling(SegmentRadius(start, end));
            int segmentMinY = originY + (int)Math.Floor(Math.Min(start.Y, end.Y)) - radius;
            int segmentMaxY = originY + (int)Math.Ceiling(Math.Max(start.Y, end.Y)) + radius;
            planMinY = Math.Min(planMinY, segmentMinY);
            planMaxY = Math.Max(planMaxY, segmentMaxY);

            for (int cellX = minCellX; cellX <= maxCellX; cellX++)
            {
                for (int cellZ = minCellZ; cellZ <= maxCellZ; cellZ++)
                {
                    int cell = cellZ * gridSizeX + cellX;
                    cellSegments[cellSegmentStart[cell] + cursor[cell]++] = segment;
                    if (segmentMinY < cellMinY[cell]) cellMinY[cell] = segmentMinY;
                    if (segmentMaxY > cellMaxY[cell]) cellMaxY[cell] = segmentMaxY;
                }
            }
        }

        MinY = planMinY == int.MaxValue ? originY : planMinY;
        MaxY = planMaxY == int.MinValue ? originY : planMaxY;
    }

    public static SplineNetworkPlan? Create(
        in ProceduralDepositInstance instance,
        SplineNetworkDefinition settings)
    {
        var random = new ProceduralDepositRandom(instance.FeatureId ^ PlanSalt);
        int reach = Math.Max(16, settings.HorizontalRadius);
        double bodyMargin = Math.Max(settings.Thickness, settings.Width) * 0.5 + 2;
        double horizontalLimit = Math.Max(8, reach - bodyMargin);
        double verticalLimit = Math.Max(8, settings.VerticalHalfHeight - bodyMargin);
        double spacing = Math.Clamp(settings.SegmentLength, 2, 16);
        double roll = random.Range(settings.SheetRollMinDeg, settings.SheetRollMaxDeg) * Math.PI / 180.0;
        double trunkHeading = random.Range(0, Math.PI * 2);
        double upwardSlope = random.Range(settings.UpwardSlopeMin, settings.UpwardSlopeMax);
        double trunkSeed = random.Range(0, 100);
        double trunkLength = settings.TrunkLength;

        var nodes = new List<SplineNode>(256);
        var segments = new List<int>(256);

        // Shrink the trunk until the built, centred centerline fits the declared plan bounds.
        int trunkStart = 0;
        int trunkCount = 0;
        for (int attempt = 0; attempt < 5; attempt++)
        {
            nodes.Clear();
            segments.Clear();
            trunkStart = 0;
            BuildBranch(
                nodes,
                0,
                0,
                0,
                trunkHeading,
                Math.Atan(upwardSlope),
                trunkLength,
                settings.Thickness,
                settings.Width,
                roll,
                trunkSeed,
                settings,
                spacing,
                0,
                true);
            trunkCount = nodes.Count - trunkStart;
            if (trunkCount < 2) return null;

            Recenter(nodes, trunkStart, trunkCount);
            MeasureExtent(nodes, out double horizontal, out double vertical);
            double horizontalRatio = horizontal <= horizontalLimit ? 1 : horizontalLimit / horizontal;
            double verticalRatio = vertical <= verticalLimit ? 1 : verticalLimit / vertical;
            double ratio = Math.Min(horizontalRatio, verticalRatio);
            if (ratio >= 1) break;

            trunkLength *= ratio * 0.96;
            if (trunkLength < spacing * 3) return null;
        }

        AddSegments(segments, trunkStart, trunkCount);

        // Branches leave the parent sheet at its margin, not from the centerline.
        int branchCount = random.NextInt(settings.BranchMin, settings.BranchMax);
        int builtBranches = 1;
        var branchRanges = new List<(int Start, int Count)>(branchCount + 1) { (trunkStart, trunkCount) };
        for (int index = 0; index < branchCount; index++)
        {
            double attachFraction = 0.28 + 0.54 * index / Math.Max(1, branchCount - 1);
            attachFraction = Math.Clamp(attachFraction + random.Range(-0.04, 0.04), 0.1, 0.92);
            SplineNode anchor = nodes[trunkStart + Math.Clamp(
                (int)Math.Round(attachFraction * (trunkCount - 1)),
                0,
                trunkCount - 1)];
            int side = index % 2 == 0 ? 1 : -1;
            double edgeOffset = anchor.HalfWidth * 0.98 * side;
            double divergence = side * settings.BranchDivergenceDeg * random.Range(0.62, 1.0) * Math.PI / 180.0;
            double branchLength = trunkLength
                * settings.BranchLengthFraction
                * random.Range(0.62, 1.0);
            double branchScale = Math.Clamp(settings.BranchScale, 0.2, 1.0) * random.Range(0.9, 1.1);

            int branchStart = nodes.Count;
            BuildBranch(
                nodes,
                anchor.X + anchor.BinormalX * edgeOffset,
                anchor.Y + anchor.BinormalY * edgeOffset,
                anchor.Z + anchor.BinormalZ * edgeOffset,
                Math.Atan2(anchor.TangentZ, anchor.TangentX) + divergence,
                Math.Asin(Math.Clamp(anchor.TangentY, -1, 1)) + random.Range(-0.08, 0.08),
                branchLength,
                settings.Thickness * branchScale,
                settings.Width * branchScale,
                roll + random.Range(-0.1, 0.1),
                trunkSeed + 11 + index * 12,
                settings,
                spacing,
                builtBranches,
                false);

            int addedNodes = nodes.Count - branchStart;
            if (addedNodes < 2)
            {
                nodes.RemoveRange(branchStart, addedNodes);
                continue;
            }

            addedNodes = ClipToBounds(nodes, branchStart, addedNodes, horizontalLimit, verticalLimit);
            if (addedNodes < 2)
            {
                nodes.RemoveRange(branchStart, nodes.Count - branchStart);
                continue;
            }

            AddSegments(segments, branchStart, addedNodes);
            branchRanges.Add((branchStart, addedNodes));
            builtBranches++;
        }

        SplineNode[] nodeArray = nodes.ToArray();
        ReplacementPod[] podArray = BuildPods(nodeArray, branchRanges, settings, ref random);
        MeasureExtent(nodes, out double finalHorizontal, out double finalVertical);

        return new SplineNetworkPlan(
            instance,
            reach,
            nodeArray,
            segments.ToArray(),
            podArray,
            builtBranches,
            finalHorizontal,
            finalVertical);
    }

    public bool TryGetColumn(int worldX, int worldZ, out SplineColumn column)
    {
        int localX = worldX - originX + reach;
        int localZ = worldZ - originZ + reach;
        if (localX < 0 || localZ < 0)
        {
            column = default;
            return false;
        }

        int cellX = localX / CellSize;
        int cellZ = localZ / CellSize;
        if (cellX >= gridSizeX || cellZ >= gridSizeZ)
        {
            column = default;
            return false;
        }

        int cell = cellZ * gridSizeX + cellX;
        int start = cellSegmentStart[cell];
        int count = cellSegmentStart[cell + 1] - start;
        if (count == 0)
        {
            column = default;
            return false;
        }

        column = new SplineColumn(start, count, cellMinY[cell], cellMaxY[cell]);
        return true;
    }

    public SplineNetworkSample Sample(int worldX, int worldY, int worldZ)
    {
        return TryGetColumn(worldX, worldZ, out SplineColumn column)
            ? Sample(column, worldX, worldY, worldZ)
            : default;
    }

    public SplineNetworkSample Sample(in SplineColumn column, int worldX, int worldY, int worldZ)
    {
        double px = worldX - originX;
        double py = worldY - originY;
        double pz = worldZ - originZ;
        double bestMetric = double.PositiveInfinity;
        var best = default(SplineNetworkSample);

        for (int index = 0; index < column.Count; index++)
        {
            EvaluateSegment(cellSegments[column.Start + index], px, py, pz, ref bestMetric, ref best);
        }

        return best.Inside ? best with { Body = SamplePods(px, py, pz) } : best;
    }

    /// <summary>
    /// Reference evaluation over every segment. Used by verification to prove that the
    /// spatial index registers each segment in every cell that can reach it.
    /// </summary>
    public SplineNetworkSample SampleExhaustive(int worldX, int worldY, int worldZ)
    {
        double px = worldX - originX;
        double py = worldY - originY;
        double pz = worldZ - originZ;
        double bestMetric = double.PositiveInfinity;
        var best = default(SplineNetworkSample);

        for (int segment = 0; segment < segmentNodes.Length; segment++)
        {
            EvaluateSegment(segment, px, py, pz, ref bestMetric, ref best);
        }

        return best.Inside ? best with { Body = SamplePods(px, py, pz) } : best;
    }

    private SplineReplacementBody SamplePods(double px, double py, double pz)
    {
        foreach (ReplacementPod pod in pods)
        {
            double dx = px - pod.X;
            double dy = py - pod.Y;
            double dz = pz - pod.Z;
            double along = (dx * pod.TangentX + dy * pod.TangentY + dz * pod.TangentZ) / pod.RadiusLength;
            double through = (dx * pod.NormalX + dy * pod.NormalY + dz * pod.NormalZ) / pod.RadiusThrough;
            double across = (dx * pod.BinormalX + dy * pod.BinormalY + dz * pod.BinormalZ) / pod.RadiusAcross;
            if (along * along + through * through + across * across <= 1.0) return pod.Body;
        }
        return SplineReplacementBody.None;
    }

    private void EvaluateSegment(
        int segment,
        double px,
        double py,
        double pz,
        ref double bestMetric,
        ref SplineNetworkSample best)
    {
        int nodeIndex = segmentNodes[segment];
        SplineNode start = nodes[nodeIndex];
        SplineNode end = nodes[nodeIndex + 1];

        double axisX = end.X - start.X;
        double axisY = end.Y - start.Y;
        double axisZ = end.Z - start.Z;
        double axisLengthSquared = axisX * axisX + axisY * axisY + axisZ * axisZ;
        if (axisLengthSquared <= 1e-9) return;

        double offsetX = px - start.X;
        double offsetY = py - start.Y;
        double offsetZ = pz - start.Z;
        double projection = (offsetX * axisX + offsetY * axisY + offsetZ * axisZ) / axisLengthSquared;
        double clamped = Math.Clamp(projection, 0, 1);

        double deltaX = offsetX - axisX * clamped;
        double deltaY = offsetY - axisY * clamped;
        double deltaZ = offsetZ - axisZ * clamped;

        double normalX = start.NormalX + (end.NormalX - start.NormalX) * clamped;
        double normalY = start.NormalY + (end.NormalY - start.NormalY) * clamped;
        double normalZ = start.NormalZ + (end.NormalZ - start.NormalZ) * clamped;
        double normalScale = Math.Sqrt(normalX * normalX + normalY * normalY + normalZ * normalZ);
        if (normalScale <= 1e-9) return;
        normalX /= normalScale;
        normalY /= normalScale;
        normalZ /= normalScale;

        double binormalX = start.BinormalX + (end.BinormalX - start.BinormalX) * clamped;
        double binormalY = start.BinormalY + (end.BinormalY - start.BinormalY) * clamped;
        double binormalZ = start.BinormalZ + (end.BinormalZ - start.BinormalZ) * clamped;
        double binormalScale = Math.Sqrt(
            binormalX * binormalX + binormalY * binormalY + binormalZ * binormalZ);
        if (binormalScale <= 1e-9) return;
        binormalX /= binormalScale;
        binormalY /= binormalScale;
        binormalZ /= binormalScale;

        double halfThickness = Math.Max(
            0.35,
            start.HalfThickness + (end.HalfThickness - start.HalfThickness) * clamped);
        double halfWidth = Math.Max(
            0.5,
            start.HalfWidth + (end.HalfWidth - start.HalfWidth) * clamped);

        double through = (deltaX * normalX + deltaY * normalY + deltaZ * normalZ) / halfThickness;
        double across = (deltaX * binormalX + deltaY * binormalY + deltaZ * binormalZ) / halfWidth;
        double acrossSquared = across * across;
        double overshoot = projection < 0
            ? -projection
            : (projection > 1 ? projection - 1 : 0);
        double overshootDistance = overshoot * Math.Sqrt(axisLengthSquared) / halfThickness;

        double metric = through * through
            + acrossSquared * acrossSquared * acrossSquared
            + overshootDistance * overshootDistance;
        if (metric >= bestMetric) return;

        bestMetric = metric;
        best = new SplineNetworkSample(
            metric <= 1.0,
            through,
            Math.Abs(through),
            Math.Abs(across),
            start.AlongRatio + (end.AlongRatio - start.AlongRatio) * clamped,
            start.BranchIndex,
            start.IsTrunk,
            SplineReplacementBody.None);
    }

    private void GetSegmentCellRange(
        int segment,
        out int minCellX,
        out int maxCellX,
        out int minCellZ,
        out int maxCellZ)
    {
        SplineNode start = nodes[segmentNodes[segment]];
        SplineNode end = nodes[segmentNodes[segment] + 1];
        double radius = SegmentRadius(start, end);
        minCellX = CellIndex(Math.Min(start.X, end.X) - radius, gridSizeX);
        maxCellX = CellIndex(Math.Max(start.X, end.X) + radius, gridSizeX);
        minCellZ = CellIndex(Math.Min(start.Z, end.Z) - radius, gridSizeZ);
        maxCellZ = CellIndex(Math.Max(start.Z, end.Z) + radius, gridSizeZ);
    }

    private int CellIndex(double localOffset, int gridSize)
    {
        int cell = (int)Math.Floor((localOffset + reach) / CellSize);
        return Math.Clamp(cell, 0, gridSize - 1);
    }

    private static double SegmentRadius(in SplineNode start, in SplineNode end)
    {
        return Math.Max(
            Math.Max(start.HalfThickness, end.HalfThickness),
            Math.Max(start.HalfWidth, end.HalfWidth)) + 1.0;
    }

    private static void AddSegments(List<int> segments, int start, int count)
    {
        for (int index = 0; index < count - 1; index++)
        {
            segments.Add(start + index);
        }
    }

    private static void Recenter(List<SplineNode> nodes, int start, int count)
    {
        double sumX = 0;
        double sumY = 0;
        double sumZ = 0;
        for (int index = start; index < start + count; index++)
        {
            sumX += nodes[index].X;
            sumY += nodes[index].Y;
            sumZ += nodes[index].Z;
        }

        double centerX = sumX / count;
        double centerY = sumY / count;
        double centerZ = sumZ / count;
        for (int index = 0; index < nodes.Count; index++)
        {
            SplineNode node = nodes[index];
            nodes[index] = node with
            {
                X = node.X - centerX,
                Y = node.Y - centerY,
                Z = node.Z - centerZ
            };
        }
    }

    private static void MeasureExtent(List<SplineNode> nodes, out double horizontal, out double vertical)
    {
        horizontal = 0;
        vertical = 0;
        foreach (SplineNode node in nodes)
        {
            horizontal = Math.Max(horizontal, Math.Max(Math.Abs(node.X), Math.Abs(node.Z)));
            vertical = Math.Max(vertical, Math.Abs(node.Y));
        }
    }

    private static int ClipToBounds(
        List<SplineNode> nodes,
        int start,
        int count,
        double horizontalLimit,
        double verticalLimit)
    {
        int kept = 0;
        while (kept < count)
        {
            SplineNode node = nodes[start + kept];
            if (Math.Abs(node.X) > horizontalLimit
                || Math.Abs(node.Z) > horizontalLimit
                || Math.Abs(node.Y) > verticalLimit)
            {
                break;
            }
            kept++;
        }

        if (kept < count) nodes.RemoveRange(start + kept, count - kept);
        return kept;
    }

    /// <summary>
    /// Coarse replacement bodies: one broad pollucite dome in the upper intermediate zone plus
    /// lepidolite blankets at splay junctions, each an ellipsoid oriented in the local sheet frame.
    /// </summary>
    private static ReplacementPod[] BuildPods(
        SplineNode[] nodes,
        List<(int Start, int Count)> branchRanges,
        SplineNetworkDefinition settings,
        ref ProceduralDepositRandom random)
    {
        var pods = new List<ReplacementPod>(4);
        (int trunkStart, int trunkCount) = branchRanges[0];
        double length = settings.TrunkLength;

        pods.Add(CreatePod(
            nodes,
            trunkStart,
            trunkCount,
            0.72 + random.Range(-0.05, 0.05),
            settings.Thickness * 0.24,
            length * 0.13,
            settings.Thickness * 0.51,
            settings.Width * 0.28,
            SplineReplacementBody.Pollucite));

        int lensCount = random.NextInt(settings.LepidoliteLensMin, settings.LepidoliteLensMax);
        double[] lensFractions = { 0.35, 0.48, 0.88 };
        for (int index = 0; index < lensCount; index++)
        {
            (int rangeStart, int rangeCount) = branchRanges[Math.Min(index, branchRanges.Count - 1)];
            double scale = index == 0 ? 1.0 : 0.82;
            pods.Add(CreatePod(
                nodes,
                rangeStart,
                rangeCount,
                lensFractions[index % lensFractions.Length] + random.Range(-0.05, 0.05),
                settings.Thickness * 0.21,
                length * 0.18 * scale,
                settings.Thickness * 0.49 * scale,
                settings.Width * 0.33 * scale,
                SplineReplacementBody.Lepidolite));
        }

        return pods.ToArray();
    }

    private static ReplacementPod CreatePod(
        SplineNode[] nodes,
        int start,
        int count,
        double fraction,
        double normalOffset,
        double radiusLength,
        double radiusThrough,
        double radiusAcross,
        SplineReplacementBody body)
    {
        SplineNode node = nodes[start + Math.Clamp(
            (int)Math.Round(Math.Clamp(fraction, 0, 1) * (count - 1)),
            0,
            count - 1)];
        return new ReplacementPod(
            node.X + node.NormalX * normalOffset,
            node.Y + node.NormalY * normalOffset,
            node.Z + node.NormalZ * normalOffset,
            node.TangentX,
            node.TangentY,
            node.TangentZ,
            node.NormalX,
            node.NormalY,
            node.NormalZ,
            node.BinormalX,
            node.BinormalY,
            node.BinormalZ,
            Math.Max(1.0, radiusLength),
            Math.Max(0.8, radiusThrough),
            Math.Max(1.0, radiusAcross),
            body);
    }

    /// <summary>
    /// Heading/pitch walk from the deposit model: the sheet climbs at its upward slope while
    /// azimuth and pitch wander by at most a few degrees per step, giving a curved dike rather than
    /// a straight slab.
    /// </summary>
    private static void BuildBranch(
        List<SplineNode> nodes,
        double originX,
        double originY,
        double originZ,
        double heading,
        double pitch,
        double length,
        double thickness,
        double width,
        double roll,
        double seed,
        SplineNetworkDefinition settings,
        double spacing,
        int branchIndex,
        bool isTrunk)
    {
        int steps = Math.Max(4, (int)Math.Round(length / spacing));
        double step = length / steps;
        double maxStep = settings.MaxStepAngleDeg * Math.PI / 180.0;
        var positions = new List<(double X, double Y, double Z, double Fraction)>(steps + 1)
        {
            (originX, originY, originZ, 0)
        };

        double currentX = originX;
        double currentY = originY;
        double currentZ = originZ;
        for (int index = 1; index <= steps; index++)
        {
            double fraction = (double)index / steps;
            heading += (Math.Sin(fraction * Math.PI * 4.2 + seed) * 0.7
                + Math.Sin(fraction * Math.PI * 9.1 + seed * 1.7) * 0.3) * maxStep;
            pitch += (Math.Cos(fraction * Math.PI * 3.8 + seed * 1.3) * 0.75
                + Math.Sin(fraction * Math.PI * 7.7 + seed * 2.1) * 0.25) * maxStep;

            double cosPitch = Math.Cos(pitch);
            currentX += step * cosPitch * Math.Cos(heading);
            currentY += step * Math.Sin(pitch);
            currentZ += step * cosPitch * Math.Sin(heading);
            positions.Add((currentX, currentY, currentZ, fraction));
        }

        double midScale = (settings.PinchScaleMin + settings.PinchScaleMax) * 0.5;
        double amplitude = (settings.PinchScaleMax - settings.PinchScaleMin) * 0.5;

        for (int index = 0; index < positions.Count; index++)
        {
            (double x, double y, double z, double fraction) = positions[index];
            int aheadIndex = Math.Min(index + 1, positions.Count - 1);
            int behindIndex = Math.Max(index - 1, 0);
            double tangentX = positions[aheadIndex].X - positions[behindIndex].X;
            double tangentY = positions[aheadIndex].Y - positions[behindIndex].Y;
            double tangentZ = positions[aheadIndex].Z - positions[behindIndex].Z;
            double tangentScale = Math.Sqrt(
                tangentX * tangentX + tangentY * tangentY + tangentZ * tangentZ);
            if (tangentScale <= 1e-9) continue;
            tangentX /= tangentScale;
            tangentY /= tangentScale;
            tangentZ /= tangentScale;

            // Width runs horizontally across the sheet, thickness through it: a broad tabular dike.
            double binormalX = tangentZ;
            double binormalY = 0;
            double binormalZ = -tangentX;
            double binormalScale = Math.Sqrt(binormalX * binormalX + binormalZ * binormalZ);
            if (binormalScale <= 1e-6)
            {
                binormalX = 0;
                binormalY = 0;
                binormalZ = 1;
                binormalScale = 1;
            }
            binormalX /= binormalScale;
            binormalZ /= binormalScale;

            Cross(binormalX, binormalY, binormalZ, tangentX, tangentY, tangentZ,
                out double normalX, out double normalY, out double normalZ);
            double normalScale = Math.Sqrt(normalX * normalX + normalY * normalY + normalZ * normalZ);
            if (normalScale <= 1e-6) continue;
            normalX /= normalScale;
            normalY /= normalScale;
            normalZ /= normalScale;

            double cos = Math.Cos(roll);
            double sin = Math.Sin(roll);
            double rolledNormalX = normalX * cos + binormalX * sin;
            double rolledNormalY = normalY * cos + binormalY * sin;
            double rolledNormalZ = normalZ * cos + binormalZ * sin;
            double rolledBinormalX = binormalX * cos - normalX * sin;
            double rolledBinormalY = binormalY * cos - normalY * sin;
            double rolledBinormalZ = binormalZ * cos - normalZ * sin;

            double thickOscillation = Math.Sin(fraction * Math.PI * 5.0 + seed) * 0.6
                + Math.Cos(fraction * Math.PI * 11.0 + seed * 1.5) * 0.4;
            double widthOscillation = Math.Cos(fraction * Math.PI * 4.0 + seed * 1.2) * 0.6
                + Math.Sin(fraction * Math.PI * 9.0 + seed * 0.7) * 0.4;
            double tipTaper = isTrunk
                ? 1.0
                : Math.Max(0.05, Math.Sqrt(Math.Max(0, 1.0 - fraction * fraction)));
            double scaleThickness = (midScale + thickOscillation * amplitude) * tipTaper;
            double scaleWidth = (midScale + widthOscillation * amplitude) * tipTaper;

            nodes.Add(new SplineNode(
                x,
                y,
                z,
                tangentX,
                tangentY,
                tangentZ,
                rolledNormalX,
                rolledNormalY,
                rolledNormalZ,
                rolledBinormalX,
                rolledBinormalY,
                rolledBinormalZ,
                Math.Max(0.4, thickness * 0.5 * scaleThickness),
                Math.Max(0.6, width * 0.5 * scaleWidth),
                fraction,
                branchIndex,
                isTrunk));
        }
    }

    private static void Cross(
        double ax,
        double ay,
        double az,
        double bx,
        double by,
        double bz,
        out double x,
        out double y,
        out double z)
    {
        x = ay * bz - az * by;
        y = az * bx - ax * bz;
        z = ax * by - ay * bx;
    }

    private readonly record struct SplineNode(
        double X,
        double Y,
        double Z,
        double TangentX,
        double TangentY,
        double TangentZ,
        double NormalX,
        double NormalY,
        double NormalZ,
        double BinormalX,
        double BinormalY,
        double BinormalZ,
        double HalfThickness,
        double HalfWidth,
        double AlongRatio,
        int BranchIndex,
        bool IsTrunk);

    private readonly record struct ReplacementPod(
        double X,
        double Y,
        double Z,
        double TangentX,
        double TangentY,
        double TangentZ,
        double NormalX,
        double NormalY,
        double NormalZ,
        double BinormalX,
        double BinormalY,
        double BinormalZ,
        double RadiusLength,
        double RadiusThrough,
        double RadiusAcross,
        SplineReplacementBody Body);
}
