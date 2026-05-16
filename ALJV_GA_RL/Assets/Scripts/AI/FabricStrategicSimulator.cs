using System.Collections.Generic;
using UnityEngine;

public class FabricStrategicSimulator
{
    private readonly List<StructureDefinition> definitions;
    private readonly RLRewardWeights rewardWeights;
    private readonly float stepSeconds;
    private readonly float totalMatchSeconds;
    private readonly ResourceType targetResource;

    private readonly Dictionary<ResourceType, int> resources = new Dictionary<ResourceType, int>();
    private readonly Dictionary<string, int> ownedById = new Dictionary<string, int>();
    private readonly Dictionary<string, List<int>> placedLevelsById = new Dictionary<string, List<int>>();
    private readonly Dictionary<string, int> activeConverterRecipeById = new Dictionary<string, int>();

    private float timeLeft;

    public FabricStrategicSimulator(
        List<StructureDefinition> definitions,
        List<ResourceAmount> startingResources,
        List<StructureStockEntry> startingStructures,
        RLRewardWeights rewardWeights,
        float stepSeconds,
        float totalMatchSeconds,
        ResourceType targetResource)
    {
        this.definitions = definitions;
        this.rewardWeights = rewardWeights;
        this.stepSeconds = stepSeconds;
        this.totalMatchSeconds = totalMatchSeconds;
        this.targetResource = targetResource;

        Reset(startingResources, startingStructures);
    }

    public void Reset(List<ResourceAmount> startingResources, List<StructureStockEntry> startingStructures)
    {
        resources.Clear();
        ownedById.Clear();
        placedLevelsById.Clear();
        activeConverterRecipeById.Clear();

        resources[ResourceType.Wood] = 0;
        resources[ResourceType.Brick] = 0;
        resources[ResourceType.Steel] = 0;
        resources[ResourceType.Psi] = 0;

        if (startingResources != null)
        {
            for (int i = 0; i < startingResources.Count; i++)
            {
                ResourceAmount amount = startingResources[i];
                if (amount == null || amount.Resource == ResourceType.None)
                    continue;

                resources[amount.Resource] += Mathf.Max(0, amount.Amount);
            }
        }

        if (definitions != null)
        {
            for (int i = 0; i < definitions.Count; i++)
            {
                StructureDefinition def = definitions[i];
                if (def == null)
                    continue;

                string id = def.GetStableId();
                ownedById[id] = 0;
                placedLevelsById[id] = new List<int>();

                if (def.StructureType == StructureType.Converter)
                    activeConverterRecipeById[id] = 0;
            }
        }

        if (startingStructures != null)
        {
            for (int i = 0; i < startingStructures.Count; i++)
            {
                StructureStockEntry entry = startingStructures[i];
                if (entry == null || entry.Definition == null)
                    continue;

                ownedById[entry.Definition.GetStableId()] = Mathf.Max(0, entry.Count);
            }
        }

        timeLeft = totalMatchSeconds;
    }

    public bool IsTerminal()
    {
        return timeLeft <= 0f;
    }

    public string GetStateKey()
    {
        StrategicObservation observation = BuildObservation();
        return StrategicStateEncoder.BuildKey(observation, definitions);
    }

    public List<StrategicAction> GetAvailableActions()
    {
        List<StrategicAction> actions = new List<StrategicAction>
        {
            StrategicAction.Wait()
        };

        if (definitions == null)
            return actions;

        for (int i = 0; i < definitions.Count; i++)
        {
            StructureDefinition def = definitions[i];
            if (def == null)
                continue;

            string id = def.GetStableId();

            if (CanAfford(def.BuyCost))
                actions.Add(new StrategicAction(StrategicActionKind.BuyStructure, id));

            if (ownedById.TryGetValue(id, out int owned) && owned > 0)
                actions.Add(new StrategicAction(StrategicActionKind.PlaceStructure, id));

            if (CanUpgradeAnyPlaced(def))
                actions.Add(new StrategicAction(StrategicActionKind.UpgradeStructure, id));

            if (def.StructureType == StructureType.Converter && placedLevelsById[id].Count > 0 && def.GetConverterRecipeCount() > 1)
            {
                for (int r = 0; r < def.GetConverterRecipeCount(); r++)
                {
                    actions.Add(new StrategicAction(StrategicActionKind.SetConverterRecipe, id, r));
                }
            }
        }

        return actions;
    }

