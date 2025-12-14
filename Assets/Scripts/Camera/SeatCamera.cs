using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Абстракція "сидінної" камери: нормальна та zoom версії.
/// Підтримує застосування follow/lookAt до Cinemachine (через reflection),
/// активацію/деактивацію, перемикання zoom та управління пріоритетами VCams.
/// </summary>
[DisallowMultipleComponent]
public class SeatCamera : MonoBehaviour
{
    [Tooltip("GameObject for 'normal' seat camera (vcam or camera root)")]
    public GameObject normalCameraGO;

    [Tooltip("GameObject for 'zoom' seat camera (vcam or camera root). If null, fallback behavior applied.")]
    public GameObject zoomCameraGO;

    // cached targets
    private Transform _follow;
    private Transform _lookAt;

    private bool _isZoom = false;
    public bool IsZoomed => _isZoom;

    // Cinemachine vcam type (resolved via reflection)
    private static Type _vcamType => Type.GetType("Cinemachine.CinemachineVirtualCamera, Cinemachine");

    // store original priorities for all discovered VCams (component -> original priority)
    private readonly Dictionary<Component, int> _origPriorities = new Dictionary<Component, int>();

    // store original FOVs for fallback cameras
    private readonly Dictionary<Camera, float> _origFovs = new Dictionary<Camera, float>();

    /// <summary>Apply follow/lookAt to available virtual cameras (if Cinemachine present) or other camera components.</summary>
    public void ApplyTargets(Transform follow, Transform lookAt)
    {
        _follow = follow;
        _lookAt = lookAt;
        TryApplyToCinemachine(normalCameraGO, follow, lookAt);
        TryApplyToCinemachine(zoomCameraGO, follow, lookAt);
    }

    public void SetZoom(bool on)
    {
        _isZoom = on;
        if (zoomCameraGO != null)
        {
            if (normalCameraGO != null) normalCameraGO.SetActive(!on);
            zoomCameraGO.SetActive(on);
        }
        else
        {
            // fallback: toggle child named "Zoom" or adjust FOV
            var zoomChild = transform.Find("Zoom");
            if (zoomChild != null)
            {
                zoomChild.gameObject.SetActive(on);
            }
            else
            {
                // try adjust camera FOV on normalCameraGO
                TryAdjustCameraFOV(normalCameraGO, on);
            }
        }
    }

    public void ToggleZoom() => SetZoom(!_isZoom);

    /// <summary>Activate seat camera (enable the correct GameObjects and reapply targets)</summary>
    public void Activate()
    {
        if (zoomCameraGO != null)
        {
            if (normalCameraGO != null) normalCameraGO.SetActive(!_isZoom);
            zoomCameraGO.SetActive(_isZoom);
        }
        else
        {
            if (normalCameraGO != null) normalCameraGO.SetActive(true);
        }

        ApplyTargets(_follow, _lookAt);
    }

    public void Deactivate()
    {
        if (normalCameraGO != null) normalCameraGO.SetActive(false);
        if (zoomCameraGO != null) zoomCameraGO.SetActive(false);

        // restore fallback FOVs on deactivate (defensive)
        RestoreFovs();
    }

    // -------------------- Priority helpers --------------------

    /// <summary>
    /// Set priority for all found Cinemachine VCams inside normalCameraGO and zoomCameraGO.
    /// Caches original priorities the first time it sees each vcam.
    /// If Cinemachine is not present, silently does nothing.
    /// </summary>
    public void SetPriority(int priority)
    {
        if (_vcamType == null) return;

        var normalVcams = FindVcamsIn(normalCameraGO);
        var zoomVcams = FindVcamsIn(zoomCameraGO);

        foreach (var v in normalVcams)
            SetPriorityForVcamComponent(v, priority);

        foreach (var v in zoomVcams)
            SetPriorityForVcamComponent(v, priority);
    }

    /// <summary>
    /// Restore cached original priorities (if any).
    /// </summary>
    public void RestorePriorities()
    {
        if (_vcamType == null) return;

        // restore priorities
        foreach (var kv in new List<KeyValuePair<Component, int>>(_origPriorities))
        {
            var comp = kv.Key;
            var orig = kv.Value;
            if (comp == null) continue;
            var prop = _vcamType.GetProperty("Priority");
            try
            {
                if (prop != null) prop.SetValue(comp, orig);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[SeatCamera] Failed to restore vcam priority: {e.Message}");
            }
        }
        _origPriorities.Clear();

        // restore fallback FOVs as well
        RestoreFovs();
    }

    // -------------------- Helpers --------------------

    private List<Component> FindVcamsIn(GameObject root)
    {
        var result = new List<Component>();
        if (root == null) return result;
        var vt = _vcamType;
        if (vt == null) return result;

        // GetComponentsInChildren(Type) returns Component[]; call safely outside try/catch yields
        Component[] comps = null;
        try
        {
            comps = root.GetComponentsInChildren(vt, true) as Component[];
        }
        catch
        {
            comps = null;
        }

        if (comps != null && comps.Length > 0)
        {
            result.AddRange(comps);
            return result;
        }

        // fallback: try single component on root
        try
        {
            var single = root.GetComponent(vt) as Component;
            if (single != null) result.Add(single);
        }
        catch { /* ignore */ }

        return result;
    }

    private void SetPriorityForVcamComponent(Component vcamComp, int priority)
    {
        if (vcamComp == null) return;
        var prop = _vcamType.GetProperty("Priority");
        if (prop == null) return;

        // cache original value if not cached
        if (!_origPriorities.ContainsKey(vcamComp))
        {
            try
            {
                var cur = prop.GetValue(vcamComp);
                if (cur is int ival)
                    _origPriorities[vcamComp] = ival;
            }
            catch { /* ignore */ }
        }

        try
        {
            prop.SetValue(vcamComp, priority);
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[SeatCamera] Failed to set vcam priority: {e.Message}");
        }
    }

    private void TryApplyToCinemachine(GameObject go, Transform follow, Transform lookAt)
    {
        if (go == null) return;

        var vcamType = _vcamType;
        if (vcamType == null) return;

        // try all vcams found and set Follow/LookAt
        var comps = FindVcamsIn(go);
        if (comps == null || comps.Count == 0) return;

        var followProp = vcamType.GetProperty("Follow");
        var lookAtProp = vcamType.GetProperty("LookAt");

        foreach (var vcam in comps)
        {
            if (vcam == null) continue;
            try
            {
                if (followProp != null) followProp.SetValue(vcam, follow);
                if (lookAtProp != null) lookAtProp.SetValue(vcam, lookAt);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[SeatCamera] Cinemachine set Follow/LookAt failed: {e.Message}");
            }
        }
    }

    private void TryAdjustCameraFOV(GameObject go, bool zoomOn)
    {
        if (go == null) return;
        var cams = go.GetComponentsInChildren<Camera>(true);
        if (cams == null || cams.Length == 0) return;

        foreach (var cam in cams)
        {
            if (cam == null) continue;
            if (!_origFovs.ContainsKey(cam)) _origFovs[cam] = cam.fieldOfView;
            try
            {
                cam.fieldOfView = zoomOn ? Mathf.Max(10f, _origFovs[cam] * 0.5f) : _origFovs[cam];
            }
            catch { /* ignore */ }
        }
    }

    private void RestoreFovs()
    {
        foreach (var kv in new List<KeyValuePair<Camera, float>>(_origFovs))
        {
            var cam = kv.Key;
            var f = kv.Value;
            if (cam == null) continue;
            try
            {
                cam.fieldOfView = f;
            }
            catch { /* ignore */ }
        }
        _origFovs.Clear();
    }
}
