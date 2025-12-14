using UnityEngine;

public class PlayerAimTargetDriver : MonoBehaviour
{
    [SerializeField] private Transform aimTarget;
    [SerializeField] private Transform cam;
    [SerializeField] private float distance = 20f;
    [SerializeField] private float lerp = 20f;
    [SerializeField] private LayerMask hitMask = ~0;

    void Reset() { if (!cam && Camera.main) cam = Camera.main.transform; }

    void Awake()
    {
        if (!aimTarget)
        {
            var t = transform.Find("AimTarget ") ?? transform.Find("AimTarget");
            if (t) aimTarget = t;
        }
        if (aimTarget && !aimTarget.gameObject.activeSelf)
            aimTarget.gameObject.SetActive(true);
        if (!cam && Camera.main) cam = Camera.main.transform;
    }

    void LateUpdate()
    {
        if (!aimTarget || !cam) return;

        Vector3 dst = cam.position + cam.forward * distance;
        if (Physics.Raycast(cam.position, cam.forward, out var hit, distance, hitMask, QueryTriggerInteraction.Ignore))
            dst = hit.point;

        aimTarget.position = Vector3.Lerp(aimTarget.position, dst, 1 - Mathf.Exp(-lerp * Time.deltaTime));
        aimTarget.rotation = Quaternion.LookRotation((dst - cam.position).normalized, Vector3.up);
    }
}
