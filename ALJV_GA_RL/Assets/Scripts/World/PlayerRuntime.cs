using System.Collections.Generic;
using UnityEngine;

public class PlayerRuntime : MonoBehaviour
{
    [Header("Identity")]
    [SerializeField] private string playerName = "Player";
    [SerializeField] private Transform cameraFocus;

    [Header("Core")]
    [SerializeField] private BoardManager boardManager;
    [SerializeField] private PlayerInventory playerInventory;
    [SerializeField] private MatchClock matchClock;
    [SerializeField] private List<StructureDefinition> structureDefinitions = new List<StructureDefinition>();

    [Header("View")]
    [SerializeField] private bool isAIPlayer = false;

    public bool IsAIPlayer => isAIPlayer;

    public string PlayerName => playerName;
    public Transform CameraFocus => cameraFocus;
    public BoardManager Board => boardManager;
    public PlayerInventory Inventory => playerInventory;
    public MatchClock Clock => matchClock;
    public List<StructureDefinition> StructureDefinitions => structureDefinitions;

    public bool TryBuy(StructureDefinition definition)
    {
        if (definition == null || playerInventory == null)
            return false;

        return playerInventory.TryBuyStructure(definition, 1);
    }

    public bool TryPlaceOwned(StructureDefinition definition, Vector3Int cell, Direction direction)
    {
        if (definition == null || playerInventory == null || boardManager == null)
            return false;

        if (playerInventory.GetOwnedCount(definition) <= 0)
            return false;

        Structure structure = definition.CreateStructureInstance();
        if (structure == null)
            return false;

        structure.SetOutputDirection(direction);

        bool placed = boardManager.TryPlaceStructure(cell, structure);
        if (!placed)
            return false;

        return playerInventory.ConsumeStructure(definition, 1);
    }

    public bool TryUpgradeBestPlaced(StructureDefinition definition)
    {
        if (definition == null || playerInventory == null || boardManager == null)
            return false;

        Structure candidate = null;

        var active = boardManager.ActiveStructures;
        for (int i = 0; i < active.Count; i++)
        {
            Structure s = active[i];
            if (s == null || s.OriginDefinition != definition)
                continue;

            if (s.IsMaxLevel())
                continue;

            if (candidate == null || s.Level < candidate.Level)
                candidate = s;
        }

        if (candidate == null)
            return false;

        return candidate.TryUpgrade(playerInventory);
    }

    public bool TrySetConverterRecipeAtCell(Vector3Int cell, int recipeIndex)
    {
        if (boardManager == null)
            return false;

        Structure structure = boardManager.GetStructureAt(cell);
        if (structure is ConverterStructure converter)
        {
            return converter.TrySetActiveRecipe(recipeIndex);
        }

        return false;
    }

    public bool TrySetAllPlacedConverterRecipes(StructureDefinition definition, int recipeIndex)
    {
        if (definition == null || boardManager == null)
            return false;

        bool changedAny = false;

        var active = boardManager.ActiveStructures;
        for (int i = 0; i < active.Count; i++)
        {
            Structure s = active[i];
            if (s is ConverterStructure converter && s.OriginDefinition == definition)
            {
                if (converter.TrySetActiveRecipe(recipeIndex))
                    changedAny = true;
            }
        }

        return changedAny;
    }

    public List<Structure> GetPlacedStructures(StructureDefinition definition)
    {
        List<Structure> result = new List<Structure>();

        if (definition == null || boardManager == null)
            return result;

        var active = boardManager.ActiveStructures;
        for (int i = 0; i < active.Count; i++)
        {
            Structure s = active[i];
            if (s != null && s.OriginDefinition == definition)
                result.Add(s);
        }

        return result;
    }

