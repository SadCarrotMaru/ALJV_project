using System.Collections.Generic;
using UnityEngine;

public class AssemblyLineStructure : Structure
{
    private class TransitItem
    {
        public ResourceOperation Operation;
        public float TimeLeft;

        public TransitItem(ResourceOperation operation, float timeLeft)
        {
            Operation = operation;
            TimeLeft = timeLeft;
        }
    }

    private readonly float baseTravelTime;
    private readonly List<TransitItem> transit = new List<TransitItem>();

    public override bool HasDirectionalOutput => false;
    public override bool SupportsRotation => false;

    public AssemblyLineStructure(float baseTravelTime = 0.4f)
    {
        structureType = StructureType.AssemblyLine;
        this.baseTravelTime = baseTravelTime;
    }

    public override void RebuildConnections(IStructureBoard board)
    {
        DisconnectAllOutputs();

        if (ownerCell == null || ownerBoard == null)
            return;

        List<Structure> neighbors = ownerBoard.GetAdjacentStructures(ownerCell.Value);

        for (int i = 0; i < neighbors.Count; i++)
        {
            Structure neighbor = neighbors[i];

            if (neighbor is AssemblyLineStructure || neighbor is CollectorStructure)
            {
                ConnectOutput(neighbor);
            }
        }
    }

    protected override void HandleIncomingOperation(ResourceOperation operation)
    {
        transit.Add(new TransitItem(operation, GetTravelTime()));
    }

    protected override void OnTick(float deltaTime)
    {
        for (int i = transit.Count - 1; i >= 0; i--)
        {
            transit[i].TimeLeft -= deltaTime;

            if (transit[i].TimeLeft > 0f)
                continue;

            if (ownerBoard == null || ownerCell == null)
            {
                transit[i].TimeLeft = 0.15f;
                continue;
            }

            Structure nextHop = ownerBoard.GetBestNextHopForLine(
                ownerCell.Value,
                transit[i].Operation.Resource,
                transit[i].Operation.Quantity
            );

            if (nextHop == null)
            {
                transit[i].TimeLeft = 0.15f;
                continue;
            }

            bool sent = TrySendToTarget(nextHop, transit[i].Operation);

            if (sent)
            {
                transit.RemoveAt(i);
            }
            else
            {
                transit[i].TimeLeft = 0.15f;
            }
        }
    }

    public float GetTravelTime()
    {
        return Mathf.Max(0.05f, baseTravelTime - (level - 1) * 0.05f);
    }

    protected override void DrainAdditionalResourcesTo(Dictionary<ResourceType, int> sink)
    {
        for (int i = 0; i < transit.Count; i++)
        {
            ResourceOperation op = transit[i].Operation;
            if (op == null || op.Resource == ResourceType.None || op.Quantity <= 0)
                continue;

            if (!sink.ContainsKey(op.Resource))
                sink[op.Resource] = 0;

            sink[op.Resource] += op.Quantity;
        }

        transit.Clear();
    }

    protected override void AppendDetailLines(List<string> lines)
    {
        lines.Add($"Travel time: {GetTravelTime():0.##}s");
        lines.Add($"Connected paths: {outputs.Count}");
    }
}