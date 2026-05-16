using UnityEngine;

public class CollectorPayoutManager : MonoBehaviour
{
    [SerializeField] private BoardManager boardManager;
    [SerializeField] private PlayerInventory playerInventory;
    [SerializeField] private float payoutInterval = 0.5f;

    private float timer;

    private void Update()
    {
        if (boardManager == null || playerInventory == null)
            return;

        timer += Time.deltaTime;
        if (timer < payoutInterval)
            return;

        timer = 0f;
        PayoutCollectors();
    }

    private void PayoutCollectors()
    {
        var structures = boardManager.ActiveStructures;

        for (int i = 0; i < structures.Count; i++)
        {
            if (structures[i] is CollectorStructure collector)
            {
                ResourceType resource = collector.AcceptedResource;
                int stored = collector.GetStoredResource(resource);

                if (stored <= 0)
                    continue;

                int taken = collector.Take(resource, stored);
                if (taken > 0)
                {
                    playerInventory.AddResource(resource, taken);
                }
            }
        }
    }
}