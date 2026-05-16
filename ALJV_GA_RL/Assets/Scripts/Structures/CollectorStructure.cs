using System.Collections.Generic;
using UnityEngine;

public class CollectorStructure : Structure
{
    private readonly ResourceType acceptedResource;
    private readonly int baseMaxQueuedUnits;
    private readonly int baseMaxIntakePerTick;

    public override bool HasDirectionalOutput => false;
    public override bool SupportsRotation => false;

    public ResourceType AcceptedResource => acceptedResource;

    public CollectorStructure(ResourceType acceptedResource, int maxQueuedUnits = 12, int maxIntakePerTick = 4)
    {
        structureType = StructureType.Collector;
        this.acceptedResource = acceptedResource;
        baseMaxQueuedUnits = Mathf.Max(1, maxQueuedUnits);
        baseMaxIntakePerTick = Mathf.Max(1, maxIntakePerTick);
    }

    public override bool CanReceive(ResourceOperation operation)
    {
        if (operation == null || operation.Quantity <= 0)
            return false;

        if (operation.Resource != acceptedResource)
            return false;

        int pending = GetPendingQueuedUnits();
        return pending + operation.Quantity <= GetMaxQueuedUnits();
    }

    protected override void ProcessIncomingOperations()
    {
        int remainingIntake = GetMaxIntakePerTick();
        int originalCount = incomingOperations.Count;

        for (int i = 0; i < originalCount && remainingIntake > 0; i++)
        {
            ResourceOperation operation = incomingOperations.Dequeue();

            if (operation == null || operation.Quantity <= 0)
                continue;

            if (operation.Quantity <= remainingIntake)
            {
                HandleIncomingOperation(operation);
                remainingIntake -= operation.Quantity;
            }
            else
            {
                ResourceOperation partial = new ResourceOperation(operation.Source, operation.Resource, remainingIntake);
                HandleIncomingOperation(partial);

                operation.Quantity -= remainingIntake;
                remainingIntake = 0;
                incomingOperations.Enqueue(operation);
            }
        }
    }

    protected override void HandleIncomingOperation(ResourceOperation operation)
    {
        AddStoredResource(operation.Resource, operation.Quantity);
    }

    public int Take(ResourceType resource, int amount)
    {
        if (resource != acceptedResource || amount <= 0)
            return 0;

        int available = GetStoredResource(resource);
        int taken = Mathf.Min(available, amount);

        if (taken > 0)
        {
            ConsumeStoredResource(resource, taken);
        }

        return taken;
    }

    public int GetPendingQueuedUnits()
    {
        int total = 0;

        foreach (ResourceOperation op in incomingOperations)
        {
            if (op != null && op.Resource == acceptedResource)
                total += op.Quantity;
        }

        return total;
    }

    public int GetMaxQueuedUnits()
    {
        return baseMaxQueuedUnits + (level - 1) * 6;
    }

    public int GetMaxIntakePerTick()
    {
        return baseMaxIntakePerTick + (level - 1) * 2;
    }

    protected override void AppendDetailLines(List<string> lines)
    {
        lines.Add($"Collects: {acceptedResource}");
        lines.Add($"Stored: {GetStoredResource(acceptedResource)}");
        lines.Add($"Queued: {GetPendingQueuedUnits()}/{GetMaxQueuedUnits()}");
        lines.Add($"Intake / tick: {GetMaxIntakePerTick()}");
    }
}