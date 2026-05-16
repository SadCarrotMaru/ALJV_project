using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Game/Structure Definition", fileName = "StructureDefinition")]
public class StructureDefinition : ScriptableObject
{
    [Header("Identity")]
    public string Id;
    public string DisplayName;
    public Sprite Icon;

    [Header("Type")]
    public StructureType StructureType;
    public ResourceType ResourceVariant = ResourceType.None;

    [Header("Shop Cost")]
    public List<ResourceAmount> BuyCost = new List<ResourceAmount>();

    [Header("Upgrade")]
    public int MaxLevel = 1;
    public List<UpgradeCostTier> UpgradeCosts = new List<UpgradeCostTier>();

    [Header("Runtime Parameters")]
    public float ProducerRate = 1f;
    public float AssemblyLineTravelTime = 0.4f;
    public float DelayerInterval = 1f;

    [Header("Converter Defaults")]
    public List<ConversionRecipeConfig> ConverterRecipes = new List<ConversionRecipeConfig>();

    public string GetStableId()
    {
        return string.IsNullOrWhiteSpace(Id) ? name : Id;
    }

    public List<ResourceAmount> GetUpgradeCostForTargetLevel(int targetLevel)
    {
        for (int i = 0; i < UpgradeCosts.Count; i++)
        {
            if (UpgradeCosts[i] != null && UpgradeCosts[i].TargetLevel == targetLevel)
                return UpgradeCosts[i].Costs;
        }

        return null;
    }

    public int GetConverterRecipeCount()
    {
        return ConverterRecipes != null ? ConverterRecipes.Count : 0;
    }

    public Structure CreateStructureInstance()
    {
        Structure created = null;

        switch (StructureType)
        {
            case StructureType.Producer:
                created = new ProducerStructure(ResourceVariant, ProducerRate);
                break;

            case StructureType.Collector:
                created = new CollectorStructure(ResourceVariant);
                break;

            case StructureType.AssemblyLine:
                created = new AssemblyLineStructure(AssemblyLineTravelTime);
                break;

            case StructureType.Converter:
                {
                    List<ConversionRecipe> recipes = new List<ConversionRecipe>();

                    if (ConverterRecipes != null && ConverterRecipes.Count > 0)
                    {
                        for (int i = 0; i < ConverterRecipes.Count; i++)
                        {
                            recipes.Add(ConverterRecipes[i].ToRuntimeRecipe());
                        }
                    }
                    else
                    {
                        recipes.Add(new ConversionRecipe(ResourceType.Wood, ResourceType.Steel, 4, 1));
                    }

                    created = new ConverterStructure(recipes);
                    break;
                }

            case StructureType.Observer:
                created = new ObserverStructure();
                break;

            case StructureType.MagicalConverter:
                created = new MagicalConverterStructure();
                break;

            case StructureType.Delayer:
                created = new DelayerStructure(DelayerInterval);
                break;

            case StructureType.Retriever:
                created = new RetrieverStructure();
                break;
        }

        if (created != null)
        {
            created.SetOriginDefinition(this);
        }

        return created;
    }
}

[System.Serializable]
public class ConversionRecipeConfig
{
    public ResourceType Input = ResourceType.Wood;
    public ResourceType Output = ResourceType.Steel;
    public int InputAmount = 4;
    public int OutputAmount = 1;

    public ConversionRecipe ToRuntimeRecipe()
    {
        return new ConversionRecipe(Input, Output, InputAmount, OutputAmount);
    }
}