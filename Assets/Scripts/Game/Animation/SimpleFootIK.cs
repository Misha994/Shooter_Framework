using UnityEngine;
using UnityEngine.Animations.Rigging;

public class SimpleFootIK : MonoBehaviour
{
    [Header("IK")]
    public TwoBoneIKConstraint leftFootIK, rightFootIK;
    public Transform pelvis;

    [Header("Tuning")]
    public LayerMask groundMask = ~0;
    public float footRaycast = 1.2f;
    public float footOffset = 0.02f;
    public float pelvisLerp = 10f;
    public float footLerp = 20f;

    private float _pelvisOffset;                    // поточний офсет по Y (локальний)
    private Vector3 _pelvisBaseLocalPos;            // базова локальна позиція таза

    void Awake()
    {
        if (pelvis) _pelvisBaseLocalPos = pelvis.localPosition;
    }

    void LateUpdate()
    {
        if (!leftFootIK || !rightFootIK) return;

        // safe: якщо у IK не виставлені Tip/Target — нічого не робимо
        if (leftFootIK.data.tip == null || leftFootIK.data.target == null ||
            rightFootIK.data.tip == null || rightFootIK.data.target == null)
            return;

        Vector3 lPos, rPos; Quaternion lRot, rRot;
        bool lHit = Probe(leftFootIK.data.tip, out lPos, out lRot);
        bool rHit = Probe(rightFootIK.data.tip, out rPos, out rRot);

        // Ліва нога
        if (lHit)
        {
            leftFootIK.data.target.position = Vector3.Lerp(
                leftFootIK.data.target.position, lPos, 1 - Mathf.Exp(-footLerp * Time.deltaTime));
            leftFootIK.data.target.rotation = Quaternion.Slerp(
                leftFootIK.data.target.rotation, lRot, 1 - Mathf.Exp(-footLerp * Time.deltaTime));
            leftFootIK.weight = 1f;
        }
        else
        {
            leftFootIK.weight = Mathf.Lerp(leftFootIK.weight, 0f, 1 - Mathf.Exp(-footLerp * Time.deltaTime));
        }

        // Права нога
        if (rHit)
        {
            rightFootIK.data.target.position = Vector3.Lerp(
                rightFootIK.data.target.position, rPos, 1 - Mathf.Exp(-footLerp * Time.deltaTime));
            rightFootIK.data.target.rotation = Quaternion.Slerp(
                rightFootIK.data.target.rotation, rRot, 1 - Mathf.Exp(-footLerp * Time.deltaTime));
            rightFootIK.weight = 1f;
        }
        else
        {
            rightFootIK.weight = Mathf.Lerp(rightFootIK.weight, 0f, 1 - Mathf.Exp(-footLerp * Time.deltaTime));
        }

        // Підйом/опуск тазу — абсолютне позиціонування відносно базового, без дрейфу
        if (pelvis && (lHit || rHit))
        {
            float target = 0f;
            if (lHit) target = Mathf.Min(target, WorldToPelvisY(lPos));
            if (rHit) target = Mathf.Min(target, WorldToPelvisY(rPos));

            _pelvisOffset = Mathf.Lerp(_pelvisOffset, target, 1 - Mathf.Exp(-pelvisLerp * Time.deltaTime));

            var lp = _pelvisBaseLocalPos;
            pelvis.localPosition = new Vector3(lp.x, lp.y - _pelvisOffset, lp.z);
        }
    }

    bool Probe(Transform footTip, out Vector3 pos, out Quaternion rot)
    {
        pos = Vector3.zero;
        rot = Quaternion.identity;
        if (!footTip) return false;

        Vector3 origin = footTip.position + Vector3.up * 0.5f;
        float maxDist = footRaycast + 0.5f;

        // Гарантовано виключаємо шар гравця із маски на випадок помилок в інспекторі
        int mask = groundMask;
        mask &= ~(1 << gameObject.layer);

        // Шукаємо перший хіт, який НЕ належить нашому кореню
        var hits = Physics.RaycastAll(origin, Vector3.down, maxDist, mask, QueryTriggerInteraction.Ignore);
        if (hits == null || hits.Length == 0) return false;
        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        RaycastHit hit = default;
        bool found = false;
        foreach (var h in hits)
        {
            if (h.collider && h.collider.transform.root != transform.root)
            { hit = h; found = true; break; }
        }
        if (!found) return false;

        pos = hit.point + hit.normal * footOffset;

        Vector3 fwd = Vector3.ProjectOnPlane(footTip.forward, hit.normal).normalized;
        if (fwd.sqrMagnitude < 1e-6f)
            fwd = Vector3.ProjectOnPlane(transform.forward, hit.normal).normalized;

        rot = Quaternion.LookRotation(fwd, hit.normal);
        return true;
    }

    float WorldToPelvisY(Vector3 worldFoot) => transform.InverseTransformPoint(worldFoot).y;
}
