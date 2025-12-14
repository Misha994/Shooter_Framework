using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class UseExitInputKeyboard : MonoBehaviour, IUseExitInput
{
#if ENABLE_INPUT_SYSTEM
    [SerializeField] private Key useKey = Key.E;
    [SerializeField] private Key exitKey = Key.F;
#else
    [SerializeField] private KeyCode useKey = KeyCode.E;
    [SerializeField] private KeyCode exitKey = KeyCode.F;
#endif

    public bool IsUsePressed()
    {
#if ENABLE_INPUT_SYSTEM
        return Keyboard.current != null && Keyboard.current[useKey].wasPressedThisFrame;
#else
        return Input.GetKeyDown(useKey);
#endif
    }

    public bool IsExitPressed()
    {
#if ENABLE_INPUT_SYSTEM
        return Keyboard.current != null && Keyboard.current[exitKey].wasPressedThisFrame;
#else
        return Input.GetKeyDown(exitKey);
#endif
    }
}
