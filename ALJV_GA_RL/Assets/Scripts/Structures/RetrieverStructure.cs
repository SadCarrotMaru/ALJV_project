using System.Collections.Generic;
using UnityEngine;

public class RetrieverStructure : Structure
{
    private CollectorStructure boundCollector;
    private readonly int pullAmount;
    private readonly float pullInterval;
    private float timer;

    public RetrieverStructure(CollectorStructure boundCollector = null, int pullAmount = 1, float pullInterval = 1f)
    {
        structureType = StructureType.Retriever;
        this.boundCollector = boundCollector;
        this.pullAmount = pullAmount;
        this.pullInterval = pullInterval;
    }

    public override bool CanReceive(ResourceOperation operation)
    {
        return false;
    }

    public int GetPullAmount()
    {
        return pullAmount + (level - 1);
    }

    public float GetPullInterval()
    {
        return Mathf.Max(0.1f, pullInterval - (level - 1) * 0.1f);
    }

    protected override void OnTick(float deltaTime)
    {
        if (boundCollector == null)
            return;

        timer += deltaTime;
        if (timer < GetPullInterval())
            return;

        timer = 0f;

        ResourceType resource = boundCollector.AcceptedResource;
        int taken = boundCollector.Take(resource, GetPullAmount());

        if (taken <= 0)
            return;

        bool sent = outputs.Count > 0 && TrySend(resource, taken, 0);
        if (!sent)
        {
            DiscardOperation(new ResourceOperation(this, resource, taken));
        }
    }

    protected override void AppendDetailLines(List<string> lines)
    {
        lines.Add($"Pull amount: {GetPullAmount()}");
        lines.Add($"Pull interval: {GetPullInterval():0.##}s");
        lines.Add($"Bound collector: {(boundCollector != null ? boundCollector.GetDisplayName() : "None")}");
    }

    public void BindCollector(CollectorStructure collector)
    {
        boundCollector = collector;
    }
}