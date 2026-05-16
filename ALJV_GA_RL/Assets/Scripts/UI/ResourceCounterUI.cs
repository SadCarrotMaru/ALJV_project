using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ResourceCounterUI : MonoBehaviour
{
    [SerializeField] private Image iconImage;
    [SerializeField] private TMP_Text countText;

    private PlayerInventory inventory;
    private ResourceType resourceType;

    public void Setup(PlayerInventory playerInventory, ResourceType type, Sprite icon)
    {
        inventory = playerInventory;
        resourceType = type;

        if (iconImage != null)
            iconImage.sprite = icon;

        Refresh();

        if (inventory != null)
            inventory.OnInventoryChanged += Refresh;
    }

    private void OnDestroy()
    {
        if (inventory != null)
            inventory.OnInventoryChanged -= Refresh;
    }

    public void Refresh()
    {
        if (countText != null && inventory != null)
        {
            countText.text = inventory.GetResource(resourceType).ToString();
        }
    }
}