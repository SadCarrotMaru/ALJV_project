using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class BasicPipelineUIController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private BoardManager boardManager;
    [SerializeField] private FabricSimulationManager simulationManager;
    [SerializeField] private TilePlacementController placementController;

    [Header("UI")]
    [SerializeField] private Button buildButton;
    [SerializeField] private Button clearButton;
    [SerializeField] private Button pauseButton;
    [SerializeField] private TMP_Text statusText;
    [SerializeField] private TMP_Text collectorText;

    private void Awake()
    {
        if (buildButton != null)
            buildButton.onClick.AddListener(OnBuildPressed);

        if (clearButton != null)
            clearButton.onClick.AddListener(ClearBoard);

        if (pauseButton != null)
            pauseButton.onClick.AddListener(TogglePause);

        if (placementController != null)
            placementController.StatusChanged += UpdateStatus;
    }

    private void OnDestroy()
    {
        if (buildButton != null)
            buildButton.onClick.RemoveListener(OnBuildPressed);

        if (clearButton != null)
            clearButton.onClick.RemoveListener(ClearBoard);

        if (pauseButton != null)
            pauseButton.onClick.RemoveListener(TogglePause);

        if (placementController != null)
            placementController.StatusChanged -= UpdateStatus;
    }

    private void Start()
    {
        UpdateStatus("Ready.");
        UpdateInfoText();
    }

    private void Update()
    {
        UpdateInfoText();
    }

    private void OnBuildPressed()
    {
        if (placementController == null)
        {
            UpdateStatus("TilePlacementController is missing.");
            return;
        }

        placementController.BeginBuildMode();
    }

    public void ClearBoard()
    {
        Debug.Log("N ARE MA CUM GEN");
        if (boardManager == null)
        {
            UpdateStatus("BoardManager is missing.");
            return;
        }

        boardManager.ClearAllStructures();
        UpdateStatus("Board cleared.");
    }

    public void TogglePause()
    {
        if (simulationManager == null)
        {
            UpdateStatus("SimulationManager is missing.");
            return;
        }

        simulationManager.TogglePause();
        UpdateStatus(simulationManager.IsPaused ? "Simulation paused." : "Simulation running.");
    }

    private void UpdateInfoText()
    {
        if (collectorText == null)
            return;

        if (placementController == null)
        {
            collectorText.text = "Selected: -";
            return;
        }

        if (placementController.IsBuildMode)
        {
            collectorText.text = $"Build: {placementController.DebugSelectionLabel}\nClick a tile...";
        }
        else
        {
            collectorText.text = $"Selected: {placementController.DebugSelectionLabel}";
        }
    }

    private void UpdateStatus(string message)
    {
        if (statusText != null)
            statusText.text = message;
    }
}