    public float Step(StrategicAction action)
    {
        float spent = 0f;
        float upgradeBonus = 0f;
        float recipeSwitchBonus = 0f;

        if (action != null)
        {
            switch (action.Kind)
            {
                case StrategicActionKind.BuyStructure:
                    spent = TryBuy(action.StructureId);
                    break;

                case StrategicActionKind.PlaceStructure:
                    TryPlace(action.StructureId);
                    break;

                case StrategicActionKind.UpgradeStructure:
                    if (TryUpgrade(action.StructureId, out float upgradeSpent))
                    {
                        spent += upgradeSpent;
                        upgradeBonus = 1f;
                    }
                    break;

                case StrategicActionKind.SetConverterRecipe:
                    if (TrySetConverterRecipe(action.StructureId, action.RecipeIndex))
                    {
                        recipeSwitchBonus = 1f;
                    }
                    break;
            }
        }

        int beforeWood = resources[ResourceType.Wood];
        int beforeBrick = resources[ResourceType.Brick];
        int beforeSteel = resources[ResourceType.Steel];
        int beforePsi = resources[ResourceType.Psi];

        float wastedOutput = AdvanceProduction();

        int deltaWood = resources[ResourceType.Wood] - beforeWood;
        int deltaBrick = resources[ResourceType.Brick] - beforeBrick;
        int deltaSteel = resources[ResourceType.Steel] - beforeSteel;
        int deltaPsi = resources[ResourceType.Psi] - beforePsi;

        int generalDelta = Mathf.Max(0, deltaWood) + Mathf.Max(0, deltaBrick) + Mathf.Max(0, deltaSteel);
        int counterDelta = 0;

        switch (targetResource)
        {
            case ResourceType.Wood: counterDelta = Mathf.Max(0, deltaWood); break;
            case ResourceType.Brick: counterDelta = Mathf.Max(0, deltaBrick); break;
            case ResourceType.Steel: counterDelta = Mathf.Max(0, deltaSteel); break;
            case ResourceType.Psi: counterDelta = Mathf.Max(0, deltaPsi); break;
        }

        timeLeft -= stepSeconds;
        if (timeLeft < 0f)
            timeLeft = 0f;

        int idleStructures = CalculateIdleStructureCount();

        float reward =
            rewardWeights.CounterResourceWeight * counterDelta
            - rewardWeights.WastedOutputPenalty * wastedOutput
            - rewardWeights.BuildCostPenalty * spent
            + rewardWeights.PsiWeight * Mathf.Max(0, deltaPsi)
            + rewardWeights.GeneralResourceWeight * generalDelta
            + rewardWeights.UpgradeBonusWeight * upgradeBonus
            + rewardWeights.RecipeSwitchBonusWeight * recipeSwitchBonus
            - rewardWeights.IdleStructurePenalty * idleStructures;

        return reward;
    }

    private int CalculateIdleStructureCount()
    {
        int lineCount = CountPlaced(StructureType.AssemblyLine);

        int woodProducers = GetPlacedLevels(StructureType.Producer, ResourceType.Wood).Count;
        int brickProducers = GetPlacedLevels(StructureType.Producer, ResourceType.Brick).Count;
        int steelProducers = GetPlacedLevels(StructureType.Producer, ResourceType.Steel).Count;

        int woodCollectors = GetPlacedLevels(StructureType.Collector, ResourceType.Wood).Count;
        int brickCollectors = GetPlacedLevels(StructureType.Collector, ResourceType.Brick).Count;
        int steelCollectors = GetPlacedLevels(StructureType.Collector, ResourceType.Steel).Count;

        int woodActive = Mathf.Min(woodProducers, woodCollectors, lineCount);
        int brickActive = Mathf.Min(brickProducers, brickCollectors, lineCount);
        int steelActive = Mathf.Min(steelProducers, steelCollectors, lineCount);

        int idle =
            Mathf.Max(0, woodProducers - woodActive) +
            Mathf.Max(0, brickProducers - brickActive) +
            Mathf.Max(0, steelProducers - steelActive) +
            Mathf.Max(0, woodCollectors - woodActive) +
            Mathf.Max(0, brickCollectors - brickActive) +
            Mathf.Max(0, steelCollectors - steelActive);

        return idle;
    }

    private float TryBuy(string structureId)
    {
        StructureDefinition def = FindDefinition(structureId);
        if (def == null || !CanAfford(def.BuyCost))
            return 0f;

        float totalSpent = 0f;

        for (int i = 0; i < def.BuyCost.Count; i++)
        {
            ResourceAmount cost = def.BuyCost[i];
            if (cost == null || cost.Resource == ResourceType.None)
                continue;

            resources[cost.Resource] -= Mathf.Max(0, cost.Amount);
            totalSpent += cost.Amount;
        }

        ownedById[structureId] += 1;
        return totalSpent;
    }

    private void TryPlace(string structureId)
    {
        if (!ownedById.TryGetValue(structureId, out int owned) || owned <= 0)
            return;

        ownedById[structureId] -= 1;
        placedLevelsById[structureId].Add(1);
    }

