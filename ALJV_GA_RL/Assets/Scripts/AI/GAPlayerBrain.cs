using UnityEngine;

public class GAPlayerBrain : MonoBehaviour
{
    [SerializeField] private PlayerRuntime runtime;
    [SerializeField] private GeneticBlueprintPlanner planner;
    [SerializeField] private float decisionInterval = 1.0f;
    [SerializeField] private bool runPlannerOnStart = true;
    [SerializeField] private bool verboseLogging = true;

    private FabricBlueprint activeBlueprint;
    private float timer;

    public string LastActionText { get; private set; } = "None";

    private void Start()
    {
        if (runPlannerOnStart && planner != null)
        {
            planner.RunEvolution();
            activeBlueprint = planner.GetBestBlueprint();

            if (verboseLogging)
                Debug.Log($"[GA:{runtime.PlayerName}] Planner generated blueprint.");
        }
    }

    private void Update()
    {
        if (runtime == null || runtime.Clock == null || runtime.Clock.IsFinished)
            return;

        timer += Time.deltaTime;
        if (timer < decisionInterval)
            return;

        timer = 0f;
        ExecuteNextBlueprintStep();
    }

    private void ExecuteNextBlueprintStep()
    {
        if (activeBlueprint == null || activeBlueprint.Genes == null || activeBlueprint.Genes.Count == 0)
        {
            LastActionText = "No blueprint";
            return;
        }

        Vector3Int origin = runtime.Board.GetPlayableOriginCell();

        for (int i = 0; i < activeBlueprint.Genes.Count; i++)
        {
            BlueprintGene gene = activeBlueprint.Genes[i];
            if (gene == null || gene.Definition == null)
                continue;

            Vector3Int cell = origin + new Vector3Int(gene.LocalCell.x, gene.LocalCell.y, 0);

            if (!runtime.Board.IsWithinPlayableArea(cell))
                continue;

            Structure existing = runtime.Board.GetStructureAt(cell);

            if (existing != null && existing.OriginDefinition == gene.Definition)
            {
                bool changedRecipe = false;

                if (existing is ConverterStructure)
                {
                    changedRecipe = runtime.TrySetConverterRecipeAtCell(cell, gene.ConverterRecipeIndex);
                }

                if (existing.Level < gene.TargetLevel)
                {
                    bool upgraded = runtime.TryUpgradeAtCell(cell);

                    if (upgraded)
                    {
                        LastActionText = $"Upgrade {gene.Definition.DisplayName} at {cell}";
                        if (verboseLogging)
                            Debug.Log($"[GA:{runtime.PlayerName}] {LastActionText}");
                        return;
                    }

                    continue;
                }

                if (changedRecipe)
                {
                    LastActionText = $"Set recipe for {gene.Definition.DisplayName} at {cell}";
                    if (verboseLogging)
                        Debug.Log($"[GA:{runtime.PlayerName}] {LastActionText}");
                    return;
                }

                continue;
            }

            if (existing != null)
                continue;

            if (runtime.Inventory.GetOwnedCount(gene.Definition) <= 0)
            {
                bool bought = runtime.TryBuy(gene.Definition);

                if (bought)
                {
                    LastActionText = $"Buy {gene.Definition.DisplayName}";
                    if (verboseLogging)
                        Debug.Log($"[GA:{runtime.PlayerName}] {LastActionText}");
                    return;
                }

                continue;
            }

            bool placed = runtime.TryPlaceOwned(gene.Definition, cell, gene.Direction);

            if (placed)
            {
                if (gene.Definition.StructureType == StructureType.Converter)
                {
                    runtime.TrySetConverterRecipeAtCell(cell, gene.ConverterRecipeIndex);
                }

                LastActionText = $"Place {gene.Definition.DisplayName} at {cell}";
                if (verboseLogging)
                    Debug.Log($"[GA:{runtime.PlayerName}] {LastActionText}");
                return;
            }

        }

        for (int i = 0; i < runtime.StructureDefinitions.Count; i++)
        {
            StructureDefinition def = runtime.StructureDefinitions[i];
            if (def == null)
                continue;

            if (runtime.TryUpgradeBestPlaced(def))
            {
                LastActionText = $"Fallback upgrade {def.DisplayName}";
                if (verboseLogging)
                    Debug.Log($"[GA:{runtime.PlayerName}] {LastActionText}");
                return;
            }
        }

        LastActionText = "No feasible action";
    }
}