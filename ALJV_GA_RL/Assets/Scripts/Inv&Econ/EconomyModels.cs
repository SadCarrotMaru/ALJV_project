using System;
using UnityEngine;

[Serializable]
public class ResourceAmount
{
    public ResourceType Resource;
    public int Amount;
}

[Serializable]
public class StructureStockEntry
{
    public StructureDefinition Definition;
    public int Count;
}

[Serializable]
public class ResourceIconEntry
{
    public ResourceType Resource;
    public Sprite Icon;
}