    private bool TryUpgrade(string structureId, out float totalSpent)
    {
        totalSpent = 0f;

        StructureDefinition def = FindDefinition(structureId);
        if (def == null)
            return false;

        if (!placedLevelsById.TryGetValue(structureId, out List<int> placed) || placed.Count == 0)
            return false;

        int candidateIndex = -1;
        int lowestLevel = int.MaxValue;

        for (int i = 0; i < placed.Count; i++)
        {
            int currentLevel = placed[i];
            if (currentLevel >= def.MaxLevel)
                continue;

            List<ResourceAmount> nextCost = def.GetUpgradeCostForTargetLevel(currentLevel + 1);
            if (nextCost == null || !CanAfford(nextCost))
                continue;

            if (currentLevel < lowestLevel)
            {
                lowestLevel = currentLevel;
                candidateIndex = i;
            }
        }

        if (candidateIndex < 0)
            return false;

        List<ResourceAmount> costToSpend = def.GetUpgradeCostForTargetLevel(placed[candidateIndex] + 1);
        for (int i = 0; i < costToSpend.Count; i++)
        {
            ResourceAmount cost = costToSpend[i];
            if (cost == null || cost.Resource == ResourceType.None)
                continue;

            resources[cost.Resource] -= cost.Amount;
            totalSpent += cost.Amount;
        }

        placed[candidateIndex] += 1;
        return true;
    }

    private bool TrySetConverterRecipe(string structureId, int recipeIndex)
    {
        StructureDefinition def = FindDefinition(structureId);
        if (def == null || def.StructureType != StructureType.Converter)
            return false;

        if (!placedLevelsById.TryGetValue(structureId, out List<int> placed) || placed.Count == 0)
            return false;

        if (recipeIndex < 0 || recipeIndex >= def.GetConverterRecipeCount())
            return false;

        activeConverterRecipeById[structureId] = recipeIndex;
        return true;
    }

    private bool CanUpgradeAnyPlaced(StructureDefinition def)
    {
        if (def == null)
            return false;

        string id = def.GetStableId();
        if (!placedLevelsById.TryGetValue(id, out List<int> placed) || placed.Count == 0)
            return false;

        for (int i = 0; i < placed.Count; i++)
        {
            int currentLevel = placed[i];
            if (currentLevel >= def.MaxLevel)
                continue;

            List<ResourceAmount> nextCost = def.GetUpgradeCostForTargetLevel(currentLevel + 1);
            if (nextCost != null && CanAfford(nextCost))
                return true;
        }

        return false;
    }

    private bool CanAfford(List<ResourceAmount> costs)
    {
        if (costs == null)
            return true;

        for (int i = 0; i < costs.Count; i++)
        {
            ResourceAmount cost = costs[i];
            if (cost == null || cost.Resource == ResourceType.None)
                continue;

            if (resources[cost.Resource] < cost.Amount)
                return false;
        }

        return true;
    }

    private float AdvanceProduction()
    {
        int lineCount = CountPlaced(StructureType.AssemblyLine);
        float wastedOutput = 0f;

        ResourceType[] basicResources =
        {
            ResourceType.Wood,
            ResourceType.Brick,
            ResourceType.Steel
        };

        for (int i = 0; i < basicResources.Length; i++)
        {
            ResourceType type = basicResources[i];

            List<int> producerLevels = GetPlacedLevels(StructureType.Producer, type);
            List<int> collectorLevels = GetPlacedLevels(StructureType.Collector, type);

            int producerCount = producerLevels.Count;
            int collectorCount = collectorLevels.Count;

            if (producerCount <= 0)
                continue;

            float totalProducerRate = 0f;
            for (int p = 0; p < producerLevels.Count; p++)
                totalProducerRate += GetProducerRateAtLevel(producerLevels[p]);

            int activePipelines = Mathf.Min(producerCount, collectorCount, lineCount);
            float efficiency = producerCount > 0 ? (float)activePipelines / producerCount : 0f;

            float avgCollectorLevel = GetAverageLevel(collectorLevels);
            float collectorBoost = 1f + 0.15f * Mathf.Max(0, avgCollectorLevel - 1);

            int produced = Mathf.RoundToInt(totalProducerRate * efficiency * collectorBoost * stepSeconds);
            resources[type] += produced;

            float wasted = totalProducerRate * (1f - efficiency) * stepSeconds;
            wastedOutput += Mathf.Max(0f, wasted);
        }

        for (int i = 0; i < definitions.Count; i++)
        {
            StructureDefinition def = definitions[i];
            if (def == null || def.StructureType != StructureType.Converter)
                continue;

            string id = def.GetStableId();

            if (!placedLevelsById.TryGetValue(id, out List<int> placedLevels) || placedLevels.Count == 0)
                continue;

            if (def.ConverterRecipes == null || def.ConverterRecipes.Count == 0)
                continue;

            int selectedRecipe = activeConverterRecipeById.TryGetValue(id, out int r) ? r : 0;
            selectedRecipe = Mathf.Clamp(selectedRecipe, 0, def.ConverterRecipes.Count - 1);

            ConversionRecipeConfig recipe = def.ConverterRecipes[selectedRecipe];
            if (recipe == null)
                continue;

            for (int c = 0; c < placedLevels.Count; c++)
            {
                int level = placedLevels[c];
                int effectiveInput = Mathf.Max(1, recipe.InputAmount - (level - 1));

                if (resources[recipe.Input] >= effectiveInput)
                {
                    resources[recipe.Input] -= effectiveInput;
                    resources[recipe.Output] += recipe.OutputAmount;
                }
            }
        }

        List<int> magicalLevels = GetPlacedLevels(StructureType.MagicalConverter, ResourceType.None);
        if (magicalLevels.Count > 0)
        {
            float psiGain = 0f;
            for (int i = 0; i < magicalLevels.Count; i++)
                psiGain += (0.10f + 0.03f * (magicalLevels[i] - 1)) * stepSeconds;

            resources[ResourceType.Psi] += Mathf.RoundToInt(psiGain);
        }

        return wastedOutput;
    }

