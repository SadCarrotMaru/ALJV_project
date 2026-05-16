using System.Collections.Generic;
using UnityEngine;

public class ConverterStructure : Structure
{
    private readonly List<ConversionRecipe> recipes;
    private int activeRecipeIndex;
    private readonly float conversionInterval;
    private float conversionTimer;

    public IReadOnlyList<ConversionRecipe> Recipes => recipes;
    public int ActiveRecipeIndex => activeRecipeIndex;

    public ConverterStructure(List<ConversionRecipe> recipes, int activeRecipeIndex = 0, float conversionInterval = 1f)
    {
        structureType = StructureType.Converter;
        this.recipes = recipes ?? new List<ConversionRecipe>();
        this.activeRecipeIndex = Mathf.Clamp(activeRecipeIndex, 0, Mathf.Max(0, this.recipes.Count - 1));
        this.conversionInterval = conversionInterval;
    }

    public override bool CanReceive(ResourceOperation operation)
    {
        if (operation == null || operation.Quantity <= 0)
            return false;

        ConversionRecipe recipe = GetActiveRecipe();
        if (recipe == null)
            return false;

        return operation.Resource == recipe.Input;
    }

    protected override void HandleIncomingOperation(ResourceOperation operation)
    {
        AddStoredResource(operation.Resource, operation.Quantity);
    }

    protected override void OnTick(float deltaTime)
    {
        if (recipes.Count == 0)
            return;

        conversionTimer += deltaTime;
        if (conversionTimer < conversionInterval)
            return;

        conversionTimer = 0f;
        ProcessRecipe();
    }

    private void ProcessRecipe()
    {
        ConversionRecipe recipe = GetActiveRecipe();
        if (recipe == null)
            return;

        int effectiveInputCost = GetEffectiveInputCost(recipe.InputAmount);

        int availableInput = GetStoredResource(recipe.Input);
        int possibleBatches = availableInput / effectiveInputCost;

        if (possibleBatches <= 0)
            return;

        int totalInputNeeded = possibleBatches * effectiveInputCost;
        int totalOutputProduced = possibleBatches * recipe.OutputAmount;

        if (!ConsumeStoredResource(recipe.Input, totalInputNeeded))
            return;

        bool sent = outputs.Count > 0 && TrySend(recipe.Output, totalOutputProduced, 0);

        if (!sent)
        {
            DiscardOperation(new ResourceOperation(this, recipe.Output, totalOutputProduced));
        }
    }

    public ConversionRecipe GetActiveRecipe()
    {
        if (recipes == null || recipes.Count == 0)
            return null;

        return recipes[Mathf.Clamp(activeRecipeIndex, 0, recipes.Count - 1)];
    }

    public int GetEffectiveInputCost(int baseInputCost)
    {
        return Mathf.Max(1, baseInputCost - (level - 1));
    }

    public bool TrySetActiveRecipe(int index)
    {
        if (recipes == null || recipes.Count == 0)
            return false;

        if (index < 0 || index >= recipes.Count)
            return false;

        activeRecipeIndex = index;
        return true;
    }

    public int GetPendingInputUnits()
    {
        ConversionRecipe recipe = GetActiveRecipe();
        if (recipe == null)
            return 0;

        int total = 0;

        foreach (ResourceOperation op in incomingOperations)
        {
            if (op != null && op.Resource == recipe.Input)
                total += op.Quantity;
        }

        return total;
    }

    public List<string> GetRecipeOptionLabels()
    {
        List<string> labels = new List<string>();

        if (recipes == null)
            return labels;

        for (int i = 0; i < recipes.Count; i++)
        {
            ConversionRecipe recipe = recipes[i];
            if (recipe == null)
            {
                labels.Add("Invalid Recipe");
                continue;
            }

            labels.Add(recipe.GetDisplayLabel(GetEffectiveInputCost(recipe.InputAmount)));
        }

        return labels;
    }

    protected override void AppendDetailLines(List<string> lines)
    {
        lines.Add($"Conversion interval: {conversionInterval:0.##}s");

        ConversionRecipe active = GetActiveRecipe();
        if (active != null)
        {
            lines.Add($"Active recipe: {active.Input} -> {active.Output}");
            lines.Add($"Effective cost: {GetEffectiveInputCost(active.InputAmount)} for {active.OutputAmount}");
            lines.Add($"Stored {active.Input}: {GetStoredResource(active.Input)}");
            lines.Add($"Queued {active.Input}: {GetPendingInputUnits()}");
        }
    }
}