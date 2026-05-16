using System.Collections.Generic;
using UnityEngine;

public static class BlueprintEvaluator
{
    public static float Evaluate(
        FabricBlueprint blueprint,
        GeneticAlgorithmSettings settings,
        GAFitnessWeights weights,
        ResourceType targetResource)
    {
        if (blueprint == null)
            return float.MinValue;

        float fitness = 0f;
        HashSet<Vector2Int> occupied = new HashSet<Vector2Int>();
        Dictionary<Vector2Int, BlueprintGene> map = new Dictionary<Vector2Int, BlueprintGene>();

        float buildCost = 0f;
        float wasted = 0f;

        for (int i = 0; i < blueprint.Genes.Count; i++)
        {
            BlueprintGene gene = blueprint.Genes[i];
            if (gene == null || gene.Definition == null)
                continue;

            bool outOfBounds =
                gene.LocalCell.x < 0 || gene.LocalCell.y < 0 ||
                gene.LocalCell.x >= settings.GridWidth || gene.LocalCell.y >= settings.GridHeight;

            if (outOfBounds || occupied.Contains(gene.LocalCell))
            {
                fitness -= weights.InvalidPlacementPenalty;
                continue;
            }

            occupied.Add(gene.LocalCell);
            map[gene.LocalCell] = gene;

            buildCost += GetTotalInvestmentCost(gene);
        }

        for (int i = 0; i < blueprint.Genes.Count; i++)
        {
            BlueprintGene gene = blueprint.Genes[i];
            if (gene == null || gene.Definition == null)
                continue;

            StructureDefinition def = gene.Definition;
            int level = Mathf.Clamp(gene.TargetLevel, 1, Mathf.Max(1, def.MaxLevel));

            if (def.StructureType == StructureType.Producer)
            {
                bool connected = FollowPipeline(map, gene, out int pathLength, out BlueprintGene endGene);
                float rate = def.ProducerRate + (level - 1) * 0.5f;
                float baseOutput = rate * settings.HorizonSeconds;

                if (connected && endGene != null &&
                    endGene.Definition.StructureType == StructureType.Collector &&
                    endGene.Definition.ResourceVariant == def.ResourceVariant)
                {
                    float collectorBoost = 1f + 0.15f * Mathf.Max(0, endGene.TargetLevel - 1);
                    float linePenalty = Mathf.Clamp01(1f - (pathLength - 1) * 0.03f);
                    float outputScore = baseOutput * collectorBoost * linePenalty;

                    fitness += outputScore * weights.OutputWeight;

                    if (def.ResourceVariant == targetResource)
                        fitness += outputScore * weights.TargetBonusWeight;

                    if (level > 1)
                        fitness += level * weights.UpgradeBonusWeight;
                }
                else
                {
                    wasted += baseOutput;
                }
            }
            else if (def.StructureType == StructureType.Converter)
            {
                if (HasAdjacentLine(map, gene.LocalCell))
                {
                    fitness += weights.UtilityBonus * 1.5f * (1f + 0.2f * (level - 1));

                    int recipeIndex = Mathf.Clamp(gene.ConverterRecipeIndex, 0, Mathf.Max(0, def.GetConverterRecipeCount() - 1));
                    if (def.ConverterRecipes != null && def.ConverterRecipes.Count > 0)
                    {
                        ConversionRecipeConfig recipe = def.ConverterRecipes[recipeIndex];

                        if (recipe != null)
                        {
                            if (recipe.Output == targetResource)
                                fitness += weights.RecipeMatchWeight * 4f;

                            if (HasPotentialSourceForResource(map, recipe.Input))
                                fitness += weights.RecipeMatchWeight * 2f;
                        }
                    }
                }
            }
            else if (def.StructureType == StructureType.MagicalConverter)
            {
                float psiScore = settings.HorizonSeconds * (0.10f + 0.03f * (level - 1));
                fitness += psiScore * weights.PsiWeight;
            }
            else if (def.StructureType == StructureType.Observer ||
                     def.StructureType == StructureType.Delayer ||
                     def.StructureType == StructureType.Retriever)
            {
                if (HasAdjacentLine(map, gene.LocalCell))
                    fitness += weights.UtilityBonus * (1f + 0.2f * (level - 1));
            }
            else if (def.StructureType == StructureType.AssemblyLine)
            {
                fitness += 0.2f * level;
            }
            else if (def.StructureType == StructureType.Collector)
            {
                fitness += 0.3f * level;
            }
        }

        fitness -= buildCost * weights.CostPenaltyWeight;
        fitness -= wasted * weights.WastePenaltyWeight;

        return fitness;
    }

