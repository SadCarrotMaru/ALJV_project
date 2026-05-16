using System.Collections.Generic;

public static class StrategicActionCatalog
{
    public static List<StrategicAction> BuildActions(List<StructureDefinition> definitions)
    {
        List<StrategicAction> actions = new List<StrategicAction>
        {
            StrategicAction.Wait()
        };

        if (definitions == null)
            return actions;

        for (int i = 0; i < definitions.Count; i++)
        {
            StructureDefinition def = definitions[i];
            if (def == null)
                continue;

            string id = def.GetStableId();

            actions.Add(new StrategicAction(StrategicActionKind.BuyStructure, id));
            actions.Add(new StrategicAction(StrategicActionKind.PlaceStructure, id));
            actions.Add(new StrategicAction(StrategicActionKind.UpgradeStructure, id));

            if (def.StructureType == StructureType.Converter && def.GetConverterRecipeCount() > 1)
            {
                for (int r = 0; r < def.GetConverterRecipeCount(); r++)
                {
                    actions.Add(new StrategicAction(StrategicActionKind.SetConverterRecipe, id, r));
                }
            }
        }

        return actions;
    }
}