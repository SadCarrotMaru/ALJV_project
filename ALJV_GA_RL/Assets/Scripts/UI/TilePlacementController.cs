using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class TilePlacementController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Camera mainCamera;
    [SerializeField] private BoardManager boardManager;
    [SerializeField] private PlayerInventory playerInventory;
    [SerializeField] private StructureDetailsPanelUI detailsPanel;

    [Header("Input Actions (New Input System)")]
    [SerializeField] private InputActionReference pointAction;
    [SerializeField] private InputActionReference clickAction;
    [SerializeField] private InputActionReference cancelAction;

    [Header("Placement")]
    [SerializeField] private Direction placementDirection = Direction.Right;

    private bool isBuildMode;
    private Vector3Int hoveredCell;
    private bool hoveredCellValid;

    private bool pendingPrimaryClick;
    private bool pendingCancelRequest;
    private bool ignoreNextPlacementClick;

    private StructureDefinition selectedDefinition;

    public bool IsBuildMode => isBuildMode;
    public StructureDefinition SelectedDefinition => selectedDefinition;

    public string DebugSelectionLabel => selectedDefinition != null ? selectedDefinition.DisplayName : "None";

    public event Action<string> StatusChanged;
    public event Action<StructureDefinition> SelectionChanged;

    private void OnEnable()
    {
        if (pointAction != null) pointAction.action.Enable();
        if (clickAction != null) clickAction.action.Enable();
        if (cancelAction != null) cancelAction.action.Enable();

        if (clickAction != null)
            clickAction.action.performed += OnClickPerformed;

        if (cancelAction != null)
            cancelAction.action.performed += OnCancelPerformed;
    }

    private void OnDisable()
    {
        if (clickAction != null)
            clickAction.action.performed -= OnClickPerformed;

        if (cancelAction != null)
            cancelAction.action.performed -= OnCancelPerformed;

        if (pointAction != null) pointAction.action.Disable();
        if (clickAction != null) clickAction.action.Disable();
        if (cancelAction != null) cancelAction.action.Disable();
    }

    private void Update()
    {
        if (boardManager == null || mainCamera == null || pointAction == null)
            return;

        UpdateHoveredCell();

        if (isBuildMode)
        {
            boardManager.ShowPreview(hoveredCell, hoveredCellValid);
        }
        else
        {
            boardManager.ClearPreview();
        }

        if (pendingCancelRequest)
        {
            pendingCancelRequest = false;

            if (isBuildMode)
                CancelBuildMode();

            return;
        }

        if (!pendingPrimaryClick)
            return;

        pendingPrimaryClick = false;

        if (ignoreNextPlacementClick)
        {
            ignoreNextPlacementClick = false;
            return;
        }

        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            return;

        if (isBuildMode)
        {
            HandleBuildClick();
        }
        else
        {
            HandleInspectClick();
        }
    }

    public void SelectStructureDefinition(StructureDefinition definition)
    {
        selectedDefinition = definition;
        SelectionChanged?.Invoke(selectedDefinition);

        if (selectedDefinition != null)
            RaiseStatus($"Selected {selectedDefinition.DisplayName}.");
        else
            RaiseStatus("Selection cleared.");
    }

    public void BeginBuildMode()
    {
        if (boardManager == null)
        {
            RaiseStatus("BoardManager is missing.");
            return;
        }

        if (selectedDefinition == null)
        {
            RaiseStatus("Select a structure first.");
            return;
        }

        if (playerInventory == null || playerInventory.GetOwnedCount(selectedDefinition) <= 0)
        {
            RaiseStatus("You do not own any copies of the selected structure.");
            return;
        }

        if (detailsPanel != null)
            detailsPanel.HidePanel();

        isBuildMode = true;
        pendingPrimaryClick = false;
        pendingCancelRequest = false;
        ignoreNextPlacementClick = true;

        RaiseStatus($"Build mode enabled. Click a tile to place {selectedDefinition.DisplayName}.");
    }

    public void CancelBuildMode()
    {
        isBuildMode = false;
        pendingPrimaryClick = false;

        if (boardManager != null)
            boardManager.ClearPreview();

        RaiseStatus("Build mode cancelled.");
    }

    public void SetPlacementDirection(Direction direction)
    {
        placementDirection = direction;
    }

    private void UpdateHoveredCell()
    {
        Vector2 screenPosition = pointAction.action.ReadValue<Vector2>();

        Vector3 worldPosition = mainCamera.ScreenToWorldPoint(
            new Vector3(screenPosition.x, screenPosition.y, -mainCamera.transform.position.z)
        );

        hoveredCell = boardManager.WorldToCell(worldPosition);
        hoveredCellValid = boardManager.IsValidBuildCell(hoveredCell) && !boardManager.HasStructureAt(hoveredCell);
    }

    private void OnClickPerformed(InputAction.CallbackContext context)
    {
        pendingPrimaryClick = true;
    }

    private void OnCancelPerformed(InputAction.CallbackContext context)
    {
        pendingCancelRequest = true;
    }

    private void HandleBuildClick()
    {
        if (selectedDefinition == null)
        {
            RaiseStatus("No structure selected.");
            return;
        }

        if (playerInventory == null || playerInventory.GetOwnedCount(selectedDefinition) <= 0)
        {
            RaiseStatus("You do not own any copies of the selected structure.");
            return;
        }

        if (!hoveredCellValid)
        {
            RaiseStatus("Invalid tile for placement.");
            return;
        }

        Structure structureToPlace = selectedDefinition.CreateStructureInstance();
        if (structureToPlace == null)
        {
            RaiseStatus("Could not create structure instance.");
            return;
        }

        structureToPlace.SetOutputDirection(placementDirection);

        bool placed = boardManager.TryPlaceStructure(hoveredCell, structureToPlace);

        if (!placed)
        {
            RaiseStatus("Placement failed.");
            return;
        }

        bool consumed = playerInventory.ConsumeStructure(selectedDefinition, 1);
        if (!consumed)
        {
            RaiseStatus("Placed structure, but inventory consume failed.");
        }

        isBuildMode = false;
        boardManager.ClearPreview();
        RaiseStatus($"Placed {selectedDefinition.DisplayName} at {hoveredCell}.");
    }

    private void HandleInspectClick()
    {
        if (!boardManager.IsValidBuildCell(hoveredCell))
        {
            if (detailsPanel != null)
                detailsPanel.HidePanel();

            return;
        }

        Structure clickedStructure = boardManager.GetStructureAt(hoveredCell);

        if (clickedStructure != null)
        {
            if (detailsPanel != null)
            {
                Debug.Log($"Showing details for {clickedStructure.GetDisplayName()} at {hoveredCell}.");
                detailsPanel.ShowStructure(clickedStructure);
            }
            
            RaiseStatus($"Viewing {clickedStructure.GetDisplayName()}.");
        }
        else
        {
            if (detailsPanel != null)
                detailsPanel.HidePanel();
        }
    }

    private void RaiseStatus(string message)
    {
        StatusChanged?.Invoke(message);
        Debug.Log(message);
    }
}