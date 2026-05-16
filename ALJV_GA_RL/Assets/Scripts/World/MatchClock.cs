using UnityEngine;

public class MatchClock : MonoBehaviour
{
    [SerializeField] private float totalSeconds = 600f;
    [SerializeField] private bool runningOnStart = true;

    private float timeLeft;
    private bool isRunning;

    public float TotalSeconds => totalSeconds;
    public float TimeLeft => timeLeft;
    public bool IsRunning => isRunning;
    public bool IsFinished => timeLeft <= 0f;

    private void Awake()
    {
        timeLeft = totalSeconds;
        isRunning = runningOnStart;
    }

    private void Update()
    {
        if (!isRunning || timeLeft <= 0f)
            return;

        timeLeft -= Time.deltaTime;
        if (timeLeft < 0f)
            timeLeft = 0f;
    }

    public void StartClock()
    {
        isRunning = true;
    }

    public void PauseClock()
    {
        isRunning = false;
    }

    public void ResetClock()
    {
        timeLeft = totalSeconds;
    }
}