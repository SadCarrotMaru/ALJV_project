using System.Collections.Generic;
using UnityEngine;

public class GeneticBlueprintPlanner : MonoBehaviour
{
    [Header("Definitions")]
    [SerializeField] private List<StructureDefinition> allowedDefinitions = new List<StructureDefinition>();

    [Header("Target")]
    [SerializeField] private ResourceType targetResource = ResourceType.Wood;

    [Header("GA")]
    [SerializeField] private GeneticAlgorithmSettings settings = new GeneticAlgorithmSettings();
    [SerializeField] private GAFitnessWeights fitnessWeights = new GAFitnessWeights();

    [TextArea(10, 20)]
    [SerializeField] private string lastBestBlueprintText;

    private FabricBlueprint bestBlueprint;

    [ContextMenu("Run GA Evolution")]
    public void RunEvolution()
    {
        List<FabricBlueprint> population = SeedPopulation();
        EvaluatePopulation(population);

        for (int generation = 0; generation < settings.Generations; generation++)
        {
            List<FabricBlueprint> next = new List<FabricBlueprint>();

            FabricBlueprint elite = GetBest(population).Clone();
            next.Add(elite);

            while (next.Count < settings.PopulationSize)
            {
                FabricBlueprint parentA = Tournament(population);
                FabricBlueprint parentB = Tournament(population);

                FabricBlueprint child;
                if (Random.value < settings.CrossoverRate)
                    child = Crossover(parentA, parentB);
                else
                    child = parentA.Clone();

                Mutate(child);
                child.Fitness = BlueprintEvaluator.Evaluate(child, settings, fitnessWeights, targetResource);
                next.Add(child);
            }

            population = next;

            if ((generation + 1) % 10 == 0)
                Debug.Log($"GA generation {generation + 1}/{settings.Generations}");
        }

        bestBlueprint = GetBest(population).Clone();
        lastBestBlueprintText = BlueprintToText(bestBlueprint);

        Debug.Log("GA finished.\n" + lastBestBlueprintText);
    }

    public FabricBlueprint GetBestBlueprint()
    {
        return bestBlueprint;
    }

    private List<FabricBlueprint> SeedPopulation()
    {
        List<FabricBlueprint> population = new List<FabricBlueprint>();

        population.Add(CreateStarterTemplate(ResourceType.Wood));
        population.Add(CreateStarterTemplate(ResourceType.Brick));
        population.Add(CreateStarterTemplate(ResourceType.Steel));

        while (population.Count < settings.PopulationSize)
        {
            FabricBlueprint random = new FabricBlueprint();
            int geneCount = Random.Range(3, settings.MaxGenes + 1);

            for (int i = 0; i < geneCount; i++)
                random.Genes.Add(RandomGene());

            population.Add(random);
        }

        return population;
    }

    private FabricBlueprint CreateStarterTemplate(ResourceType resource)
    {
        FabricBlueprint blueprint = new FabricBlueprint();

        StructureDefinition producer = FindDefinition(StructureType.Producer, resource);
        StructureDefinition collector = FindDefinition(StructureType.Collector, resource);
        StructureDefinition line = FindDefinition(StructureType.AssemblyLine, ResourceType.None);

        if (producer != null)
        {
            blueprint.Genes.Add(new BlueprintGene
            {
                Definition = producer,
                LocalCell = new Vector2Int(1, 1),
                Direction = Direction.Right
            });
        }

        if (line != null)
        {
            blueprint.Genes.Add(new BlueprintGene
            {
                Definition = line,
                LocalCell = new Vector2Int(2, 1),
                Direction = Direction.Right
            });
        }

        if (collector != null)
        {
            blueprint.Genes.Add(new BlueprintGene
            {
                Definition = collector,
                LocalCell = new Vector2Int(3, 1),
                Direction = Direction.Right
            });
        }

        blueprint.Fitness = BlueprintEvaluator.Evaluate(blueprint, settings, fitnessWeights, targetResource);
        return blueprint;
    }

    private void EvaluatePopulation(List<FabricBlueprint> population)
    {
        for (int i = 0; i < population.Count; i++)
        {
            population[i].Fitness = BlueprintEvaluator.Evaluate(population[i], settings, fitnessWeights, targetResource);
        }
    }

    private FabricBlueprint GetBest(List<FabricBlueprint> population)
    {
        FabricBlueprint best = population[0];
        for (int i = 1; i < population.Count; i++)
        {
            if (population[i].Fitness > best.Fitness)
                best = population[i];
        }
        return best;
    }

