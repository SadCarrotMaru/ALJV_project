using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class TilemapBuildInput : MonoBehaviour
{
    [SerializeField] private Camera mainCamera;
    [SerializeField] private BoardManager boardManager;

    [Header("Input Actions")]
    [SerializeField] private InputActionReference pointAction;
    [SerializeField] private InputActionReference placeAction;
    [SerializeField] private InputActionReference removeAction;
    [SerializeField] private InputActionReference rotateAction;

    private StructureType selectedStructureType = StructureType.Producer;
    private Direction placementDirection = Direction.Right;
    private Vector3Int currentHoveredCell;

    private void OnEnable()
    {
        pointAction.action.Enable();
        placeAction.action.Enable();
        removeAction.action.Enable();
        rotateAction.action.Enable();

        placeAction.action.performed += OnPlacePerformed;
        removeAction.action.performed += OnRemovePerformed;
        rotateAction.action.performed += OnRotatePerformed;
    }

    private void OnDisable()
    {
        placeAction.action.performed -= OnPlacePerformed;
        removeAction.action.performed -= OnRemovePerformed;
        rotateAction.action.performed -= OnRotatePerformed;

        pointAction.action.Disable();
        placeAction.action.Disable();
        removeAction.action.Disable();
        rotateAction.action.Disable();
    }

    private void Update()
    {
        Vector2 screenPos = pointAction.action.ReadValue<Vector2>();
        Vector3 world = mainCamera.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, -mainCamera.transform.position.z));
        currentHoveredCell = boardManager.WorldToCell(world);

        bool valid = boardManager.IsValidBuildCell(currentHoveredCell) &&
                     boardManager.GetStructureAt(currentHoveredCell) == null;

        boardManager.ShowPreview(currentHoveredCell, valid);
    }

    private void OnPlacePerformed(InputAction.CallbackContext ctx)
    {
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            return;

        if (!boardManager.IsValidBuildCell(currentHoveredCell))
            return;

        if (boardManager.GetStructureAt(currentHoveredCell) != null)
            return;

        Structure newStructure = CreateStructure(selectedStructureType);
        if (newStructure == null) return;

        newStructure.SetOutputDirection(placementDirection);
        boardManager.TryPlaceStructure(currentHoveredCell, newStructure);
    }

    private void OnRemovePerformed(InputAction.CallbackContext ctx)
    {
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            return;

        boardManager.TryRemoveStructure(currentHoveredCell);
    }

    private void OnRotatePerformed(InputAction.CallbackContext ctx)
    {
        placementDirection = placementDirection.RotateClockwise();
    }

    public void SetSelectedStructureType(int typeIndex)
    {
        selectedStructureType = (StructureType)typeIndex;
    }

    private Structure CreateStructure(StructureType type)
    {
        switch (type)
        {
            case StructureType.Producer:
                return new ProducerStructure(ResourceType.Wood);

            case StructureType.Collector:
                return new CollectorStructure(ResourceType.Wood);

            case StructureType.AssemblyLine:
                return new AssemblyLineStructure();

            default:
                return null;
        }
    }
}