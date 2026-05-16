using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class StructureInventoryItemUI : MonoBehaviour
{
    [SerializeField] private Button button;
    [SerializeField] private Image iconImage;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text countText;
    [SerializeField] private GameObject selectionHighlight;

    private StructureDefinition definition;
    private PlayerInventory inventory;
    private TilePlacementController placementController;

    public void Setup(StructureDefinition structureDefinition, PlayerInventory playerInventory, TilePlacementController controller)
    {
        definition = structureDefinition;
        inventory = playerInventory;
        placementController = controller;

        if (iconImage != null)
            iconImage.sprite = definition.Icon;

        if (nameText != null)
            nameText.text = definition.DisplayName;

        if (button != null)
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(OnClicked);
        }

        if (inventory != null)
            inventory.OnInventoryChanged += Refresh;

        if (placementController != null)
            placementController.SelectionChanged += RefreshSelection;

        Refresh();
        RefreshSelection(placementController != null ? placementController.SelectedDefinition : null);
    }

    private void OnDestroy()
    {
        if (inventory != null)
            inventory.OnInventoryChanged -= Refresh;

        if (placementController != null)
            placementController.SelectionChanged -= RefreshSelection;
    }

    private void OnClicked()
    {
        if (placementController != null)
            placementController.SelectStructureDefinition(definition);
    }

    public void Refresh()
    {
        if (countText != null && inventory != null)
        {
            countText.text = inventory.GetOwnedCount(definition).ToString();
        }
    }

    private void RefreshSelection(StructureDefinition selected)
    {
        if (selectionHighlight != null)
        {
            selectionHighlight.SetActive(selected == definition);
        }
    }
}