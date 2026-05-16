using TMPro;
using UnityEngine;

public class ObservedPlayerSummaryUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerViewSwitcher switcher;

    [Header("Panel Root")]
    [SerializeField] private GameObject panelRoot;

    [Header("UI")]
    [SerializeField] private TMP_Text playerNameText;
    [SerializeField] private TMP_Text woodText;
    [SerializeField] private TMP_Text brickText;
    [SerializeField] private TMP_Text steelText;
    [SerializeField] private TMP_Text psiText;
    [SerializeField] private TMP_Text timeLeftText;
    [SerializeField] private TMP_Text structureCountText;

    private PlayerRuntime currentRuntime;

    private void OnEnable()
    {
        if (switcher != null)
            switcher.ObservedRuntimeChanged += BindRuntime;
    }

    private void OnDisable()
    {
        if (switcher != null)
            switcher.ObservedRuntimeChanged -= BindRuntime;
    }

    private void Start()
    {
        if (switcher != null && switcher.CurrentRuntime != null)
            BindRuntime(switcher.CurrentRuntime);
    }

    private void Update()
    {
        Refresh();
    }

    private void BindRuntime(PlayerRuntime runtime)
    {
        currentRuntime = runtime;
        RefreshVisibility();
        Refresh();
    }

    private void RefreshVisibility()
    {
        if (panelRoot == null)
            return;

        bool shouldShow = currentRuntime != null && currentRuntime.IsAIPlayer;
        panelRoot.SetActive(shouldShow);
    }

    private void Refresh()
    {
        if (currentRuntime == null)
            return;

        if (panelRoot != null && !panelRoot.activeSelf)
            return;

        if (playerNameText != null)
            playerNameText.text = currentRuntime.PlayerName;

        if (currentRuntime.Inventory != null)
        {
            if (woodText != null)
                woodText.text = $"Wood: {currentRuntime.Inventory.GetResource(ResourceType.Wood)}";

            if (brickText != null)
                brickText.text = $"Brick: {currentRuntime.Inventory.GetResource(ResourceType.Brick)}";

            if (steelText != null)
                steelText.text = $"Steel: {currentRuntime.Inventory.GetResource(ResourceType.Steel)}";

            if (psiText != null)
                psiText.text = $"Psi: {currentRuntime.Inventory.GetResource(ResourceType.Psi)}";
        }

        if (currentRuntime.Clock != null && timeLeftText != null)
        {
            int totalSeconds = Mathf.CeilToInt(currentRuntime.Clock.TimeLeft);
            int minutes = totalSeconds / 60;
            int seconds = totalSeconds % 60;
            timeLeftText.text = $"Time: {minutes:00}:{seconds:00}";
        }

        if (currentRuntime.Board != null && structureCountText != null)
        {
            structureCountText.text = $"Structures: {currentRuntime.Board.ActiveStructures.Count}";
        }
    }
}