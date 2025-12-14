using UnityEngine;

public class GlobalAudioListener : MonoBehaviour
{
    public static bool Exists { get; private set; }

    private void Awake()
    {
        if (Exists)
        {
            Destroy(gameObject); // Запобігає дублюванню
            return;
        }

        Exists = true;
        DontDestroyOnLoad(gameObject);
    }

    private void OnDestroy()
    {
        if (ReferenceEquals(this, null)) return;
        Exists = false;
    }
}
