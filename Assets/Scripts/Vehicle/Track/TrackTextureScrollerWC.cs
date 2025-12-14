// Assets/Scripts/Vehicle/TracksWC/TrackTextureScrollerWC.cs
// SPDX-License-Identifier: MIT
using UnityEngine;
using TracksWC;

[ExecuteAlways]
public class TrackTextureScrollerWC : MonoBehaviour
{
    [Header("Renderer")]
    public Renderer trackRenderer;

    [Header("Wheel sources")]
    public WheelCollider[] wheels; // всі колеса цієї гусениці (ліва або права)

    [Header("Motion → UV")]
    [Tooltip("Meters of real tread per one V tiling of the texture.")]
    public float metersPerTile = 0.50f;
    [Tooltip("Wheel radius used to convert RPM to m/s (if 0, tries to read WheelCollider.radius).")]
    public float wheelRadius = 0.0f;
    [Tooltip("+1 scrolls ‘down’ when moving forward, -1 to reverse.")]
    public float sign = +1f;

    MaterialPropertyBlock _mpb;
    float _uvOffset;

    void Reset()
    {
        trackRenderer = GetComponent<Renderer>();
    }

    void OnEnable()
    {
        if (_mpb == null) _mpb = new MaterialPropertyBlock();
    }

    void OnValidate()
    {
        metersPerTile = Mathf.Max(1e-4f, metersPerTile);
        if (!trackRenderer) trackRenderer = GetComponent<Renderer>();
    }

    void FixedUpdate()
    {
        if (!Application.isPlaying) return;
        Tick(Time.fixedDeltaTime);
    }

    void Update()
    {
        if (Application.isPlaying) return;
        Tick(Time.deltaTime);
    }

    void Tick(float dt)
    {
        if (!trackRenderer || wheels == null || wheels.Length == 0) return;

        // avg RPM (grounded if possible)
        float rpm = TankTrackDrive.AverageGroundedRPM(wheels);

        // radius
        float R = wheelRadius > 0.0001f ? wheelRadius : Mathf.Max(0.0001f, wheels[0].radius);

        // linear speed (m/s)
        float v = (rpm * 2f * Mathf.PI * R) / 60f;

        _uvOffset = Mathf.Repeat(_uvOffset + sign * (v / metersPerTile) * dt, 1f);

        if (_mpb == null) _mpb = new MaterialPropertyBlock();
        trackRenderer.GetPropertyBlock(_mpb);

        // URP/HDRP: _BaseMap_ST; Built-in: _MainTex_ST
        var mat = trackRenderer.sharedMaterial;
        if (mat)
        {
            if (mat.HasProperty("_BaseMap_ST"))
            {
                Vector4 st = mat.GetVector("_BaseMap_ST");
                st.w = -_uvOffset;
                _mpb.SetVector("_BaseMap_ST", st);
            }
            if (mat.HasProperty("_MainTex_ST"))
            {
                Vector4 st = mat.GetVector("_MainTex_ST");
                st.w = -_uvOffset;
                _mpb.SetVector("_MainTex_ST", st);
            }
        }

        trackRenderer.SetPropertyBlock(_mpb);
    }
}
