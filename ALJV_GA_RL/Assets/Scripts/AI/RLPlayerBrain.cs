using UnityEngine;

public class RLPlayerBrain : MonoBehaviour
{
    [SerializeField] private PlayerRuntime runtime;
    [SerializeField] private ResourceType targetResource = ResourceType.Wood;
    [SerializeField] private float decisionInterval = 1.5f;
    [SerializeField] private float runtimeEpsilon = 0.08f;
    [SerializeField] private string qTableFileName = "qtable.json";
    [SerializeField] private bool verboseLogging = true;

    private readonly QTable qTable = new QTable();
    private float timer;

    public string LastActionText { get; private set; } = "None";

    private void Awake()
    {
        string path = System.IO.Path.Combine(Application.persistentDataPath, qTableFileName);
        qTable.Load(path);

        if (verboseLogging)
            Debug.Log($"[RL] Loaded Q-table from: {path}");
    }

    private void Update()
    {
        if (runtime == null || runtime.Clock == null || runtime.Clock.IsFinished)
            return;

        timer += Time.deltaTime;
        if (timer < decisionInterval)
            return;

        timer = 0f;
        ThinkAndAct();
    }

    private void ThinkAndAct()
    {
        var observation = runtime.BuildObservation(targetResource);
        string stateKey = StrategicStateEncoder.BuildKey(observation, runtime.StructureDefinitions);
        var actions = runtime.BuildAvailableActions();

        if (actions.Count == 0)
        {
            LastActionText = "No available actions";
            return;
        }

        StrategicAction action;

        if (Random.value < runtimeEpsilon)
            action = actions[Random.Range(0, actions.Count)];
        else
            action = qTable.GetBestAction(stateKey, actions);

        LastActionText = action.ToString();

        if (verboseLogging)
            Debug.Log($"[RL:{runtime.PlayerName}] Action = {LastActionText}");

        Execute(action);
    }

    private void Execute(StrategicAction action)
    {
        if (action == null || runtime == null)
            return;

        StructureDefinition def = runtime.FindDefinitionById(action.StructureId);

        switch (action.Kind)
        {
            case StrategicActionKind.Wait:
                break;

            case StrategicActionKind.BuyStructure:
                runtime.TryBuy(def);
                break;

            case StrategicActionKind.PlaceStructure:
                {
                    if (def == null)
                        return;

                    if (PlacementHeuristics.TryFindPlacement(runtime, def, out Vector3Int cell, out Direction dir))
                    {
                        bool placed = runtime.TryPlaceOwned(def, cell, dir);

                        if (verboseLogging)
                            Debug.Log($"[RL:{runtime.PlayerName}] TryPlace {def.DisplayName} at {cell} -> {placed}");
                    }
                    else if (verboseLogging)
                    {
                        Debug.Log($"[RL:{runtime.PlayerName}] No valid placement found for {def.DisplayName}");
                    }

                    break;
                }

            case StrategicActionKind.UpgradeStructure:
                runtime.TryUpgradeBestPlaced(def);
                break;

            case StrategicActionKind.SetConverterRecipe:
                runtime.TrySetAllPlacedConverterRecipes(def, action.RecipeIndex);
                break;
        }
    }
}