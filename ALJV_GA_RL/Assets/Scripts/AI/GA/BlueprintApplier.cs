using UnityEngine;

public class BlueprintApplier : MonoBehaviour
{
    [SerializeField] private BoardManager boardManager;
    [SerializeField] private GeneticBlueprintPlanner planner;
    [SerializeField] private Vector3Int origin = new Vector3Int(0, 0, 0);

    [ContextMenu("Apply Best Blueprint")]
    public void ApplyBestBlueprint()
    {
        if (boardManager == null || planner == null)
            return;

        FabricBlueprint blueprint = planner.GetBestBlueprint();
        if (blueprint == null)
        {
            Debug.LogWarning("No blueprint available. Run GA first.");
            return;
        }

        for (int i = 0; i < blueprint.Genes.Count; i++)
        {
            BlueprintGene gene = blueprint.Genes[i];
            if (gene == null || gene.Definition == null)
                continue;

            Vector3Int cell = origin + new Vector3Int(gene.LocalCell.x, gene.LocalCell.y, 0);

            if (!boardManager.IsValidBuildCell(cell) || boardManager.HasStructureAt(cell))
                continue;

            Structure structure = gene.Definition.CreateStructureInstance();
            structure.SetOutputDirection(gene.Direction);
            boardManager.TryPlaceStructure(cell, structure);
        }
    }
}