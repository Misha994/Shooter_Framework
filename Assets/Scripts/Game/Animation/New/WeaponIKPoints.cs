using UnityEngine;

public class WeaponIKPoints : MonoBehaviour
{
    [Header("Right Hand")]
    public Transform rightHipTarget;
    public Transform rightHipHint;
    public Transform rightAdsTarget;
    public Transform rightAdsHint;

    [Header("Left Hand")]
    public Transform leftHipTarget;
    public Transform leftHipHint;
    public Transform leftAdsTarget;
    public Transform leftAdsHint;

    // Для зручності перевірок
    public bool IsComplete =>
        rightHipTarget && rightHipHint && rightAdsTarget && rightAdsHint &&
        leftHipTarget && leftHipHint && leftAdsTarget && leftAdsHint;
}
