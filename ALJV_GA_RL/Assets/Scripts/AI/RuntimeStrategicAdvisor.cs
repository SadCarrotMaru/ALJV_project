using System.Collections.Generic;
using UnityEngine;

public class RuntimeStrategicAdvisor : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private BoardManager boardManager;
    [SerializeField] private PlayerInventory playerInventory;
    [SerializeField] private MatchClock matchClock;

    [Header("Definitions")]
    [SerializeField] private List<StructureDefinition> structureDefinitions = new List<StructureDefinition>();

    [Header("Target")]
    [SerializeField] private ResourceType targetResource = ResourceType.Wood;

    [Header("QTable")]
    [SerializeField] private string qTableFileName = "qtable.json";

    private readonly QTable qTable = new QTable();

    private void Awake()
    {
        string path = System.IO.Path.Combine(Application.persistentDataPath, qTableFileName);
        qTable.Load(path);
    }

    [ContextMenu("Log Recommended Action")]
    public void LogRecommendedAction()
    {
        StrategicAction action = GetRecommendedAction();
        Debug.Log(action != null ? $"Recommended: {action}" : "No recommendation.");
    }

    public StrategicAction GetRecommendedAction()
    {
        if (boardManager == null || playerInventory == null || matchClock == null)
            return StrategicAction.Wait();

        StrategicObservation observation = BuildObservationFromRuntime();
        string stateKey = StrategicStateEncoder.BuildKey(observation, structureDefinitions);
        List<StrategicAction> availableActions = BuildAvailableActionsFromRuntime();

        return qTable.GetBestAction(stateKey, availableActions);
    }

    private StrategicObservation BuildObservationFromRuntime()
    {
        StrategicObservation observation = new StrategicObservation();

        Dictionary<ResourceType, int> resourceSnapshot = playerInventory.GetResourceSnapshot();
        foreach (var kvp in resourceSnapshot)
            observation.Resources[kvp.Key] = kvp.Value;

        Dictionary<StructureDefinition, int> ownedSnapshot = playerInventory.GetStructureSnapshot();
        foreach (var kvp in ownedSnapshot)
            observation.OwnedById[kvp.Key.GetStableId()] = kvp.Value;

        for (int i = 0; i < structureDefinitions.Count; i++)
        {
            StructureDefinition def = structureDefinitions[i];
            if (def == null)
                continue;

            string id = def.GetStableId();
            if (!observation.OwnedById.ContainsKey(id))
                observation.OwnedById[id] = 0;
            if (!observation.PlacedById.ContainsKey(id))
                observation.PlacedById[id] = 0;
        }

        int lineCount = 0;
        float woodScore = 0f;
        float brickScore = 0f;
        float steelScore = 0f;

        var active = boardManager.ActiveStructures;
        for (int i = 0; i < active.Count; i++)
        {
            Structure structure = active[i];

            if (structure is ProducerStructure producer)
            {
                ResourceType type = producer.ProducedResource;
                string id = FindProducerDefinitionId(type, producer.GetProductionRate());

                if (!string.IsNullOrEmpty(id))
                    observation.PlacedById[id] = observation.PlacedById.TryGetValue(id, out int c) ? c + 1 : 1;

                switch (type)
                {
                    case ResourceType.Wood: woodScore += producer.GetProductionRate(); break;
                    case ResourceType.Brick: brickScore += producer.GetProductionRate(); break;
                    case ResourceType.Steel: steelScore += producer.GetProductionRate(); break;
                }
            }
            else if (structure is CollectorStructure collector)
            {
                string id = FindCollectorDefinitionId(collector.AcceptedResource);
                if (!string.IsNullOrEmpty(id))
                    observation.PlacedById[id] = observation.PlacedById.TryGetValue(id, out int c) ? c + 1 : 1;
            }
            else
            {
                StructureDefinition def = FindDefinitionByType(structure.Type);
                if (def != null)
                    observation.PlacedById[def.GetStableId()] = observation.PlacedById.TryGetValue(def.GetStableId(), out int c) ? c + 1 : 1;
            }

            if (structure is AssemblyLineStructure)
                lineCount++;
        }

        observation.AssemblyLineCount = lineCount;
        observation.TimeLeftSeconds = matchClock.TimeLeft;
        observation.TargetResource = targetResource;

        if (woodScore >= brickScore && woodScore >= steelScore) observation.DominantResource = ResourceType.Wood;
        else if (brickScore >= woodScore && brickScore >= steelScore) observation.DominantResource = ResourceType.Brick;
        else observation.DominantResource = ResourceType.Steel;

        return observation;
    }

    private List<StrategicAction> BuildAvailableActionsFromRuntime()
    {
        List<StrategicAction> actions = new List<StrategicAction>
        {
            StrategicAction.Wait()
        };

        Dictionary<StructureDefinition, int> ownedSnapshot = playerInventory.GetStructureSnapshot();

        for (int i = 0; i < structureDefinitions.Count; i++)
        {
            StructureDefinition def = structureDefinitions[i];
            if (def == null)
                continue;

            if (playerInventory.CanAfford(def.BuyCost))
                actions.Add(new StrategicAction(StrategicActionKind.BuyStructure, def.GetStableId()));

            if (ownedSnapshot.TryGetValue(def, out int count) && count > 0)
                actions.Add(new StrategicAction(StrategicActionKind.PlaceStructure, def.GetStableId()));
        }

        return actions;
    }

    private string FindProducerDefinitionId(ResourceType type, float rate)
    {
        for (int i = 0; i < structureDefinitions.Count; i++)
        {
            StructureDefinition def = structureDefinitions[i];
            if (def == null) continue;

            if (def.StructureType == StructureType.Producer && def.ResourceVariant == type)
                return def.GetStableId();
        }

        return "";
    }

    private string FindCollectorDefinitionId(ResourceType type)
    {
        for (int i = 0; i < structureDefinitions.Count; i++)
        {
            StructureDefinition def = structureDefinitions[i];
            if (def == null) continue;

            if (def.StructureType == StructureType.Collector && def.ResourceVariant == type)
                return def.GetStableId();
        }

        return "";
    }

    private StructureDefinition FindDefinitionByType(StructureType type)
    {
        for (int i = 0; i < structureDefinitions.Count; i++)
        {
            if (structureDefinitions[i] != null && structureDefinitions[i].StructureType == type)
                return structureDefinitions[i];
        }

        return null;
    }
}