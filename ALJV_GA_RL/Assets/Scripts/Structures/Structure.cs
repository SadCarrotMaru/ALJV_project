using System;
using System.Collections.Generic;
using UnityEngine;

public abstract class Structure
{
    protected StructureType structureType;
    protected int level = 1;
    protected Vector3Int? ownerCell;
    protected Direction outputDirection = Direction.Right;
    protected StructureDefinition originDefinition;
    protected BoardManager ownerBoard;

    protected readonly Queue<ResourceOperation> incomingOperations = new Queue<ResourceOperation>();
    protected readonly List<Structure> outputs = new List<Structure>();
    protected readonly Dictionary<ResourceType, int> storedResources = new Dictionary<ResourceType, int>();

    public StructureType Type => structureType;
    public int Level => level;
    public Vector3Int? OwnerCell => ownerCell;
    public Direction OutputDirection => outputDirection;
    public IReadOnlyList<Structure> Outputs => outputs;
    public StructureDefinition OriginDefinition => originDefinition;
    public BoardManager OwnerBoard => ownerBoard;

    public virtual bool HasDirectionalOutput => true;
    public virtual bool SupportsRotation => true;

    protected Structure()
    {
        foreach (ResourceType resource in Enum.GetValues(typeof(ResourceType)))
        {
            if (resource == ResourceType.None)
                continue;

            storedResources[resource] = 0;
        }
    }

    public void SetOriginDefinition(StructureDefinition definition)
    {
        originDefinition = definition;
    }

    public virtual void SetBoard(BoardManager board)
    {
        ownerBoard = board;
    }

    public virtual void SetCell(Vector3Int? cell)
    {
        ownerCell = cell;
    }

    public virtual void SetOutputDirection(Direction direction)
    {
        if (!SupportsRotation)
            return;

        outputDirection = direction;
    }

    public virtual void RotateClockwise()
    {
        if (!SupportsRotation)
            return;

        outputDirection = outputDirection.RotateClockwise();
    }

    public virtual void RebuildConnections(IStructureBoard board)
    {
        DisconnectAllOutputs();

        if (!HasDirectionalOutput || ownerCell == null || board == null)
            return;

        Vector3Int nextCell = ownerCell.Value + (Vector3Int)outputDirection.ToVector2Int();
        Structure next = board.GetStructureAt(nextCell);

        if (next != null)
        {
            ConnectOutput(next);
        }
    }

    public virtual void ConnectOutput(Structure target)
    {
        if (target == null || outputs.Contains(target))
            return;

        outputs.Add(target);
    }

    public virtual void DisconnectOutput(Structure target)
    {
        if (target == null)
            return;

        outputs.Remove(target);
    }

    public virtual void DisconnectAllOutputs()
    {
        outputs.Clear();
    }

    public virtual bool CanReceive(ResourceOperation operation)
    {
        return operation != null && operation.Quantity > 0;
    }

    public virtual bool EnqueueOperation(ResourceOperation operation)
    {
        if (!CanReceive(operation))
            return false;

        incomingOperations.Enqueue(operation);
        return true;
    }

    public virtual void SimulationTick(float deltaTime)
    {
        ProcessIncomingOperations();
        OnTick(deltaTime);
    }

    protected virtual void ProcessIncomingOperations()
    {
        int count = incomingOperations.Count;

        for (int i = 0; i < count; i++)
        {
            ResourceOperation operation = incomingOperations.Dequeue();
            HandleIncomingOperation(operation);
        }
    }

    protected virtual void HandleIncomingOperation(ResourceOperation operation)
    {
        AddStoredResource(operation.Resource, operation.Quantity);
    }

    protected virtual void OnTick(float deltaTime)
    {
    }

    public virtual void LevelUp()
    {
        level++;
    }

    public virtual int GetMaxLevel()
    {
        if (originDefinition == null)
            return 1;

        return Mathf.Max(1, originDefinition.MaxLevel);
    }

    public bool IsMaxLevel()
    {
        return level >= GetMaxLevel();
    }

    public virtual List<ResourceAmount> GetNextUpgradeCost()
    {
        if (originDefinition == null)
            return null;

        return originDefinition.GetUpgradeCostForTargetLevel(level + 1);
    }

