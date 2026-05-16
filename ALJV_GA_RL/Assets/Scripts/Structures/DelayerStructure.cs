using System.Collections.Generic;
using UnityEngine;

public class DelayerStructure : Structure
{
    private float intervalSeconds;
    private readonly int maxBufferedOperations;
    private readonly Queue<ResourceOperation> delayedQueue = new Queue<ResourceOperation>();
    private float timer;

    public DelayerStructure(float intervalSeconds = 1f, int maxBufferedOperations = 10)
    {
        structureType = StructureType.Delayer;
        this.intervalSeconds = Mathf.Max(0.05f, intervalSeconds);
        this.maxBufferedOperations = maxBufferedOperations;
    }

    protected override void HandleIncomingOperation(ResourceOperation operation)
    {
        if (delayedQueue.Count >= maxBufferedOperations)
        {
            DiscardOperation(operation);
            return;
        }

        delayedQueue.Enqueue(operation);
    }

    public float GetEffectiveInterval()
    {
        return Mathf.Max(0.05f, intervalSeconds - (level - 1) * 0.15f);
    }

    protected override void OnTick(float deltaTime)
    {
        if (delayedQueue.Count == 0)
            return;

        timer += deltaTime;
        if (timer < GetEffectiveInterval())
            return;

        timer = 0f;

        ResourceOperation operation = delayedQueue.Dequeue();
        bool sent = outputs.Count > 0 && TrySend(operation, 0);

        if (!sent)
        {
            DiscardOperation(operation);
        }
    }

    protected override void AppendDetailLines(List<string> lines)
    {
        lines.Add($"Buffered ops: {delayedQueue.Count}");
        lines.Add($"Interval: {GetEffectiveInterval():0.##}s");
    }
    protected override void DrainAdditionalResourcesTo(Dictionary<ResourceType, int> sink)
    {
        while (delayedQueue.Count > 0)
        {
            ResourceOperation op = delayedQueue.Dequeue();
            if (op == null || op.Resource == ResourceType.None || op.Quantity <= 0)
                continue;

            if (!sink.ContainsKey(op.Resource))
                sink[op.Resource] = 0;

            sink[op.Resource] += op.Quantity;
        }
    }
}