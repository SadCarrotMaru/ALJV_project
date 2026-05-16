using System.Collections.Generic;
using UnityEngine;

public class StrategicQLearningTrainer : MonoBehaviour
{
    [Header("Definitions")]
    [SerializeField] private List<StructureDefinition> structureDefinitions = new List<StructureDefinition>();

    [Header("Starting State")]
    [SerializeField] private List<ResourceAmount> startingResources = new List<ResourceAmount>();
    [SerializeField] private List<StructureStockEntry> startingStructures = new List<StructureStockEntry>();

    [Header("Target")]
    [SerializeField] private ResourceType targetResource = ResourceType.Wood;

    [Header("RL")]
    [SerializeField] private QLearningSettings settings = new QLearningSettings();
    [SerializeField] private RLRewardWeights rewardWeights = new RLRewardWeights();

    [Header("Save")]
    [SerializeField] private string saveFileName = "qtable.json";

    private readonly QTable qTable = new QTable();

    [ContextMenu("Train Q-Learning")]
    public void Train()
    {
        List<StrategicAction> allActions = StrategicActionCatalog.BuildActions(structureDefinitions);
        float epsilon = settings.Epsilon;

        for (int episode = 0; episode < settings.Episodes; episode++)
        {
            FabricStrategicSimulator simulator = new FabricStrategicSimulator(
                structureDefinitions,
                startingResources,
                startingStructures,
                rewardWeights,
                settings.StepSeconds,
                settings.MatchSeconds,
                targetResource
            );

            while (!simulator.IsTerminal())
            {
                string stateKey = simulator.GetStateKey();
                List<StrategicAction> availableActions = simulator.GetAvailableActions();

                StrategicAction chosenAction;
                if (Random.value < epsilon)
                {
                    chosenAction = availableActions[Random.Range(0, availableActions.Count)];
                }
                else
                {
                    chosenAction = qTable.GetBestAction(stateKey, availableActions);
                }

                float reward = simulator.Step(chosenAction);
                string nextStateKey = simulator.GetStateKey();
                List<StrategicAction> nextActions = simulator.GetAvailableActions();

                float oldQ = qTable.GetValue(stateKey, chosenAction.ToKey());
                StrategicAction bestNext = qTable.GetBestAction(nextStateKey, nextActions);
                float maxNextQ = qTable.GetValue(nextStateKey, bestNext.ToKey());

                float newQ = oldQ + settings.Alpha * (reward + settings.Gamma * maxNextQ - oldQ);
                qTable.SetValue(stateKey, chosenAction.ToKey(), newQ);
            }

            epsilon = Mathf.Max(settings.MinEpsilon, epsilon * settings.EpsilonDecay);

            if ((episode + 1) % 250 == 0)
            {
                Debug.Log($"RL training progress: {episode + 1}/{settings.Episodes}");
            }
        }

        string path = System.IO.Path.Combine(Application.persistentDataPath, saveFileName);
        qTable.Save(path);
        Debug.Log($"Q-table saved to: {path}");
    }

    public QTable GetQTable()
    {
        return qTable;
    }
}