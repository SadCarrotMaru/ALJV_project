using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class PlayerViewSwitcher : MonoBehaviour
{
    [Header("Camera")]
    [SerializeField] private Camera targetCamera;
    [SerializeField] private Vector2 cameraOffsetXY = Vector2.zero;
    [SerializeField] private bool smoothFollow = true;
    [SerializeField] private float followSpeed = 8f;
    [SerializeField] private bool preserveInitialCameraZ = true;
    [SerializeField] private float manualCameraZ = -10f;

    [Header("Players")]
    [SerializeField] private List<PlayerRuntime> runtimes = new List<PlayerRuntime>();

    [Header("UI")]
    [SerializeField] private TMP_Text observedPlayerText;

    private int currentIndex = 0;
    private float fixedCameraZ = -10f;

    public PlayerRuntime CurrentRuntime
    {
        get
        {
            if (runtimes == null || runtimes.Count == 0)
                return null;

            if (currentIndex < 0 || currentIndex >= runtimes.Count)
                return null;

            return runtimes[currentIndex];
        }
    }

    public event Action<PlayerRuntime> ObservedRuntimeChanged;

    private void Awake()
    {
        if (targetCamera != null)
        {
            fixedCameraZ = preserveInitialCameraZ ? targetCamera.transform.position.z : manualCameraZ;
        }
        else
        {
            fixedCameraZ = manualCameraZ;
        }
    }

    private void Start()
    {
        ApplyCurrent(true);
    }

    private void LateUpdate()
    {
        FollowCurrentRuntime();
    }

    public void OnNextPlayerPressed()
    {
        ViewNext();
    }

    public void OnPreviousPlayerPressed()
    {
        ViewPrevious();
    }

    public void ViewNext()
    {
        if (runtimes == null || runtimes.Count == 0)
            return;

        currentIndex = (currentIndex + 1) % runtimes.Count;
        ApplyCurrent(true);
    }

    public void ViewPrevious()
    {
        if (runtimes == null || runtimes.Count == 0)
            return;

        currentIndex = (currentIndex - 1 + runtimes.Count) % runtimes.Count;
        ApplyCurrent(true);
    }

    public void ViewIndex(int index)
    {
        if (runtimes == null || index < 0 || index >= runtimes.Count)
            return;

        currentIndex = index;
        ApplyCurrent(true);
    }

    private void ApplyCurrent(bool snapCamera)
    {
        PlayerRuntime runtime = CurrentRuntime;
        if (runtime == null)
            return;

        if (observedPlayerText != null)
            observedPlayerText.text = $"Viewing: {runtime.PlayerName}";

        if (snapCamera)
            SnapToCurrentRuntime();

        ObservedRuntimeChanged?.Invoke(runtime);
    }

    private void SnapToCurrentRuntime()
    {
        if (targetCamera == null || CurrentRuntime == null)
            return;

        Vector3 focusPosition = GetFocusPosition(CurrentRuntime);
        targetCamera.transform.position = new Vector3(
            focusPosition.x + cameraOffsetXY.x,
            focusPosition.y + cameraOffsetXY.y,
            fixedCameraZ
        );
    }

    private void FollowCurrentRuntime()
    {
        if (targetCamera == null || CurrentRuntime == null)
            return;

        Vector3 focusPosition = GetFocusPosition(CurrentRuntime);

        Vector3 targetPosition = new Vector3(
            focusPosition.x + cameraOffsetXY.x,
            focusPosition.y + cameraOffsetXY.y,
            fixedCameraZ
        );

        if (smoothFollow)
        {
            Vector3 current = targetCamera.transform.position;
            Vector3 next = Vector3.Lerp(current, targetPosition, Time.deltaTime * followSpeed);

            next.z = fixedCameraZ;
            targetCamera.transform.position = next;
        }
        else
        {
            targetCamera.transform.position = targetPosition;
        }
    }

    private Vector3 GetFocusPosition(PlayerRuntime runtime)
    {
        if (runtime == null)
            return Vector3.zero;

        if (runtime.CameraFocus != null)
            return runtime.CameraFocus.position;

        return runtime.transform.position;
    }
}