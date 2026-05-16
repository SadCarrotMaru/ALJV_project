using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class BoardManager : MonoBehaviour, IStructureBoard
{
    [Header("Tilemaps")]
    [SerializeField] private Tilemap groundTilemap;
    [SerializeField] private Tilemap structureTilemap;
    [SerializeField] private Tilemap previewTilemap;

    [Header("Structure Visual Tiles")]
    [SerializeField] private TileBase woodProducerTile;
    [SerializeField] private TileBase brickProducerTile;
    [SerializeField] private TileBase steelProducerTile;

    [SerializeField] private TileBase woodCollectorTile;
    [SerializeField] private TileBase brickCollectorTile;
    [SerializeField] private TileBase steelCollectorTile;

    [SerializeField] private TileBase assemblyLineTile;
    [SerializeField] private TileBase converterTile;
    [SerializeField] private TileBase observerTile;
    [SerializeField] private TileBase magicalConverterTile;
    [SerializeField] private TileBase delayerTile;
    [SerializeField] private TileBase retrieverTile;

    [SerializeField] private TileBase previewValidTile;
    [SerializeField] private TileBase previewInvalidTile;

    [Header("AI / Playable Area")]
    [SerializeField] private bool usePlayableAreaForAI = true;
    [SerializeField] private Vector3Int playableMinCell = new Vector3Int(-6, 4, 0);
    [SerializeField] private Vector3Int playableMaxCell = new Vector3Int(5, -4, 0);

    private readonly Dictionary<Vector3Int, Tilez> tiles = new Dictionary<Vector3Int, Tilez>();
    private readonly List<Structure> activeStructures = new List<Structure>();

    private Vector3Int? lastPreviewCell;

    private static readonly Vector3Int[] CardinalOffsets =
    {
        Vector3Int.up,
        Vector3Int.right,
        Vector3Int.down,
        Vector3Int.left
    };

    public IReadOnlyList<Structure> ActiveStructures => activeStructures;

    public bool IsValidBuildCell(Vector3Int cell)
    {
        return groundTilemap != null && groundTilemap.HasTile(cell);
    }

    public Tilez GetOrCreateTilez(Vector3Int cell)
    {
        if (!tiles.TryGetValue(cell, out Tilez tilez))
        {
            tilez = new Tilez(cell);
            tiles[cell] = tilez;
        }

        return tilez;
    }

    public Structure GetStructureAt(Vector3Int cell)
    {
        if (!tiles.TryGetValue(cell, out Tilez tilez))
            return null;

        return tilez.PlacedStructure;
    }

    public bool HasStructureAt(Vector3Int cell)
    {
        return GetStructureAt(cell) != null;
    }

    public bool TryPlaceStructure(Vector3Int cell, Structure structure)
    {
        if (structure == null)
            return false;

        if (!IsValidBuildCell(cell))
            return false;

        Tilez tilez = GetOrCreateTilez(cell);

        if (!tilez.TryPlaceStructure(structure))
            return false;

        structure.SetBoard(this);

        activeStructures.Add(structure);

        RefreshConnectionsAround(cell);
        RedrawCell(cell);

        return true;
    }

    public bool IsWithinPlayableArea(Vector3Int cell)
    {
        if (!usePlayableAreaForAI)
            return true;

        return cell.x >= playableMinCell.x &&
               cell.x <= playableMaxCell.x &&
               cell.y >= playableMinCell.y &&
               cell.y <= playableMaxCell.y;
    }

    public Vector3Int GetPlayableOriginCell()
    {
        return playableMinCell;
    }

    public Vector2Int GetPlayableSize()
    {
        return new Vector2Int(
            playableMaxCell.x - playableMinCell.x + 1,
            playableMaxCell.y - playableMinCell.y + 1
        );
    }

    public List<Vector3Int> GetAllBuildableCells(bool playableOnly = true)
    {
        List<Vector3Int> cells = new List<Vector3Int>();

        if (groundTilemap == null)
            return cells;

        BoundsInt bounds = groundTilemap.cellBounds;

        foreach (Vector3Int cell in bounds.allPositionsWithin)
        {
            if (!groundTilemap.HasTile(cell))
                continue;

            if (playableOnly && !IsWithinPlayableArea(cell))
                continue;

            cells.Add(cell);
        }

        return cells;
    }

    public bool TryRemoveStructure(Vector3Int cell)
    {
        if (!tiles.TryGetValue(cell, out Tilez tilez))
            return false;

        if (!tilez.IsOccupied)
            return false;

        Structure removedStructure = tilez.PlacedStructure;

        activeStructures.Remove(removedStructure);
        tilez.RemoveStructure();

        RefreshConnectionsAround(cell);
        RedrawCell(cell);

        return true;
    }

    public void RefreshConnectionsAround(Vector3Int center)
    {
        Vector3Int[] offsets =
        {
            Vector3Int.zero,
            Vector3Int.up,
            Vector3Int.right,
            Vector3Int.down,
            Vector3Int.left
        };

        foreach (Vector3Int offset in offsets)
        {
            Vector3Int pos = center + offset;
            Structure structure = GetStructureAt(pos);

            if (structure != null)
            {
                structure.SetBoard(this);
                structure.RebuildConnections(this);
            }
        }
    }

    public void ShowPreview(Vector3Int cell, bool valid)
    {
        if (previewTilemap == null)
            return;

        if (lastPreviewCell.HasValue && lastPreviewCell.Value != cell)
        {
            previewTilemap.SetTile(lastPreviewCell.Value, null);
        }

        if (!IsValidBuildCell(cell))
        {
            lastPreviewCell = null;
            return;
        }

        previewTilemap.SetTile(cell, valid ? previewValidTile : previewInvalidTile);
        lastPreviewCell = cell;
    }

    public void ClearPreview()
    {
        if (previewTilemap == null)
            return;

        if (lastPreviewCell.HasValue)
        {
            previewTilemap.SetTile(lastPreviewCell.Value, null);
            lastPreviewCell = null;
        }
    }

    public Vector3 GetCellCenterWorld(Vector3Int cell)
    {
        if (groundTilemap == null)
            return Vector3.zero;

        return groundTilemap.GetCellCenterWorld(cell);
    }

    public Vector3Int WorldToCell(Vector3 world)
    {
        if (groundTilemap == null)
            return Vector3Int.zero;

        return groundTilemap.WorldToCell(world);
    }

    public void RedrawCell(Vector3Int cell)
    {
        if (structureTilemap == null)
            return;

        Structure structure = GetStructureAt(cell);
        structureTilemap.SetTile(cell, GetTileForStructure(structure));
    }

    public void RedrawAllStructures()
    {
        if (structureTilemap == null)
            return;

        structureTilemap.ClearAllTiles();

        foreach (KeyValuePair<Vector3Int, Tilez> kvp in tiles)
        {
            if (kvp.Value != null && kvp.Value.PlacedStructure != null)
            {
                structureTilemap.SetTile(kvp.Key, GetTileForStructure(kvp.Value.PlacedStructure));
            }
        }
    }

    public List<Structure> GetAdjacentStructures(Vector3Int cell)
    {
        List<Structure> neighbors = new List<Structure>();

        for (int i = 0; i < CardinalOffsets.Length; i++)
        {
            Vector3Int neighborCell = cell + CardinalOffsets[i];
            Structure structure = GetStructureAt(neighborCell);

            if (structure != null)
                neighbors.Add(structure);
        }

        return neighbors;
    }

    public Structure GetBestNextHopForLine(Vector3Int startLineCell, ResourceType resource, int quantity)
    {
        Structure startStructure = GetStructureAt(startLineCell);
        if (!(startStructure is AssemblyLineStructure))
            return null;

        Queue<Vector3Int> queue = new Queue<Vector3Int>();
        HashSet<Vector3Int> visited = new HashSet<Vector3Int>();
        Dictionary<Vector3Int, Vector3Int> parent = new Dictionary<Vector3Int, Vector3Int>();
        Dictionary<Vector3Int, int> distance = new Dictionary<Vector3Int, int>();

        queue.Enqueue(startLineCell);
        visited.Add(startLineCell);
        distance[startLineCell] = 0;

        Structure bestTarget = null;
        Vector3Int bestAdjacentLine = Vector3Int.zero;
        int bestDistance = int.MaxValue;
        int bestPendingLoad = int.MaxValue;

        while (queue.Count > 0)
        {
            Vector3Int current = queue.Dequeue();
            int currentDistance = distance[current];

            for (int i = 0; i < CardinalOffsets.Length; i++)
            {
                Vector3Int neighborCell = current + CardinalOffsets[i];
                Structure neighbor = GetStructureAt(neighborCell);

                if (neighbor == null)
                    continue;

                ResourceOperation probe = new ResourceOperation(null, resource, quantity);

                if (neighbor is CollectorStructure collector)
                {
                    if (collector.AcceptedResource != resource)
                        continue;

                    if (!collector.CanReceive(probe))
                        continue;

                    int candidateDistance = currentDistance + 1;
                    int candidateLoad = collector.GetPendingQueuedUnits();

                    bool better =
                        candidateDistance < bestDistance ||
                        (candidateDistance == bestDistance && candidateLoad < bestPendingLoad);

                    if (better)
                    {
                        bestTarget = collector;
                        bestAdjacentLine = current;
                        bestDistance = candidateDistance;
                        bestPendingLoad = candidateLoad;
                    }
                }
                else if (neighbor is ConverterStructure converter)
                {
                    if (!converter.CanReceive(probe))
                        continue;

                    int candidateDistance = currentDistance + 1;
                    int candidateLoad = converter.GetPendingInputUnits();

                    bool better =
                        candidateDistance < bestDistance ||
                        (candidateDistance == bestDistance && candidateLoad < bestPendingLoad);

                    if (better)
                    {
                        bestTarget = converter;
                        bestAdjacentLine = current;
                        bestDistance = candidateDistance;
                        bestPendingLoad = candidateLoad;
                    }
                }
                else if (neighbor is AssemblyLineStructure)
                {
                    if (visited.Contains(neighborCell))
                        continue;

                    visited.Add(neighborCell);
                    parent[neighborCell] = current;
                    distance[neighborCell] = currentDistance + 1;
                    queue.Enqueue(neighborCell);
                }
            }
        }

        if (bestTarget == null)
            return null;

        if (bestAdjacentLine == startLineCell)
            return bestTarget;

        Vector3Int step = bestAdjacentLine;

        while (parent.ContainsKey(step) && parent[step] != startLineCell)
        {
            step = parent[step];
        }

        return GetStructureAt(step);
    }

    public void ClearAllStructures()
    {
        foreach (var kvp in tiles)
        {
            if (kvp.Value != null)
            {
                kvp.Value.RemoveStructure();
            }
        }

        activeStructures.Clear();

        if (structureTilemap != null)
        {
            structureTilemap.ClearAllTiles();
        }

        ClearPreview();
    }

    public void ClearAllStructuresToInventory(PlayerInventory playerInventory)
    {
        Dictionary<ResourceType, int> recovered = new Dictionary<ResourceType, int>
        {
            [ResourceType.Wood] = 0,
            [ResourceType.Brick] = 0,
            [ResourceType.Steel] = 0,
            [ResourceType.Psi] = 0
        };

        Dictionary<StructureDefinition, int> refundedStructures = new Dictionary<StructureDefinition, int>();

        for (int i = 0; i < activeStructures.Count; i++)
        {
            Structure structure = activeStructures[i];
            if (structure == null)
                continue;

            structure.DrainAllResourcesTo(recovered);

            if (structure.OriginDefinition != null)
            {
                if (!refundedStructures.ContainsKey(structure.OriginDefinition))
                    refundedStructures[structure.OriginDefinition] = 0;

                refundedStructures[structure.OriginDefinition] += 1;
            }
        }

        if (playerInventory != null)
        {
            foreach (var kvp in recovered)
            {
                if (kvp.Key == ResourceType.None || kvp.Value <= 0)
                    continue;

                playerInventory.AddResource(kvp.Key, kvp.Value);
            }

            foreach (var kvp in refundedStructures)
            {
                if (kvp.Key == null || kvp.Value <= 0)
                    continue;

                playerInventory.AddStructure(kvp.Key, kvp.Value);
            }
        }

        foreach (var kvp in tiles)
        {
            if (kvp.Value != null)
            {
                kvp.Value.RemoveStructure();
            }
        }

        activeStructures.Clear();

        if (structureTilemap != null)
            structureTilemap.ClearAllTiles();

        ClearPreview();
    }

    private TileBase GetTileForStructure(Structure structure)
    {
        if (structure == null)
            return null;

        if (structure is ProducerStructure producer)
        {
            switch (producer.ProducedResource)
            {
                case ResourceType.Wood:
                    return woodProducerTile;
                case ResourceType.Brick:
                    return brickProducerTile;
                case ResourceType.Steel:
                    return steelProducerTile;
                default:
                    return woodProducerTile;
            }
        }

        if (structure is CollectorStructure collector)
        {
            switch (collector.AcceptedResource)
            {
                case ResourceType.Wood:
                    return woodCollectorTile;
                case ResourceType.Brick:
                    return brickCollectorTile;
                case ResourceType.Steel:
                    return steelCollectorTile;
                default:
                    return woodCollectorTile;
            }
        }

        if (structure is AssemblyLineStructure)
            return assemblyLineTile;

        if (structure is ConverterStructure)
            return converterTile;

        if (structure is ObserverStructure)
            return observerTile;

        if (structure is MagicalConverterStructure)
            return magicalConverterTile;

        if (structure is DelayerStructure)
            return delayerTile;

        if (structure is RetrieverStructure)
            return retrieverTile;

        return null;
    }
    public List<Vector3Int> GetAllBuildableCells()
    {
        List<Vector3Int> cells = new List<Vector3Int>();

        if (groundTilemap == null)
            return cells;

        BoundsInt bounds = groundTilemap.cellBounds;

        foreach (Vector3Int cell in bounds.allPositionsWithin)
        {
            if (groundTilemap.HasTile(cell))
                cells.Add(cell);
        }

        return cells;
    }
}