    private StrategicObservation BuildObservation()
    {
        StrategicObservation observation = new StrategicObservation();
        observation.TimeLeftSeconds = timeLeft;
        observation.TargetResource = targetResource;
        observation.DominantResource = CalculateDominantResource();
        observation.AssemblyLineCount = CountPlaced(StructureType.AssemblyLine);

        foreach (var kvp in resources)
            observation.Resources[kvp.Key] = kvp.Value;

        foreach (var kvp in ownedById)
            observation.OwnedById[kvp.Key] = kvp.Value;

        foreach (var kvp in placedLevelsById)
        {
            observation.PlacedById[kvp.Key] = kvp.Value.Count;
            observation.AvgPlacedLevelById[kvp.Key] = kvp.Value.Count > 0
                ? Mathf.RoundToInt(GetAverageLevel(kvp.Value))
                : 0;
        }

        foreach (var kvp in activeConverterRecipeById)
            observation.ActiveConverterRecipeById[kvp.Key] = kvp.Value;

        return observation;
    }

    private ResourceType CalculateDominantResource()
    {
        float wood = SumProducerPower(ResourceType.Wood);
        float brick = SumProducerPower(ResourceType.Brick);
        float steel = SumProducerPower(ResourceType.Steel);

        if (wood >= brick && wood >= steel) return ResourceType.Wood;
        if (brick >= wood && brick >= steel) return ResourceType.Brick;
        return ResourceType.Steel;
    }

    private float SumProducerPower(ResourceType type)
    {
        List<int> levels = GetPlacedLevels(StructureType.Producer, type);
        float total = 0f;

        for (int i = 0; i < levels.Count; i++)
            total += GetProducerRateAtLevel(levels[i]);

        return total;
    }

    private float GetProducerRateAtLevel(int level)
    {
        return 1f + (level - 1) * 0.5f;
    }

    private List<int> GetPlacedLevels(StructureType type, ResourceType variant)
    {
        List<int> result = new List<int>();

        for (int i = 0; i < definitions.Count; i++)
        {
            StructureDefinition def = definitions[i];
            if (def == null)
                continue;

            if (def.StructureType != type)
                continue;

            if ((type == StructureType.Producer || type == StructureType.Collector) && def.ResourceVariant != variant)
                continue;

            if (placedLevelsById.TryGetValue(def.GetStableId(), out List<int> levels))
                result.AddRange(levels);
        }

        return result;
    }

    private int CountPlaced(StructureType type)
    {
        int total = 0;

        for (int i = 0; i < definitions.Count; i++)
        {
            StructureDefinition def = definitions[i];
            if (def == null || def.StructureType != type)
                continue;

            if (placedLevelsById.TryGetValue(def.GetStableId(), out List<int> levels))
                total += levels.Count;
        }

        return total;
    }

    private float GetAverageLevel(List<int> levels)
    {
        if (levels == null || levels.Count == 0)
            return 0f;

        float sum = 0f;
        for (int i = 0; i < levels.Count; i++)
            sum += levels[i];

        return sum / levels.Count;
    }

    private StructureDefinition FindDefinition(string id)
    {
        for (int i = 0; i < definitions.Count; i++)
        {
            if (definitions[i] != null && definitions[i].GetStableId() == id)
                return definitions[i];
        }

        return null;
    }
}