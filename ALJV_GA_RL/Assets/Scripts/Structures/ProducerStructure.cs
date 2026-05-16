using System.Collections.Generic;
using UnityEngine;

public class ProducerStructure : Structure
{
    private readonly ResourceType producedResource;
    private readonly float baseProductionPerSecond;
    private float productionBuffer;

    public ResourceType ProducedResource => producedResource;

    public ProducerStructure(ResourceType producedResource, float baseProductionPerSecond = 1f)
    {
        structureType = StructureType.Producer;
        this.producedResource = producedResource;
        this.baseProductionPerSecond = baseProductionPerSecond;
    }

    public override bool CanReceive(ResourceOperation operation)
    {
        return false;
    }

    protected override void OnTick(float deltaTime)
    {
        productionBuffer += GetProductionRate() * deltaTime;

        int wholeUnits = Mathf.FloorToInt(productionBuffer);
        if (wholeUnits <= 0)
            return;

        productionBuffer -= wholeUnits;

        bool sent = outputs.Count > 0 && TrySend(producedResource, wholeUnits, 0);
        if (!sent)
        {
            DiscardOperation(new ResourceOperation(this, producedResource, wholeUnits));
        }
    }

    public float GetProductionRate()
    {
        return baseProductionPerSecond + (level - 1) * 0.5f;
    }

    protected override void AppendDetailLines(List<string> lines)
    {
        lines.Add($"Produces: {producedResource}");
        lines.Add($"Rate: {GetProductionRate():0.##}/s");
        lines.Add($"Facing: {outputDirection}");
    }
}