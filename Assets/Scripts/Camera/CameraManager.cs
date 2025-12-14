using UnityEngine;

/// <summary>
/// Простий singleton CameraManager з підтримкою defaultCameraGO і активної SeatCamera.
/// ЗАПИС: При інтеграції в більший CameraManager — інтегруйте ці методи/поля або змерджіть логіку.
/// </summary>
public class CameraManager : MonoBehaviour
{
    public static CameraManager Instance { get; private set; }

    [Header("Scene cameras")]
    [Tooltip("Main default camera root (should NOT be a child of Player or Vehicle)")]
    public GameObject defaultCameraGO;

    // active seat camera
    private SeatCamera _activeSeatCamera;
    private bool _defaultWasActiveBeforeSwitch = true;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(this.gameObject); return; }
        Instance = this;
    }

    /// <summary>
    /// Switch camera to seat's SeatCamera. follow/lookAt will be applied on the SeatCamera.
    /// </summary>
    public void SwitchToSeat(SeatCamera sc, Transform follow = null, Transform lookAt = null)
    {
        if (sc == null)
        {
            Debug.LogWarning("[CameraManager] SwitchToSeat called with null SeatCamera.");
            return;
        }

        if (_activeSeatCamera == sc) return;

        // deactivate previous
        if (_activeSeatCamera != null)
        {
            _activeSeatCamera.Deactivate();
        }

        // remember default camera active state and disable it
        if (defaultCameraGO != null)
        {
            _defaultWasActiveBeforeSwitch = defaultCameraGO.activeSelf;
            defaultCameraGO.SetActive(false);
        }

        // set new
        _activeSeatCamera = sc;
        _activeSeatCamera.ApplyTargets(follow, lookAt);
        _activeSeatCamera.Activate();
    }

    /// <summary>
    /// Restore default gameplay camera (disable seat camera and enable default camera).
    /// </summary>
    public void RestoreToDefault()
    {
        if (_activeSeatCamera != null)
        {
            _activeSeatCamera.Deactivate();
            _activeSeatCamera = null;
        }

        if (defaultCameraGO != null)
            defaultCameraGO.SetActive(true);
    }

    public void ToggleActiveSeatZoom()
    {
        if (_activeSeatCamera == null) return;
        _activeSeatCamera.ToggleZoom();
    }

    public void SetActiveSeatZoom(bool on)
    {
        if (_activeSeatCamera == null) return;
        _activeSeatCamera.SetZoom(on);
    }

    // Optional: get active SeatCamera
    public SeatCamera GetActiveSeatCamera() => _activeSeatCamera;
}
