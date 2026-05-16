using System;
using UnityEngine;

public enum ResourceType
{
    None,
    Wood,
    Brick,
    Steel,
    Psi
}

public enum StructureType
{
    Producer,
    Collector,
    AssemblyLine,
    Converter,
    Observer,
    MagicalConverter,
    Delayer,
    Retriever
}

public enum Direction
{
    Up,
    Right,
    Down,
    Left
}

public enum ObserverActionType
{
    RouteToOutput,
    Discard,
    Hold
}

[Serializable]
public class ResourceOperation
{
    public Structure Source;
    public ResourceType Resource;
    public int Quantity;

    public ResourceOperation(Structure source, ResourceType resource, int quantity)
    {
        Source = source;
        Resource = resource;
        Quantity = quantity;
    }
}

[Serializable]
public class ConversionRecipe
{
    public ResourceType Input;
    public ResourceType Output;
    public int InputAmount;
    public int OutputAmount;

    public ConversionRecipe(ResourceType input, ResourceType output, int inputAmount, int outputAmount)
    {
        Input = input;
        Output = output;
        InputAmount = inputAmount;
        OutputAmount = outputAmount;
    }

    public string GetDisplayLabel(int effectiveInputCost)
    {
        return $"{effectiveInputCost} {Input} -> {OutputAmount} {Output}";
    }
}

[Serializable]
public class ObserverRule
{
    public ResourceType Resource;
    public ObserverActionType ActionType;
    public int OutputIndex;

    public ObserverRule(ResourceType resource, ObserverActionType actionType, int outputIndex)
    {
        Resource = resource;
        ActionType = actionType;
        OutputIndex = outputIndex;
    }
}

public interface IStructureBoard
{
    Structure GetStructureAt(Vector3Int cell);
}