using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ShopItemUI : MonoBehaviour
{
    [SerializeField] private Image iconImage;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text costText;
    [SerializeField] private Button buyButton;

    private StructureDefinition definition;
    private PlayerInventory inventory;
    private ShopManager shopManager;

    public void Setup(StructureDefinition structureDefinition, PlayerInventory playerInventory, ShopManager manager)
    {
        definition = structureDefinition;
        inventory = playerInventory;
        shopManager = manager;

        if (iconImage != null)
            iconImage.sprite = definition.Icon;

        if (nameText != null)
            nameText.text = definition.DisplayName;

        if (buyButton != null)
        {
            buyButton.onClick.RemoveAllListeners();
            buyButton.onClick.AddListener(OnBuyClicked);
        }

        Refresh();

        if (inventory != null)
            inventory.OnInventoryChanged += Refresh;
    }

    private void OnDestroy()
    {
        if (inventory != null)
            inventory.OnInventoryChanged -= Refresh;
    }

    private void OnBuyClicked()
    {
        if (shopManager != null)
            shopManager.TryBuy(definition);
    }

    public void Refresh()
    {
        if (costText != null)
            costText.text = BuildCostText();

        if (buyButton != null && inventory != null && definition != null)
            buyButton.interactable = inventory.CanAfford(definition.BuyCost);
    }

    private string BuildCostText()
    {
        if (definition == null || definition.BuyCost == null || definition.BuyCost.Count == 0)
            return "Free";

        StringBuilder sb = new StringBuilder();

        for (int i = 0; i < definition.BuyCost.Count; i++)
        {
            ResourceAmount cost = definition.BuyCost[i];
            if (cost == null) continue;

            if (sb.Length > 0)
                sb.Append(" | ");

            sb.Append(cost.Resource);
            sb.Append(": ");
            sb.Append(cost.Amount);
        }

        return sb.ToString();
    }
}