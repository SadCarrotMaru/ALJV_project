using UnityEngine;

public class FabricSimulationManager : MonoBehaviour
{
    [SerializeField] private BoardManager boardManager;
    [SerializeField] private float simulationDeltaTime = 0.2f;
    [SerializeField] private bool isPaused = false;

    public bool IsPaused => isPaused;

    public void SetPaused(bool paused)
    {
        isPaused = paused;
    }

    public void TogglePause()
    {
        isPaused = !isPaused;
    }

    private void FixedUpdate()
    {
        if (isPaused || boardManager == null)
            return;

        var structures = boardManager.ActiveStructures;

        for (int i = 0; i < structures.Count; i++)
        {
            if (structures[i] != null)
            {
                structures[i].SimulationTick(simulationDeltaTime);
            }
        }
    }
}