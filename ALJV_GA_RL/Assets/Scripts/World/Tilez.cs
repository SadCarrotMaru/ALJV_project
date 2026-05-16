using System;
using UnityEngine;

[Serializable]
public class Tilez
{
    public Vector3Int CellPosition;
    public Structure PlacedStructure;

    public bool IsOccupied => PlacedStructure != null;

    public Tilez(Vector3Int cellPosition)
    {
        CellPosition = cellPosition;
    }

    public bool TryPlaceStructure(Structure structure)
    {
        if (structure == null || PlacedStructure != null)
            return false;

        PlacedStructure = structure;
        structure.SetCell(CellPosition);
        return true;
    }

    public void RemoveStructure()
    {
        if (PlacedStructure != null)
        {
            PlacedStructure.SetCell(null);
            PlacedStructure = null;
        }
    }
}