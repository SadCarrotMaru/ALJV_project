using System;
using System.Collections.Generic;
using UnityEngine;

public class PlayerInventory : MonoBehaviour
{
    [Header("Starting Resources")]
    [SerializeField] private List<ResourceAmount> startingResources = new List<ResourceAmount>();

    [Header("Starting Structures")]
    [SerializeField] private List<StructureStockEntry> startingStructures = new List<StructureStockEntry>();

    private readonly Dictionary<ResourceType, int> resourceMap = new Dictionary<ResourceType, int>();
    private readonly Dictionary<StructureDefinition, int> structureMap = new Dictionary<StructureDefinition, int>();

    public event Action OnInventoryChanged;

    private void Awake()
    {
        InitializeResources();
        InitializeStructures();
    }

    private void InitializeResources()
    {
        resourceMap.Clear();

        foreach (ResourceType resource in Enum.GetValues(typeof(ResourceType)))
        {
            if (resource == ResourceType.None)
                continue;

            resourceMap[resource] = 0;
        }

        for (int i = 0; i < startingResources.Count; i++)
        {
            ResourceAmount entry = startingResources[i];
            if (entry == null || entry.Resource == ResourceType.None)
                continue;

            resourceMap[entry.Resource] += Mathf.Max(0, entry.Amount);
        }
    }

    private void InitializeStructures()
    {
        structureMap.Clear();

        for (int i = 0; i < startingStructures.Count; i++)
        {
            StructureStockEntry entry = startingStructures[i];
            if (entry == null || entry.Definition == null)
                continue;

            structureMap[entry.Definition] = Mathf.Max(0, entry.Count);
        }
    }

    public int GetResource(ResourceType resource)
    {
        if (resource == ResourceType.None)
            return 0;

        return resourceMap.TryGetValue(resource, out int amount) ? amount : 0;
    }

    public void AddResource(ResourceType resource, int amount)
    {
        if (resource == ResourceType.None || amount <= 0)
            return;

        if (!resourceMap.ContainsKey(resource))
            resourceMap[resource] = 0;

        Debug.Log($"Adding {amount} of {resource} to inventory.");

        resourceMap[resource] += amount;
        OnInventoryChanged?.Invoke();
    }

    public bool CanAfford(List<ResourceAmount> costs)
    {
        if (costs == null)
            return true;

        for (int i = 0; i < costs.Count; i++)
        {
            ResourceAmount cost = costs[i];
            if (cost == null || cost.Resource == ResourceType.None)
                continue;

            if (GetResource(cost.Resource) < cost.Amount)
                return false;
        }

        return true;
    }

    public bool SpendResources(List<ResourceAmount> costs)
    {
        if (!CanAfford(costs))
            return false;

        if (costs != null)
        {
            for (int i = 0; i < costs.Count; i++)
            {
                ResourceAmount cost = costs[i];
                if (cost == null || cost.Resource == ResourceType.None)
                    continue;

                resourceMap[cost.Resource] -= Mathf.Max(0, cost.Amount);
            }
        }

        OnInventoryChanged?.Invoke();
        return true;
    }

    public int GetOwnedCount(StructureDefinition definition)
    {
        if (definition == null)
            return 0;

        return structureMap.TryGetValue(definition, out int amount) ? amount : 0;
    }

    public void AddStructure(StructureDefinition definition, int amount = 1)
    {
        if (definition == null || amount <= 0)
            return;

        if (!structureMap.ContainsKey(definition))
            structureMap[definition] = 0;

        structureMap[definition] += amount;
        OnInventoryChanged?.Invoke();
    }

    public bool ConsumeStructure(StructureDefinition definition, int amount = 1)
    {
        if (definition == null || amount <= 0)
            return false;

        int current = GetOwnedCount(definition);
        if (current < amount)
            return false;

        structureMap[definition] = current - amount;
        OnInventoryChanged?.Invoke();
        return true;
    }

    public bool TryBuyStructure(StructureDefinition definition, int amount = 1)
    {
        if (definition == null || amount <= 0)
            return false;

        for (int i = 0; i < amount; i++)
        {
            if (!SpendResources(definition.BuyCost))
                return false;

            AddStructure(definition, 1);
        }

        return true;
    }
    public Dictionary<ResourceType, int> GetResourceSnapshot()
    {
        return new Dictionary<ResourceType, int>(resourceMap);
    }

    public Dictionary<StructureDefinition, int> GetStructureSnapshot()
    {
        return new Dictionary<StructureDefinition, int>(structureMap);
    }
}