    private static bool HasPotentialSourceForResource(Dictionary<Vector2Int, BlueprintGene> map, ResourceType resource)
    {
        foreach (var kvp in map)
        {
            BlueprintGene gene = kvp.Value;
            if (gene == null || gene.Definition == null)
                continue;

            if (gene.Definition.StructureType == StructureType.Producer && gene.Definition.ResourceVariant == resource)
                return true;

            if (gene.Definition.StructureType == StructureType.Converter &&
                gene.Definition.ConverterRecipes != null &&
                gene.Definition.ConverterRecipes.Count > 0)
            {
                int recipeIndex = Mathf.Clamp(gene.ConverterRecipeIndex, 0, gene.Definition.ConverterRecipes.Count - 1);
                ConversionRecipeConfig recipe = gene.Definition.ConverterRecipes[recipeIndex];

                if (recipe != null && recipe.Output == resource)
                    return true;
            }
        }

        return false;
    }

    private static float GetTotalInvestmentCost(BlueprintGene gene)
    {
        if (gene == null || gene.Definition == null)
            return 0f;

        float total = 0f;

        if (gene.Definition.BuyCost != null)
        {
            for (int i = 0; i < gene.Definition.BuyCost.Count; i++)
            {
                ResourceAmount cost = gene.Definition.BuyCost[i];
                if (cost != null)
                    total += cost.Amount;
            }
        }

        int cappedTargetLevel = Mathf.Clamp(gene.TargetLevel, 1, Mathf.Max(1, gene.Definition.MaxLevel));

        for (int level = 2; level <= cappedTargetLevel; level++)
        {
            List<ResourceAmount> upgradeCost = gene.Definition.GetUpgradeCostForTargetLevel(level);
            if (upgradeCost == null)
                continue;

            for (int i = 0; i < upgradeCost.Count; i++)
            {
                ResourceAmount cost = upgradeCost[i];
                if (cost != null)
                    total += cost.Amount;
            }
        }

        return total;
    }

    private static bool FollowPipeline(
        Dictionary<Vector2Int, BlueprintGene> map,
        BlueprintGene producer,
        out int pathLength,
        out BlueprintGene endGene)
    {
        pathLength = 0;
        endGene = null;

        Vector2Int dir = producer.Direction.ToVector2Int();
        Vector2Int cursor = producer.LocalCell + dir;

        while (map.TryGetValue(cursor, out BlueprintGene current))
        {
            pathLength++;

            if (current.Definition == null)
                return false;

            if (current.Definition.StructureType == StructureType.AssemblyLine)
            {
                cursor += dir;
                continue;
            }

            if (current.Definition.StructureType == StructureType.Collector)
            {
                endGene = current;
                return true;
            }

            return false;
        }

        return false;
    }

    private static bool HasAdjacentLine(Dictionary<Vector2Int, BlueprintGene> map, Vector2Int cell)
    {
        Vector2Int[] offsets =
        {
            Vector2Int.up,
            Vector2Int.right,
            Vector2Int.down,
            Vector2Int.left
        };

        for (int i = 0; i < offsets.Length; i++)
        {
            Vector2Int neighbor = cell + offsets[i];
            if (map.TryGetValue(neighbor, out BlueprintGene gene) &&
                gene.Definition != null &&
                gene.Definition.StructureType == StructureType.AssemblyLine)
            {
                return true;
            }
        }

        return false;
    }
}