using System;
using System.Collections.Generic;
using UnityEngine;

public class ShopManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerInventory playerInventory;

    [Header("Shop UI")]
    [SerializeField] private GameObject shopPanel;
    [SerializeField] private Transform contentRoot;
    [SerializeField] private ShopItemUI shopItemPrefab;

    [Header("Shop Data")]
    [SerializeField] private List<StructureDefinition> shopDefinitions = new List<StructureDefinition>();

    public bool IsOpen => shopPanel != null && shopPanel.activeSelf;

    public event Action<string> StatusChanged;
    public event Action<bool> ShopOpenChanged;

    private void Start()
    {
        BuildShopUI();

        if (shopPanel != null)
            shopPanel.SetActive(false);
    }

    public void ToggleShop()
    {
        if (shopPanel == null)
        {
            RaiseStatus("Shop panel is missing.");
            return;
        }

        bool newState = !shopPanel.activeSelf;
        shopPanel.SetActive(newState);

        ShopOpenChanged?.Invoke(newState);
        RaiseStatus(newState ? "Shop opened." : "Shop closed.");
    }

    public void SetShopOpen(bool open)
    {
        if (shopPanel == null)
            return;

        shopPanel.SetActive(open);
        ShopOpenChanged?.Invoke(open);
    }

    public void TryBuy(StructureDefinition definition)
    {
        if (playerInventory == null)
        {
            RaiseStatus("PlayerInventory is missing.");
            return;
        }

        if (definition == null)
        {
            RaiseStatus("Invalid shop item.");
            return;
        }

        bool bought = playerInventory.TryBuyStructure(definition, 1);

        if (bought)
            RaiseStatus($"Bought 1x {definition.DisplayName}.");
        else
            RaiseStatus($"Not enough resources for {definition.DisplayName}.");
    }

    private void BuildShopUI()
    {
        if (contentRoot == null || shopItemPrefab == null)
            return;

        for (int i = contentRoot.childCount - 1; i >= 0; i--)
        {
            Destroy(contentRoot.GetChild(i).gameObject);
        }

        for (int i = 0; i < shopDefinitions.Count; i++)
        {
            StructureDefinition definition = shopDefinitions[i];
            if (definition == null)
                continue;

            ShopItemUI item = Instantiate(shopItemPrefab, contentRoot, false);
            item.Setup(definition, playerInventory, this);
        }
    }

    private void RaiseStatus(string message)
    {
        StatusChanged?.Invoke(message);
        Debug.Log(message);
    }
}