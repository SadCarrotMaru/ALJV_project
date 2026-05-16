using System.Collections.Generic;
using UnityEngine;

public static class PlacementHeuristics
{
    private static readonly Vector3Int[] Cardinal =
    {
        Vector3Int.up,
        Vector3Int.right,
        Vector3Int.down,
        Vector3Int.left
    };

    public static bool TryFindPlacement(PlayerRuntime runtime, StructureDefinition definition, out Vector3Int cell, out Direction direction)
    {
        cell = Vector3Int.zero;
        direction = Direction.Right;

        if (runtime == null || runtime.Board == null || definition == null)
            return false;

        List<Vector3Int> buildable = runtime.Board.GetAllBuildableCells(true);

        switch (definition.StructureType)
        {
            case StructureType.AssemblyLine:
                if (TryFindAssemblyLinePlacement(runtime, buildable, out cell))
                {
                    direction = Direction.Right;
                    return true;
                }
                break;

            case StructureType.Producer:
                if (TryFindProducerPlacement(runtime, definition, buildable, out cell, out direction))
                    return true;
                break;

            case StructureType.Collector:
                if (TryFindCollectorPlacement(runtime, definition, buildable, out cell))
                {
                    direction = Direction.Right;
                    return true;
                }
                break;

            default:
                if (TryFindCellAdjacentToLine(runtime, buildable, out cell))
                {
                    direction = Direction.Right;
                    return true;
                }
                break;
        }

        return false;
    }

    private static bool TryFindAssemblyLinePlacement(PlayerRuntime runtime, List<Vector3Int> cells, out Vector3Int result)
    {
        var active = runtime.Board.ActiveStructures;
        for (int i = 0; i < active.Count; i++)
        {
            if (active[i] is ProducerStructure producer && producer.OwnerCell.HasValue)
            {
                Vector3Int forward = producer.OwnerCell.Value + (Vector3Int)producer.OutputDirection.ToVector2Int();

                if (runtime.Board.IsWithinPlayableArea(forward) &&
                    runtime.Board.IsValidBuildCell(forward) &&
                    !runtime.Board.HasStructureAt(forward))
                {
                    result = forward;
                    return true;
                }
            }
        }

        for (int i = 0; i < active.Count; i++)
        {
            if (active[i] is CollectorStructure collector && collector.OwnerCell.HasValue)
            {
                bool alreadyConnectedToLine = false;
                foreach (var offset in Cardinal)
                {
                    Structure neighbor = runtime.Board.GetStructureAt(collector.OwnerCell.Value + offset);
                    if (neighbor is AssemblyLineStructure)
                    {
                        alreadyConnectedToLine = true;
                        break;
                    }
                }

                if (alreadyConnectedToLine)
                    continue;

                foreach (var offset in Cardinal)
                {
                    Vector3Int candidate = collector.OwnerCell.Value + offset;
                    if (runtime.Board.IsWithinPlayableArea(candidate) &&
                        runtime.Board.IsValidBuildCell(candidate) &&
                        !runtime.Board.HasStructureAt(candidate))
                    {
                        result = candidate;
                        return true;
                    }
                }
            }
        }

        for (int i = 0; i < active.Count; i++)
        {
            if (active[i] is AssemblyLineStructure line && line.OwnerCell.HasValue)
            {
                foreach (var offset in Cardinal)
                {
                    Vector3Int candidate = line.OwnerCell.Value + offset;
                    if (runtime.Board.IsWithinPlayableArea(candidate) &&
                        runtime.Board.IsValidBuildCell(candidate) &&
                        !runtime.Board.HasStructureAt(candidate))
                    {
                        result = candidate;
                        return true;
                    }
                }
            }
        }

        result = Vector3Int.zero;
        return false;
    }

    private static bool TryFindProducerPlacement(PlayerRuntime runtime, StructureDefinition definition, List<Vector3Int> cells, out Vector3Int result, out Direction direction)
    {
        for (int i = 0; i < cells.Count; i++)
        {
            Vector3Int c = cells[i];
            if (runtime.Board.HasStructureAt(c))
                continue;

            for (int d = 0; d < Cardinal.Length; d++)
            {
                Vector3Int neighbor = c + Cardinal[d];
                Structure s = runtime.Board.GetStructureAt(neighbor);

                if (s is AssemblyLineStructure)
                {
                    result = c;
                    direction = ToDirection(Cardinal[d]);
                    return true;
                }
            }
        }

        Vector3Int center = GetPlayableCenter(runtime);
        Vector3Int best = Vector3Int.zero;
        float bestDist = float.MaxValue;
        bool found = false;

        for (int i = 0; i < cells.Count; i++)
        {
            Vector3Int c = cells[i];
            if (runtime.Board.HasStructureAt(c))
                continue;

            float dist = Vector3Int.Distance(c, center);
            if (dist < bestDist)
            {
                bestDist = dist;
                best = c;
                found = true;
            }
        }

        result = best;
        direction = Direction.Right;
        return found;
    }

    private static bool TryFindCollectorPlacement(PlayerRuntime runtime, StructureDefinition definition, List<Vector3Int> cells, out Vector3Int result)
    {
        if (TryFindCellAdjacentToLine(runtime, cells, out result))
            return true;

        result = Vector3Int.zero;
        return false;
    }

    private static bool TryFindCellAdjacentToLine(PlayerRuntime runtime, List<Vector3Int> cells, out Vector3Int result)
    {
        for (int i = 0; i < cells.Count; i++)
        {
            Vector3Int c = cells[i];
            if (runtime.Board.HasStructureAt(c))
                continue;

            for (int d = 0; d < Cardinal.Length; d++)
            {
                Structure neighbor = runtime.Board.GetStructureAt(c + Cardinal[d]);
                if (neighbor is AssemblyLineStructure)
                {
                    result = c;
                    return true;
                }
            }
        }

        result = Vector3Int.zero;
        return false;
    }

    private static Direction ToDirection(Vector3Int offset)
    {
        if (offset == Vector3Int.up) return Direction.Up;
        if (offset == Vector3Int.right) return Direction.Right;
        if (offset == Vector3Int.down) return Direction.Down;
        return Direction.Left;
    }

    private static Vector3Int GetPlayableCenter(PlayerRuntime runtime)
    {
        Vector3Int min = runtime.Board.GetPlayableOriginCell();
        Vector2Int size = runtime.Board.GetPlayableSize();

        return new Vector3Int(
            min.x + size.x / 2,
            min.y + size.y / 2,
            0
        );
    }
}