    public virtual bool CanUpgrade(PlayerInventory inventory)
    {
        if (inventory == null)
            return false;

        if (IsMaxLevel())
            return false;

        List<ResourceAmount> cost = GetNextUpgradeCost();
        if (cost == null)
            return false;

        return inventory.CanAfford(cost);
    }

    public virtual bool TryUpgrade(PlayerInventory inventory)
    {
        if (!CanUpgrade(inventory))
            return false;

        List<ResourceAmount> cost = GetNextUpgradeCost();
        if (!inventory.SpendResources(cost))
            return false;

        level++;
        OnUpgraded();
        return true;
    }

    protected virtual void OnUpgraded()
    {
    }

    public virtual string GetDisplayName()
    {
        if (originDefinition != null && !string.IsNullOrWhiteSpace(originDefinition.DisplayName))
            return originDefinition.DisplayName;

        return structureType.ToString();
    }

    public virtual List<string> GetDetailLines()
    {
        List<string> lines = new List<string>
        {
            $"Level: {level}/{GetMaxLevel()}",
            $"Type: {structureType}"
        };

        AppendDetailLines(lines);
        return lines;
    }

    protected virtual void AppendDetailLines(List<string> lines)
    {
    }

    protected virtual bool TrySend(ResourceType resource, int quantity, int outputIndex = 0)
    {
        if (quantity <= 0)
            return false;

        if (outputIndex < 0 || outputIndex >= outputs.Count)
            return false;

        Structure target = outputs[outputIndex];
        if (target == null)
            return false;

        return target.EnqueueOperation(new ResourceOperation(this, resource, quantity));
    }

    protected virtual bool TrySend(ResourceOperation operation, int outputIndex = 0)
    {
        if (operation == null || operation.Quantity <= 0)
            return false;

        if (outputIndex < 0 || outputIndex >= outputs.Count)
            return false;

        Structure target = outputs[outputIndex];
        if (target == null)
            return false;

        return target.EnqueueOperation(operation);
    }

    protected bool TrySendToTarget(Structure target, ResourceOperation operation)
    {
        if (target == null || operation == null || operation.Quantity <= 0)
            return false;

        return target.EnqueueOperation(operation);
    }

    protected void AddStoredResource(ResourceType resource, int amount)
    {
        if (resource == ResourceType.None || amount <= 0)
            return;

        storedResources[resource] += amount;
    }

    protected bool ConsumeStoredResource(ResourceType resource, int amount)
    {
        if (resource == ResourceType.None || amount <= 0)
            return false;

        if (storedResources[resource] < amount)
            return false;

        storedResources[resource] -= amount;
        return true;
    }

    public int GetStoredResource(ResourceType resource)
    {
        if (resource == ResourceType.None)
            return 0;

        return storedResources.TryGetValue(resource, out int value) ? value : 0;
    }

    public virtual void DrainAllResourcesTo(Dictionary<ResourceType, int> sink)
    {
        if (sink == null)
            return;

        foreach (var kvp in storedResources)
        {
            if (kvp.Key == ResourceType.None || kvp.Value <= 0)
                continue;

            if (!sink.ContainsKey(kvp.Key))
                sink[kvp.Key] = 0;

            sink[kvp.Key] += kvp.Value;
        }

        List<ResourceType> keys = new List<ResourceType>(storedResources.Keys);
        for (int i = 0; i < keys.Count; i++)
        {
            storedResources[keys[i]] = 0;
        }

        while (incomingOperations.Count > 0)
        {
            ResourceOperation op = incomingOperations.Dequeue();
            if (op == null || op.Resource == ResourceType.None || op.Quantity <= 0)
                continue;

            if (!sink.ContainsKey(op.Resource))
                sink[op.Resource] = 0;

            sink[op.Resource] += op.Quantity;
        }

        DrainAdditionalResourcesTo(sink);
    }

    protected virtual void DrainAdditionalResourcesTo(Dictionary<ResourceType, int> sink)
    {
    }

    protected virtual void DiscardOperation(ResourceOperation operation)
    {
    }
}