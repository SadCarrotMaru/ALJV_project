using System;
using System.Collections.Generic;
using UnityEngine;

public enum StrategicActionKind
{
    Wait,
    BuyStructure,
    PlaceStructure,
    UpgradeStructure,
    SetConverterRecipe
}

[Serializable]
public class RLRewardWeights
{
    public float IdleStructurePenalty = 0.35f;
    public float CounterResourceWeight = 2.0f;
    public float WastedOutputPenalty = 1.0f;
    public float BuildCostPenalty = 0.2f;
    public float PsiWeight = 1.5f;
    public float GeneralResourceWeight = 0.7f;
    public float UpgradeBonusWeight = 0.6f;
    public float RecipeSwitchBonusWeight = 0.2f;
}

[Serializable]
public class QLearningSettings
{
    public int Episodes = 3000;
    public float Alpha = 0.15f;
    public float Gamma = 0.95f;
    public float Epsilon = 1.0f;
    public float MinEpsilon = 0.05f;
    public float EpsilonDecay = 0.997f;
    public float StepSeconds = 5f;
    public float MatchSeconds = 600f;
}

[Serializable]
public class StrategicAction
{
    public StrategicActionKind Kind;
    public string StructureId;
    public int RecipeIndex;

    public StrategicAction(StrategicActionKind kind, string structureId = "", int recipeIndex = -1)
    {
        Kind = kind;
        StructureId = structureId ?? "";
        RecipeIndex = recipeIndex;
    }

    public string ToKey()
    {
        return $"{Kind}|{StructureId}|{RecipeIndex}";
    }

    public override string ToString()
    {
        if (Kind == StrategicActionKind.SetConverterRecipe)
            return $"{Kind}::{StructureId}::Recipe{RecipeIndex}";

        return string.IsNullOrEmpty(StructureId) ? Kind.ToString() : $"{Kind}::{StructureId}";
    }

    public static StrategicAction Wait()
    {
        return new StrategicAction(StrategicActionKind.Wait);
    }
}

public class StrategicObservation
{
    public Dictionary<ResourceType, int> Resources = new Dictionary<ResourceType, int>();
    public Dictionary<string, int> OwnedById = new Dictionary<string, int>();
    public Dictionary<string, int> PlacedById = new Dictionary<string, int>();
    public Dictionary<string, int> AvgPlacedLevelById = new Dictionary<string, int>();

    public Dictionary<string, int> ActiveConverterRecipeById = new Dictionary<string, int>();

    public float TimeLeftSeconds;
    public ResourceType TargetResource;
    public ResourceType DominantResource;
    public int AssemblyLineCount;
}