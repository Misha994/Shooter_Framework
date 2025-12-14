using UnityEngine;
using UnityEngine.Animations.Rigging;

/// <summary>
/// Прив’язує MultiAimConstraint(Head/Chest) до єдиного AimTarget (який рухає PlayerAimTargetDriver).
/// Роби це, а не до IKPoints. IKPoints = руки; AimTarget = куди дивитися голові/грудям.
/// </summary>
public class MultiAimBinder : MonoBehaviour
{
    [Header("Constraints")]
    [SerializeField] MultiAimConstraint head;
    [SerializeField] MultiAimConstraint chest;

    [Header("Target")]
    [SerializeField] Transform aimTarget; // посилання на "AimTarget " із префабу Player

    void Reset()
    {
        // авто-пошук по іменах з твого префабу
        if (!aimTarget)
        {
            var t = transform.root.Find("Infantryman/AimTarget ");
            if (!t) t = transform.root.Find("AimTarget ");
            aimTarget = t;
        }

        if (!head) head = transform.GetComponentInChildren<MultiAimConstraint>(true);
        if (!chest)
        {
            var all = transform.GetComponentsInChildren<MultiAimConstraint>(true);
            if (all.Length > 1) chest = all[1];
        }
    }

    void Awake()
    {
        Apply(head);
        Apply(chest);

        // обов’язково увімкни сам об’єкт AimTarget у префабі!
        if (aimTarget && !aimTarget.gameObject.activeSelf)
            aimTarget.gameObject.SetActive(true);
    }

    void Apply(MultiAimConstraint c)
    {
        if (!c || !aimTarget) return;

        var arr = new WeightedTransformArray();
#if UNITY_2020_3_OR_NEWER
        arr.Add(new WeightedTransform(aimTarget, 1f));
#else
        // старі версії — якщо Add недоступний, можна через utility:
        var wt = new WeightedTransform(aimTarget, 1f);
        arr.Insert(0, wt);
#endif
        var data = c.data;
        data.sourceObjects = arr;
        c.data = data;
    }
}
