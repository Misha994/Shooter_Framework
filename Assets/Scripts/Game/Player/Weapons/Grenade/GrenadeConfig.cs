using UnityEngine;

[CreateAssetMenu(fileName = "NewGrenadeConfig", menuName = "FPS/Grenade Config")]
public class GrenadeConfig : ScriptableObject
{
    [Header("General")]
    public string grenadeName = "Fragmentation";

    [Header("Prefab")]
    public GameObject prefab; // <— Це і є поле, що потрібно додати

    [Header("Throw Settings")]
    public float throwForce = 15f;

    [Header("Explosion Settings")]
    public float fuseTime = 3f;
    public float damage = 100f;
    public float explosionRadius = 5f;

    [Header("Cooking")]
    public float maxCookTime = 4f; // Граничний час утримування гранати
}
