using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

[Serializable]
public class QActionValueData
{
    public string ActionKey;
    public float Value;
}

[Serializable]
public class QStateRowData
{
    public string StateKey;
    public List<QActionValueData> Actions = new List<QActionValueData>();
}

[Serializable]
public class QTableData
{
    public List<QStateRowData> Rows = new List<QStateRowData>();
}

public class QTable
{
    private readonly Dictionary<string, Dictionary<string, float>> table = new Dictionary<string, Dictionary<string, float>>();

    public float GetValue(string stateKey, string actionKey)
    {
        if (!table.TryGetValue(stateKey, out Dictionary<string, float> actions))
            return 0f;

        return actions.TryGetValue(actionKey, out float value) ? value : 0f;
    }

    public void SetValue(string stateKey, string actionKey, float value)
    {
        if (!table.TryGetValue(stateKey, out Dictionary<string, float> actions))
        {
            actions = new Dictionary<string, float>();
            table[stateKey] = actions;
        }

        actions[actionKey] = value;
    }

    public StrategicAction GetBestAction(string stateKey, List<StrategicAction> availableActions)
    {
        if (availableActions == null || availableActions.Count == 0)
            return StrategicAction.Wait();

        float bestValue = float.NegativeInfinity;
        List<StrategicAction> bestActions = new List<StrategicAction>();

        for (int i = 0; i < availableActions.Count; i++)
        {
            StrategicAction action = availableActions[i];
            float value = GetValue(stateKey, action.ToKey());

            if (value > bestValue)
            {
                bestValue = value;
                bestActions.Clear();
                bestActions.Add(action);
            }
            else if (Mathf.Approximately(value, bestValue))
            {
                bestActions.Add(action);
            }
        }

        if (bestActions.Count == 0)
            return StrategicAction.Wait();

        return bestActions[UnityEngine.Random.Range(0, bestActions.Count)];
    }

    public void Save(string absoluteFilePath)
    {
        QTableData data = new QTableData();

        foreach (var row in table)
        {
            QStateRowData rowData = new QStateRowData();
            rowData.StateKey = row.Key;

            foreach (var action in row.Value)
            {
                rowData.Actions.Add(new QActionValueData
                {
                    ActionKey = action.Key,
                    Value = action.Value
                });
            }

            data.Rows.Add(rowData);
        }

        string json = JsonUtility.ToJson(data, true);
        File.WriteAllText(absoluteFilePath, json);
    }

    public void Load(string absoluteFilePath)
    {
        table.Clear();

        if (!File.Exists(absoluteFilePath))
            return;

        string json = File.ReadAllText(absoluteFilePath);
        QTableData data = JsonUtility.FromJson<QTableData>(json);

        if (data == null || data.Rows == null)
            return;

        for (int i = 0; i < data.Rows.Count; i++)
        {
            QStateRowData row = data.Rows[i];

            if (!table.ContainsKey(row.StateKey))
                table[row.StateKey] = new Dictionary<string, float>();

            for (int j = 0; j < row.Actions.Count; j++)
            {
                QActionValueData action = row.Actions[j];
                table[row.StateKey][action.ActionKey] = action.Value;
            }
        }
    }
}