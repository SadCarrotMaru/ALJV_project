using System.Collections.Generic;

public class ObserverStructure : Structure
{
    private readonly List<ObserverRule> rules;
    private readonly Queue<ResourceOperation> heldOperations = new Queue<ResourceOperation>();

    public ObserverStructure(List<ObserverRule> initialRules = null)
    {
        structureType = StructureType.Observer;
        rules = initialRules ?? new List<ObserverRule>();
    }

    protected override void HandleIncomingOperation(ResourceOperation operation)
    {
        ObserverRule matchingRule = GetRule(operation.Resource);

        if (matchingRule == null)
        {
            bool defaultSent = outputs.Count > 0 && TrySend(operation, 0);
            if (!defaultSent)
            {
                DiscardOperation(operation);
            }
            return;
        }

        switch (matchingRule.ActionType)
        {
            case ObserverActionType.RouteToOutput:
                {
                    bool sent = TrySend(operation, matchingRule.OutputIndex);
                    if (!sent)
                    {
                        DiscardOperation(operation);
                    }
                    break;
                }

            case ObserverActionType.Discard:
                {
                    DiscardOperation(operation);
                    break;
                }

            case ObserverActionType.Hold:
                {
                    heldOperations.Enqueue(operation);
                    break;
                }
        }
    }

    public void ReleaseHeldOperations(int outputIndex = 0, int maxCount = int.MaxValue)
    {
        int released = 0;

        while (heldOperations.Count > 0 && released < maxCount)
        {
            ResourceOperation operation = heldOperations.Dequeue();
            bool sent = TrySend(operation, outputIndex);

            if (!sent)
            {
                DiscardOperation(operation);
            }

            released++;
        }
    }

    public void SetRule(ResourceType resource, ObserverActionType actionType, int outputIndex)
    {
        ObserverRule existing = GetRule(resource);

        if (existing != null)
        {
            existing.ActionType = actionType;
            existing.OutputIndex = outputIndex;
            return;
        }

        rules.Add(new ObserverRule(resource, actionType, outputIndex));
    }

    private ObserverRule GetRule(ResourceType resource)
    {
        for (int i = 0; i < rules.Count; i++)
        {
            if (rules[i].Resource == resource)
            {
                return rules[i];
            }
        }

        return null;
    }
    protected override void DrainAdditionalResourcesTo(Dictionary<ResourceType, int> sink)
    {
        while (heldOperations.Count > 0)
        {
            ResourceOperation op = heldOperations.Dequeue();
            if (op == null || op.Resource == ResourceType.None || op.Quantity <= 0)
                continue;

            if (!sink.ContainsKey(op.Resource))
                sink[op.Resource] = 0;

            sink[op.Resource] += op.Quantity;
        }
    }
    protected override void AppendDetailLines(List<string> lines)
    {
        lines.Add($"Held operations: {heldOperations.Count}");
        lines.Add($"Rules: {rules.Count}");
    }
}