    private FabricBlueprint Tournament(List<FabricBlueprint> population)
    {
        FabricBlueprint a = population[Random.Range(0, population.Count)];
        FabricBlueprint b = population[Random.Range(0, population.Count)];
        return a.Fitness >= b.Fitness ? a : b;
    }

    private FabricBlueprint Crossover(FabricBlueprint a, FabricBlueprint b)
    {
        FabricBlueprint child = new FabricBlueprint();

        int splitA = a.Genes.Count > 0 ? Random.Range(0, a.Genes.Count) : 0;
        int splitB = b.Genes.Count > 0 ? Random.Range(0, b.Genes.Count) : 0;

        for (int i = 0; i < splitA && i < a.Genes.Count; i++)
            child.Genes.Add(a.Genes[i].Clone());

        for (int i = splitB; i < b.Genes.Count; i++)
            child.Genes.Add(b.Genes[i].Clone());

        while (child.Genes.Count > settings.MaxGenes)
            child.Genes.RemoveAt(child.Genes.Count - 1);

        return child;
    }

    private void Mutate(FabricBlueprint blueprint)
    {
        if (blueprint == null)
            return;

        if (Random.value < settings.MutationRate && blueprint.Genes.Count < settings.MaxGenes)
            blueprint.Genes.Add(RandomGene());

        if (Random.value < settings.MutationRate && blueprint.Genes.Count > 1)
            blueprint.Genes.RemoveAt(Random.Range(0, blueprint.Genes.Count));

        for (int i = 0; i < blueprint.Genes.Count; i++)
        {
            if (Random.value < settings.MutationRate)
            {
                blueprint.Genes[i].LocalCell = new Vector2Int(
                    Random.Range(0, settings.GridWidth),
                    Random.Range(0, settings.GridHeight)
                );
            }

            if (Random.value < settings.MutationRate && blueprint.Genes[i].Definition != null)
            {
                StructureDefinition def = blueprint.Genes[i].Definition;

                if (def.StructureType == StructureType.Converter && def.GetConverterRecipeCount() > 0)
                {
                    blueprint.Genes[i].ConverterRecipeIndex = Random.Range(0, def.GetConverterRecipeCount());
                }
            }

            if (Random.value < settings.MutationRate && blueprint.Genes[i].Definition != null)
            {
                int maxLevel = Mathf.Max(1, blueprint.Genes[i].Definition.MaxLevel);
                blueprint.Genes[i].TargetLevel = Random.Range(1, maxLevel + 1);
            }

            if (Random.value < settings.MutationRate)
            {
                blueprint.Genes[i].Direction = (Direction)Random.Range(0, 4);
            }

            if (Random.value < settings.MutationRate)
            {
                blueprint.Genes[i].Definition = allowedDefinitions[Random.Range(0, allowedDefinitions.Count)];
            }
        }
    }

    private BlueprintGene RandomGene()
    {
        StructureDefinition def = allowedDefinitions[Random.Range(0, allowedDefinitions.Count)];

        int recipeIndex = 0;
        if (def != null && def.StructureType == StructureType.Converter && def.GetConverterRecipeCount() > 0)
        {
            recipeIndex = Random.Range(0, def.GetConverterRecipeCount());
        }

        return new BlueprintGene
        {
            Definition = def,
            LocalCell = new Vector2Int(Random.Range(0, settings.GridWidth), Random.Range(0, settings.GridHeight)),
            Direction = (Direction)Random.Range(0, 4),
            TargetLevel = def != null ? Random.Range(1, Mathf.Max(2, def.MaxLevel + 1)) : 1,
            ConverterRecipeIndex = recipeIndex
        };
    }

    private StructureDefinition FindDefinition(StructureType type, ResourceType variant)
    {
        for (int i = 0; i < allowedDefinitions.Count; i++)
        {
            StructureDefinition def = allowedDefinitions[i];
            if (def == null)
                continue;

            if (def.StructureType == type)
            {
                if (type == StructureType.Producer || type == StructureType.Collector)
                {
                    if (def.ResourceVariant == variant)
                        return def;
                }
                else
                {
                    return def;
                }
            }
        }

        return null;
    }

    private string BlueprintToText(FabricBlueprint blueprint)
    {
        if (blueprint == null)
            return "No blueprint.";

        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        sb.AppendLine($"Fitness: {blueprint.Fitness:F2}");

        for (int i = 0; i < blueprint.Genes.Count; i++)
        {
            BlueprintGene gene = blueprint.Genes[i];
            if (gene == null || gene.Definition == null)
                continue;

            sb.AppendLine($"{i + 1}. {gene.Definition.DisplayName} @ ({gene.LocalCell.x},{gene.LocalCell.y}) dir={gene.Direction}");
        }

        return sb.ToString();
    }
}