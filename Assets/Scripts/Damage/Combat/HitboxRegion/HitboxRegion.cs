using UnityEngine;
using Combat.Core;

[DisallowMultipleComponent]
public sealed class HitboxRegion : MonoBehaviour
{
    [Tooltip("який рег≥он т≥ла/вузол техн≥ки в≥дпов≥даЇ цьому колайдеру.")]
    public BodyRegion region = BodyRegion.Default;
}
