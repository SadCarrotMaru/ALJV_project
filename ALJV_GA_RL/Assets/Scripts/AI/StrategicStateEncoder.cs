using System.Collections.Generic;
using System.Text;

public static class StrategicStateEncoder
{
    public static string BuildKey(StrategicObservation observation, List<StructureDefinition> orderedDefinitions)
    {
        StringBuilder sb = new StringBuilder();

        sb.Append("T:").Append(BinTime(observation.TimeLeftSeconds)).Append('|');
        sb.Append("TR:").Append(observation.TargetResource).Append('|');
        sb.Append("DR:").Append(observation.DominantResource).Append('|');

        sb.Append("RW:").Append(Bin(GetResource(observation, ResourceType.Wood))).Append('|');
        sb.Append("RB:").Append(Bin(GetResource(observation, ResourceType.Brick))).Append('|');
        sb.Append("RS:").Append(Bin(GetResource(observation, ResourceType.Steel))).Append('|');
        sb.Append("RP:").Append(Bin(GetResource(observation, ResourceType.Psi))).Append('|');

        sb.Append("L:").Append(BinSmall(observation.AssemblyLineCount)).Append('|');

        if (orderedDefinitions != null)
        {
            for (int i = 0; i < orderedDefinitions.Count; i++)
            {
                StructureDefinition def = orderedDefinitions[i];
                if (def == null)
                    continue;

                string id = def.GetStableId();

                int owned = observation.OwnedById.TryGetValue(id, out int o) ? o : 0;
                int placed = observation.PlacedById.TryGetValue(id, out int p) ? p : 0;
                int avgLevel = observation.AvgPlacedLevelById.TryGetValue(id, out int l) ? l : 0;

                sb.Append(id).Append("_O:").Append(BinSmall(owned)).Append('|');
                sb.Append(id).Append("_P:").Append(BinSmall(placed)).Append('|');
                sb.Append(id).Append("_L:").Append(BinLevel(avgLevel)).Append('|');

                if (def.StructureType == StructureType.Converter)
                {
                    int recipe = observation.ActiveConverterRecipeById.TryGetValue(id, out int r) ? r : 0;
                    sb.Append(id).Append("_R:").Append(recipe).Append('|');
                }
            }
        }

        return sb.ToString();
    }

    private static int GetResource(StrategicObservation observation, ResourceType type)
    {
        return observation.Resources.TryGetValue(type, out int value) ? value : 0;
    }

    private static int Bin(int value)
    {
        if (value <= 0) return 0;
        if (value <= 4) return 1;
        if (value <= 9) return 2;
        if (value <= 19) return 3;
        if (value <= 39) return 4;
        return 5;
    }

    private static int BinSmall(int value)
    {
        if (value <= 0) return 0;
        if (value == 1) return 1;
        if (value <= 3) return 2;
        if (value <= 6) return 3;
        return 4;
    }

    private static int BinLevel(int value)
    {
        if (value <= 0) return 0;
        if (value == 1) return 1;
        if (value == 2) return 2;
        return 3;
    }

    private static int BinTime(float secondsLeft)
    {
        if (secondsLeft <= 60f) return 0;
        if (secondsLeft <= 180f) return 1;
        if (secondsLeft <= 300f) return 2;
        if (secondsLeft <= 480f) return 3;
        return 4;
    }
}