    public StrategicObservation BuildObservation(ResourceType targetResource)
    {
        StrategicObservation observation = new StrategicObservation();

        Dictionary<ResourceType, int> resources = playerInventory.GetResourceSnapshot();
        foreach (var kvp in resources)
            observation.Resources[kvp.Key] = kvp.Value;

        Dictionary<StructureDefinition, int> owned = playerInventory.GetStructureSnapshot();
        foreach (var kvp in owned)
            observation.OwnedById[kvp.Key.GetStableId()] = kvp.Value;

        Dictionary<string, int> placedCounts = new Dictionary<string, int>();
        Dictionary<string, int> placedLevelSums = new Dictionary<string, int>();
        Dictionary<string, Dictionary<int, int>> recipeUsageCounts = new Dictionary<string, Dictionary<int, int>>();

        float woodScore = 0f;
        float brickScore = 0f;
        float steelScore = 0f;
        int lineCount = 0;

        var active = boardManager.ActiveStructures;
        for (int i = 0; i < active.Count; i++)
        {
            Structure structure = active[i];
            if (structure == null || structure.OriginDefinition == null)
                continue;

            string id = structure.OriginDefinition.GetStableId();

            if (!placedCounts.ContainsKey(id))
            {
                placedCounts[id] = 0;
                placedLevelSums[id] = 0;
            }

            placedCounts[id]++;
            placedLevelSums[id] += structure.Level;

            if (structure is AssemblyLineStructure)
                lineCount++;

            if (structure is ProducerStructure producer)
            {
                switch (producer.ProducedResource)
                {
                    case ResourceType.Wood: woodScore += producer.GetProductionRate(); break;
                    case ResourceType.Brick: brickScore += producer.GetProductionRate(); break;
                    case ResourceType.Steel: steelScore += producer.GetProductionRate(); break;
                }
            }

            if (structure is ConverterStructure converter)
            {
                if (!recipeUsageCounts.ContainsKey(id))
                    recipeUsageCounts[id] = new Dictionary<int, int>();

                int recipeIndex = converter.ActiveRecipeIndex;
                if (!recipeUsageCounts[id].ContainsKey(recipeIndex))
                    recipeUsageCounts[id][recipeIndex] = 0;

                recipeUsageCounts[id][recipeIndex]++;
            }
        }

        for (int i = 0; i < structureDefinitions.Count; i++)
        {
            StructureDefinition def = structureDefinitions[i];
            if (def == null)
                continue;

            string id = def.GetStableId();

            observation.PlacedById[id] = placedCounts.TryGetValue(id, out int count) ? count : 0;

            if (placedCounts.TryGetValue(id, out int placed) && placed > 0)
                observation.AvgPlacedLevelById[id] = Mathf.RoundToInt((float)placedLevelSums[id] / placed);
            else
                observation.AvgPlacedLevelById[id] = 0;

            if (!observation.OwnedById.ContainsKey(id))
                observation.OwnedById[id] = 0;

            if (def.StructureType == StructureType.Converter)
            {
                observation.ActiveConverterRecipeById[id] = GetDominantRecipeIndex(recipeUsageCounts, id);
            }
        }

        observation.AssemblyLineCount = lineCount;
        observation.TimeLeftSeconds = matchClock != null ? matchClock.TimeLeft : 0f;
        observation.TargetResource = targetResource;

        if (woodScore >= brickScore && woodScore >= steelScore) observation.DominantResource = ResourceType.Wood;
        else if (brickScore >= woodScore && brickScore >= steelScore) observation.DominantResource = ResourceType.Brick;
        else observation.DominantResource = ResourceType.Steel;

        return observation;
    }

    public List<StrategicAction> BuildAvailableActions()
    {
        List<StrategicAction> actions = new List<StrategicAction>
        {
            StrategicAction.Wait()
        };

        for (int i = 0; i < structureDefinitions.Count; i++)
        {
            StructureDefinition def = structureDefinitions[i];
            if (def == null)
                continue;

            string id = def.GetStableId();

            if (playerInventory.CanAfford(def.BuyCost))
                actions.Add(new StrategicAction(StrategicActionKind.BuyStructure, id));

            if (playerInventory.GetOwnedCount(def) > 0)
                actions.Add(new StrategicAction(StrategicActionKind.PlaceStructure, id));

            if (CanUpgradeAnyPlaced(def))
                actions.Add(new StrategicAction(StrategicActionKind.UpgradeStructure, id));

            if (def.StructureType == StructureType.Converter && HasAnyPlaced(def) && def.GetConverterRecipeCount() > 1)
            {
                for (int r = 0; r < def.GetConverterRecipeCount(); r++)
                {
                    actions.Add(new StrategicAction(StrategicActionKind.SetConverterRecipe, id, r));
                }
            }
        }

        return actions;
    }

    public bool CanUpgradeAnyPlaced(StructureDefinition definition)
    {
        List<Structure> placed = GetPlacedStructures(definition);

        for (int i = 0; i < placed.Count; i++)
        {
            if (placed[i].CanUpgrade(playerInventory))
                return true;
        }

        return false;
    }

    public bool TryUpgradeAtCell(Vector3Int cell)
    {
        if (boardManager == null || playerInventory == null)
            return false;

        Structure structure = boardManager.GetStructureAt(cell);
        if (structure == null)
            return false;

        return structure.TryUpgrade(playerInventory);
    }

    public bool HasAnyPlaced(StructureDefinition definition)
    {
        if (definition == null || boardManager == null)
            return false;

        var active = boardManager.ActiveStructures;
        for (int i = 0; i < active.Count; i++)
        {
            if (active[i] != null && active[i].OriginDefinition == definition)
                return true;
        }

        return false;
    }

    public StructureDefinition FindDefinitionById(string id)
    {
        for (int i = 0; i < structureDefinitions.Count; i++)
        {
            if (structureDefinitions[i] != null && structureDefinitions[i].GetStableId() == id)
                return structureDefinitions[i];
        }

        return null;
    }

    private int GetDominantRecipeIndex(Dictionary<string, Dictionary<int, int>> usage, string id)
    {
        if (!usage.TryGetValue(id, out Dictionary<int, int> counts))
            return 0;

        int bestRecipe = 0;
        int bestCount = int.MinValue;

        foreach (var kvp in counts)
        {
            if (kvp.Value > bestCount)
            {
                bestCount = kvp.Value;
                bestRecipe = kvp.Key;
            }
        }

        return bestRecipe;
    }
}