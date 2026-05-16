using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class GAFitnessWeights
{
    public float OutputWeight = 1.0f;
    public float TargetBonusWeight = 2.0f;
    public float PsiWeight = 1.2f;
    public float CostPenaltyWeight = 0.25f;
    public float WastePenaltyWeight = 1.0f;
    public float InvalidPlacementPenalty = 8.0f;
    public float UtilityBonus = 0.5f;
    public float UpgradeBonusWeight = 0.8f;
    public float RecipeMatchWeight = 1.0f;
}

[Serializable]
public class GeneticAlgorithmSettings
{
    public int PopulationSize = 40;
    public int Generations = 80;
    public float MutationRate = 0.18f;
    public float CrossoverRate = 0.8f;
    public int MaxGenes = 14;
    public int GridWidth = 8;
    public int GridHeight = 8;
    public int HorizonSeconds = 600;
}

[Serializable]
public class BlueprintGene
{
    public StructureDefinition Definition;
    public Vector2Int LocalCell;
    public Direction Direction;
    public int TargetLevel = 1;
    public int ConverterRecipeIndex = 0;

    public BlueprintGene Clone()
    {
        return new BlueprintGene
        {
            Definition = Definition,
            LocalCell = LocalCell,
            Direction = Direction,
            TargetLevel = TargetLevel,
            ConverterRecipeIndex = ConverterRecipeIndex
        };
    }
}

[Serializable]
public class FabricBlueprint
{
    public List<BlueprintGene> Genes = new List<BlueprintGene>();
    public float Fitness;

    public FabricBlueprint Clone()
    {
        FabricBlueprint copy = new FabricBlueprint();
        copy.Fitness = Fitness;

        for (int i = 0; i < Genes.Count; i++)
            copy.Genes.Add(Genes[i].Clone());

        return copy;
    }
}