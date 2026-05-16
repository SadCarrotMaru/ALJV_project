using System.Collections.Generic;
using UnityEngine;

public class MagicalConverterStructure : Structure
{
    private readonly int inputPerRoll;
    private readonly float baseSuccessChance;
    private readonly float rollInterval;
    private float rollTimer;

    public MagicalConverterStructure(int inputPerRoll = 1, float baseSuccessChance = 0.55f, float rollInterval = 1f)
    {
        structureType = StructureType.MagicalConverter;
        this.inputPerRoll = inputPerRoll;
        this.baseSuccessChance = baseSuccessChance;
        this.rollInterval = rollInterval;
    }

    protected override void HandleIncomingOperation(ResourceOperation operation)
    {
        AddStoredResource(operation.Resource, operation.Quantity);
    }

    protected override void OnTick(float deltaTime)
    {
        rollTimer += deltaTime;

        if (rollTimer < rollInterval)
            return;

        rollTimer = 0f;
        ProcessRandomConversion();
    }

    private void ProcessRandomConversion()
    {
        ResourceType[] resourcePriority =
        {
            ResourceType.Wood,
            ResourceType.Brick,
            ResourceType.Steel,
            ResourceType.Psi
        };

        for (int i = 0; i < resourcePriority.Length; i++)
        {
            ResourceType input = resourcePriority[i];
            int available = GetStoredResource(input);

            if (available < inputPerRoll)
                continue;

            ConsumeStoredResource(input, inputPerRoll);

            bool success = Random.value <= GetSuccessChance();
            if (!success)
            {
                return;
            }

            ResourceType output = RollRandomResource();
            bool sent = outputs.Count > 0 && TrySend(output, 1, 0);

            if (!sent)
            {
                DiscardOperation(new ResourceOperation(this, output, 1));
            }

            return;
        }
    }

    public float GetSuccessChance()
    {
        return Mathf.Clamp01(baseSuccessChance + (level - 1) * 0.1f);
    }

    private ResourceType RollRandomResource()
    {
        float roll = Random.value;

        if (roll < 0.30f) return ResourceType.Wood;
        if (roll < 0.60f) return ResourceType.Brick;
        if (roll < 0.90f) return ResourceType.Steel;
        return ResourceType.Psi;
    }
    protected override void AppendDetailLines(List<string> lines)
    {
        lines.Add($"Success chance: {GetSuccessChance():P0}");
        lines.Add($"Input per roll: {inputPerRoll}");
        lines.Add($"Roll interval: {rollInterval:0.##}s");
    }
}