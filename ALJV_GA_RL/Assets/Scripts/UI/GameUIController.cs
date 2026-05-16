using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GameUIController : MonoBehaviour
{
    [Header("Core References")]
    [SerializeField] private BoardManager boardManager;
    [SerializeField] private FabricSimulationManager simulationManager;
    [SerializeField] private TilePlacementController placementController;
    [SerializeField] private PlayerInventory playerInventory;
    [SerializeField] private ShopManager shopManager;

    [Header("HUD Buttons")]
    [SerializeField] private Button buildButton;
    [SerializeField] private Button shopButton;
    [SerializeField] private Button clearButton;
    [SerializeField] private Button pauseButton;

    [Header("HUD Text")]
    [SerializeField] private TMP_Text statusText;

    [Header("Resource UI")]
    [SerializeField] private Transform resourceCounterRoot;
    [SerializeField] private ResourceCounterUI resourceCounterPrefab;
    [SerializeField] private List<ResourceIconEntry> resourceIcons = new List<ResourceIconEntry>();
    [SerializeField]
    private List<ResourceType> shownResources = new List<ResourceType>
    {
        ResourceType.Wood,
        ResourceType.Brick,
        ResourceType.Steel,
        ResourceType.Psi
    };

    [Header("Structure Inventory UI")]
    [SerializeField] private Transform inventoryRoot;
    [SerializeField] private StructureInventoryItemUI inventoryItemPrefab;
    [SerializeField] private List<StructureDefinition> inventoryDefinitions = new List<StructureDefinition>();

    [Header("Panels / Visibility")]
    [SerializeField] private GameObject inventoryPanelObject;
    [SerializeField] private GameObject clearButtonObject;
    [SerializeField] private GameObject pauseButtonObject;
    [SerializeField] private GameObject buildButtonObject;

    private readonly Dictionary<ResourceType, Sprite> resourceIconMap = new Dictionary<ResourceType, Sprite>();
    [SerializeField] private StructureDetailsPanelUI detailsPanel;

    private void Awake()
    {
        if (buildButton != null)
            buildButton.onClick.AddListener(OnBuildPressed);

        if (shopButton != null)
            shopButton.onClick.AddListener(OnShopPressed);

        if (clearButton != null)
            clearButton.onClick.AddListener(OnClearPressed);

        if (pauseButton != null)
            pauseButton.onClick.AddListener(OnPausePressed);

        if (placementController != null)
            placementController.StatusChanged += UpdateStatus;

        if (shopManager != null)
        {
            shopManager.StatusChanged += UpdateStatus;
            shopManager.ShopOpenChanged += OnShopOpenChanged;
        }
    }

    private void OnDestroy()
    {
        if (buildButton != null)
            buildButton.onClick.RemoveListener(OnBuildPressed);

        if (shopButton != null)
            shopButton.onClick.RemoveListener(OnShopPressed);

        if (clearButton != null)
            clearButton.onClick.RemoveListener(OnClearPressed);

        if (pauseButton != null)
            pauseButton.onClick.RemoveListener(OnPausePressed);

        if (placementController != null)
            placementController.StatusChanged -= UpdateStatus;

        if (shopManager != null)
        {
            shopManager.StatusChanged -= UpdateStatus;
            shopManager.ShopOpenChanged -= OnShopOpenChanged;
        }
    }

    private void Start()
    {
        BuildResourceCounters();
        BuildStructureInventory();
        UpdateStatus("Ready.");
    }

    private void OnBuildPressed()
    {
        if (placementController == null)
        {
            UpdateStatus("Placement controller is missing.");
            return;
        }

        placementController.BeginBuildMode();
    }

    private void OnShopOpenChanged(bool isOpen)
    {
        if (isOpen && detailsPanel != null)
            detailsPanel.HidePanel();

        if (inventoryPanelObject != null)
            inventoryPanelObject.SetActive(!isOpen);

        if (clearButtonObject != null)
            clearButtonObject.SetActive(!isOpen);

        if (pauseButtonObject != null)
            pauseButtonObject.SetActive(!isOpen);

        if (buildButtonObject != null)
            buildButtonObject.SetActive(!isOpen);

        if (isOpen && placementController != null && placementController.IsBuildMode)
        {
            placementController.CancelBuildMode();
        }
    }
    private void OnShopPressed()
    {
        if (shopManager == null)
        {
            UpdateStatus("Shop manager is missing.");
            return;
        }

        shopManager.ToggleShop();
    }

    private void OnClearPressed()
    {
        if (boardManager == null)
        {
            UpdateStatus("Board manager is missing.");
            return;
        }

        if (detailsPanel != null)
            detailsPanel.HidePanel();

        if (playerInventory == null)
        {
            UpdateStatus("Player inventory is missing.");
            return;
        }

        boardManager.ClearAllStructuresToInventory(playerInventory);
        UpdateStatus("Board cleared and resources returned to inventory.");
        boardManager.ClearAllStructures();
    }

    private void OnPausePressed()
    {
        if (simulationManager == null)
        {
            UpdateStatus("Simulation manager is missing.");
            return;
        }

        simulationManager.TogglePause();
        UpdateStatus(simulationManager.IsPaused ? "Simulation paused." : "Simulation running.");
    }

    private void BuildResourceCounters()
    {
        if (resourceCounterRoot == null || resourceCounterPrefab == null || playerInventory == null)
            return;

        resourceIconMap.Clear();

        for (int i = 0; i < resourceIcons.Count; i++)
        {
            ResourceIconEntry entry = resourceIcons[i];
            if (entry != null && !resourceIconMap.ContainsKey(entry.Resource))
            {
                resourceIconMap.Add(entry.Resource, entry.Icon);
            }
        }

        for (int i = resourceCounterRoot.childCount - 1; i >= 0; i--)
        {
            Destroy(resourceCounterRoot.GetChild(i).gameObject);
        }

        for (int i = 0; i < shownResources.Count; i++)
        {
            ResourceType resource = shownResources[i];
            Sprite icon = resourceIconMap.TryGetValue(resource, out Sprite sprite) ? sprite : null;

            ResourceCounterUI counter = Instantiate(resourceCounterPrefab, resourceCounterRoot, false);
            counter.Setup(playerInventory, resource, icon);
        }
    }

    private void BuildStructureInventory()
    {
        if (inventoryRoot == null || inventoryItemPrefab == null || playerInventory == null || placementController == null)
            return;

        for (int i = inventoryRoot.childCount - 1; i >= 0; i--)
        {
            Destroy(inventoryRoot.GetChild(i).gameObject);
        }

        for (int i = 0; i < inventoryDefinitions.Count; i++)
        {
            StructureDefinition definition = inventoryDefinitions[i];
            if (definition == null)
                continue;

            StructureInventoryItemUI item = Instantiate(inventoryItemPrefab, inventoryRoot, false);
            item.Setup(definition, playerInventory, placementController);
        }
    }

    private void UpdateStatus(string message)
    {
        if (statusText != null)
            statusText.text = message;

        Debug.Log(message);
    }
}