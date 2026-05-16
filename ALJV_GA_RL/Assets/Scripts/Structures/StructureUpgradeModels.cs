using System;
using System.Collections.Generic;

[Serializable]
public class UpgradeCostTier
{
    public int TargetLevel = 2;
    public List<ResourceAmount> Costs = new List<ResourceAmount>();
}