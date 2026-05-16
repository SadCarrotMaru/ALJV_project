using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class StructureDetailsPanelUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerInventory playerInventory;

    [Header("Panel")]
    [SerializeField] private GameObject panelRoot;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text levelText;
    [SerializeField] private TMP_Text statsText;
    [SerializeField] private TMP_Text upgradeCostText;
    [SerializeField] private TMP_Text upgradeButtonText;
    [SerializeField] private Button upgradeButton;
    [SerializeField] private Button closeButton;

    [Header("Converter Recipe UI")]
    [SerializeField] private GameObject recipeSectionRoot;
    [SerializeField] private TMP_Text recipeSectionTitleText;
    [SerializeField] private TMP_Dropdown recipeDropdown;

    private Structure selectedStructure;
    private bool suppressRecipeDropdownCallback;

    public Structure SelectedStructure => selectedStructure;

    private void Awake()
    {
        if (upgradeButton != null)
            upgradeButton.onClick.AddListener(OnUpgradePressed);

        if (closeButton != null)
            closeButton.onClick.AddListener(HidePanel);

        if (recipeDropdown != null)
            recipeDropdown.onValueChanged.AddListener(OnRecipeDropdownChanged);

        if (panelRoot != null)
            panelRoot.SetActive(false);

        if (recipeSectionRoot != null)
            recipeSectionRoot.SetActive(false);
    }

    private void OnEnable()
    {
        if (playerInventory != null)
            playerInventory.OnInventoryChanged += RefreshUI;
    }

    private void OnDisable()
    {
        if (playerInventory != null)
            playerInventory.OnInventoryChanged -= RefreshUI;
    }

    private void OnDestroy()
    {
        if (upgradeButton != null)
            upgradeButton.onClick.RemoveListener(OnUpgradePressed);

        if (closeButton != null)
            closeButton.onClick.RemoveListener(HidePanel);

        if (recipeDropdown != null)
            recipeDropdown.onValueChanged.RemoveListener(OnRecipeDropdownChanged);
    }

    public void ShowStructure(Structure structure)
    {
        selectedStructure = structure;

        if (panelRoot != null)
            panelRoot.SetActive(structure != null);

        RefreshUI();
    }

    public void HidePanel()
    {
        selectedStructure = null;

        if (panelRoot != null)
            panelRoot.SetActive(false);

        if (recipeSectionRoot != null)
            recipeSectionRoot.SetActive(false);
    }

    public void RefreshUI()
    {
        if (selectedStructure == null)
            return;

        if (titleText != null)
            titleText.text = selectedStructure.GetDisplayName();

        if (levelText != null)
            levelText.text = $"Lvl {selectedStructure.Level}/{selectedStructure.GetMaxLevel()}";

        if (statsText != null)
        {
            List<string> lines = selectedStructure.GetDetailLines();
            StringBuilder sb = new StringBuilder();

            for (int i = 0; i < lines.Count; i++)
            {
                if (i > 0)
                    sb.AppendLine();

                sb.Append(lines[i]);
            }

            statsText.text = sb.ToString();
        }

        List<ResourceAmount> nextUpgradeCost = selectedStructure.GetNextUpgradeCost();

        bool canUpgrade = selectedStructure.CanUpgrade(playerInventory);
        bool isMaxLevel = selectedStructure.IsMaxLevel();

        if (upgradeCostText != null)
        {
            if (isMaxLevel)
                upgradeCostText.text = "Upgrade Cost: MAX";
            else
                upgradeCostText.text = "Upgrade Cost: " + BuildCostText(nextUpgradeCost);
        }

        if (upgradeButton != null)
            upgradeButton.interactable = !isMaxLevel && canUpgrade;

        if (upgradeButtonText != null)
            upgradeButtonText.text = isMaxLevel ? "Max Level" : "Upgrade";

        RefreshRecipeSection();
    }

    private void RefreshRecipeSection()
    {
        if (recipeSectionRoot == null || recipeDropdown == null)
            return;

        if (selectedStructure is ConverterStructure converter)
        {
            recipeSectionRoot.SetActive(true);

            if (recipeSectionTitleText != null)
                recipeSectionTitleText.text = "Recipe";

            List<string> labels = converter.GetRecipeOptionLabels();

            suppressRecipeDropdownCallback = true;
            recipeDropdown.ClearOptions();
            recipeDropdown.AddOptions(labels);
            recipeDropdown.SetValueWithoutNotify(converter.ActiveRecipeIndex);
            suppressRecipeDropdownCallback = false;
        }
        else
        {
            recipeSectionRoot.SetActive(false);
        }
    }

    private void OnUpgradePressed()
    {
        if (selectedStructure == null || playerInventory == null)
            return;

        bool upgraded = selectedStructure.TryUpgrade(playerInventory);
        if (upgraded)
        {
            RefreshUI();
        }
    }

    private void OnRecipeDropdownChanged(int optionIndex)
    {
        if (suppressRecipeDropdownCallback)
            return;

        if (selectedStructure is ConverterStructure converter)
        {
            bool changed = converter.TrySetActiveRecipe(optionIndex);
            if (changed)
            {
                RefreshUI();
            }
        }
    }

    private string BuildCostText(List<ResourceAmount> costs)
    {
        if (costs == null || costs.Count == 0)
            return "Free";

        StringBuilder sb = new StringBuilder();

        for (int i = 0; i < costs.Count; i++)
        {
            ResourceAmount cost = costs[i];
            if (cost == null || cost.Resource == ResourceType.None)
                continue;

            if (sb.Length > 0)
                sb.Append(" | ");

            sb.Append(cost.Resource);
            sb.Append(": ");
            sb.Append(cost.Amount);
        }

        return sb.ToString();
    }
}